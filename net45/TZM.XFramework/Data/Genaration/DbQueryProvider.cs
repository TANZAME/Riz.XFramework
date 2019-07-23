
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
        /// <param name="dbQueryable">查询 语句</param>
        /// <param name="indent">缩进</param>
        /// <param name="isOuter">是否最外层，内层查询不需要结束符(;)</param>
        /// <param name="token">解析上下文参数</param>
        /// <returns></returns>
        public Command Resolve<T>(IDbQueryable<T> dbQueryable, int indent, bool isOuter, ParserToken token)
        {
            // 设置该查询是否需要参数化
            if (!((DbQueryable)dbQueryable).HasSetParameterized) dbQueryable.Parameterized = true;
            if (dbQueryable.Parameterized)
            {
                if (token == null) token = new ParserToken();
                if (token.Parameters == null) token.Parameters = new List<IDbDataParameter>(8);
            }

            // 解析查询语义
            IDbQueryableInfo<T> info = DbQueryParser.Parse(dbQueryable);

            DbQueryableInfo_Select<T> sQuery = info as DbQueryableInfo_Select<T>;
            if (sQuery != null) return this.ParseSelectCommand<T>(sQuery, indent, isOuter, dbQueryable.Parameterized ? token : null);

            DbQueryableInfo_Insert<T> nQuery = info as DbQueryableInfo_Insert<T>;
            if (nQuery != null) return this.ParseInsertCommand<T>(nQuery, dbQueryable.Parameterized ? token : null);

            DbQueryableInfo_Update<T> uQuery = info as DbQueryableInfo_Update<T>;
            if (uQuery != null) return this.ParseUpdateCommand<T>(uQuery, dbQueryable.Parameterized ? token : null);

            DbQueryableInfo_Delete<T> dQuery = info as DbQueryableInfo_Delete<T>;
            if (dQuery != null) return this.ParseDeleteCommand<T>(dQuery, dbQueryable.Parameterized ? token : null);

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
            ParserToken token = null;

            foreach (var obj in dbQueryables)
            {
                if (obj == null) continue;

                if (obj is IDbQueryable)
                {
                    IDbQueryable dbQueryable = (IDbQueryable)obj;
                    dbQueryable.Parameterized = true;
                    if (token == null) token = new ParserToken();
                    if (token.Parameters == null) token.Parameters = new List<IDbDataParameter>(8);

                    var cmd2 = dbQueryable.Resolve(0, true, token);
                    sqlList.Add(cmd2);
                    if (cmd2.Parameters != null && cmd2.Parameters.Count > 1000)
                    {
                        // 1000个参数，就要重新分批
                        sqlList.Add(null);
                        token = new ParserToken();
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
        /// 创建数据会话
        /// </summary>
        /// <param name="connString">数据库连接字符串</param>
        /// <param name="commandTimeout">执行命令超时时间</param>
        /// <returns></returns>
        public virtual IDatabase CreateDbSession(string connString, int? commandTimeout)
        {
            return new Database(this.DbProviderFactory, connString)
            {
                CommandTimeout = commandTimeout
            };
        }

        /// <summary>
        /// 创建 SQL 构造器
        /// </summary>
        /// <param name="parameter">参数列表，NULL 或者 Parameters=NULL 时表示不使用参数化</param>
        /// <returns></returns>
        public abstract ISqlBuilder CreateSqlBuilder(ParserToken parameter);

        /// <summary>
        /// 创建方法表达式访问器
        /// </summary>
        /// <returns></returns>
        public abstract IMethodCallExressionVisitor CreateMethodCallVisitor(ExpressionVisitorBase visitor);

        #endregion

        #region 私有函数

        // 创建 SELECT 命令
        protected abstract Command ParseSelectCommand<T>(DbQueryableInfo_Select<T> sQuery, int indent, bool isOuter, ParserToken token);

        // 创建 INSRT 命令
        protected abstract Command ParseInsertCommand<T>(DbQueryableInfo_Insert<T> nQuery, ParserToken token);

        // 创建 DELETE 命令
        protected abstract Command ParseDeleteCommand<T>(DbQueryableInfo_Delete<T> dQuery, ParserToken token);

        // 创建 UPDATE 命令
        protected abstract Command ParseUpdateCommand<T>(DbQueryableInfo_Update<T> uQuery, ParserToken token);

        // 获取 JOIN 子句关联表的的别名
        protected TableAliasCache PrepareAlias<T>(DbQueryableInfo_Select<T> query)
        {
            TableAliasCache aliases = new TableAliasCache((query.Join != null ? query.Join.Count : 0) + 1);
            foreach (DbExpression exp in query.Join)
            {
                // [INNER/LEFT JOIN]
                if (exp.DbExpressionType == DbExpressionType.GroupJoin || exp.DbExpressionType == DbExpressionType.Join || exp.DbExpressionType == DbExpressionType.GroupRightJoin)
                    this.PrepareLfInJoinAlias(exp, aliases);
                else if (exp.DbExpressionType == DbExpressionType.SelectMany)
                    this.PrepareCrossJoinAlias(exp, aliases);
            }

            return aliases;
        }

        // 获取 LEFT JOIN / INNER JOIN 子句关联表的的别名
        private void PrepareLfInJoinAlias(DbExpression exp, TableAliasCache aliases)
        {
            Type type = exp.Expressions[0].Type.GetGenericArguments()[0];
            string name = TypeRuntimeInfoCache.GetRuntimeInfo(type).TableName;

            // on a.Name equals b.Name 或 on new{ Name = a.Name,Id=a.Id } equals new { Name = b.Name,Id=b.Id }
            LambdaExpression left = exp.Expressions[1] as LambdaExpression;
            LambdaExpression right = exp.Expressions[2] as LambdaExpression;
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
                aliases.GetTableAlias(exp.Expressions[1]);
                string alias = aliases.GetTableAlias(exp.Expressions[2]);
                // 记录显示指定的LEFT JOIN 表别名
                aliases.AddOrUpdateJoinTableAlias(name, alias);
            }
        }

        // 获取 CROSS JOIN 子句关联表的的别名
        private void PrepareCrossJoinAlias(DbExpression exp, TableAliasCache aliases)
        {
            LambdaExpression lambdaExp = exp.Expressions[1] as LambdaExpression;
            for (int index = 0; index < lambdaExp.Parameters.Count; ++index)
            {
                aliases.GetTableAlias(lambdaExp.Parameters[index]);
            }
        }

        // 解析批量 INSERT 语句
        protected void ResolveBulk(List<Command> sqlList, List<IDbQueryable> bulkList)
        {
            // SQL 只能接收1000个
            int pageSize = 1000;
            int pages = bulkList.Page(pageSize);
            for (int pageIndex = 1; pageIndex <= pages; pageIndex++)
            {
                var dbQueryables = bulkList.Skip((pageIndex - 1) * pageSize).Take(pageSize);
                int i = 0;
                int t = dbQueryables.Count();
                var builder = new System.Text.StringBuilder(128);

                foreach (IDbQueryable query in dbQueryables)
                {
                    i += 1;
                    query.Parameterized = false;
                    query.Bulk = new BulkInsertInfo { OnlyValue = i != 1, IsEndPos = i == t };

                    Command cmd = query.Resolve();
                    builder.Append(cmd.CommandText);
                }

                if (builder.Length > 0) sqlList.Add(new Command(builder.ToString()));
            }
        }

        #endregion
    }


}
