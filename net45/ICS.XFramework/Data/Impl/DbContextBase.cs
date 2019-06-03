
using System;
using System.Linq.Expressions;
using System.Collections.Generic;

namespace ICS.XFramework.Data
{
    /// <summary>
    /// 数据上下文基类入口
    /// </summary>
    public abstract partial class DbContextBase : IDbContext
    {
        // 结构层次： 
        // IDataContext=>IDbQueryable=>IDbQueryableInfo

        #region 私有字段

        private readonly List<object> _dbQueryables = new List<object>();
        private readonly object _oLock = new object();
        private Database _database = null;
        private IDbQueryProvider _provider = null;

        #endregion

        #region 公开属性

        /// <summary>
        /// <see cref="IDbQueryable"/> 的解析执行提供程序
        /// </summary>
        public IDbQueryProvider Provider
        {
            get { return _provider; }
        }

        /// <summary>
        /// 数据库对象，持有当前上下文的会话
        /// </summary>
        public IDatabase Database
        {
            get { return _database; }
        }

        #endregion

        #region 构造函数

        /// <summary>
        /// 初始化 <see cref="DbContextBase"/> 类的新实例
        /// <para>
        /// 默认读取 XFrameworkConnString 配置里的连接串
        /// </para>
        /// </summary>
        public DbContextBase()
            : this(XfwCommon.GetConnString("XFrameworkConnString"))
        {
        }

        /// <summary>
        /// 初始化 <see cref="DbContextBase"/> 类的新实例
        /// <param name="connString">数据库连接字符串</param>
        /// </summary>
        public DbContextBase(string connString)
            : this(connString, null)
        {
        }

        /// <summary>
        /// 初始化 <see cref="DbContextBase"/> 类的新实例
        /// <param name="connString">数据库连接字符串</param>
        /// <param name="commandTimeout">执行命令超时时间</param>
        /// </summary>
        public DbContextBase(string connString, int? commandTimeout)
        {
            _database = (Database)this.CreateDatabase(connString, commandTimeout);
            _provider = this.CreateQueryProvider();
            _provider.DbProviderFactory = _database.DbProviderFactory;
        }

        #endregion

        #region 公开方法

        /// <summary>
        /// 新增记录
        /// </summary>
        public virtual void Insert<T>(T TEntity)
        {
            IDbQueryable<T> table = this.GetTable<T>();
            table.DbExpressions.Add(new DbExpression
            {
                DbExpressionType = DbExpressionType.Insert,
                Expressions = new[] { Expression.Constant(TEntity) }
            });

            lock (this._oLock)
                _dbQueryables.Add(table);
        }

        /// <summary>
        /// 批量新增记录
        /// </summary>
        public virtual void Insert<T>(IEnumerable<T> collection)
        {
            this.Insert<T>(collection, null);
        }

        /// <summary>
        /// 批量新增记录
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection">批量插入列表</param>
        /// <param name="entityColumns">指定插入的列</param>
        public virtual void Insert<T>(IEnumerable<T> collection, IList<Expression> entityColumns)
        {
            List<IDbQueryable> bulkList = new List<IDbQueryable>();
            foreach (T value in collection)
            {
                IDbQueryable<T> table = this.GetTable<T>();
                table.DbExpressions.Add(new DbExpression
                {
                    DbExpressionType = DbExpressionType.Insert,
                    Expressions = entityColumns != null ? new[] { Expression.Constant(value), Expression.Constant(entityColumns) } : new[] { Expression.Constant(value) }
                });

                bulkList.Add(table);
            }

            lock (this._oLock)
                _dbQueryables.Add(bulkList);
        }

        /// <summary>
        /// 批量新增记录
        /// </summary>
        public virtual void Insert<T>(IDbQueryable<T> query)
        {
            query = query.CreateQuery<T>(new DbExpression(DbExpressionType.Insert));
            lock (this._oLock)
                _dbQueryables.Add(query);
        }

        /// <summary>
        /// 删除记录
        /// </summary>
        public void Delete<T>(T TEntity)
        {
            IDbQueryable<T> query = this.GetTable<T>();
            query.DbExpressions.Add(new DbExpression
            {
                DbExpressionType = DbExpressionType.Delete,
                Expressions = new[] { Expression.Constant(TEntity) }
            });
            lock (this._oLock)
                _dbQueryables.Add(query);
        }

        /// <summary>
        /// 删除记录
        /// </summary>
        public void Delete<T>(Expression<Func<T, bool>> predicate)
        {
            IDbQueryable<T> query = this.GetTable<T>();
            this.Delete<T>(query.Where(predicate));
        }

        /// <summary>
        /// 删除记录
        /// </summary>
        public void Delete<T>(IDbQueryable<T> query)
        {
            query = query.CreateQuery<T>(new DbExpression(DbExpressionType.Delete));
            lock (this._oLock)
                _dbQueryables.Add(query);
        }

        /// <summary>
        /// 更新记录
        /// </summary>
        public void Update<T>(T TEntity)
        {
            IDbQueryable<T> query = this.GetTable<T>();
            this.Update<T>(Expression.Constant(TEntity), query);
        }

        /// <summary>
        /// 更新记录
        /// </summary>
        public virtual void Update<T>(Expression<Func<T, object>> updateExpression, Expression<Func<T, bool>> predicate)
        {
            IDbQueryable<T> query = this.GetTable<T>();
            this.Update<T>((Expression)updateExpression, query.Where(predicate));
        }

        /// <summary>
        /// 更新记录
        /// </summary>
        public virtual void Update<T>(Expression<Func<T, object>> updateExpression, IDbQueryable<T> query)
        {
            this.Update<T>((Expression)updateExpression, query);
        }

        /// <summary>
        /// 更新记录
        /// </summary>
        public virtual void Update<T, TSource>(Expression<Func<T, TSource, object>> updateExpression, IDbQueryable<T> query)
        {
            this.Update<T>((Expression)updateExpression, query);
        }

        /// <summary>
        /// 更新记录
        /// </summary>
        public virtual void Update<T, TSource1, TSource2>(Expression<Func<T, TSource1, TSource2, object>> updateExpression, IDbQueryable<T> query)
        {
            this.Update<T>((Expression)updateExpression, query);
        }

        /// <summary>
        /// 更新记录
        /// </summary>
        public virtual void Update<T, TSource1, TSource2, TSource3>(Expression<Func<T, TSource1, TSource2, TSource3, object>> updateExpression, IDbQueryable<T> query)
        {
            this.Update<T>((Expression)updateExpression, query);
        }

        /// <summary>
        /// 更新记录
        /// </summary>
        protected void Update<T>(Expression updateExpression, IDbQueryable<T> query)
        {
            query = query.CreateQuery<T>(new DbExpression
            {
                DbExpressionType = DbExpressionType.Update,
                Expressions = new[] { updateExpression }
            });
            lock (this._oLock)
                _dbQueryables.Add(query);
        }

        /// <summary>
        /// 附加查询项
        /// </summary>
        public void AddQuery(string query, params object[] args)
        {
            var builder = _provider.CreateSqlBuilder(null);
            if (args != null && !string.IsNullOrEmpty(query))
            {
                for (int i = 0; i < args.Length; i++) args[i] = builder.GetSqlValue(args[i]);
                query = string.Format(query, args);
            }
            lock (this._oLock)
                if (!string.IsNullOrEmpty(query)) _dbQueryables.Add(query);
        }

        /// <summary>
        /// 附加查询项
        /// </summary>
        public void AddQuery(IDbQueryable query)
        {
            lock (this._oLock)
                _dbQueryables.Add(query);
        }

        /// <summary>
        /// 计算要插入、更新或删除的已修改对象的集，并执行相应命令以实现对数据库的更改
        /// </summary>
        /// <returns></returns>
        public virtual int SubmitChanges()
        {
            int rowCount = _dbQueryables.Count;
            if (rowCount == 0) return 0;

            List<DbCommandDefinition> sqlList = _provider.Resolve(_dbQueryables);
            List<int> identitys = _database.Submit(sqlList);
            SetAutoIncrementValue(_dbQueryables, identitys);
            this.InternalDispose();

            return rowCount;
        }

        /// <summary>
        /// 计算要插入、更新或删除的已修改对象的集，并执行相应命令以实现对数据库的更改
        /// </summary>
        /// <typeparam name="T">T</typeparam>
        /// <param name="result">提交更改并查询数据</param>
        /// <returns></returns>
        public virtual int SubmitChanges<T>(out List<T> result)
        {
            result = new List<T>();
            int rowCount = _dbQueryables.Count;
            if (rowCount == 0) return 0;

            List<DbCommandDefinition> sqlList = _provider.Resolve(_dbQueryables);
            List<int> identitys = _database.Submit<T>(sqlList, out result);
            SetAutoIncrementValue(_dbQueryables, identitys);
            this.InternalDispose();

            return rowCount;
        }

        /// <summary>
        /// 计算要插入、更新或删除的已修改对象的集，并执行相应命令以实现对数据库的更改
        /// </summary>
        /// <typeparam name="T1">T</typeparam>
        /// <typeparam name="T2">T</typeparam>
        /// <param name="result1">提交更改并查询数据</param>
        /// <returns></returns>
        public virtual int SubmitChanges<T1, T2>(out List<T1> result1, out List<T2> result2)
        {
            result1 = new List<T1>();
            result2 = new List<T2>();
            int rowCount = _dbQueryables.Count;
            if (rowCount == 0) return 0;

            List<DbCommandDefinition> sqlList = _provider.Resolve(_dbQueryables);
            List<int> identitys = _database.Submit<T1, T2>(sqlList, out result1, out result2);
            SetAutoIncrementValue(_dbQueryables, identitys);
            this.InternalDispose();

            return rowCount;
        }

        /// <summary>
        /// 返回特定类型的对象的集合，其中类型由 T 参数定义
        /// </summary>
        public IDbQueryable<T> GetTable<T>()
        {
            DbQueryable<T> queryable = new DbQueryable<T> { DbContext = this };
            queryable.DbExpressions = new List<DbExpression> { new DbExpression
            {
                DbExpressionType = DbExpressionType.GetTable,
                Expressions = new[] { Expression.Constant(typeof(T)) }
            } };
            return queryable;
        }

        /// <summary>
        /// 释放由 <see cref="DbContextBase"/> 类的当前实例占用的所有资源
        /// </summary>
        public void Dispose()
        {
            this.InternalDispose();
            if (this.Database != null) this.Database.Dispose();
        }

        /// <summary>
        /// 释放由 <see cref="DbContextBase"/> 类的当前实例占用的所有资源
        /// </summary>
        protected void InternalDispose()
        {
            lock (this._oLock) this._dbQueryables.Clear();
        }

        #endregion

        #region 私有函数

        /// <summary>
        /// 创建据库会话实例
        /// </summary>
        /// <param name="connString">连接字符串</param>
        /// <param name="commandTimeout">执行命令超时时间</param>
        /// <returns></returns>
        protected abstract IDatabase CreateDatabase(string connString, int? commandTimeout);

        /// <summary>
        /// 创建 <see cref="IDbQueryProvider"/> 对象实例
        /// </summary>
        /// <returns></returns>
        protected abstract IDbQueryProvider CreateQueryProvider();

        // 更新自增列
        private void SetAutoIncrementValue(List<object> dbQueryables, List<int> identitys)
        {
            if (identitys == null || identitys.Count == 0) return;

            int index = -1;
            foreach (var obj in dbQueryables)
            {
                IDbQueryable query = obj as IDbQueryable;
                if (query == null) continue;

                var info = query.DbQueryInfo as IDbQueryableInfo_Insert;
                if (info != null && info.Entity != null && info.AutoIncrement != null)
                {
                    index += 1;
                    var identity = identitys[index];
                    info.AutoIncrement.Invoke(info.Entity, identity);
                }
            }
        }

        #endregion
    }
}
