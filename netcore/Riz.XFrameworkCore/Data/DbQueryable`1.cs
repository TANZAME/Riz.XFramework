
using System.Data;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Riz.XFramework.Data
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
            : base(context)
        {
            this._dbExpressions = new ReadOnlyCollection<DbExpression>(collection != null ? collection : new List<DbExpression>(0));
        }

        /// <summary>
        /// 创建查询
        /// </summary>
        public IDbQueryable<TSource> CreateQuery<TSource>(DbExpressionType dbExpressionType, System.Linq.Expressions.Expression expression = null)
        {
            return this.CreateQuery<TSource>(new DbExpression(dbExpressionType, expression));
        }

        /// <summary>
        /// 创建查询
        /// </summary>
        public IDbQueryable<TSource> CreateQuery<TSource>(DbExpression dbExpression = null)
        {
            List<DbExpression> collection = new List<DbExpression>(this.DbExpressions.Count + (dbExpression != null ? 1 : 0));
            collection.AddRange(this.DbExpressions);
            if (dbExpression != null) collection.Add(dbExpression);

            IDbQueryable<TSource> query = new DbQueryable<TSource>(this.DbContext, collection);
            return query;
        }

        /// <summary>
        /// 解析成 SQL 命令
        /// </summary>
        public override DbRawCommand Translate()
        {
            return this.Translate(0, true, null);
        }

        /// <summary>
        /// 解析成 SQL 命令
        /// </summary>
        /// <param name="indent">缩进</param>
        /// <param name="isOutQuery">是否最外层，内层查询不需要结束符(;)</param>
        /// <param name="context">解析SQL命令上下文</param>
        /// <returns></returns>
        public override DbRawCommand Translate(int indent, bool isOutQuery, ITranslateContext context)
        {
            var cmd = this.Provider.Translate(this, indent, isOutQuery, context);
            return cmd;
        }

        /// <summary>
        /// 执行查询
        /// </summary>
        /// <returns></returns>
        public override TResult Execute<TResult>()
        {
            return this.DbContext.Database.Execute<TResult>(this);
        }

#if !net40

        /// <summary>
        /// 执行查询
        /// </summary>
        /// <returns></returns>
        public override async Task<TResult> ExecuteAsync<TResult>()
        {
            return await this.DbContext.Database.ExecuteAsync<TResult>(this);
        }

#endif

        /// <summary>
        /// 解析查询语义
        /// </summary>
        public override DbQueryTree Parse()
        {
            return DbQueryableParser.Parse(this);
        }

        /// <summary>
        /// 字符串表示形式
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            var newQuery = this.CreateQuery<TElement>();
            newQuery.Parameterized = false;
            var cmd = newQuery.Translate();
            return cmd.CommandText;
        }
    }
}
