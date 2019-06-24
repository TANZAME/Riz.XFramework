
using System.Data;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// SELECT 语义的SQL命令描述
    /// </summary>
    public interface IDbCommandInfo_Select : IDbCommandInfo
    {
        /// <summary>
        /// 表达式是否包含 一对多 类型的导航属性
        /// </summary>
        bool HaveListNavigation { get; set; }

        /// <summary>
        /// 选择字段范围
        /// </summary>
        /// <remarks>INSERT 表达式可能用这些字段</remarks>
        IDictionary<string, Column> Columns { get; set; }

        /// <summary>
        /// 导航属性描述集合
        /// <para>
        /// 用于实体与 <see cref="IDataRecord"/> 做映射
        /// </para>
        /// </summary>
        NavigationCollection Navigations { get; set; }

        /// <summary>
        /// 导航属性表达式集合
        /// </summary>
        IDictionary<string, MemberExpression> NavMembers { get; }
    }
}
