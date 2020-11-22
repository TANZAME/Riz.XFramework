namespace Riz.XFramework.Data
{
    /// <summary>
    /// 表示一项以命令目录树形式表示的删除操作
    /// </summary>
    public class DbQueryDeleteTree : IDbQueryTree
    {
        /// <summary>
        /// 实体对象
        /// </summary>
        public object Entity { get; set; }

        /// <summary>
        /// 删除语义的查询部分，表示删除范围
        /// </summary>
        public DbQuerySelectTree SelectTree { get; set; }
    }
}
