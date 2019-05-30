
using System;
using System.Data;

namespace ICS.XFramework.Data
{
    /// <summary>
    /// 数据库命令执行拦截器
    /// </summary>
    public class DbCommandInterceptor : IDbCommandInterceptor
    {
        /// <summary>
        /// 初始化<see cref="DbCommandInterceptor"/>类的新实例
        /// </summary>
        public DbCommandInterceptor()
        {
 
        }

        /// <summary>
        /// 执行 SQL 前的动作
        /// </summary>
        public Action<IDbCommand> OnExecuting { get; set; }

        /// <summary>
        /// 执行 SQL 后的动作
        /// </summary>
        public Action<IDbCommand> OnExecuted { get; set; }

        /// <summary>
        /// 执行 SQL 出现异常的动作
        /// </summary>
        public Action<DbCommandException> OnException { get; set; }

        /// <summary>
        /// 执行 SQL 前
        /// </summary>
        public void OnDbCommandExecuting(IDbCommand cmd)
        {
            if (this.OnExecuting != null) this.OnExecuting(cmd);
        }

        /// <summary>
        /// 执行 SQL 后
        /// </summary>
        public void OnDbCommandExecuted(IDbCommand cmd)
        {
            if (this.OnExecuted != null) this.OnExecuted(cmd);
        }

        /// <summary>
        /// 执行 SQL 异常
        /// </summary>
        public void OnDbCommandException(DbCommandException e)
        {
            if (this.OnException != null) this.OnException(e);
        }
    }
}
