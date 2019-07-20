
using System.Data;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// 数据查询
    /// </summary>
    public abstract class DbQueryable : IDbQueryable
    {
        private bool _parameterized = false;
        private bool _hasSetParameterized = false;
        private IDbQueryableInfo _dbQueryInfo = null;
        private IDbQueryProvider _provider = null;
        protected IDbContext _context = null;

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
        public IDbQueryableInfo DbQueryInfo
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
        public IDbContext DbContext
        {
            get { return _context; }
            set { _context = value; }
        }

        /// <summary>
        /// 解析成 SQL 命令
        /// </summary>
        /// <param name="indent">缩进</param>
        /// <param name="isOuter">是否最外层，内层查询不需要结束符(;)</param>
        /// <param name="parameters">已存在的参数列表</param>
        /// <returns></returns>
        public abstract DbCommandDefinition Resolve(int indent = 0, bool isOuter = true, List<IDbDataParameter> parameters = null);

        /// <summary>
        /// 解析查询语义
        /// </summary>
        public abstract IDbQueryableInfo Parse(int startIndex = 0);
    }
}
