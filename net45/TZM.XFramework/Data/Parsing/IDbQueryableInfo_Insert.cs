
using System.Linq.Expressions;
using System.Collections.Generic;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// 提供对数据类型未知的特定数据源进行新增的语义表示
    /// </summary>
    public interface IDbQueryableInfo_Insert
    {
        /// <summary>
        /// 插入的实体
        /// </summary>
        object Entity { get; set; }

        /// <summary>
        /// 当 Entity 不为空时，指定插入的列
        /// </summary>
        IList<Expression> EntityColumns { get; set; }

        /// <summary>
        /// 批量插入信息
        /// </summary>
        BulkInsertInfo Bulk { get; set; }

        /// <summary>
        /// 插入语义的查询部分，表示新增范围
        /// </summary>
        IDbQueryableInfo_Select Query { get; set; }
    }
}
