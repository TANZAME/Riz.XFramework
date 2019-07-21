
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Collections.Generic;
using Npgsql;

namespace TZM.XFramework.Data.SqlClient
{
    /// <summary>
    /// 数据查询提供者
    /// </summary>
    public sealed class NpgDbQueryProvider : DbQueryProvider
    {
        // 注意：在core 版本在，nuget上的ngpsql.4.0.6.dll standard版本不能运行~
        // 需要去github上下载4.1.0版本重新编译成本地包再引用

        /// <summary>
        /// 查询语义提供者 单例
        /// </summary>
        public static NpgDbQueryProvider Instance = new NpgDbQueryProvider();

        /// <summary>
        /// 数据源类的提供程序实现的实例
        /// </summary>
        public override DbProviderFactory DbProviderFactory { get { return NpgsqlFactory.Instance; } }

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
                return "Postgre";
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
        /// 实例化 <see cref="NpgDbQueryProvider"/> 类的新实例
        /// </summary>
        private NpgDbQueryProvider() : base()
        {

        }

        /// <summary>
        /// 创建 SQL 构造器
        /// </summary>
        /// <returns></returns>
        public override ISqlBuilder CreateSqlBuilder(List<IDbDataParameter> parameters = null)
        {
            return new NpgSqlBuilder(this, parameters);
        }

        /// <summary>
        /// 创建方法表达式访问器
        /// </summary>
        /// <returns></returns>
        public override IMethodCallExressionVisitor CreateMethodCallVisitor(ExpressionVisitorBase visitor)
        {
            return new NpgMethodCallExressionVisitor(this, visitor);
        }

        // 创建 SELECT 命令
        protected override Command ParseSelectCommand<T>(DbQueryableInfo_Select<T> sQuery, int indent = 0, bool isOuter = true, List<IDbDataParameter> parameters = null)
        {
            var cmd = (SelectCommand)this.ParseSelectCommandImpl<T>(sQuery, indent, isOuter, parameters);
            cmd.Convergence();
            if (isOuter) cmd.JoinFragment.Append(';');
            return cmd;
        }

        // 创建 SELECT 命令
        Command ParseSelectCommandImpl<T>(DbQueryableInfo_Select<T> sQuery, int indent, bool isOuter, List<IDbDataParameter> parameters = null)
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
            DbQueryableInfo_Select<T> innerQuery = sQuery.SubQueryInfo as DbQueryableInfo_Select<T>;
            if (sQuery.HaveListNavigation && innerQuery != null && innerQuery.Statis != null) sQuery = innerQuery;

            bool useNesting = sQuery.HaveDistinct || sQuery.GroupBy != null || sQuery.Skip > 0 || sQuery.Take > 0;
            bool useStatis = sQuery.Statis != null;
            // 没有统计函数或者使用 'Skip' 子句，则解析OrderBy
            // 导航属性如果使用嵌套，除非有 TOP 或者 OFFSET 子句，否则不能用ORDER BY
            bool useOrderBy = (!useStatis || sQuery.Skip > 0) && !sQuery.HaveAny && (!sQuery.ResultByListNavigation || (sQuery.Skip > 0 || sQuery.Take > 0));

            IDbQueryable dbQueryable = sQuery.SourceQuery;
            TableAliasCache aliases = this.PrepareAlias<T>(sQuery);
            SelectCommand cmd = new SelectCommand(this, aliases, parameters) { HaveListNavigation = sQuery.HaveListNavigation };
            ISqlBuilder jf = cmd.JoinFragment;
            ISqlBuilder wf = cmd.WhereFragment;
            (jf as NpgSqlBuilder).IsOuter = isOuter;

            jf.Indent = indent;

            #region 嵌套查询

            if (useStatis && useNesting)
            {
                // SELECT
                jf.Append("SELECT ");
                jf.AppendNewLine();

                // SELECT COUNT(1)
                var visitor2 = new StatisExpressionVisitor(this, aliases, sQuery.Statis, sQuery.GroupBy, "t0");
                visitor2.Write(jf);
                cmd.AddNavMembers(visitor2.NavMembers);

                // SELECT COUNT(1) FROM
                jf.AppendNewLine();
                jf.Append("FROM ( ");

                indent += 1;
                jf.Indent = indent;
                (jf as NpgSqlBuilder).IsOuter = false;
            }

            #endregion

            #region 选择子句

            // SELECT 子句
            if (jf.Indent > 0) jf.AppendNewLine();
            jf.Append("SELECT ");

            if (sQuery.HaveAny)
            {
                jf.Append("CASE WHEN COUNT(1) = 1 THEN 1 ELSE 0 END FROM (");
                indent += 1;
                jf.Indent = indent;
                jf.AppendNewLine();
                jf.Append("SELECT 1 ");
            }

            if (useStatis && !useNesting)
            {
                // 如果有统计函数，并且不是嵌套的话，则直接使用SELECT <MAX,MIN...>，不需要解析选择的字段
                jf.AppendNewLine();
                var visitor2 = new StatisExpressionVisitor(this, aliases, sQuery.Statis, sQuery.GroupBy);
                visitor2.Write(jf);
                cmd.AddNavMembers(visitor2.NavMembers);
            }
            else
            {

                // DISTINCT 子句
                if (sQuery.HaveDistinct) jf.Append("DISTINCT ");

                #region 选择字段

                if (!sQuery.HaveAny)
                {
                    // SELECT 范围
                    var visitor2 = new ColumnExpressionVisitor(this, aliases, sQuery);
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
            if (sQuery.SubQueryInfo != null)
            {
                // 子查询
                jf.Append('(');
                Command cmd2 = this.ParseSelectCommandImpl<T>(sQuery.SubQueryInfo as DbQueryableInfo_Select<T>, indent + 1, false, jf.Parameters);
                jf.Append(cmd2.CommandText);
                jf.AppendNewLine();
                jf.Append(") t0 ");
            }
            else
            {
                var typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo(sQuery.FromType);
                jf.AppendMember(typeRuntime.TableName, !typeRuntime.IsTemporary);
                jf.Append(" t0 ");
                //if (dbQueryable.DbContext.NoLock && !string.IsNullOrEmpty(this.WidthNoLock)) jf.Append(this.WidthNoLock);
            }

            // LEFT<INNER> JOIN 子句
            ExpressionVisitorBase visitor = new JoinExpressionVisitor(this, aliases, sQuery.Join);
            visitor.Write(jf);

            wf.Indent = jf.Indent;

            // WHERE 子句
            visitor = new WhereExpressionVisitor(this, aliases, sQuery.Where);
            visitor.Write(wf);
            cmd.AddNavMembers(visitor.NavMembers);

            // GROUP BY 子句
            visitor = new GroupByExpressionVisitor(this, aliases, sQuery.GroupBy);
            visitor.Write(wf);
            cmd.AddNavMembers(visitor.NavMembers);

            // HAVING 子句
            visitor = new HavingExpressionVisitor(this, aliases, sQuery.Having, sQuery.GroupBy);
            visitor.Write(wf);
            cmd.AddNavMembers(visitor.NavMembers);

            // ORDER 子句
            if (sQuery.OrderBy.Count > 0 && useOrderBy)
            {
                visitor = new OrderByExpressionVisitor(this, aliases, sQuery.OrderBy, sQuery.GroupBy);
                visitor.Write(wf);
                cmd.AddNavMembers(visitor.NavMembers);
            }

            #endregion

            #region 分页查询

            if (sQuery.Take > 0) wf.AppendNewLine().AppendFormat("LIMIT {0}", wf.GetSqlValue(sQuery.Take));
            if (sQuery.Skip > 0) wf.AppendFormat(" OFFSET {0}", wf.GetSqlValue(sQuery.Skip));

            #endregion

            #region 嵌套查询

            if (useStatis && useNesting)
            {
                cmd.Convergence();
                indent -= 1;
                jf.Indent = indent;
                jf.AppendNewLine();
                jf.Append(" ) t0");
            }

            #endregion

            #region 嵌套导航

            // TODO Include 从表，没分页，OrderBy 报错
            if (sQuery.HaveListNavigation && innerQuery != null && innerQuery.OrderBy.Count > 0 && innerQuery.Statis == null && !(innerQuery.Skip > 0 || innerQuery.Take > 0))
            {
                cmd.Convergence();
                visitor = new OrderByExpressionVisitor(this, aliases, innerQuery.OrderBy);//, null, "t0");
                visitor.Write(jf);
            }

            #endregion

            #region 并集查询

            // UNION 子句
            if (sQuery.Union != null && sQuery.Union.Count > 0)
            {
                cmd.Convergence();
                for (int index = 0; index < sQuery.Union.Count; index++)
                {
                    jf.AppendNewLine();
                    jf.Append("UNION ALL");
                    if (indent == 0) jf.AppendNewLine();
                    Command cmd2 = this.ParseSelectCommandImpl<T>(sQuery.Union[index] as DbQueryableInfo_Select<T>, indent, isOuter, jf.Parameters);
                    jf.Append(cmd2.CommandText);
                }
            }

            #endregion

            #region Any 子句

            // 'Any' 子句
            if (sQuery.HaveAny)
            {
                // 产生 WHERE 子句
                cmd.Convergence();

                // 如果没有分页，则显式指定只查一笔记录
                if (sQuery.Take == 0 && sQuery.Skip == 0)
                {
                    jf.AppendNewLine();
                    jf.Append("LIMIT 1");
                }

                indent -= 1;
                jf.Indent = indent;
                jf.AppendNewLine();
                jf.Append(") t0");
            }

            #endregion

            return cmd;
        }

        // 创建 INSRT 命令
        protected override Command ParseInsertCommand<T>(DbQueryableInfo_Insert<T> nQuery, List<IDbDataParameter> parameters = null)
        {
            ISqlBuilder builder = this.CreateSqlBuilder(parameters);
            TypeRuntimeInfo typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo<T>();
            TableAliasCache aliases = new TableAliasCache();

            if (nQuery.Entity != null)
            {
                object entity = nQuery.Entity;
                ISqlBuilder columnsBuilder = this.CreateSqlBuilder(builder.Parameters);
                ISqlBuilder valuesBuilder = this.CreateSqlBuilder(builder.Parameters);

                // 指定插入列
                Dictionary<string, MemberInvokerBase> invokers = typeRuntime.Invokers;
                if (nQuery.EntityColumns != null && nQuery.EntityColumns.Count > 0)
                {
                    invokers = new Dictionary<string, MemberInvokerBase>();
                    for (int i = 0; i < nQuery.EntityColumns.Count; i++)
                    {
                        Expression curExpr = nQuery.EntityColumns[i];
                        if (curExpr.NodeType == ExpressionType.Lambda) curExpr = (curExpr as LambdaExpression).Body.ReduceUnary();
                        if (curExpr.NodeType != ExpressionType.MemberAccess)
                            throw new XFrameworkException("Can't read field name from expression {0}", nQuery.EntityColumns[i]);

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
                    if (invoker.Member.MemberType == System.Reflection.MemberTypes.Method) continue;

                    if (invoker != nQuery.AutoIncrement)
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

                if (nQuery.Bulk == null || !nQuery.Bulk.OnlyValue)
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

                if (nQuery.Bulk != null && nQuery.Bulk.OnlyValue) builder.AppendNewTab();
                builder.Append('(');
                builder.Append(valuesBuilder);
                builder.Append(')');
                if (nQuery.Bulk != null && !nQuery.Bulk.IsEndPos)
                {
                    builder.Append(",");
                    builder.AppendNewLine();
                }

                if (nQuery.Bulk == null && nQuery.AutoIncrement != null)
                {
                    builder.Append(';');
                    builder.AppendNewLine();

                    // sequence，命名原则是 tablename_columnname_seq.
                    builder.AppendFormat("SELECT CURRVAL('{0}_{1}_seq')", typeRuntime.TableName, nQuery.AutoIncrement.Member.Name);
                    builder.Append(" AS ");
                    builder.Append(this.QuotePrefix);
                    builder.Append(Constant.AUTOINCREMENTNAME);
                    builder.Append(this.QuoteSuffix);
                }
            }
            else if (nQuery.SelectInfo != null)
            {
                builder.Append("INSERT INTO ");
                builder.AppendMember(typeRuntime.TableName, !typeRuntime.IsTemporary);
                builder.Append('(');

                int i = 0;
                SelectCommand cmd2 = this.ParseSelectCommandImpl(nQuery.SelectInfo, 0, false, builder.Parameters) as SelectCommand;
                //for (int i = 0; i < seg.Columns.Count; i++)
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

            if (nQuery.Bulk == null || nQuery.Bulk.IsEndPos) builder.Append(';');
            return new Command(builder.ToString(), builder.Parameters, System.Data.CommandType.Text);
        }

        // 创建 DELETE 命令
        protected override Command ParseDeleteCommand<T>(DbQueryableInfo_Delete<T> dQuery, List<IDbDataParameter> parameters = null)
        {
            TypeRuntimeInfo typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo<T>();
            ISqlBuilder builder = this.CreateSqlBuilder(parameters);
            bool useKey = false;

            builder.Append("DELETE FROM ");
            builder.AppendMember(typeRuntime.TableName, !typeRuntime.IsTemporary);
            builder.Append(" t0 ");

            if (dQuery.Entity != null)
            {
                object entity = dQuery.Entity;

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
            else if (dQuery.SelectInfo != null)
            {
                TableAliasCache aliases = this.PrepareAlias<T>(dQuery.SelectInfo);
                var cmd2 = new NpgSelectInfoCommand(this, aliases, NpgCommandType.DELETE, builder.Parameters);
                cmd2.HaveListNavigation = dQuery.SelectInfo.HaveListNavigation;

                var visitor0 = new NpgExistsExpressionVisitor(this, aliases, dQuery.SelectInfo.Join, NpgCommandType.DELETE);
                visitor0.Write(cmd2);

                var visitor1 = new NpgWhereExpressionVisitor(this, aliases, dQuery.SelectInfo.Where);
                visitor1.Write(cmd2.WhereFragment);
                cmd2.AddNavMembers(visitor1.NavMembers);
                builder.Append(cmd2.CommandText);
            }

            builder.Append(';');
            return new Command(builder.ToString(), builder.Parameters, System.Data.CommandType.Text);
        }

        // 创建 UPDATE 命令
        protected override Command ParseUpdateCommand<T>(DbQueryableInfo_Update<T> uQuery, List<IDbDataParameter> parameters = null)
        {
            ISqlBuilder builder = this.CreateSqlBuilder(parameters);
            var typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo<T>();

            builder.Append("UPDATE ");
            builder.AppendMember(typeRuntime.TableName, !typeRuntime.IsTemporary);
            builder.Append(" t0 SET");
            builder.AppendNewLine();

            if (uQuery.Entity != null)
            {
                object entity = uQuery.Entity;
                ISqlBuilder whereBuilder = this.CreateSqlBuilder(builder.Parameters);
                bool useKey = false;
                int length = 0;

                foreach (var kv in typeRuntime.Invokers)
                {
                    MemberInvokerBase invoker = kv.Value;
                    var column = invoker.Column;
                    if (column != null && column.IsIdentity) goto gotoLabel; // fix issue# 自增列同时又是主键
                    if (column != null && column.NoMapped) continue;
                    if (invoker.ForeignKey != null) continue;
                    if (invoker.Member.MemberType == System.Reflection.MemberTypes.Method) continue;

                    builder.AppendMember(invoker.Member.Name);
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
                        whereBuilder.AppendMember(invoker.Member.Name);
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
            else if (uQuery.Expression != null)
            {
                TableAliasCache aliases = this.PrepareAlias<T>(uQuery.SelectInfo);
                ExpressionVisitorBase visitor = null;
                visitor = new NpgUpdateExpressionVisitor(this, aliases, uQuery.Expression);
                visitor.Write(builder);

                var cmd2 = new NpgSelectInfoCommand(this, aliases, NpgCommandType.UPDATE, builder.Parameters);
                cmd2.HaveListNavigation = uQuery.SelectInfo.HaveListNavigation;

                var visitor0 = new NpgExistsExpressionVisitor(this, aliases, uQuery.SelectInfo.Join, NpgCommandType.UPDATE);
                visitor0.Write(cmd2);

                var visitor1 = new NpgWhereExpressionVisitor(this, aliases, uQuery.SelectInfo.Where);
                visitor1.Write(cmd2.WhereFragment);
                cmd2.AddNavMembers(visitor1.NavMembers);
                builder.Append(cmd2.CommandText);
            }

            builder.Append(';');
            return new Command(builder.ToString(), builder.Parameters, System.Data.CommandType.Text);
        }
    }
}
