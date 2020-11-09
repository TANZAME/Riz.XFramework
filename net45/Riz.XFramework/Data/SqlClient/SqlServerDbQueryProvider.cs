﻿
using System;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq.Expressions;
using System.Collections.Generic;

namespace Riz.XFramework.Data.SqlClient
{
    /// <summary>
    /// 查询语义提供者，用来构建、解析查询语义
    /// </summary>
    internal sealed class SqlServerDbQueryProvider : DbQueryProvider
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
        public override DbProviderFactory DbProvider { get { return SqlClientFactory.Instance; } }

        /// <summary>
        /// SQL值片断生成器
        /// </summary>
        public override DbValueResolver DbResolver { get { return SqlServerDbValueResolver.Instance; } }

        /// <summary>
        /// 实体转换映射委托生成器
        /// </summary>
        public override TypeDeserializerImpl TypeDeserializerImpl { get { return TypeDeserializerImpl.Instance; } }

        /// <summary>
        /// 无阻塞 WITH(NOLOCK)
        /// </summary>
        public string WidthNoLock
        {
            get { return _widthNoLock; }
        }

        /// <summary>
        /// 数据库安全字符 左
        /// </summary>
        public override string QuotePrefix
        {
            get
            {
                return "[";
            }
        }

        /// <summary>
        /// 数据库安全字符 右
        /// </summary>
        public override string QuoteSuffix
        {
            get
            {
                return "]";
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
                return "SqlServer";
            }
        }

        /// <summary>
        /// 命令参数前缀
        /// </summary>
        public override string ParameterPrefix
        {
            get
            {
                return "@";
            }
        }

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
        public override MethodCallExpressionVisitor CreateMethodCallVisitor(LinqExpressionVisitor visitor)
        {
            return new SqlServerMethodCallExressionVisitor(visitor);
        }

        /// <summary>
        /// 解析 SELECT 命令
        /// </summary>
        /// <param name="tree">查询语义</param>
        /// <param name="indent">缩进</param>
        /// <param name="isOutQuery">是否是最外层查询</param>
        /// <param name="context">解析上下文</param>
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
            var result = new SqlServerSelectCommand(context, aliasResolver);
            result.HasMany = tree.HasMany;

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
                var visitor_ = new AggregateExpressionVisitor(aliasResolver, tree.Aggregate, tree.GroupBy);
                visitor_.Write(jf);
                result.AddNavMembers(visitor_.NavMembers);
            }
            else
            {
                // DISTINCT 子句
                if (tree.HasDistinct) jf.Append("DISTINCT ");
                // TOP 子句
                if (tree.Take > 0 && tree.Skip == 0) jf.AppendFormat("TOP({0})", this.DbResolver.GetSqlValue(tree.Take, context));
                // Any
                if (tree.HasAny) jf.Append("TOP 1 1");

                #region 字段

                if (!tree.HasAny)
                {
                    // SELECT 范围
                    var visitor_ = new ColumnExpressionVisitor(aliasResolver, tree);
                    visitor_.Write(jf);

                    result.PickColumns = visitor_.PickColumns;
                    result.PickColumnText = visitor_.PickColumnText;
                    result.PickNavDescriptors = visitor_.PickNavDescriptors;
                    result.AddNavMembers(visitor_.NavMembers);
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
            else
            {
                var typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo(tree.FromType);
                jf.AppendMember(typeRuntime.TableName, !typeRuntime.IsTemporary);
                jf.Append(' ');
                jf.Append(alias);
                jf.Append(' ');

                bool isNoLock = ((SqlServerDbContext)context.DbContext).NoLock;
                if (isNoLock && !string.IsNullOrEmpty(this._widthNoLock)) jf.Append(this._widthNoLock);
            }

            // LEFT<INNER> JOIN 子句
            LinqExpressionVisitor visitor = new SqlServerJoinExpressionVisitor(aliasResolver, tree.Joins);
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

            #endregion 顺序解析

            #region 分页查询

            if (tree.Skip > 0)
            {
                if (tree.OrderBys.Count == 0) throw new XFrameworkException("The method 'OrderBy' must be called before 'Skip'.");
                wf.AppendNewLine();
                wf.Append("OFFSET ");
                wf.Append(this.DbResolver.GetSqlValue(tree.Skip, context));
                wf.Append(" ROWS");

                if (tree.Take > 0)
                {
                    wf.Append(" FETCH NEXT ");
                    wf.Append(this.DbResolver.GetSqlValue(tree.Take, context));
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

            if (tree.HasMany && subquery.Aggregate == null && subquery != null && subquery.OrderBys.Count > 0 && !(subquery.Skip > 0 || subquery.Take > 0))
            {
                // TODO Include 从表，没分页，OrderBy 报错
                result.CombineFragments();
                visitor = new OrderByExpressionVisitor(aliasResolver, subquery.OrderBys);
                visitor.Write(jf);
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
                    DbRawCommand cmd = this.TranslateSelectCommand(tree.Unions[index], indent, isOutQuery, context);
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

            context.DbExpressionType = srcDbExpressionType;
            context.IsOutQuery = srcIsOutQuery;

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
            else if (tree.SelectTree != null)
            {
                builder.Append("INSERT INTO ");
                builder.AppendMember(typeRuntime.TableName, !typeRuntime.IsTemporary);
                builder.Append('(');

                var srcDbExpressionType = context.DbExpressionType;
                var srcIsOutQuery = context.IsOutQuery;
                context.DbExpressionType = DbExpressionType.Insert;
                context.IsOutQuery = true;

                var cmd = this.TranslateSelectCommand(tree.SelectTree, 0, true, context) as DbSelectCommand;

                context.DbExpressionType = srcDbExpressionType;
                context.IsOutQuery = srcIsOutQuery;

                int index = 0;
                foreach (ColumnDescriptor column in cmd.PickColumns)
                {
                    builder.AppendMember(column.NewName);
                    if (index < cmd.PickColumns.Count - 1) builder.Append(',');
                    index++;
                }

                builder.Append(')');
                builder.AppendNewLine();
                builder.Append(cmd.CommandText);
            }

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

                foreach (var m in typeRuntime.KeyMembers)
                {
                    var value = m.Invoke(entity);
                    var sqlExpression = this.DbResolver.GetSqlValue(value, context, ((FieldAccessorBase)m).Column);

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
                var cmd = new SqlServerSelectCommand(context, aliasResolver);
                cmd.HasMany = tree.SelectTree.HasMany;

                LinqExpressionVisitor visitor = new SqlServerJoinExpressionVisitor(aliasResolver, tree.SelectTree.Joins);
                visitor.Write(cmd.JoinFragment);

                visitor = new WhereExpressionVisitor(aliasResolver, tree.SelectTree.Where);
                visitor.Write(cmd.WhereFragment);
                cmd.AddNavMembers(visitor.NavMembers);

                builder.Append(cmd.CommandText);
            }

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
                    var sqlExpression = this.DbResolver.GetSqlValueWidthDefault(value, context, m.Column);

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
                builder.AppendMember(typeRuntime.TableName, !typeRuntime.IsTemporary);
                builder.Append(" t0");

                builder.AppendNewLine();
                builder.Append("WHERE ");
                builder.Append(whereBuilder);
            }
            else if (tree.Expression != null)
            {
                TableAliasResolver aliasResolver = this.PrepareTableAlias(tree.SelectTree, context.AliasPrefix);
                LinqExpressionVisitor visitor = null;
                visitor = new UpdateExpressionVisitor(aliasResolver, tree.Expression);
                visitor.Write(builder);

                builder.AppendNewLine();
                builder.Append("FROM ");
                builder.AppendMember(typeRuntime.TableName, !typeRuntime.IsTemporary);
                builder.AppendAs("t0");

                var cmd = new SqlServerSelectCommand(context, aliasResolver);
                cmd.HasMany = tree.SelectTree.HasMany;

                visitor = new SqlServerJoinExpressionVisitor(aliasResolver, tree.SelectTree.Joins);
                visitor.Write(cmd.JoinFragment);

                visitor = new WhereExpressionVisitor(aliasResolver, tree.SelectTree.Where);
                visitor.Write(cmd.WhereFragment);
                cmd.AddNavMembers(visitor.NavMembers);

                builder.Append(cmd.CommandText);
            }

            return new DbRawCommand(builder.ToString(), builder.TranslateContext != null ? builder.TranslateContext.Parameters : null, System.Data.CommandType.Text);
        }
    }
}