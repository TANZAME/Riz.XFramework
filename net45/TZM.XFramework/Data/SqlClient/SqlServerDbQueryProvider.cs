
using System;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq.Expressions;
using System.Collections.Generic;

namespace TZM.XFramework.Data.SqlClient
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
        public override DbProviderFactory DbProviderFactory { get { return SqlClientFactory.Instance; } }

        /// <summary>
        /// SQL值片断生成器
        /// </summary>
        public override DbValue DbValue { get { return SqlServerDbValue.Instance; } }

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
                return "Sql";
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
        private SqlServerDbQueryProvider() : base()
        {
        }

        /// <summary>
        /// 创建 SQL 构造器
        /// </summary>
        /// <param name="token">参数列表，NULL 或者 Parameters=NULL 时表示不使用参数化</param>
        /// <returns></returns>
        public override ISqlBuilder CreateSqlBuilder(ResolveToken token)
        {
            return new SqlServerSqlBuilder(this, token);
        }

        /// <summary>
        /// 创建方法表达式访问器
        /// </summary>
        /// <param name="visitor">表达式访问器</param>
        /// <returns></returns>
        public override MethodCallExpressionVisitor CreateMethodVisitor(ExpressionVisitorBase visitor)
        {
            return new SqlServerMethodCallExressionVisitor(this, visitor);
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

            // 导航属性中有1:n关系，只统计主表
            // 例：AccountList = a.Client.AccountList,
            var subQuery = dbQuery.Subquery as IDbQueryableInfo_Select;
            if (dbQuery.HasMany && subQuery != null && subQuery.Aggregate != null) dbQuery = subQuery;

            bool useStatis = dbQuery.Aggregate != null;
            // 没有聚合函数或者使用 'Skip' 子句，则解析OrderBy
            // 导航属性如果使用嵌套，除非有 TOP 或者 OFFSET 子句，否则不能用ORDER BY
            string alias0 = token != null && !string.IsNullOrEmpty(token.AliasPrefix) ? (token.AliasPrefix + "0") : "t0";
            bool useSubQuery = dbQuery.HasDistinct || dbQuery.GroupBy != null || dbQuery.Skip > 0 || dbQuery.Take > 0;
            bool useOrderBy = (!useStatis || dbQuery.Skip > 0) && !dbQuery.HasAny && (!dbQuery.IsParsedByMany || (dbQuery.Skip > 0 || dbQuery.Take > 0));

            var context = (SqlServerDbContext)token.DbContext;
            TableAliasCache aliases = this.PrepareTableAlias(dbQuery, token);
            var result = new SqlServerMapperCommand(context, aliases, token) { HasMany = dbQuery.HasMany };
            ISqlBuilder jf = result.JoinFragment;
            ISqlBuilder wf = result.WhereFragment;

            jf.Indent = indent;

            #region 嵌套查询

            if (useStatis && useSubQuery)
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
            }

            #endregion 嵌套查询

            #region 选择子句

            // SELECT 子句
            if (jf.Indent > 0) jf.AppendNewLine();
            if (dbQuery.HasAny)
            {
                jf.Append("IF EXISTS(");
                indent += 1;
                jf.Indent = indent;
                jf.AppendNewLine();
            }

            jf.Append("SELECT ");

            if (useStatis && !useSubQuery)
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
                // TOP 子句
                if (dbQuery.Take > 0 && dbQuery.Skip == 0) jf.AppendFormat("TOP({0})", this.DbValue.GetSqlValue(dbQuery.Take, token));
                // Any
                if (dbQuery.HasAny) jf.Append("TOP 1 1");

                #region 字段

                if (!dbQuery.HasAny)
                {
                    // SELECT 范围
                    var visitor_ = new ColumnExpressionVisitor(this, aliases, dbQuery);
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
                if (context.NoLock && !string.IsNullOrEmpty(this._widthNoLock)) jf.Append(this._widthNoLock);
            }

            // LEFT<INNER> JOIN 子句
            ExpressionVisitorBase visitor = new SqlServerJoinExpressionVisitor(context, aliases, dbQuery.Joins);
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

            #endregion 顺序解析

            #region 分页查询

            if (dbQuery.Skip > 0)
            {
                if (dbQuery.OrderBys.Count == 0) throw new XFrameworkException("The method 'OrderBy' must be called before 'Skip'.");
                wf.AppendNewLine();
                wf.Append("OFFSET ");
                wf.Append(this.DbValue.GetSqlValue(dbQuery.Skip, token));
                wf.Append(" ROWS");

                if (dbQuery.Take > 0)
                {
                    wf.Append(" FETCH NEXT ");
                    wf.Append(this.DbValue.GetSqlValue(dbQuery.Take, token));
                    wf.Append(" ROWS ONLY ");
                }
            }

            #endregion 分页查询

            #region 嵌套查询

            if (useStatis && useSubQuery)
            {
                result.CombineFragments();
                indent -= 1;
                jf.Indent = indent;
                jf.AppendNewLine();
                jf.Append(") ");
                jf.Append(alias0);
                jf.Append(' ');
            }

            #endregion 嵌套查询

            #region 嵌套导航

            if (dbQuery.HasMany && subQuery.Aggregate == null && subQuery != null && subQuery.OrderBys.Count > 0 && !(subQuery.Skip > 0 || subQuery.Take > 0))
            {
                // TODO Include 从表，没分页，OrderBy 报错
                result.CombineFragments();
                visitor = new OrderByExpressionVisitor(this, aliases, subQuery.OrderBys);
                visitor.Write(jf);
            }

            #endregion 嵌套导航

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
                    RawCommand cmd = this.ResolveSelectCommand(dbQuery.Unions[index], indent, isOuter, token);
                    jf.Append(cmd.CommandText);
                }
            }

            #endregion 并集查询

            #region Any 子句

            // 'Any' 子句
            if (dbQuery.HasAny)
            {
                result.CombineFragments();
                indent -= 1;
                jf.Indent = indent;
                jf.AppendNewLine();
                jf.Append(") SELECT 1 ELSE SELECT 0");
            }

            #endregion Any 子句

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
            TableAliasCache aliases = new TableAliasCache();
            ISqlBuilder builder = this.CreateSqlBuilder(token);
            var typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo<T>();

            if (dbQuery.Entity != null)
            {
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
                            throw new XFrameworkException("ERR {0}: Only support MemberAccess expression.", dbQuery.EntityColumns[i]);

                        var member = curExpr as MemberExpression;
                        string name = member.Member.Name;
                        memberAccessors[name] = typeRuntime.Members[name];
                    }
                }

                foreach (var m in memberAccessors)
                {
                    var column = m.Column;
                    if (column != null && column.NoMapped) continue;
                    if (m.ForeignKey != null) continue;
                    if (m.Column != null && column.DbType is SqlDbType && (SqlDbType)column.DbType == SqlDbType.Timestamp) continue; // 行版本号
                    if (m.Member.MemberType == System.Reflection.MemberTypes.Method) continue;

                    if (m != typeRuntime.Identity)
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
                    builder.AppendTab();
                    builder.Append("VALUES");
                }

                builder.Append('(');
                builder.Append(seg_Values);
                builder.Append(')');
                if (dbQuery.Bulk != null && !dbQuery.Bulk.IsEndPos) builder.Append(",");

                if (dbQuery.Bulk == null && typeRuntime.Identity != null)
                {
                    builder.AppendNewLine();
                    builder.Append("SELECT SCOPE_IDENTITY()");
                    builder.AppendAs(Constant.AUTO_INCREMENT_NAME);
                }
            }
            else if (dbQuery.Query != null)
            {
                builder.Append("INSERT INTO ");
                builder.AppendMember(typeRuntime.TableName, !typeRuntime.IsTemporary);
                builder.Append('(');

                int i = 0;
                var cmd = this.ResolveSelectCommand(dbQuery.Query, 0, true, token) as MappingCommand;
                foreach (DbColumn column in cmd.PickColumns)
                {
                    builder.AppendMember(column.NewName);
                    if (i < cmd.PickColumns.Count - 1) builder.Append(',');
                    i++;
                }

                builder.Append(')');
                builder.AppendNewLine();
                builder.Append(cmd.CommandText);
            }

            return new RawCommand(builder.ToString(), builder.Token != null ? builder.Token.Parameters : null, System.Data.CommandType.Text);
        }

        /// <summary>
        /// 创建 DELETE 命令
        /// </summary>
        /// <param name="dbQuery">查询语义</param>
        /// <param name="token">解析上下文</param>
        /// <returns></returns>
        protected override RawCommand ResolveDeleteCommand<T>(IDbQueryableInfo_Delete dbQuery, ResolveToken token)
        {
            var context = (SqlServerDbContext)token.DbContext;
            ISqlBuilder builder = this.CreateSqlBuilder(token);
            var typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo<T>();

            builder.Append("DELETE t0 FROM ");
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
                TableAliasCache aliases = this.PrepareTableAlias(dbQuery.Query, token);
                var cmd = new SqlServerMapperCommand(context, aliases, token) { HasMany = dbQuery.Query.HasMany };

                ExpressionVisitorBase visitor = new SqlServerJoinExpressionVisitor(context, aliases, dbQuery.Query.Joins);
                visitor.Write(cmd.JoinFragment);

                visitor = new WhereExpressionVisitor(this, aliases, dbQuery.Query.Where);
                visitor.Write(cmd.WhereFragment);
                cmd.AddNavMembers(visitor.NavMembers);

                builder.Append(cmd.CommandText);
            }

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
            var context = (SqlServerDbContext)token.DbContext;
            ISqlBuilder builder = this.CreateSqlBuilder(token);
            var typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo<T>();

            builder.Append("UPDATE t0 SET");
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
                    if (m.Column != null && column.DbType is SqlDbType && (SqlDbType)column.DbType == SqlDbType.Timestamp) continue; // 行版本号
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

                if (!useKey) throw new XFrameworkException("Update<T>(T value) require entity must have key column.");

                builder.Length = length;
                seg_Where.Length -= 5;

                builder.AppendNewLine();
                builder.Append("FROM ");
                builder.AppendMember(typeRuntime.TableName, !typeRuntime.IsTemporary);
                builder.Append(" t0");

                builder.AppendNewLine();
                builder.Append("WHERE ");
                builder.Append(seg_Where);
            }
            else if (dbQuery.Expression != null)
            {
                TableAliasCache aliases = this.PrepareTableAlias(dbQuery.Query, token);
                ExpressionVisitorBase visitor = null;
                visitor = new UpdateExpressionVisitor(this, aliases, dbQuery.Expression);
                visitor.Write(builder);

                builder.AppendNewLine();
                builder.Append("FROM ");
                builder.AppendMember(typeRuntime.TableName, !typeRuntime.IsTemporary);
                builder.AppendAs("t0");

                var cmd = new SqlServerMapperCommand(context, aliases, token) { HasMany = dbQuery.Query.HasMany };

                visitor = new SqlServerJoinExpressionVisitor(context, aliases, dbQuery.Query.Joins);
                visitor.Write(cmd.JoinFragment);

                visitor = new WhereExpressionVisitor(this, aliases, dbQuery.Query.Where);
                visitor.Write(cmd.WhereFragment);
                cmd.AddNavMembers(visitor.NavMembers);

                builder.Append(cmd.CommandText);
            }

            return new RawCommand(builder.ToString(), builder.Token != null ? builder.Token.Parameters : null, System.Data.CommandType.Text);
        }
    }
}