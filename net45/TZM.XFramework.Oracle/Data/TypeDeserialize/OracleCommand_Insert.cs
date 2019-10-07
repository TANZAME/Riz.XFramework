
using System.Data;
using System.Collections.Generic;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// Oracle INSERT命令描述
    /// </summary>
    public sealed class OracleCommand_Insert : Command
    {
        /// <summary>
        /// 是否有 SEQUENCE 列
        /// </summary>
        public bool HaveSEQ { get; set; }

        /// <summary>
        /// 初始化 <see cref="OracleCommand_Insert"/> 类的新实例
        /// </summary>
        public OracleCommand_Insert(string commandText, List<IDbDataParameter> parameters = null, CommandType? commandType = null)
            : base(commandText, parameters, commandType)
        {
        }
    }
}
