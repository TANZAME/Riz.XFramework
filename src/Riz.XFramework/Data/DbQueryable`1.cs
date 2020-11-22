
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.Collections.ObjectModel;

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
        public override ReadOnlyCollection<DbExpression> DbExpressions => _dbExpressions;

        /// <summary>
        /// 字符串表示形式。无参数，主要用于调式
        /// </summary>
        public override string Sql
        {
            get
            {
                var newQuery = (DbQueryable)this.CreateQuery<TElement>(null);
                newQuery.Parameterized = false;
                var cmd = newQuery.Translate();
                return cmd.CommandText;
            }
        }

        /// <summary>
        /// 实例化类<see cref="DbQueryable"/>的新实例
        /// </summary>
        /// <param name="context">数据查询上下文</param>
        /// <param name="collection">查询表达式集合</param>
        public DbQueryable(IDbContext context, IList<DbExpression> collection)
            : base(context)
        {
            XFrameworkException.Check.NotNull(collection, nameof(collection));
            _dbExpressions = new ReadOnlyCollection<DbExpression>(collection);
        }

        /// <summary>
        /// 创建查询表达式
        /// </summary>
        /// <typeparam name="TResult">返回的元素类型</typeparam>
        /// <param name="dbExpressionType">查询类型</param>
        /// <param name="expression">查询表达式</param>
        /// <returns></returns>
        public IDbQueryable<TResult> CreateQuery<TResult>(DbExpressionType dbExpressionType, Expression expression)
            => this.CreateQuery<TResult>(new DbExpression(dbExpressionType, expression));

        /// <summary>
        /// 构造一个 <see cref="IDbQueryable"/> 对象，该对象可计算指定表达式树所表示的查询
        /// </summary>
        /// <typeparam name="TResult">返回的元素类型</typeparam>
        /// <param name="dbExpression">查询表达式</param>
        /// <returns></returns>
        public IDbQueryable<TResult> CreateQuery<TResult>(DbExpression dbExpression)
        {
            var collection = new List<DbExpression>(this.DbExpressions.Count + (dbExpression != null ? 1 : 0));
            collection.AddRange(_dbExpressions);
            if (dbExpression != null) collection.Add(dbExpression);

            IDbQueryable<TResult> query = new DbQueryable<TResult>(this.DbContext, collection);
            return query;
        }

        /// <summary>
        /// 解析成 SQL 命令
        /// </summary>
        public override DbRawCommand Translate() => this.Translate(0, true, null);

        /// <summary>
        /// 解析成 SQL 命令
        /// </summary>
        /// <param name="indent">缩进</param>
        /// <param name="isOutQuery">是否最外层，内层查询不需要结束符(;)</param>
        /// <param name="context">解析SQL命令上下文</param>
        /// <returns></returns>
        internal override DbRawCommand Translate(int indent, bool isOutQuery, ITranslateContext context) => ((DbQueryProvider)this.Provider).Translate(this, indent, isOutQuery, context);

        /// <summary>
        /// 执行查询
        /// </summary>
        /// <returns></returns>
        internal override TResult Execute<TResult>() => this.DbContext.Database.Execute<TResult>(this);

#if !net40

        /// <summary>
        /// 执行查询
        /// </summary>
        /// <returns></returns>
        internal override async Task<TResult> ExecuteAsync<TResult>() => await this.DbContext.Database.ExecuteAsync<TResult>(this);

#endif

        /// <summary>
        /// 解析查询语义
        /// </summary>
        internal override IDbQueryTree Parse() => DbQueryableParser.Parse(this);
    }
}
