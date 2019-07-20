using System;
using System.Linq.Expressions;
using System.Collections.Generic;

namespace TZM.XFramework.Data.Internal
{
    /// <summary>
    /// 字符串类型方法解析服务
    /// </summary>
    public class StringMethodCallExpressionVisitor
    {
        private ISqlBuilder _builder = null;
        private IDbQueryProvider _provider = null;
        private MemberVisitedMark _visitedMark = null;
        private ExpressionVisitorBase _visitor = null;

        /// <summary>
        /// 实例化 <see cref="StringMethodCallExpressionVisitor"/> 类的新实例
        /// </summary>
        public StringMethodCallExpressionVisitor(IDbQueryProvider provider, ExpressionVisitorBase visitor)
        {
            _provider = provider;
            _visitor = visitor;
            _builder = visitor.SqlBuilder;
            _visitedMark = _visitor.VisitedMark;
        }

        /// <summary>
        /// 访问 ToString 方法
        /// </summary>
        protected virtual Expression VisitToString(MethodCallExpression m)
        {
            // => a.ID.ToString()
            Expression node = null;
            if (m.Object != null) node = m.Object;
            else if (m.Arguments != null && m.Arguments.Count > 0) node = m.Arguments[0];
            // 字符串不进行转换
            if (node == null || node.Type == typeof(string)) return _visitor.Visit(node);

            string name = "NVARCHAR";
            var member = _visitedMark.Current;
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
        protected virtual Expression VisitContains(MethodCallExpression m)
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
        protected virtual Expression VisitStartsWith(MethodCallExpression m)
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
        protected virtual Expression VisitEndsWith(MethodCallExpression m)
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
        protected virtual Expression VisitTrimStart(MethodCallExpression m)
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
        protected virtual Expression VisitTrimEnd(MethodCallExpression m)
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
        protected virtual Expression VisitTrim(MethodCallExpression m)
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
        protected virtual Expression VisitSubstring(MethodCallExpression m)
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
                    string value = _builder.GetSqlValue(index, System.Data.DbType.Int32);
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
        protected virtual Expression VisitLength(MemberExpression m)
        {
            _builder.Append("LEN(");
            _visitor.Visit(m.Expression);
            _builder.Append(")");

            return m;
        }

        /// <summary>
        /// 判断指定类型是否是unicode
        /// </summary>
        protected virtual bool IsUnicode(object dbType)
        {
            throw new NotImplementedException("IsUnicode not implemented.");
            // return SqlDbTypeInfo.IsUnicode2(dbType);
        }

        /// <summary>
        /// 生成SQL片断
        /// </summary>
        protected string GetSqlValue(ConstantExpression c, ref bool unicode)
        {
            var member = _visitedMark.Current;
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
    }
}
