
using System;
using System.Data;

using System.Linq.Expressions;
using System.Collections.Generic;
using Oracle.ManagedDataAccess.Client;
using System.Data.Common;

namespace TZM.XFramework.Data.SqlClient
{
    /// <summary>
    /// 数据查询提供者
    /// </summary>
    public sealed class OracleDbQueryProvider : DbQueryProvider
    {
        /// <summary>
        /// 查询语义提供者 单例
        /// </summary>
        public static OracleDbQueryProvider Instance = new OracleDbQueryProvider();

        /// <summary>
        /// 数据源类的提供程序实现的实例
        /// </summary>
        public override DbProviderFactory DbProviderFactory { get { return OracleClientFactory.Instance; } }

        /// <summary>
        /// 数据库安全字符 左
        /// </summary>
        public override string QuotePrefix
        {
            get
            {
                return "\"";
            }
        }

        /// <summary>
        /// 数据库安全字符 右
        /// </summary>
        public override string QuoteSuffix
        {
            get
            {
                return "\"";
            }
        }

        /// <summary>
        /// 字符串引号
        /// </summary>
        public override string SingleQuoteChar
        {
            get
            {
                return "'";
            }
        }

        /// <summary>
        /// 数据查询提供者 名称
        /// </summary>
        public override string ProviderName
        {
            get
            {
                return "Oracle";
            }
        }

        /// <summary>
        /// 命令参数前缀
        /// </summary>
        public override string ParameterPrefix
        {
            get
            {
                return ":";
            }
        }

        /// <summary>
        /// 实例化 <see cref="OracleDbQueryProvider"/> 类的新实例
        /// </summary>
        private OracleDbQueryProvider() 
            : base()
        {

        }

        /// <summary>
        /// 创建数据会话
        /// </summary>
        /// <param name="connString">数据库连接字符串</param>
        /// <param name="commandTimeout">执行命令超时时间</param>
        /// <returns></returns>
        public override IDatabase CreateDbSession(string connString, int? commandTimeout)
        {
            return new OracleDatabase(this.DbProviderFactory, connString)
            {
                CommandTimeout = commandTimeout
            };
        }

        /// <summary>
        /// 创建 SQL 构造器
        /// </summary>
        /// <returns></returns>
        public override ISqlBuilder CreateSqlBuilder(List<IDbDataParameter> parameters = null)
        {
            return new OracleSqlBuilder(this, parameters);
        }

        /// <summary>
        /// 创建方法表达式访问器
        /// </summary>
        /// <returns></returns>
        public override IMethodCallExressionVisitor CreateMethodCallVisitor(ExpressionVisitorBase visitor)
        {
            return new OracleMethodCallExressionVisitor(this, visitor);
        }

        /// <summary>
        /// 解析 SQL 命令
        /// <para>
        /// 返回的已经解析语义中执行批次用 null 分开
        /// </para>
        /// </summary>
        /// <param name="dbQueryables">查询语句</param>
        /// <returns></returns>
        public override List<DbCommandDefinition> Resolve(List<object> dbQueryables)
        {
            List<DbCommandDefinition> sqlList = new List<DbCommandDefinition>();
            List<IDbDataParameter> parameters = null;
            bool haveBegin = false;

            for (int i = 0; i < dbQueryables.Count; i++)
            {
                object obj = dbQueryables[i];
                if (obj == null) continue;

                if (obj is IDbQueryable)
                {
                    IDbQueryable dbQueryable = (IDbQueryable)obj;
                    dbQueryable.Parameterized = true;
                    if (parameters == null) parameters = new List<IDbDataParameter>(8);

                    var cmd2 = dbQueryable.Resolve(0, true, parameters);
                    if (cmd2 is DbCommandDefinition_Select)
                    {
                        if (sqlList.Count > 0 && (i - 1) >= 0 && sqlList[sqlList.Count - 1] != null) sqlList.Add(null);

                        sqlList.Add(cmd2);
                        sqlList.Add(null);
                        parameters = new List<IDbDataParameter>(8);
                    }
                    else
                    {
                        if (!haveBegin)
                        {
                            sqlList.Add(new DbCommandDefinition("BEGIN"));
                            haveBegin = true;
                        }
                        sqlList.Add(cmd2);
                        if (cmd2.Parameters != null && cmd2.Parameters.Count > 1000)
                        {
                            // 1000个参数，就要重新分批
                            sqlList.Add(new DbCommandDefinition("END;"));
                            sqlList.Add(null);
                            parameters = new List<IDbDataParameter>(8);
                            haveBegin = false;
                        }

                        if (i + 1 < dbQueryables.Count && (dbQueryables[i + 1] is IDbQueryable))
                        {
                            var queryInfo = ((IDbQueryable)dbQueryables[i + 1]).Parse();
                            if (queryInfo is IDbQueryableInfo_Select)
                            {
                                if (haveBegin)
                                {
                                    sqlList.Add(new DbCommandDefinition("END;"));
                                    haveBegin = false;
                                }
                                parameters = new List<IDbDataParameter>(8);
                            }
                        }
                        else if (i + 1 < dbQueryables.Count && (dbQueryables[i + 1] is string))
                        {
                            string sql = obj
                                .ToString()
                                .TrimStart();
                            string method = string.Empty;
                            if (sql.Length > 5) method = sql.Substring(0, 6);
                            method = method.ToUpper();
                            if (!(method == "INSERT" || method == "UPDATE" || method == "DELETE"))
                            {
                                if (haveBegin)
                                {
                                    sqlList.Add(new DbCommandDefinition("END;"));
                                    haveBegin = false;
                                }
                                parameters = new List<IDbDataParameter>(8);
                            }
                        }
                    }

                }
                else if (obj is string)
                {
                    // 除了 INSERT DELETE UPDATE 语句外，其它的都单独执行
                    string sql = obj
                        .ToString()
                        .TrimStart();
                    string method = string.Empty;
                    if (sql.Length > 5) method = sql.Substring(0, 6);
                    method = method.ToUpper();
                    if (method == "INSERT" || method == "UPDATE" || method == "DELETE")
                    {
                        if (!haveBegin)
                        {
                            sqlList.Add(new DbCommandDefinition("BEGIN"));
                            haveBegin = true;
                        }
                        sqlList.Add(new DbCommandDefinition(sql));
                    }
                    else
                    {
                        if (sqlList.Count > 0 && (i - 1) >= 0 && sqlList[sqlList.Count - 1] != null) sqlList.Add(null);
                        sqlList.Add(new DbCommandDefinition(sql));
                        sqlList.Add(null);
                    }

                }
                else
                {
                    if (!haveBegin)
                    {
                        sqlList.Add(new DbCommandDefinition("BEGIN"));
                        haveBegin = true;
                    }
                    // 解析批量插入操作
                    List<IDbQueryable> bulkList = obj as List<IDbQueryable>;
                    if (bulkList != null && bulkList.Count > 0) this.ResolveBulk(sqlList, bulkList);
                }

                if (haveBegin && i == dbQueryables.Count - 1) sqlList.Add(new DbCommandDefinition("END;"));
            }

            return sqlList;
        }

        // 创建 SELECT 命令
        protected override DbCommandDefinition ParseSelectCommand<T>(DbQueryableInfo_Select<T> sQuery, int indent = 0, bool isOuter = true, List<IDbDataParameter> parameters = null)
        {
            // 说明：
            // 1.OFFSET 前必须要有 'ORDER BY'，即 'Skip' 子句前必须使用 'OrderBy' 子句
            // 2.在有统计函数的<MAX,MIN...>情况下，如果有 'Distinct' 'GroupBy' 'Skip' 'Take' 子句，则需要使用嵌套查询
            // 3.'Any' 子句将翻译成 IF EXISTS...
            // 4.分组再分页时需要使用嵌套查询，此时子查询不需要 'OrderBy' 子句，但最外层则需要
            // 5.'Skip' 'Take' 子句视为语义结束符，在其之后的子句将使用嵌套查询
            // 6.导航属性中有 1:n 关系的，需要使用嵌套查询，否则分页查询会有问题
            // 8.如果有分页，则使用嵌套
            // 9.如果有分页还有OrderBy，则使用嵌套的嵌套

            // 导航属性中有1:n关系，只统计主表
            // 例：AccountList = a.Client.AccountList,
            DbQueryableInfo_Select<T> innerQuery = sQuery.SubQueryInfo as DbQueryableInfo_Select<T>;
            if (sQuery.HaveListNavigation && innerQuery != null && innerQuery.Statis != null) sQuery = innerQuery;

            bool useNesting = sQuery.HaveDistinct || sQuery.GroupBy != null || sQuery.Skip > 0 || sQuery.Take > 0;
            bool useStatis = sQuery.Statis != null;
            // 没有统计函数或者使用 'Skip' 子句，则解析OrderBy
            // 导航属性如果使用嵌套，除非有 TOP 或者 OFFSET 子句，否则不能用ORDER BY
            bool useOrderBy = (!useStatis || sQuery.Skip > 0) && !sQuery.HaveAny && (!sQuery.GenByListNavigation || (sQuery.Skip > 0 || sQuery.Take > 0));

            IDbQueryable dbQueryable = sQuery.SourceQuery;
            TableAliasCache aliases = this.PrepareAlias<T>(sQuery);
            DbCommandDefinition_Select cmd = new DbCommandDefinition_Select(this, aliases, parameters) { HaveListNavigation = sQuery.HaveListNavigation };
            ISqlBuilder jf = cmd.JoinFragment;
            ISqlBuilder wf = cmd.WhereFragment;
            (jf as OracleSqlBuilder).IsOuter = isOuter;

            jf.Indent = indent;

            #region 嵌套查询

            if (useStatis && useNesting)
            {
                // SELECT
                jf.Append("SELECT ");
                jf.AppendNewLine();

                // SELECT COUNT(1)
                var visitor2 = new StatisExpressionVisitor(this, aliases, sQuery.Statis, sQuery.GroupBy, "t0");
                visitor2.Write(jf);
                cmd.AddNavMembers(visitor2.NavMembers);

                // SELECT COUNT(1) FROM
                jf.AppendNewLine();
                jf.Append("FROM ( ");

                indent += 1;
                jf.Indent = indent;
                (jf as OracleSqlBuilder).IsOuter = false;
            }

            #endregion

            #region 选择子句

            // SELECT 子句
            if (jf.Indent > 0) jf.AppendNewLine();
            jf.Append("SELECT ");

            if (sQuery.HaveAny)
            {
                jf.Append("CASE WHEN COUNT(1) = 1 THEN 1 ELSE 0 END FROM (");
                indent += 1;
                jf.Indent = indent;
                jf.AppendNewLine();
                jf.Append("SELECT 1 ");
            }

            if (useStatis && !useNesting)
            {
                // 如果有统计函数，并且不是嵌套的话，则直接使用SELECT <MAX,MIN...>，不需要解析选择的字段
                jf.AppendNewLine();
                var visitor2 = new StatisExpressionVisitor(this, aliases, sQuery.Statis, sQuery.GroupBy);
                visitor2.Write(jf);
                cmd.AddNavMembers(visitor2.NavMembers);
            }
            else
            {
                // DISTINCT 子句
                if (sQuery.HaveDistinct) jf.Append("DISTINCT ");

                #region 选择字段

                if (!sQuery.HaveAny)
                {
                    // SELECT 范围
                    ISqlBuilder sf = this.CreateSqlBuilder(jf.Parameters);
                    sf.Indent = jf.Indent + ((sQuery.Skip > 0 || sQuery.Take > 0) ? 2 : 0);
                    (sf as OracleSqlBuilder).IsOuter = (sQuery.Skip > 0 || sQuery.Take > 0) ? false : (jf as OracleSqlBuilder).IsOuter;

                    var visitor2 = new ColumnExpressionVisitor(this, aliases, sQuery);
                    visitor2.Write(sf);

                    cmd.Columns = visitor2.Columns;
                    cmd.Navigations = visitor2.Navigations;
                    cmd.AddNavMembers(visitor2.NavMembers);

                    // 分页，产生两层嵌套
                    if (sQuery.Skip > 0 || sQuery.Take > 0)
                    {
                        // 第一层嵌套
                        int index = 0;
                        jf.AppendNewLine();
                        foreach (var entry in cmd.Columns)
                        {
                            jf.AppendMember("t0", entry.Key);
                            jf.AppendAs(entry.Key);
                            index += 1;
                            if (index < cmd.Columns.Count)
                            {
                                jf.Append(',');
                                jf.AppendNewLine();
                            }
                        }

                        jf.AppendNewLine();
                        jf.Append("FROM(");

                        // 第二层嵌套
                        indent += 1;
                        isOuter = false;
                        jf.Indent = indent;
                        (jf as OracleSqlBuilder).IsOuter = isOuter;
                        jf.AppendNewLine();

                        jf.Append("SELECT");
                        jf.AppendNewLine();

                        foreach (var entry in cmd.Columns)
                        {
                            jf.AppendMember("t0", entry.Key);
                            jf.AppendAs(entry.Key);
                            jf.Append(',');
                            jf.AppendNewLine();
                        }
                        jf.Append("ROWNUM AS Row_Number0");
                        jf.AppendNewLine();
                        jf.Append("FROM(");

                        // 第三层嵌套
                        indent += 1;
                        jf.Indent = indent;
                        jf.AppendNewLine();
                        jf.Append("SELECT");
                    }

                    jf.Append(sf);
                }

                #endregion
            }

            #endregion

            #region 顺序解析

            // FROM 子句
            jf.AppendNewLine();
            jf.Append("FROM ");
            if (sQuery.SubQueryInfo != null)
            {
                // 子查询
                jf.Append('(');
                DbCommandDefinition cmd2 = this.ParseSelectCommand<T>(sQuery.SubQueryInfo as DbQueryableInfo_Select<T>, indent + 1, false, jf.Parameters);
                jf.Append(cmd2.CommandText);
                jf.AppendNewLine();
                jf.Append(") t0 ");
            }
            else
            {
                var typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo(sQuery.FromType);
                jf.AppendMember(typeRuntime.TableName, !typeRuntime.IsTemporary);
                jf.Append(" t0 ");
                //if (dbQueryable.DbContext.NoLock && !string.IsNullOrEmpty(this.WidthNoLock)) jf.Append(this.WidthNoLock);
            }

            // LEFT<INNER> JOIN 子句
            ExpressionVisitorBase visitor = new JoinExpressionVisitor(this, aliases, sQuery.Join);
            visitor.Write(jf);

            wf.Indent = jf.Indent;

            // WHERE 子句
            visitor = new WhereExpressionVisitor(this, aliases, sQuery.Where);
            visitor.Write(wf);
            cmd.AddNavMembers(visitor.NavMembers);

            // GROUP BY 子句
            visitor = new GroupByExpressionVisitor(this, aliases, sQuery.GroupBy);
            visitor.Write(wf);
            cmd.AddNavMembers(visitor.NavMembers);

            // HAVING 子句
            visitor = new HavingExpressionVisitor(this, aliases, sQuery.Having, sQuery.GroupBy);
            visitor.Write(wf);
            cmd.AddNavMembers(visitor.NavMembers);

            // ORDER 子句
            if (sQuery.OrderBy.Count > 0 && useOrderBy)
            {
                visitor = new OrderByExpressionVisitor(this, aliases, sQuery.OrderBy, sQuery.GroupBy);
                visitor.Write(wf);
                cmd.AddNavMembers(visitor.NavMembers);
            }

            #endregion

            #region 嵌套查询

            if (useStatis && useNesting)
            {
                cmd.Convergence();
                indent -= 1;
                jf.Indent = indent;
                jf.AppendNewLine();
                jf.Append(" ) t0");
            }

            #endregion

            #region 嵌套导航

            // TODO Include 从表，没分页，OrderBy 报错
            if (sQuery.HaveListNavigation && innerQuery != null && innerQuery.OrderBy.Count > 0 && innerQuery.Statis == null && !(innerQuery.Skip > 0 || innerQuery.Take > 0))
            {
                // OrderBy("a.CloudServer.CloudServerName");
                cmd.Convergence();
                visitor = new OrderByExpressionVisitor(this, aliases, innerQuery.OrderBy);//, null, "t0");
                visitor.Write(jf);
            }

            #endregion

            #region 并集查询

            // UNION 子句
            if (sQuery.Union != null && sQuery.Union.Count > 0)
            {
                cmd.Convergence();
                for (int index = 0; index < sQuery.Union.Count; index++)
                {
                    jf.AppendNewLine();
                    jf.Append("UNION ALL");
                    if (indent == 0) jf.AppendNewLine();
                    DbCommandDefinition cmd2 = this.ParseSelectCommand<T>(sQuery.Union[index] as DbQueryableInfo_Select<T>, indent, isOuter, jf.Parameters);
                    jf.Append(cmd2.CommandText);
                }
            }

            #endregion

            #region 分页查询

            if (sQuery.Take > 0 || sQuery.Skip > 0)
            {
                // 合并 WHERE
                cmd.Convergence();

                indent -= 1;
                jf.Indent = indent;
                jf.AppendNewLine();
                jf.Append(") t0");

                indent -= 1;
                jf.Indent = indent;
                jf.AppendNewLine();
                jf.Append(") t0");

                jf.AppendNewLine();
                jf.Append("WHERE ");
                if (sQuery.Skip > 0)
                {
                    jf.Append("t0.Row_Number0 > ");
                    jf.Append(jf.GetSqlValue(sQuery.Skip));
                }
                if (sQuery.Take > 0)
                {
                    if (sQuery.Skip > 0) jf.Append(" AND ");
                    jf.Append("t0.Row_Number0 <= ");
                    jf.Append(jf.GetSqlValue((sQuery.Skip + sQuery.Take)));
                }
            }

            #endregion

            #region Any 子句

            // 'Any' 子句
            if (sQuery.HaveAny)
            {
                // 产生 WHERE 子句
                cmd.Convergence();
                // 如果没有分页，则显式指定只查一笔记录
                if (sQuery.Take == 0 && sQuery.Skip == 0)
                {
                    if (sQuery.Where != null && sQuery.Where.Expressions != null) jf.Append(" AND ROWNUM <= 1");
                    else
                    {
                        jf.AppendNewLine();
                        jf.Append("WHERE  ROWNUM <= 1");
                    }
                }

                indent -= 1;
                jf.Indent = indent;
                jf.AppendNewLine();
                jf.Append(") t0");
            }

            #endregion

            return cmd;
        }

        // 创建 INSRT 命令
        protected override DbCommandDefinition ParseInsertCommand<T>(DbQueryableInfo_Insert<T> nQuery, List<IDbDataParameter> parameters = null)
        {
            ISqlBuilder builder = this.CreateSqlBuilder(parameters);
            TypeRuntimeInfo typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo<T>();
            TableAliasCache aliases = new TableAliasCache();
            bool useSEQ = false;

            if (nQuery.Entity != null)
            {
                // 如果没有Sequence列，使用 INSERT ALL INTO 语法，否则就一条一条逐行写入~~
                // 批量 INSERT，自增列不会自动赋值 

                object entity = nQuery.Entity;
                ISqlBuilder columnsBuilder = this.CreateSqlBuilder(builder.Parameters);
                ISqlBuilder valuesBuilder = this.CreateSqlBuilder(builder.Parameters);

                // 指定插入列
                Dictionary<string, MemberInvokerBase> invokers = typeRuntime.Invokers;
                if (nQuery.EntityColumns != null && nQuery.EntityColumns.Count > 0)
                {
                    invokers = new Dictionary<string, MemberInvokerBase>();
                    for (int i = 0; i < nQuery.EntityColumns.Count; i++)
                    {
                        Expression curExpr = nQuery.EntityColumns[i];
                        if (curExpr.NodeType == ExpressionType.Lambda) curExpr = (curExpr as LambdaExpression).Body.ReduceUnary();
                        if (curExpr.NodeType != ExpressionType.MemberAccess)
                            throw new XFrameworkException("Can't read field name from expression {0}", nQuery.EntityColumns[i]);

                        MemberExpression member = curExpr as MemberExpression;
                        string name = member.Member.Name;
                        invokers[name] = typeRuntime.Invokers[name];
                    }
                }

                // 自增列参数
                IDbDataParameter seqParameter = null;
                // 自增列标记
                ColumnAttribute seqColumn = null;
                foreach (var kv in invokers)
                {
                    MemberInvokerBase invoker = kv.Value;
                    var column = invoker.Column;
                    if (column != null && column.NoMapped) continue;
                    if (invoker.ForeignKey != null) continue;
                    if (invoker.Member.MemberType == System.Reflection.MemberTypes.Method) continue;

                    columnsBuilder.AppendMember(invoker.Member.Name);
                    columnsBuilder.Append(',');

                    if (invoker == nQuery.AutoIncrement)
                    {
                        seqColumn = column;
                        if (nQuery.Bulk == null)
                        {
                            // 非批量INSERT，产生一个 OUTPUT 类型的参数
                            string pName = string.Format("{0}p{1}", this.ParameterPrefix, builder.Parameters.Count);
                            var database = nQuery.SourceQuery.DbContext.Database;
                            seqParameter = database.CreateParameter(pName, -1, direction: ParameterDirection.Output);
                            builder.Parameters.Add(seqParameter);
                            valuesBuilder.Append(seqParameter.ParameterName);
                            valuesBuilder.Append(',');
                        }
                        else
                        {
                            valuesBuilder.Append(((OracleColumnAttribute)column).SEQName);
                            valuesBuilder.Append(".NEXTVAL");
                            valuesBuilder.Append(',');
                        }
                    }
                    else
                    {
                        var value = invoker.Invoke(entity);
                        string seg = builder.GetSqlValueWidthDefault(value, column);
                        valuesBuilder.Append(seg);
                        valuesBuilder.Append(',');
                    }
                }
                columnsBuilder.Length -= 1;
                valuesBuilder.Length -= 1;

                if (nQuery.Bulk == null)
                {
                    // 非批量INSERT，产生一个 OUTPUT 类型的参数
                    if (seqParameter != null)
                    {
                        seqParameter.Direction = ParameterDirection.Output;
                        seqParameter.DbType = DbType.Int64;
                        builder.Append("SELECT ");
                        builder.Append(((OracleColumnAttribute)seqColumn).SEQName);
                        builder.Append(".NEXTVAL INTO ");
                        builder.Append(seqParameter.ParameterName);
                        builder.Append(" FROM DUAL;");
                        builder.AppendNewLine();
                        useSEQ = true;
                    }
                    builder.Append("INSERT ");
                }
                else
                {
                    // 批量 INSERT
                    if (!nQuery.Bulk.OnlyValue || seqColumn != null) builder.Append("INSERT ");
                    // 如果有自增列则不使用 INSERT ALL INTO 语法
                    if (!nQuery.Bulk.OnlyValue && seqColumn == null) builder.Append("ALL ");
                }

                builder.Append("INTO ");
                builder.AppendMember(typeRuntime.TableName, !typeRuntime.IsTemporary);
                builder.Append('(');
                builder.Append(columnsBuilder);
                builder.Append(')');
                builder.AppendNewLine();
                builder.AppendNewTab();
                builder.Append("VALUES");
                builder.Append('(');
                builder.Append(valuesBuilder);
                builder.Append(')');

                if (nQuery.Bulk == null) builder.Append(';');
                else
                {
                    if (seqColumn != null)
                    {
                        if (nQuery.Bulk.IsEndPos) builder.Append(";");
                        else builder.AppendNewLine(";");
                    }
                    else
                    {
                        builder.AppendNewLine();
                        if (nQuery.Bulk.IsEndPos) builder.Append("SELECT 1 FROM DUAL;");
                    }
                }
            }
            else if (nQuery.SelectInfo != null)
            {
                builder.Append("INSERT INTO ");
                builder.AppendMember(typeRuntime.TableName, !typeRuntime.IsTemporary);
                builder.Append('(');

                int i = 0;
                DbCommandDefinition_Select cmd2 = this.ParseSelectCommand(nQuery.SelectInfo, 0, false, builder.Parameters) as DbCommandDefinition_Select;
                foreach (var kvp in cmd2.Columns)
                {
                    builder.AppendMember(kvp.Key);
                    if (i < cmd2.Columns.Count - 1) builder.Append(',');
                    i++;
                }

                builder.Append(')');
                builder.AppendNewLine();
                builder.Append(cmd2.CommandText);
                builder.Append(';');
            }

            var cmd = new OracleDbCommandDefinition_Insert(builder.ToString(), builder.Parameters, System.Data.CommandType.Text);
            cmd.HaveSEQ = useSEQ;
            return cmd;
        }

        // 创建 DELETE 命令
        protected override DbCommandDefinition ParseDeleteCommand<T>(DbQueryableInfo_Delete<T> dQuery, List<IDbDataParameter> parameters = null)
        {
            TypeRuntimeInfo typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo<T>();
            ISqlBuilder builder = this.CreateSqlBuilder(parameters);
            bool useKey = false;

            builder.Append("DELETE FROM ");
            builder.AppendMember(typeRuntime.TableName, !typeRuntime.IsTemporary);
            builder.Append(" t0 ");

            if (dQuery.Entity != null)
            {
                object entity = dQuery.Entity;

                builder.AppendNewLine();
                builder.Append("WHERE ");

                foreach (var kv in typeRuntime.Invokers)
                {
                    MemberInvokerBase invoker = kv.Value;
                    var column = invoker.Column;

                    if (column != null && column.IsKey)
                    {
                        useKey = true;
                        var value = invoker.Invoke(entity);
                        var seg = builder.GetSqlValue(value, column);
                        builder.AppendMember("t0", invoker.Member.Name);
                        builder.Append(" = ");
                        builder.Append(seg);
                        builder.Append(" AND ");
                    };
                }
                builder.Length -= 5;

                if (!useKey) throw new XFrameworkException("Delete<T>(T value) require T must have key column.");
            }
            else if (dQuery.SelectInfo != null)
            {
                TableAliasCache aliases = this.PrepareAlias<T>(dQuery.SelectInfo);
                var cmd2 = new OracleDbCommandDefinition_Delete(this, aliases, builder.Parameters);
                cmd2.HaveListNavigation = dQuery.SelectInfo.HaveListNavigation;

                var visitor0 = new OracleJoinExpressionVisitor(this, aliases, dQuery.SelectInfo.Join, dQuery.SelectInfo.Where);
                visitor0.Write(cmd2);

                var visitor1 = new OracleWhereExpressionVisitor(this, aliases, dQuery.SelectInfo.Where);
                visitor1.Write(cmd2.WhereFragment);
                cmd2.AddNavMembers(visitor1.NavMembers);
                builder.Append(cmd2.CommandText);
            }

            builder.Append(';');
            return new DbCommandDefinition(builder.ToString(), builder.Parameters, System.Data.CommandType.Text);
        }

        // 创建 UPDATE 命令
        protected override DbCommandDefinition ParseUpdateCommand<T>(DbQueryableInfo_Update<T> uQuery, List<IDbDataParameter> parameters = null)
        {
            ISqlBuilder builder = this.CreateSqlBuilder(parameters);
            var typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo<T>();

            builder.Append("UPDATE ");
            builder.AppendMember(typeRuntime.TableName, !typeRuntime.IsTemporary);
            builder.Append(" t0 SET");
            builder.AppendNewLine();

            if (uQuery.Entity != null)
            {
                object entity = uQuery.Entity;
                ISqlBuilder whereBuilder = this.CreateSqlBuilder(builder.Parameters);
                bool useKey = false;
                int length = 0;


                foreach (var kv in typeRuntime.Invokers)
                {
                    MemberInvokerBase invoker = kv.Value;
                    var column = invoker.Column;
                    if (column != null && column.IsIdentity) goto gotoLabel; // fix issue# 自增列同时又是主键
                    if (column != null && column.NoMapped) continue;
                    if (invoker.ForeignKey != null) continue;
                    if (invoker.Member.MemberType == System.Reflection.MemberTypes.Method) continue;

                    builder.AppendMember(invoker.Member.Name);
                    builder.Append(" = ");

                gotoLabel:

                    if (column == null || !column.IsIdentity)
                    {
                        var value = invoker.Invoke(entity);
                        var seg = builder.GetSqlValueWidthDefault(value, column);

                        builder.Append(seg);
                        length = builder.Length;
                        builder.Append(',');
                        builder.AppendNewLine();
                    }

                    if (column != null && column.IsKey) useKey = true;
                }

                if (!useKey) throw new XFrameworkException("Update<T>(T value) require T must have key column.");


                // ORACLE 需要注意参数顺序问题 
                int index = -1;
                foreach (var kv in typeRuntime.KeyInvokers)
                {
                    MemberInvokerBase invoker = kv.Value;
                    var column = invoker.Column;
                    var value = invoker.Invoke(entity);
                    var seg = builder.GetSqlValueWidthDefault(value, column);
                    index += 1;

                    whereBuilder.AppendMember(invoker.Member.Name);
                    whereBuilder.Append(" = ");
                    whereBuilder.Append(seg);
                    if (index < typeRuntime.KeyInvokers.Count - 1) whereBuilder.Append(" AND ");
                }

                builder.Length = length;
                builder.AppendNewLine();
                builder.Append("WHERE ");
                builder.Append(whereBuilder);

            }
            else if (uQuery.Expression != null)
            {
                TableAliasCache aliases = this.PrepareAlias<T>(uQuery.SelectInfo);
                ExpressionVisitorBase visitor = null;
                visitor = new OracleUpdateExpressionVisitor(this, aliases, uQuery.Expression);
                visitor.Write(builder);

                var cmd2 = new OracleDbCommandDefinition_Delete(this, aliases, builder.Parameters);
                cmd2.HaveListNavigation = uQuery.SelectInfo.HaveListNavigation;

                var visitor0 = new OracleJoinExpressionVisitor(this, aliases, uQuery.SelectInfo.Join, uQuery.SelectInfo.Where);
                visitor0.Write(cmd2);

                var visitor1 = new OracleWhereExpressionVisitor(this, aliases, uQuery.SelectInfo.Where);
                visitor1.Write(cmd2.WhereFragment);
                cmd2.AddNavMembers(visitor1.NavMembers);
                builder.Append(cmd2.CommandText);
            }

            builder.Append(';');
            return new DbCommandDefinition(builder.ToString(), builder.Parameters, System.Data.CommandType.Text);
        }
    }
}

//说明：
//1.第一句用的是insert all into 不是 insert into
//2.最后跟的selecr 1 from dual语句中的dual表可以被替换为任何一个只要不是tb_red的表
//3.和mysql的写法不一样，多个values之间不用逗号分隔，但是需要加into tablename的形式的语句在每个values前面
//4.只适合于Oralce 9i以上版本
//5.例：
//INSERT ALL INTO tb_red VALUES(1000, 8001, '2016-10-10 10:59:59', 1, 8001, '测试用户1000', '红名单0', '男', '膜法学院', '被测')  
//INTO tb_red  VALUES (1001, 8001, '2016-10-10 11:00:00', 2, 8001, '测试用户1001', '红名单1', '男', '膜法学院', '被测')  
//INTO tb_red  VALUES (1002, 8001, '2016-10-10 11:00:01', 0, 8001, '测试用户1002', '红名单2', '男', '膜法学院', '被测')  
//INTO tb_red  VALUES (1003, 8001, '2016-10-11 10:59:59', 1, 8001, '测试用户1003', '红名单3', '男', '膜法学院', '被测')  
//INTO tb_red  VALUES (1004, 8001, '2016-10-11 11:00:00', 2, 8001, '测试用户1004', '红名单4', '男', '膜法学院', '被测')  
//INTO tb_red  VALUES (1005, 8001, '2016-10-11 11:00:01', 0, 8001, '测试用户1005', '红名单5', '男', '膜法学院', '被测')  
//select 1 from dual;
//--------------------- 
//INSERT INTO TIMEZONE_TEST SELECT TIMESTAMP '2016-04-24 15:14:00 +3:00',TIMESTAMP '2016-04-24 15:14:00 +3:00',TIMESTAMP '2016-04-24 15:14:00 +3:00' FROM DUAL;
//SQL>  insert into ff values(TO_TIMESTAMP_TZ('2006-12-14 19:45:09.9003 13:00', 'YYYY-MM-DD HH24:MI:SS.FF TZH:TZM'))

//insert into TIMESTAMP_TEST values(
//TO_TIMESTAMP_TZ('2010-12-01 23:12:56.788 -12:44', 'YYYY-MM-DD HH24:MI:SS.FF TZH:TZM'),
//TO_TIMESTAMP_TZ('2010-12-01 23:12:56.788-12:44', 'YYYY-MM-DD HH24:MI:SS.FF TZH:TZM'),
//TO_TIMESTAMP_TZ('2010-12-01 23:12:56.788 -12:44', 'YYYY-MM-DD HH24:MI:SS.FF TZH:TZM'),
//TO_TIMESTAMP_TZ('2010-12-0123:12:56.788 -12:44', 'YYYY-MM-DD HH24:MI:SS.FF TZH:TZM'));
//（tzh：时区中的小时，tzm:时区中的分）