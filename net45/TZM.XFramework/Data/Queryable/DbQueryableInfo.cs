namespace TZM.XFramework.Data
{
    /// <summary>
    /// 提供对数据类型未知的特定数据源进行增/删/改查的语义表示
    /// </summary>
    public abstract class DbQueryableInfo : IDbQueryableInfo
    {
        /// <summary>
        /// 源查询语句
        /// </summary>
        public IDbQueryable SourceQuery { get; set; }
    }
}
