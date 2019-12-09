
namespace TZM.XFramework.Data
{
    /// <summary>
    /// 提供对数据类型未知的特定数据源进行 &lt;改&gt; 操作的语义表示
    /// </summary>
    public class DbQueryableInfo_Update : DbQueryableInfo, IDbQueryableInfo_Update
    {
        /// <summary>
        /// 实体对象
        /// </summary>
        public object Entity { get; set; }

        /// <summary>
        /// 更新指定字段表达式
        /// </summary>
        public System.Linq.Expressions.Expression Expression { get; set; }

        /// <summary>
        /// 更新语义的查询部分，表示更新范围
        /// </summary>
        public IDbQueryableInfo_Select Query { get; set; }
    }
}
