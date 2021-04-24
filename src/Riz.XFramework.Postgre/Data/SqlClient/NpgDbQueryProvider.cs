
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Collections.Generic;
using Npgsql;

namespace Riz.XFramework.Data.SqlClient
{
    /// <summary>
    /// 数据查询提供者
    /// </summary>
    public sealed class NpgDbQueryProvider : DbQueryProvider
    {
        /// <summary>
        /// 查询语义提供者实例
        /// </summary>
        public static NpgDbQueryProvider Instance = new NpgDbQueryProvider();

        /// <summary>
        /// 数据源类的提供程序实现的实例
        /// </summary>
        public override DbProviderFactory DbProvider => NpgsqlFactory.Instance;

        /// <summary>
        /// 常量值转SQL表达式解析器
        /// </summary>
        protected internal override DbConstor Constor => NpgDbConstor.Instance;

        /// <summary>
        /// 实体转换映射委托生成器
        /// </summary>
        protected internal override TypeDeserializerImpl TypeDeserializerImpl => NpgTypeDeserializerImpl.Instance;

        /// <summary>
        /// 数据库安全字符 左
        /// </summary>
        public override string QuotePrefix => "\"";

        /// <summary>
        /// 数据库安全字符 右
        /// </summary>
        public override string QuoteSuffix => "\"";

        /// <summary>
        /// 字符串引号
        /// </summary>
        public override string SingleQuoteChar => "'";

        /// <summary>
        /// 数据查询提供者 名称
        /// </summary>
        public override string ProviderName => "Postgre";

        /// <summary>
        /// 命令参数前缀
        /// </summary>
        public override string ParameterPrefix => "@";

        /// <summary>
        /// 实例化 <see cref="NpgDbQueryProvider"/> 类的新实例
        /// </summary>
        private NpgDbQueryProvider()
            : base()
        {

        }

        /// <summary>
        /// 创建 SQL 构造器
        /// </summary>
        /// <param name="context">解析SQL命令上下文</param>
        /// <returns></returns>
        protected internal override ISqlBuilder CreateSqlBuilder(ITranslateContext context) => new NpgSqlBuilder(context);

        /// <summary>
        /// 创建方法表达式访问器
        /// </summary>
        /// <returns></returns>
        protected internal override MethodCallExpressionVisitor CreateMethodCallVisitor(DbExpressionVisitor visitor) => new NpgMethodCallExressionVisitor(visitor);

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

        // 创建 SELECT 命令
        DbRawCommand ResolveSelectCommandImpl(DbQuerySelectTree tree, int indent, bool isOutQuery, ITranslateContext context)
        {
            // 说明：
            // 1.OFFSET 前必须要有 'ORDER BY'，即 'Skip' 子句前必须使用 'OrderBy' 子句
            // 2.在有聚合函数的<MAX,MIN...>情况下，如果有 'Distinct' 'GroupBy' 'Skip' 'Take' 子句，则需要使用嵌套查询
            // 3.'Any' 子句将翻译成 IF EXISTS...
            // 4.分组再分页时需要使用嵌套查询，此时子查询不需要 'OrderBy' 子句，但最外层则需要
            // 5.'Skip' 'Take' 子句视为语义结束符，在其之后的子句将使用嵌套查询
            // 6.导航属性中有 1:n 关系的，需要使用嵌套查询，否则分页查询会有问题


            // 导航属性中有1:n关系，只统计主表
            // 例：AccountList = a.Client.AccountList,
            var subquery = tree.Subquery as DbQuerySelectTree;
            if (tree.SelectHasMany && subquery != null && subquery.Aggregate != null) tree = subquery;

            var srcDbExpressionType = context.DbExpressionType;
            var srcIsOutQuery = context.IsOutermostQuery;
            if (srcDbExpressionType == null)
                context.DbExpressionType = DbExpressionType.Select;
            if (srcIsOutQuery == null || !isOutQuery)
                context.IsOutermostQuery = isOutQuery;

            bool useAggregate = tree.Aggregate != null;
            // 没有聚合函数或者使用 'Skip' 子句，则解析OrderBy
            // 导航属性如果使用嵌套，除非有 TOP 或者 OFFSET 子句，否则不能用ORDER BY
            // 第一层的表别名
            string alias = context != null && !string.IsNullOrEmpty(context.AliasPrefix) ? (context.AliasPrefix + "0") : "t0";
            bool useSubquery = tree.HasDistinct || tree.GroupBy != null || tree.Skip > 0 || tree.Take > 0;
            bool useOrderBy = (!useAggregate || tree.Skip > 0) && !tree.HasAny && (!tree.SelectHasMany || (tree.Skip > 0 || tree.Take > 0));

            AliasGenerator ag = this.PrepareTableAlias(tree, context != null ? context.AliasPrefix : null);
            var result = new DbSelectCommand(context, ag, tree.SelectHasMany);

            ISqlBuilder jf = result.JoinFragment;
            ISqlBuilder wf = result.WhereFragment;
            (jf as NpgSqlBuilder).UseQuote = isOutQuery;

            jf.Indent = indent;

            #region 嵌套查询

            if (useAggregate && useSubquery)
            {
                // SELECT
                jf.Append("SELECT ");
                jf.AppendNewLine();

                // SELECT COUNT(1)
                var visitor = new AggregateExpressionVisitor(ag, jf, tree.GroupBy, alias);
                visitor.Visit(tree.Aggregate);
                result.AddNavMembers(visitor.NavMembers);

                // SELECT COUNT(1) FROM
                jf.AppendNewLine();
                jf.Append("FROM ( ");

                indent += 1;
                jf.Indent = indent;
                (jf as NpgSqlBuilder).UseQuote = false;
                context.IsOutermostQuery = false;
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
                var visitor = new AggregateExpressionVisitor(ag, jf, tree.GroupBy, null);
                visitor.Visit(tree.Aggregate);
                result.AddNavMembers(visitor.NavMembers);
            }
            else
            {

                // DISTINCT 子句
                if (tree.HasDistinct) jf.Append("DISTINCT ");

                #region 选择字段

                if (!tree.HasAny)
                {
                    // SELECT 范围
                    var visitor = new ColumnExpressionVisitor(ag, jf, tree);
                    visitor.Visit(tree.Select);

                    result.SelectedColumns = visitor.SelectedColumns;
                    result.SelectedColumnText = visitor.SelectedColumnText;
                    result.SelectedNavs = visitor.SelectedNavDescriptors;
                    result.AddNavMembers(visitor.NavMembers);
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
                DbRawCommand cmd2 = this.ResolveSelectCommandImpl(tree.Subquery, indent + 1, false, context);
                jf.Append(cmd2.CommandText);
                jf.AppendNewLine();
                jf.Append(") ");
                jf.Append(alias);
                jf.Append(' ');
            }
            else
            {
                var typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo(tree.From);
                jf.AppendTable(typeRuntime.TableSchema, typeRuntime.TableName, typeRuntime.IsTemporary);
                jf.Append(' ');
                jf.Append(alias);
                jf.Append(' ');
            }

            // LEFT<INNER> JOIN 子句
            if (tree.Joins != null)
            {
                var visitor = new JoinExpressionVisitor(ag, jf);
                visitor.Visit(tree.Joins);
            }

            wf.Indent = jf.Indent;

            // WHERE 子句
            if (tree.Wheres != null)
            {
                var visitor = new WhereExpressionVisitor(ag, wf);
                visitor.Visit(tree.Wheres);
                result.AddNavMembers(visitor.NavMembers);
            }

            // GROUP BY 子句
            if (tree.GroupBy != null)
            {
                var visitor = new GroupByExpressionVisitor(ag, wf);
                visitor.Visit(tree.GroupBy);
                result.AddNavMembers(visitor.NavMembers);
            }

            // HAVING 子句
            if (tree.Havings != null)
            {
                var visitor = new HavingExpressionVisitor(ag, wf, tree.GroupBy);
                visitor.Visit(tree.Havings);
                result.AddNavMembers(visitor.NavMembers);
            }

            // ORDER 子句
            if (tree.OrderBys != null && tree.OrderBys.Count > 0 && useOrderBy)
            {
                var visitor = new OrderByExpressionVisitor(ag, wf, tree.GroupBy, null);
                visitor.Visit(tree.OrderBys);
                result.AddNavMembers(visitor.NavMembers);
            }

            #endregion

            #region 分页查询

            if (tree.Take > 0) wf.AppendNewLine().AppendFormat("LIMIT {0}", this.Constor.GetSqlValue(tree.Take, context));
            if (tree.Skip > 0) wf.AppendFormat(" OFFSET {0}", this.Constor.GetSqlValue(tree.Skip, context));

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
            if (tree.SelectHasMany && subquery.Aggregate == null &&
                subquery != null && subquery.OrderBys != null && subquery.OrderBys.Count > 0 && !(subquery.Skip > 0 || subquery.Take > 0))
            {
                result.CombineFragments();
                var visitor = new OrderByExpressionVisitor(ag, jf, null, null);
                visitor.Visit(subquery.OrderBys);
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
                    DbRawCommand cmd2 = this.ResolveSelectCommandImpl(tree.Unions[index], indent, isOutQuery, context);
                    jf.Append(cmd2.CommandText);
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
                    jf.Append("LIMIT 1");
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
            context.IsOutermostQuery = srcIsOutQuery;

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
            ISqlBuilder builder = this.CreateSqlBuilder(context);
            var typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo<T>();
            var caseSensitive = context != null && context.DbContext != null ? ((NpgDbContext)context.DbContext).CaseSensitive : false;

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

                        var member = curExpr as MemberExpression;
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
                        string sqlExpression = this.Constor.GetSqlValueWidthDefault(value, context, m.Column);
                        valuesBuilder.Append(sqlExpression);
                        valuesBuilder.Append(',');
                    }
                }
                columnsBuilder.Length -= 1;
                valuesBuilder.Length -= 1;

                if (tree.Bulk == null || !tree.Bulk.OnlyValue)
                {
                    builder.Append("INSERT INTO ");
                    builder.AppendTable(typeRuntime.TableSchema, typeRuntime.TableName, typeRuntime.IsTemporary);
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

                    // sequence，命名原则是 tablename_columnname_seq.
                    builder.AppendFormat("SELECT CURRVAL('{2}{0}_{1}_seq{2}')", typeRuntime.TableName, TypeUtils.GetFieldName(typeRuntime.Identity.Member, typeRuntime.Type), caseSensitive ? "\"" : string.Empty);
                    builder.Append(" AS ");
                    builder.Append(this.QuotePrefix);
                    builder.Append(AppConst.AUTO_INCREMENT_NAME);
                    builder.Append(this.QuoteSuffix);
                }
            }
            else if (tree.Query != null)
            {
                builder.Append("INSERT INTO ");
                builder.AppendTable(typeRuntime.TableSchema, typeRuntime.TableName, typeRuntime.IsTemporary);
                builder.Append('(');

                var srcDbExpressionType = context.DbExpressionType;
                var srcIsOutQuery = context.IsOutermostQuery;
                context.DbExpressionType = DbExpressionType.Insert;
                context.IsOutermostQuery = false;
                var cmd = this.ResolveSelectCommandImpl(tree.Query, 0, false, context) as DbSelectCommand;

                context.DbExpressionType = srcDbExpressionType;
                context.IsOutermostQuery = srcIsOutQuery;

                int index = 0;
                foreach (var column in cmd.SelectedColumns)
                {
                    builder.AppendMember(column.Name);
                    if (index < cmd.SelectedColumns.Count - 1) builder.Append(',');
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

            builder.Append("DELETE FROM ");
            builder.AppendTable(typeRuntime.TableSchema, typeRuntime.TableName, typeRuntime.IsTemporary);
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
                    var sqlExpression = this.Constor.GetSqlValue(value, context, m.Column);

                    builder.AppendMember("t0", m.Member, typeRuntime.Type);
                    builder.Append(" = ");
                    builder.Append(sqlExpression);
                    builder.Append(" AND ");
                }
                builder.Length -= 5;
            }
            else if (tree.Query != null)
            {
                AliasGenerator ag = this.PrepareTableAlias(tree.Query, context.AliasPrefix);
                var cmd = new NpgDbSelectCommand(context, ag, DbExpressionType.Delete, tree.Query.SelectHasMany);

                if (tree.Query.Joins != null)
                {
                    var visitor = new NpgJoinExpressionVisitor(ag, cmd.JoinFragment, DbExpressionType.Delete, cmd);
                    visitor.Visit(tree.Query.Joins);
                }

                if (tree.Query.Wheres != null)
                {
                    var visitor = new NpgWhereExpressionVisitor(ag, cmd.WhereFragment);
                    visitor.Visit(tree.Query.Wheres);
                    cmd.AddNavMembers(visitor.NavMembers);
                }

                builder.Append(cmd.CommandText);
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
            builder.AppendTable(typeRuntime.TableSchema, typeRuntime.TableName, typeRuntime.IsTemporary);
            builder.Append(" t0 SET");
            builder.AppendNewLine();

            if (tree.Entity != null)
            {
                object entity = tree.Entity;
                ISqlBuilder whereBuilder = this.CreateSqlBuilder(context);
                bool useKey = false;
                int length = 0;

                foreach (var item in typeRuntime.Members)
                {
                    var m = item as FieldAccessorBase;
                    if (m == null || !m.IsDbField) continue;

                    if (m.Column != null && m.Column.IsIdentity) goto LABEL; // Fix issue# 自增列同时又是主键

                    builder.AppendMember(null, m.Member, typeRuntime.Type);
                    builder.Append(" = ");

                LABEL:
                    var value = m.Invoke(entity);
                    var sqlExpression = this.Constor.GetSqlValueWidthDefault(value, context, m.Column);

                    if (m.Column == null || !m.Column.IsIdentity)
                    {
                        builder.Append(sqlExpression);
                        length = builder.Length;
                        builder.Append(',');
                        builder.AppendNewLine();
                    }

                    if (m.Column != null && m.Column.IsKey)
                    {
                        useKey = true;
                        whereBuilder.AppendMember(null, m.Member, typeRuntime.Type);
                        whereBuilder.Append(" = ");
                        whereBuilder.Append(sqlExpression);
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
                AliasGenerator ag = this.PrepareTableAlias(tree.Query, context.AliasPrefix);
                DbExpressionVisitor visitor = null;
                visitor = new NpgUpdateExpressionVisitor(ag, builder);
                visitor.Visit(tree.Expression);

                var cmd = new NpgDbSelectCommand(context, ag, DbExpressionType.Update, tree.Query.SelectHasMany);
                if (tree.Query.Joins != null)
                {
                    visitor = new NpgJoinExpressionVisitor(ag, cmd.JoinFragment, DbExpressionType.Update, cmd);
                    visitor.Visit(tree.Query.Joins);
                }

                if (tree.Query.Wheres != null)
                {
                    visitor = new NpgWhereExpressionVisitor(ag, cmd.WhereFragment);
                    visitor.Visit(tree.Query.Wheres);
                    cmd.AddNavMembers(visitor.NavMembers);
                }

                builder.Append(cmd.CommandText);
            }

            builder.Append(';');
            return new DbRawCommand(builder.ToString(), builder.TranslateContext != null ? builder.TranslateContext.Parameters : null, System.Data.CommandType.Text);
        }
    }
}
