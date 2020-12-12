
using System;
using System.Data;
using System.Linq;
using System.Data.Common;
using System.Linq.Expressions;
using System.Collections.Generic;

namespace Riz.XFramework.Data
{
    /// <summary>
    /// 查询语义提供者，用来构建、解析查询语义
    /// </summary>
    public abstract partial class DbQueryProvider : IDbQueryProvider
    {
        #region 公开属性

        /// <summary>
        /// 数据查询提供者 名称
        /// </summary>
        public abstract string ProviderName { get; }

        /// <summary>
        /// 左安全括符，如MS用[
        /// </summary>
        public abstract string QuotePrefix { get; }

        /// <summary>
        /// 右安全括符，如MS用]
        /// </summary>
        public abstract string QuoteSuffix { get; }

        /// <summary>
        /// 字符串单引号
        /// </summary>
        public abstract string SingleQuoteChar { get; }

        /// <summary>
        /// 命令参数前缀
        /// </summary>
        public abstract string ParameterPrefix { get; }

        /// <summary>
        /// 数据源类的提供程序实现的实例
        /// </summary>
        public abstract DbProviderFactory DbProvider { get; }

        /// <summary>
        /// 常量值转SQL表达式解析器
        /// </summary>
        protected internal abstract DbConstor Constor { get; }

        /// <summary>
        /// <see cref="IDataReader"/> 转实体映射器
        /// </summary>
        protected internal abstract TypeDeserializerImpl TypeDeserializerImpl { get; }

        #endregion

        #region 构造函数

        #endregion

        #region 接口实现

        /// <summary>
        /// 创建解析命令上下文
        /// </summary>
        /// <param name="context">当前查询上下文</param>
        /// <returns></returns>
        protected internal virtual ITranslateContext CreateTranslateContext(IDbContext context) => new TranslateContext(context);

        /// <summary>
        /// 创建 SQL 构造器
        /// </summary>
        /// <param name="context">解析SQL命令上下文</param>
        /// <returns></returns>
        protected internal virtual ISqlBuilder CreateSqlBuilder(ITranslateContext context) => new SqlBuilder(context);

        /// <summary>
        /// 创建方法表达式访问器
        /// </summary>
        /// <returns></returns>
        protected internal abstract MethodCallExpressionVisitor CreateMethodCallVisitor(DbExpressionVisitor visitor);

        /// <summary>
        /// 创建 SQL 命令
        /// </summary>
        /// <param name="dbQueryable">查询 语句</param>
        public DbRawCommand Translate<T>(IDbQueryable<T> dbQueryable) => this.Translate(dbQueryable, 0, true, null);

        /// <summary>
        /// 创建 SQL 命令
        /// </summary>
        /// <param name="dbQuery">查询 语句</param>
        /// <param name="indent">缩进</param>
        /// <param name="isOutQuery">是否最外层，内层查询不需要结束符(;)</param>
        /// <param name="context">解析SQL命令上下文</param>
        /// <returns></returns>
        internal DbRawCommand Translate<T>(IDbQueryable<T> dbQuery, int indent, bool isOutQuery, ITranslateContext context)
        {
            DbQueryable<T> query = (DbQueryable<T>)dbQuery;
            // 当前查询语义如果不设置参数化，默认使用参数化
            if (!query.HasSetParameterized) query.Parameterized = true;
            // 如果解析上下文是空，默认创建一个上下文，用来承载 DbContext （查询上下文）
            if (context == null) context = this.CreateTranslateContext(query.DbContext);
            // 如果使用参数化查询并且参数列表为空，默认创建了一个新的参数列表
            if (query.Parameterized && context.Parameters == null) context.Parameters = new List<IDbDataParameter>(8);

            // 解析查询语义
            IDbQueryTree tree = DbQueryableParser.Parse<T>(query);
            // 查询
            if (tree is DbQuerySelectTree) return this.TranslateSelectCommand((DbQuerySelectTree)tree, indent, isOutQuery, context);
            // 新增
            else if (tree is DbQueryInsertTree) return this.TranslateInsertCommand<T>((DbQueryInsertTree)tree, context);
            // 更新
            else if (tree is DbQueryUpdateTree) return this.TranslateUpdateCommand<T>((DbQueryUpdateTree)tree, context);
            // 删除
            else if (tree is DbQueryDeleteTree) return this.TranslateDeleteCommand<T>((DbQueryDeleteTree)tree, context);

            throw new NotImplementedException();
        }

        /// <summary>
        /// 解析 SQL 命令
        /// <para>
        /// 返回的已经解析语义中执行批次用 null 分开
        /// </para>
        /// </summary>
        /// <param name="dbQueryables">查询语句</param>
        /// <returns></returns>
        public virtual List<DbRawCommand> Translate(List<object> dbQueryables)
        {
            ITranslateContext context = null;
            var sqlList = new List<DbRawCommand>();

            foreach (var obj in dbQueryables)
            {
                if (obj == null) continue;

                if (obj is IDbQueryable)
                {
                    DbQueryable dbQuery = (DbQueryable)obj;
                    dbQuery.Parameterized = true;
                    if (context == null) context = this.CreateTranslateContext(dbQuery.DbContext);
                    if (context.Parameters == null) context.Parameters = new List<IDbDataParameter>(8);

                    var cmd = dbQuery.Translate(0, true, context);
                    sqlList.Add(cmd);
                    if (cmd.Parameters != null && cmd.Parameters.Count > 1000)
                    {
                        // 1000个参数，就要重新分批
                        sqlList.Add(null);
                        context = this.CreateTranslateContext(dbQuery.DbContext);
                        context.Parameters = new List<IDbDataParameter>(8);
                    }
                }
                else if (obj is DbRawSql)
                {
                    DbRawSql rawSql = (DbRawSql)obj;
                    if (context == null) context = this.CreateTranslateContext(rawSql.DbContext);
                    if (context.Parameters == null) context.Parameters = new List<IDbDataParameter>(8);

                    // 解析参数
                    object[] args = null;
                    if (rawSql.Parameters != null)
                        args = rawSql.Parameters.Select(x => this.Constor.GetSqlValue(x, context)).ToArray();
                    string sql = rawSql.CommandText;
                    if (args != null && args.Length > 0) sql = string.Format(sql, args);

                    var cmd = new DbRawCommand(sql, context.Parameters, CommandType.Text);
                    sqlList.Add(cmd);
                    if (cmd.Parameters != null && cmd.Parameters.Count > 1000)
                    {
                        // 1000个参数，就要重新分批
                        sqlList.Add(null);
                        context = this.CreateTranslateContext(rawSql.DbContext);
                        context.Parameters = new List<IDbDataParameter>(8);
                    }
                }
                else if (obj is string)
                {
                    string sql = obj.ToString();
                    sqlList.Add(new DbRawCommand(sql));
                }
                else
                {
                    // 解析批量插入操作
                    List<IDbQueryable> bulkList = obj as List<IDbQueryable>;
                    if (bulkList != null && bulkList.Count > 0) this.TranslateBulk(sqlList, bulkList);
                }
            }

            return sqlList;
        }

        #endregion

        #region 私有函数

        /// <summary>
        /// 解析 SELECT 命令
        /// </summary>
        /// <param name="tree"></param>
        /// <param name="indent">缩进</param>
        /// <param name="isOutQuery">是否是最外层查询</param>
        /// <param name="context">解析上下文</param>
        /// <returns></returns>
        protected abstract DbRawCommand TranslateSelectCommand(DbQuerySelectTree tree, int indent, bool isOutQuery, ITranslateContext context);

        /// <summary>
        /// 创建 INSRT 命令
        /// </summary>
        /// <param name="tree">查询语义</param>
        /// <param name="context">解析上下文</param>
        /// <returns></returns>
        protected abstract DbRawCommand TranslateInsertCommand<T>(DbQueryInsertTree tree, ITranslateContext context);

        /// <summary>
        /// 创建 DELETE 命令
        /// </summary>
        /// <param name="tree">查询语义</param>
        /// <param name="context">解析上下文</param>
        /// <returns></returns>
        protected abstract DbRawCommand TranslateDeleteCommand<T>(DbQueryDeleteTree tree, ITranslateContext context);

        /// <summary>
        /// 创建 UPDATE 命令
        /// </summary>
        /// <param name="tree">查询语义</param>
        /// <param name="context">解析上下文</param>
        /// <returns></returns>
        protected abstract DbRawCommand TranslateUpdateCommand<T>(DbQueryUpdateTree tree, ITranslateContext context);

        /// <summary>
        /// 生成关联子句所表示的别名列表
        /// </summary>
        /// <param name="tree">查询语义</param>
        /// <param name="prefix">表别名前缀</param>
        /// <returns></returns>
        protected AliasGenerator PrepareTableAlias(DbQuerySelectTree tree, string prefix)
        {
            var ag = new AliasGenerator((tree.Joins != null ? tree.Joins.Count : 0) + 1, prefix);
            for (int index = 0; index < (tree.Joins != null ? tree.Joins.Count : -1); index++)
            {
                DbExpression d = tree.Joins[index];
                // [INNER/LEFT JOIN]
                if (d.DbExpressionType == DbExpressionType.GroupJoin || d.DbExpressionType == DbExpressionType.Join || d.DbExpressionType == DbExpressionType.GroupRightJoin)
                    this.PrepareJoinAlias(d, ag);
                else if (d.DbExpressionType == DbExpressionType.SelectMany)
                    this.PrepareCrossAlias(d, ag);
            }

            return ag;
        }

        // 获取 LEFT JOIN / INNER JOIN 子句关联表的的别名
        private void PrepareJoinAlias(DbExpression dbExpression, AliasGenerator ag)
        {
            Type type = dbExpression.Expressions[0].Type.GetGenericArguments()[0];
            string name = TypeRuntimeInfoCache.GetRuntimeInfo(type).TableFullName;
            string outerAlias = null;

            // on a.Name equals b.Name 或 on new{ Name = a.Name,Id=a.Id } equals new { Name = b.Name,Id=b.Id }
            var left = dbExpression.Expressions[1] as LambdaExpression;
            var right = dbExpression.Expressions[2] as LambdaExpression;
            if (left.Body.NodeType == ExpressionType.New)
            {
                var body1 = left.Body as NewExpression;
                var body2 = right.Body as NewExpression;
                for (int index = 0; index < body1.Arguments.Count; ++index)
                {
                    ag.GetTableAlias(body1.Arguments[index]);
                }
                for (int index = 0; index < body2.Arguments.Count; ++index)
                {
                    string alias = ag.GetTableAlias(body2.Arguments[index]);
                    outerAlias = alias;
                    // 记录显示指定的LEFT JOIN 表别名
                    ag.AddJoinTableAlias(name, alias);
                }
            }
            else if (left.Body.NodeType == ExpressionType.MemberInit)
            {
                var body1 = left.Body as MemberInitExpression;
                var body2 = right.Body as MemberInitExpression;
                for (int index = 0; index < body1.Bindings.Count; ++index)
                {
                    ag.GetTableAlias((body1.Bindings[index] as MemberAssignment).Expression);
                }
                for (int index = 0; index < body2.Bindings.Count; ++index)
                {
                    string alias = ag.GetTableAlias((body2.Bindings[index] as MemberAssignment).Expression);
                    outerAlias = alias;
                    // 记录显示指定的LEFT JOIN 表别名
                    ag.AddJoinTableAlias(name, alias);
                }
            }
            else
            {
                ag.GetTableAlias(dbExpression.Expressions[1]);
                string alias = ag.GetTableAlias(dbExpression.Expressions[2]);
                outerAlias = alias;
                // 记录显示指定的LEFT JOIN 表别名
                ag.AddJoinTableAlias(name, alias);
            }

            // 由 GetTable 重载指定的导航属性表别名
            if (dbExpression.Expressions.Length > 4)
            {
                if (string.IsNullOrEmpty(outerAlias) || outerAlias == AliasGenerator.EMPTYNAME)
                {
                    var lambda = dbExpression.Expressions[3] as LambdaExpression;
                    string alias = ag.GetTableAlias(lambda.Parameters[1]);
                    outerAlias = alias;
                    // 记录显示指定的LEFT JOIN 表别名
                    ag.AddJoinTableAlias(name, alias);
                }

                var member = (dbExpression.Expressions[4] as LambdaExpression).Body as MemberExpression;
                string keyId = member.GetKeyWidthoutAnonymous();
                // 记录GetTable<,>(path)指定的表别名
                ag.AddGetTableAlias(keyId, outerAlias);
            }
        }

        // 获取 CROSS JOIN 子句关联表的的别名
        private void PrepareCrossAlias(DbExpression dbExpression, AliasGenerator ag)
        {
            var lambda = dbExpression.Expressions[1] as LambdaExpression;
            for (int index = 0; index < lambda.Parameters.Count; ++index)
            {
                ag.GetTableAlias(lambda.Parameters[index]);
            }
        }

        /// <summary>
        /// 解析批量 INSERT 语句
        /// </summary>
        /// <param name="sqlList">SQL 命令列表 </param>
        /// <param name="bulkQueryables">批量查询语义列表</param>
        protected void TranslateBulk(List<DbRawCommand> sqlList, List<IDbQueryable> bulkQueryables)
        {
            // SQL 只能接收1000个
            int pageSize = 1000;
            int pages = bulkQueryables.Page(pageSize);
            for (int pageIndex = 1; pageIndex <= pages; pageIndex++)
            {
                var dbQueryables = bulkQueryables.Skip((pageIndex - 1) * pageSize).Take(pageSize);
                int i = 0;
                int count = dbQueryables.Count();
                var builder = new System.Text.StringBuilder(128);

                foreach (DbQueryable query in dbQueryables)
                {
                    i += 1;
                    query.Parameterized = false;
                    query.Bulk = new BulkInsertInfo { OnlyValue = i != 1, IsEndPos = i == count };

                    DbRawCommand cmd = query.Translate();
                    builder.Append(cmd.CommandText);
                }

                if (builder.Length > 0) sqlList.Add(new DbRawCommand(builder.ToString()));
            }
        }

        #endregion
    }
}
