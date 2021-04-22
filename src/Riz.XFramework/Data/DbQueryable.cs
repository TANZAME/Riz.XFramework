
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Riz.XFramework.Data
{
    /// <summary>
    /// 数据查询
    /// </summary>
    public abstract class DbQueryable : IDbQueryable
    {
        private IDbContext _context = null;
        private bool _parameterized = false;
        private bool _hasSetParameterized = false;

        /// <summary>
        /// 数据查询提供者
        /// </summary>
        public IDbQueryProvider Provider => _context.Provider;

        /// <summary>
        /// 查询表达式
        /// </summary>
        public abstract ReadOnlyCollection<DbExpression> DbExpressions { get; }

        /// <summary>
        /// 字符串表示形式。无参数，主要用于调式
        /// </summary>
        public abstract string Sql { get; }

        /// <summary>
        /// 参数化
        /// </summary>
        internal bool Parameterized
        {
            get { return _parameterized; }
            set
            {
                _parameterized = value;
                _hasSetParameterized = true;
            }
        }

        /// <summary>
        /// Parameterized 属性是否已被设置
        /// </summary>
        internal bool HasSetParameterized => _hasSetParameterized;

        /// <summary>
        /// 批量插入信息
        /// </summary>
        internal BulkInsertInfo Bulk { get; set; }

        /// <summary>
        /// 当前查询上下文
        /// </summary>
        internal IDbContext DbContext => _context;

        /// <summary>
        /// 实例化类<see cref="DbQueryable"/>的新实例
        /// </summary>
        /// <param name="context"></param>
        public DbQueryable(IDbContext context) => this._context = context;

        /// <summary>
        /// 解析成 SQL 命令
        /// </summary>
        public abstract DbRawCommand Translate();

        /// <summary>
        /// 解析成 SQL 命令
        /// </summary>
        /// <param name="indent">缩进</param>
        /// <param name="isOuterQuery">是否最外层，内层查询不需要结束符(;)</param>
        /// <param name="context">解析SQL命令上下文</param>
        /// <returns></returns>
        internal abstract DbRawCommand Translate(int indent, bool isOuterQuery, ITranslateContext context);

        /// <summary>
        /// 执行查询
        /// </summary>
        /// <returns></returns>
        internal abstract TResult Execute<TResult>();

#if !net40

        /// <summary>
        /// 执行查询
        /// </summary>
        /// <returns></returns>
        internal abstract Task<TResult> ExecuteAsync<TResult>();

#endif

        /// <summary>
        /// 解析查询语义
        /// </summary>
        internal abstract IDbQueryTree Parse();
    }
}
