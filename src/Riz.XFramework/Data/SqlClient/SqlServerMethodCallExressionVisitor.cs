
using System;
using System.Linq.Expressions;

namespace Riz.XFramework.Data.SqlClient
{
    /// <summary>
    /// <see cref="SqlServerMethodCallExressionVisitor"/> 表达式访问器
    /// </summary>
    internal class SqlServerMethodCallExressionVisitor : MethodCallExpressionVisitor
    {
        private ISqlBuilder _builder = null;
        private DbFuncletizer _funcletizer = null;
        private LinqExpressionVisitor _visitor = null;
        private MemberVisitedStack _visitedMark = null;
        private static TypeRuntimeInfo _typeRuntime = null;

        /// <summary>
        /// 运行时类成员
        /// </summary>
        protected override TypeRuntimeInfo TypeRuntime
        {
            get
            {
                if (_typeRuntime == null)
                    _typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo(this.GetType(), true);
                return _typeRuntime;
            }
        }

        /// <summary>
        /// 实例化 <see cref="SqlServerMethodCallExressionVisitor"/> 类的新实例
        /// </summary>
        /// <param name="visitor">表达式访问器</param>
        public SqlServerMethodCallExressionVisitor(LinqExpressionVisitor visitor)
            : base(visitor)
        {
            _visitor = visitor;
            _builder = visitor.SqlBuilder;
            _visitedMark = _visitor.VisitedStack;
            _funcletizer = _builder.TranslateContext.DbContext.Provider.Funcletizer;
        }

        /// <summary>
        /// 访问 StartWidth 方法
        /// </summary>
        /// <param name="m">方法表达式</param>
        protected override Expression VisitStartsWith(MethodCallExpression m)
        {
            _visitor.Visit(m.Object);
            if (this.NotOperands != null && this.NotOperands.Contains(m)) _builder.Append(" NOT");
            _builder.Append(" LIKE ");
            if (m.Arguments[0].CanEvaluate())
            {
                ColumnAttribute column = null;
                bool unicode = DbTypeUtils.IsUnicode(_visitedMark.Current, out column);
                string value = _funcletizer.GetSqlValue(m.Arguments[0].Evaluate().Value, _builder.TranslateContext, column);
                if (!_builder.Parameterized && value != null) value = value.TrimStart('N').Trim('\'');

                if (_builder.Parameterized)
                {
                    _builder.Append("(");
                    _builder.Append(value);
                    _builder.Append(" + '%')");
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

            return m;
        }

        /// <summary>
        /// 访问 EndWidth 方法
        /// </summary>
        /// <param name="m">方法表达式</param>
        protected override Expression VisitEndsWith(MethodCallExpression m)
        {
            _visitor.Visit(m.Object);
            if (this.NotOperands != null && this.NotOperands.Contains(m)) _builder.Append(" NOT");
            _builder.Append(" LIKE ");
            if (m.Arguments[0].CanEvaluate())
            {
                ColumnAttribute column = null;
                bool isUnicode = DbTypeUtils.IsUnicode(_visitedMark.Current, out column);
                string value = _funcletizer.GetSqlValue(m.Arguments[0].Evaluate().Value, _builder.TranslateContext, column);
                if (!_builder.Parameterized && value != null) value = value.TrimStart('N').Trim('\'');

                if (_builder.Parameterized)
                {
                    _builder.Append("('%' + ");
                    _builder.Append(value);
                    _builder.Append(')');
                }
                else
                {
                    if (isUnicode) _builder.Append('N');
                    _builder.Append("'%");
                    _builder.Append(value);
                    _builder.Append("'");
                }
            }
            else
            {
                _builder.Append("('%' + ");
                _visitor.Visit(m.Arguments[0]);
                _builder.Append(')');
            }

            return m;
        }

        /// <summary>
        /// 访问 string.Contains 方法
        /// </summary>
        /// <param name="m">方法表达式</param>
        protected override Expression VisitStringContains(MethodCallExpression m)
        {
            // https://www.cnblogs.com/yangmingyu/p/6928209.html
            // 对于其他的特殊字符：'^'， '-'， ']' 因为它们本身在包含在 '[]' 中使用，所以需要用另外的方式来转义，于是就引入了 like 中的 escape 子句，另外值得注意的是：escape 可以转义所有的特殊字符。
            // EF 的 Like 不用参数化...

            _visitor.Visit(m.Object);
            if (this.NotOperands != null && this.NotOperands.Contains(m)) _builder.Append(" NOT");
            _builder.Append(" LIKE ");
            if (m.Arguments[0].CanEvaluate())
            {
                ColumnAttribute column = null;
                bool isUnicode = DbTypeUtils.IsUnicode(_visitedMark.Current, out column);
                string value = _funcletizer.GetSqlValue(m.Arguments[0].Evaluate().Value, _builder.TranslateContext, column);
                if (!_builder.Parameterized && value != null) value = value.TrimStart('N').Trim('\'');

                if (_builder.Parameterized)
                {
                    _builder.Append("(");
                    _builder.Append("'%' + ");
                    _builder.Append(value);
                    _builder.Append(" + '%'");
                    _builder.Append(")");
                }
                else
                {
                    if (isUnicode) _builder.Append('N');
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
            return m;
        }

        /// <summary>
        /// 访问 IsNullOrEmpty 方法
        /// </summary>
        /// <param name="m">方法表达式</param>
        protected override Expression VisitIsNullOrEmpty(MethodCallExpression m)
        {
            _builder.Append("ISNULL(");
            _visitor.Visit(m.Arguments[0]);
            _builder.Append(",");
            bool isUnicode = DbTypeUtils.IsUnicode(_visitedMark.Current);
            string empty = isUnicode ? "N''" : "''";
            _builder.Append(empty);
            _builder.Append(") = ");
            _builder.Append(empty);
            return m;
        }

        /// <summary>
        /// 访问 ToString 方法
        /// </summary>
        protected override Expression VisitToStringImpl(Expression node)
        {
            // => a.ID.ToString()
            // 字符串不进行转换
            if (node == null || node.Type == typeof(string)) return _visitor.Visit(node);

            ColumnAttribute column = null;
            bool isUnicode = DbTypeUtils.IsUnicode(_visitedMark.Current, out column);
            string native = isUnicode ? "NVARCHAR" : "VARCHAR";

            // 其它类型转字符串
            bool isDate = node.Type == typeof(DateTime) ||
                node.Type == typeof(DateTime?) ||
                node.Type == typeof(TimeSpan) ||
                node.Type == typeof(TimeSpan?) ||
                node.Type == typeof(DateTimeOffset) ||
                node.Type == typeof(DateTimeOffset?);
            if (isDate)
            {
                native = string.Format("{0}(64)", native);
                _builder.Append("CONVERT(");
                _builder.Append(native);
                _builder.Append(",");
                _visitor.Visit(node);
                _builder.Append(",121)");
            }
            else if (node.Type == typeof(byte[]))
            {
                native = string.Format("{0}(max)", native);
                _builder.Append("CONVERT(");
                _builder.Append(native);
                _builder.Append(",");
                _visitor.Visit(node);
                _builder.Append(",1)");
            }
            else if (node.Type == typeof(Guid))
            {
                native = string.Format("{0}(64)", native);
                _builder.Append("CAST(");
                _visitor.Visit(node);
                _builder.Append(" AS ");
                _builder.Append(native);
                _builder.Append(')');
            }
            else
            {
                if (column != null && column.Size > 0) native = string.Format("{0}({1})", native, column.Size);
                else if (column != null && column.Size == -1) native = string.Format("{0}(max)", native);
                else native = string.Format("{0}(max)", native);

                _builder.Append("CAST(");
                _visitor.Visit(node);
                _builder.Append(" AS ");
                _builder.Append(native);
                _builder.Append(')');
            }

            return node;
        }

        /// <summary>
        /// 访问 PadLeft 方法
        /// </summary>
        protected override Expression VisitPadLeft(MethodCallExpression b)
        {
            if (b != null)
            {
                _builder.Append("(REPLICATE(");

                if (b.Arguments.Count == 1)
                    _builder.Append("N' '");
                else
                    _visitor.Visit(b.Arguments[1]);

                _builder.Append(',');
                _visitor.Visit(b.Arguments[0]);
                _builder.Append(" - LEN(");
                _visitor.Visit(b.Object);
                _builder.Append(")) + ");
                _visitor.Visit(b.Object);
                _builder.Append(")");
            }

            return b;
        }

        /// <summary>
        /// 访问 PadRight 方法
        /// </summary>
        protected override Expression VisitPadRight(MethodCallExpression b)
        {
            if (b != null)
            {
                _builder.Append("(");
                _visitor.Visit(b.Object);
                _builder.Append(" + REPLICATE(");

                if (b.Arguments.Count == 1)
                    _builder.Append("N' '");
                else
                    _visitor.Visit(b.Arguments[1]);

                _builder.Append(',');
                _visitor.Visit(b.Arguments[0]);
                _builder.Append(" - LEN(");
                _visitor.Visit(b.Object);
                _builder.Append(")))");
            }

            return b;
        }

        // CAST 如果转换时没有指定数据类型的长度，则SQServer自动提供长度为30
    }
}
