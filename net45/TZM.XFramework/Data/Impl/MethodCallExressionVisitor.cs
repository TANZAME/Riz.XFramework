
using System;
using System.Linq;
using System.Data;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace TZM.XFramework.Data.SqlClient
{
    /// <summary>
    /// <see cref="MethodCallExressionVisitor"/> 表达式访问器
    /// </summary>
    public abstract class MethodCallExressionVisitor : IMethodCallExressionVisitor
    {
        private ExpressionVisitorBase _visitor = null;
        private IDbQueryProvider _provider = null;
        private ISqlBuilder _builder = null;
        private VisitedMemberList _visitedMember = null;
        private static VistorTypeRuntimeInfo _typeRuntime = null;

        #region 构造函数

        /// <summary>
        /// 实例化 <see cref="MethodCallExressionVisitor"/> 类的新实例
        /// </summary>
        public MethodCallExressionVisitor(IDbQueryProvider provider, ExpressionVisitorBase visitor)
        {
            _provider = provider;
            _visitor = visitor;
            _builder = visitor.SqlBuilder;
            _visitedMember = _visitor.VisitedMember;
        }

        #endregion

        #region 接口方法

        /// <summary>
        /// 访问表示 null 判断运算的节点 a.Name == null
        /// </summary>
        public virtual Expression VisitEqualNull(BinaryExpression b)
        {
            // a.Name == null => a.Name Is Null
            Expression left = b.Left.NodeType == ExpressionType.Constant ? b.Right : b.Left;
            Expression right = b.Left.NodeType == ExpressionType.Constant ? b.Left : b.Right;
            string oper = b.NodeType == ExpressionType.Equal ? " IS " : " IS NOT ";

            _visitor.Visit(left);
            _builder.Append(oper);
            _visitor.Visit(right);

            return b;
        }

        /// <summary>
        /// 访问表示 null 合并运算的节点 a ?? b
        /// </summary>
        /// <param name="b">二元表达式节点</param>
        /// <returns></returns>
        public virtual Expression VisitCoalesce(BinaryExpression b)
        {
            // 例： a.Name ?? "TAN" => ISNULL(a.Name,'TAN')
            Expression left = b.Left.NodeType == ExpressionType.Constant ? b.Right : b.Left;
            Expression right = b.Left.NodeType == ExpressionType.Constant ? b.Left : b.Right;

            _builder.Append("ISNULL(");
            _visitor.Visit(left);
            _builder.Append(',');
            _visitor.Visit(right);
            _builder.Append(')');


            return b;
        }

        /// <summary>
        /// 访问表示方法调用的节点
        /// </summary>
        /// <param name="node">方法调用节点</param>
        /// <returns></returns>
        public virtual Expression VisitMethodCall(MethodCallExpression node)
        {
            Type type = node.Method.ReflectedType != null ? node.Method.ReflectedType : node.Method.DeclaringType;
            string methodName = string.Empty;

            if (type == typeof(SqlMethod) || type == typeof(Guid)) methodName = "VisitSQL";
            else if (type == typeof(string) || node.Method.Name == "ToString") methodName = "VisitSTR";
            else if (type == typeof(Enumerable) || typeof(IEnumerable).IsAssignableFrom(type)) methodName = "VisitENM";

            if (_typeRuntime == null) _typeRuntime = new VistorTypeRuntimeInfo(this.GetType());
            MemberInvokerBase invoker = _typeRuntime.GetInvoker(methodName + node.Method.Name);
            if (invoker != null)
            {
                object exp = invoker.Invoke(this, new object[] { node });
                return exp as Expression;
            }

            throw new XFrameworkException("{0}.{1} is not supported.", node.Method.DeclaringType, node.Method.Name);
        }

        /// <summary>
        /// 访问表示字段或者属性的属性的节点 a.Name.Length
        /// </summary>
        public virtual Expression VisitMemberMember(MemberExpression node)
        {
            Type type = node.Expression.Type;
            string methodName = string.Empty;

            if (type == typeof(string)) methodName = "VisitSTR";

            if (_typeRuntime == null) _typeRuntime = new VistorTypeRuntimeInfo(this.GetType());
            MemberInvokerBase invoker = _typeRuntime.GetInvoker(methodName + node.Member.Name);
            if (invoker != null)
            {
                object exp = invoker.Invoke(this, new object[] { node });
                return exp as Expression;
            }

            throw new XFrameworkException("{0}.{1} is not supported.", node.Member.DeclaringType, node.Member.Name);
        }

        /// <summary>
        /// 访问一元运算符
        /// </summary>
        /// <param name="node">一元运算符表达式</param>
        /// <returns></returns>
        public virtual Expression VisitUnary(UnaryExpression node)
        {
            //if (node.NodeType == ExpressionType.Convert && node.Type != node.Operand.Type && node.Operand.Type != typeof(char))
            //{

            //}
            _visitor.Visit(node.Operand);
            return node;
        }

        #endregion

        #region 虚有函数

        /// <summary>
        /// 访问 ToString 方法
        /// </summary>
        protected virtual Expression VisitSTRToString(MethodCallExpression m)
        {
            // => a.ID.ToString()
            Expression node = null;
            if (m.Object != null) node = m.Object;
            else if (m.Arguments != null && m.Arguments.Count > 0) node = m.Arguments[0];
            // 字符串不进行转换
            if (node == null || node.Type == typeof(string)) return _visitor.Visit(node);

            string name = "NVARCHAR";
            var member = _visitedMember.Current;
            var column = _builder.GetColumnAttribute(member != null ? member.Member : null, member != null ? member.Expression.Type : null);
            bool unicode = this.IsUnicode(column != null ? column.DbType : null);
            name = unicode ? "NVARCHAR" : "VARCHAR";

            if (column != null && column.Size > 0) name = string.Format("{0}({1})", name, column.Size);
            else if (column != null && column.Size == -1) name = string.Format("{0}(max)", name);
            else name = string.Format("{0}(max)", name);

            _builder.Append("CAST(");
            _visitor.Visit(node);
            _builder.Append(" AS ");
            _builder.Append(name);
            _builder.Append(')');

            return m;
        }

        /// <summary>
        /// 访问 Contains 方法
        /// </summary>
        protected virtual Expression VisitSTRContains(MethodCallExpression m)
        {
            // https://www.cnblogs.com/yangmingyu/p/6928209.html
            // 对于其他的特殊字符：'^'， '-'， ']' 因为它们本身在包含在 '[]' 中使用，所以需要用另外的方式来转义，于是就引入了 like 中的 escape 子句，另外值得注意的是：escape 可以转义所有的特殊字符。
            // EF 的 Like 不用参数化...

            if (m != null)
            {
                _visitor.Visit(m.Object);
                _builder.Append(" LIKE ");
                if (m.Arguments[0].CanEvaluate())
                {
                    bool unicode = true;
                    string value = this.GetSqlValue(m.Arguments[0].Evaluate(), ref unicode);

                    if (_builder.Parameterized)
                    {
                        _builder.Append("'%' + ");
                        _builder.Append(value);
                        _builder.Append(" + '%'");
                    }
                    else
                    {
                        if (unicode) _builder.Append('N');
                        _builder.Append("'%");
                        _builder.Append(value);
                        _builder.Append("%'");
                    }
                }
                else
                {
                    _builder.Append("('%' + ");
                    _visitor.Visit(m.Arguments[0]);
                    _builder.Append(" + '%')");
                }
            }

            return m;
        }

        /// <summary>
        /// 访问 StartWidth 方法
        /// </summary>
        protected virtual Expression VisitSTRStartsWith(MethodCallExpression m)
        {
            if (m != null)
            {
                _visitor.Visit(m.Object);
                _builder.Append(" LIKE ");
                if (m.Arguments[0].CanEvaluate())
                {
                    bool unicode = true;
                    string value = this.GetSqlValue(m.Arguments[0].Evaluate(), ref unicode);

                    if (_builder.Parameterized)
                    {
                        _builder.Append(value);
                        _builder.Append(" + '%'");
                    }
                    else
                    {
                        if (unicode) _builder.Append('N');
                        _builder.Append("'");
                        _builder.Append(value);
                        _builder.Append("%'");
                    }
                }
                else
                {
                    _builder.Append("(");
                    _visitor.Visit(m.Arguments[0]);
                    _builder.Append(" + '%')");
                }
            }

            return m;
        }

        /// <summary>
        /// 访问 EndWidth 方法
        /// </summary>
        protected virtual Expression VisitSTREndsWith(MethodCallExpression m)
        {
            if (m != null)
            {
                _visitor.Visit(m.Object);
                _builder.Append(" LIKE ");
                if (m.Arguments[0].CanEvaluate())
                {
                    bool unicode = true;
                    string value = this.GetSqlValue(m.Arguments[0].Evaluate(), ref unicode);

                    if (_builder.Parameterized)
                    {
                        _builder.Append("'%' + ");
                        _builder.Append(value);
                    }
                    else
                    {
                        if (unicode) _builder.Append('N');
                        _builder.Append("'%");
                        _builder.Append(value);
                        _builder.Append("'");
                    }
                }
                else
                {
                    _builder.Append("('%' + ");
                    _visitor.Visit(m.Arguments[0]);
                    _builder.Append(")");
                }
            }

            return m;
        }

        /// <summary>
        /// 访问 TrimStart 方法
        /// </summary>
        protected virtual Expression VisitSTRTrimStart(MethodCallExpression m)
        {
            if (m != null)
            {
                _builder.Append("LTRIM(");
                _visitor.Visit(m.Object != null ? m.Object : m.Arguments[0]);
                _builder.Append(")");
            }

            return m;
        }

        /// <summary>
        /// 访问 TrimEnd 方法
        /// </summary>
        protected virtual Expression VisitSTRTrimEnd(MethodCallExpression m)
        {
            if (m != null)
            {
                _builder.Append("RTRIM(");
                _visitor.Visit(m.Object != null ? m.Object : m.Arguments[0]);
                _builder.Append(")");
            }

            return m;
        }

        /// <summary>
        /// 访问 Trim 方法
        /// </summary>
        protected virtual Expression VisitSTRTrim(MethodCallExpression m)
        {
            if (m != null)
            {
                _builder.Append("RTRIM(LTRIM(");
                _visitor.Visit(m.Object != null ? m.Object : m.Arguments[0]);
                _builder.Append("))");
            }

            return m;
        }

        /// <summary>
        /// 访问 SubString 方法
        /// </summary>
        protected virtual Expression VisitSTRSubstring(MethodCallExpression m)
        {
            if (m != null)
            {
                List<Expression> args = new List<Expression>(m.Arguments);
                if (m.Object != null) args.Insert(0, m.Object);

                _builder.Append("SUBSTRING(");
                _visitor.Visit(args[0]);
                _builder.Append(",");

                if (args[1].CanEvaluate())
                {
                    ConstantExpression c = args[1].Evaluate();
                    int index = Convert.ToInt32(c.Value);
                    index += 1;
                    string value = _builder.GetSqlValue(index, DbType.Int32);
                    _builder.Append(value);
                    _builder.Append(',');
                }
                else
                {
                    _visitor.Visit(args[1]);
                    _builder.Append(" + 1,");
                }

                if (args.Count == 3) _visitor.Visit(args[2]);
                else
                {
                    _builder.Append("LEN(");
                    _visitor.Visit(args[0]);
                    _builder.Append(")");
                }
                _builder.Append(")");
            }

            return m;
        }

        /// <summary>
        /// 访问 Length 属性
        /// </summary>
        protected virtual Expression VisitSTRLength(MemberExpression m)
        {
            _builder.Append("LEN(");
            _visitor.Visit(m.Expression);
            _builder.Append(")");

            return m;
        }

        /// <summary>
        /// 访问 Contains 方法
        /// </summary>
        protected virtual Expression VisitENMContains(MethodCallExpression m)
        {
            if (m == null) return m;

            _visitedMember.ClearImmediately = false;
            _visitor.Visit(m.Arguments[m.Arguments.Count - 1]);
            _builder.Append(" IN(");

            Expression exp = m.Object != null ? m.Object : m.Arguments[0];
            if (exp.NodeType == ExpressionType.Constant)
            {
                _visitor.Visit(exp);
            }
            else if (exp.NodeType == ExpressionType.MemberAccess)
            {
                _visitor.Visit(exp.Evaluate());
            }
            else if (exp.NodeType == ExpressionType.NewArrayInit)
            {
                // => new[] { 1, 2, 3 }.Contains(a.DemoId)
                var expressions = (exp as NewArrayExpression).Expressions;
                for (int i = 0; i < expressions.Count; i++)
                {
                    _visitor.Visit(expressions[i]);
                    if (i < expressions.Count - 1) _builder.Append(",");
                    else if (i == expressions.Count - 1) _visitedMember.ClearImmediately = true;
                }
            }
            else if (exp.NodeType == ExpressionType.ListInit)
            {
                // => new List<int> { 1, 2, 3 }.Contains(a.DemoId)
                var initializers = (exp as ListInitExpression).Initializers;
                for (int i = 0; i < initializers.Count; i++)
                {
                    foreach (var args in initializers[i].Arguments) _visitor.Visit(args);

                    if (i < initializers.Count - 1) _builder.Append(",");
                    else if (i == initializers.Count - 1) _visitedMember.ClearImmediately = true;
                }
            }
            else if (exp.NodeType == ExpressionType.New)
            {
                // => new List<int>(_demoIdList).Contains(a.DemoId)
                var arguments = (exp as NewExpression).Arguments;
                for (int i = 0; i < arguments.Count; i++)
                {
                    if (i == arguments.Count - 1) _visitedMember.ClearImmediately = true;
                    _visitor.Visit(arguments[i]);
                }
            }

            _visitedMember.ClearImmediately = true;
            _builder.Append(")");

            return m;
        }

        /// <summary>
        /// 访问 RowNumber 方法
        /// </summary>
        protected virtual Expression VisitSQLRowNumber(MethodCallExpression m)
        {
            if (m == null) return m;

            _builder.Append("ROW_NUMBER() Over(Order By ");
            _visitor.Visit(m.Arguments[0]);
            if (m.Arguments.Count > 1)
            {
                var c = (ConstantExpression)m.Arguments[1];
                if (!((bool)c.Value)) _builder.Append(" DESC");
            }

            _builder.Append(')');

            return m;
        }        
        
        /// <summary>
        /// 访问 RowNumber 方法
        /// </summary>
        protected virtual Expression VisitSQLPartitionRowNumber(MethodCallExpression m)
        {
            if (m == null) return m;

            _builder.Append("ROW_NUMBER() Over(");

            // PARTITION BY
            _builder.Append("PARTITION BY ");
            _visitor.Visit(m.Arguments[0]);

            // ORDER BY
            _builder.Append(" ORDER BY ");
            _visitor.Visit(m.Arguments[1]);
            if (m.Arguments.Count > 2)
            {
                var c = (ConstantExpression)m.Arguments[2];
                if (!((bool)c.Value)) _builder.Append(" DESC");
            }

            _builder.Append(')');

            return m;
        }

        /// <summary>
        /// 访问 new Guid 方法
        /// </summary>
        protected virtual Expression VisitSQLNewGuid(MethodCallExpression m)
        {
            _builder.Append("NEWID()");
            return m;
        }

        #endregion

        #region 辅助函数

        /// <summary>
        /// 生成SQL片断
        /// </summary>
        protected string GetSqlValue(ConstantExpression c, ref bool unicode)
        {
            var member = _visitedMember.Current;
            var column = _builder.GetColumnAttribute(member != null ? member.Member : null, member != null ? member.Expression.Type : null);
            unicode = this.IsUnicode(column != null ? column.DbType : null);

            if (_builder.Parameterized)
            {
                unicode = false;
                string value = _builder.GetSqlValue(c.Value, member != null ? member.Member : null, member != null ? member.Expression.Type : null);
                return value;
            }
            else
            {
                string value = c.Value != null ? c.Value.ToString() : string.Empty;
                value = _builder.EscapeQuote(value, false, true, false);
                unicode = this.IsUnicode(column != null ? column.DbType : null);

                return value;
            }
        }

        /// <summary>
        /// 判断指定类型是否是unicode
        /// </summary>
        protected virtual bool IsUnicode(object dbType)
        {
            return SqlDbTypeInfo.IsUnicode2(dbType);
        }

        #endregion

        #region 运行时类

        class VistorTypeRuntimeInfo : TypeRuntimeInfo
        {
            internal VistorTypeRuntimeInfo(Type type)
                : base(type)
            {
            }

            // 获取当前类型的成员
            protected override IEnumerable<MemberInfo> GetMembers(Type type)
            {
                return TypeUtils.GetMembers(type, true);
            }
        }

        #endregion
    }
}
