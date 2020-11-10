using System;

namespace Riz.XFramework.Data
{
    /// <summary>
    /// 对应数据库表的说明特性
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public class TableAttribute : Attribute
    {
        /// <summary>
        /// 映射到数据库的列表
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 是否临时表
        /// </summary>
        public bool IsTemporary  { get; set; }
    }
}
