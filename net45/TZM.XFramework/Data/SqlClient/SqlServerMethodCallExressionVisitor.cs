

using System.Linq.Expressions;

namespace TZM.XFramework.Data.SqlClient
{
    /// <summary>
    /// <see cref="SqlServerMethodCallExressionVisitor"/> 表达式访问器
    /// </summary>
    public class SqlServerMethodCallExressionVisitor : MethodCallExressionVisitor
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

            _visitedMark.Clear();
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

            _visitedMark.Clear();
            return b;
        }

        // CAST 如果转换时没有指定数据类型的长度，则SQServer自动提供长度为30
    }
}
