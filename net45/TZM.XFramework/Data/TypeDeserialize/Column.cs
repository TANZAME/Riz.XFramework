
namespace TZM.XFramework.Data
{
    /// <summary>
    /// 数据库字段与实体字段的列映射
    /// </summary>
    public class Column
    {
        /// <summary>
        /// 列名
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// AS 列名
        /// </summary>
        public string NewName { get; set; }

        /// <summary>
        /// 出现次数
        /// </summary>
        public int DupCount { get; set; }
    }
}
