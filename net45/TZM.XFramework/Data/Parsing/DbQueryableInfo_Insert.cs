
using System.Linq;
using System.Linq.Expressions;
using System.Collections.Generic;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// 提供对数据类型未知的特定数据源进行 &lt;增&gt; 操作的语义表示
    /// </summary>
    public class DbQueryableInfo_Insert<T> : DbQueryableInfo<T>, IDbQueryableInfo_Insert
    {
        /// <summary>
        /// SELECT 对象
        /// </summary>
        public DbQueryableInfo_Select<T> SelectInfo { get; set; }

        /// <summary>
        /// 实体对象
        /// </summary>
        public object Entity { get; set; }

        /// <summary>
        /// 当 Entity 不为空时，指定插入的列
        /// </summary>
        public IList<Expression> EntityColumns { get; set; }

        /// <summary>
        /// 批量插入信息
        /// </summary>
        public BulkInsertInfo Bulk { get; set; }
    }
}
