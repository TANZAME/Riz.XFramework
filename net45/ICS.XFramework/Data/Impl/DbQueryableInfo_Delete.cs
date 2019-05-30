namespace ICS.XFramework.Data
{
    /// <summary>
    /// 提供对数据类型未知的特定数据源进行 &lt;删&gt; 操作的语义表示
    /// </summary>
    public class DbQueryableInfo_Delete<T> : DbQueryableInfo<T>, IDbQueryableInfo_Delete
    {
        /// <summary>
        /// 实体对象
        /// </summary>
        public object Entity { get; set; }

        /// <summary>
        /// 删除数据范围，支持 WHERE 和 JOIN 数据源
        /// </summary>
        public DbQueryableInfo_Select<T> SelectInfo { get; set; }
    }
}
