
using System;
using System.Data;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// 上下文适配器
    /// </summary>
    internal sealed partial class NpgDatabase : Database
    {
        /// <summary>
        /// 实体转换映射委托生成器
        /// </summary>
        protected override TypeDeserializerImpl TypeDeserializerImpl { get { return NpgTypeDeserializerImpl.Instance; } }

        /// <summary>
        /// 初始化 <see cref="NpgDatabase"/> 类的新实例
        /// </summary>
        /// <param name="provider">查询语义提供者</param>
        /// <param name="connectionString">数据库连接字符串</param>
        public NpgDatabase(IDbQueryProvider provider, string connectionString)
            : base(provider, connectionString)
        {
        }
    }
}
