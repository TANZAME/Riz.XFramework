
using System.Data;
using System.Collections.Generic;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// SQL 命令描述基类
    /// </summary>
    public class Command
    {
        private string _commandText;
        private List<IDbDataParameter> _parameters;
        private CommandType? _commandType;

        /// <summary>
        /// 针对数据源运行的文本命令
        /// </summary>
        public virtual string CommandText
        {
            get { return _commandText; }
            set { _commandText = value; }
        }

        /// <summary>
        /// 命令参数
        /// </summary>
        public virtual List<IDbDataParameter> Parameters
        {
            get { return _parameters; }
            set { _parameters = value; }
        }

        /// <summary>
        /// 命令类型
        /// </summary>
        public virtual CommandType? CommandType
        {
            get { return _commandType; }
            set { _commandType = value; }
        }

        /// <summary>
        /// 初始化 <see cref="Command"/> 类的新实例
        /// </summary>
        public Command(string commandText, List<IDbDataParameter> parameters = null, CommandType? commandType = null)
        {
            this._commandText = commandText;
            this._parameters = parameters;
            this._commandType = commandType;
        }
    }
}
