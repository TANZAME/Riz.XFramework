
using System;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Data.Common;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Data.SQLite;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// 上下文适配器
    /// </summary>
    internal sealed partial class SQLiteDatabase : Database
    {
        static FieldInfo _disposed = typeof(SQLiteConnection).GetField("disposed", BindingFlags.NonPublic | BindingFlags.Instance);
        static MemberAccessorBase _disposedAccessor = new FieldAccessor(_disposed);

        /// <summary>
        /// 实体转换映射委托生成器
        /// </summary>
        public override TypeDeserializerImpl TypeDeserializerImpl { get { return SQLiteDeserializerImpl.Instance; } }

        /// <summary>
        /// 初始化 <see cref="SQLiteDatabase"/> 类的新实例
        /// </summary>
        /// <param name="provider">查询语义提供者</param>
        /// <param name="connectionString">数据库连接字符串</param>
        public SQLiteDatabase(IDbQueryProvider provider, string connectionString)
            : base(provider, connectionString)
        {
        }

        /// <summary>
        /// 强制释放所有资源
        /// </summary>
        /// <param name="disposing">指示释放资源</param>
        protected override void Dispose(bool disposing)
        {
            // SQLiteConnection 连续调用 Dispose 方法会抛异常
            var connection = base.Connection;
            if (connection == null) base.Dispose(disposing);
            else
            {
                bool disposed = (bool)_disposedAccessor.Invoke(connection);
                base.Dispose(!disposed);
            }
        }
    }
}
