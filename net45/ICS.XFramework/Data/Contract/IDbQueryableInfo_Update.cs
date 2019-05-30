
using System.Linq.Expressions;
namespace ICS.XFramework.Data
{
    /// <summary>
    /// 提供对数据类型未知的特定数据源进行更新的语义表示
    /// </summary>
    public interface IDbQueryableInfo_Update
    {
        /// <summary>
        /// 实体对象
        /// </summary>
        object Entity { get; set; }

        /// <summary>
        /// 更新指定字段表达式
        /// </summary>
        Expression Expression { get; set; }
    }
}
