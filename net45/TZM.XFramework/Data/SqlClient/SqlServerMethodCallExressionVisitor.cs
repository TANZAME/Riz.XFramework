

using System.Linq.Expressions;

namespace TZM.XFramework.Data.SqlClient
{
    /// <summary>
    /// <see cref="SqlServerMethodCallExressionVisitor"/> 表达式访问器
    /// </summary>
    public class SqlServerMethodCallExressionVisitor : MethodCallExpressionVisitor
    {
        // CAST 如果转换时没有指定数据类型的长度，则SQServer自动提供长度为30
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

        /// <summary>
        /// 访问 DateTime.AddDays 方法
        /// </summary>
        protected override Expression VisitAddHours(MethodCallExpression m)
        {
            ColumnAttribute column = _provider.DbValue.GetColumnAttribute(_visitedMark.Current);
            bool isDate = DbTypeUtils.IsDate(column.DbType);

            _builder.Append("DATEADD(HOUR,");
            _visitor.Visit(m.Arguments[0]);
            _builder.Append(",");

            if (!isDate) _visitor.Visit(m.Object);
            else
            {
                _builder.Append("CAST(");
                _visitor.Visit(m.Object);
                _builder.Append(" AS DATETIME)");
            }

            _builder.Append(')');
            return m;
        }

        /// <summary>
        /// 访问 DateTime.AddMinutes 方法
        /// </summary>
        protected override Expression VisitAddMinutes(MethodCallExpression m)
        {
            ColumnAttribute column = _provider.DbValue.GetColumnAttribute(_visitedMark.Current);
            bool isDate = DbTypeUtils.IsDate(column.DbType);

            _builder.Append("DATEADD(MINUTE,");
            _visitor.Visit(m.Arguments[0]);
            _builder.Append(",");

            if (!isDate) _visitor.Visit(m.Object);
            else
            {
                _builder.Append("CAST(");
                _visitor.Visit(m.Object);
                _builder.Append(" AS DATETIME)");
            }

            _builder.Append(')');
            return m;
        }

        /// <summary>
        /// 访问 DateTime.AddSeconds 方法
        /// </summary>
        protected override Expression VisitAddSeconds(MethodCallExpression m)
        {
            ColumnAttribute column = _provider.DbValue.GetColumnAttribute(_visitedMark.Current);
            bool isDate = DbTypeUtils.IsDate(column.DbType)|| DbTypeUtils.IsDateTime(column.DbType);

            _builder.Append("DATEADD(SECOND,");
            _visitor.Visit(m.Arguments[0]);
            _builder.Append(",");

            if (!isDate) _visitor.Visit(m.Object);
            else
            {
                _builder.Append("CAST(");
                _visitor.Visit(m.Object);
                _builder.Append(" AS DATETIME2)");
            }

            _builder.Append(')');
            return m;
        }

        /// <summary>
        /// 访问 DateTime.AddMilliseconds 方法
        /// </summary>
        protected override Expression VisitAddMilliseconds(MethodCallExpression m)
        {
            ColumnAttribute column = _provider.DbValue.GetColumnAttribute(_visitedMark.Current);
            bool isDate = DbTypeUtils.IsDate(column.DbType) || DbTypeUtils.IsDateTime(column.DbType);

            _builder.Append("DATEADD(MILLISECOND,");
            _visitor.Visit(m.Arguments[0]);
            _builder.Append(",");

            if (!isDate) _visitor.Visit(m.Object);
            else
            {
                _builder.Append("CAST(");
                _visitor.Visit(m.Object);
                _builder.Append(" AS DATETIME2)");
            }

            _builder.Append(')');
            return m;
        }

        /// <summary>
        /// 访问 DateTime.AddTicks 方法
        /// </summary>
        protected override Expression VisitAddTicks(MethodCallExpression m)
        {
            ColumnAttribute column = _provider.DbValue.GetColumnAttribute(_visitedMark.Current);
            bool isDate = DbTypeUtils.IsDate(column.DbType) || DbTypeUtils.IsDateTime(column.DbType);

            // 1tick = 100纳秒
            _builder.Append("DATEADD(NANOSECOND,");
            if (m.Arguments[0].CanEvaluate()) _builder.Append(m.Arguments[0].Evaluate().Value, null);
            else
            {
                if (m.Arguments[0].NodeType != ExpressionType.MemberAccess) _builder.Append("(");
                _visitor.Visit(m.Arguments[0]);
                if (m.Arguments[0].NodeType != ExpressionType.MemberAccess) _builder.Append(")");
            }
            _builder.Append(" * 100,");

            if (!isDate) _visitor.Visit(m.Object);
            else
            {
                _builder.Append("CAST(");
                _visitor.Visit(m.Object);
                _builder.Append(" AS DATETIME2)");
            }

            _builder.Append(')');
            return m;
        }
    }
}
