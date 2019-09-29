
using System.Data;
using System.Collections.Generic;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// SQL 命令描述基类
    /// </summary>
    public interface IDbCommandInfo
    {
        /// <summary>
        /// 针对数据源运行的文本命令
        /// </summary>
        string CommandText { get; set; }

        /// <summary>
        /// 命令参数
        /// </summary>
        List<IDbDataParameter> Parameters { get; set; }

        /// <summary>
        /// 命令类型
        /// </summary>
        CommandType? CommandType { get; set; }
    }
}
