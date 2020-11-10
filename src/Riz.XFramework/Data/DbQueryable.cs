
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Riz.XFramework.Data
{
    /// <summary>
    /// 数据查询
    /// </summary>
    public abstract class DbQueryable : IDbQueryable
    {
        private bool _parameterized = false;
        private bool _hasSetParameterized = false;
        private DbQueryTree _dbQueryInfo = null;
        private IDbQueryProvider _provider = null;
        private IDbContext _context = null;

        /// <summary>
        /// 参数化
        /// </summary>
        public bool Parameterized
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
        public bool HasSetParameterized
        {
            get { return _hasSetParameterized; }
        }


        /// <summary>
        /// 数据查询提供者
        /// </summary>
        public IDbQueryProvider Provider
        {
            get
            {
                if (_provider == null) _provider = _context.Provider;
                return _provider;
            }
        }

        /// <summary>
        /// 查询表达式
        /// </summary>
        public abstract ReadOnlyCollection<DbExpression> DbExpressions { get; }

        /// <summary>
        /// 转换后的查询对象
        /// </summary>
        public DbQueryTree DbQueryInfo
        {
            get { return _dbQueryInfo; }
            set { _dbQueryInfo = value; }
        }

        /// <summary>
        /// 批量插入信息
        /// </summary>
        public BulkInsertInfo Bulk { get; set; }

        /// <summary>
        /// 当前查询上下文
        /// </summary>
        public IDbContext DbContext { get { return _context; } }

        /// <summary>
        /// 实例化类<see cref="DbQueryable"/>的新实例
        /// </summary>
        /// <param name="context"></param>
        public DbQueryable(IDbContext context)
        {
            this._context = context;
        }

        /// <summary>
        /// 解析成 SQL 命令
        /// </summary>
        public abstract DbRawCommand Translate();

        /// <summary>
        /// 解析成 SQL 命令
        /// </summary>
        /// <param name="indent">缩进</param>
        /// <param name="isOutQuery">是否最外层，内层查询不需要结束符(;)</param>
        /// <param name="context">解析SQL命令上下文</param>
        /// <returns></returns>
        public abstract DbRawCommand Translate(int indent, bool isOutQuery, ITranslateContext context);

        /// <summary>
        /// 执行查询
        /// </summary>
        /// <returns></returns>
        public abstract TResult Execute<TResult>();

#if !net40

        /// <summary>
        /// 执行查询
        /// </summary>
        /// <returns></returns>
        public abstract Task<TResult> ExecuteAsync<TResult>();

#endif

        /// <summary>
        /// 解析查询语义
        /// </summary>
        public abstract DbQueryTree Parse();
    }
}
