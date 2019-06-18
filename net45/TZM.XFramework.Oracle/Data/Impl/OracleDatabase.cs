
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
            List<SelectDbCommandDefinition> shapes = sqlList.ToList(x => x as SelectDbCommandDefinition, x => x is SelectDbCommandDefinition);

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
                        deserializer1 = new TypeDeserializer(reader, shapes.Count > 0 ? shapes[0] : null);
                        q1 = deserializer1.Deserialize<T1>();
                    }
                    else if (q2 == null)
                    {
                        deserializer2 = new TypeDeserializer(reader, shapes.Count > 1 ? shapes[1] : null);
                        q2 = deserializer2.Deserialize<T2>();
                    }
                    else if (q3 == null)
                    {
                        deserializer3 = new TypeDeserializer(reader, shapes.Count > 2 ? shapes[2] : null);
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

        // 执行提交
        protected override List<int> DoSubmit(List<DbCommandDefinition> sqlList)
        {
            List<int> identitys = new List<int>();
            IDataReader reader = null;

            try
            {
                Func<IDbCommand, object> func = cmd =>
                {
                    reader = base.ExecuteReader(cmd);
                    TypeDeserializer deserializer = new TypeDeserializer(reader, null);
                    do
                    {
                        List<int> autoIncrements = null;
                        deserializer.Deserialize<object>(out autoIncrements);

                        if (cmd.Parameters != null)
                        {
                            foreach (IDbDataParameter p in cmd.Parameters)
                            {
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

                base.DoExecute<object>(sqlList, func);
                return identitys;
            }
            finally
            {
                if (reader != null) reader.Dispose();
            }
        }

        // 执行提交
        protected override List<int> DoSubmit<T>(List<DbCommandDefinition> sqlList, out List<T> result)
        {
            IDataReader reader = null;
            List<int> identitys = null;
            result = null;
            List<T> q1 = null;

            Func<IDbCommand, object> doExecute = cmd =>
            {
                reader = base.ExecuteReader(cmd);
                TypeDeserializer deserializer = new TypeDeserializer(reader, null);
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
                base.DoExecute<object>(sqlList, doExecute);
                result = q1 ?? new List<T>(0);
                return identitys;
            }
            finally
            {
                if (reader != null) reader.Dispose();
            }
        }

        // 执行提交
        protected override List<int> DoSubmit<T1, T2>(List<DbCommandDefinition> sqlList, out List<T1> result1, out List<T2> result2)
        {
            IDataReader reader = null;
            List<int> identitys = null;
            result1 = null;
            List<T1> q1 = null;
            List<T2> q2 = null;
            List<SelectDbCommandDefinition> shapes = sqlList.ToList(x => x as SelectDbCommandDefinition, x => x is SelectDbCommandDefinition);

            Func<IDbCommand, object> doExecute = cmd =>
            {
                reader = base.ExecuteReader(cmd);
                TypeDeserializer deserializer1 = null;
                TypeDeserializer deserializer2 = null;
                do
                {
                    if (q1 == null)
                    {
                        // 先查第一个类型集合
                        List<int> autoIncrements = null;
                        if (deserializer1 == null) deserializer1 = new TypeDeserializer(reader, shapes.Count > 0 ? shapes[0] : null);
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
                        if (deserializer2 == null) deserializer2 = new TypeDeserializer(reader, shapes.Count > 1 ? shapes[1] : null);
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
                base.DoExecute<object>(sqlList, doExecute);
                result1 = q1 ?? new List<T1>(0);
                result2 = q2 ?? new List<T2>(0);
                return identitys;
            }
            finally
            {
                if (reader != null) reader.Dispose();
            }
        }
    }
}
