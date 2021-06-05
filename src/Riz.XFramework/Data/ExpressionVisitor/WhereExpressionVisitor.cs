
using System.Linq;
using System.Linq.Expressions;
using System.Collections.Generic;

namespace Riz.XFramework.Data
{
    /// <summary>
    /// WHERE 表达式解析器
    /// </summary>
    internal class WhereExpressionVisitor : DbExpressionVisitor
    {
        private ISqlBuilder _builder = null;
        private IDbQueryProvider _provider = null;
        private DbConstor _constor = null;
        private ITranslateContext _context = null;

        /// <summary>
        /// 初始化 <see cref="WhereExpressionVisitor"/> 类的新实例
        /// </summary>
        /// <param name="ag">表别名解析器</param>
        /// <param name="builder">SQL 语句生成器</param>
        public WhereExpressionVisitor(AliasGenerator ag, ISqlBuilder builder)
            : base(ag, builder)
        {
            _builder = builder;
            _provider = _builder.Provider;
            _constor = ((DbQueryProvider)_provider).Constor;
            _context = _builder.TranslateContext;
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
                    var node = d.Expressions[0];
                    if (node.NodeType == ExpressionType.Lambda)
                        node = ((LambdaExpression)node).Body;
                    node = BooleanUnaryToBinary(node);

                    if (!(node is ConstantExpression)) base.Visit(node);
                    else
                    {
                        // 字符符动态SQL，解析参数
                        DbRawSql rawSql = (node as ConstantExpression).Value as DbRawSql;
                        object[] args = null;
                        if (rawSql.Parameters != null)
                            args = rawSql.Parameters.Select(x => _constor.GetSqlValue(x, _context)).ToArray();
                        string sql = rawSql.CommandText;

                        if (args != null && args.Length > 0) sql = string.Format(sql, args);
                        _builder.Append(sql);
                    }
                   
                    if (index < wheres.Count - 1) _builder.Append(" AND ");
                }
            }

            return null;
        }
    }
}