
using System;
using System.Data;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// 上下文适配器
    /// </summary>
    internal sealed partial class OracleDatabase : Database
    {
        /// <summary>
        /// 初始化 <see cref="OracleDatabase"/> 类的新实例
        /// </summary>
        /// <param name="providerFactory">数据源提供者</param>
        /// <param name="connectionString">数据库连接字符串</param>
        public OracleDatabase(DbProviderFactory providerFactory, string connectionString)
            : base(providerFactory, connectionString)
        {
        }

        /// <summary>
        /// 执行 SQL 语句，并返回两个实体集合
        /// </summary>
        /// <param name="query1">SQL 命令</param>
        /// <param name="query2">SQL 命令</param>
        public override Tuple<List<T1>, List<T2>> ExecuteMultiple<T1, T2>(IDbQueryable<T1> query1, IDbQueryable<T2> query2)
        {
            var result = this.ExecuteMultiple<T1, T2, None>(query1, query2, null);
            return new Tuple<List<T1>, List<T2>>(result.Item1, result.Item2);
        }

        /// <summary>
        /// 执行 SQL 语句，并返回两个实体集合
        /// </summary>
        /// <param name="query1">SQL 命令</param>
        /// <param name="query2">SQL 命令</param>
        /// <param name="query3">SQL 命令</param>
        /// <param name="trans">事务对象</param>
        public override Tuple<List<T1>, List<T2>, List<T3>> ExecuteMultiple<T1, T2, T3>(IDbQueryable<T1> query1, IDbQueryable<T2> query2, IDbQueryable<T3> query3)
        {
            List<T1> q1 = null;
            List<T2> q2 = null;
            List<T3> q3 = null;
            IDataReader reader = null;
            List<DbCommandDefinition> sqlList = query1.Provider.Resolve(new List<object> { query1, query2, query3 });
            List<DbCommandDefinition_Select> defines = sqlList.ToList(x => x as DbCommandDefinition_Select, x => x is DbCommandDefinition_Select);

            TypeDeserializer deserializer1 = null;
            TypeDeserializer deserializer2 = null;
            TypeDeserializer deserializer3 = null;

            Func<IDbCommand, object> doExecute = cmd =>
            {
                reader = base.ExecuteReader(cmd);
                do
                {
                    if (q1 == null)
                    {
                        deserializer1 = new TypeDeserializer(reader, defines.Count > 0 ? defines[0] : null);
                        q1 = deserializer1.Deserialize<T1>();
                    }
                    else if (q2 == null)
                    {
                        deserializer2 = new TypeDeserializer(reader, defines.Count > 1 ? defines[1] : null);
                        q2 = deserializer2.Deserialize<T2>();
                    }
                    else if (q3 == null)
                    {
                        deserializer3 = new TypeDeserializer(reader, defines.Count > 2 ? defines[2] : null);
                        q3 = deserializer3.Deserialize<T3>();
                    }
                }
                while (reader.NextResult());

                // 释放当前的reader
                if (reader != null) reader.Dispose();
                return null;
            };

            try
            {
                base.DoExecute<object>(sqlList, doExecute);
                return new Tuple<List<T1>, List<T2>, List<T3>>(q1, q2, q3);
            }
            finally
            {
                if (reader != null) reader.Dispose();
            }
        }

        /// <summary>
        /// 执行 SQL 语句，并返回多个实体集合
        /// </summary>
        /// <param name="cmd">SQL 命令</param>
        public override Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>, List<T7>> ExecuteMultiple<T1, T2, T3, T4, T5, T6, T7>(IDbCommand cmd)
        {
            throw new NotSupportedException("Oracle ExecuteMultiple not supported.");
        }
    }
}
