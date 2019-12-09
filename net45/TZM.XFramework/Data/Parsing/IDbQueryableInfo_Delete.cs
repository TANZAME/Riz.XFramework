
namespace TZM.XFramework.Data
{
    /// <summary>
    /// 提供对数据类型未知的特定数据源进行删除的语义表示
    /// </summary>
    public interface IDbQueryableInfo_Delete
    {
        /// <summary>
        /// 实体对象
        /// </summary>
        object Entity { get; set; }

        /// <summary>
        /// 删除语义的查询部分，表示删除范围
        /// </summary>
        IDbQueryableInfo_Select Query { get; set; }
    }
}
