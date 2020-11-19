
#if !net40

using System;
using System.Data;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;

namespace Riz.XFramework.Data
{
    /// <summary>
    /// 上下文适配器
    /// </summary>
    internal sealed partial class OracleDatabase
    {
        /// <summary>
        /// 异步执行 SQL 语句，并返回两个实体集合
        /// </summary>
        /// <param name="query1">SQL 命令</param>
        /// <param name="query2">SQL 命令</param>
        public override async Task<Tuple<List<T1>, List<T2>>> ExecuteAsync<T1, T2>(IDbQueryable<T1> query1, IDbQueryable<T2> query2)
        {
            var result = await this.ExecuteAsync<T1, T2, None>(query1, query2, null);
            return new Tuple<List<T1>, List<T2>>(result.Item1, result.Item2);
        }

        /// <summary>
        /// 异步执行 SQL 语句，并返回两个实体集合
        /// </summary>
        /// <param name="query1">SQL 命令</param>
        /// <param name="query2">SQL 命令</param>
        /// <param name="query3">SQL 命令</param>
        public override async Task<Tuple<List<T1>, List<T2>, List<T3>>> ExecuteAsync<T1, T2, T3>(IDbQueryable<T1> query1, IDbQueryable<T2> query2, IDbQueryable<T3> query3)
        {
            List<T1> q1 = null;
            List<T2> q2 = null;
            List<T3> q3 = null;
            IDataReader reader = null;
            List<DbRawCommand> sqlList = query1.Provider.Translate(new List<object> { query1, query2, query3 });
            List<IMapInfo> maps = sqlList.ToList(a => a is IMapInfo, a => a as IMapInfo);

            TypeDeserializer deserializer1 = null;
            TypeDeserializer deserializer2 = null;
            TypeDeserializer deserializer3 = null;

            Func<System.Data.Common.DbCommand, Task<object>> doExecute = async cmd =>
            {
                reader = await base.ExecuteReaderAsync(cmd);
                do
                {
                    if (q1 == null)
                    {
                        deserializer1 = new TypeDeserializer(_desrializerImpl, reader, maps.Count > 0 ? maps[0] : null);
                        q1 = deserializer1.Deserialize<List<T1>>();
                    }
                    else if (q2 == null)
                    {
                        deserializer2 = new TypeDeserializer(_desrializerImpl, reader, maps.Count > 1 ? maps[1] : null);
                        q2 = deserializer2.Deserialize<List<T2>>();
                    }
                    else if (q3 == null)
                    {
                        deserializer3 = new TypeDeserializer(_desrializerImpl, reader, maps.Count > 2 ? maps[2] : null);
                        q3 = deserializer3.Deserialize<List<T3>>();
                    }
                }
                while (reader.NextResult());

                // 释放当前的reader
                if (reader != null) reader.Dispose();
                return null;
            };

            try
            {
                await base.DoExecuteAsync<object>(sqlList, doExecute);
                return new Tuple<List<T1>, List<T2>, List<T3>>(q1, q2, q3);
            }
            finally
            {
                if (reader != null) reader.Dispose();
            }
        }

        /// <summary>
        /// 异步执行 SQL 语句，并返回多个实体集合
        /// </summary>
        /// <param name="command">SQL 命令</param>
        public override Task<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>, List<T7>>> ExecuteAsync<T1, T2, T3, T4, T5, T6, T7>(IDbCommand command)
        {
            return this.ExecuteAsync<T1, T2, T3, T4, T5, T6, T7>(command);
        }

        /// <summary>
        /// 异步执行 SQL 语句，并返回多个实体集合
        /// </summary>
        /// <typeparam name="T1">第一个列表的元素类型</typeparam>
        /// <typeparam name="T2">第二个列表的元素类型</typeparam>
        /// <typeparam name="T3">第三个列表的元素类型</typeparam>
        /// <typeparam name="T4">第四个列表的元素类型</typeparam>
        /// <typeparam name="T5">第五个列表的元素类型</typeparam>
        /// <typeparam name="T6">第六个列表的元素类型</typeparam>
        /// <typeparam name="T7">第七个列表的元素类型</typeparam>
        /// <param name="command">SQL 命令</param>
        /// <param name="maps">实体映射描述集合</param>
        protected override async Task<Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>, List<T7>>> ExecuteAsync<T1, T2, T3, T4, T5, T6, T7>(IDbCommand command, List<IMapInfo> maps = null)
        {
            List<T1> q1 = null;
            List<T2> q2 = null;
            List<T3> q3 = null;
            List<T4> q4 = null;
            List<T5> q5 = null;
            List<T6> q6 = null;
            List<T7> q7 = null;
            IDataReader reader = null;
            List<DbRawCommand> myList = this.ParseCommand(command.CommandText, command.Parameters, command.CommandType);
            if (myList == null) q1 = await this.ExecuteAsync<List<T1>>(command, maps != null && maps.Count > 0 ? maps[0] : null);
            else
            {
                TypeDeserializer deserializer1 = null;
                TypeDeserializer deserializer2 = null;
                TypeDeserializer deserializer3 = null;
                TypeDeserializer deserializer4 = null;
                TypeDeserializer deserializer5 = null;
                TypeDeserializer deserializer6 = null;
                TypeDeserializer deserializer7 = null;

                Func<IDbCommand, Task<object>> doExecute = async cmd =>
                {
                    reader = await base.ExecuteReaderAsync(cmd);
                    do
                    {
                        if (q1 == null)
                        {
                            deserializer1 = new TypeDeserializer(_desrializerImpl, reader, maps != null && maps.Count > 0 ? maps[0] : null);
                            q1 = deserializer1.Deserialize<List<T1>>();
                        }
                        else if (q2 == null)
                        {
                            deserializer2 = new TypeDeserializer(_desrializerImpl, reader, maps != null && maps.Count > 1 ? maps[1] : null);
                            q2 = deserializer2.Deserialize<List<T2>>();
                        }
                        else if (q3 == null)
                        {
                            deserializer3 = new TypeDeserializer(_desrializerImpl, reader, maps != null && maps.Count > 2 ? maps[2] : null);
                            q3 = deserializer3.Deserialize<List<T3>>();
                        }
                        else if (q4 == null)
                        {
                            deserializer4 = new TypeDeserializer(_desrializerImpl, reader, maps != null && maps.Count > 3 ? maps[3] : null);
                            q4 = deserializer4.Deserialize<List<T4>>();
                        }
                        else if (q5 == null)
                        {
                            deserializer5 = new TypeDeserializer(_desrializerImpl, reader, maps != null && maps.Count > 4 ? maps[4] : null);
                            q5 = deserializer5.Deserialize<List<T5>>();
                        }
                        else if (q6 == null)
                        {
                            deserializer6 = new TypeDeserializer(_desrializerImpl, reader, maps != null && maps.Count > 5 ? maps[5] : null);
                            q6 = deserializer6.Deserialize<List<T6>>();
                        }
                        else if (q7 == null)
                        {
                            deserializer7 = new TypeDeserializer(_desrializerImpl, reader, maps != null && maps.Count > 6 ? maps[6] : null);
                            q7 = deserializer7.Deserialize<List<T7>>();
                        }
                    }
                    while (reader.NextResult());

                    // 释放当前的reader
                    if (reader != null) reader.Dispose();
                    return null;
                };

                try
                {
                    await base.DoExecuteAsync<object>(myList, doExecute);
                }
                finally
                {
                    if (reader != null) reader.Dispose();
                }
            }

            return new Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>, List<T7>>(
                q1 ?? new List<T1>(), q2 ?? new List<T2>(), q3 ?? new List<T3>(), q4 ?? new List<T4>(), q5 ?? new List<T5>(), q6 ?? new List<T6>(), q7 ?? new List<T7>());
        }

        /// <summary>
        /// 执行SQL 语句，并返回由 T 指定的对象
        /// </summary>
        /// <param name="sqlList">查询语句</param>
        /// <returns></returns>
        public override async Task<T> ExecuteAsync<T>(List<DbRawCommand> sqlList)
        {
            if (typeof(T) != typeof(DataSet)) return await base.ExecuteAsync<T>(sqlList);

            // 返回 DataSet
            List<DbRawCommand> myList = this.ParseCommand(sqlList);
            if (myList == null)
                return await base.ExecuteAsync<T>(sqlList);
            else
                return (T)(object)this.Execute(sqlList);
        }

        /// <summary>
        /// 执行SQL 语句，并返回由 T 指定的对象
        /// </summary>
        /// <param name="command">SQL 命令</param>
        /// <returns></returns>
        public override async Task<T> ExecuteAsync<T>(IDbCommand command)
        {
            if (typeof(T) != typeof(DataSet)) return await base.ExecuteAsync<T>(command);

            // 返回 DataSet
            List<DbRawCommand> myList = this.ParseCommand(command.CommandText, command.Parameters, command.CommandType);
            if (myList == null)
                return await base.ExecuteAsync<T>(command);
            else
                return (T)(object)this.Execute(myList);
        }
    }
}

#endif
