
namespace ICS.XFramework.Data
{
    /// <summary>
    /// 提供对数据类型未知的特定数据源进行新增的语义表示
    /// </summary>
    public interface IDbQueryableInfo_Insert
    {
        /// <summary>
        /// 插入的实体
        /// </summary>
        object Entity { get; set; }

        /// <summary>
        /// 自增列
        /// </summary>
        MemberInvokerBase AutoIncrement { get; }
    }
}
