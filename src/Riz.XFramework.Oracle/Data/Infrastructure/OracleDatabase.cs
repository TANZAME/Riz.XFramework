

using System;
using System.Data;
using System.Linq;
using System.Data.Common;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Oracle.ManagedDataAccess.Client;

namespace Riz.XFramework.Data
{
    /// <summary>
    /// 上下文适配器
    /// </summary>
    public sealed partial class OracleDatabase : Database
    {
        private IDbContext _context = null;
        private DbProviderFactory _provider = null;

        /// <summary>
        /// 初始化 <see cref="OracleDatabase"/> 类的新实例
        /// </summary>
        /// <param name="context">当前查询上下文</param>
        public OracleDatabase(IDbContext context)
            : base(context)
        {
            _context = context;
            _provider = _context.Provider.DbProvider;
        }

        /// <summary>
        /// 执行 SQL 语句，并返回两个实体集合
        /// </summary>
        /// <param name="query1">SQL 命令</param>
        /// <param name="query2">SQL 命令</param>
        public override Tuple<List<T1>, List<T2>> Execute<T1, T2>(IDbQueryable<T1> query1, IDbQueryable<T2> query2)
        {
            var result = this.Execute<T1, T2, None>(query1, query2, null);
            return new Tuple<List<T1>, List<T2>>(result.Item1, result.Item2);
        }

        /// <summary>
        /// 执行 SQL 语句，并返回两个实体集合
        /// </summary>
        /// <param name="query1">SQL 命令</param>
        /// <param name="query2">SQL 命令</param>
        /// <param name="query3">SQL 命令</param>
        public override Tuple<List<T1>, List<T2>, List<T3>> Execute<T1, T2, T3>(IDbQueryable<T1> query1, IDbQueryable<T2> query2, IDbQueryable<T3> query3)
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

            Func<IDbCommand, object> doExecute = cmd =>
            {
                reader = base.ExecuteReader(cmd);
                do
                {
                    if (q1 == null)
                    {
                        deserializer1 = new TypeDeserializer(_context, reader, maps.Count > 0 ? maps[0] : null);
                        q1 = deserializer1.Deserialize<List<T1>>();
                    }
                    else if (q2 == null)
                    {
                        deserializer2 = new TypeDeserializer(_context, reader, maps.Count > 1 ? maps[1] : null);
                        q2 = deserializer2.Deserialize<List<T2>>();
                    }
                    else if (q3 == null)
                    {
                        deserializer3 = new TypeDeserializer(_context, reader, maps.Count > 2 ? maps[2] : null);
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
        public override Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>, List<T7>> Execute<T1, T2, T3, T4, T5, T6, T7>(IDbCommand command)
        {
            return this.Execute<T1, T2, T3, T4, T5, T6, T7>(command);
        }

        /// <summary>
        /// 执行 SQL 语句，并返回多个实体集合
        /// </summary>
        /// <typeparam name="T1">第一个列表的元素类型</typeparam>
        /// <typeparam name="T2">第二个列表的元素类型</typeparam>
        /// <typeparam name="T3">第三个列表的元素类型</typeparam>
        /// <typeparam name="T4">第四个列表的元素类型</typeparam>
        /// <typeparam name="T5">第五个列表的元素类型</typeparam>
        /// <typeparam name="T6">第六个列表的元素类型</typeparam>
        /// <typeparam name="T7">第七个列表的元素类型</typeparam>
        /// <param name="command">SQL 命令</param>
        /// <param name="maps">实体映射描述列表</param>
        /// <returns></returns>
        protected override Tuple<List<T1>, List<T2>, List<T3>, List<T4>, List<T5>, List<T6>, List<T7>> Execute<T1, T2, T3, T4, T5, T6, T7>(IDbCommand command, List<IMapInfo> maps = null)
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
            if (myList == null) q1 = this.Execute<List<T1>>(command, maps != null && maps.Count > 0 ? maps[0] : null);
            else
            {
                TypeDeserializer deserializer1 = null;
                TypeDeserializer deserializer2 = null;
                TypeDeserializer deserializer3 = null;
                TypeDeserializer deserializer4 = null;
                TypeDeserializer deserializer5 = null;
                TypeDeserializer deserializer6 = null;
                TypeDeserializer deserializer7 = null;

                Func<IDbCommand, object> doExecute = cmd =>
                {
                    reader = base.ExecuteReader(cmd);
                    do
                    {
                        if (q1 == null)
                        {
                            deserializer1 = new TypeDeserializer(_context, reader, maps != null && maps.Count > 0 ? maps[0] : null);
                            q1 = deserializer1.Deserialize<List<T1>>();
                        }
                        else if (q2 == null)
                        {
                            deserializer2 = new TypeDeserializer(_context, reader, maps != null && maps.Count > 1 ? maps[1] : null);
                            q2 = deserializer2.Deserialize<List<T2>>();
                        }
                        else if (q3 == null)
                        {
                            deserializer3 = new TypeDeserializer(_context, reader, maps != null && maps.Count > 2 ? maps[2] : null);
                            q3 = deserializer3.Deserialize<List<T3>>();
                        }
                        else if (q4 == null)
                        {
                            deserializer4 = new TypeDeserializer(_context, reader, maps != null && maps.Count > 3 ? maps[3] : null);
                            q4 = deserializer4.Deserialize<List<T4>>();
                        }
                        else if (q5 == null)
                        {
                            deserializer5 = new TypeDeserializer(_context, reader, maps != null && maps.Count > 4 ? maps[4] : null);
                            q5 = deserializer5.Deserialize<List<T5>>();
                        }
                        else if (q6 == null)
                        {
                            deserializer6 = new TypeDeserializer(_context, reader, maps != null && maps.Count > 5 ? maps[5] : null);
                            q6 = deserializer6.Deserialize<List<T6>>();
                        }
                        else if (q7 == null)
                        {
                            deserializer7 = new TypeDeserializer(_context, reader, maps != null && maps.Count > 6 ? maps[6] : null);
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
                    base.DoExecute<object>(myList, doExecute);
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
        public override T Execute<T>(List<DbRawCommand> sqlList)
        {
            if (typeof(T) != typeof(DataSet)) return base.Execute<T>(sqlList);

            // 返回 DataSet
            List<DbRawCommand> myList = this.ParseCommand(sqlList);
            if (myList == null)
                return base.Execute<T>(sqlList);
            else
                return (T)(object)this.Execute(sqlList);
        }

        /// <summary>
        /// 执行SQL 语句，并返回由 T 指定的对象
        /// </summary>
        /// <param name="command">SQL 命令</param>
        /// <returns></returns>
        public override T Execute<T>(IDbCommand command)
        {
            if (typeof(T) != typeof(DataSet)) return base.Execute<T>(command);

            // 返回 DataSet
            List<DbRawCommand> myList = this.ParseCommand(command.CommandText, command.Parameters, command.CommandType);
            if (myList == null)
                return base.Execute<T>(command);
            else
                return (T)(object)this.Execute(myList);
        }

        DataSet Execute(List<DbRawCommand> sqlList)
        {
            int index = 0;
            var result = new DataSet();

            Func<IDbCommand, object> doExecute = cmd =>
            {
                DataTable table = base.Execute<DataTable>(cmd);
                table.TableName = string.Format("TALBE{0}", index);
                index += 1;
                result.Tables.Add(table);
                return null;
            };

            base.DoExecute<object>(sqlList, doExecute);
            return result;
        }

        // 拆分脚本命令
        List<DbRawCommand> ParseCommand(List<DbRawCommand> sqlList)
        {
            // 重新组合脚本，把 SELECT 和 UPDATE/DELETE 等分开来
            bool haveBegin = false;
            var myList = new List<DbRawCommand>();

            // 创建新命令
            Func<DbRawCommand, DbRawCommand> newCommand = cmd =>
            {
                var parameters = cmd.Parameters == null
                    ? null
                    : cmd.Parameters.ToList(a => (IDbDataParameter)_provider.CreateParameter(a.ParameterName, a.Value, a.DbType, a.Size, a.Precision, a.Scale, a.Direction));
                var result = new DbRawCommand(cmd.CommandText, parameters, cmd.CommandType);
                return result;
            };

            for (int i = 0; i < sqlList.Count; i++)
            {
                var cmd = sqlList[i];
                if (cmd == null) continue;

                string sql = cmd.CommandText;
                if (string.IsNullOrEmpty(sql)) continue;

                string methodName = string.Empty;
                if (sql.Length > 6) methodName = sql.Substring(0, 6).Trim().ToUpper();
                if (cmd is DbSelectCommand || methodName == "SELECT")
                {
                    // 查询单独执行
                    if (myList.Count > 0 && (i - 1) >= 0 && myList[myList.Count - 1] != null) myList.Add(null);

                    // 创建新命令
                    DbRawCommand @new = newCommand(cmd);
                    myList.Add(@new);
                    myList.Add(null);
                }
                else
                {
                    // 增删改
                    if (!haveBegin)
                    {
                        myList.Add(new DbRawCommand("BEGIN"));
                        haveBegin = true;
                    }

                    // 创建新命令
                    DbRawCommand @new = newCommand(cmd);
                    myList.Add(@new);

                    // 检查下一条是否是选择语句
                    bool isQuery = false;
                    if (i + 1 < sqlList.Count)
                    {
                        cmd = sqlList[i + 1];
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
                            myList.Add(new DbRawCommand("END;"));
                            haveBegin = false;
                            myList.Add(null);
                        }
                    }
                }

                if (haveBegin && i == sqlList.Count - 1) myList.Add(new DbRawCommand("END;"));
            }

            return myList;
        }

        // 拆分脚本命令
        List<DbRawCommand> ParseCommand(string commandText, IDataParameterCollection collection, CommandType? commandType)
        {
            // 存储过程不需要拆分
            if (commandType != null && commandType.Value == CommandType.StoredProcedure) return null;

            bool haveBegin = false;
            string methodName = string.Empty;
            var myList = new List<DbRawCommand>();
            string[] parts = commandText.Split(';');
            //var regex = new Regex(@"(?<ParameterName>:p\d+)", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            var regex = new Regex(@"(?<ParameterName>:[0-9a-zA-Z_]+)", RegexOptions.IgnoreCase | RegexOptions.Multiline);

            // 创建新命令
            Func<string, bool, DbRawCommand> newCommand = (sql, isQuery) =>
            {
                List<IDbDataParameter> parameters = null;
                if (collection != null && collection.Count > 0)
                {
                    parameters = new List<IDbDataParameter>();
                    MatchCollection matches = regex.Matches(sql);
                    foreach (Match m in matches)
                    {
                        var p = (IDbDataParameter)collection[m.Groups["ParameterName"].Value];
                        parameters.Add(p);
                    }
                }

                DbRawCommand cmd = new DbRawCommand(string.Format("{0}{1}", sql, isQuery ? string.Empty : ";"), parameters);
                return cmd;
            };

            // 只有一条 SQL 时也不用拆分
            if (parts.Length == 1) return null;

            // 语句块不拆分
            methodName = string.Empty;
            if (commandText.Length > 6) methodName = commandText.Substring(0, 6).Trim().ToUpper();
            if (methodName == "BEGIN") return null;

            for (int i = 0; i < parts.Length; i++)
            {
                string sql = parts[i];
                methodName = string.Empty;
                if (string.IsNullOrEmpty(sql)) continue;
                if (sql.Length > 6) methodName = sql.Substring(0, 6).Trim().ToUpper();

                if (methodName == "SELECT")
                {
                    // 查询单独执行
                    if (myList.Count > 0 && (i - 1) >= 0 && myList[myList.Count - 1] != null) myList.Add(null);

                    // 创建新命令
                    DbRawCommand cmd = newCommand(sql, true);
                    myList.Add(cmd);
                    myList.Add(null);
                }
                else
                {
                    // 增删改
                    if (!haveBegin)
                    {
                        myList.Add(new DbRawCommand("BEGIN"));
                        haveBegin = true;
                    }

                    // 创建新命令
                    DbRawCommand cmd = newCommand(sql, false);
                    myList.Add(cmd);

                    // 检查下一条是否是选择语句
                    bool isQuery = false;
                    if (i + 1 < parts.Length)
                    {
                        sql = parts[i + 1];
                        methodName = string.Empty;
                        if (!string.IsNullOrEmpty(sql) && sql.Length > 6) methodName = sql.Substring(0, 6).Trim().ToUpper();
                        isQuery = methodName == "SELECT";
                    }

                    // 如果下一条是SELECT 语句，则需要结束当前语句块
                    if (isQuery)
                    {
                        if (haveBegin)
                        {
                            myList.Add(new DbRawCommand("END;"));
                            haveBegin = false;
                            myList.Add(null);
                        }
                    }
                }

                if (haveBegin && i == parts.Length - 1) myList.Add(new DbRawCommand("END;"));
            }

            return myList;
        }
    }
}
