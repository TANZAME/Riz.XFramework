
namespace TZM.XFramework.Data
{
    /// <summary>
    /// 原始 SQL 命令
    /// </summary>
    public sealed class RawSql
    {
        /// <summary>
        /// SQL 脚本
        /// </summary>
        public string CommandText { get; set; }

        /// <summary>
        /// 参数列表
        /// </summary>
        public object[] Parameters { get; set; }

        /// <summary>
        /// 初始化 <see cref="RawSql"/> 类的新实例
        /// </summary>
        /// <param name="commandText">SQL 脚本</param>
        /// <param name="parameters">参数列表</param>
        public RawSql(string commandText, params object[] parameters)
        {
            this.CommandText = commandText;
            this.Parameters = parameters;
        }
    }
}
