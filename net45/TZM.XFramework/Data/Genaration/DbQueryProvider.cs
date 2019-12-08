
using System;
using System.Data;
using System.Linq;
using System.Data.Common;
using System.Linq.Expressions;
using System.Collections.Generic;

namespace TZM.XFramework.Data
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
        public abstract DbProviderFactory DbProviderFactory { get; }

        /// <summary>
        /// SQL字段值生成器
        /// </summary>
        public abstract DbValue DbValue { get; }

        #endregion

        #region 构造函数

        #endregion

        #region 接口实现

        /// <summary>
        /// 创建 SQL 命令
        /// </summary>
        /// <param name="dbQueryable">查询 语句</param>
        public Command Resolve<T>(IDbQueryable<T> dbQueryable)
        {
            return this.Resolve(dbQueryable, 0, true, null);
        }

        /// <summary>
        /// 创建 SQL 命令
        /// </summary>
        /// <param name="dbQuery">查询 语句</param>
        /// <param name="indent">缩进</param>
        /// <param name="isOuter">是否最外层，内层查询不需要结束符(;)</param>
        /// <param name="token">解析上下文参数</param>
        /// <returns></returns>
        public Command Resolve<T>(IDbQueryable<T> dbQuery, int indent, bool isOuter, ResolveToken token)
        {
            // 参数化设置
            if (token == null) token = new ResolveToken();
            if (!dbQuery.HasSetParameterized) dbQuery.Parameterized = true;
            if (dbQuery.Parameterized && token.Parameters == null) token.Parameters = new List<IDbDataParameter>(8);
            if (token.DbContext == null) token.DbContext = dbQuery.DbContext;

            // 解析查询语义
            IDbQueryableInfo result = DbQueryParser.Parse(dbQuery);
            // 查询
            var result_Query = result as IDbQueryableInfo_Select;
            if (result_Query != null) return this.ResolveSelectCommand(result_Query, indent, isOuter, token);
            // 新增
            var result_Insert = result as IDbQueryableInfo_Insert;
            if (result_Insert != null) return this.ResolveInsertCommand(result_Insert, token);
            // 更新
            var result_Update = result as IDbQueryableInfo_Update;
            if (result_Update != null) return this.ResolveUpdateCommand(result_Update, token);
            // 删除
            var result_Delete = result as IDbQueryableInfo_Delete;
            if (result_Delete != null) return this.ResolveDeleteCommand(result_Delete, token);

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
        public virtual List<Command> Resolve(List<object> dbQueryables)
        {
            List<Command> sqlList = new List<Command>();
            ResolveToken token = null;

            foreach (var obj in dbQueryables)
            {
                if (obj == null) continue;

                if (obj is IDbQueryable)
                {
                    IDbQueryable dbQueryable = (IDbQueryable)obj;
                    dbQueryable.Parameterized = true;
                    if (token == null) token = new ResolveToken();
                    if (token.Parameters == null) token.Parameters = new List<IDbDataParameter>(8);

                    var cmd2 = dbQueryable.Resolve(0, true, token);
                    sqlList.Add(cmd2);
                    if (cmd2.Parameters != null && cmd2.Parameters.Count > 1000)
                    {
                        // 1000个参数，就要重新分批
                        sqlList.Add(null);
                        token = new ResolveToken();
                        token.Parameters = new List<IDbDataParameter>(8);
                    }
                }
                else if (obj is RawSql)
                {
                    RawSql rawSql = (RawSql)obj;
                    if (token == null) token = new ResolveToken();
                    if (token.Parameters == null) token.Parameters = new List<IDbDataParameter>(8);

                    // 解析参数
                    object[] args = null;
                    if (rawSql.Parameters != null)
                        args = rawSql.Parameters.Select(x => this.DbValue.GetSqlValue(x, token)).ToArray();
                    string sql = rawSql.CommandText;
                    if (args != null && args.Length > 0) sql = string.Format(sql, args);

                    var cmd2 = new Command(sql, token.Parameters, CommandType.Text);
                    sqlList.Add(cmd2);
                    if (cmd2.Parameters != null && cmd2.Parameters.Count > 1000)
                    {
                        // 1000个参数，就要重新分批
                        sqlList.Add(null);
                        token = new ResolveToken();
                        token.Parameters = new List<IDbDataParameter>(8);
                    }
                }
                else if (obj is string)
                {
                    string sql = obj.ToString();
                    sqlList.Add(new Command(sql));
                }
                else
                {
                    // 解析批量插入操作
                    List<IDbQueryable> bulkList = obj as List<IDbQueryable>;
                    if (bulkList != null && bulkList.Count > 0) this.ResolveBulk(sqlList, bulkList);
                }
            }

            return sqlList;
        }

        /// <summary>
        /// 创建 SQL 构造器
        /// </summary>
        /// <param name="parameter">参数列表，NULL 或者 Parameters=NULL 时表示不使用参数化</param>
        /// <returns></returns>
        public abstract ISqlBuilder CreateSqlBuilder(ResolveToken parameter);

        /// <summary>
        /// 创建方法表达式访问器
        /// </summary>
        /// <returns></returns>
        public abstract MethodCallExpressionVisitor CreateMethodVisitor(ExpressionVisitorBase visitor);

        #endregion

        #region 私有函数

        /// <summary>
        /// 解析 SELECT 命令
        /// </summary>
        /// <param name="dbQuery">查询语义</param>
        /// <param name="indent">缩进</param>
        /// <param name="isOuter">指示是最外层查询</param>
        /// <param name="token">解析上下文</param>
        /// <returns></returns>
        protected abstract Command ResolveSelectCommand(IDbQueryableInfo_Select dbQuery, int indent, bool isOuter, ResolveToken token);

        /// <summary>
        /// 创建 INSRT 命令
        /// </summary>
        /// <param name="dbQuery">查询语义</param>
        /// <param name="token">解析上下文</param>
        /// <returns></returns>
        protected abstract Command ResolveInsertCommand(IDbQueryableInfo_Insert dbQuery, ResolveToken token);

        /// <summary>
        /// 创建 DELETE 命令
        /// </summary>
        /// <param name="dbQuery">查询语义</param>
        /// <param name="token">解析上下文</param>
        /// <returns></returns>
        protected abstract Command ResolveDeleteCommand(IDbQueryableInfo_Delete dbQuery, ResolveToken token);

        /// <summary>
        /// 创建 UPDATE 命令
        /// </summary>
        /// <param name="dbQuery">查询语义</param>
        /// <param name="token">解析上下文</param>
        /// <returns></returns>
        protected abstract Command ResolveUpdateCommand(IDbQueryableInfo_Update dbQuery, ResolveToken token);

        /// <summary>
        /// 生成关联子句所表示的别名列表
        /// </summary>
        /// <param name="dbQuery">查询语义</param>
        /// <param name="token">解析上下文</param>
        /// <returns></returns>
        protected TableAliasCache PrepareTableAlias(IDbQueryableInfo_Select dbQuery, ResolveToken token)
        {
            var aliases = new TableAliasCache((dbQuery.Joins != null ? dbQuery.Joins.Count : 0) + 1, token != null ? token.AliasPrefix : null);
            foreach (DbExpression exp in dbQuery.Joins)
            {
                // [INNER/LEFT JOIN]
                if (exp.DbExpressionType == DbExpressionType.GroupJoin || exp.DbExpressionType == DbExpressionType.Join || exp.DbExpressionType == DbExpressionType.GroupRightJoin)
                    this.PrepareJoinAlias(exp, aliases);
                else if (exp.DbExpressionType == DbExpressionType.SelectMany)
                    this.PrepareCrossAlias(exp, aliases);
            }

            return aliases;
        }

        // 获取 LEFT JOIN / INNER JOIN 子句关联表的的别名
        private void PrepareJoinAlias(DbExpression dbExpression, TableAliasCache aliases)
        {
            Type type = dbExpression.Expressions[0].Type.GetGenericArguments()[0];
            string name = TypeRuntimeInfoCache.GetRuntimeInfo(type).TableName;

            // on a.Name equals b.Name 或 on new{ Name = a.Name,Id=a.Id } equals new { Name = b.Name,Id=b.Id }
            LambdaExpression left = dbExpression.Expressions[1] as LambdaExpression;
            LambdaExpression right = dbExpression.Expressions[2] as LambdaExpression;
            if (left.Body.NodeType == ExpressionType.New)
            {
                NewExpression body1 = left.Body as NewExpression;
                NewExpression body2 = right.Body as NewExpression;
                for (int index = 0; index < body1.Arguments.Count; ++index)
                {
                    aliases.GetTableAlias(body1.Arguments[index]);
                }
                for (int index = 0; index < body2.Arguments.Count; ++index)
                {
                    string alias = aliases.GetTableAlias(body2.Arguments[index]);
                    // 记录显示指定的LEFT JOIN 表别名
                    aliases.AddOrUpdateJoinTableAlias(name, alias);
                }
            }
            else if (left.Body.NodeType == ExpressionType.MemberInit)
            {
                MemberInitExpression body1 = left.Body as MemberInitExpression;
                MemberInitExpression body2 = right.Body as MemberInitExpression;
                for (int index = 0; index < body1.Bindings.Count; ++index)
                {
                    aliases.GetTableAlias((body1.Bindings[index] as MemberAssignment).Expression);
                }
                for (int index = 0; index < body2.Bindings.Count; ++index)
                {
                    string alias = aliases.GetTableAlias((body2.Bindings[index] as MemberAssignment).Expression);
                    // 记录显示指定的LEFT JOIN 表别名
                    aliases.AddOrUpdateJoinTableAlias(name, alias);
                }
            }
            else
            {
                aliases.GetTableAlias(dbExpression.Expressions[1]);
                string alias = aliases.GetTableAlias(dbExpression.Expressions[2]);
                // 记录显示指定的LEFT JOIN 表别名
                aliases.AddOrUpdateJoinTableAlias(name, alias);
            }
        }

        // 获取 CROSS JOIN 子句关联表的的别名
        private void PrepareCrossAlias(DbExpression dbExpression, TableAliasCache aliases)
        {
            LambdaExpression lambdaExp = dbExpression.Expressions[1] as LambdaExpression;
            for (int index = 0; index < lambdaExp.Parameters.Count; ++index)
            {
                aliases.GetTableAlias(lambdaExp.Parameters[index]);
            }
        }

        /// <summary>
        /// 解析批量 INSERT 语句
        /// </summary>
        /// <param name="sqlList">SQL 命令列表 </param>
        /// <param name="bulkList">批量查询语义列表</param>
        protected void ResolveBulk(List<Command> sqlList, List<IDbQueryable> bulkList)
        {
            // SQL 只能接收1000个
            int pageSize = 1000;
            int pages = bulkList.Page(pageSize);
            for (int pageIndex = 1; pageIndex <= pages; pageIndex++)
            {
                var dbQueryables = bulkList.Skip((pageIndex - 1) * pageSize).Take(pageSize);
                int i = 0;
                int count = dbQueryables.Count();
                var builder = new System.Text.StringBuilder(128);

                foreach (IDbQueryable query in dbQueryables)
                {
                    i += 1;
                    query.Parameterized = false;
                    query.Bulk = new BulkInsertInfo { OnlyValue = i != 1, IsEndPos = i == count };

                    Command cmd = query.Resolve();
                    builder.Append(cmd.CommandText);
                }

                if (builder.Length > 0) sqlList.Add(new Command(builder.ToString()));
            }
        }

        #endregion
    }
}
