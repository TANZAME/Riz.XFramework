
using System;
using System.Linq.Expressions;

namespace TZM.XFramework.Data.SqlClient
{
    /// <summary>
    /// <see cref="SqlServerMethodCallExressionVisitor"/> 表达式访问器
    /// </summary>
    public class SqlServerMethodCallExressionVisitor : MethodCallExpressionVisitor
    {
        private ISqlBuilder _builder = null;
        private IDbQueryProvider _provider = null;
        private ExpressionVisitorBase _visitor = null;
        private MemberVisitedMark _visitedMark = null;

        /// <summary>
        /// 实例化 <see cref="SqlServerMethodCallExressionVisitor"/> 类的新实例
        /// </summary>
        public SqlServerMethodCallExressionVisitor(IDbQueryProvider provider, ExpressionVisitorBase visitor)
            : base(provider, visitor)
        {
            _provider = provider;
            _visitor = visitor;
            _builder = visitor.SqlBuilder;
            _visitedMark = _visitor.VisitedMark;
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
            bool isUnicode = _provider.DbValue.IsUnicode(_visitedMark.Current, out column);
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
