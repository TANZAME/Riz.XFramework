
using System.Data;
using System.Linq.Expressions;

using System.Data.Common;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace TZM.XFramework.Data.SqlClient
{
    /// <summary>
    /// 数据查询提供者
    /// </summary>
    /// <remarks>
    public sealed class MySqlDbQueryProvider : DbQueryProvider
    {
        /// <summary>
        /// 查询语义提供者实例
        /// </summary>
        public static MySqlDbQueryProvider Instance = new MySqlDbQueryProvider();

        /// <summary>
        /// 数据源类的提供程序实现的实例
        /// </summary>
        public override DbProviderFactory DbProviderFactory { get { return MySqlClientFactory.Instance; } }

        /// <summary>
        /// SQL字段值生成器
        /// </summary>
        public override DbValue DbValue { get { return MySqlDbValue.Instance; } }

        /// <summary>
        /// 数据库安全字符 左
        /// </summary>
        public override string QuotePrefix
        {
            get
            {
                return "`";
            }
        }

        /// <summary>
        /// 数据库安全字符 右
        /// </summary>
        public override string QuoteSuffix
        {
            get
            {
                return "`";
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
                return "MySql";
            }
        }

        /// <summary>
        /// 命令参数前缀
        /// </summary>
        public override string ParameterPrefix
        {
            get
            {
                return "?"; // mssql@ mysql? oracel:
            }
        }

        /// <summary>
        /// 实例化 <see cref="MySqlDbQueryProvider"/> 类的新实例
        /// </summary>
        private MySqlDbQueryProvider() : base()
        {

        }

        /// <summary>
        /// 创建 SQL 构造器
        /// </summary>
        /// <param name="token">参数列表，NULL 或者 parameter=NULL 时表示不使用参数化</param>
        /// <returns></returns>
        public override ISqlBuilder CreateSqlBuilder(ResolveToken token)
        {
            return new MySqlSqlBuilder(this, token);
        }

        /// <summary>
        /// 创建方法表达式访问器
        /// </summary>
        /// <returns></returns>
        public override MethodCallExpressionVisitor CreateMethodVisitor(ExpressionVisitorBase visitor)
        {
            return new MySqlMethodCallExressionVisitor(this, visitor);
        }

        // 创建 SELECT 命令
        protected override Command ResolveSelectCommand<T>(DbQueryableInfo_Select<T> dbQuery, int indent, bool isOuter, ResolveToken token)
        {
            var cmd = (MapperCommand)this.ResolveSelectCommandImpl(dbQuery, indent, isOuter, token);
            cmd.CombineFragments();
            if (isOuter) cmd.JoinFragment.Append(';');
            return cmd;
        }

        // 创建 SELECT 命令
        Command ResolveSelectCommandImpl<T>(DbQueryableInfo_Select<T> dbQuery, int indent, bool isOuter, ResolveToken token)
        {
            // 说明：
            // 1.OFFSET 前必须要有 'ORDER BY'，即 'Skip' 子句前必须使用 'OrderBy' 子句
            // 2.在有聚合函数的<MAX,MIN...>情况下，如果有 'Distinct' 'GroupBy' 'Skip' 'Take' 子句，则需要使用嵌套查询
            // 3.'Any' 子句将翻译成 IF EXISTS...
            // 4.分组再分页时需要使用嵌套查询，此时子查询不需要 'OrderBy' 子句，但最外层则需要
            // 5.'Skip' 'Take' 子句视为语义结束符，在其之后的子句将使用嵌套查询
            // 6.导航属性中有 1:n 关系的，需要使用嵌套查询，否则分页查询会有问题
            // 8.如果只有 Skip 没有 Take，则使用 Row_Number() Over()分页语法，其它使用 LIMIT ## OFFSET 语法


            // 导航属性中有1:n关系，只统计主表
            // 例：AccountList = a.Client.AccountList,
            var subQuery = dbQuery.SubQueryInfo as DbQueryableInfo_Select<T>;
            if (dbQuery.HasMany && subQuery != null && subQuery.Aggregate != null) dbQuery = subQuery;

            bool useStatis = dbQuery.Aggregate != null;
            // 没有聚合函数或者使用 'Skip' 子句，则解析OrderBy
            // 导航属性如果使用嵌套，除非有 TOP 或者 OFFSET 子句，否则不能用ORDER BY
            string alias0 = token != null && !string.IsNullOrEmpty(token.AliasPrefix) ? (token.AliasPrefix + "0") : "t0";
            bool useSubQuery = dbQuery.HasDistinct || dbQuery.GroupBy != null || dbQuery.Skip > 0 || dbQuery.Take > 0;
            bool useOrderBy = (!useStatis || dbQuery.Skip > 0) && !dbQuery.HasAny && (!dbQuery.SubQueryOfMany || (dbQuery.Skip > 0 || dbQuery.Take > 0));

            TableAliasCache aliases = this.PrepareTableAlias<T>(dbQuery, token);
            MapperCommand cmd = new MapperCommand(this, aliases, token) { HasMany = dbQuery.HasMany };
            ISqlBuilder jf = cmd.JoinFragment;
            ISqlBuilder wf = cmd.WhereFragment;
            ISqlBuilder sf = null;

            jf.Indent = indent;

            #region 嵌套查询

            if (useStatis && useSubQuery)
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

            if (useStatis && !useSubQuery)
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
                    var visitor2 = new ColumnExpressionVisitor(this, aliases, dbQuery);
                    if (dbQuery.Skip > 0 && dbQuery.Take == 0)
                    {
                        sf = this.CreateSqlBuilder(token);
                        sf.Indent = jf.Indent + 1;
                        visitor2.Write(sf);
                    }
                    else visitor2.Write(jf);

                    cmd.PickColumns = visitor2.PickColumns;
                    cmd.PickColumnText = visitor2.PickColumnText;
                    cmd.Navigations = visitor2.Navigations;
                    cmd.AddNavMembers(visitor2.NavMembers);

                    if (sf != null)
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
                        jf.Indent = indent;
                        jf.AppendNewLine();
                        jf.Append("SELECT");
                        jf.Append(sf);
                        jf.Append(',');
                        jf.AppendNewLine();

                        if (dbQuery.OrderBys.Count == 0) throw new XFrameworkException("The method 'OrderBy' must be called before 'Skip'.");
                        jf.Append("ROW_NUMBER() OVER(");
                        var visitor3 = new OrderByExpressionVisitor(this, aliases, dbQuery.OrderBys, dbQuery.GroupBy);
                        visitor3.Write(jf, false);
                        cmd.AddNavMembers(visitor3.NavMembers);
                        jf.Append(") Row_Number0");
                    }
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
                jf.Append("(");
                var cmd2 = this.ResolveSelectCommandImpl<T>(dbQuery.SubQueryInfo as DbQueryableInfo_Select<T>, indent + 1, false, token);
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
            if (dbQuery.OrderBys.Count > 0 && useOrderBy)// && !groupByPaging)
            {
                visitor = new OrderByExpressionVisitor(this, aliases, dbQuery.OrderBys, dbQuery.GroupBy);
                visitor.Write(wf);
                cmd.AddNavMembers(visitor.NavMembers);
            }

            #endregion

            #region 分页查询

            // LIMIT 子句可以被用于强制 SELECT 语句返回指定的记录数。
            // LIMIT 接受一个或两个数字参数。参数必须是一个整数常量。如果给定两个参数，第一个参数指定第一个返回记录行的偏移量，第二个参数指定返回记录行的最大数目。
            // 初始记录行的偏移量是 0(而不是 1)： 为了与 PostgreSQL 兼容，MySQL 也支持句法： LIMIT # OFFSET #。
            // Limit n,-1 语法不支持，使用ROW_Number()语法代替

            if (dbQuery.Take > 0)
            {
                wf.AppendNewLine().AppendFormat("LIMIT {0}", this.DbValue.GetSqlValue(dbQuery.Take, token));
                wf.AppendFormat(" OFFSET {0}", this.DbValue.GetSqlValue(dbQuery.Skip, token));
            }

            #endregion

            #region 嵌套查询

            if (useStatis && useSubQuery)
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
                    Command cmd2 = this.ResolveSelectCommandImpl<T>(dbQuery.Unions[index] as DbQueryableInfo_Select<T>, indent, isOuter, token);
                    jf.Append(cmd2.CommandText);
                }
            }

            #endregion

            #region 分页查询

            if (sf != null)
            {
                // 合并 WHERE
                cmd.CombineFragments();

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
                    jf.AppendNewLine();
                    jf.Append("LIMIT 0,1");
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
            TableAliasCache aliases = new TableAliasCache();
            ISqlBuilder builder = this.CreateSqlBuilder(token);
            TypeRuntimeInfo typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo<T>();

            if (dbQuery.Entity != null)
            {
                object entity = dbQuery.Entity;
                ISqlBuilder seg_Columns = this.CreateSqlBuilder(token);
                ISqlBuilder seg_Values = this.CreateSqlBuilder(token);

                // 指定插入列
                MemberAccessorCollection memberAssessors = typeRuntime.MemberAccessors;
                if (dbQuery.EntityColumns != null && dbQuery.EntityColumns.Count > 0)
                {
                    memberAssessors = new MemberAccessorCollection();
                    for (int i = 0; i < dbQuery.EntityColumns.Count; i++)
                    {
                        Expression curExpr = dbQuery.EntityColumns[i];
                        if (curExpr.NodeType == ExpressionType.Lambda) curExpr = (curExpr as LambdaExpression).Body.ReduceUnary();
                        if (curExpr.NodeType != ExpressionType.MemberAccess)
                            throw new XFrameworkException("Can't read field name from expression {0}", dbQuery.EntityColumns[i]);

                        MemberExpression member = curExpr as MemberExpression;
                        string name = member.Member.Name;
                        memberAssessors[name] = typeRuntime.MemberAccessors[name];
                    }
                }

                foreach (var m in memberAssessors)
                {
                    var column = m.Column;
                    if (column != null && column.NoMapped) continue;
                    if (m.ForeignKey != null) continue;
                    if (m.Member.MemberType == System.Reflection.MemberTypes.Method) continue;

                    if (m != dbQuery.AutoIncrement)
                    {
                        seg_Columns.AppendMember(m.Member.Name);
                        seg_Columns.Append(',');

                        var value = m.Invoke(entity);
                        string seg = this.DbValue.GetSqlValueWidthDefault(value, token, column);
                        seg_Values.Append(seg);
                        seg_Values.Append(',');
                    }
                }
                seg_Columns.Length -= 1;
                seg_Values.Length -= 1;

                if (dbQuery.Bulk == null || !dbQuery.Bulk.OnlyValue)
                {
                    builder.Append("INSERT INTO ");
                    builder.AppendMember(typeRuntime.TableName, !typeRuntime.IsTemporary);
                    builder.Append('(');
                    builder.Append(seg_Columns);
                    builder.Append(')');
                    builder.AppendNewLine();
                    builder.AppendNewTab();
                    builder.Append("VALUES");
                }

                builder.Append('(');
                builder.Append(seg_Values);
                builder.Append(')');
                if (dbQuery.Bulk != null && !dbQuery.Bulk.IsEndPos) builder.Append(",");

                if (dbQuery.Bulk == null && dbQuery.AutoIncrement != null)
                {
                    builder.Append(';');
                    builder.AppendNewLine();
                    builder.Append("SELECT LAST_INSERT_ID()");
                    builder.AppendAs(Constant.AUTOINCREMENTNAME);
                }
            }
            else if (dbQuery.SelectInfo != null)
            {
                builder.Append("INSERT INTO ");
                builder.AppendMember(typeRuntime.TableName, !typeRuntime.IsTemporary);
                builder.Append('(');

                int i = 0;
                MapperCommand cmd2 = this.ResolveSelectCommandImpl(dbQuery.SelectInfo, 0, true, token) as MapperCommand;
                foreach (var column in cmd2.PickColumns)
                {
                    builder.AppendMember(column.NewName);
                    if (i < cmd2.PickColumns.Count - 1) builder.Append(',');
                    i++;
                }

                builder.Append(')');
                builder.AppendNewLine();
                builder.Append(cmd2.CommandText);
            }

            if (dbQuery.Bulk == null || dbQuery.Bulk.IsEndPos) builder.Append(';');
            return new Command(builder.ToString(), builder.Token != null ? builder.Token.Parameters : null, System.Data.CommandType.Text);
        }

        // 创建 DELETE 命令
        protected override Command ResolveDeleteCommand<T>(DbQueryableInfo_Delete<T> dbQuery, ResolveToken token)
        {
            ISqlBuilder builder = this.CreateSqlBuilder(token);
            TypeRuntimeInfo typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo<T>();

            builder.Append("DELETE t0 FROM ");
            builder.AppendMember(typeRuntime.TableName, !typeRuntime.IsTemporary);
            builder.Append(" t0 ");

            if (dbQuery.Entity != null)
            {
                if (typeRuntime.KeyAccessors == null || typeRuntime.KeyAccessors.Count == 0)
                    throw new XFrameworkException("Delete<T>(T value) require entity must have key column.");

                object entity = dbQuery.Entity;

                builder.AppendNewLine();
                builder.Append("WHERE ");

                foreach (var m in typeRuntime.KeyAccessors)
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
            else if (dbQuery.SelectInfo != null)
            {
                TableAliasCache aliases = this.PrepareTableAlias<T>(dbQuery.SelectInfo, token);
                var cmd2 = new MapperCommand(this, aliases, token) { HasMany = dbQuery.SelectInfo.HasMany };
                if (token != null && token.Extendsions == null)
                {
                    token.Extendsions = new Dictionary<string, object>();
                    if (!token.Extendsions.ContainsKey("MySqlDelete")) token.Extendsions.Add("MySqlDelete", null);
                }

                ExpressionVisitorBase visitor = new JoinExpressionVisitor(this, aliases, dbQuery.SelectInfo.Joins);
                visitor.Write(cmd2.JoinFragment);

                visitor = new WhereExpressionVisitor(this, aliases, dbQuery.SelectInfo.Condtion);
                visitor.Write(cmd2.WhereFragment);
                cmd2.AddNavMembers(visitor.NavMembers);

                builder.Append(cmd2.CommandText);
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
            builder.Append(" t0");

            if (dbQuery.Entity != null)
            {
                object entity = dbQuery.Entity;
                ISqlBuilder seg_Where = this.CreateSqlBuilder(token);
                bool useKey = false;
                int length = 0;
                builder.AppendNewLine(" SET");

                foreach (var m in typeRuntime.MemberAccessors)
                {
                    var column = m.Column;
                    if (column != null && column.IsIdentity) goto gotoLabel; // fix issue# 自增列同时又是主键
                    if (column != null && column.NoMapped) continue;
                    if (m.ForeignKey != null) continue;
                    if (m.Member.MemberType == System.Reflection.MemberTypes.Method) continue;

                    builder.AppendMember("t0", m.Member.Name);
                    builder.Append(" = ");

                gotoLabel:
                    var value = m.Invoke(entity);
                    var seg = this.DbValue.GetSqlValueWidthDefault(value, token, column);

                    if (column == null || !column.IsIdentity)
                    {
                        builder.Append(seg);
                        length = builder.Length;
                        builder.Append(',');
                        builder.AppendNewLine();
                    }

                    if (column != null && column.IsKey)
                    {
                        useKey = true;
                        seg_Where.AppendMember("t0", m.Member.Name);
                        seg_Where.Append(" = ");
                        seg_Where.Append(seg);
                        seg_Where.Append(" AND ");
                    }
                }

                if (!useKey) throw new XFrameworkException("Update<T>(T value) require T must have key column.");

                builder.Length = length;
                seg_Where.Length -= 5;

                builder.AppendNewLine();
                builder.Append("WHERE ");
                builder.Append(seg_Where);

            }
            else if (dbQuery.Expression != null)
            {
                TableAliasCache aliases = this.PrepareTableAlias<T>(dbQuery.SelectInfo, token);
                ExpressionVisitorBase visitor = null;

                var cmd2 = new MapperCommand(this, aliases, token) { HasMany = dbQuery.SelectInfo.HasMany };

                visitor = new JoinExpressionVisitor(this, aliases, dbQuery.SelectInfo.Joins);
                visitor.Write(cmd2.JoinFragment);

                cmd2.WhereFragment.AppendNewLine();
                cmd2.WhereFragment.AppendNewLine("SET");
                visitor = new UpdateExpressionVisitor(this, aliases, dbQuery.Expression);
                visitor.Write(cmd2.WhereFragment);

                visitor = new WhereExpressionVisitor(this, aliases, dbQuery.SelectInfo.Condtion);
                visitor.Write(cmd2.WhereFragment);
                cmd2.AddNavMembers(visitor.NavMembers);

                builder.Append(cmd2.CommandText);
            }

            builder.Append(';');
            return new Command(builder.ToString(), builder.Token != null ? builder.Token.Parameters : null, System.Data.CommandType.Text);
        }
    }
}
