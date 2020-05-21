
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
        /// <param name="visitor">表达式访问器</param>
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
        public override List<RawCommand> Resolve(List<object> dbQueryables)
        {
            bool haveBegin = false;
            ResolveToken token = null;
            List<RawCommand> sqlList = new List<RawCommand>();

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
                    if (cmd2 is MappingCommand)
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
                            sqlList.Add(new RawCommand("BEGIN"));
                            haveBegin = true;
                        }
                        sqlList.Add(cmd2);
                        if (cmd2.Parameters != null && cmd2.Parameters.Count > 1000)
                        {
                            // 1000个参数，就要重新分批
                            if (haveBegin)
                            {
                                sqlList.Add(new RawCommand("END;"));
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
                                    sqlList.Add(new RawCommand("END;"));
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


                    var cmd2 = new RawCommand(sql, token.Parameters, CommandType.Text);
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
                            sqlList.Add(new RawCommand("END;"));
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
                        sqlList.Add(new RawCommand("BEGIN"));
                        haveBegin = true;
                    }
                    // 解析批量插入操作
                    List<IDbQueryable> bulkList = obj as List<IDbQueryable>;
                    if (bulkList != null && bulkList.Count > 0) this.ResolveBulk(sqlList, bulkList);
                }

                if (haveBegin && i == dbQueryables.Count - 1) sqlList.Add(new RawCommand("END;"));
            }

            return sqlList;
        }

        /// <summary>
        /// 解析 SELECT 命令
        /// </summary>
        /// <param name="dbQuery">查询语义</param>
        /// <param name="indent">缩进</param>
        /// <param name="isOuter">指示是最外层查询</param>
        /// <param name="token">解析上下文</param>
        /// <returns></returns>
        protected override RawCommand ResolveSelectCommand(IDbQueryableInfo_Select dbQuery, int indent, bool isOuter, ResolveToken token)
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
            var subQuery = dbQuery.Subquery as IDbQueryableInfo_Select;
            if (dbQuery.HasMany && subQuery != null && subQuery.Aggregate != null) dbQuery = subQuery;

            bool useStatis = dbQuery.Aggregate != null;
            bool useNesting = dbQuery.HasDistinct || dbQuery.GroupBy != null || dbQuery.Skip > 0 || dbQuery.Take > 0;
            string alias0 = token != null && !string.IsNullOrEmpty(token.AliasPrefix) ? (token.AliasPrefix + "0") : "t0";
            // 没有聚合函数或者使用 'Skip' 子句，则解析OrderBy
            // 导航属性如果使用嵌套，除非有 TOP 或者 OFFSET 子句，否则不能用ORDER BY
            bool useOrderBy = (!useStatis || dbQuery.Skip > 0) && !dbQuery.HasAny && (!dbQuery.IsParsedByMany || (dbQuery.Skip > 0 || dbQuery.Take > 0));

            TableAliasCache aliases = this.PrepareTableAlias(dbQuery, token);
            var result = new MappingCommand(this, aliases, token) { HasMany = dbQuery.HasMany };
            ISqlBuilder jf = result.JoinFragment;
            ISqlBuilder wf = result.WhereFragment;
            (jf as OracleSqlBuilder).UseQuote = isOuter;

            jf.Indent = indent;

            #region 嵌套查询

            if (useStatis && useNesting)
            {
                // SELECT
                jf.Append("SELECT ");
                jf.AppendNewLine();

                // SELECT COUNT(1)
                var visitor_ = new AggregateExpressionVisitor(this, aliases, dbQuery.Aggregate, dbQuery.GroupBy, alias0);
                visitor_.Write(jf);
                result.AddNavMembers(visitor_.NavMembers);

                // SELECT COUNT(1) FROM
                jf.AppendNewLine();
                jf.Append("FROM ( ");

                indent += 1;
                jf.Indent = indent;
                (jf as OracleSqlBuilder).UseQuote = false;
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
                var visitor_ = new AggregateExpressionVisitor(this, aliases, dbQuery.Aggregate, dbQuery.GroupBy);
                visitor_.Write(jf);
                result.AddNavMembers(visitor_.NavMembers);
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
                    (sf as OracleSqlBuilder).UseQuote = (dbQuery.Skip > 0 || dbQuery.Take > 0) ? false : (jf as OracleSqlBuilder).UseQuote;

                    var visitor_ = new OracleColumnExpressionVisitor(this, aliases, dbQuery);
                    visitor_.Write(sf);

                    result.PickColumns = visitor_.PickColumns;
                    result.PickColumnText = visitor_.PickColumnText;
                    result.Navigations = visitor_.Navigations;
                    result.AddNavMembers(visitor_.NavMembers);

                    // 分页，产生两层嵌套
                    if (dbQuery.Skip > 0 || dbQuery.Take > 0)
                    {
                        // 第一层嵌套
                        int index = 0;
                        jf.AppendNewLine();
                        foreach (var column in result.PickColumns)
                        {
                            jf.AppendMember(alias0, column.Name);
                            jf.AppendAs(column.NewName);
                            index += 1;
                            if (index < result.PickColumns.Count)
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
                        (jf as OracleSqlBuilder).UseQuote = isOuter;
                        jf.AppendNewLine();

                        jf.Append("SELECT");
                        jf.AppendNewLine();

                        foreach (var column in result.PickColumns)
                        {
                            jf.AppendMember(alias0, column.NewName);
                            jf.AppendAs(column.NewName);
                            jf.Append(',');
                            jf.AppendNewLine();
                        }
                        jf.Append("ROWNUM");
                        jf.AppendAs("Row_Number0");
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
            if (dbQuery.Subquery != null)
            {
                // 子查询
                jf.Append('(');
                RawCommand cmd = this.ResolveSelectCommand(dbQuery.Subquery, indent + 1, false, token);
                jf.Append(cmd.CommandText);
                jf.AppendNewLine();
                jf.Append(") ");
                jf.Append(alias0);
                jf.Append(' ');
            }
            else
            {
                var typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo(dbQuery.FromType);
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
            visitor = new WhereExpressionVisitor(this, aliases, dbQuery.Where);
            visitor.Write(wf);
            result.AddNavMembers(visitor.NavMembers);

            // GROUP BY 子句
            visitor = new GroupByExpressionVisitor(this, aliases, dbQuery.GroupBy);
            visitor.Write(wf);
            result.AddNavMembers(visitor.NavMembers);

            // HAVING 子句
            visitor = new HavingExpressionVisitor(this, aliases, dbQuery.Having, dbQuery.GroupBy);
            visitor.Write(wf);
            result.AddNavMembers(visitor.NavMembers);

            // ORDER 子句
            if (dbQuery.OrderBys.Count > 0 && useOrderBy)
            {
                visitor = new OrderByExpressionVisitor(this, aliases, dbQuery.OrderBys, dbQuery.GroupBy);
                visitor.Write(wf);
                result.AddNavMembers(visitor.NavMembers);
            }

            #endregion

            #region 嵌套查询

            if (useStatis && useNesting)
            {
                result.CombineFragments();
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
                result.CombineFragments();
                visitor = new OrderByExpressionVisitor(this, aliases, subQuery.OrderBys);//, null, "t0");
                visitor.Write(jf);
            }

            #endregion

            #region 并集查询

            // UNION 子句
            if (dbQuery.Unions != null && dbQuery.Unions.Count > 0)
            {
                result.CombineFragments();
                for (int index = 0; index < dbQuery.Unions.Count; index++)
                {
                    jf.AppendNewLine();
                    jf.Append("UNION ALL");
                    if (indent == 0) jf.AppendNewLine();
                    RawCommand cmd2 = this.ResolveSelectCommand(dbQuery.Unions[index], indent, isOuter, token);
                    jf.Append(cmd2.CommandText);
                }
            }

            #endregion

            #region 分页查询

            if (dbQuery.Take > 0 || dbQuery.Skip > 0)
            {
                // 合并 WHERE
                result.CombineFragments();

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
                result.CombineFragments();
                // 如果没有分页，则显式指定只查一笔记录
                if (dbQuery.Take == 0 && dbQuery.Skip == 0)
                {
                    if (dbQuery.Where != null && dbQuery.Where.Expressions != null) jf.Append(" AND ROWNUM <= 1");
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

            return result;
        }

        /// <summary>
        /// 创建 INSRT 命令
        /// </summary>
        /// <param name="dbQuery">查询语义</param>
        /// <param name="token">解析上下文</param>
        /// <returns></returns>
        protected override RawCommand ResolveInsertCommand<T>(IDbQueryableInfo_Insert dbQuery, ResolveToken token)
        {
            ISqlBuilder builder = this.CreateSqlBuilder(token);
            TableAliasCache aliases = new TableAliasCache();
            var typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo<T>();

            if (dbQuery.Entity != null)
            {
                // 如果没有Sequence列，使用 INSERT ALL INTO 语法，否则就一条一条逐行写入~~
                // 批量 INSERT，自增列不会自动赋值

                object entity = dbQuery.Entity;
                ISqlBuilder seg_Columns = this.CreateSqlBuilder(token);
                ISqlBuilder seg_Values = this.CreateSqlBuilder(token);

                // 指定插入列
                MemberAccessorCollection memberAccessors = typeRuntime.Members;
                if (dbQuery.EntityColumns != null && dbQuery.EntityColumns.Count > 0)
                {
                    memberAccessors = new MemberAccessorCollection();
                    for (int i = 0; i < dbQuery.EntityColumns.Count; i++)
                    {
                        Expression curExpr = dbQuery.EntityColumns[i];
                        if (curExpr.NodeType == ExpressionType.Lambda) curExpr = (curExpr as LambdaExpression).Body.ReduceUnary();
                        if (curExpr.NodeType != ExpressionType.MemberAccess)
                            throw new XFrameworkException("Can't read field name from expression {0}", dbQuery.EntityColumns[i]);

                        MemberExpression member = curExpr as MemberExpression;
                        string name = member.Member.Name;
                        memberAccessors[name] = typeRuntime.Members[name];
                    }
                }

                // 自增列参数
                IDbDataParameter seqParameter = null;
                // 自增列标记
                ColumnAttribute seqColumn = null;
                foreach (var m in memberAccessors)
                {
                    var column = m.Column;
                    if (column != null && column.NoMapped) continue;
                    if (m.ForeignKey != null) continue;
                    if (m.Member.MemberType == System.Reflection.MemberTypes.Method) continue;

                    seg_Columns.AppendMember(m.Member.Name);
                    seg_Columns.Append(',');

                    if (m == typeRuntime.Identity)
                    {
                        seqColumn = column;
                        if (dbQuery.Bulk == null)
                        {
                            // 非批量INSERT，产生一个 OUTPUT 类型的参数
                            string pName = string.Format("{0}p{1}", this.ParameterPrefix, token.Parameters.Count);
                            var database = token.DbContext.Database;
                            seqParameter = database.CreateParameter(pName, -1, direction: ParameterDirection.Output);
                            token.Parameters.Add(seqParameter);
                            seg_Values.Append(seqParameter.ParameterName);
                            seg_Values.Append(',');
                        }
                        else
                        {
                            seg_Values.Append(((OracleColumnAttribute)column).SEQName);
                            seg_Values.Append(".NEXTVAL");
                            seg_Values.Append(',');
                        }
                    }
                    else
                    {
                        var value = m.Invoke(entity);
                        string seg = this.DbValue.GetSqlValueWidthDefault(value, token, column);
                        seg_Values.Append(seg);
                        seg_Values.Append(',');
                    }
                }
                seg_Columns.Length -= 1;
                seg_Values.Length -= 1;

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
                builder.Append(seg_Columns);
                builder.Append(')');
                builder.AppendNewLine();
                builder.AppendTab();
                builder.Append("VALUES");
                builder.Append('(');
                builder.Append(seg_Values);
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
            else if (dbQuery.Query != null)
            {
                builder.Append("INSERT INTO ");
                builder.AppendMember(typeRuntime.TableName, !typeRuntime.IsTemporary);
                builder.Append('(');

                int i = 0;
                MappingCommand cmd = this.ResolveSelectCommand(dbQuery.Query, 0, false, token) as MappingCommand;
                foreach (var column in cmd.PickColumns)
                {
                    builder.AppendMember(column.Name);
                    if (i < cmd.PickColumns.Count - 1) builder.Append(',');
                    i++;
                }

                builder.Append(')');
                builder.AppendNewLine();
                builder.Append(cmd.CommandText);
                builder.Append(';');
            }

            var result = new RawCommand(builder.ToString(), builder.Token != null ? builder.Token.Parameters : null, System.Data.CommandType.Text);
            return result;
        }

        /// <summary>
        /// 创建 DELETE 命令
        /// </summary>
        /// <param name="dbQuery">查询语义</param>
        /// <param name="token">解析上下文</param>
        /// <returns></returns>
        protected override RawCommand ResolveDeleteCommand<T>(IDbQueryableInfo_Delete dbQuery, ResolveToken token)
        {
            ISqlBuilder builder = this.CreateSqlBuilder(token);
            var typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo<T>();

            builder.Append("DELETE FROM ");
            builder.AppendMember(typeRuntime.TableName, !typeRuntime.IsTemporary);
            builder.Append(" t0 ");

            if (dbQuery.Entity != null)
            {
                if (typeRuntime.KeyMembers == null || typeRuntime.KeyMembers.Count == 0)
                    throw new XFrameworkException("Delete<T>(T value) require entity must have key column.");

                object entity = dbQuery.Entity;

                builder.AppendNewLine();
                builder.Append("WHERE ");

                foreach (var m in typeRuntime.KeyMembers)
                {
                    var column = m.Column;
                    var value = m.Invoke(entity);
                    var seg = this.DbValue.GetSqlValue(value, token, column);

                    builder.AppendMember("t0", m.Member.Name);
                    builder.Append(" = ");
                    builder.Append(seg);
                    builder.Append(" AND ");
                }
                builder.Length -= 5;
            }
            else if (dbQuery.Query != null)
            {
                // 解析查询用来确定是否需要嵌套
                var cmd = this.ResolveSelectCommand(dbQuery.Query, 1, false, null) as MappingCommand;
                if ((cmd.NavMembers != null && cmd.NavMembers.Count > 0) || dbQuery.Query.Joins.Count > 0)
                {
                    // 最外层仅选择 RowID 列
                    var outQuery = dbQuery.Query;
                    outQuery.Select = new DbExpression(DbExpressionType.Select, Expression.Constant("t0.RowId", typeof(string)));
                    var iterator = outQuery;
                    while (iterator.Subquery != null)
                    {
                        var subQuery = new OracleDbQueryableInfo_Select(iterator.Subquery);
                        iterator.Subquery = subQuery;
                        iterator = subQuery;
                    }

                    // 解析成 RowId IN 结构
                    cmd = (MappingCommand)this.ResolveSelectCommand(dbQuery.Query, 1, false, token);
                    builder.Append("WHERE t0.RowId IN(");
                    builder.AppendNewLine(cmd.CommandText);
                    builder.Append(')');
                }
                else
                {
                    TableAliasCache aliases = this.PrepareTableAlias(dbQuery.Query, token);
                    var visitor = new JoinExpressionVisitor(this, aliases, dbQuery.Query.Joins);
                    visitor.Write(builder);

                    var visitor_ = new WhereExpressionVisitor(this, aliases, dbQuery.Query.Where);
                    visitor_.Write(builder);
                }
            }

            builder.Append(';');
            return new RawCommand(builder.ToString(), builder.Token != null ? builder.Token.Parameters : null, System.Data.CommandType.Text);
        }

        /// <summary>
        /// 创建 UPDATE 命令
        /// </summary>
        /// <param name="dbQuery">查询语义</param>
        /// <param name="token">解析上下文</param>
        /// <returns></returns>
        protected override RawCommand ResolveUpdateCommand<T>(IDbQueryableInfo_Update dbQuery, ResolveToken token)
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
                ISqlBuilder seg_Where = this.CreateSqlBuilder(token);
                bool useKey = false;
                int length = 0;


                foreach (var m in typeRuntime.Members)
                {
                    var column = m.Column;
                    if (column != null && column.IsIdentity) goto gotoLabel; // Fix issue# 自增列同时又是主键
                    if (column != null && column.NoMapped) continue;
                    if (m.ForeignKey != null) continue;
                    if (m.Member.MemberType == System.Reflection.MemberTypes.Method) continue;

                    builder.AppendMember(m.Member.Name);
                    builder.Append(" = ");

                gotoLabel:

                    if (column == null || !column.IsIdentity)
                    {
                        var value = m.Invoke(entity);
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
                foreach (var m in typeRuntime.KeyMembers)
                {
                    var column = m.Column;
                    var value = m.Invoke(entity);
                    var seg = this.DbValue.GetSqlValueWidthDefault(value, token, column);
                    index += 1;

                    seg_Where.AppendMember(m.Member.Name);
                    seg_Where.Append(" = ");
                    seg_Where.Append(seg);
                    if (index < typeRuntime.KeyMembers.Count - 1) seg_Where.Append(" AND ");
                }

                builder.Length = length;
                builder.AppendNewLine();
                builder.Append("WHERE ");
                builder.Append(seg_Where);

            }
            else if (dbQuery.Expression != null)
            {
                // SELECT 表达式
                LambdaExpression lambda = dbQuery.Expression as LambdaExpression;
                var body = lambda.Body;
                Expression expression = null;
                if (body.NodeType == ExpressionType.MemberInit)
                {
                    var @init = body as MemberInitExpression;
                    var bindings = new List<MemberBinding>(@init.Bindings);
                    foreach (var m in typeRuntime.KeyMembers)
                    {
                        var member = Expression.MakeMemberAccess(lambda.Parameters[0], m.Member);
                        var binding = Expression.Bind(m.Member, member);
                        if (!bindings.Any(x => x.Member == m.Member)) bindings.Add(binding);
                    }
                    expression = Expression.MemberInit(@init.NewExpression, bindings);
                }
                else if (body.NodeType == ExpressionType.New)
                {
                    var newExpression = body as NewExpression;
                    var bindings = new List<MemberBinding>();
                    for (int i = 0; i < newExpression.Members.Count; i++)
                    {
                        var m = typeRuntime.GetMember(newExpression.Members[i].Name);
                        var binding = Expression.Bind(m.Member, newExpression.Arguments[i].Type != m.DataType
                            ? Expression.Convert(newExpression.Arguments[i], m.DataType)
                            : newExpression.Arguments[i]);
                        bindings.Add(binding);
                    }

                    foreach (var m in typeRuntime.KeyMembers)
                    {
                        var member = Expression.MakeMemberAccess(lambda.Parameters[0], m.Member);
                        var binding = Expression.Bind(m.Member, member);
                        if (!bindings.Any(x => x.Member == m.Member)) bindings.Add(binding);
                    }

                    var newExpression2 = Expression.New(typeRuntime.Constructor.Constructor);
                    expression = Expression.MemberInit(newExpression2, bindings);
                }

                // 解析查询以确定是否需要嵌套
                dbQuery.Query.Select = new DbExpression(DbExpressionType.Select, expression);
                var cmd = (MappingCommand)this.ResolveSelectCommand(dbQuery.Query, 1, false, null);//, token);

                if ((cmd.NavMembers != null && cmd.NavMembers.Count > 0) || dbQuery.Query.Joins.Count > 0)
                {
                    // 无法使用 DISTINCT, GROUP BY 等子句从视图中选择 ROWID 或采样。UPDATE 不能用rowid
                    // 有导航属性或者关联查询，使用 MERGE INTO 语法。要求必须有主键

                    if (typeRuntime.KeyMembers == null || typeRuntime.KeyMembers.Count == 0)
                        throw new XFrameworkException("Update<T>(Expression<Func<T, object>> updateExpression) require entity must have key column.");

                    builder.Length = 0;
                    builder.Append("MERGE INTO ");
                    builder.AppendMember(typeRuntime.TableName, !typeRuntime.IsTemporary);
                    builder.AppendNewLine(" t0");
                    builder.Append("USING (");

                    cmd = (MappingCommand)this.ResolveSelectCommand(dbQuery.Query, 1, false, token);
                    builder.AppendNewLine(cmd.CommandText);
                    builder.Append(") t1 ON (");
                    foreach (var m in typeRuntime.KeyMembers)
                    {
                        builder.AppendMember("t0", m.Name);
                        builder.Append(" = ");
                        builder.AppendMember("t1", m.Name);
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
                    TableAliasCache aliases = this.PrepareTableAlias(dbQuery.Query, token);
                    var visitor = new UpdateExpressionVisitor(this, aliases, dbQuery.Expression);
                    visitor.Write(builder);

                    var visitor_ = new WhereExpressionVisitor(this, aliases, dbQuery.Query.Where);
                    visitor_.Write(builder);
                }
            }

            builder.Append(';');
            return new RawCommand(builder.ToString(), builder.Token != null ? builder.Token.Parameters : null, System.Data.CommandType.Text);
        }
    }
}