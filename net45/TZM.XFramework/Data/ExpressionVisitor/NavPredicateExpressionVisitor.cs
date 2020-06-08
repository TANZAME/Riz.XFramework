using System.Linq.Expressions;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// 导航属性过滤谓词表达式解析器
    /// </summary>
    public class NavPredicateExpressionVisitor : ExpressionVisitorBase
    {
        private Expression _expression = null;
        private string _alias = null;

        /// <summary>
        /// 初始化 <see cref="NavPredicateExpressionVisitor"/> 类的新实例
        /// </summary>
        /// <param name="provider">查询语义提供者</param>
        /// <param name="aliases">表别名集合</param>
        /// <param name="predicate">要访问的表达式</param>
        /// <param name="alias">指定的表别名</param>
        public NavPredicateExpressionVisitor(IDbQueryProvider provider, TableAlias aliases, Expression predicate, string alias)
            : base(provider, aliases, predicate)
        {
            _expression = base.Expression;
            _alias = alias;
        }

        /// <summary>
        /// 将表达式所表示的SQL片断写入SQL构造器
        /// </summary>
        /// <param name="builder">SQL 语句生成器</param>
        public override void Write(ISqlBuilder builder)
        {
            if (base.Expression != null)
            {
                base._builder = builder;
                _builder.Append(" AND ");
            }

            base.Write(builder);
        }

        /// <summary>
        /// 遍历表达式
        /// </summary>
        public override Expression Visit(Expression node)
        {
            if (node == null) return null;

            // 如果初始表达式是 a=>a.Allowused && xxx 这种形式，则将 a.Allowused 解析成 a.Allowused == 1
            if (node == _expression)
            {
                node = TryMakeBinary(node);
                _expression = node;
            }
            return base.Visit(node);
        }

        /// <summary>
        /// 访问 Lambda 表达式
        /// </summary>
        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            LambdaExpression lambda = node as LambdaExpression;
            Expression expr = this.TryMakeBinary(lambda.Body);
            if (expr != lambda.Body) node = Expression.Lambda<T>(expr, lambda.Parameters);

            return base.VisitLambda<T>(node);
        }

        /// <summary>
        /// 访问字段或者属性表达式
        /// </summary>
        /// <param name="node">字段或者成员表达式</param>
        /// <returns></returns>
        protected override Expression VisitMember(MemberExpression node)
        {
            if (node == null) return node;

            // 嵌套
            _builder.AppendMember(_alias, node.Member.Name);
            return node;
        }
    }
}