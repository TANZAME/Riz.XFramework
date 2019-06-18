using System.Data;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// 数据库命令执行拦截器
    /// </summary>
    public interface IDbCommandInterceptor
    {
        /// <summary>
        /// 执行 SQL 命令前
        /// </summary>
        void OnDbCommandExecuting(IDbCommand cmd);

        /// <summary>
        /// 执行 SQL 命令后
        /// </summary>
        void OnDbCommandExecuted(IDbCommand cmd);

        /// <summary>
        /// 执行 SQL 命令出错
        /// </summary>
        /// <param name="e"></param>
        void OnDbCommandException(DbCommandException e);
    }
}
