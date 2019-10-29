
using System.Collections.Generic;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// 实体映射描述
    /// </summary>
    public interface IMapping
    {
        /// <summary>
        /// 选择字段范围
        /// </summary>
        /// <remarks>INSERT 表达式可能用这些字段</remarks>
        ColumnCollection PickColumns { get; set; }

        /// <summary>
        /// 导航属性描述集合
        /// <para>
        /// 用于实体与 <see cref="IDataRecord"/> 做映射
        /// </para>
        /// </summary>
        NavigationCollection Navigations { get; set; }

        /// <summary>
        /// 表达式是否包含 一对多 类型的导航属性
        /// </summary>
        bool HasMany { get; set; }
    }
}
