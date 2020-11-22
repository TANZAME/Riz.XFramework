using System.Collections.Generic;
using System.Linq.Expressions;

namespace Riz.XFramework.Data
{
    /// <summary>
    /// WHERE 表达式解析器
    /// </summary>
    internal class WhereExpressionVisitor : DbExpressionVisitor
    {
        private ISqlBuilder _builder = null;

        /// <summary>
        /// 初始化 <see cref="WhereExpressionVisitor"/> 类的新实例
        /// </summary>
        /// <param name="aliasGenerator">表别名解析器</param>
        /// <param name="builder">SQL 语句生成器</param>
        public WhereExpressionVisitor(AliasGenerator aliasGenerator, ISqlBuilder builder)
            : base(aliasGenerator, builder)
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
                _builder.AppendNewLine();
                _builder.Append("WHERE ");

                for (int index = 0; index < wheres.Count; index++)
                {
                    DbExpression d = wheres[index];
                    if (d.Expressions == null || d.Expressions.Length == 0) continue;

                    var node = d.Expressions[0];
                    if (node.NodeType == ExpressionType.Lambda)
                        node = ((LambdaExpression)node).Body;
                    node = FixBinary(node);

                    base.Visit(node);
                }
            }

            return null;
        }

        ///// <summary>
        ///// 遍历表达式
        ///// </summary>
        //public override Expression Visit(Expression node)
        //{
        //    if (node == null) return null;

        //    // 如果初始表达式是 a=>a.Allowused && xxx 这种形式，则将 a.Allowused 解析成 a.Allowused == 1
        //    if (node == _expression)
        //    {
        //        node = FixBinary(node);
        //        _expression = node;
        //    }
        //    return base.Visit(node);
        //}

        ///// <summary>
        ///// 访问 Lambda 表达式
        ///// </summary>
        //protected override Expression VisitLambda<T>(Expression<T> node)
        //{
        //    LambdaExpression lambda = node as LambdaExpression;
        //    Expression expr = this.FixBinary(lambda.Body);
        //    if (expr != lambda.Body) node = Expression.Lambda<T>(expr, lambda.Parameters);

        //    return base.VisitLambda<T>(node);
        //}

        ///// <summary>
        ///// 写入SQL字符
        ///// </summary>
        //protected virtual void WriteImpl(ISqlBuilder builder)
        //{
        //    base.Write(builder);
        //}
    }
}