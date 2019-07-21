
using System.Data;
using System.Collections.Generic;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// Oracle INSERT命令描述
    /// </summary>
    public sealed class OracleInsertCommand : Command
    {
        /// <summary>
        /// 是否有 SEQUENCE 列
        /// </summary>
        public bool HaveSEQ { get; set; }

        /// <summary>
        /// 初始化 <see cref="OracleInsertCommand"/> 类的新实例
        /// </summary>
        public OracleInsertCommand(string commandText, List<IDbDataParameter> parameters = null, CommandType? commandType = null)
            : base(commandText, parameters, commandType)
        {
        }
    }
}
