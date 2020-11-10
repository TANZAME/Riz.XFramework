
using System;

namespace Riz.XFramework.Data
{
    /// <summary>
    /// ORACLE 数据库列标识
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class OracleColumnAttribute : ColumnAttribute
    {
        private bool _isIdentity = false;

        /// <summary>
        /// SEQ 类型的名称
        /// </summary>
        public string SEQName { get; set; }

        /// <summary>
        /// 是否自增列
        /// <para>
        /// SEQName 不为空表示自增列
        /// </para>
        /// </summary>
        public override bool IsIdentity
        {
            get { return !string.IsNullOrEmpty(SEQName); }
            set { _isIdentity = value; }
        }

        ///// <summary>
        ///// 字段名强制使用双引号 "
        ///// </summary>
        //public bool ForceQuote { get; set; }
    }
}
