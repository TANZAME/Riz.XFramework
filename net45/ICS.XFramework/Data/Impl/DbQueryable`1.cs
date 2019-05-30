
using System;
using System.Data;
using System.Text;
using System.Linq.Expressions;
using System.Collections.Generic;

namespace ICS.XFramework.Data
{
    /// <summary>
    /// 数据查询
    /// </summary>
    public class DbQueryable<TElement> : DbQueryable, IDbQueryable<TElement>
    {
        private IList<DbExpression> _dbExpressions = null;

        /// <summary>
        /// 查询表达式
        /// </summary>
        public override IList<DbExpression> DbExpressions
        {
            get { return _dbExpressions; }
            set { _dbExpressions = value; }
        }

        /// <summary>
        /// 创建查询
        /// </summary>
        public IDbQueryable<TResult> CreateQuery<TResult>(DbExpressionType dbExpressionType, System.Linq.Expressions.Expression expression = null)
        {
            return this.CreateQuery<TResult>(new DbExpression
            {
                DbExpressionType = dbExpressionType,
                Expressions = expression != null ? new[] { expression } : null
            });
        }

        /// <summary>
        /// 创建查询
        /// </summary>
        public IDbQueryable<TResult> CreateQuery<TResult>(DbExpression exp = null)
        {
            IDbQueryable<TResult> query = new DbQueryable<TResult>
            {
                DbContext = _context,
                DbExpressions = new List<DbExpression>(_dbExpressions)
            };

            if (exp != null) query.DbExpressions.Add(exp);
            return query;
        }

        /// <summary>
        /// 解析成 SQL 命令
        /// </summary>
        /// <param name="indent">缩进</param>
        /// <param name="isOuter">是否最外层，内层查询不需要结束符(;)</param>
        /// <param name="parameters">已存在的参数列表</param>
        /// <returns></returns>
        public override DbCommandDefinition Resolve(int indent = 0, bool isOuter = true, List<IDbDataParameter> parameters = null)
        {
            var cmd = this.Provider.Resolve(this, indent, isOuter, parameters);
            return cmd;
        }

        /// <summary>
        /// 解析查询语义
        /// </summary>
        public override IDbQueryableInfo Parse(int startIndex = 0)
        {
            return DbQueryParser.Parse(this);
        }

        /// <summary>
        /// 字符串表示形式
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            var newQuery = this.CreateQuery<TElement>();
            newQuery.Parameterized = false;
            var cmd = newQuery.Resolve(0, true, null);
            return cmd.CommandText;
        }
    }
}
