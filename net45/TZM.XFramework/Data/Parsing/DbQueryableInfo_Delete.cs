namespace TZM.XFramework.Data
{
    /// <summary>
    /// 提供对数据类型未知的特定数据源进行 &lt;删&gt; 操作的语义表示
    /// </summary>
    public class DbQueryableInfo_Delete : DbQueryableInfo, IDbQueryableInfo_Delete
    {
        /// <summary>
        /// 实体对象
        /// </summary>
        public object Entity { get; set; }

        /// <summary>
        /// 删除语义的查询部分，表示删除范围
        /// </summary>
        public IDbQueryableInfo_Select Query { get; set; }
    }
}
