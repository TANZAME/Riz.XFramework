using System.Linq.Expressions;

namespace Riz.XFramework.Data
{
    /// <summary>
    /// 导航属性过滤谓词表达式解析器
    /// </summary>
    internal class NavPredicateExpressionVisitor : DbExpressionVisitor
    {
        private string _alias = null;
        private ISqlBuilder _builder = null;

        /// <summary>
        /// 初始化 <see cref="NavPredicateExpressionVisitor"/> 类的新实例
        /// </summary>
        /// <param name="aliasGenerator">表别名解析器</param>
        /// <param name="builder">SQL 语句生成器</param>
        /// <param name="alias">指定的表别名</param>
        public NavPredicateExpressionVisitor(AliasGenerator aliasGenerator, ISqlBuilder builder, string alias)
            : base(aliasGenerator, builder)
        {
            _alias = alias;
            _builder = builder;
        }


        /// <summary>
        /// 访问表达式节点
        /// </summary>
        /// <param name="node">表达式节点</param>
        public override Expression Visit(Expression node)
        {
            if (node != null)
            {
                _builder.Append(" AND ");
                base.Visit(node);
            }
            return node;
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
            _builder.AppendMember(_alias, node.Member, node.Expression.Type);
            return node;
        }
    }
}