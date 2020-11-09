using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Riz.XFramework.Data
{
    /// <summary>
    /// 表达式解析器基类，提供公共的表达式处理方式
    /// </summary>
    public class LinqExpressionVisitor : ExpressionVisitor
    {
        #region 私有字段

        private TableAliasResolver _aliasResolver = null;
        private Expression _expression = null;
        private HashCollection<NavMember> _navMembers = null;

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
        protected MemberVisitedStack _visitedStack = null;

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
        public HashCollection<NavMember> NavMembers
        {
            get { return _navMembers; }
        }

        /// <summary>
        /// 成员访问痕迹
        /// </summary>
        public MemberVisitedStack VisitedStack { get { return _visitedStack; } }

        #endregion

        #region 构造函数

        /// <summary>
        /// 初始化 <see cref="LinqExpressionVisitor"/> 类的新实例
        /// </summary>
        /// <param name="aliasResolver">表别名解析器</param>
        /// <param name="expression">将访问的表达式</param>
        public LinqExpressionVisitor(TableAliasResolver aliasResolver, Expression expression)
        {
            _aliasResolver = aliasResolver;
            _expression = expression;
            _visitedStack = new MemberVisitedStack();
            _navMembers = new HashCollection<NavMember>();
        }

        #endregion

        #region 重写方法

        /// <summary>
        /// 将表达式所表示的SQL片断写入SQL构造器
        /// </summary>
        /// <param name="builder">SQL 语句生成器</param>
        public virtual void Write(ISqlBuilder builder)
        {
            this.Initialize(builder);
            if (_expression != null) this.Visit(_expression);
        }

        /// <summary>
        /// 访问二元表达式
        /// </summary>
        /// <param name="node">二元表达式</param>
        /// <returns></returns>
        protected override Expression VisitBinary(BinaryExpression node)
        {
            return this.VisitWithoutRemark(a => this.VisitBinaryImpl(node));
        }

        // 访问二元表达式
        Expression VisitBinaryImpl(BinaryExpression b)
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
            var constExpression = left as ConstantExpression ?? right as ConstantExpression;
            if (constExpression != null && constExpression.Value == null) return _methodVisitor.Visit(b, MethodCall.EqualNull);

            // 例： a.Name == a.FullName  or like a.Name == "TAN"
            return this.VisitBinary_Condition(b);
        }

        /// <summary>
        /// 访问包含条件运算符的表达式
        /// </summary>
        /// <param name="node">要访问的表达式</param>
        /// <returns></returns>
        protected override Expression VisitConditional(ConditionalExpression node)
        {
            // 例： a.Name == null ? "TAN" : a.Name => CASE WHEN a.Name IS NULL THEN 'TAN' ELSE a.Name End

            Expression testExpression = this.TryMakeBinary(node.Test);

            _builder.Append("(CASE WHEN ");
            this.Visit(testExpression);
            _builder.Append(" THEN ");
            this.Visit(node.IfTrue);
            _builder.Append(" ELSE ");
            this.Visit(node.IfFalse);
            _builder.Append(" END)");

            return node;
        }

        /// <summary>
        /// 访问常量表达式
        /// </summary>
        /// <param name="c">常量表达式</param>
        /// <returns></returns>
        protected override Expression VisitConstant(ConstantExpression c)
        {
            //fix# char ~~，because expression tree converted char type 2 int.
            if (c != null && c.Value != null)
            {
                var m = _visitedStack.Current;
                bool isChar = m != null && (m.DataType == typeof(char) || m.DataType == typeof(char?)) && c.Type == typeof(int);
                if (isChar)
                {
                    char @char = Convert.ToChar(c.Value);
                    c = Expression.Constant(@char, typeof(char));
                }
            }

            _builder.Append(c.Value, _visitedStack.Current);
            return c;
        }

        /// <summary>
        /// 访问字段或者属性表达式
        /// </summary>
        /// <param name="node">字段或者成员表达式</param>
        /// <returns></returns>
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
            if (isNullable)
            {
                this.Visit(node.Expression);
                return node;
            }

            // 记录访问成员栈
            _visitedStack.Add(node);

            // => a.Name.Length
            if (TypeUtils.IsPrimitiveType(node.Expression.Type)) return _methodVisitor.Visit(node, MethodCall.MemberMember);
            // => <>h__3.b.ClientName
            if (!node.Expression.Visitable())
            {
                _builder.AppendMember(_aliasResolver, node);
                return node;
            }
            // => a.Accounts[0].Markets[0].MarketId
            // => b.Client.Address.AddressName
            Expression objExpression = node.Expression;
            bool isMethodCall = objExpression != null && objExpression.NodeType == ExpressionType.Call;
            if (isMethodCall)
            {
                MethodCallExpression methodExpression = objExpression as MethodCallExpression;
                bool isIndex = methodExpression.IsCollectionIndex();
                if (isIndex) objExpression = methodExpression.Object;
            }
            // => b.Client.Address.AddressName
            this.VisitNavMember(objExpression, TypeUtils.GetFieldName(node.Member, node.Expression.Type));

            return node;
        }

        /// <summary>
        /// 访问方法表达式
        /// </summary>
        /// <param name="node">方法表达式</param>
        /// <returns></returns>
        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            // => List<int>[]
            if (node.CanEvaluate()) return this.VisitConstant(node.Evaluate());
            return _methodVisitor.Visit(node, MethodCall.MethodCall);
        }

        /// <summary>
        /// 访问一元表达式
        /// </summary>
        /// <param name="u">一元表达式</param>
        /// <returns></returns>
        protected override Expression VisitUnary(UnaryExpression u)
        {
            return _methodVisitor.Visit(u, MethodCall.Unary);
        }

        /// <summary>
        /// 访问表达式树后自动删掉访问的成员痕迹
        /// </summary>
        /// <param name="visit">访问委托</param>
        protected void VisitWithoutRemark(Action<object> visit)
        {
            int visitedQty = _visitedStack.Count;
            visit(null);
            if (_visitedStack.Count != visitedQty) _visitedStack.Remove(_visitedStack.Count - visitedQty);
        }

        /// <summary>
        /// 访问表达式树后自动删掉访问的成员痕迹
        /// </summary>
        /// <param name="visit">访问实现</param>
        /// <returns></returns>
        public Expression VisitWithoutRemark(Func<object, Expression> visit)
        {
            int visitedQty = _visitedStack.Count;
            var newNode = visit(null);
            if (_visitedStack.Count != visitedQty) _visitedStack.Remove(_visitedStack.Count - visitedQty);
            return newNode;
        }

        #endregion

        #region 私有函数

        /// <summary>
        /// 访问二元表达式
        /// </summary>
        /// <param name="b">二元表达式</param>
        /// <returns></returns>
        protected virtual Expression VisitBinary_Condition(BinaryExpression b)
        {
            // 例： a.Name == a.FullName 
            // or like a.Name == "TAN"

            if (b == null) return b;
            // 取模运算
            else if (b.NodeType == ExpressionType.Modulo) return _methodVisitor.Visit(b, MethodCall.BinaryCall);
            // 除法运算
            else if (b.NodeType == ExpressionType.Divide) return _methodVisitor.Visit(b, MethodCall.BinaryCall);
            // 字符相加
            else if (b.NodeType == ExpressionType.Add && b.Type == typeof(string)) return _methodVisitor.Visit(b, MethodCall.BinaryCall);
            else
            {
                // 常量表达式放在右边，以充分利用 MemberVisitedMark
                string oper = this.GetOperator(b);
                bool use = this.UseBracket(b, b.Left);

                if (use) _builder.Append('(');
                this.Visit(b.Left);
                if (use) _builder.Append(')');

                _builder.Append(oper);

                bool use2 = this.UseBracket(b, b.Right);
                if (use2) _builder.Append('(');
                this.Visit(b.Right);
                if (use2) _builder.Append(')');

                return b;
            }
        }

        /// <summary>
        /// 访问导航属性
        /// </summary>
        /// <param name="expression">导航属性表达式</param>
        /// <param name="memberName">成员名称</param>
        /// <returns></returns>
        protected virtual string VisitNavMember(Expression expression, string memberName = null)
        {
            // 表达式 => b.Client.Address.AddressName
            Expression node = expression;
            Stack<NavMember> stack = null;
            string alias = string.Empty;
            while (node != null && node.Visitable())
            {
                if (node.NodeType != ExpressionType.MemberAccess) break;

                if (stack == null) stack = new Stack<NavMember>();
                var member = node as MemberExpression;

                var typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo(member.Expression.Type);
                ForeignKeyAttribute attribute = typeRuntime.GetMemberAttribute<ForeignKeyAttribute>(member.Member.Name);
                if (attribute == null)
                {
                    
                }

                string key = member.GetKeyWidthoutAnonymous();
                stack.Push(new NavMember(key, member));
                node = member.Expression;
                if (node.NodeType == ExpressionType.Call) node = (node as MethodCallExpression).Object;
            }

            if (stack != null && stack.Count > 0)
            {
                while (stack != null && stack.Count > 0)
                {
                    NavMember nav = stack.Pop();
                    Type type = nav.Expression.Type;
                    if (type.IsGenericType) type = type.GetGenericArguments()[0];

                    var typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo(type);
                    // 检查表达式是否由 GetTable<,>(path) 显式指定过别名
                    alias = _aliasResolver.GetGetTableAlias(nav.Key);
                    if (string.IsNullOrEmpty(alias))
                    {
                        // 如果没有，检查查询表达式是否显示指定该表关联
                        alias = _aliasResolver.GetJoinTableAlias(typeRuntime.TableName);
                    }
                    if (string.IsNullOrEmpty(alias))
                    {
                        // 如果没有，则使用导航属性别名
                        alias = _aliasResolver.GetNavTableAlias(nav.Key);
                        if (!_navMembers.Contains(nav.Key)) _navMembers.Add(nav);
                    }

                    // 例： a.Client.ClientId
                    if (stack.Count == 0 && !string.IsNullOrEmpty(memberName)) _builder.AppendMember(alias, memberName);
                }
            }
            else
            {
                // => SelectMany 也会产生类似 'b.Client.Address.AddressName' 这样的表达式
                alias = _aliasResolver.GetTableAlias(expression);
                _builder.AppendMember(alias, memberName);
            }

            // Fix issue# Join 表达式显式指定导航属性时时，alias 为空
            return alias;
        }

        /// <summary>
        /// 尝试将一元表达式转换成二元表达式，如 TRUE=>1 == 1
        /// </summary>
        /// <param name="expression">将要转换的表达式</param>
        /// <returns></returns>
        protected virtual Expression TryMakeBinary(Expression expression)
        {
            if (expression.Type != typeof(bool)) return expression;
            //else if (expression.NodeType == ExpressionType.Constant && ignoreConst) return expression;

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

        /// <summary>
        /// 使用 SqlBuilder 初始化
        /// </summary>
        /// <param name="builder"></param>
        protected virtual void Initialize(ISqlBuilder builder)
        {
            _builder = builder;
            var context = _builder.TranslateContext.DbContext;
            if (_methodVisitor == null)
                _methodVisitor = context.Provider.CreateMethodCallVisitor(this);
        }

        // 获取二元表达式对应的操作符
        string GetOperator(BinaryExpression b)
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
        bool UseBracket(Expression expression, Expression subExpression = null)
        {
            if (subExpression != null)
            {
                var unary = subExpression as UnaryExpression;
                if (unary != null) return this.UseBracket(expression, unary.Operand);

                var invocation = subExpression as InvocationExpression;
                if (invocation != null) return this.UseBracket(expression, invocation.Expression);

                var lambda = subExpression as LambdaExpression;
                if (lambda != null) return this.UseBracket(expression, lambda.Body);

                var b = subExpression as BinaryExpression;
                if (b != null)
                {
                    if (expression.NodeType == ExpressionType.OrElse)
                        return true;
                }
            }

            return this.GetPriority(expression) < this.GetPriority(subExpression);
        }

        // 获取表达式优先级
        int GetPriority(Expression expression)
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