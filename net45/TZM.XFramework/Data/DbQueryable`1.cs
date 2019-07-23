
using System.Data;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// 数据查询表达对象
    /// </summary>
    public class DbQueryable<TElement> : DbQueryable, IDbQueryable<TElement>
    {
        private ReadOnlyCollection<DbExpression> _dbExpressions = null;

        /// <summary>
        /// 查询表达式
        /// </summary>
        public override ReadOnlyCollection<DbExpression> DbExpressions
        {
            get { return _dbExpressions; }
        }

        /// <summary>
        /// 实例化类<see cref="DbQueryable"/>的新实例
        /// </summary>
        /// <param name="context"></param>
        /// <param name="collection"></param>
        public DbQueryable(IDbContext context, IList<DbExpression> collection)
        {
            this.DbContext = context;
            this._dbExpressions = new ReadOnlyCollection<DbExpression>(collection != null ? collection : new List<DbExpression>(0));
        }

        /// <summary>
        /// 创建查询
        /// </summary>
        public IDbQueryable<TResult> CreateQuery<TResult>(DbExpressionType dbExpressionType, System.Linq.Expressions.Expression expression = null)
        {
            return this.CreateQuery<TResult>(new DbExpression(dbExpressionType, expression));
        }

        /// <summary>
        /// 创建查询
        /// </summary>
        public IDbQueryable<TResult> CreateQuery<TResult>(DbExpression dbExpression = null)
        {
            List<DbExpression> collection = new List<DbExpression>(this.DbExpressions.Count + (dbExpression != null ? 1 : 0));
            collection.AddRange(this.DbExpressions);
            if (dbExpression != null) collection.Add(dbExpression);

            IDbQueryable<TResult> query = new DbQueryable<TResult>(this.DbContext, collection);
            return query;
        }

        /// <summary>
        /// 解析成 SQL 命令
        /// </summary>
        public override Command Resolve()
        {
            var cmd = this.Provider.Resolve(this, 0, true, null);
            return cmd;
        }

        /// <summary>
        /// 解析成 SQL 命令
        /// </summary>
        /// <param name="indent">缩进</param>
        /// <param name="isOuter">是否最外层，内层查询不需要结束符(;)</param>
        /// <param name="token">解析上下文参数</param>
        /// <returns></returns>
        public override Command Resolve(int indent, bool isOuter, ParserToken token)
        {
            var cmd = this.Provider.Resolve(this, indent, isOuter, token);
            return cmd;
        }

        /// <summary>
        /// 解析查询语义
        /// </summary>
        public override IDbQueryableInfo Parse()
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
            var cmd = newQuery.Resolve();
            return cmd.CommandText;
        }
    }
}
