
using System;
using System.Linq.Expressions;


namespace TZM.XFramework.Data
{
    /// <summary>
    /// 选择列表达式解析器
    /// </summary>
    internal class OracleColumnExpressionVisitor : ColumnExpressionVisitor
    {
        IDbQueryableInfo_Select _dbQuery = null;

        /// <summary>
        /// 初始化 <see cref="OracleColumnExpressionVisitor"/> 类的新实例
        /// </summary>
        /// <param name="provider">查询语义提供者</param>
        /// <param name="aliases">表别名集合</param>
        /// <param name="dbQuery">查询语义</param>
        public OracleColumnExpressionVisitor(IDbQueryProvider provider, TableAlias aliases, IDbQueryableInfo_Select dbQuery)
            : base(provider, aliases, dbQuery)
        {
            _dbQuery = dbQuery;
        }

        /// <summary>
        /// 将表达式所表示的SQL片断写入SQL构造器
        /// </summary>
        /// <param name="builder">SQL 语句生成器</param>
        public override void Write(ISqlBuilder builder)
        {
            base.Write(builder);
            if (_dbQuery is IWithRowId)
            {
                if (_builder.Length == 0) _builder.Append(',');
                _builder.AppendNewLine();
                _builder.Append("t0.RowId");
            }
        }

        /// <summary>
        /// 选择所有的字段
        /// </summary>
        /// <param name="type">实体类型</param>
        /// <param name="alias">表别名</param>
        /// <param name="node">即将访问的节点</param>
        /// <returns></returns>
        protected override Expression VisitAllMember(Type type, string alias, Expression node = null)
        {
            if (type != null) return base.VisitAllMember(type, alias, node);
            else
            {
                string value = (string)(node as ConstantExpression).Value;
                _builder.Append(value);
                return node;
            }
        }
    }
}