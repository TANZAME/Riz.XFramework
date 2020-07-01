using System.Linq.Expressions;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// HAVING 表达式解析器
    /// </summary>
    public class HavingExpressionVisitor : ExpressionVisitorBase
    {
        private DbExpression _groupBy = null;
        private TableAlias _aliases = null;

        /// <summary>
        /// 初始化 <see cref="HavingExpressionVisitor"/> 类的新实例
        /// </summary>
        /// <param name="provider">查询语义提供者</param>
        /// <param name="aliases">表别名集合</param>
        /// <param name="having">HAVING 子句</param>
        /// <param name="groupBy">GROUP BY 子句</param>
        public HavingExpressionVisitor(IDbQueryProvider provider, TableAlias aliases, DbExpression having, DbExpression groupBy)
            : base(provider, aliases, having != null && having.Expressions != null ? having.Expressions[0] : null)
        {
            _groupBy = groupBy;
            _aliases = aliases;
        }

        /// <summary>
        /// 将表达式所表示的SQL片断写入 SQL 生成器
        /// </summary>
        /// <param name="builder">SQL 语句生成器</param>
        public override void Write(ISqlBuilder builder)
        {
            if (base.Expression != null)
            {
                builder.AppendNewLine();
                builder.Append("Having ");
            }

            base.Write(builder);
        }

        /// <summary>
        /// 遍历表达式
        /// </summary>
        /// <param name="node">要访问的表达式</param>
        /// <returns></returns>
        public override Expression Visit(Expression node)
        {
            if (node == null) return null;

            // 如果初始表达式是 a=>a.Allowused && xxx 这种形式，则将 a.Allowused 解析成 a.Allowused == 1
            if (node == base.Expression) node = TryMakeBinary(node);
            return base.Visit(node);
        }

        /// <summary>
        /// 访问 Lambda 表达式
        /// </summary>
        /// <typeparam name="T">Lambda 表达式返回的类型</typeparam>
        /// <param name="node">Lambda 表达式</param>
        /// <returns></returns>
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

            // Group By 解析
            if (_groupBy != null && node.IsGrouping())
            {
                string memberName = node.Member.Name;

                // CompanyName = g.Key.Name
                LambdaExpression keySelector = _groupBy.Expressions[0] as LambdaExpression;
                Expression exp = null;
                Expression body = keySelector.Body;


                if (body.NodeType == ExpressionType.MemberAccess)
                {
                    // a.Name
                    exp = body;
                }
                else if (body.NodeType == ExpressionType.New)
                {
                    // new { Name = a.Name  }
                    NewExpression newExp = body as NewExpression;
                    int index = newExp.Members.IndexOf(x => x.Name == memberName);
                    exp = newExp.Arguments[index];
                }

                return this.Visit(exp);
            }

            return base.VisitMember(node);
        }
    }
}
