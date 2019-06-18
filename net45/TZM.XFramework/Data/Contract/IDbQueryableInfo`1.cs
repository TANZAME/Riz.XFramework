
namespace TZM.XFramework.Data
{
    /// <summary>
    /// 提供对数据类型未知的特定数据源进行增/删/改查的语义表示
    /// </summary>
    public interface IDbQueryableInfo<T> : IDbQueryableInfo
    {
        /// <summary>
        /// 嵌套查询语义
        /// 注意，T 可能不是 参数T 所表示的类型
        /// </summary>
        IDbQueryableInfo<T> SubQueryInfo { get; set; }
    }
}
