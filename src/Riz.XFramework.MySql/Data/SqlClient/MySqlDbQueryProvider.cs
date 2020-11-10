
using System.Data;
using System.Linq.Expressions;

using System.Data.Common;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace Riz.XFramework.Data.SqlClient
{
    /// <summary>
    /// MYSQL 数据查询提供者
    /// </summary>
    internal sealed class MySqlDbQueryProvider : DbQueryProvider
    {
        /// <summary>
        /// 查询语义提供者实例
        /// </summary>
        public static MySqlDbQueryProvider Instance = new MySqlDbQueryProvider();

        /// <summary>
        /// 数据源类的提供程序实现的实例
        /// </summary>
        public override DbProviderFactory DbProvider { get { return MySqlClientFactory.Instance; } }

        /// <summary>
        /// SQL字段值解析器
        /// </summary>
        public override DbValueResolver DbResolver { get { return MySqlDbValueResolver.Instance; } }

        /// <summary>
        /// SQL值片断生成器
        /// </summary>
        public override TypeDeserializerImpl TypeDeserializerImpl { get { return TypeDeserializerImpl.Instance; } }

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
        private MySqlDbQueryProvider()
            : base()
        {

        }

        /// <summary>
        /// 创建解析命令上下文
        /// </summary>
        /// <param name="context">当前查询上下文</param>
        /// <returns></returns>
        public override ITranslateContext CreateTranslateContext(IDbContext context)
        {
            return new MySqlTranslateContext(context);
        }

        /// <summary>
        /// 创建方法表达式访问器
        /// </summary>
        /// <param name="visitor">表达式访问器</param>
        /// <returns></returns>
        public override MethodCallExpressionVisitor CreateMethodCallVisitor(LinqExpressionVisitor visitor)
        {
            return new MySqlMethodCallExressionVisitor(visitor);
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
            var cmd = (DbSelectCommand)this.ResolveSelectCommandImpl(tree, indent, isOutQuery, context);
            cmd.CombineFragments();
            if (isOutQuery) cmd.JoinFragment.Append(';');
            return cmd;
        }

        DbRawCommand ResolveSelectCommandImpl(DbQuerySelectTree tree, int indent, bool isOutQuery, ITranslateContext context)
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
            ISqlBuilder sf = null;

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
                    var visitor_ = new ColumnExpressionVisitor(aliasResolver, tree);
                    if (tree.Skip > 0 && tree.Take == 0)
                    {
                        sf = this.CreateSqlBuilder(context);
                        sf.Indent = jf.Indent + 1;
                        visitor_.Write(sf);
                    }
                    else visitor_.Write(jf);

                    result.PickColumns = visitor_.PickColumns;
                    result.PickColumnText = visitor_.PickColumnText;
                    result.PickNavDescriptors = visitor_.PickNavDescriptors;
                    result.AddNavMembers(visitor_.NavMembers);

                    if (sf != null)
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
                        jf.Indent = indent;
                        jf.AppendNewLine();
                        jf.Append("SELECT");
                        jf.Append(sf);
                        jf.Append(',');
                        jf.AppendNewLine();

                        if (tree.OrderBys.Count == 0) throw new XFrameworkException("The method 'OrderBy' must be called before 'Skip'.");
                        jf.Append("ROW_NUMBER() OVER(");
                        var visitor3 = new OrderByExpressionVisitor(aliasResolver, tree.OrderBys, tree.GroupBy);
                        visitor3.Write(jf, false);
                        result.AddNavMembers(visitor3.NavMembers);
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
            if (tree.Subquery != null)
            {
                // 子查询
                jf.Append("(");
                var cmd = this.ResolveSelectCommandImpl(tree.Subquery, indent + 1, false, context);
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
            if (tree.OrderBys.Count > 0 && useOrderBy)// && !groupByPaging)
            {
                visitor = new OrderByExpressionVisitor(aliasResolver, tree.OrderBys, tree.GroupBy);
                visitor.Write(wf);
                result.AddNavMembers(visitor.NavMembers);
            }

            #endregion

            #region 分页查询

            // LIMIT 子句可以被用于强制 SELECT 语句返回指定的记录数。
            // LIMIT 接受一个或两个数字参数。参数必须是一个整数常量。如果给定两个参数，第一个参数指定第一个返回记录行的偏移量，第二个参数指定返回记录行的最大数目。
            // 初始记录行的偏移量是 0(而不是 1)： 为了与 PostgreSQL 兼容，MySQL 也支持句法： LIMIT # OFFSET #。
            // Limit n,-1 语法不支持，使用ROW_Number()语法代替

            if (tree.Take > 0)
            {
                wf.AppendNewLine().AppendFormat("LIMIT {0}", this.DbResolver.GetSqlValue(tree.Take, context));
                wf.AppendFormat(" OFFSET {0}", this.DbResolver.GetSqlValue(tree.Skip, context));
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
                    DbRawCommand cmd = this.ResolveSelectCommandImpl(tree.Unions[index], indent, isOutQuery, context);
                    jf.Append(cmd.CommandText);
                }
            }

            #endregion

            #region 分页查询

            if (sf != null)
            {
                // 合并 WHERE
                result.CombineFragments();

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
                    jf.AppendNewLine();
                    jf.Append("LIMIT 0,1");
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
        /// 创建 INSERT 命令
        /// </summary>
        /// <param name="tree">查询语义</param>
        /// <param name="context">解析SQL命令上下文</param>
        /// <returns></returns>
        protected override DbRawCommand TranslateInsertCommand<T>(DbQueryInsertTree tree, ITranslateContext context)
        {
            ISqlBuilder builder = this.CreateSqlBuilder(context);
            var typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo<T>();

            if (tree.Entity != null)
            {
                object entity = tree.Entity;
                ISqlBuilder columnsBuilder = this.CreateSqlBuilder(context);
                ISqlBuilder valuesBuilder = this.CreateSqlBuilder(context);

                // 指定插入列
                var members = typeRuntime.Members;
                if (tree.EntityColumns != null && tree.EntityColumns.Count > 0)
                {
                    members = new MemberAccessorCollection();
                    for (int i = 0; i < tree.EntityColumns.Count; i++)
                    {
                        Expression curExpr = tree.EntityColumns[i];
                        if (curExpr.NodeType == ExpressionType.Lambda) curExpr = (curExpr as LambdaExpression).Body.ReduceUnary();
                        if (curExpr.NodeType != ExpressionType.MemberAccess)
                            throw new XFrameworkException("Can't read field name from expression {0}", tree.EntityColumns[i]);

                        MemberExpression member = curExpr as MemberExpression;
                        string name = member.Member.Name;
                        members[name] = typeRuntime.Members[name];
                    }
                }

                foreach (var item in members)
                {
                    var m = item as FieldAccessorBase;
                    if (m == null || !m.IsDbField) continue;

                    if (m != typeRuntime.Identity)
                    {
                        columnsBuilder.AppendMember(null, m.Member, typeRuntime.Type);
                        columnsBuilder.Append(',');

                        var value = m.Invoke(entity);
                        string sqlExpression = this.DbResolver.GetSqlValueWidthDefault(value, context, m.Column);
                        valuesBuilder.Append(sqlExpression);
                        valuesBuilder.Append(',');
                    }
                }
                columnsBuilder.Length -= 1;
                valuesBuilder.Length -= 1;

                if (tree.Bulk == null || !tree.Bulk.OnlyValue)
                {
                    builder.Append("INSERT INTO ");
                    builder.AppendMember(typeRuntime.TableName, !typeRuntime.IsTemporary);
                    builder.Append('(');
                    builder.Append(columnsBuilder);
                    builder.Append(')');
                    builder.AppendNewLine();
                    builder.AppendTab();
                    builder.Append("VALUES");
                }

                builder.Append('(');
                builder.Append(valuesBuilder);
                builder.Append(')');
                if (tree.Bulk != null && !tree.Bulk.IsEndPos) builder.Append(",");

                if (tree.Bulk == null && typeRuntime.Identity != null)
                {
                    builder.Append(';');
                    builder.AppendNewLine();
                    builder.Append("SELECT LAST_INSERT_ID()");
                    builder.AppendAs(AppConst.AUTO_INCREMENT_NAME);
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
                context.IsOutQuery = true;

                var cmd = this.ResolveSelectCommandImpl(tree.SelectTree, 0, true, context) as DbSelectCommand;

                context.DbExpressionType = srcDbExpressionType;
                context.IsOutQuery = srcIsOutQuery;

                int index = 0;
                foreach (var column in cmd.PickColumns)
                {
                    builder.AppendMember(column.NewName);
                    if (index < cmd.PickColumns.Count - 1) builder.Append(',');
                    index++;
                }

                builder.Append(')');
                builder.AppendNewLine();
                builder.Append(cmd.CommandText);
            }

            if (tree.Bulk == null || tree.Bulk.IsEndPos) builder.Append(';');
            return new DbRawCommand(builder.ToString(), builder.TranslateContext != null ? builder.TranslateContext.Parameters : null, System.Data.CommandType.Text);
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

            builder.Append("DELETE t0 FROM ");
            builder.AppendMember(typeRuntime.TableName, !typeRuntime.IsTemporary);
            builder.Append(" t0 ");

            if (tree.Entity != null)
            {
                if (typeRuntime.KeyMembers == null || typeRuntime.KeyMembers.Count == 0)
                    throw new XFrameworkException("Delete<T>(T value) require entity must have key column.");

                object entity = tree.Entity;

                builder.AppendNewLine();
                builder.Append("WHERE ");

                foreach (FieldAccessorBase m in typeRuntime.KeyMembers)
                {
                    var value = m.Invoke(entity);
                    var sqlExpression = this.DbResolver.GetSqlValue(value, context, m.Column);

                    builder.AppendMember("t0", m.Member, typeRuntime.Type);
                    builder.Append(" = ");
                    builder.Append(sqlExpression);
                    builder.Append(" AND ");
                }
                builder.Length -= 5;
            }
            else if (tree.SelectTree != null)
            {
                TableAliasResolver aliasResolver = this.PrepareTableAlias(tree.SelectTree, context.AliasPrefix);
                var cmd = new DbSelectCommand(context, aliasResolver);
                cmd.HasMany = tree.SelectTree.HasMany;

                // 标记当前解析上下文是删除语句产生的
                var isDelete = ((MySqlTranslateContext)context).IsDelete;
                if (context != null)
                    ((MySqlTranslateContext)context).IsDelete = true;

                LinqExpressionVisitor visitor = new JoinExpressionVisitor(aliasResolver, tree.SelectTree.Joins);
                visitor.Write(cmd.JoinFragment);

                visitor = new WhereExpressionVisitor(aliasResolver, tree.SelectTree.Where);
                visitor.Write(cmd.WhereFragment);
                cmd.AddNavMembers(visitor.NavMembers);

                builder.Append(cmd.CommandText);

                // 恢复当前解析上下文的删除标记
                if (context != null)
                    ((MySqlTranslateContext)context).IsDelete = isDelete;
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
            builder.Append(" t0");

            if (tree.Entity != null)
            {
                object entity = tree.Entity;
                ISqlBuilder whereBuilder = this.CreateSqlBuilder(context);
                bool useKey = false;
                int length = 0;
                builder.AppendNewLine(" SET");

                foreach (var item in typeRuntime.Members)
                {
                    var m = item as FieldAccessorBase;
                    if (m == null || !m.IsDbField) continue;
                    if (m.Column != null && m.Column.IsIdentity) goto LABEL; // Fix issue# 自增列同时又是主键

                    builder.AppendMember("t0", m.Member, typeRuntime.Type);
                    builder.Append(" = ");

                    LABEL:
                    var value = m.Invoke(entity);
                    var seg = this.DbResolver.GetSqlValueWidthDefault(value, context, m.Column);

                    if (m.Column == null || !m.Column.IsIdentity)
                    {
                        builder.Append(seg);
                        length = builder.Length;
                        builder.Append(',');
                        builder.AppendNewLine();
                    }

                    if (m.Column != null && m.Column.IsKey)
                    {
                        useKey = true;
                        whereBuilder.AppendMember("t0", m.Member, typeRuntime.Type);
                        whereBuilder.Append(" = ");
                        whereBuilder.Append(seg);
                        whereBuilder.Append(" AND ");
                    }
                }

                if (!useKey) throw new XFrameworkException("Update<T>(T value) require T must have key column.");

                builder.Length = length;
                whereBuilder.Length -= 5;

                builder.AppendNewLine();
                builder.Append("WHERE ");
                builder.Append(whereBuilder);

            }
            else if (tree.Expression != null)
            {
                TableAliasResolver aliasResolver = this.PrepareTableAlias(tree.SelectTree, context.AliasPrefix);
                LinqExpressionVisitor visitor = null;

                var cmd = new DbSelectCommand(context, aliasResolver);
                cmd.HasMany = tree.SelectTree.HasMany;

                visitor = new JoinExpressionVisitor(aliasResolver, tree.SelectTree.Joins);
                visitor.Write(cmd.JoinFragment);

                cmd.WhereFragment.AppendNewLine();
                cmd.WhereFragment.AppendNewLine("SET");
                visitor = new UpdateExpressionVisitor(aliasResolver, tree.Expression);
                visitor.Write(cmd.WhereFragment);

                visitor = new WhereExpressionVisitor(aliasResolver, tree.SelectTree.Where);
                visitor.Write(cmd.WhereFragment);
                cmd.AddNavMembers(visitor.NavMembers);

                builder.Append(cmd.CommandText);
            }

            builder.Append(';');
            return new DbRawCommand(builder.ToString(), builder.TranslateContext != null ? builder.TranslateContext.Parameters : null, System.Data.CommandType.Text);
        }
    }
}
