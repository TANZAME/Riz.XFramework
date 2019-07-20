
using System;
using System.Linq.Expressions;
using System.Collections.Generic;

namespace TZM.XFramework.Data.SqlClient
{
    /// <summary>
    /// <see cref="OracleMethodCallExressionVisitor"/> 表达式访问器
    /// </summary>
    public class OracleMethodCallExressionVisitor : MethodCallExressionVisitor
    {
        private ISqlBuilder _builder = null;
        private ExpressionVisitorBase _visitor = null;

        #region 构造函数

        /// <summary>
        /// 实例化 <see cref="OracleMethodCallExressionVisitor"/> 类的新实例
        /// </summary>
        public OracleMethodCallExressionVisitor(IDbQueryProvider provider, ExpressionVisitorBase visitor)
            : base(provider, visitor)
        {
            _visitor = visitor;
            _builder = visitor.SqlBuilder;

            // 替换掉解析器
            base.InternalVisitors.Replace(typeof(string), (provider2, visitor2) => new StringMethodCallExpressionVisitor(provider2, visitor2));
            base.InternalVisitors.Replace(typeof(SqlMethod), (provider2, visitor2) => new SqlMethodMethodCallExpressionVisitor(provider2, visitor2));
        }

        #endregion

        #region 重写方法

        /// <summary>
        /// 访问表示 null 合并运算的节点 a ?? b
        /// </summary>
        public override Expression VisitCoalesce(BinaryExpression b)
        {
            // 例： a.Name ?? "TAN" => ISNULL(a.Name,'TAN')
            Expression left = b.Left.NodeType == ExpressionType.Constant ? b.Right : b.Left;
            Expression right = b.Left.NodeType == ExpressionType.Constant ? b.Left : b.Right;

            _builder.Append("NVL(");
            _visitor.Visit(left);
            _builder.Append(',');
            _visitor.Visit(right);
            _builder.Append(')');


            return b;
        }

        /// <summary>
        /// 访问一元运算符
        /// </summary>
        /// <param name="node">一元运算符表达式</param>
        /// <returns></returns>
        public override Expression VisitUnary(UnaryExpression node)
        {
            // ORACLE的AVG函数，DataReader.GetValue()会抛异常
            if (node.NodeType == ExpressionType.Convert)
            {
                string name = "";
                if (node.Type == typeof(float)) name = "BINARY_FLOAT";
                else if (node.Type == typeof(double)) name = "BINARY_DOUBLE";
                if (!string.IsNullOrEmpty(name))
                {
                    _builder.Append("CAST(");
                    _visitor.Visit(node.Operand);
                    _builder.Append(" AS ");
                    _builder.Append(name);
                    _builder.Append(')');
                    return node;
                }
            }

            return base.VisitUnary(node);
        }

        #endregion

        #region 解析服务

        /// <summary>
        /// 字符串类型方法解析服务
        /// </summary>
        public class StringMethodCallExpressionVisitor : Internal.StringMethodCallExpressionVisitor
        {
            private ISqlBuilder _builder = null;
            private ExpressionVisitorBase _visitor = null;

            /// <summary>
            /// 实例化 <see cref="StringMethodCallExpressionVisitor"/> 类的新实例
            /// </summary>
            public StringMethodCallExpressionVisitor(IDbQueryProvider provider, ExpressionVisitorBase visitor)
                : base(provider, visitor)
            {
                _visitor = visitor;
                _builder = visitor.SqlBuilder;
            }

            /// <summary>
            /// 访问 ToString 方法
            /// </summary>
            protected override Expression VisitToString(MethodCallExpression m)
            {
                _builder.Append("TO_CHAR(");
                _visitor.Visit(m.Object != null ? m.Object : m.Arguments[0]);
                _builder.Append(")");

                return m;
            }

            /// <summary>
            /// 访问 Contains 方法
            /// </summary>
            protected override Expression VisitContains(MethodCallExpression m)
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
                            _builder.Append("'%' || ");
                            _builder.Append(value);
                            _builder.Append(" || '%'");
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
                        _builder.Append("('%' || ");
                        _visitor.Visit(m.Arguments[0]);
                        _builder.Append(" || '%')");
                    }
                }

                return m;
            }

            /// <summary>
            /// 访问 StartWidth 方法
            /// </summary>
            protected override Expression VisitStartsWith(MethodCallExpression m)
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
                            _builder.Append(" || '%'");
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
                        _builder.Append(" || '%')");
                    }
                }

                return m;
            }

            /// <summary>
            /// 访问 EndWidth 方法
            /// </summary>
            protected override Expression VisitEndsWith(MethodCallExpression m)
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
                            _builder.Append("'%' || ");
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
                        _builder.Append("('%' || ");
                        _visitor.Visit(m.Arguments[0]);
                        _builder.Append(")");
                    }
                }

                return m;
            }

            /// <summary>
            /// 访问 TrimStart 方法
            /// </summary>
            protected override Expression VisitSubstring(MethodCallExpression m)
            {
                if (m != null)
                {
                    List<Expression> args = new List<Expression>(m.Arguments);
                    if (m.Object != null) args.Insert(0, m.Object);

                    _builder.Append("SUBSTR(");
                    _visitor.Visit(args[0]);
                    _builder.Append(",");

                    if (args[1].CanEvaluate())
                    {
                        ConstantExpression c = args[1].Evaluate();
                        int index = Convert.ToInt32(c.Value);
                        index += 1;
                        string value = _builder.GetSqlValue(index);
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
                        _builder.Append("LENGTH(");
                        _visitor.Visit(args[0]);
                        _builder.Append(')');
                    }
                    _builder.Append(')');
                }

                return m;
            }

            /// <summary>
            /// 访问 TrimEnd 方法
            /// </summary>
            protected override Expression VisitLength(MemberExpression m)
            {
                _builder.Append("LENGTH(");
                _visitor.Visit(m.Expression);
                _builder.Append(")");

                return m;
            }

            /// <summary>
            /// 判断指定类型是否是unicode
            /// </summary>
            protected override bool IsUnicode(object dbType)
            {
                return dbType == null ? true : OracleDbTypeInfo.Create(dbType).IsUnicode;
            }
        }

        /// <summary>
        /// SqlMethod方法解析服务
        /// </summary>
        public class SqlMethodMethodCallExpressionVisitor : Internal.SqlMethodMethodCallExpressionVisitor
        {
            private ISqlBuilder _builder = null;

            /// <summary>
            /// 实例化 <see cref="SqlMethodVisitor"/> 类的新实例
            /// </summary>
            public SqlMethodMethodCallExpressionVisitor(IDbQueryProvider provider, ExpressionVisitorBase visitor)
                : base(provider, visitor)
            {
                _builder = visitor.SqlBuilder;
            }

            /// <summary>
            /// 访问 new Guid 方法
            /// </summary>
            protected override Expression VisitNewGuid(MethodCallExpression m)
            {
                _builder.Append("UUID()");
                return m;
            }
        }

        #endregion
    }
}
