using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// 表达式解析器基类，提供公共的表达式处理方式
    /// </summary>
    public class ExpressionVisitorBase : ExpressionVisitor
    {
        #region 私有字段

        private IDbQueryProvider _provider = null;
        private TableAliasCache _aliases = null;
        private Expression _expression = null;
        private IDictionary<string, MemberExpression> _navMembers = null;

        /// <summary>
        /// SQL 构造器
        /// </summary>
        protected ISqlBuilder _builder = null;

        /// <summary>
        /// 方法解析器
        /// </summary>
        protected MethodCallExpressionVisitor _methodVisitor = null;

        /// <summary>
        /// 成员访问痕迹
        /// </summary>
        protected MemberVisitedMark _visitedMark = null;

        //防SQL注入字符
        //private static readonly Regex RegSystemThreats = 
        //new Regex(@"\s?or\s*|\s?;\s?|\s?drop\s|\s?grant\s|^'|\s?--|\s?union\s|\s?delete\s|\s?truncate\s|" +
        //    @"\s?sysobjects\s?|\s?xp_.*?|\s?syslogins\s?|\s?sysremote\s?|\s?sysusers\s?|\s?sysxlogins\s?|\s?sysdatabases\s?|\s?aspnet_.*?|\s?exec\s?",
        //    RegexOptions.Compiled | RegexOptions.IgnoreCase);

        #endregion

        #region 公开属性

        /// <summary>
        /// 即将解析的表达式
        /// </summary>
        public Expression Expression
        {
            get { return _expression; }
        }

        /// <summary>
        /// 即将解析的表达式
        /// </summary>
        public ISqlBuilder SqlBuilder
        {
            get { return _builder; }
        }

        /// <summary>
        /// 导航属性表达式列表
        /// </summary>
        public IDictionary<string, MemberExpression> NavMembers
        {
            get { return _navMembers; }
        }

        /// <summary>
        /// 成员访问痕迹
        /// </summary>
        public MemberVisitedMark VisitedMark { get { return _visitedMark; } }

        #endregion

        #region 构造函数

        /// <summary>
        /// 初始化 <see cref="ExpressionVisitorBase"/> 类的新实例
        /// </summary>
        public ExpressionVisitorBase(IDbQueryProvider provider, TableAliasCache aliases, Expression exp, bool useNominate = true)
        {
            _provider = provider;
            _aliases = aliases;
            _expression = exp;
            _visitedMark = new MemberVisitedMark();
            _navMembers = new Dictionary<string, MemberExpression>();
        }

        #endregion

        #region 重写方法

        /// <summary>
        /// 将表达式所表示的SQL片断写入SQL构造器
        /// </summary>
        public virtual void Write(ISqlBuilder builder)
        {
            _builder = builder;
            if (_methodVisitor == null) _methodVisitor = _provider.CreateMethodVisitor(this);
            if (_expression != null) this.Visit(_expression);
        }

        /// <summary>
        /// 访问二元表达式
        /// </summary>
        protected override Expression VisitBinary(BinaryExpression node)
        {
            return this.VisitWithoutRemark(x => this.VisitBinaryImpl(node));
        }

        /// <summary>
        /// 访问二元表达式
        /// </summary>
        protected virtual Expression VisitBinaryImpl(BinaryExpression b)
        {
            if (b == null) return b;

            // array[0]
            if (b.NodeType == ExpressionType.ArrayIndex) return this.Visit(b.Evaluate());

            Expression left = b.Left.ReduceUnary();
            Expression right = b.Right.ReduceUnary();
            if (b.NodeType == ExpressionType.AndAlso || b.NodeType == ExpressionType.OrElse)
            {
                // expression maybe a.Name == "TAN" && a.Allowused
                left = TryMakeBinary(b.Left);
                right = TryMakeBinary(b.Right);

                if (left != b.Left || right != b.Right)
                {
                    b = Expression.MakeBinary(b.NodeType, left, right);
                    return this.Visit(b);
                }
            }

            // 例： a.Name ?? "TAN"
            if (b.NodeType == ExpressionType.Coalesce) return _methodVisitor.Visit(b, MethodCall.Coalesce);

            // 例： a.Name == null
            ConstantExpression constExpression = left as ConstantExpression ?? right as ConstantExpression;
            if (constExpression != null && constExpression.Value == null) return _methodVisitor.Visit(b, MethodCall.EqualNull);

            // 例： a.Name == a.FullName  or like a.Name == "TAN"
            return this.VisitBinary_Condition(b);
        }

        protected override Expression VisitConditional(ConditionalExpression node)
        {
            // 例： a.Name == null ? "TAN" : a.Name => CASE WHEN a.Name IS NULL THEN 'TAN' ELSE a.Name End

            Expression testExpression = this.TryMakeBinary(node.Test, false);
            Expression ifTrueExpression = this.TryMakeBinary(node.IfTrue, true);
            Expression ifFalseExpression = this.TryMakeBinary(node.IfFalse, true);

            _builder.Append("(CASE WHEN ");
            this.Visit(testExpression);
            _builder.Append(" THEN ");
            this.Visit(ifTrueExpression);
            _builder.Append(" ELSE ");
            this.Visit(ifFalseExpression);
            _builder.Append(" END)");

            return node;
        }

        protected override Expression VisitConstant(ConstantExpression c)
        {
            //fix# char ~~，because expression tree converted char type 2 int.
            if (c != null && c.Value != null)
            {
                MemberExpression visited = _visitedMark.Current;
                bool isChar = visited != null && (visited.Type == typeof(char) || visited.Type == typeof(char?)) && c.Type == typeof(int);
                if (isChar)
                {
                    char @char = Convert.ToChar(c.Value);
                    c = Expression.Constant(@char, typeof(char));
                }
            }

            _builder.Append(c.Value, _visitedMark.Current);
            return c;
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            // 1.<>h__TransparentIdentifier3.b.Client.ClientName
            // 2.<>h__TransparentIdentifier3.b.Client.ClientName.Length
            // 3.<>h__TransparentIdentifier3.b.Client.Address.AddressName
            // 4.<>h__TransparentIdentifier3.b.ClientName
            // <>h__TransparentIdentifier2.<>h__TransparentIdentifier3.b.ClientName
            // <>h__TransparentIdentifier2.<>h__TransparentIdentifier3.b.Client.ClientName
            // <>h__TransparentIdentifier2.<>h__TransparentIdentifier3.b.Client.Address.AddressName
            // 5.b.ClientName


            if (node == null) return node;
            // => a.ActiveDate == DateTime.Now  => a.State == (byte)state
            if (node.CanEvaluate()) return this.VisitConstant(node.Evaluate());
            // => DateTime.Now
            if (node.Type == typeof(DateTime) && node.Expression == null) return _methodVisitor.Visit(node, MethodCall.MemberMember);
            // => a.Nullable.Value
            bool isNullable = node.Expression.Type.IsGenericType && node.Member.Name == "Value" && node.Expression.Type.GetGenericTypeDefinition() == typeof(Nullable<>);
            if (isNullable) return this.Visit(node.Expression);

            _visitedMark.Add(node);
            // => a.Name.Length
            if (TypeUtils.IsPrimitiveType(node.Expression.Type)) return _methodVisitor.Visit(node, MethodCall.MemberMember);
            // => <>h__3.b.ClientName
            if (!node.Expression.Acceptable()) return _builder.AppendMember(node, _aliases);
            // => a.Accounts[0].Markets[0].MarketId
            // => b.Client.Address.AddressName
            Expression objExpression = node.Expression;
            bool isMethodCall = objExpression != null && objExpression.NodeType == ExpressionType.Call;
            if (isMethodCall)
            {
                MethodCallExpression methodExpression = objExpression as MethodCallExpression;
                bool isGetItem = methodExpression.IsGetListItem();
                if (isGetItem) objExpression = methodExpression.Object;
            }
            // => b.Client.Address.AddressName
            this.VisitNavMember(objExpression, node.Member.Name);

            return node;
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            // => List<int>[]
            if (node.CanEvaluate()) return this.VisitConstant(node.Evaluate());
            return _methodVisitor.Visit(node, MethodCall.MethodCall);
        }

        protected override Expression VisitUnary(UnaryExpression u)
        {
            return _methodVisitor.Visit(u, MethodCall.Unary);
        }

        // 访问表达式树后自动删掉访问的成员痕迹
        protected void VisitWithoutRemark(Action<object> visit)
        {
            int visitedQty = _visitedMark.Count;
            visit(null);
            if (_visitedMark.Count != visitedQty) _visitedMark.Remove(_visitedMark.Count - visitedQty);
        }

        /// <summary>
        /// 访问表达式树后自动删掉访问的成员痕迹
        /// </summary>
        /// <param name="visit">访问实现</param>
        /// <returns></returns>
        public Expression VisitWithoutRemark(Func<object, Expression> visit)
        {
            int visitedQty = _visitedMark.Count;
            var newNode = visit(null);
            if (_visitedMark.Count != visitedQty) _visitedMark.Remove(_visitedMark.Count - visitedQty);
            return newNode;
        }

        #endregion

        #region 私有函数

        protected virtual Expression VisitBinary_Condition(BinaryExpression b)
        {
            // 例： a.Name == a.FullName 
            // or like a.Name == "TAN"

            if (b == null) return b;
            // 字符相加
            else if (b.NodeType == ExpressionType.Add && b.Type == typeof(string)) return _methodVisitor.Visit(b, MethodCall.BinaryCall);
            // 取模运算
            else if (b.NodeType == ExpressionType.Modulo) return _methodVisitor.Visit(b, MethodCall.BinaryCall);
            else
            {
                // 常量表达式放在右边，以充分利用 MemberVisitedMark
                string oper = this.GetOperator(b);
                Expression left = b.Left.CanEvaluate() ? b.Right : b.Left;
                Expression right = b.Left.CanEvaluate() ? b.Left : b.Right;

                bool use = this.UseBracket(b, b.Left);
                if (use) _builder.Append('(');
                this.Visit(left);
                if (use) _builder.Append(')');

                _builder.Append(oper);

                bool use2 = this.UseBracket(b, b.Right);
                if (use2) _builder.Append('(');
                this.Visit(right);
                if (use2) _builder.Append(')');

                return b;
            }
        }

        // 访问导航属性
        protected virtual string VisitNavMember(Expression expression, string memberName = null)
        {
            // 表达式 => b.Client.Address.AddressName
            Expression node = expression;
            Stack<KeyValuePair<string, MemberExpression>> stack = null;
            string alias = string.Empty;
            while (node != null && node.Acceptable())
            {
                if (node.NodeType != ExpressionType.MemberAccess) break;

                if (stack == null) stack = new Stack<KeyValuePair<string, MemberExpression>>();
                MemberExpression memberExpression = node as MemberExpression;

                TypeRuntimeInfo typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo(memberExpression.Expression.Type);
                ForeignKeyAttribute attribute = typeRuntime.GetInvokerAttribute<ForeignKeyAttribute>(memberExpression.Member.Name);
                if (attribute == null) break;

                string key = memberExpression.GetKeyWidthoutAnonymous();
                stack.Push(new KeyValuePair<string, MemberExpression>(key, memberExpression));
                node = memberExpression.Expression;
                if (node.NodeType == ExpressionType.Call) node = (node as MethodCallExpression).Object;
            }

            if (stack != null && stack.Count > 0)
            {
                while (stack != null && stack.Count > 0)
                {
                    KeyValuePair<string, MemberExpression> kvp = stack.Pop();
                    string key = kvp.Key;
                    MemberExpression m = kvp.Value;
                    Type type = m.Type;
                    if (type.IsGenericType) type = type.GetGenericArguments()[0];

                    TypeRuntimeInfo typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo(type);
                    // 检查查询表达式是否显示指定该表关联
                    alias = _aliases.GetJoinTableAlias(typeRuntime.TableName);
                    if (string.IsNullOrEmpty(alias))
                    {
                        // 如果没有，则使用导航属性别名
                        alias = _aliases.GetNavigationTableAlias(key);
                        if (!_navMembers.ContainsKey(kvp.Key)) _navMembers.Add(kvp);
                    }

                    // 例： a.Client.ClientId
                    if (stack.Count == 0 && !string.IsNullOrEmpty(memberName)) _builder.AppendMember(alias, memberName);
                }
            }
            else
            {
                // => SelectMany 也会产生类似 'b.Client.Address.AddressName' 这样的表达式
                alias = _aliases.GetTableAlias(expression);
                _builder.AppendMember(alias, memberName);
            }

            // fix issue# Join 表达式显式指定导航属性时时，alias 为空
            return alias;
        }

        protected virtual Expression TryMakeBinary(Expression expression, bool skipConstant = false)
        {
            if (expression.Type != typeof(bool)) return expression;
            else if (expression.NodeType == ExpressionType.Constant && skipConstant) return expression;

            Expression left = null;
            Expression right = null;

            if (expression.NodeType == ExpressionType.Constant)
            {
                // true => 1=2
                left = Expression.Constant(1);
                right = Expression.Constant(Convert.ToBoolean(((ConstantExpression)expression).Value) ? 1 : 2);
            }
            else if (expression.NodeType == ExpressionType.MemberAccess)
            {
                // a.FieldName => a.FieldName = 1
                left = expression;
                right = Expression.Constant(true);
            }
            else if (expression.NodeType == ExpressionType.Not)
            {
                var unaryExpression = expression as UnaryExpression;
                if (unaryExpression.Operand.NodeType == ExpressionType.MemberAccess)
                {
                    // !a.FieldName => a.FieldName = 0
                    left = ((UnaryExpression)expression).Operand;
                    right = Expression.Constant(false);
                }
            }

            if (left != null)
                expression = Expression.MakeBinary(ExpressionType.Equal, left, right);
            return expression;
        }

        protected virtual string GetOperator(BinaryExpression b)
        {
            string opr = string.Empty;
            switch (b.NodeType)
            {
                case ExpressionType.Equal:
                    opr = " = ";
                    break;
                case ExpressionType.NotEqual:
                    opr = " <> ";
                    break;
                case ExpressionType.GreaterThan:
                    opr = " > ";
                    break;
                case ExpressionType.GreaterThanOrEqual:
                    opr = " >= ";
                    break;
                case ExpressionType.LessThan:
                    opr = " < ";
                    break;
                case ExpressionType.LessThanOrEqual:
                    opr = " <= ";
                    break;
                case ExpressionType.And:
                case ExpressionType.AndAlso:
                    opr = b.Type == typeof(bool) ? " AND " : " & ";
                    break;
                case ExpressionType.Or:
                case ExpressionType.OrElse:
                    opr = b.Type == typeof(bool) ? " OR " : " | ";
                    break;
                case ExpressionType.Add:
                case ExpressionType.AddChecked:
                    opr = " + ";
                    break;
                case ExpressionType.Subtract:
                case ExpressionType.SubtractChecked:
                    opr = " - ";
                    break;
                case ExpressionType.Multiply:
                case ExpressionType.MultiplyChecked:
                    opr = " * ";
                    break;
                case ExpressionType.Divide:
                    opr = " / ";
                    break;
                case ExpressionType.Modulo:
                    opr = " % ";
                    break;
                case ExpressionType.Coalesce:
                    opr = "ISNULL";
                    break;
                default:
                    throw new NotSupportedException(string.Format("{0} is not supported.", b.NodeType));
            }

            return opr;
        }

        // 判断是否需要括号
        protected bool UseBracket(Expression expression, Expression subExpression = null)
        {
            if (subExpression != null)
            {
                UnaryExpression unaryExpression = subExpression as UnaryExpression;
                if (unaryExpression != null) return this.UseBracket(expression, unaryExpression.Operand);

                InvocationExpression invokeExpression = subExpression as InvocationExpression;
                if (invokeExpression != null) return this.UseBracket(expression, invokeExpression.Expression);

                LambdaExpression lambdaExpression = subExpression as LambdaExpression;
                if (lambdaExpression != null) return this.UseBracket(expression, lambdaExpression.Body);

                BinaryExpression b = subExpression as BinaryExpression;
                if (b != null)
                {
                    if (expression.NodeType == ExpressionType.OrElse)
                        return true;
                }
            }

            return this.GetPriority(expression) < this.GetPriority(subExpression);
        }

        protected int GetPriority(Expression expression)
        {
            switch (expression.NodeType)
            {
                case ExpressionType.Add:
                case ExpressionType.AddChecked:
                case ExpressionType.ExclusiveOr:
                case ExpressionType.Subtract:
                case ExpressionType.SubtractChecked:
                    return 3;
                case ExpressionType.And:
                    return expression.Type == typeof(bool) ? 6 : 3;
                case ExpressionType.AndAlso:
                    return 6;
                case ExpressionType.Divide:
                case ExpressionType.Modulo:
                case ExpressionType.Multiply:
                case ExpressionType.MultiplyChecked:
                    return 2;
                case ExpressionType.Equal:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                case ExpressionType.NotEqual:
                    return 4;
                case ExpressionType.Not:
                    return expression.Type == typeof(bool) ? 5 : 1;
                case ExpressionType.Or:
                    return expression.Type == typeof(bool) ? 7 : 3;
                case ExpressionType.OrElse:
                    return 7;
                default:
                    return 0;
            }
        }

        #endregion
    }
}