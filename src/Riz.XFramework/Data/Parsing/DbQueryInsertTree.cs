
using System.Linq.Expressions;
using System.Collections.Generic;

namespace Riz.XFramework.Data
{
    /// <summary>
    /// 表示一项以命令目录树形式表示的插入操作
    /// </summary>
    public class DbQueryInsertTree : IDbQueryTree
    {
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

        /// <summary>
        /// 插入语义的查询部分，表示新增范围
        /// </summary>
        public DbQuerySelectTree SelectTree { get; set; }
    }
}
