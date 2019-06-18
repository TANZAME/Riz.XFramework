
using System.Data;
using System.Collections.Generic;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// Oracle INSERT命令描述
    /// </summary>
    public sealed class OracleInsertDbCommandDefinition : DbCommandDefinition
    {
        /// <summary>
        /// 是否有 SEQUENCE 列
        /// </summary>
        public bool HaveSEQ { get; set; }

        /// <summary>
        /// 初始化 <see cref="OracleInsertDbCommandDefinition"/> 类的新实例
        /// </summary>
        public OracleInsertDbCommandDefinition(string commandText, List<IDbDataParameter> parameters = null, CommandType? commandType = null)
            : base(commandText, parameters, commandType)
        {
        }
    }
}
