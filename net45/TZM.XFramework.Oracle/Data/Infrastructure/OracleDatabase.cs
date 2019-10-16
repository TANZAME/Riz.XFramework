
using System;
using System.Data;
using System.Linq;
using System.Data.Common;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// 上下文适配器
    /// </summary>
    internal sealed partial class OracleDatabase : Database
    {
        /// <summary>
        /// 实体转换映射委托生成器
        /// </summary>
        public override TypeDeserializerImpl TypeDeserializerImpl { get { return OracleTypeDeserializerImpl.Instance; } }

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
            List<Command> sqlList = query1.Provider.Resolve(new List<object> { query1, query2, query3 });
            List<IMapping> maps = sqlList.ToList(x => x as IMapping, x => x is IMapping);

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
                        deserializer1 = new TypeDeserializer(this, reader, maps.Count > 0 ? maps[0] : null);
                        q1 = deserializer1.Deserialize<T1>();
                    }
                    else if (q2 == null)
                    {
                        deserializer2 = new TypeDeserializer(this, reader, maps.Count > 1 ? maps[1] : null);
                        q2 = deserializer2.Deserialize<T2>();
                    }
                    else if (q3 == null)
                    {
                        deserializer3 = new TypeDeserializer(this, reader, maps.Count > 2 ? maps[2] : null);
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
        /// <param name="command">SQL 命令</param>
        public override Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>, List<T7>> ExecuteMultiple<T1, T2, T3, T4, T5, T6, T7>(IDbCommand command)
        {
            throw new NotSupportedException("Oracle ExecuteMultiple not supported.");
        }

        /// <summary>
        /// 执行SQL 语句，并返回 <see cref="DataSet"/> 对象
        /// </summary>
        /// <param name="sqlList">SQL 命令</param>
        /// <returns></returns>
        public override DataSet ExecuteDataSet(List<Command> sqlList)
        {
            int index = 0;
            var result = new DataSet();
            IDataReader reader = null;
            List<Command> myList = this.Resove(sqlList);

            Func<IDbCommand, DataTable> doExecute = cmd =>
            {
                DataTable table = this.ExecuteDataTable(cmd);
                table.TableName = string.Format("TALBE{0}", index);
                index += 1;
                result.Tables.Add(table);
                return null;
            };

            try
            {
                base.DoExecute<DataTable>(myList, doExecute);
                return result;
            }
            finally
            {
                if (reader != null) reader.Dispose();
            }
        }

        List<Command> Resove(List<Command> sqlList)
        {            // 重新组合脚本，把 SELECT 和 UPDATE/DELETE 等分开来
            bool haveBegin = false;
            var myList = new List<Command>();
            
            for (int i = 0; i < sqlList.Count; i++)
            {
                var cmd = sqlList[i];
                if (cmd == null) continue;

                string sql = cmd.CommandText;
                if (string.IsNullOrEmpty(sql)) continue;

                string methodName = string.Empty;
                if (sql.Length > 6) methodName = sql.Substring(0, 6).Trim().ToUpper();
                if (cmd is NavigationCommand || methodName == "SELECT")
                {
                    // 查询单独执行
                    if (myList.Count > 0 && (i - 1) >= 0 && myList[myList.Count - 1] != null) myList.Add(null);

                    // 创建新命令
                    var parameters = cmd.Parameters == null
                        ? null
                        : cmd.Parameters.ToList(x => (IDbDataParameter)this.DbProviderFactory.CreateParameter(x.ParameterName, x.Value, x.DbType, x.Size, x.Precision, x.Scale, x.Direction));
                    myList.Add(new Command(cmd.CommandText, parameters, cmd.CommandType));
                    myList.Add(null);
                }
                else
                {
                    // 增删改
                    if (!haveBegin)
                    {
                        myList.Add(new Command("BEGIN"));
                        haveBegin = true;
                    }

                    // 创建新命令
                    var parameters = cmd.Parameters == null
                        ? null
                        : cmd.Parameters.ToList(x => (IDbDataParameter)this.DbProviderFactory.CreateParameter(x.ParameterName, x.Value, x.DbType, x.Size, x.Precision, x.Scale, x.Direction));
                    myList.Add(new Command(cmd.CommandText, parameters, cmd.CommandType));

                    // 检查下一条是否是选择语句
                    bool isQuery = false;
                    if (i + 1 < sqlList.Count)
                    {
                        cmd = sqlList[i];
                        sql = cmd.CommandText;
                        methodName = string.Empty;
                        if (!string.IsNullOrEmpty(sql) && sql.Length > 6) methodName = sql.Substring(0, 6).Trim().ToUpper();
                        isQuery = methodName == "SELECT";
                    }

                    // 如果下一条是SELECT 语句，则需要结束当前语句块
                    if (isQuery)
                    {
                        if (haveBegin)
                        {
                            myList.Add(new Command("END;"));
                            haveBegin = false;
                            myList.Add(null);
                        }
                    }
                }

                if (haveBegin && i == sqlList.Count - 1) myList.Add(new Command("END;"));
            }

            return myList;
        }
    }
}
