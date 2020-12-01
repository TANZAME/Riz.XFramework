using System;
using System.Linq;
using System.Text;
using System.Data;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Riz.XFramework.Data
{
    /// <summary>
    /// 执行 SQL 命令时产生的异常
    /// </summary>
    public class DbCommandException : Exception
    {
        /// <summary>
        /// 当前执行的 SQL 命令
        /// </summary>
        public IDbCommand DbCommand { get; private set; }

        /// <summary>
        /// 初始化 <see cref="DbCommandException"/> 类的新实例
        /// </summary>
        public DbCommandException()
            : base()
        {
        }

        /// <summary>
        /// 初始化 <see cref="DbCommandException"/> 类的新实例
        /// </summary>
        public DbCommandException(string message, IDbCommand command)
            : base(message) => this.DbCommand = command;

        /// <summary>
        /// 初始化 <see cref="DbCommandException"/> 类的新实例
        /// </summary>
        public DbCommandException(string message, Exception innerException, IDbCommand command)
            : base(message, innerException) => this.DbCommand = command;
    }
}
