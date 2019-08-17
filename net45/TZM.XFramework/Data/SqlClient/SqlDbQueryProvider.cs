
using System.Data;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data.Common;

namespace TZM.XFramework.Data.SqlClient
{
    /// <summary>
    /// 查询语义提供者，用来构建、解析查询语义
    /// </summary>
    public sealed class SqlDbQueryProvider : DbQueryProvider
    {
        // 无阻塞模式
        private string _widthNoLock = "WITH(NOLOCK)";

        /// <summary>
        /// 查询语义提供者 单例
        /// </summary>
        public static SqlDbQueryProvider Instance = Singleton.Instance;

        /// <summary>
        /// 数据源类的提供程序实现的实例
        /// </summary>
        public override DbProviderFactory DbProviderFactory { get { return SqlClientFactory.Instance; } }

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
        /// 实例化 <see cref="SqlDbQueryProvider"/> 类的新实例
        /// </summary>
        private SqlDbQueryProvider() : base()
        {

        }

        /// <summary>
        /// 创建 SQL 构造器
        /// </summary>
        /// <param name="token">参数列表，NULL 或者 Parameters=NULL 时表示不使用参数化</param>
        /// <returns></returns>
        public override ISqlBuilder CreateSqlBuilder(ParserToken token)
        {
            return new SqlBuilder(this, token);
        }

        /// <summary>
        /// 创建方法表达式访问器
        /// </summary>
        /// <returns></returns>
        public override IMethodCallExressionVisitor CreateMethodCallVisitor(ExpressionVisitorBase visitor)
        {
            return new SqlMethodCallExressionVisitor(this, visitor);
        }

        // 创建 SELECT 命令
        protected override Command ParseSelectCommand<T>(DbQueryableInfo_Select<T> sQueryInfo, int indent, bool isOuter, ParserToken token)
        {
            // 说明：
            // 1.OFFSET 前必须要有 'ORDER BY'，即 'Skip' 子句前必须使用 'OrderBy' 子句
            // 2.在有统计函数的<MAX,MIN...>情况下，如果有 'Distinct' 'GroupBy' 'Skip' 'Take' 子句，则需要使用嵌套查询
            // 3.'Any' 子句将翻译成 IF EXISTS...
            // 4.分组再分页时需要使用嵌套查询，此时子查询不需要 'OrderBy' 子句，但最外层则需要
            // 5.'Skip' 'Take' 子句视为语义结束符，在其之后的子句将使用嵌套查询
            // 6.导航属性中有 1:n 关系的，需要使用嵌套查询，否则分页查询会有问题

            // 导航属性中有1:n关系，只统计主表
            // 例：AccountList = a.Client.AccountList,
            var subQueryInfo = sQueryInfo.SubQueryInfo as DbQueryableInfo_Select<T>;
            if (sQueryInfo.HasManyNavigation && subQueryInfo != null && subQueryInfo.StatisExpression != null) sQueryInfo = subQueryInfo;

            bool useStatis = sQueryInfo.StatisExpression != null;
            // 没有统计函数或者使用 'Skip' 子句，则解析OrderBy
            // 导航属性如果使用嵌套，除非有 TOP 或者 OFFSET 子句，否则不能用ORDER BY
            string alias0 = token != null && !string.IsNullOrEmpty(token.TableAliasName) ? (token.TableAliasName + "0") : "t0";
            bool useSubQuery = sQueryInfo.HaveDistinct || sQueryInfo.GroupByExpression != null || sQueryInfo.Skip > 0 || sQueryInfo.Take > 0;
            bool useOrderBy = (!useStatis || sQueryInfo.Skip > 0) && !sQueryInfo.HaveAny && (!sQueryInfo.ResultByManyNavigation || (sQueryInfo.Skip > 0 || sQueryInfo.Take > 0));

            IDbQueryable dbQueryable = sQueryInfo.SourceQuery;
            TableAliasCache aliases = this.PrepareAlias<T>(sQueryInfo, token);
            SelectCommand cmd = new SelectCommand(this, aliases, token) { HasManyNavigation = sQueryInfo.HasManyNavigation };
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
                var visitor2 = new StatisExpressionVisitor(this, aliases, sQueryInfo.StatisExpression, sQueryInfo.GroupByExpression, alias0);
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
            if (sQueryInfo.HaveAny)
            {
                jf.Append("IF EXISTS(");
                indent += 1;
                jf.Indent = indent;
                jf.AppendNewLine();
            }

            jf.Append("SELECT ");

            if (useStatis && !useSubQuery)
            {
                // 如果有统计函数，并且不是嵌套的话，则直接使用SELECT <MAX,MIN...>，不需要解析选择的字段
                jf.AppendNewLine();
                var visitor2 = new StatisExpressionVisitor(this, aliases, sQueryInfo.StatisExpression, sQueryInfo.GroupByExpression);
                visitor2.Write(jf);
                cmd.AddNavMembers(visitor2.NavMembers);
            }
            else
            {
                // DISTINCT 子句
                if (sQueryInfo.HaveDistinct) jf.Append("DISTINCT ");
                // TOP 子句
                if (sQueryInfo.Take > 0 && sQueryInfo.Skip == 0) jf.AppendFormat("TOP({0})", jf.GetSqlValue(sQueryInfo.Take));
                // Any 
                if (sQueryInfo.HaveAny) jf.Append("TOP 1 1");

                #region 字段

                if (!sQueryInfo.HaveAny)
                {
                    // SELECT 范围
                    var visitor2 = new ColumnExpressionVisitor(this, aliases, sQueryInfo);
                    visitor2.Write(jf);

                    cmd.Columns = visitor2.Columns;
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
            if (sQueryInfo.SubQueryInfo != null)
            {
                // 子查询
                jf.Append('(');
                Command cmd2 = this.ParseSelectCommand<T>(sQueryInfo.SubQueryInfo as DbQueryableInfo_Select<T>, indent + 1, false, token);
                jf.Append(cmd2.CommandText);
                jf.AppendNewLine();
                jf.Append(") ");
                jf.Append(alias0);
                jf.Append(' ');
            }
            else
            {
                var typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo(sQueryInfo.FromType);
                jf.AppendMember(typeRuntime.TableName, !typeRuntime.IsTemporary);
                jf.Append(' ');
                jf.Append(alias0);
                jf.Append(' ');
                SqlDbContext context = (SqlDbContext)dbQueryable.DbContext;
                if (context.NoLock && !string.IsNullOrEmpty(this._widthNoLock)) jf.Append(this._widthNoLock);
            }

            // LEFT<INNER> JOIN 子句
            ExpressionVisitorBase visitor = new JoinExpressionVisitor(this, aliases, sQueryInfo.Joins);
            visitor.Write(jf);

            wf.Indent = jf.Indent;

            // WHERE 子句
            visitor = new WhereExpressionVisitor(this, aliases, sQueryInfo.WhereExpression);
            visitor.Write(wf);
            cmd.AddNavMembers(visitor.NavMembers);

            // GROUP BY 子句
            visitor = new GroupByExpressionVisitor(this, aliases, sQueryInfo.GroupByExpression);
            visitor.Write(wf);
            cmd.AddNavMembers(visitor.NavMembers);

            // HAVING 子句
            visitor = new HavingExpressionVisitor(this, aliases, sQueryInfo.HavingExpression, sQueryInfo.GroupByExpression);
            visitor.Write(wf);
            cmd.AddNavMembers(visitor.NavMembers);

            // ORDER 子句
            if (sQueryInfo.OrderBys.Count > 0 && useOrderBy)
            {
                visitor = new OrderByExpressionVisitor(this, aliases, sQueryInfo.OrderBys, sQueryInfo.GroupByExpression);
                visitor.Write(wf);
                cmd.AddNavMembers(visitor.NavMembers);
            }

            #endregion

            #region 分页查询

            if (sQueryInfo.Skip > 0)
            {
                if (sQueryInfo.OrderBys.Count == 0) throw new XFrameworkException("The method 'OrderBy' must be called before 'Skip'.");
                wf.AppendNewLine();
                wf.Append("OFFSET ");
                wf.Append(wf.GetSqlValue(sQueryInfo.Skip));
                wf.Append(" ROWS");

                if (sQueryInfo.Take > 0)
                {
                    wf.Append(" FETCH NEXT ");
                    wf.Append(wf.GetSqlValue(sQueryInfo.Take));
                    wf.Append(" ROWS ONLY ");
                }
            }

            #endregion

            #region 嵌套查询

            if (useStatis && useSubQuery)
            {
                cmd.Convergence();
                indent -= 1;
                jf.Indent = indent;
                jf.AppendNewLine();
                jf.Append(") ");
                jf.Append(alias0);
                jf.Append(' ');
            }

            #endregion

            #region 嵌套导航

            if (sQueryInfo.HasManyNavigation && subQueryInfo.StatisExpression == null && subQueryInfo != null && subQueryInfo.OrderBys.Count > 0 && !(subQueryInfo.Skip > 0 || subQueryInfo.Take > 0))
            {
                // TODO Include 从表，没分页，OrderBy 报错
                cmd.Convergence();
                visitor = new OrderByExpressionVisitor(this, aliases, subQueryInfo.OrderBys);
                visitor.Write(jf);
            }

            #endregion

            #region 并集查询

            // UNION 子句
            if (sQueryInfo.Unions != null && sQueryInfo.Unions.Count > 0)
            {
                cmd.Convergence();
                for (int index = 0; index < sQueryInfo.Unions.Count; index++)
                {
                    jf.AppendNewLine();
                    jf.Append("UNION ALL");
                    if (indent == 0) jf.AppendNewLine();
                    Command cmd2 = this.ParseSelectCommand<T>(sQueryInfo.Unions[index] as DbQueryableInfo_Select<T>, indent, isOuter, token);
                    jf.Append(cmd2.CommandText);
                }

            }

            #endregion

            #region Any 子句

            // 'Any' 子句
            if (sQueryInfo.HaveAny)
            {
                cmd.Convergence();
                indent -= 1;
                jf.Indent = indent;
                jf.AppendNewLine();
                jf.Append(") SELECT 1 ELSE SELECT 0");
            }

            #endregion

            return cmd;
        }

        // 创建 INSRT 命令
        protected override Command ParseInsertCommand<T>(DbQueryableInfo_Insert<T> nQueryInfo, ParserToken token)
        {
            ISqlBuilder builder = this.CreateSqlBuilder(token);
            TypeRuntimeInfo typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo<T>();
            TableAliasCache aliases = new TableAliasCache();

            if (nQueryInfo.Entity != null)
            {
                object entity = nQueryInfo.Entity;
                ISqlBuilder columnsBuilder = this.CreateSqlBuilder(token);
                ISqlBuilder valuesBuilder = this.CreateSqlBuilder(token);

                // 指定插入列
                Dictionary<string, MemberInvokerBase> invokers = typeRuntime.Invokers;
                if (nQueryInfo.EntityColumns != null && nQueryInfo.EntityColumns.Count > 0)
                {
                    invokers = new Dictionary<string, MemberInvokerBase>();
                    for (int i = 0; i < nQueryInfo.EntityColumns.Count; i++)
                    {
                        Expression curExpr = nQueryInfo.EntityColumns[i];
                        if (curExpr.NodeType == ExpressionType.Lambda) curExpr = (curExpr as LambdaExpression).Body.ReduceUnary();
                        if (curExpr.NodeType != ExpressionType.MemberAccess)
                            throw new XFrameworkException("ERR {0}: Only support MemberAccess expression.", nQueryInfo.EntityColumns[i]);

                        MemberExpression member = curExpr as MemberExpression;
                        string name = member.Member.Name;
                        invokers[name] = typeRuntime.Invokers[name];
                    }
                }

                foreach (var kv in invokers)
                {
                    MemberInvokerBase invoker = kv.Value;
                    var column = invoker.Column;
                    if (column != null && column.NoMapped) continue;
                    if (invoker.ForeignKey != null) continue;
                    if (invoker.Column != null && column.DbType is SqlDbType && (SqlDbType)column.DbType == SqlDbType.Timestamp) continue; // 行版本号
                    if (invoker.Member.MemberType == System.Reflection.MemberTypes.Method) continue;

                    if (invoker != nQueryInfo.AutoIncrement)
                    {
                        columnsBuilder.AppendMember(invoker.Member.Name);
                        columnsBuilder.Append(',');

                        var value = invoker.Invoke(entity);
                        string seg = builder.GetSqlValueWidthDefault(value, column);
                        valuesBuilder.Append(seg);
                        valuesBuilder.Append(',');
                    }
                }

                columnsBuilder.Length -= 1;
                valuesBuilder.Length -= 1;

                if (nQueryInfo.Bulk == null || !nQueryInfo.Bulk.OnlyValue)
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

                if (nQueryInfo.Bulk != null && nQueryInfo.Bulk.OnlyValue) builder.AppendNewTab();
                builder.Append('(');
                builder.Append(valuesBuilder);
                builder.Append(')');
                if (nQueryInfo.Bulk != null && !nQueryInfo.Bulk.IsEndPos)
                {
                    builder.Append(",");
                    builder.AppendNewLine();
                }

                if (nQueryInfo.Bulk == null && nQueryInfo.AutoIncrement != null)
                {
                    builder.AppendNewLine();
                    builder.Append("SELECT SCOPE_IDENTITY()");
                    builder.AppendAs(Constant.AUTOINCREMENTNAME);
                }
            }
            else if (nQueryInfo.SelectInfo != null)
            {
                builder.Append("INSERT INTO ");
                builder.AppendMember(typeRuntime.TableName, !typeRuntime.IsTemporary);
                builder.Append('(');

                int i = 0;
                SelectCommand cmd2 = this.ParseSelectCommand(nQueryInfo.SelectInfo, 0, true, token) as SelectCommand;
                foreach (var kvp in cmd2.Columns)
                {
                    builder.AppendMember(kvp.Key);
                    if (i < cmd2.Columns.Count - 1) builder.Append(',');
                    i++;
                }

                builder.Append(')');
                builder.AppendNewLine();
                builder.Append(cmd2.CommandText);
            }

            return new Command(builder.ToString(), builder.Token != null ? builder.Token.Parameters : null, System.Data.CommandType.Text);
        }

        // 创建 DELETE 命令
        protected override Command ParseDeleteCommand<T>(DbQueryableInfo_Delete<T> dQueryInfo, ParserToken token)
        {
            TypeRuntimeInfo typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo<T>();
            ISqlBuilder builder = this.CreateSqlBuilder(token);
            bool useKey = false;

            builder.Append("DELETE t0 FROM ");
            builder.AppendMember(typeRuntime.TableName, !typeRuntime.IsTemporary);
            builder.Append(" t0 ");

            if (dQueryInfo.Entity != null)
            {
                object entity = dQueryInfo.Entity;

                builder.AppendNewLine();
                builder.Append("WHERE ");

                foreach (var kv in typeRuntime.Invokers)
                {
                    MemberInvokerBase invoker = kv.Value;
                    var column = invoker.Column;

                    if (column != null && column.IsKey)
                    {
                        useKey = true;
                        var value = invoker.Invoke(entity);
                        var seg = builder.GetSqlValue(value, column);
                        builder.AppendMember("t0", invoker.Member.Name);
                        builder.Append(" = ");
                        builder.Append(seg);
                        builder.Append(" AND ");
                    };
                }
                builder.Length -= 5;

                if (!useKey) throw new XFrameworkException("Delete<T>(T value) require T must have key column.");
            }
            else if (dQueryInfo.SelectInfo != null)
            {
                IDbQueryable dbQueryable = dQueryInfo.SourceQuery;
                TableAliasCache aliases = this.PrepareAlias<T>(dQueryInfo.SelectInfo, token);
                var cmd2 = new SelectCommand(this, aliases, token) { HasManyNavigation = dQueryInfo.SelectInfo.HasManyNavigation };

                ExpressionVisitorBase visitor = new JoinExpressionVisitor(this, aliases, dQueryInfo.SelectInfo.Joins);
                visitor.Write(cmd2.JoinFragment);

                visitor = new WhereExpressionVisitor(this, aliases, dQueryInfo.SelectInfo.WhereExpression);
                visitor.Write(cmd2.WhereFragment);
                cmd2.AddNavMembers(visitor.NavMembers);

                builder.Append(cmd2.CommandText);
            }

            return new Command(builder.ToString(), builder.Token != null ? builder.Token.Parameters : null, System.Data.CommandType.Text);
        }

        // 创建 UPDATE 命令
        protected override Command ParseUpdateCommand<T>(DbQueryableInfo_Update<T> uQueryInfo, ParserToken token)
        {
            ISqlBuilder builder = this.CreateSqlBuilder(token);
            var typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo<T>();

            builder.Append("UPDATE t0 SET");
            builder.AppendNewLine();

            if (uQueryInfo.Entity != null)
            {
                object entity = uQueryInfo.Entity;
                ISqlBuilder whereBuilder = this.CreateSqlBuilder(token);
                bool useKey = false;
                int length = 0;

                foreach (var kv in typeRuntime.Invokers)
                {
                    MemberInvokerBase invoker = kv.Value;
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
                    var seg = builder.GetSqlValueWidthDefault(value, column);

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

                if (!useKey) throw new XFrameworkException("Update<T>(T value) require T must have key column.");

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
            else if (uQueryInfo.Expression != null)
            {
                TableAliasCache aliases = this.PrepareAlias<T>(uQueryInfo.SelectInfo, token);
                ExpressionVisitorBase visitor = null;
                visitor = new UpdateExpressionVisitor(this, aliases, uQueryInfo.Expression);
                visitor.Write(builder);

                builder.AppendNewLine();
                builder.Append("FROM ");
                builder.AppendMember(typeRuntime.TableName, !typeRuntime.IsTemporary);
                builder.AppendAs("t0");

                var cmd2 = new SelectCommand(this, aliases, token) { HasManyNavigation = uQueryInfo.SelectInfo.HasManyNavigation };

                visitor = new JoinExpressionVisitor(this, aliases, uQueryInfo.SelectInfo.Joins);
                visitor.Write(cmd2.JoinFragment);

                visitor = new WhereExpressionVisitor(this, aliases, uQueryInfo.SelectInfo.WhereExpression);
                visitor.Write(cmd2.WhereFragment);
                cmd2.AddNavMembers(visitor.NavMembers);

                builder.Append(cmd2.CommandText);
            }

            return new Command(builder.ToString(), builder.Token != null ? builder.Token.Parameters : null, System.Data.CommandType.Text);
        }

        /// <summary>
        /// 单例提供者
        /// </summary>
        class Singleton
        {
            /// <summary>
            /// 查询语义提供者实例
            /// </summary>
            public static SqlDbQueryProvider Instance = new SqlDbQueryProvider();
        }
    }
}
