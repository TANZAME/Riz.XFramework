
using System.Linq;
using System.Linq.Expressions;
using System.Collections.Generic;

namespace ICS.XFramework.Data
{
    /// <summary>
    /// 提供对数据类型未知的特定数据源进行 &lt;改&gt; 操作的语义表示
    /// </summary>
    public class DbQueryableInfo_Update<T> : DbQueryableInfo<T>,IDbQueryableInfo_Update
    {
        /// <summary>
        /// 实体对象
        /// </summary>
        public object Entity { get; set; }

        /// <summary>
        /// 更新指定字段表达式
        /// </summary>
        public Expression Expression { get; set; }

        /// <summary>
        /// 更新数据范围，支持 WHERE 和 JOIN 数据源
        /// </summary>
        public DbQueryableInfo_Select<T> SelectInfo { get; set; }
    }
}
