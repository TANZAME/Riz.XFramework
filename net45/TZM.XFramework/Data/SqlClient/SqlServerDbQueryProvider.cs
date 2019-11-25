
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
        /// SQL字段值生成器
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
        /// <returns></returns>
        public override MethodCallExpressionVisitor CreateMethodVisitor(ExpressionVisitorBase visitor)
        {
            return new SqlServerMethodCallExressionVisitor(this, visitor);
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

            // 导航属性中有1:n关系，只统计主表
            // 例：AccountList = a.Client.AccountList,
            var subQuery = dbQuery.SubQueryInfo as DbQueryableInfo_Select<T>;
            if (dbQuery.HasMany && subQuery != null && subQuery.Aggregate != null) dbQuery = subQuery;

            bool useStatis = dbQuery.Aggregate != null;
            // 没有聚合函数或者使用 'Skip' 子句，则解析OrderBy
            // 导航属性如果使用嵌套，除非有 TOP 或者 OFFSET 子句，否则不能用ORDER BY
            string alias0 = token != null && !string.IsNullOrEmpty(token.TableAliasName) ? (token.TableAliasName + "0") : "t0";
            bool useSubQuery = dbQuery.HasDistinct || dbQuery.GroupBy != null || dbQuery.Skip > 0 || dbQuery.Take > 0;
            bool useOrderBy = (!useStatis || dbQuery.Skip > 0) && !dbQuery.HasAny && (!dbQuery.SubQueryOfMany || (dbQuery.Skip > 0 || dbQuery.Take > 0));

            IDbQueryable sourceQuery = dbQuery.SourceQuery;
            var context = (SqlServerDbContext)sourceQuery.DbContext;
            TableAliasCache aliases = this.PrepareTableAlias<T>(dbQuery, token);
            MapperCommand cmd = new SqlServerMapperCommand(context, aliases, token) { HasMany = dbQuery.HasMany };
            ISqlBuilder jf = cmd.JoinFragment;
            ISqlBuilder wf = cmd.WhereFragment;

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
                var visitor2 = new AggregateExpressionVisitor(this, aliases, dbQuery.Aggregate, dbQuery.GroupBy);
                visitor2.Write(jf);
                cmd.AddNavMembers(visitor2.NavMembers);
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
                    var visitor2 = new ColumnExpressionVisitor(this, aliases, dbQuery);
                    visitor2.Write(jf);

                    cmd.PickColumns = visitor2.PickColumns;
                    cmd.PickColumnText = visitor2.PickColumnText;
                    cmd.Navigations = visitor2.Navigations;
                    cmd.AddNavMembers(visitor2.NavMembers);
                }

                #endregion 字段
            }

            #endregion 选择子句

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
                if (context.NoLock && !string.IsNullOrEmpty(this._widthNoLock)) jf.Append(this._widthNoLock);
            }

            // LEFT<INNER> JOIN 子句
            ExpressionVisitorBase visitor = new SqlServerJoinExpressionVisitor(context, aliases, dbQuery.Joins);
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
                cmd.CombineFragments();
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
                cmd.CombineFragments();
                visitor = new OrderByExpressionVisitor(this, aliases, subQuery.OrderBys);
                visitor.Write(jf);
            }

            #endregion 嵌套导航

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

            #endregion 并集查询

            #region Any 子句

            // 'Any' 子句
            if (dbQuery.HasAny)
            {
                cmd.CombineFragments();
                indent -= 1;
                jf.Indent = indent;
                jf.AppendNewLine();
                jf.Append(") SELECT 1 ELSE SELECT 0");
            }

            #endregion Any 子句

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
                            throw new XFrameworkException("ERR {0}: Only support MemberAccess expression.", dbQuery.EntityColumns[i]);

                        MemberExpression member = curExpr as MemberExpression;
                        string name = member.Member.Name;
                        invokers[name] = typeRuntime.Invokers[name];
                    }
                }

                foreach (var invoker in invokers)
                {
                    var column = invoker.Column;
                    if (column != null && column.NoMapped) continue;
                    if (invoker.ForeignKey != null) continue;
                    if (invoker.Column != null && column.DbType is SqlDbType && (SqlDbType)column.DbType == SqlDbType.Timestamp) continue; // 行版本号
                    if (invoker.Member.MemberType == System.Reflection.MemberTypes.Method) continue;

                    if (invoker != dbQuery.AutoIncrement)
                    {
                        columnsBuilder.AppendMember(invoker.Member.Name);
                        columnsBuilder.Append(',');

                        var value = invoker.Invoke(entity);
                        string seg = this.DbValue.GetSqlValueWidthDefault(value, token, column);
                        valuesBuilder.Append(seg);
                        valuesBuilder.Append(',');
                    }
                }

                columnsBuilder.Length -= 1;
                valuesBuilder.Length -= 1;

                if (dbQuery.Bulk == null || !dbQuery.Bulk.OnlyValue)
                {
                    builder.Append("INSERT INTO ");
                    builder.AppendMember(typeRuntime.TableName, !typeRuntime.IsTemporary);
                    builder.Append('(');
                    builder.Append(columnsBuilder);
                    builder.Append(')');
                    builder.AppendNewLine();
                    builder.AppendNewTab();
                    builder.Append("VALUES");
                }

                builder.Append('(');
                builder.Append(valuesBuilder);
                builder.Append(')');
                if (dbQuery.Bulk != null && !dbQuery.Bulk.IsEndPos) builder.Append(",");

                if (dbQuery.Bulk == null && dbQuery.AutoIncrement != null)
                {
                    builder.AppendNewLine();
                    builder.Append("SELECT SCOPE_IDENTITY()");
                    builder.AppendAs(Constant.AUTOINCREMENTNAME);
                }
            }
            else if (dbQuery.SelectInfo != null)
            {
                builder.Append("INSERT INTO ");
                builder.AppendMember(typeRuntime.TableName, !typeRuntime.IsTemporary);
                builder.Append('(');

                int i = 0;
                MapperCommand cmd2 = this.ResolveSelectCommand(dbQuery.SelectInfo, 0, true, token) as MapperCommand;
                foreach (Column column in cmd2.PickColumns)
                {
                    builder.AppendMember(column.NewName);
                    if (i < cmd2.PickColumns.Count - 1) builder.Append(',');
                    i++;
                }

                builder.Append(')');
                builder.AppendNewLine();
                builder.Append(cmd2.CommandText);
            }

            return new Command(builder.ToString(), builder.Token != null ? builder.Token.Parameters : null, System.Data.CommandType.Text);
        }

        // 创建 DELETE 命令
        protected override Command ResolveDeleteCommand<T>(DbQueryableInfo_Delete<T> dbQuery, ResolveToken token)
        {
            ISqlBuilder builder = this.CreateSqlBuilder(token);
            TypeRuntimeInfo typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo<T>();
            IDbQueryable sourceQuery = dbQuery.SourceQuery;
            var context = (SqlServerDbContext)sourceQuery.DbContext;

            builder.Append("DELETE t0 FROM ");
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
                TableAliasCache aliases = this.PrepareTableAlias<T>(dbQuery.SelectInfo, token);
                var cmd2 = new SqlServerMapperCommand(context, aliases, token) { HasMany = dbQuery.SelectInfo.HasMany };

                ExpressionVisitorBase visitor = new SqlServerJoinExpressionVisitor(context, aliases, dbQuery.SelectInfo.Joins);
                visitor.Write(cmd2.JoinFragment);

                visitor = new WhereExpressionVisitor(this, aliases, dbQuery.SelectInfo.Condtion);
                visitor.Write(cmd2.WhereFragment);
                cmd2.AddNavMembers(visitor.NavMembers);

                builder.Append(cmd2.CommandText);
            }

            return new Command(builder.ToString(), builder.Token != null ? builder.Token.Parameters : null, System.Data.CommandType.Text);
        }

        // 创建 UPDATE 命令
        protected override Command ResolveUpdateCommand<T>(DbQueryableInfo_Update<T> dbQuery, ResolveToken token)
        {
            ISqlBuilder builder = this.CreateSqlBuilder(token);
            var typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo<T>();
            IDbQueryable sourceQuery = dbQuery.SourceQuery;
            var context = (SqlServerDbContext)sourceQuery.DbContext;

            builder.Append("UPDATE t0 SET");
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
                    if (invoker.Column != null && column.DbType is SqlDbType && (SqlDbType)column.DbType == SqlDbType.Timestamp) continue; // 行版本号
                    if (invoker.ForeignKey != null) continue;
                    if (invoker.Member.MemberType == System.Reflection.MemberTypes.Method) continue;

                    builder.AppendMember("t0", invoker.Member.Name);
                    builder.Append(" = ");

                gotoLabel:
                    var value = invoker.Invoke(entity);
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
                        whereBuilder.AppendMember("t0", invoker.Member.Name);
                        whereBuilder.Append(" = ");
                        whereBuilder.Append(seg);
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
            else if (dbQuery.Expression != null)
            {
                TableAliasCache aliases = this.PrepareTableAlias<T>(dbQuery.SelectInfo, token);
                ExpressionVisitorBase visitor = null;
                visitor = new UpdateExpressionVisitor(this, aliases, dbQuery.Expression);
                visitor.Write(builder);

                builder.AppendNewLine();
                builder.Append("FROM ");
                builder.AppendMember(typeRuntime.TableName, !typeRuntime.IsTemporary);
                builder.AppendAs("t0");

                var cmd2 = new SqlServerMapperCommand(context, aliases, token) { HasMany = dbQuery.SelectInfo.HasMany };

                visitor = new SqlServerJoinExpressionVisitor(context, aliases, dbQuery.SelectInfo.Joins);
                visitor.Write(cmd2.JoinFragment);

                visitor = new WhereExpressionVisitor(this, aliases, dbQuery.SelectInfo.Condtion);
                visitor.Write(cmd2.WhereFragment);
                cmd2.AddNavMembers(visitor.NavMembers);

                builder.Append(cmd2.CommandText);
            }

            return new Command(builder.ToString(), builder.Token != null ? builder.Token.Parameters : null, System.Data.CommandType.Text);
        }

        // JOIN 表达式解析器
        class SqlServerJoinExpressionVisitor : JoinExpressionVisitor
        {
            private List<DbExpression> _joins = null;
            private TableAliasCache _aliases = null;
            private SqlServerDbContext _context = null;
            private SqlServerDbQueryProvider _provider = null;

            /// <summary>
            /// 初始化 <see cref="SqlServerJoinExpressionVisitor"/> 类的新实例
            /// </summary>
            internal SqlServerJoinExpressionVisitor(SqlServerDbContext context, TableAliasCache aliases, List<DbExpression> joins)
                : base(context.Provider, aliases, joins)
            {
                _joins = joins;
                _aliases = aliases;
                _context = context;
                _provider = context.Provider as SqlServerDbQueryProvider;
            }

            /// <summary>
            /// 写入SQL片断
            /// </summary>
            public override void Write(ISqlBuilder builder)
            {
                base._builder = builder;
                if (base._methodVisitor == null) base._methodVisitor = _provider.CreateMethodVisitor(this);

                foreach (DbExpression dbExpression in _joins)
                {
                    builder.AppendNewLine();

                    // [INNER/LEFT JOIN]
                    if (dbExpression.DbExpressionType == DbExpressionType.GroupJoin || dbExpression.DbExpressionType == DbExpressionType.Join || dbExpression.DbExpressionType == DbExpressionType.GroupRightJoin)
                    {
                        JoinType joinType = JoinType.InnerJoin;
                        if (dbExpression.DbExpressionType == DbExpressionType.GroupJoin) joinType = JoinType.LeftJoin;
                        else if (dbExpression.DbExpressionType == DbExpressionType.GroupRightJoin) joinType = JoinType.RightJoin;
                        this.AppendJoinType(builder, joinType);
                        this.AppendLfInJoin(builder, dbExpression, _aliases);
                    }
                    else if (dbExpression.DbExpressionType == DbExpressionType.SelectMany)
                    {
                        this.AppendJoinType(builder, JoinType.CrossJoin);
                        this.AppendCrossJoin(builder, dbExpression, _aliases);
                    }
                }
            }

            // LEFT OR INNER JOIN
            private void AppendLfInJoin(ISqlBuilder builder, DbExpression dbExpression, TableAliasCache aliases)
            {
                bool withNoLock = false;
                builder.Append(' ');
                IDbQueryable dbQuery = (IDbQueryable)((dbExpression.Expressions[0] as ConstantExpression).Value);
                if (dbQuery.DbExpressions.Count == 1 && dbQuery.DbExpressions[0].DbExpressionType == DbExpressionType.GetTable)
                {
                    Type type = dbExpression.Expressions[0].Type.GetGenericArguments()[0];
                    var typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo(type);
                    builder.AppendMember(typeRuntime.TableName, !typeRuntime.IsTemporary);

                    withNoLock = !typeRuntime.IsTemporary && _context.NoLock && !string.IsNullOrEmpty(_provider.WidthNoLock);
                }
                else
                {
                    // 嵌套
                    var cmd = dbQuery.Resolve(builder.Indent + 1, false, builder.Token);
                    builder.Append("(");
                    builder.Append(cmd.CommandText);
                    builder.AppendNewLine();
                    builder.Append(')');
                }


                var left = dbExpression.Expressions[1] as LambdaExpression;
                var right = dbExpression.Expressions[2] as LambdaExpression;

                // t0(t1)
                string alias = !(left.Body.NodeType == ExpressionType.New || left.Body.NodeType == ExpressionType.MemberInit)
                    ? aliases.GetTableAlias(dbExpression.Expressions[2])
                    : aliases.GetTableAlias(right.Parameters[0]);
                builder.Append(' ');
                builder.Append(alias);
                builder.Append(' ');

                if (withNoLock)
                {
                    builder.Append(_provider.WidthNoLock);
                    builder.Append(' ');
                }

                // ON a.Name = b.Name AND a.Id = b.Id
                builder.Append("ON ");

                if (left.Body.NodeType == ExpressionType.New)
                {

                    var body1 = left.Body as NewExpression;
                    var body2 = right.Body as NewExpression;

                    for (int index = 0; index < body1.Arguments.Count; ++index)
                    {
                        builder.AppendMember(aliases, body1.Arguments[index]);
                        builder.Append(" = ");
                        builder.AppendMember(aliases, body2.Arguments[index]);
                        if (index < body1.Arguments.Count - 1) builder.Append(" AND ");
                    }
                }
                else if (left.Body.NodeType == ExpressionType.MemberInit)
                {
                    var body1 = left.Body as MemberInitExpression;
                    var body2 = right.Body as MemberInitExpression;

                    for (int index = 0; index < body1.Bindings.Count; ++index)
                    {
                        builder.AppendMember(aliases, (body1.Bindings[index] as MemberAssignment).Expression);
                        builder.Append(" = ");
                        builder.AppendMember(aliases, (body2.Bindings[index] as MemberAssignment).Expression);
                        if (index < body1.Bindings.Count - 1) builder.Append(" AND ");
                    }
                }
                else
                {
                    builder.AppendMember(aliases, left.Body.ReduceUnary());
                    builder.Append(" = ");
                    builder.AppendMember(aliases, right.Body.ReduceUnary());
                }
            }

            // Cross Join
            private void AppendCrossJoin(ISqlBuilder builder, DbExpression exp, TableAliasCache aliases)
            {
                var lambdaExp = exp.Expressions[1] as LambdaExpression;
                Type type = lambdaExp.Parameters[1].Type;
                var typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo(type);
                bool withNoLock = !typeRuntime.IsTemporary && _context.NoLock && !string.IsNullOrEmpty(_provider.WidthNoLock);

                builder.Append(' ');
                builder.AppendMember(typeRuntime.TableName, !typeRuntime.IsTemporary);

                string alias = aliases.GetTableAlias(lambdaExp.Parameters[1]);
                builder.Append(' ');
                builder.Append(alias);
                builder.Append(' ');

                if (withNoLock)
                {
                    builder.Append(_provider.WidthNoLock);
                    builder.Append(' ');
                }
            }
        }

        // 含实体映射信息的SQL命令
        class SqlServerMapperCommand : MapperCommand
        {
            private TableAliasCache _aliases = null;
            private SqlServerDbContext _context = null;
            private SqlServerDbQueryProvider _provider = null;

            /// <summary>
            /// 实例化 <see cref="SqlServerMapperCommand"/> 类的新实例
            /// </summary>
            /// <param name="context">数据查询提供者</param>
            /// <param name="aliases">别名</param>
            /// <param name="token">解析上下文参数</param>
            public SqlServerMapperCommand(SqlServerDbContext context, TableAliasCache aliases, ResolveToken token)
                : base(context.Provider, aliases, token)
            {
                _aliases = aliases;
                _context = context;
                _provider = context.Provider as SqlServerDbQueryProvider;
            }

            // 添加导航属性关联
            protected override void AppendNavigation()
            {
                if (base.NavMembers == null || base.NavMembers.Count == 0) return;

                // 如果有一对多的导航属性，肯定会产生嵌套查询。那么内层查询别名肯定是t0，所以需要清掉
                if (this.HasMany) _aliases = new TableAliasCache(_aliases.Declared);
                //开始产生LEFT JOIN 子句
                ISqlBuilder builder = this.JoinFragment;
                foreach (var kvp in base.NavMembers)
                {
                    string key = kvp.Key;
                    MemberExpression m = kvp.Value;
                    TypeRuntimeInfo typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo(m.Expression.Type);
                    ForeignKeyAttribute attribute = typeRuntime.GetInvokerAttribute<ForeignKeyAttribute>(m.Member.Name);

                    string innerKey = string.Empty;
                    string outerKey = key;
                    string innerAlias = string.Empty;

                    if (!m.Expression.Acceptable())
                    {
                        innerKey = m.Expression.NodeType == ExpressionType.Parameter
                            ? (m.Expression as ParameterExpression).Name
                            : (m.Expression as MemberExpression).Member.Name;
                    }
                    else
                    {
                        MemberExpression mLeft = null;
                        if (m.Expression.NodeType == ExpressionType.MemberAccess) mLeft = m.Expression as MemberExpression;
                        else if (m.Expression.NodeType == ExpressionType.Call) mLeft = (m.Expression as MethodCallExpression).Object as MemberExpression;
                        string name = TypeRuntimeInfoCache.GetRuntimeInfo(mLeft.Type).TableName;
                        innerAlias = _aliases.GetJoinTableAlias(name);

                        if (string.IsNullOrEmpty(innerAlias))
                        {
                            string keyLeft = mLeft.GetKeyWidthoutAnonymous();
                            if (base.NavMembers.ContainsKey(keyLeft)) innerKey = keyLeft;
                            innerAlias = _aliases.GetNavigationTableAlias(innerKey);
                        }
                    }

                    string alias1 = !string.IsNullOrEmpty(innerAlias) ? innerAlias : _aliases.GetTableAlias(innerKey);
                    string alias2 = _aliases.GetNavigationTableAlias(outerKey);


                    builder.AppendNewLine();
                    builder.Append("LEFT JOIN ");
                    Type type = m.Type;
                    if (type.IsGenericType) type = type.GetGenericArguments()[0];
                    var typeRuntime2 = TypeRuntimeInfoCache.GetRuntimeInfo(type);
                    builder.AppendMember(typeRuntime2.TableName, !typeRuntime2.IsTemporary);
                    builder.Append(" ");
                    builder.Append(alias2);

                    bool withNoLock = !typeRuntime2.IsTemporary && _context.NoLock && !string.IsNullOrEmpty(_provider.WidthNoLock);
                    if (withNoLock)
                    {
                        builder.Append(' ');
                        builder.Append(_provider.WidthNoLock);
                    }

                    builder.Append(" ON ");
                    for (int i = 0; i < attribute.InnerKeys.Length; i++)
                    {
                        builder.Append(alias1);
                        builder.Append('.');
                        builder.AppendMember(attribute.InnerKeys[i]);
                        builder.Append(" = ");
                        builder.Append(alias2);
                        builder.Append('.');
                        builder.AppendMember(attribute.OuterKeys[i]);

                        if (i < attribute.InnerKeys.Length - 1) builder.Append(" AND ");
                    }
                }
            }
        }
    }
}