
using System.Data;
using System.Collections.Generic;

namespace Riz.XFramework.Data
{
    /// <summary>
    /// SQL 命令描述基类
    /// </summary>
    public class DbRawCommand
    {
        private string _commandText;
        private IList<IDbDataParameter> _parameters;
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
        public virtual IList<IDbDataParameter> Parameters
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
        /// 初始化 <see cref="DbRawCommand"/> 类的新实例
        /// </summary>
        public DbRawCommand(string commandText, IList<IDbDataParameter> parameters = null, CommandType? commandType = null)
        {
            this._commandText = commandText;
            this._parameters = parameters;
            this._commandType = commandType;
        }
    }
}
