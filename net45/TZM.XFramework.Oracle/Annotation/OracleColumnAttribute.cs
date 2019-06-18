using System;
using System.Data;

namespace TZM.XFramework.Data
{
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
    }
}
