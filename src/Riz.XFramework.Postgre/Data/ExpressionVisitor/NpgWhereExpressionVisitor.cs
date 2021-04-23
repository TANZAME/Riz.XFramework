
using System.Linq.Expressions;
using System.Collections.Generic;

namespace Riz.XFramework.Data
{
    /// <summary>
    /// WHERE 表达式解析器
    /// </summary>
    internal class NpgWhereExpressionVisitor : WhereExpressionVisitor
    {
        private ISqlBuilder _builder = null;

        /// <summary>
        /// 初始化 <see cref="NpgWhereExpressionVisitor"/> 类的新实例
        /// </summary>
        /// <param name="ag">表别名解析器</param>
        /// <param name="builder">SQL 语句生成器</param>
        public NpgWhereExpressionVisitor(AliasGenerator ag, ISqlBuilder builder)
            : base(ag, builder)
        {
            _builder = builder;
        }

        /// <summary>
        /// 访问表达式节点
        /// </summary>
        /// <param name="wheres">筛选表达式</param>
        public override Expression Visit(List<DbExpression> wheres)
        {
            if (wheres != null && wheres.Count > 0)
            {
                if (_builder.Length > 0) _builder.Append(" AND ");

                for (int index = 0; index < wheres.Count; index++)
                {
                    DbExpression d = wheres[index];
                    var node = d.Expressions[0];
                    if (node.NodeType == ExpressionType.Lambda)
                        node = ((LambdaExpression)node).Body;
                    node = BooleanUnaryToBinary(node);

                    base.Visit(node);
                    if (index < wheres.Count - 1) _builder.Append(" AND ");
                }
            }

            return null;
        }
    }
}
