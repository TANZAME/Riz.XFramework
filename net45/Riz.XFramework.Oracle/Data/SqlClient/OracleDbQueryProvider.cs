﻿
using System;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Collections.Generic;
using Oracle.ManagedDataAccess.Client;
using System.Data.Common;

namespace Riz.XFramework.Data.SqlClient
{
    /// <summary>
    /// 数据查询提供者
    /// </summary>
    internal sealed class OracleDbQueryProvider : DbQueryProvider
    {
        /// <summary>
        /// 查询语义提供者实例
        /// </summary>
        public static OracleDbQueryProvider Instance = new OracleDbQueryProvider();

        /// <summary>
        /// 数据源类的提供程序实现的实例
        /// </summary>
        public override DbProviderFactory DbProvider { get { return OracleClientFactory.Instance; } }

        /// <summary>
        /// SQL字段值解析器
        /// </summary>
        public override DbValueResolver DbResolver { get { return OracleDbValueResolver.Instance; } }

        /// <summary>
        /// 实体转换映射委托生成器
        /// </summary>
        public override TypeDeserializerImpl TypeDeserializerImpl { get { return OracleTypeDeserializerImpl.Instance; } }

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
        /// <param name="context">解析SQL命令上下文</param>
        /// <returns></returns>
        public override ISqlBuilder CreateSqlBuilder(ITranslateContext context)
        {
            return new OracleSqlBuilder(context);
        }

        /// <summary>
        /// 创建方法表达式访问器
        /// </summary>
        /// <param name="visitor">表达式访问器</param>
        /// <returns></returns>
        public override MethodCallExpressionVisitor CreateMethodCallVisitor(LinqExpressionVisitor visitor)
        {
            return new OracleMethodCallExressionVisitor(visitor);
        }

        /// <summary>
        /// 解析 SQL 命令
        /// <para>
        /// 返回的已经解析语义中执行批次用 null 分开
        /// </para>
        /// </summary>
        /// <param name="dbQueryables">查询语句</param>
        /// <returns></returns>
        public override List<DbRawCommand> Translate(List<object> dbQueryables)
        {
            bool haveBegin = false;
            ITranslateContext context = null;
            var sqlList = new List<DbRawCommand>();

            for (int i = 0; i < dbQueryables.Count; i++)
            {
                object obj = dbQueryables[i];
                if (obj == null) continue;

                if (obj is IDbQueryable)
                {
                    IDbQueryable dbQuery = (IDbQueryable)obj;
                    dbQuery.Parameterized = true;
                    if (context == null) context = this.CreateTranslateContext(dbQuery.DbContext);
                    if (context.Parameters == null) context.Parameters = new List<IDbDataParameter>(8);

                    var cmd = dbQuery.Translate(0, true, context);
                    if (cmd is DbSelectCommand)
                    {
                        // 查询单独执行
                        if (sqlList.Count > 0 && (i - 1) >= 0 && sqlList[sqlList.Count - 1] != null) sqlList.Add(null);

                        sqlList.Add(cmd);
                        sqlList.Add(null);
                        context = this.CreateTranslateContext(dbQuery.DbContext);
                        context.Parameters = new List<IDbDataParameter>(8);
                    }
                    else
                    {
                        // 增删改
                        if (!haveBegin)
                        {
                            sqlList.Add(new DbRawCommand("BEGIN"));
                            haveBegin = true;
                        }
                        sqlList.Add(cmd);
                        if (cmd.Parameters != null && cmd.Parameters.Count > 1000)
                        {
                            // 1000个参数，就要重新分批
                            if (haveBegin)
                            {
                                sqlList.Add(new DbRawCommand("END;"));
                                haveBegin = false;
                                sqlList.Add(null);
                            }
                            context = this.CreateTranslateContext(dbQuery.DbContext);
                            context.Parameters = new List<IDbDataParameter>(8);
                        }

                        if (i + 1 < dbQueryables.Count)
                        {
                            // 检查下一条是否是选择语句
                            bool isQuery = false;
                            if (dbQueryables[i + 1] is IDbQueryable)
                            {
                                var tree = ((IDbQueryable)dbQueryables[i + 1]).Parse();
                                isQuery = tree is DbQuerySelectTree;
                            }
                            else if ((dbQueryables[i + 1] is string))
                            {
                                string sql = dbQueryables[i + 1].ToString();
                                string method = string.Empty;
                                if (sql.Length > 6) method = sql.Substring(0, 6).Trim().ToUpper();
                                isQuery = method == "SELECT";
                            }
                            else if (dbQueryables[i + 1] is DbRawSql)
                            {
                                string sql = ((DbRawSql)dbQueryables[i + 1]).CommandText;
                                string method = string.Empty;
                                if (sql.Length > 6) method = sql.Substring(0, 6).Trim().ToUpper();
                                isQuery = method == "SELECT";
                            }

                            // 如果下一条是SELECT 语句，则需要结束当前语句块
                            if (isQuery)
                            {
                                if (haveBegin)
                                {
                                    sqlList.Add(new DbRawCommand("END;"));
                                    haveBegin = false;
                                    sqlList.Add(null);
                                }
                                context = this.CreateTranslateContext(dbQuery.DbContext);
                                context.Parameters = new List<IDbDataParameter>(8);
                            }
                        }
                    }

                }
                else if (obj is DbRawSql || obj is string)
                {
                    string sql = string.Empty;
                    if (obj is string) sql = obj.ToString();
                    else
                    {
                        DbRawSql rawSql = (DbRawSql)obj;
                        // 解析参数
                        object[] args = null;
                        if (rawSql.Parameters != null)
                            args = rawSql.Parameters.Select(x => this.DbResolver.GetSqlValue(x, context)).ToArray();
                        sql = rawSql.CommandText;
                        if (args != null && args.Length > 0) sql = string.Format(sql, args);
                    }


                    string methodName = string.Empty;
                    if (sql.Length > 6) methodName = sql.Substring(0, 6).Trim().ToUpper();
                    if (methodName == "SELECT")
                    {
                        if (sqlList.Count > 0 && (i - 1) >= 0 && sqlList[sqlList.Count - 1] != null) sqlList.Add(null);
                    }


                    var cmd = new DbRawCommand(sql, context.Parameters, CommandType.Text);
                    sqlList.Add(cmd);

                    if (methodName == "SELECT")
                    {
                        sqlList.Add(cmd);
                        sqlList.Add(null);
                        context = this.CreateTranslateContext(obj is DbRawSql ? ((DbRawSql)obj).DbContext : null);
                        context.Parameters = new List<IDbDataParameter>(8);
                    }
                    else if (cmd.Parameters != null && cmd.Parameters.Count > 1000)
                    {
                        // 1000个参数，就要重新分批
                        if (haveBegin)
                        {
                            sqlList.Add(new DbRawCommand("END;"));
                            sqlList.Add(null);
                            haveBegin = false;
                        }
                        context = this.CreateTranslateContext(obj is DbRawSql ? ((DbRawSql)obj).DbContext : null);
                        context.Parameters = new List<IDbDataParameter>(8);
                    }
                }
                else
                {
                    if (!haveBegin)
                    {
                        sqlList.Add(new DbRawCommand("BEGIN"));
                        haveBegin = true;
                    }
                    // 解析批量插入操作
                    List<IDbQueryable> bulkList = obj as List<IDbQueryable>;
                    if (bulkList != null && bulkList.Count > 0) this.TranslateBulk(sqlList, bulkList);
                }

                if (haveBegin && i == dbQueryables.Count - 1) sqlList.Add(new DbRawCommand("END;"));
            }

            return sqlList;
        }

        /// <summary>
        /// 解析 SELECT 命令
        /// </summary>
        /// <param name="tree">查询语义</param>
        /// <param name="indent">缩进</param>
        /// <param name="isOutQuery">是否是最外层查询</param>
        /// <param name="context">解析SQL命令上下文</param>
        /// <returns></returns>
        protected override DbRawCommand TranslateSelectCommand(DbQuerySelectTree tree, int indent, bool isOutQuery, ITranslateContext context)
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
            var subquery = tree.Subquery as DbQuerySelectTree;
            if (tree.HasMany && subquery != null && subquery.Aggregate != null) tree = subquery;

            var srcDbExpressionType = context.DbExpressionType;
            var srcIsOutQuery = context.IsOutQuery;
            if (srcDbExpressionType == null)
                context.DbExpressionType = DbExpressionType.Select;
            if (srcIsOutQuery == null || !isOutQuery)
                context.IsOutQuery = isOutQuery;

            bool useAggregate = tree.Aggregate != null;
            // 没有聚合函数或者使用 'Skip' 子句，则解析OrderBy
            // 导航属性如果使用嵌套，除非有 TOP 或者 OFFSET 子句，否则不能用ORDER BY
            // 第一层的表别名
            string alias = context != null && !string.IsNullOrEmpty(context.AliasPrefix) ? (context.AliasPrefix + "0") : "t0";
            bool useSubquery = tree.HasDistinct || tree.GroupBy != null || tree.Skip > 0 || tree.Take > 0;
            bool useOrderBy = (!useAggregate || tree.Skip > 0) && !tree.HasAny && (!tree.IsParsedByMany || (tree.Skip > 0 || tree.Take > 0));

            TableAliasResolver aliasResolver = this.PrepareTableAlias(tree, context != null ? context.AliasPrefix : null);
            var result = new DbSelectCommand(context, aliasResolver);
            result.HasMany = tree.HasMany;

            ISqlBuilder jf = result.JoinFragment;
            ISqlBuilder wf = result.WhereFragment;
            (jf as OracleSqlBuilder).UseQuote = isOutQuery;

            jf.Indent = indent;

            #region 嵌套查询

            if (useAggregate && useSubquery)
            {
                // SELECT
                jf.Append("SELECT ");
                jf.AppendNewLine();

                // SELECT COUNT(1)
                var visitor_ = new AggregateExpressionVisitor(aliasResolver, tree.Aggregate, tree.GroupBy, alias);
                visitor_.Write(jf);
                result.AddNavMembers(visitor_.NavMembers);

                // SELECT COUNT(1) FROM
                jf.AppendNewLine();
                jf.Append("FROM ( ");

                indent += 1;
                jf.Indent = indent;
                (jf as OracleSqlBuilder).UseQuote = false;
                context.IsOutQuery = false;
            }

            #endregion

            #region 选择子句

            // SELECT 子句
            if (jf.Indent > 0) jf.AppendNewLine();
            jf.Append("SELECT ");

            if (tree.HasAny)
            {
                jf.Append("CASE WHEN COUNT(1) = 1 THEN 1 ELSE 0 END FROM (");
                indent += 1;
                jf.Indent = indent;
                jf.AppendNewLine();
                jf.Append("SELECT 1 ");
            }

            if (useAggregate && !useSubquery)
            {
                // 如果有聚合函数，并且不是嵌套的话，则直接使用SELECT <MAX,MIN...>，不需要解析选择的字段
                jf.AppendNewLine();
                var visitor_ = new AggregateExpressionVisitor(aliasResolver, tree.Aggregate, tree.GroupBy);
                visitor_.Write(jf);
                result.AddNavMembers(visitor_.NavMembers);
            }
            else
            {
                // DISTINCT 子句
                if (tree.HasDistinct) jf.Append("DISTINCT ");

                #region 选择字段

                if (!tree.HasAny)
                {
                    // SELECT 范围
                    ISqlBuilder sf = this.CreateSqlBuilder(context);
                    sf.Indent = jf.Indent + ((tree.Skip > 0 || tree.Take > 0) ? 2 : 0);
                    (sf as OracleSqlBuilder).UseQuote = (tree.Skip > 0 || tree.Take > 0) ? false : (jf as OracleSqlBuilder).UseQuote;

                    var visitor_ = new OracleColumnExpressionVisitor(aliasResolver, tree);
                    visitor_.Write(sf);

                    result.PickColumns = visitor_.PickColumns;
                    result.PickColumnText = visitor_.PickColumnText;
                    result.PickNavDescriptors = visitor_.PickNavDescriptors;
                    result.AddNavMembers(visitor_.NavMembers);

                    // 分页，产生两层嵌套
                    if (tree.Skip > 0 || tree.Take > 0)
                    {
                        // 第一层嵌套
                        int index = 0;
                        jf.AppendNewLine();
                        foreach (var column in result.PickColumns)
                        {
                            jf.AppendMember(alias, column.Name);
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
                        isOutQuery = false;
                        jf.Indent = indent;
                        (jf as OracleSqlBuilder).UseQuote = isOutQuery;
                        jf.AppendNewLine();

                        jf.Append("SELECT");
                        jf.AppendNewLine();

                        foreach (var column in result.PickColumns)
                        {
                            jf.AppendMember(alias, column.NewName);
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
            if (tree.Subquery != null)
            {
                // 子查询
                jf.Append('(');
                DbRawCommand cmd = this.TranslateSelectCommand(tree.Subquery, indent + 1, false, context);
                jf.Append(cmd.CommandText);
                jf.AppendNewLine();
                jf.Append(") ");
                jf.Append(alias);
                jf.Append(' ');
            }
            else
            {
                var typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo(tree.FromType);
                jf.AppendMember(typeRuntime.TableName, !typeRuntime.IsTemporary);
                jf.Append(' ');
                jf.Append(alias);
                jf.Append(' ');
            }

            // LEFT<INNER> JOIN 子句
            LinqExpressionVisitor visitor = new JoinExpressionVisitor(aliasResolver, tree.Joins);
            visitor.Write(jf);

            wf.Indent = jf.Indent;

            // WHERE 子句
            visitor = new WhereExpressionVisitor(aliasResolver, tree.Where);
            visitor.Write(wf);
            result.AddNavMembers(visitor.NavMembers);

            // GROUP BY 子句
            visitor = new GroupByExpressionVisitor(aliasResolver, tree.GroupBy);
            visitor.Write(wf);
            result.AddNavMembers(visitor.NavMembers);

            // HAVING 子句
            visitor = new HavingExpressionVisitor(aliasResolver, tree.Having, tree.GroupBy);
            visitor.Write(wf);
            result.AddNavMembers(visitor.NavMembers);

            // ORDER 子句
            if (tree.OrderBys.Count > 0 && useOrderBy)
            {
                visitor = new OrderByExpressionVisitor(aliasResolver, tree.OrderBys, tree.GroupBy);
                visitor.Write(wf);
                result.AddNavMembers(visitor.NavMembers);
            }

            #endregion

            #region 嵌套查询

            if (useAggregate && useSubquery)
            {
                result.CombineFragments();
                indent -= 1;
                jf.Indent = indent;
                jf.AppendNewLine();
                jf.Append(") ");
                jf.Append(alias);
            }

            #endregion

            #region 嵌套导航

            // TODO Include 从表，没分页，OrderBy 报错
            if (tree.HasMany && subquery != null && subquery.OrderBys.Count > 0 && subquery.Aggregate == null && !(subquery.Skip > 0 || subquery.Take > 0))
            {
                // OrderBy("a.CloudServer.CloudServerName");
                result.CombineFragments();
                visitor = new OrderByExpressionVisitor(aliasResolver, subquery.OrderBys);//, null, "t0");
                visitor.Write(jf);
            }

            #endregion

            #region 并集查询

            // UNION 子句
            if (tree.Unions != null && tree.Unions.Count > 0)
            {
                result.CombineFragments();
                for (int index = 0; index < tree.Unions.Count; index++)
                {
                    jf.AppendNewLine();
                    jf.Append("UNION ALL");
                    if (indent == 0) jf.AppendNewLine();
                    DbRawCommand cmd2 = this.TranslateSelectCommand(tree.Unions[index], indent, isOutQuery, context);
                    jf.Append(cmd2.CommandText);
                }
            }

            #endregion

            #region 分页查询

            if (tree.Take > 0 || tree.Skip > 0)
            {
                // 合并 WHERE
                result.CombineFragments();

                indent -= 1;
                jf.Indent = indent;
                jf.AppendNewLine();
                jf.Append(") ");
                jf.Append(alias);

                indent -= 1;
                jf.Indent = indent;
                jf.AppendNewLine();
                jf.Append(") ");
                jf.Append(alias);

                jf.AppendNewLine();
                jf.Append("WHERE ");
                if (tree.Skip > 0)
                {
                    jf.AppendMember(alias, "Row_Number0");
                    jf.Append(" > ");
                    jf.Append(this.DbResolver.GetSqlValue(tree.Skip, context));
                }
                if (tree.Take > 0)
                {
                    if (tree.Skip > 0) jf.Append(" AND ");
                    jf.AppendMember(alias, "Row_Number0");
                    jf.Append(" <= ");
                    jf.Append(this.DbResolver.GetSqlValue((tree.Skip + tree.Take), context));
                }
            }

            #endregion

            #region Any 子句

            // 'Any' 子句
            if (tree.HasAny)
            {
                // 产生 WHERE 子句
                result.CombineFragments();
                // 如果没有分页，则显式指定只查一笔记录
                if (tree.Take == 0 && tree.Skip == 0)
                {
                    if (tree.Where != null && tree.Where.Expressions != null) jf.Append(" AND ROWNUM <= 1");
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
                jf.Append(alias);
            }

            #endregion

            #region 还原状态

            context.DbExpressionType = srcDbExpressionType;
            context.IsOutQuery = srcIsOutQuery;

            #endregion

            return result;
        }

        /// <summary>
        /// 创建 INSRT 命令
        /// </summary>
        /// <param name="tree">查询语义</param>
        /// <param name="context">解析SQL命令上下文</param>
        /// <returns></returns>
        protected override DbRawCommand TranslateInsertCommand<T>(DbQueryInsertTree tree, ITranslateContext context)
        {
            ISqlBuilder builder = this.CreateSqlBuilder(context); ;
            var typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo<T>();

            if (tree.Entity != null)
            {
                // 如果没有Sequence列，使用 INSERT ALL INTO 语法，否则就一条一条逐行写入~~
                // 批量 INSERT，自增列不会自动赋值

                object entity = tree.Entity;
                ISqlBuilder columnsBuilder = this.CreateSqlBuilder(context);
                ISqlBuilder valuesBuilder = this.CreateSqlBuilder(context);

                // 指定插入列
                MemberAccessorCollection memberAccessors = typeRuntime.Members;
                if (tree.EntityColumns != null && tree.EntityColumns.Count > 0)
                {
                    memberAccessors = new MemberAccessorCollection();
                    for (int i = 0; i < tree.EntityColumns.Count; i++)
                    {
                        Expression curExpr = tree.EntityColumns[i];
                        if (curExpr.NodeType == ExpressionType.Lambda) curExpr = (curExpr as LambdaExpression).Body.ReduceUnary();
                        if (curExpr.NodeType != ExpressionType.MemberAccess)
                            throw new XFrameworkException("Can't read field name from expression {0}", tree.EntityColumns[i]);

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

                    columnsBuilder.AppendMember(null, m.Member, typeRuntime.Type);
                    columnsBuilder.Append(',');

                    if (m == typeRuntime.Identity)
                    {
                        seqColumn = column;
                        if (tree.Bulk == null)
                        {
                            // 非批量INSERT，产生一个 OUTPUT 类型的参数
                            string pName = string.Format("{0}{1}{2}", this.ParameterPrefix, AppConst.PARAMETER_NAME_PRIFIX, context.Parameters.Count);
                            var database = context.DbContext.Database;
                            seqParameter = this.DbProvider.CreateParameter(pName, -1, direction: ParameterDirection.Output);
                            context.Parameters.Add(seqParameter);
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
                        var value = m.Invoke(entity);
                        string seg = this.DbResolver.GetSqlValueWidthDefault(value, context, column);
                        valuesBuilder.Append(seg);
                        valuesBuilder.Append(',');
                    }
                }
                columnsBuilder.Length -= 1;
                valuesBuilder.Length -= 1;

                if (tree.Bulk == null)
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
                    if (!tree.Bulk.OnlyValue || seqColumn != null) builder.Append("INSERT ");
                    // 如果有自增列则不使用 INSERT ALL INTO 语法
                    if (!tree.Bulk.OnlyValue && seqColumn == null) builder.Append("ALL ");
                }

                builder.Append("INTO ");
                builder.AppendMember(typeRuntime.TableName, !typeRuntime.IsTemporary);
                builder.Append('(');
                builder.Append(columnsBuilder);
                builder.Append(')');
                builder.AppendNewLine();
                builder.AppendTab();
                builder.Append("VALUES");
                builder.Append('(');
                builder.Append(valuesBuilder);
                builder.Append(')');

                if (tree.Bulk == null) builder.Append(';');
                else
                {
                    if (seqColumn != null)
                    {
                        if (tree.Bulk.IsEndPos) builder.Append(";");
                        else builder.AppendNewLine(";");
                    }
                    else
                    {
                        builder.AppendNewLine();
                        if (tree.Bulk.IsEndPos) builder.Append("SELECT 1 FROM DUAL;");
                    }
                }
            }
            else if (tree.SelectTree != null)
            {
                builder.Append("INSERT INTO ");
                builder.AppendMember(typeRuntime.TableName, !typeRuntime.IsTemporary);
                builder.Append('(');

                var srcDbExpressionType = context.DbExpressionType;
                var srcIsOutQuery = context.IsOutQuery;
                context.DbExpressionType = DbExpressionType.Insert;
                context.IsOutQuery = false;

                var cmd = this.TranslateSelectCommand(tree.SelectTree, 0, false, context) as DbSelectCommand;

                context.DbExpressionType = srcDbExpressionType;
                context.IsOutQuery = srcIsOutQuery;

                int i = 0;
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

            var result = new DbRawCommand(builder.ToString(), builder.TranslateContext != null ? builder.TranslateContext.Parameters : null, System.Data.CommandType.Text);
            return result;
        }

        /// <summary>
        /// 创建 DELETE 命令
        /// </summary>
        /// <param name="tree">查询语义</param>
        /// <param name="context">解析SQL命令上下文</param>
        /// <returns></returns>
        protected override DbRawCommand TranslateDeleteCommand<T>(DbQueryDeleteTree tree, ITranslateContext context)
        {
            ISqlBuilder builder = this.CreateSqlBuilder(context);
            var typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo<T>();

            builder.Append("DELETE FROM ");
            builder.AppendMember(typeRuntime.TableName, !typeRuntime.IsTemporary);
            builder.Append(" t0 ");

            if (tree.Entity != null)
            {
                if (typeRuntime.KeyMembers == null || typeRuntime.KeyMembers.Count == 0)
                    throw new XFrameworkException("Delete<T>(T value) require entity must have key column.");

                object entity = tree.Entity;

                builder.AppendNewLine();
                builder.Append("WHERE ");

                foreach (var m in typeRuntime.KeyMembers)
                {
                    var column = m.Column;
                    var value = m.Invoke(entity);
                    var seg = this.DbResolver.GetSqlValue(value, context, column);

                    builder.AppendMember("t0", m.Member, typeRuntime.Type);
                    builder.Append(" = ");
                    builder.Append(seg);
                    builder.Append(" AND ");
                }
                builder.Length -= 5;
            }
            else if (tree.SelectTree != null)
            {
                // 解析查询用来确定是否需要嵌套
                var cmd = this.TranslateSelectCommand(tree.SelectTree, 0, false, this.CreateTranslateContext(context.DbContext)) as DbSelectCommand;
                if ((cmd.NavMembers != null && cmd.NavMembers.Count > 0) || tree.SelectTree.Joins.Count > 0)
                {
                    // 最外层仅选择 RowID 列
                    var outQuery = tree.SelectTree;
                    outQuery.Select = new DbExpression(DbExpressionType.Select, Expression.Constant("t0.RowId", typeof(string)));
                    var iterator = outQuery;
                    while (iterator.Subquery != null)
                    {
                        var subQuery = new OracleDbQuerySelectTree(iterator.Subquery);
                        iterator.Subquery = subQuery;
                        iterator = subQuery;
                    }

                    // 解析成 RowId IN 结构
                    cmd = (DbSelectCommand)this.TranslateSelectCommand(tree.SelectTree, 1, false, context);
                    builder.Append("WHERE t0.RowId IN(");
                    builder.AppendNewLine(cmd.CommandText);
                    builder.Append(')');
                }
                else
                {
                    TableAliasResolver aliasResolver = this.PrepareTableAlias(tree.SelectTree, context.AliasPrefix);
                    var visitor = new JoinExpressionVisitor(aliasResolver, tree.SelectTree.Joins);
                    visitor.Write(builder);

                    var visitor_ = new WhereExpressionVisitor(aliasResolver, tree.SelectTree.Where);
                    visitor_.Write(builder);
                }
            }

            builder.Append(';');
            return new DbRawCommand(builder.ToString(), builder.TranslateContext != null ? builder.TranslateContext.Parameters : null, System.Data.CommandType.Text);
        }

        /// <summary>
        /// 创建 UPDATE 命令
        /// </summary>
        /// <param name="tree">查询语义</param>
        /// <param name="context">解析SQL命令上下文</param>
        /// <returns></returns>
        protected override DbRawCommand TranslateUpdateCommand<T>(DbQueryUpdateTree tree, ITranslateContext context)
        {
            ISqlBuilder builder = this.CreateSqlBuilder(context);
            var typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo<T>();

            builder.Append("UPDATE ");
            builder.AppendMember(typeRuntime.TableName, !typeRuntime.IsTemporary);
            builder.Append(" t0 SET");
            builder.AppendNewLine();

            if (tree.Entity != null)
            {
                object entity = tree.Entity;
                ISqlBuilder whereBuilder = this.CreateSqlBuilder(context);
                bool useKey = false;
                int length = 0;


                foreach (var m in typeRuntime.Members)
                {
                    var column = m.Column;
                    if (column != null && column.IsIdentity) goto gotoLabel; // Fix issue# 自增列同时又是主键
                    if (column != null && column.NoMapped) continue;
                    if (m.ForeignKey != null) continue;
                    if (m.Member.MemberType == System.Reflection.MemberTypes.Method) continue;

                    builder.AppendMember(null, m.Member, typeRuntime.Type);
                    builder.Append(" = ");

                gotoLabel:

                    if (column == null || !column.IsIdentity)
                    {
                        var value = m.Invoke(entity);
                        var seg = this.DbResolver.GetSqlValueWidthDefault(value, context, column);

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
                    var seg = this.DbResolver.GetSqlValueWidthDefault(value, context, column);
                    index += 1;

                    whereBuilder.AppendMember(null, m.Member, typeRuntime.Type);
                    whereBuilder.Append(" = ");
                    whereBuilder.Append(seg);
                    if (index < typeRuntime.KeyMembers.Count - 1) whereBuilder.Append(" AND ");
                }

                builder.Length = length;
                builder.AppendNewLine();
                builder.Append("WHERE ");
                builder.Append(whereBuilder);

            }
            else if (tree.Expression != null)
            {
                // SELECT 表达式
                LambdaExpression lambda = tree.Expression as LambdaExpression;
                var body = lambda.Body;
                Expression expression = null;
                if (body.NodeType == ExpressionType.MemberInit)
                {
                    var initExpression = body as MemberInitExpression;
                    var bindings = new List<MemberBinding>(initExpression.Bindings);
                    foreach (var m in typeRuntime.KeyMembers)
                    {
                        var member = Expression.MakeMemberAccess(lambda.Parameters[0], m.Member);
                        var binding = Expression.Bind(m.Member, member);
                        if (!bindings.Any(x => x.Member == m.Member)) bindings.Add(binding);
                    }
                    expression = Expression.MemberInit(initExpression.NewExpression, bindings);
                }
                else if (body.NodeType == ExpressionType.New)
                {
                    var newExpression = body as NewExpression;
                    var bindings = new List<MemberBinding>();
                    for (int i = 0; i < newExpression.Members.Count; i++)
                    {
                        var m = typeRuntime.Members.FirstOrDefault(a => a.Name == newExpression.Members[i].Name || a.Column != null && a.Column.Name == newExpression.Members[i].Name);
                        if (m == null) throw new XFrameworkException("There is no property named {0}.{1}.", typeRuntime.Type.Name, newExpression.Members[i].Name);
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

                    var newExpression2 = Expression.New(typeRuntime.Constructor.Member);
                    expression = Expression.MemberInit(newExpression2, bindings);
                }

                // 解析查询以确定是否需要嵌套
                tree.SelectTree.Select = new DbExpression(DbExpressionType.Select, expression);
                var cmd = (DbSelectCommand)this.TranslateSelectCommand(tree.SelectTree, 0, false, this.CreateTranslateContext(context.DbContext));//, token);

                if (tree.SelectTree.Joins.Count > 0 || (cmd.NavMembers != null && cmd.NavMembers.Count > 0))
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

                    cmd = (DbSelectCommand)this.TranslateSelectCommand(tree.SelectTree, 1, false, context);
                    builder.AppendNewLine(cmd.CommandText);
                    builder.Append(") t1 ON (");
                    foreach (var m in typeRuntime.KeyMembers)
                    {
                        builder.AppendMember("t0", m.Member, typeRuntime.Type);
                        builder.Append(" = ");
                        builder.AppendMember("t1", m.Member, typeRuntime.Type);
                        builder.Append(" AND ");
                    }
                    builder.Length -= 5;
                    builder.Append(')');

                    // UPDATE
                    builder.AppendNewLine();
                    builder.AppendNewLine("WHEN MATCHED THEN UPDATE SET ");

                    // SET 字段
                    var visitor = new OracleUpdateExpressionVisitor(null, tree.Expression);
                    visitor.Write(builder);
                }
                else
                {
                    // 直接 SQL 的 UPDATE 语法
                    TableAliasResolver aliasResolver = this.PrepareTableAlias(tree.SelectTree, context.AliasPrefix);
                    var visitor = new UpdateExpressionVisitor(aliasResolver, tree.Expression);
                    visitor.Write(builder);

                    var visitor_ = new WhereExpressionVisitor(aliasResolver, tree.SelectTree.Where);
                    visitor_.Write(builder);
                }
            }

            builder.Append(';');
            return new DbRawCommand(builder.ToString(), builder.TranslateContext != null ? builder.TranslateContext.Parameters : null, System.Data.CommandType.Text);
        }
    }
}