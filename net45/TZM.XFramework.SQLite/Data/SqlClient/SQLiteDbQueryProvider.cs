
using System;
using System.Data;
using System.Linq;
using System.Data.Common;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.Data.SQLite;

namespace TZM.XFramework.Data.SqlClient
{
    /// <summary>
    /// 查询语义提供者，用来构建、解析查询语义
    /// </summary>
    public sealed class SQLiteDbQueryProvider : DbQueryProvider
    {
        static Expression<Func<SQLiteRowId, string>> _rowIdExpression = x => x.RowId;

        /// <summary>
        /// 查询语义提供者实例
        /// </summary>
        public static SQLiteDbQueryProvider Instance = new SQLiteDbQueryProvider();

        /// <summary>
        /// 数据源类的提供程序实现的实例
        /// </summary>
        public override DbProviderFactory DbProviderFactory { get { return SQLiteFactory.Instance; } }

        /// <summary>
        /// SQL字段值生成器
        /// </summary>
        public override DbValue DbValue { get { return SQLiteDbValue.Instance; } }

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
                return "SQLite";
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
        private SQLiteDbQueryProvider()
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
            return new SQLiteSqlBuilder(this, token);
        }

        /// <summary>
        /// 创建方法表达式访问器
        /// </summary>
        /// <returns></returns>
        public override MethodCallExpressionVisitor CreateMethodVisitor(ExpressionVisitorBase visitor)
        {
            return new SQLiteMethodCallExressionVisitor(this, visitor);
        }

        // 创建 SELECT 命令
        protected override Command ResolveSelectCommand<T>(DbQueryableInfo_Select<T> dbQuery, int indent, bool isOuter, ResolveToken token)
        {
            var cmd = (MapperCommand)this.ResolveSelectCommandImpl<T>(dbQuery, indent, isOuter, token);
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


            // 导航属性中有1:n关系，只统计主表
            // 例：AccountList = a.Client.AccountList,
            var subQuery = dbQuery.SubQueryInfo as DbQueryableInfo_Select<T>;
            if (dbQuery.HasMany && subQuery != null && subQuery.Aggregate != null) dbQuery = subQuery;

            bool useStatis = dbQuery.Aggregate != null;
            bool useNesting = dbQuery.HasDistinct || dbQuery.GroupBy != null || dbQuery.Skip > 0 || dbQuery.Take > 0;
            string alias0 = token != null && !string.IsNullOrEmpty(token.AliasPrefix) ? (token.AliasPrefix + "0") : "t0";
            // 没有聚合函数或者使用 'Skip' 子句，则解析OrderBy
            // 导航属性如果使用嵌套，除非有 TOP 或者 OFFSET 子句，否则不能用ORDER BY
            bool useOrderBy = (!useStatis || dbQuery.Skip > 0) && !dbQuery.HasAny && (!dbQuery.SubQueryOfMany || (dbQuery.Skip > 0 || dbQuery.Take > 0));

            TableAliasCache aliases = this.PrepareTableAlias<T>(dbQuery, token);
            MapperCommand cmd = new MapperCommand(this, aliases, token) { HasMany = dbQuery.HasMany };
            ISqlBuilder jf = cmd.JoinFragment;
            ISqlBuilder wf = cmd.WhereFragment;

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
                    var visitor2 = new ColumnExpressionVisitor(this, aliases, dbQuery);
                    visitor2.Write(jf);

                    cmd.PickColumns = visitor2.PickColumns;
                    cmd.PickColumnText = visitor2.PickColumnText;
                    cmd.Navigations = visitor2.Navigations;
                    cmd.AddNavMembers(visitor2.NavMembers);
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
                Command cmd2 = this.ResolveSelectCommandImpl<T>(dbQuery.SubQueryInfo as DbQueryableInfo_Select<T>, indent + 1, false, token);
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

            #region 分页查询

            if (dbQuery.Take > 0) wf.AppendNewLine().AppendFormat("LIMIT {0}", this.DbValue.GetSqlValue(dbQuery.Take, token));
            else if (dbQuery.Take == 0 && dbQuery.Skip > 0) wf.AppendNewLine().AppendFormat("LIMIT {0}", this.DbValue.GetSqlValue(-1, token));
            if (dbQuery.Skip > 0) wf.AppendFormat(" OFFSET {0}", this.DbValue.GetSqlValue(dbQuery.Skip, token));

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
                    jf.Append("LIMIT 1");
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
                object entity = dbQuery.Entity;
                ISqlBuilder seg_Columns = this.CreateSqlBuilder(token);
                ISqlBuilder seg_Values = this.CreateSqlBuilder(token);

                // 指定插入列
                MemberAccessorCollection memberAccessors = typeRuntime.MemberAccessors;
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
                        memberAccessors[name] = typeRuntime.MemberAccessors[name];
                    }
                }

                foreach (var m in memberAccessors)
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

                    builder.AppendFormat("SELECT LAST_INSERT_ROWID()", typeRuntime.TableName, dbQuery.AutoIncrement.Member.Name);
                    builder.Append(" AS ");
                    builder.Append(this.QuotePrefix);
                    builder.Append(Constant.AUTOINCREMENTNAME);
                    builder.Append(this.QuoteSuffix);
                }
            }
            else if (dbQuery.SelectInfo != null)
            {
                builder.Append("INSERT INTO ");
                builder.AppendMember(typeRuntime.TableName, !typeRuntime.IsTemporary);
                builder.Append('(');

                int i = 0;
                MapperCommand cmd2 = this.ResolveSelectCommandImpl(dbQuery.SelectInfo, 0, false, token) as MapperCommand;
                //for (int i = 0; i < seg.Columns.Count; i++)
                foreach (var column in cmd2.PickColumns)
                {
                    builder.AppendMember(column.Name);
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

            builder.Append("DELETE FROM ");
            builder.AppendMember(typeRuntime.TableName, !typeRuntime.IsTemporary);
            builder.Append(" ");

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

                    builder.AppendMember(m.Member.Name);
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
                var parameter = Expression.Parameter(typeof(SQLiteRowId), lambda != null ? lambda.Parameters[0].Name : "x");
                var expression = Expression.MakeMemberAccess(parameter, (_rowIdExpression.Body as MemberExpression).Member);
                dbQuery.SelectInfo.Select = new DbExpression(DbExpressionType.Select, expression);
                var cmd = (MapperCommand)this.ResolveSelectCommand<T>(dbQuery.SelectInfo, 1, false, null);
                if (token != null && token.Extendsions == null)
                {
                    token.Extendsions = new Dictionary<string, object>();
                    if (!token.Extendsions.ContainsKey("SQLiteDelete")) token.Extendsions.Add("SQLiteDelete", null);
                }

                if ((cmd.NavMembers != null && cmd.NavMembers.Count > 0) || dbQuery.SelectInfo.Joins.Count > 0)
                {
                    cmd = (MapperCommand)this.ResolveSelectCommand<T>(dbQuery.SelectInfo, 1, false, token);
                    builder.Append("WHERE ");
                    builder.AppendMember("RowID");
                    builder.Append(" IN(");
                    builder.AppendNewLine(cmd.CommandText);
                    builder.Append(')');
                }
                else
                {
                    TableAliasCache aliases = this.PrepareTableAlias<T>(dbQuery.SelectInfo, token);
                    ExpressionVisitorBase visitor = null;

                    visitor = new JoinExpressionVisitor(this, aliases, dbQuery.SelectInfo.Joins);
                    visitor.Write(builder);

                    visitor = new WhereExpressionVisitor(this, null, dbQuery.SelectInfo.Condtion);
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
            builder.Append(" SET");
            builder.AppendNewLine();

            if (dbQuery.Entity != null)
            {
                object entity = dbQuery.Entity;
                ISqlBuilder seg_Where = this.CreateSqlBuilder(token);
                bool useKey = false;
                int length = 0;

                foreach (var m in typeRuntime.MemberAccessors)
                {
                    var column = m.Column;
                    if (column != null && column.IsIdentity) goto gotoLabel; // fix issue# 自增列同时又是主键
                    if (column != null && column.NoMapped) continue;
                    if (m.Column != null && column.DbType is SqlDbType && (SqlDbType)column.DbType == SqlDbType.Timestamp) continue; // 行版本号
                    if (m.ForeignKey != null) continue;
                    if (m.Member.MemberType == System.Reflection.MemberTypes.Method) continue;

                    builder.AppendMember(m.Member.Name);
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
                        seg_Where.AppendMember(m.Member.Name);
                        seg_Where.Append(" = ");
                        seg_Where.Append(seg);
                        seg_Where.Append(" AND ");
                    }
                }

                if (!useKey) throw new XFrameworkException("Update<T>(T value) require entity must have key column.");

                builder.Length = length;
                seg_Where.Length -= 5;

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
                    var memberInit = body as MemberInitExpression;
                    var bindings = new List<MemberBinding>(memberInit.Bindings);
                    foreach (var m in typeRuntime.KeyAccessors)
                    {
                        var member = Expression.MakeMemberAccess(lambda.Parameters[0], m.Member);
                        var binding = Expression.Bind(m.Member, member);
                        if (!bindings.Any(x => x.Member == m.Member)) bindings.Add(binding);
                    }
                    expression = Expression.MemberInit(memberInit.NewExpression, bindings);
                }
                else if (body.NodeType == ExpressionType.New)
                {
                    var newExpression = body as NewExpression;
                    var bindings = new List<MemberBinding>();
                    for (int i = 0; i < newExpression.Members.Count; i++)
                    {
                        var m = typeRuntime.GetAccessor(newExpression.Members[i].Name);
                        var binding = Expression.Bind(m.Member, newExpression.Arguments[i].Type != m.DataType
                            ? Expression.Convert(newExpression.Arguments[i], m.DataType)
                            : newExpression.Arguments[i]);
                        bindings.Add(binding);
                    }

                    foreach (var m in typeRuntime.KeyAccessors)
                    {
                        var member = Expression.MakeMemberAccess(lambda.Parameters[0], m.Member);
                        var binding = Expression.Bind(m.Member, member);
                        if (!bindings.Any(x => x.Member == m.Member)) bindings.Add(binding);
                    }

                    var newExpression2 = Expression.New(typeRuntime.ConstructorAccessor.Constructor);
                    expression = Expression.MemberInit(newExpression2, bindings);
                }

                // 解析查询以确定是否需要嵌套
                dbQuery.SelectInfo.Select = new DbExpression(DbExpressionType.Select, expression);
                var cmd = (MapperCommand)this.ResolveSelectCommand<T>(dbQuery.SelectInfo, 1, false, null);

                if ((cmd.NavMembers != null && cmd.NavMembers.Count > 0) || dbQuery.SelectInfo.Joins.Count > 0)
                {
                    if (typeRuntime.KeyAccessors == null || typeRuntime.KeyAccessors.Count == 0)
                        throw new XFrameworkException("Update<T>(Expression<Func<T, object>> updateExpression) require entity must have key column.");

                    // SET 字段
                    var visitor = new SQLiteUpdateExpressionVisitor<T>(this, null, dbQuery, null);
                    visitor.ParseCommand = this.ResolveSelectCommand;
                    visitor.Write(builder);

                    // WHERE部分
                    builder.AppendNewLine();
                    builder.Append("WHERE EXISTS");
                    visitor.VisitArgument(dbQuery.SelectInfo.Select.Expressions[0], true);
                }
                else
                {
                    // 直接 SQL 的 UPDATE 语法
                    TableAliasCache aliases = this.PrepareTableAlias<T>(dbQuery.SelectInfo, token);
                    var visitor = new SQLiteUpdateExpressionVisitor<T>(this, aliases, dbQuery, null);
                    visitor.ParseCommand = this.ResolveSelectCommand;
                    visitor.Write(builder);

                    var visitor2 = new WhereExpressionVisitor(this, null, dbQuery.SelectInfo.Condtion);
                    visitor2.Write(builder);
                }
            }

            builder.Append(';');
            return new Command(builder.ToString(), builder.Token != null ? builder.Token.Parameters : null, System.Data.CommandType.Text);
        }

        /// <summary>
        /// 行记录ID
        /// </summary>
        public class SQLiteRowId
        {
            /// <summary>
            /// 行记录ID
            /// </summary>
            public string RowId { get; set; }
        }
    }
}