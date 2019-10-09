
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace TZM.XFramework.Data.SqlClient
{
    /// <summary>
    /// 数据上下文，表示 Xfw 框架的主入口点
    /// </summary>
    public class OracleDbContext : DbContextBase
    {
        private IDatabase _database = null;
        private string _connString = null;
        private int? _commandTimeout = null;

        /// <summary>
        /// 查询语义提供者
        /// </summary>
        public override IDbQueryProvider Provider { get { return OracleDbQueryProvider.Instance; } }

        /// <summary>
        /// 数据库对象，持有当前上下文的会话
        /// </summary>
        public override IDatabase Database
        {
            get
            {
                if (_database == null) _database = new OracleDatabase(this.Provider.DbProviderFactory, _connString)
                {
                    CommandTimeout = _commandTimeout
                };
                return _database;
            }
        }

        /// <summary>
        /// 初始化 <see cref="OracleDbContext"/> 类的新实例
        /// <para>
        /// 默认读取 XFrameworkConnString 配置里的连接串
        /// </para>
        /// </summary>
        public OracleDbContext()
            : base()
        {
        }

        /// <summary>
        /// 初始化 <see cref="OracleDbContext"/> 类的新实例
        /// <param name="connString">数据库连接字符串</param>
        /// </summary>
        public OracleDbContext(string connString)
            : this(connString, null)
        {
        }

        /// <summary>
        /// 初始化 <see cref="OracleDbContext"/> 类的新实例
        /// <param name="connString">数据库连接字符串</param>
        /// <param name="commandTimeout">执行命令超时时间</param>
        /// </summary>
        public OracleDbContext(string connString, int? commandTimeout)
            : base(connString, commandTimeout)
        {
            _connString = connString;
            _commandTimeout = commandTimeout;
        }

        /// <summary>
        /// 计算要插入、更新或删除的已修改对象的集，并执行相应命令以实现对数据库的更改
        /// </summary>
        /// <returns></returns>
        public override int SubmitChanges()
        {
            int rowCount = _dbQueryables.Count;
            if (rowCount == 0) return 0;

            IDataReader reader = null;
            List<int> identitys = null;
            List<Command> sqlList = this.Provider.Resolve(_dbQueryables);

            Func<IDbCommand, object> doExecute = cmd =>
            {
                reader = this.Database.ExecuteReader(cmd);
                TypeDeserializer deserializer = new TypeDeserializer(this.Database, reader, null);
                do
                {
                    List<int> autoIncrements = null;
                    deserializer.Deserialize<object>(out autoIncrements);

                    if (cmd.Parameters != null)
                    {
                        foreach (IDbDataParameter p in cmd.Parameters)
                        {
                            if (p.Direction != ParameterDirection.Output) continue;
                            if (identitys == null) identitys = new List<int>();
                            identitys.Add(Convert.ToInt32(p.Value));
                        }
                    }
                }
                while (reader.NextResult());

                // 释放当前的reader
                if (reader != null) reader.Dispose();

                return null;
            };

            try
            {
                this.Database.Execute<object>(sqlList, doExecute);
                this.SetAutoIncrementValue(_dbQueryables, identitys);
                return rowCount;
            }
            finally
            {
                if (reader != null) reader.Dispose();
                this.InternalDispose();
            }
        }

        /// <summary>
        /// 计算要插入、更新或删除的已修改对象的集，并执行相应命令以实现对数据库的更改
        /// </summary>
        /// <typeparam name="T">T</typeparam>
        /// <param name="result">提交更改并查询数据</param>
        /// <returns></returns>
        public override int SubmitChanges<T>(out List<T> result)
        {
            result = new List<T>();
            int rowCount = _dbQueryables.Count;
            if (rowCount == 0) return 0;

            List<T> q1 = null;
            IDataReader reader = null;
            List<int> identitys = null;
            List<Command> sqlList = this.Provider.Resolve(_dbQueryables);

            Func<IDbCommand, object> doExecute = cmd =>
            {
                reader = this.Database.ExecuteReader(cmd);
                TypeDeserializer deserializer = new TypeDeserializer(this.Database, reader, null);
                do
                {
                    List<int> autoIncrements = null;
                    var collection = deserializer.Deserialize<T>(out autoIncrements);
                    if (collection != null)
                    {
                        if (q1 == null) q1 = collection;
                    }

                    if (cmd.Parameters != null)
                    {
                        foreach (IDbDataParameter p in cmd.Parameters)
                        {
                            if (identitys == null) identitys = new List<int>();
                            if (p.Direction != ParameterDirection.Output) continue;
                            identitys.Add(Convert.ToInt32(p.Value));
                        }
                    }
                }
                while (reader.NextResult());

                // 释放当前的reader
                if (reader != null) reader.Dispose();
                return null;
            };

            try
            {
                this.Database.Execute<object>(sqlList, doExecute);
                result = q1 ?? new List<T>(0);
                this.SetAutoIncrementValue(_dbQueryables, identitys);
                return rowCount;
            }
            finally
            {
                if (reader != null) reader.Dispose();
                this.InternalDispose();
            }
        }

        /// <summary>
        /// 计算要插入、更新或删除的已修改对象的集，并执行相应命令以实现对数据库的更改
        /// </summary>
        /// <typeparam name="T1">T</typeparam>
        /// <typeparam name="T2">T</typeparam>
        /// <param name="result1">提交更改并查询数据</param>
        /// <returns></returns>
        public override int SubmitChanges<T1, T2>(out List<T1> result1, out List<T2> result2)
        {
            result1 = new List<T1>();
            result2 = new List<T2>();
            int rowCount = _dbQueryables.Count;
            if (rowCount == 0) return 0;

            List<T1> q1 = null;
            List<T2> q2 = null;
            IDataReader reader = null;
            List<int> identitys = null;
            List<Command> sqlList = this.Provider.Resolve(_dbQueryables);
            List<IMapping> maps = sqlList.ToList(x => x as IMapping, x => x is IMapping);

            Func<IDbCommand, object> doExecute = cmd =>
            {
                reader = this.Database.ExecuteReader(cmd);
                TypeDeserializer deserializer1 = null;
                TypeDeserializer deserializer2 = null;
                do
                {
                    if (q1 == null)
                    {
                        // 先查第一个类型集合
                        List<int> autoIncrements = null;
                        if (deserializer1 == null) deserializer1 = new TypeDeserializer(this.Database, reader, maps.Count > 0 ? maps[0] : null);
                        var collection = deserializer1.Deserialize<T1>(out autoIncrements);

                        if (collection != null)
                        {
                            q1 = collection;
                        }

                        if (cmd.Parameters != null)
                        {
                            foreach (IDbDataParameter p in cmd.Parameters)
                            {
                                if (identitys == null) identitys = new List<int>();
                                if (p.Direction != ParameterDirection.Output) continue;
                                identitys.Add(Convert.ToInt32(p.Value));
                            }
                        }
                    }
                    else
                    {
                        // 再查第二个类型集合
                        List<int> autoIncrements = null;
                        if (deserializer2 == null) deserializer2 = new TypeDeserializer(this.Database, reader, maps.Count > 1 ? maps[1] : null);
                        var collection = deserializer2.Deserialize<T2>(out autoIncrements);

                        if (collection != null)
                        {
                            if (q2 == null) q2 = collection;
                        }

                        if (cmd.Parameters != null)
                        {
                            foreach (IDbDataParameter p in cmd.Parameters)
                            {
                                if (identitys == null) identitys = new List<int>();
                                if (p.Direction != ParameterDirection.Output) continue;
                                identitys.Add(Convert.ToInt32(p.Value));
                            }
                        }
                    }

                }
                while (reader.NextResult());

                // 释放当前的reader
                if (reader != null) reader.Dispose();
                return null;
            };

            try
            {
                this.Database.Execute<object>(sqlList, doExecute);
                result1 = q1 ?? new List<T1>(0);
                result2 = q2 ?? new List<T2>(0);
                this.SetAutoIncrementValue(_dbQueryables, identitys);
                return rowCount;
            }
            finally
            {
                if (reader != null) reader.Dispose();
                this.InternalDispose();
            }
        }

#if !net40

        /// <summary>
        /// 计算要插入、更新或删除的已修改对象的集，并执行相应命令以实现对数据库的更改
        /// </summary>
        /// <returns></returns>
        public override async Task<int> SubmitChangesAsync()
        {
            int rowCount = _dbQueryables.Count;
            if (rowCount == 0) return 0;

            List<Command> sqlList = this.Provider.Resolve(_dbQueryables);
            List<int> identitys = new List<int>();
            IDataReader reader = null;

            try
            {
                Func<IDbCommand, Task<object>> func = async cmd =>
                {
                    reader = await base.Database.ExecuteReaderAsync(cmd);
                    TypeDeserializer deserializer = new TypeDeserializer(this.Database, reader, null);
                    do
                    {
                        var result = deserializer.Deserialize<int>();
                        foreach (IDbDataParameter p in cmd.Parameters)
                        {
                            if (p.Direction != ParameterDirection.Output) continue;
                            identitys.Add(Convert.ToInt32(p.Value));
                        }
                    }
                    while (reader.NextResult());

                    // 释放当前的reader
                    if (reader != null) reader.Dispose();

                    return null;
                };

                await this.Database.ExecuteAsync<object>(sqlList, func);
                this.SetAutoIncrementValue(_dbQueryables, identitys);
                return rowCount;
            }
            finally
            {
                if (reader != null) reader.Dispose();
                this.InternalDispose();
            }
        }

#endif
    }
}
