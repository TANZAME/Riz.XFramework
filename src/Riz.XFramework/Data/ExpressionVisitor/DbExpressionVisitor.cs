using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Riz.XFramework.Data
{
    /// <summary>
    /// 表达式解析器基类，提供公共的表达式处理方式
    /// </summary>
    public class DbExpressionVisitor : ExpressionVisitor
    {
        #region 私有字段

        private AliasGenerator _ag = null;
        private ISqlBuilder _builder = null;
        private DbQueryProvider _provider = null;
        private MemberVisitedStack _visitedStack = null;
        private HashCollection<NavMember> _navMembers = null;
        private MethodCallExpressionVisitor _methodCallVisitor = null;

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
        public ISqlBuilder SqlBuilder => _builder;

        /// <summary>
        /// 导航属性表达式列表
        /// </summary>
        public HashCollection<NavMember> NavMembers
        {
            get
            {
                if (this._navMembers == null)
                    this._navMembers = new HashCollection<NavMember>();
                return this._navMembers;
            }
        }

        /// <summary>
        /// 成员访问栈，用于在给成员的值确定数据类型（DbType）
        /// </summary>
        public MemberVisitedStack VisitedStack => this._visitedStack;

        #endregion

        #region 构造函数

        /// <summary>
        /// 初始化 <see cref="DbExpressionVisitor"/> 类的新实例
        /// </summary>
        /// <param name="ag">表别名解析器</param>
        /// <param name="builder">SQL 语句生成器</param>
        public DbExpressionVisitor(AliasGenerator ag, ISqlBuilder builder)
        {
            _ag = ag;
            _builder = builder;
            _provider = (DbQueryProvider)_builder.Provider;
            _visitedStack = new MemberVisitedStack();
        }

        #endregion

        #region 重写方法

        /// <summary>
        /// 访问表达式节点
        /// </summary>
        /// <param name="dbExpression">将访问的表达式</param>
        public virtual Expression Visit(DbExpression dbExpression) => base.Visit(dbExpression != null && dbExpression.HasExpression ? dbExpression.Expressions[0] : null);

        /// <summary>
        /// 访问表达式节点
        /// </summary>
        /// <param name="dbExpressions">将访问的表达式</param>
        public virtual Expression Visit(List<DbExpression> dbExpressions)
        {
            for (int index = 0; index < (dbExpressions != null ? dbExpressions.Count : -1); index++)
            {
                DbExpression d = dbExpressions[index];
                base.Visit(d != null && d.HasExpression ? d.Expressions[0] : null);
            }

            return null;
        }

        /// <summary>
        /// 访问二元表达式
        /// </summary>
        /// <param name="node">二元表达式</param>
        /// <returns></returns>
        protected override Expression VisitBinary(BinaryExpression node) => this.VisitWithoutRemark(_ => this.VisitBinaryImpl(node));

        // 访问二元表达式节点
        private Expression VisitBinaryImpl(BinaryExpression node)
        {
            if (node == null) return node;

            // array[0]
            if (node.NodeType == ExpressionType.ArrayIndex) return this.Visit(node.Evaluate());

            Expression left = node.Left.ReduceUnary();
            Expression right = node.Right.ReduceUnary();
            if (node.NodeType == ExpressionType.AndAlso || node.NodeType == ExpressionType.OrElse)
            {
                // expression maybe a.Name == "TAN" && a.Allowused
                left = TryFixBinary(node.Left);
                right = TryFixBinary(node.Right);

                if (left != node.Left || right != node.Right)
                {
                    node = Expression.MakeBinary(node.NodeType, left, right);
                    return this.Visit(node);
                }
            }

            // 例： a.Name ?? "TAN"
            if (node.NodeType == ExpressionType.Coalesce) return this.VisitMethodCall(node, MethodCallType.Coalesce);

            // 例： a.Name == null
            var constExpression = left as ConstantExpression ?? right as ConstantExpression;
            if (constExpression != null && constExpression.Value == null) return this.VisitMethodCall(node, MethodCallType.EqualNull);

            // 例： a.Name == a.FullName  or like a.Name == "TAN"
            return this.VisitBinary_Condition(node);
        }

        /// <summary>
        /// 访问包含条件运算符的表达式
        /// </summary>
        /// <param name="node">要访问的表达式</param>
        /// <returns></returns>
        protected override Expression VisitConditional(ConditionalExpression node)
        {
            // 例： a.Name == null ? "TAN" : a.Name => CASE WHEN a.Name IS NULL THEN 'TAN' ELSE a.Name End

            Expression testExpression = this.TryFixBinary(node.Test);

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
            if (node.Type == typeof(DateTime) && node.Expression == null) return this.VisitMethodCall(node, MethodCallType.MemberMember);
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
            if (TypeUtils.IsPrimitiveType(node.Expression.Type)) return this.VisitMethodCall(node, MethodCallType.MemberMember);
            // => <>h__3.b.ClientName
            if (!node.Expression.Visitable())
            {
                _builder.AppendMember(_ag, node);
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
            if (node.CanEvaluate())
                return this.VisitConstant(node.Evaluate());
            else
                return this.VisitMethodCall(node, MethodCallType.MethodCall);
        }

        /// <summary>
        /// 访问一元表达式
        /// </summary>
        /// <param name="u">一元表达式</param>
        /// <returns></returns>
        protected override Expression VisitUnary(UnaryExpression u) => this.VisitMethodCall(u, MethodCallType.Unary);

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
        /// 访问方法表达式
        /// </summary>
        /// <param name="node">方法表达式</param>
        /// <param name="method">方法类型</param>
        /// <returns></returns>
        protected Expression VisitMethodCall(Expression node, MethodCallType method)
        {
            if (_methodCallVisitor == null)
                _methodCallVisitor = _provider.CreateMethodCallVisitor(this);
            _methodCallVisitor.Visit(node, method);
            return node;
        }

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
            else if (b.NodeType == ExpressionType.Modulo) return this.VisitMethodCall(b, MethodCallType.BinaryCall);
            // 除法运算
            else if (b.NodeType == ExpressionType.Divide) return this.VisitMethodCall(b, MethodCallType.BinaryCall);
            // 字符相加
            else if (b.NodeType == ExpressionType.Add && b.Type == typeof(string)) return this.VisitMethodCall(b, MethodCallType.BinaryCall);
            else
            {
                // 常量表达式放在右边，以充分利用 MemberVisitedMark
                string @operator = this.GetOperator(b);
                bool useBracket = this.UseBracket(b, b.Left);

                if (useBracket) _builder.Append('(');
                this.Visit(b.Left);
                if (useBracket) _builder.Append(')');

                _builder.Append(@operator);

                bool useBracket2 = this.UseBracket(b, b.Right);
                if (useBracket2) _builder.Append('(');
                this.Visit(b.Right);
                if (useBracket2) _builder.Append(')');

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
            Stack<NavMember> navStack = null;
            string alias = string.Empty;
            while (node != null && node.Visitable())
            {
                if (node.NodeType != ExpressionType.MemberAccess) break;

                if (navStack == null) navStack = new Stack<NavMember>();
                var member = node as MemberExpression;

                string key = member.GetKeyWidthoutAnonymous();
                navStack.Push(new NavMember(key, member));
                node = member.Expression;
                if (node.NodeType == ExpressionType.Call) node = (node as MethodCallExpression).Object;
            }

            if (navStack != null && navStack.Count > 0)
            {
                while (navStack != null && navStack.Count > 0)
                {
                    NavMember nav = navStack.Pop();
                    Type type = nav.Expression.Type;
                    if (type.IsGenericType) type = type.GetGenericArguments()[0];

                    var typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo(type);
                    // 检查表达式是否由 GetTable<,>(path) 显式指定过别名
                    alias = _ag.GetGetTableAlias(nav.Key);
                    if (string.IsNullOrEmpty(alias))
                    {
                        // 如果没有，检查查询表达式是否显示指定该表关联
                        alias = _ag.GetJoinTableAlias(typeRuntime.TableFullName);
                    }
                    if (string.IsNullOrEmpty(alias))
                    {
                        // 如果没有，则使用导航属性别名
                        alias = _ag.GetNavTableAlias(nav.Key);
                        if (!this.NavMembers.Contains(nav.Key)) this.NavMembers.Add(nav);
                    }

                    // 例： a.Client.ClientId
                    if (navStack.Count == 0 && !string.IsNullOrEmpty(memberName)) _builder.AppendMember(alias, memberName);
                }
            }
            else
            {
                // => SelectMany 也会产生类似 'b.Client.Address.AddressName' 这样的表达式
                alias = _ag.GetTableAlias(expression);
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
        protected Expression TryFixBinary(Expression expression)
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

        // 获取二元表达式对应的操作符
        private string GetOperator(BinaryExpression b)
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
        private bool UseBracket(Expression expression, Expression subExpression = null)
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
        private int GetPriority(Expression expression)
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