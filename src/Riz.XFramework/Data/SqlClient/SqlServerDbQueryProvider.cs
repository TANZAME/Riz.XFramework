
using System;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.Linq;

namespace Riz.XFramework.Data.SqlClient
{
    /// <summary>
    /// 查询语义提供者，用来构建、解析查询语义
    /// </summary>
    public sealed class SqlServerDbQueryProvider : DbQueryProvider
    {
        // 无阻塞模式
        private string _widthNoLock = "WITH (NOLOCK)";

        /// <summary>
        /// 查询语义提供者实例
        /// </summary>
        public static SqlServerDbQueryProvider Instance = new SqlServerDbQueryProvider();

        /// <summary>
        /// 数据源类的提供程序实现的实例
        /// </summary>
        public override DbProviderFactory DbProvider => SqlClientFactory.Instance;

        /// <summary>
        /// SQL值片断生成器
        /// </summary>
        protected internal override DbConstor Constor => SqlServerDbConstor.Instance;

        /// <summary>
        /// 实体转换映射委托生成器
        /// </summary>
        protected internal override TypeDeserializerImpl TypeDeserializerImpl => TypeDeserializerImpl.Instance;

        /// <summary>
        /// 无阻塞 WITH(NOLOCK)
        /// </summary>
        public string WidthNoLock => _widthNoLock;

        /// <summary>
        /// 数据库安全字符 左
        /// </summary>
        public override string QuotePrefix => "[";

        /// <summary>
        /// 数据库安全字符 右
        /// </summary>
        public override string QuoteSuffix => "]";

        /// <summary>
        /// 字符串引号
        /// </summary>
        public override string SingleQuoteChar => "'";

        /// <summary>
        /// 数据查询提供者 名称
        /// </summary>
        public override string ProviderName => "SqlServer";

        /// <summary>
        /// 命令参数前缀
        /// </summary>
        public override string ParameterPrefix => "@";

        /// <summary>
        /// 实例化 <see cref="SqlServerDbQueryProvider"/> 类的新实例
        /// </summary>
        private SqlServerDbQueryProvider()
            : base()
        {
        }

        /// <summary>
        /// 创建方法表达式访问器
        /// </summary>
        /// <param name="visitor">表达式访问器</param>
        /// <returns></returns>
        protected internal override MethodCallExpressionVisitor CreateMethodCallVisitor(DbExpressionVisitor visitor) => new SqlServerMethodCallExressionVisitor(visitor);

        /// <summary>
        /// 解析 SELECT 命令
        /// </summary>
        /// <param name="tree">查询语义</param>
        /// <param name="indent">缩进</param>
        /// <param name="isOutermost">是否外层查询，内层查询不需要结束符(;)</param>
        /// <param name="context">解析上下文</param>
        /// <returns></returns>
        protected override DbRawCommand TranslateSelectCommand(DbQuerySelectTree tree, int indent, bool isOutermost, ITranslateContext context)
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

            var srcDbExpressionType = context.CurrentExpressionType;
            var srcIsOutermost = context.CurrentIsOutermost;
            if (srcDbExpressionType == null)
                context.CurrentExpressionType = DbExpressionType.Select;
            if (srcIsOutermost == null || !isOutermost)
                context.CurrentIsOutermost = isOutermost;

            bool useAggregate = tree.Aggregate != null;
            // 没有聚合函数或者使用 'Skip' 子句，则解析OrderBy
            // 导航属性如果使用嵌套，除非有 TOP 或者 OFFSET 子句，否则不能用ORDER BY
            // 第一层的表别名
            string alias = context != null && !string.IsNullOrEmpty(context.AliasPrefix) ? (context.AliasPrefix + "0") : "t0";
            bool useSubquery = tree.HasDistinct || tree.GroupBy != null || tree.Skip > 0 || tree.Take > 0;
            bool useOrderBy = !tree.HasAny && (!useAggregate || tree.Skip > 0);// && (!tree.SelectHasMany || (tree.Skip > 0 || tree.Take > 0));

            AliasGenerator ag = this.PrepareTableAlias(tree, context != null ? context.AliasPrefix : null);
            var result = new SqlServerDbSelectCommand(context, ag, tree.SelectHasMany);

            ISqlBuilder jf = result.JoinFragment;
            ISqlBuilder wf = result.WhereFragment;

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
                context.CurrentIsOutermost = false;
            }

            #endregion 嵌套查询

            #region 选择子句

            // SELECT 子句
            if (jf.Indent > 0) jf.AppendNewLine();
            if (tree.HasAny)
            {
                jf.Append("IF EXISTS(");
                indent += 1;
                jf.Indent = indent;
                jf.AppendNewLine();
            }

            jf.Append("SELECT ");

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
                // TOP 子句
                if (tree.Take > 0 && tree.Skip == 0) jf.AppendFormat("TOP({0})", this.Constor.GetSqlValue(tree.Take, context));
                // Any
                if (tree.HasAny) jf.Append("TOP 1 1");

                #region 字段

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

                #endregion 字段
            }

            #endregion 选择子句

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
            else if (tree.FromSql != null)
            {
                if (tree.FromSql.DbContext == null)
                    tree.FromSql.DbContext = context.DbContext;
                DbRawSql rawSql = tree.FromSql;

                // 解析参数
                object[] args = null;
                if (rawSql.Parameters != null)
                    args = rawSql.Parameters.Select(x => this.Constor.GetSqlValue(x, context)).ToArray();
                string sql = rawSql.CommandText;
                if (args != null && args.Length > 0) 
                    sql = string.Format(sql, args);

                // 子查询
                jf.Append('(');
                var cmd = new DbRawCommand(sql, context.Parameters, CommandType.Text);
                jf.Append(cmd.CommandText);
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

                bool isNoLock = ((SqlServerDbContext)context.DbContext).NoLock;
                if (isNoLock && !string.IsNullOrEmpty(this._widthNoLock) && !typeRuntime.IsTemporary) jf.Append(this._widthNoLock);
            }

            // LEFT<INNER> JOIN 子句
            if (tree.Joins != null)
            {
                var visitor = new SqlServerJoinExpressionVisitor(ag, jf);
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

            if (tree.Skip > 0)
            {
                if (tree.OrderBys == null || tree.OrderBys.Count == 0) throw new XFrameworkException("The method 'OrderBy' must be called before 'Skip'.");
                wf.AppendNewLine();
                wf.Append("OFFSET ");
                wf.Append(this.Constor.GetSqlValue(tree.Skip, context));
                wf.Append(" ROWS");

                if (tree.Take > 0)
                {
                    wf.Append(" FETCH NEXT ");
                    wf.Append(this.Constor.GetSqlValue(tree.Take, context));
                    wf.Append(" ROWS ONLY ");
                }
            }

            #endregion 分页查询

            #region 嵌套查询

            if (useAggregate && useSubquery)
            {
                result.CombineFragments();
                indent -= 1;
                jf.Indent = indent;
                jf.AppendNewLine();
                jf.Append(") ");
                jf.Append(alias);
                jf.Append(' ');
            }

            #endregion 嵌套查询

            #region 嵌套导航

            if (tree.SelectHasMany && subquery.Aggregate == null &&
                subquery != null && subquery.OrderBys != null && subquery.OrderBys.Count > 0 && !(subquery.Skip > 0 || subquery.Take > 0))
            {
                // TODO Include 从表，没分页，OrderBy 报错
                result.CombineFragments();
                var visitor = new OrderByExpressionVisitor(ag, jf, null, null);
                visitor.Visit(subquery.OrderBys);
            }

            #endregion 嵌套导航

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
                    DbRawCommand cmd = this.TranslateSelectCommand(tree.Unions[index], indent, isOutermost, context);
                    jf.Append(cmd.CommandText);
                }
            }

            #endregion 并集查询

            #region Any 子句

            // 'Any' 子句
            if (tree.HasAny)
            {
                result.CombineFragments();
                indent -= 1;
                jf.Indent = indent;
                jf.AppendNewLine();
                jf.Append(") SELECT 1 ELSE SELECT 0");
            }

            #endregion Any 子句

            #region 还原状态

            context.CurrentExpressionType = srcDbExpressionType;
            context.CurrentIsOutermost = srcIsOutermost;

            #endregion

            return result;
        }

        /// <summary>
        /// 创建 INSRT 命令
        /// </summary>
        /// <param name="tree">查询语义</param>
        /// <param name="context">解析上下文</param>
        /// <returns></returns>
        protected override DbRawCommand TranslateInsertCommand<T>(DbQueryInsertTree tree, ITranslateContext context)
        {
            // 增删改不用 NOLOCK
            bool noLock = ((SqlServerDbContext)context.DbContext).NoLock;
            ((SqlServerDbContext)context.DbContext).NoLock = false;

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
                        Expression expression = tree.EntityColumns[i];
                        if (expression.NodeType == ExpressionType.Lambda) expression = (expression as LambdaExpression).Body.ReduceUnary();
                        if (expression.NodeType != ExpressionType.MemberAccess)
                            throw new XFrameworkException("ERR {0}: Only support MemberAccess expression.", tree.EntityColumns[i]);

                        var member = expression as MemberExpression;
                        string name = member.Member.Name;
                        members[name] = typeRuntime.Members[name];
                    }
                }

                foreach (var item in members)
                {
                    var m = item as FieldAccessorBase;
                    if (m == null || !m.IsDbField) continue;
                    // 忽略行版本号列
                    if (m.Column != null && m.Column.DbType is SqlDbType && (SqlDbType)m.Column.DbType == SqlDbType.Timestamp) continue;

                    if (item != typeRuntime.Identity)
                    {
                        columnsBuilder.AppendMember(null, item.Member, typeRuntime.Type);
                        columnsBuilder.Append(',');

                        var value = item.Invoke(entity);
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

                    if (tree.Bulk != null && typeRuntime.Identity != null)
                    {
                        builder.AppendNewLine();
                        builder.Append("OUTPUT INSERTED.");

                        string memberName = TypeUtils.GetFieldName(typeRuntime.Identity.Member, typeRuntime.Type);
                        builder.AppendMember(memberName);
                        builder.AppendAs(AppConst.AUTO_INCREMENT_NAME);
                    }

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
                    builder.AppendNewLine();
                    builder.Append("SELECT SCOPE_IDENTITY()");
                    builder.AppendAs(AppConst.AUTO_INCREMENT_NAME);
                }
            }
            else if (tree.Select != null)
            {
                builder.Append("INSERT INTO ");
                builder.AppendTable(typeRuntime.TableSchema, typeRuntime.TableName, typeRuntime.IsTemporary);
                builder.Append('(');

                var srcDbExpressionType = context.CurrentExpressionType;
                var srcIsOutermost = context.CurrentIsOutermost;
                context.CurrentExpressionType = DbExpressionType.Insert;
                context.CurrentIsOutermost = true;

                var cmd = this.TranslateSelectCommand(tree.Select, 0, true, context) as DbSelectCommand;

                context.CurrentExpressionType = srcDbExpressionType;
                context.CurrentIsOutermost = srcIsOutermost;

                int index = 0;
                foreach (ColumnDescriptor column in cmd.SelectedColumns)
                {
                    builder.AppendMember(column.NewName);
                    if (index < cmd.SelectedColumns.Count - 1) builder.Append(',');
                    index++;
                }

                builder.Append(')');
                builder.AppendNewLine();
                builder.Append(cmd.CommandText);
            }

            ((SqlServerDbContext)context.DbContext).NoLock = noLock;

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
            // 增删改不用 NOLOCK
            bool noLock = ((SqlServerDbContext)context.DbContext).NoLock;
            ((SqlServerDbContext)context.DbContext).NoLock = false;

            ISqlBuilder builder = this.CreateSqlBuilder(context);
            var typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo<T>();

            builder.Append("DELETE t0 FROM ");
            builder.AppendTable(typeRuntime.TableSchema, typeRuntime.TableName, typeRuntime.IsTemporary);
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
                    var value = m.Invoke(entity);
                    var sqlExpression = this.Constor.GetSqlValue(value, context, ((FieldAccessorBase)m).Column);

                    builder.AppendMember("t0", m.Member, typeRuntime.Type);
                    builder.Append(" = ");
                    builder.Append(sqlExpression);
                    builder.Append(" AND ");
                }
                builder.Length -= 5;
            }
            else if (tree.Select != null)
            {
                AliasGenerator ag = this.PrepareTableAlias(tree.Select, context.AliasPrefix);
                var cmd = new SqlServerDbSelectCommand(context, ag, tree.Select.SelectHasMany);

                if (tree.Select.Joins != null)
                {
                    var visitor = new SqlServerJoinExpressionVisitor(ag, cmd.JoinFragment);
                    visitor.Visit(tree.Select.Joins);
                }

                if (tree.Select.Wheres != null)
                {
                    var visitor = new WhereExpressionVisitor(ag, cmd.WhereFragment);
                    visitor.Visit(tree.Select.Wheres);
                    cmd.AddNavMembers(visitor.NavMembers);
                }

                builder.Append(cmd.CommandText);
            }

            ((SqlServerDbContext)context.DbContext).NoLock = noLock;

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
            // 增删改不用 NOLOCK
            bool noLock = ((SqlServerDbContext)context.DbContext).NoLock;
            ((SqlServerDbContext)context.DbContext).NoLock = false;

            ISqlBuilder builder = this.CreateSqlBuilder(context);
            var typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo<T>();

            builder.Append("UPDATE t0 SET");
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

                    // Fix issue# 自增列同时又是主键
                    if (m.Column != null && m.Column.IsIdentity) goto LABEL;
                    // 忽略行版本号列
                    if (m.Column != null && m.Column.DbType is SqlDbType && (SqlDbType)m.Column.DbType == SqlDbType.Timestamp) continue;

                    builder.AppendMember("t0", m.Member, typeRuntime.Type);
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
                        whereBuilder.AppendMember("t0", m.Member, typeRuntime.Type);
                        whereBuilder.Append(" = ");
                        whereBuilder.Append(sqlExpression);
                        whereBuilder.Append(" AND ");
                    }
                }

                if (!useKey) throw new XFrameworkException("Update<T>(T value) require entity must have key column.");

                builder.Length = length;
                whereBuilder.Length -= 5;

                builder.AppendNewLine();
                builder.Append("FROM ");
                builder.AppendTable(typeRuntime.TableSchema, typeRuntime.TableName, typeRuntime.IsTemporary);
                builder.Append(" t0");

                builder.AppendNewLine();
                builder.Append("WHERE ");
                builder.Append(whereBuilder);
            }
            else if (tree.Expression != null)
            {
                AliasGenerator ag = this.PrepareTableAlias(tree.Select, context.AliasPrefix);
                DbExpressionVisitor visitor = null;
                visitor = new UpdateExpressionVisitor(ag, builder);
                visitor.Visit(tree.Expression);

                builder.AppendNewLine();
                builder.Append("FROM ");
                builder.AppendTable(typeRuntime.TableSchema, typeRuntime.TableName, typeRuntime.IsTemporary);
                builder.AppendAs("t0");

                var cmd = new SqlServerDbSelectCommand(context, ag, tree.Select.SelectHasMany);
                if (tree.Select.Joins != null)
                {
                    visitor = new SqlServerJoinExpressionVisitor(ag, cmd.JoinFragment);
                    visitor.Visit(tree.Select.Joins);
                }

                if (tree.Select.Wheres != null)
                {
                    visitor = new WhereExpressionVisitor(ag, cmd.WhereFragment);
                    visitor.Visit(tree.Select.Wheres);
                    cmd.AddNavMembers(visitor.NavMembers);
                }

                builder.Append(cmd.CommandText);
            }

            ((SqlServerDbContext)context.DbContext).NoLock = noLock;

            return new DbRawCommand(builder.ToString(), builder.TranslateContext != null ? builder.TranslateContext.Parameters : null, System.Data.CommandType.Text);
        }
    }
}