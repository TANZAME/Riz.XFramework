
using System.Data;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// 提供对数据类型未知的特定数据源进行增/删/改查的语义表示
    /// </summary>
    public interface IDbQueryableInfo
    {
        /// <summary>
        /// 源查询语句
        /// </summary>
        IDbQueryable SourceQuery { get; set; }
    }
}
