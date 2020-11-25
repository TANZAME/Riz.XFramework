
using System;
using System.Linq.Expressions;

namespace Riz.XFramework.Data
{
    /// <summary>
    /// 选择列表达式解析器
    /// </summary>
    internal class SQLiteColumnExpressionVisitor : ColumnExpressionVisitor
    {
        private ISqlBuilder _builder = null;
        private DbQuerySelectTree _tree = null;

        /// <summary>
        /// 初始化 <see cref="SQLiteColumnExpressionVisitor"/> 类的新实例
        /// </summary>
        /// <param name="ag">表别名解析器</param>
        /// <param name="builder">SQL 语句生成器</param>
        /// <param name="tree">查询语义</param>
        public SQLiteColumnExpressionVisitor(AliasGenerator ag, ISqlBuilder builder, DbQuerySelectTree tree)
            : base(ag, builder, tree)
        {
            _tree = tree;
            _builder = builder;
        }

        /// <summary>
        /// 访问表达式节点
        /// </summary>
        /// <param name="select">选择表达式</param>
        public override Expression Visit(DbExpression select)
        {
            base.Visit(select);
            if (_tree is IWithRowId)
            {
                if (_builder.Length == 0) _builder.Append(',');
                _builder.AppendNewLine();
                _builder.Append("t0.RowId");
            }

            return select != null && select.HasExpression ? select.Expressions[0] : null;
        }

        /// <summary>
        /// 选择所有的字段
        /// </summary>
        /// <param name="type">实体类型</param>
        /// <param name="alias">表别名</param>
        /// <param name="node">节点</param>
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