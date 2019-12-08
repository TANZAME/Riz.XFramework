using System;
using System.Linq;
using System.Data;
using System.Reflection;
using System.Linq.Expressions;
using System.Collections.Generic;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// 选择列表达式解析器
    /// </summary>
    public class OracleColumnExpressionVisitor : ColumnExpressionVisitor
    {
        IDbQueryableInfo_Select _dbQuery = null;

        /// <summary>
        /// 初始化 <see cref="OracleColumnExpressionVisitor"/> 类的新实例
        /// </summary>
        public OracleColumnExpressionVisitor(IDbQueryProvider provider, TableAliasCache aliases, IDbQueryableInfo_Select dbQuery)
            : base(provider, aliases, dbQuery)
        {
            _dbQuery = dbQuery;
        }

        /// <summary>
        /// 将表达式所表示的SQL片断写入SQL构造器
        /// </summary>
        public override void Write(ISqlBuilder builder)
        {
            base.Write(builder);
            if (_dbQuery is IWidthRowId)
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