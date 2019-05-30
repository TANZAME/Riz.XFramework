namespace ICS.XFramework.Data
{
    /// <summary>
    /// 提供对数据类型未知的特定数据源进行增/删/改查的语义表示
    /// </summary>
    public abstract class DbQueryableInfo<T> : DbQueryableInfo, IDbQueryableInfo<T>
    {
        /// <summary>
        /// 子查询语义
        /// 注意，T 可能不是 参数T 所表示的类型
        /// </summary>
        public virtual IDbQueryableInfo<T> SubQueryInfo { get; set; }
    }
}
