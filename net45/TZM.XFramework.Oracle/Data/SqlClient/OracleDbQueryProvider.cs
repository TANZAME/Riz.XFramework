
using System;
using System.Data;
using System.Linq;
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
        static Expression<Func<OracleRowId, string>> _rowIdExpression = x => x.RowId;

        /// <summary>
        /// 查询语义提供者实例
        /// </summary>
        public static OracleDbQueryProvider Instance = new OracleDbQueryProvider();

        /// <summary>
        /// 数据源类的提供程序实现的实例
        /// </summary>
        public override DbProviderFactory DbProviderFactory { get { return OracleClientFactory.Instance; } }

        /// <summary>
        /// SQL字段值生成器
        /// </summary>
        public override DbValue DbValue { get { return OracleDbValue.Instance; } }

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
        /// 创建 SQL 构造器
        /// </summary>
        /// <param name="token">参数列表，NULL 或者 Parameters=NULL 时表示不使用参数化</param>
        /// <returns></returns>
        public override ISqlBuilder CreateSqlBuilder(ResolveToken token)
        {
            return new OracleSqlBuilder(this, token);
        }

        /// <summary>
        /// 创建方法表达式访问器
        /// </summary>
        /// <returns></returns>
        public override MethodCallExpressionVisitor CreateMethodVisitor(ExpressionVisitorBase visitor)
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
        public override List<Command> Resolve(List<object> dbQueryables)
        {
            bool haveBegin = false;
            ResolveToken token = null;
            List<Command> sqlList = new List<Command>();

            for (int i = 0; i < dbQueryables.Count; i++)
            {
                object obj = dbQueryables[i];
                if (obj == null) continue;

                if (obj is IDbQueryable)
                {
                    IDbQueryable dbQueryable = (IDbQueryable)obj;
                    dbQueryable.Parameterized = true;
                    if (token == null) token = new ResolveToken();
                    if (token.Parameters == null) token.Parameters = new List<IDbDataParameter>(8);

                    var cmd2 = dbQueryable.Resolve(0, true, token);
                    if (cmd2 is MapperCommand)
                    {
                        // 查询单独执行
                        if (sqlList.Count > 0 && (i - 1) >= 0 && sqlList[sqlList.Count - 1] != null) sqlList.Add(null);

                        sqlList.Add(cmd2);
                        sqlList.Add(null);
                        token = new ResolveToken();
                        token.Parameters = new List<IDbDataParameter>(8);
                    }
                    else
                    {
                        // 增删改
                        if (!haveBegin)
                        {
                            sqlList.Add(new Command("BEGIN"));
                            haveBegin = true;
                        }
                        sqlList.Add(cmd2);
                        if (cmd2.Parameters != null && cmd2.Parameters.Count > 1000)
                        {
                            // 1000个参数，就要重新分批
                            if (haveBegin)
                            {
                                sqlList.Add(new Command("END;"));
                                haveBegin = false;
                                sqlList.Add(null);
                            }
                            token = new ResolveToken();
                            token.Parameters = new List<IDbDataParameter>(8);
                        }

                        if (i + 1 < dbQueryables.Count)
                        {
                            // 检查下一条是否是选择语句
                            bool isQuery = false;
                            if (dbQueryables[i + 1] is IDbQueryable)
                            {
                                var queryInfo = ((IDbQueryable)dbQueryables[i + 1]).Parse();
                                isQuery = queryInfo is IDbQueryableInfo_Select;
                            }
                            else if ((dbQueryables[i + 1] is string))
                            {
                                string sql = dbQueryables[i + 1].ToString();
                                string method = string.Empty;
                                if (sql.Length > 6) method = sql.Substring(0, 6).Trim().ToUpper();
                                isQuery = method == "SELECT";
                            }
                            else if (dbQueryables[i + 1] is RawSql)
                            {
                                string sql = ((RawSql)dbQueryables[i + 1]).CommandText;
                                string method = string.Empty;
                                if (sql.Length > 6) method = sql.Substring(0, 6).Trim().ToUpper();
                                isQuery = method == "SELECT";
                            }

                            // 如果下一条是SELECT 语句，则需要结束当前语句块
                            if (isQuery)
                            {
                                if (haveBegin)
                                {
                                    sqlList.Add(new Command("END;"));
                                    haveBegin = false;
                                    sqlList.Add(null);
                                }
                                token = new ResolveToken();
                                token.Parameters = new List<IDbDataParameter>(8);
                            }
                        }
                    }

                }
                else if (obj is RawSql || obj is string)
                {
                    string sql = string.Empty;
                    if (obj is string) sql = obj.ToString();
                    else
                    {
                        RawSql rawSql = (RawSql)obj;
                        // 解析参数
                        object[] args = null;
                        if (rawSql.Parameters != null)
                            args = rawSql.Parameters.Select(x => this.DbValue.GetSqlValue(x, token)).ToArray();
                        sql = rawSql.CommandText;
                        if (args != null && args.Length > 0) sql = string.Format(sql, args);
                    }


                    string methodName = string.Empty;
                    if (sql.Length > 6) methodName = sql.Substring(0, 6).Trim().ToUpper();
                    if (methodName == "SELECT")
                    {
                        if (sqlList.Count > 0 && (i - 1) >= 0 && sqlList[sqlList.Count - 1] != null) sqlList.Add(null);
                    }


                    var cmd2 = new Command(sql, token.Parameters, CommandType.Text);
                    sqlList.Add(cmd2);

                    if (methodName == "SELECT")
                    {
                        sqlList.Add(cmd2);
                        sqlList.Add(null);
                        token = new ResolveToken();
                        token.Parameters = new List<IDbDataParameter>(8);
                    }
                    else if (cmd2.Parameters != null && cmd2.Parameters.Count > 1000)
                    {
                        // 1000个参数，就要重新分批
                        if (haveBegin)
                        {
                            sqlList.Add(new Command("END;"));
                            sqlList.Add(null);
                            haveBegin = false;
                        }
                        token = new ResolveToken();
                        token.Parameters = new List<IDbDataParameter>(8);
                    }
                }
                else
                {
                    if (!haveBegin)
                    {
                        sqlList.Add(new Command("BEGIN"));
                        haveBegin = true;
                    }
                    // 解析批量插入操作
                    List<IDbQueryable> bulkList = obj as List<IDbQueryable>;
                    if (bulkList != null && bulkList.Count > 0) this.ResolveBulk(sqlList, bulkList);
                }

                if (haveBegin && i == dbQueryables.Count - 1) sqlList.Add(new Command("END;"));
            }

            return sqlList;
        }

        // 创建 SELECT 命令
        protected override Command ResolveSelectCommand<T>(DbQueryableInfo_Select<T> dbQuery, int indent, bool isOuter, ResolveToken token)
        {
            // 说明：
            // 1.OFFSET 前必须要有 'ORDER BY'，即 'Skip' 子句前必须使用 'OrderBy' 子句
            // 2.在有聚合函数的<MAX,MIN...>情况下，如果有 'Distinct' 'GroupBy' 'Skip' 'Take' 子句，则需要使用嵌套查询
            // 3.'Any' 子句将翻译成 IF EXISTS...
            // 4.分组再分页时需要使用嵌套查询，此时子查询不需要 'OrderBy' 子句，但最外层则需要
            // 5.'Skip' 'Take' 子句视为语义结束符，在其之后的子句将使用嵌套查询
            // 6.导航属性中有 1:n 关系的，需要使用嵌套查询，否则分页查询会有问题
            // 8.如果有分页，则使用嵌套
            // 9.如果有分页还有OrderBy，则使用嵌套的嵌套

            // 导航属性中有1:n关系，只统计主表
            // 例：AccountList = a.Client.AccountList,
            var subQuery = dbQuery.SubQueryInfo as DbQueryableInfo_Select<T>;
            if (dbQuery.HasMany && subQuery != null && subQuery.Aggregate != null) dbQuery = subQuery;

            bool useStatis = dbQuery.Aggregate != null;
            bool useNesting = dbQuery.HasDistinct || dbQuery.GroupBy != null || dbQuery.Skip > 0 || dbQuery.Take > 0;
            string alias0 = token != null && !string.IsNullOrEmpty(token.TableAliasName) ? (token.TableAliasName + "0") : "t0";
            // 没有聚合函数或者使用 'Skip' 子句，则解析OrderBy
            // 导航属性如果使用嵌套，除非有 TOP 或者 OFFSET 子句，否则不能用ORDER BY
            bool useOrderBy = (!useStatis || dbQuery.Skip > 0) && !dbQuery.HasAny && (!dbQuery.SubQueryOfMany || (dbQuery.Skip > 0 || dbQuery.Take > 0));

            TableAliasCache aliases = this.PrepareTableAlias<T>(dbQuery, token);
            MapperCommand cmd = new MapperCommand(this, aliases, token) { HasMany = dbQuery.HasMany };
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
                var visitor2 = new AggregateExpressionVisitor(this, aliases, dbQuery.Aggregate, dbQuery.GroupBy, alias0);
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

            if (dbQuery.HasAny)
            {
                jf.Append("CASE WHEN COUNT(1) = 1 THEN 1 ELSE 0 END FROM (");
                indent += 1;
                jf.Indent = indent;
                jf.AppendNewLine();
                jf.Append("SELECT 1 ");
            }

            if (useStatis && !useNesting)
            {
                // 如果有聚合函数，并且不是嵌套的话，则直接使用SELECT <MAX,MIN...>，不需要解析选择的字段
                jf.AppendNewLine();
                var visitor2 = new AggregateExpressionVisitor(this, aliases, dbQuery.Aggregate, dbQuery.GroupBy);
                visitor2.Write(jf);
                cmd.AddNavMembers(visitor2.NavMembers);
            }
            else
            {
                // DISTINCT 子句
                if (dbQuery.HasDistinct) jf.Append("DISTINCT ");

                #region 选择字段

                if (!dbQuery.HasAny)
                {
                    // SELECT 范围
                    ISqlBuilder sf = this.CreateSqlBuilder(token);
                    sf.Indent = jf.Indent + ((dbQuery.Skip > 0 || dbQuery.Take > 0) ? 2 : 0);
                    (sf as OracleSqlBuilder).IsOuter = (dbQuery.Skip > 0 || dbQuery.Take > 0) ? false : (jf as OracleSqlBuilder).IsOuter;

                    var visitor2 = new ColumnExpressionVisitor(this, aliases, dbQuery);
                    visitor2.Write(sf);

                    cmd.PickColumns = visitor2.PickColumns;
                    cmd.PickColumnText = visitor2.PickColumnText;
                    cmd.Navigations = visitor2.Navigations;
                    cmd.AddNavMembers(visitor2.NavMembers);

                    // 分页，产生两层嵌套
                    if (dbQuery.Skip > 0 || dbQuery.Take > 0)
                    {
                        // 第一层嵌套
                        int index = 0;
                        jf.AppendNewLine();
                        foreach (var column in cmd.PickColumns)
                        {
                            jf.AppendMember(alias0, column.Name);
                            jf.AppendAs(column.NewName);
                            index += 1;
                            if (index < cmd.PickColumns.Count)
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

                        foreach (var column in cmd.PickColumns)
                        {
                            jf.AppendMember(alias0, column.NewName);
                            jf.AppendAs(column.NewName);
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
            if (dbQuery.SubQueryInfo != null)
            {
                // 子查询
                jf.Append('(');
                Command cmd2 = this.ResolveSelectCommand<T>(dbQuery.SubQueryInfo as DbQueryableInfo_Select<T>, indent + 1, false, token);
                jf.Append(cmd2.CommandText);
                jf.AppendNewLine();
                jf.Append(") ");
                jf.Append(alias0);
                jf.Append(' ');
            }
            else
            {
                var typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo(dbQuery.FromEntityType);
                jf.AppendMember(typeRuntime.TableName, !typeRuntime.IsTemporary);
                jf.Append(' ');
                jf.Append(alias0);
                jf.Append(' ');
            }

            // LEFT<INNER> JOIN 子句
            ExpressionVisitorBase visitor = new JoinExpressionVisitor(this, aliases, dbQuery.Joins);
            visitor.Write(jf);

            wf.Indent = jf.Indent;

            // WHERE 子句
            visitor = new WhereExpressionVisitor(this, aliases, dbQuery.Condtion);
            visitor.Write(wf);
            cmd.AddNavMembers(visitor.NavMembers);

            // GROUP BY 子句
            visitor = new GroupByExpressionVisitor(this, aliases, dbQuery.GroupBy);
            visitor.Write(wf);
            cmd.AddNavMembers(visitor.NavMembers);

            // HAVING 子句
            visitor = new HavingExpressionVisitor(this, aliases, dbQuery.Having, dbQuery.GroupBy);
            visitor.Write(wf);
            cmd.AddNavMembers(visitor.NavMembers);

            // ORDER 子句
            if (dbQuery.OrderBys.Count > 0 && useOrderBy)
            {
                visitor = new OrderByExpressionVisitor(this, aliases, dbQuery.OrderBys, dbQuery.GroupBy);
                visitor.Write(wf);
                cmd.AddNavMembers(visitor.NavMembers);
            }

            #endregion

            #region 嵌套查询

            if (useStatis && useNesting)
            {
                cmd.CombineFragments();
                indent -= 1;
                jf.Indent = indent;
                jf.AppendNewLine();
                jf.Append(") ");
                jf.Append(alias0);
            }

            #endregion

            #region 嵌套导航

            // TODO Include 从表，没分页，OrderBy 报错
            if (dbQuery.HasMany && subQuery != null && subQuery.OrderBys.Count > 0 && subQuery.Aggregate == null && !(subQuery.Skip > 0 || subQuery.Take > 0))
            {
                // OrderBy("a.CloudServer.CloudServerName");
                cmd.CombineFragments();
                visitor = new OrderByExpressionVisitor(this, aliases, subQuery.OrderBys);//, null, "t0");
                visitor.Write(jf);
            }

            #endregion

            #region 并集查询

            // UNION 子句
            if (dbQuery.Unions != null && dbQuery.Unions.Count > 0)
            {
                cmd.CombineFragments();
                for (int index = 0; index < dbQuery.Unions.Count; index++)
                {
                    jf.AppendNewLine();
                    jf.Append("UNION ALL");
                    if (indent == 0) jf.AppendNewLine();
                    Command cmd2 = this.ResolveSelectCommand<T>(dbQuery.Unions[index] as DbQueryableInfo_Select<T>, indent, isOuter, token);
                    jf.Append(cmd2.CommandText);
                }
            }

            #endregion

            #region 分页查询

            if (dbQuery.Take > 0 || dbQuery.Skip > 0)
            {
                // 合并 WHERE
                cmd.CombineFragments();

                indent -= 1;
                jf.Indent = indent;
                jf.AppendNewLine();
                jf.Append(") ");
                jf.Append(alias0);

                indent -= 1;
                jf.Indent = indent;
                jf.AppendNewLine();
                jf.Append(") ");
                jf.Append(alias0);

                jf.AppendNewLine();
                jf.Append("WHERE ");
                if (dbQuery.Skip > 0)
                {
                    jf.AppendMember(alias0, "Row_Number0");
                    jf.Append(" > ");
                    jf.Append(this.DbValue.GetSqlValue(dbQuery.Skip, token));
                }
                if (dbQuery.Take > 0)
                {
                    if (dbQuery.Skip > 0) jf.Append(" AND ");
                    jf.AppendMember(alias0, "Row_Number0");
                    jf.Append(" <= ");
                    jf.Append(this.DbValue.GetSqlValue((dbQuery.Skip + dbQuery.Take), token));
                }
            }

            #endregion

            #region Any 子句

            // 'Any' 子句
            if (dbQuery.HasAny)
            {
                // 产生 WHERE 子句
                cmd.CombineFragments();
                // 如果没有分页，则显式指定只查一笔记录
                if (dbQuery.Take == 0 && dbQuery.Skip == 0)
                {
                    if (dbQuery.Condtion != null && dbQuery.Condtion.Expressions != null) jf.Append(" AND ROWNUM <= 1");
                    else
                    {
                        jf.AppendNewLine();
                        jf.Append("WHERE  ROWNUM <= 1");
                    }
                }

                indent -= 1;
                jf.Indent = indent;
                jf.AppendNewLine();
                jf.Append(") ");
                jf.Append(alias0);
            }

            #endregion

            return cmd;
        }

        // 创建 INSRT 命令
        protected override Command ResolveInsertCommand<T>(DbQueryableInfo_Insert<T> dbQuery, ResolveToken token)
        {
            ISqlBuilder builder = this.CreateSqlBuilder(token);
            TypeRuntimeInfo typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo<T>();
            TableAliasCache aliases = new TableAliasCache();

            if (dbQuery.Entity != null)
            {
                // 如果没有Sequence列，使用 INSERT ALL INTO 语法，否则就一条一条逐行写入~~
                // 批量 INSERT，自增列不会自动赋值 

                object entity = dbQuery.Entity;
                ISqlBuilder columnsBuilder = this.CreateSqlBuilder(token);
                ISqlBuilder valuesBuilder = this.CreateSqlBuilder(token);

                // 指定插入列
                MemberInvokerCollection invokers = typeRuntime.Invokers;
                if (dbQuery.EntityColumns != null && dbQuery.EntityColumns.Count > 0)
                {
                    invokers = new MemberInvokerCollection();
                    for (int i = 0; i < dbQuery.EntityColumns.Count; i++)
                    {
                        Expression curExpr = dbQuery.EntityColumns[i];
                        if (curExpr.NodeType == ExpressionType.Lambda) curExpr = (curExpr as LambdaExpression).Body.ReduceUnary();
                        if (curExpr.NodeType != ExpressionType.MemberAccess)
                            throw new XFrameworkException("Can't read field name from expression {0}", dbQuery.EntityColumns[i]);

                        MemberExpression member = curExpr as MemberExpression;
                        string name = member.Member.Name;
                        invokers[name] = typeRuntime.Invokers[name];
                    }
                }

                // 自增列参数
                IDbDataParameter seqParameter = null;
                // 自增列标记
                ColumnAttribute seqColumn = null;
                foreach (var invoker in invokers)
                {
                    var column = invoker.Column;
                    if (column != null && column.NoMapped) continue;
                    if (invoker.ForeignKey != null) continue;
                    if (invoker.Member.MemberType == System.Reflection.MemberTypes.Method) continue;

                    columnsBuilder.AppendMember(invoker.Member.Name);
                    columnsBuilder.Append(',');

                    if (invoker == dbQuery.AutoIncrement)
                    {
                        seqColumn = column;
                        if (dbQuery.Bulk == null)
                        {
                            // 非批量INSERT，产生一个 OUTPUT 类型的参数
                            string pName = string.Format("{0}p{1}", this.ParameterPrefix, token.Parameters.Count);
                            var database = dbQuery.SourceQuery.DbContext.Database;
                            seqParameter = database.CreateParameter(pName, -1, direction: ParameterDirection.Output);
                            token.Parameters.Add(seqParameter);
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
                        string seg = this.DbValue.GetSqlValueWidthDefault(value, token, column);
                        valuesBuilder.Append(seg);
                        valuesBuilder.Append(',');
                    }
                }
                columnsBuilder.Length -= 1;
                valuesBuilder.Length -= 1;

                if (dbQuery.Bulk == null)
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
                        //useSEQ = true;
                    }
                    builder.Append("INSERT ");
                }
                else
                {
                    // 批量 INSERT
                    if (!dbQuery.Bulk.OnlyValue || seqColumn != null) builder.Append("INSERT ");
                    // 如果有自增列则不使用 INSERT ALL INTO 语法
                    if (!dbQuery.Bulk.OnlyValue && seqColumn == null) builder.Append("ALL ");
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

                if (dbQuery.Bulk == null) builder.Append(';');
                else
                {
                    if (seqColumn != null)
                    {
                        if (dbQuery.Bulk.IsEndPos) builder.Append(";");
                        else builder.AppendNewLine(";");
                    }
                    else
                    {
                        builder.AppendNewLine();
                        if (dbQuery.Bulk.IsEndPos) builder.Append("SELECT 1 FROM DUAL;");
                    }
                }
            }
            else if (dbQuery.SelectInfo != null)
            {
                builder.Append("INSERT INTO ");
                builder.AppendMember(typeRuntime.TableName, !typeRuntime.IsTemporary);
                builder.Append('(');

                int i = 0;
                MapperCommand cmd2 = this.ResolveSelectCommand(dbQuery.SelectInfo, 0, false, token) as MapperCommand;
                foreach (var column in cmd2.PickColumns)
                {
                    builder.AppendMember(column.Name);
                    if (i < cmd2.PickColumns.Count - 1) builder.Append(',');
                    i++;
                }

                builder.Append(')');
                builder.AppendNewLine();
                builder.Append(cmd2.CommandText);
                builder.Append(';');
            }

            var cmd = new Command(builder.ToString(), builder.Token != null ? builder.Token.Parameters : null, System.Data.CommandType.Text);
            return cmd;
        }

        // 创建 DELETE 命令
        protected override Command ResolveDeleteCommand<T>(DbQueryableInfo_Delete<T> dbQuery, ResolveToken token)
        {
            TypeRuntimeInfo typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo<T>();
            ISqlBuilder builder = this.CreateSqlBuilder(token);

            builder.Append("DELETE FROM ");
            builder.AppendMember(typeRuntime.TableName, !typeRuntime.IsTemporary);
            builder.Append(" t0 ");

            if (dbQuery.Entity != null)
            {
                if (typeRuntime.KeyInvokers == null || typeRuntime.KeyInvokers.Count == 0)
                    throw new XFrameworkException("Delete<T>(T value) require entity must have key column.");

                object entity = dbQuery.Entity;

                builder.AppendNewLine();
                builder.Append("WHERE ");

                foreach (var invoker in typeRuntime.KeyInvokers)
                {
                    var column = invoker.Column;
                    var value = invoker.Invoke(entity);
                    var seg = this.DbValue.GetSqlValue(value, token, column);

                    builder.AppendMember("t0", invoker.Member.Name);
                    builder.Append(" = ");
                    builder.Append(seg);
                    builder.Append(" AND ");
                }
                builder.Length -= 5;
            }
            else if (dbQuery.SelectInfo != null)
            {
                LambdaExpression lambda = null;
                var sourceQuery = dbQuery.SourceQuery;
                if (sourceQuery.DbExpressions != null && sourceQuery.DbExpressions.Count > 1)
                {
                    switch (sourceQuery.DbExpressions[1].DbExpressionType)
                    {
                        case DbExpressionType.Join:
                        case DbExpressionType.GroupJoin:
                        case DbExpressionType.GroupRightJoin:
                            lambda = (LambdaExpression)sourceQuery.DbExpressions[1].Expressions[1];
                            break;

                        case DbExpressionType.Select:
                        case DbExpressionType.SelectMany:
                            lambda = (LambdaExpression)sourceQuery.DbExpressions[1].Expressions[0];
                            break;
                    }
                }
                if (lambda == null)
                {
                    DbExpression dbExpression = dbQuery.SelectInfo.Select;
                    dbExpression = dbQuery.SelectInfo.Condtion;
                    if (dbExpression != null && dbExpression.Expressions != null) lambda = (LambdaExpression)dbExpression.Expressions[0];
                }

                // 解析查询以确定是否需要嵌套
                var parameter = Expression.Parameter(typeof(OracleRowId), lambda != null ? lambda.Parameters[0].Name : "x");
                var expression = Expression.MakeMemberAccess(parameter, (_rowIdExpression.Body as MemberExpression).Member);
                dbQuery.SelectInfo.Select = new DbExpression(DbExpressionType.Select, expression);
                var cmd = (MapperCommand)this.ResolveSelectCommand<T>(dbQuery.SelectInfo, 1, false, null);

                if ((cmd.NavMembers != null && cmd.NavMembers.Count > 0) || dbQuery.SelectInfo.Joins.Count > 0)
                {
                    cmd = (MapperCommand)this.ResolveSelectCommand<T>(dbQuery.SelectInfo, 1, false, token);
                    builder.Append("WHERE t0.RowID IN(");
                    builder.AppendNewLine(cmd.CommandText);
                    builder.Append(')');
                }
                else
                {
                    TableAliasCache aliases = this.PrepareTableAlias<T>(dbQuery.SelectInfo, token);
                    ExpressionVisitorBase visitor = null;

                    visitor = new JoinExpressionVisitor(this, aliases, dbQuery.SelectInfo.Joins);
                    visitor.Write(builder);

                    visitor = new WhereExpressionVisitor(this, aliases, dbQuery.SelectInfo.Condtion);
                    visitor.Write(builder);
                }
            }

            builder.Append(';');
            return new Command(builder.ToString(), builder.Token != null ? builder.Token.Parameters : null, System.Data.CommandType.Text);
        }

        // 创建 UPDATE 命令
        protected override Command ResolveUpdateCommand<T>(DbQueryableInfo_Update<T> dbQuery, ResolveToken token)
        {
            ISqlBuilder builder = this.CreateSqlBuilder(token);
            var typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo<T>();

            builder.Append("UPDATE ");
            builder.AppendMember(typeRuntime.TableName, !typeRuntime.IsTemporary);
            builder.Append(" t0 SET");
            builder.AppendNewLine();

            if (dbQuery.Entity != null)
            {
                object entity = dbQuery.Entity;
                ISqlBuilder whereBuilder = this.CreateSqlBuilder(token);
                bool useKey = false;
                int length = 0;


                foreach (var invoker in typeRuntime.Invokers)
                {
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
                        var seg = this.DbValue.GetSqlValueWidthDefault(value, token, column);

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
                foreach (var invoker in typeRuntime.KeyInvokers)
                {
                    var column = invoker.Column;
                    var value = invoker.Invoke(entity);
                    var seg = this.DbValue.GetSqlValueWidthDefault(value, token, column);
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
            else if (dbQuery.Expression != null)
            {
                // SELECT 表达式
                LambdaExpression lambda = dbQuery.Expression as LambdaExpression;
                var body = lambda.Body;
                Expression expression = null;
                if (body.NodeType == ExpressionType.MemberInit)
                {
                    var memberInit = body as MemberInitExpression;
                    var bindings = new List<MemberBinding>(memberInit.Bindings);
                    foreach (var invoker in typeRuntime.KeyInvokers)
                    {
                        var member = Expression.MakeMemberAccess(lambda.Parameters[0], invoker.Member);
                        var binding = Expression.Bind(invoker.Member, member);
                        if (!bindings.Any(x => x.Member == invoker.Member)) bindings.Add(binding);
                    }
                    expression = Expression.MemberInit(memberInit.NewExpression, bindings);
                }
                else if (body.NodeType == ExpressionType.New)
                {
                    var newExpression = body as NewExpression;
                    var bindings = new List<MemberBinding>();
                    for (int i = 0; i < newExpression.Members.Count; i++)
                    {
                        var invoker = typeRuntime.GetInvoker(newExpression.Members[i].Name);
                        var binding = Expression.Bind(invoker.Member, newExpression.Arguments[i].Type != invoker.DataType
                            ? Expression.Convert(newExpression.Arguments[i], invoker.DataType)
                            : newExpression.Arguments[i]);
                        bindings.Add(binding);
                    }

                    foreach (var invoker in typeRuntime.KeyInvokers)
                    {
                        var member = Expression.MakeMemberAccess(lambda.Parameters[0], invoker.Member);
                        var binding = Expression.Bind(invoker.Member, member);
                        if (!bindings.Any(x => x.Member == invoker.Member)) bindings.Add(binding);
                    }

                    var newExpression2 = Expression.New(typeRuntime.ConstructInvoker.Constructor);
                    expression = Expression.MemberInit(newExpression2, bindings);
                }

                // 解析查询以确定是否需要嵌套
                dbQuery.SelectInfo.Select = new DbExpression(DbExpressionType.Select, expression);
                var cmd = (MapperCommand)this.ResolveSelectCommand<T>(dbQuery.SelectInfo, 1, false, null);//, token);

                if ((cmd.NavMembers != null && cmd.NavMembers.Count > 0) || dbQuery.SelectInfo.Joins.Count > 0)
                {
                    // 无法使用 DISTINCT, GROUP BY 等子句从视图中选择 ROWID 或采样。UPDATE 不能用rowid
                    // 有导航属性或者关联查询，使用 MERGE INTO 语法。要求必须有主键

                    if (typeRuntime.KeyInvokers == null || typeRuntime.KeyInvokers.Count == 0)
                        throw new XFrameworkException("Update<T>(Expression<Func<T, object>> updateExpression) require entity must have key column.");

                    builder.Length = 0;
                    builder.Append("MERGE INTO ");
                    builder.AppendMember(typeRuntime.TableName, !typeRuntime.IsTemporary);
                    builder.AppendNewLine(" t0");
                    builder.Append("USING (");

                    cmd = (MapperCommand)this.ResolveSelectCommand<T>(dbQuery.SelectInfo, 1, false, token);
                    builder.AppendNewLine(cmd.CommandText);
                    builder.Append(") t1 ON (");
                    foreach (var invoker in typeRuntime.KeyInvokers)
                    {
                        builder.AppendMember("t0", invoker.Name);
                        builder.Append(" = ");
                        builder.AppendMember("t1", invoker.Name);
                        builder.Append(" AND ");
                    }
                    builder.Length -= 5;
                    builder.Append(')');

                    // UPDATE
                    builder.AppendNewLine();
                    builder.AppendNewLine("WHEN MATCHED THEN UPDATE SET ");

                    // SET 字段
                    var visitor = new OracleUpdateExpressionVisitor(this, null, dbQuery.Expression);
                    visitor.Write(builder);
                }
                else
                {
                    // 直接 SQL 的 UPDATE 语法
                    TableAliasCache aliases = this.PrepareTableAlias<T>(dbQuery.SelectInfo, token);
                    ExpressionVisitorBase visitor = null;
                    visitor = new UpdateExpressionVisitor(this, aliases, dbQuery.Expression);
                    visitor.Write(builder);

                    visitor = new WhereExpressionVisitor(this, aliases, dbQuery.SelectInfo.Condtion);
                    visitor.Write(builder);
                }
            }

            builder.Append(';');
            return new Command(builder.ToString(), builder.Token != null ? builder.Token.Parameters : null, System.Data.CommandType.Text);
        }

        /// <summary>
        /// 行记录ID
        /// </summary>
        public class OracleRowId
        {
            /// <summary>
            /// 行记录ID
            /// </summary>
            public string RowId { get; set; }
        }
    }
}