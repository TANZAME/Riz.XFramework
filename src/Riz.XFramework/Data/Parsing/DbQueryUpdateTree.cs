
namespace Riz.XFramework.Data
{
    /// <summary>
    /// 表示一项以命令目录树形式表示的更新操作
    /// </summary>
    internal class DbQueryUpdateTree : DbQueryTree
    {
        /// <summary>
        /// 实体对象
        /// </summary>
        public object Entity { get; set; }

        /// <summary>
        /// 更新指定字段表达式
        /// </summary>
        public System.Linq.Expressions.Expression Expression { get; set; }

        /// <summary>
        /// 更新语义的查询部分，表示更新范围
        /// </summary>
        public DbQuerySelectTree SelectTree { get; set; }
    }
}
