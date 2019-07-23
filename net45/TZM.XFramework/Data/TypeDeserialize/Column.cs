
namespace TZM.XFramework.Data
{
    /// <summary>
    /// 列。
    /// 记录实体属性原始名称以及出现的次数
    /// </summary>
    public class Column
    {
        /// <summary>
        /// 列名
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 出现次数
        /// </summary>
        public int DupCount { get; set; }

        /// <summary>
        /// 表别名
        /// </summary>
        public string TableAlias { get; set; }
    }
}
