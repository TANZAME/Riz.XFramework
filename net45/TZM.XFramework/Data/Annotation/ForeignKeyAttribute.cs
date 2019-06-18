using System;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// 外键标识
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public class ForeignKeyAttribute : Attribute
    {
        string[] _innerKeys;
        /// <summary>
        /// 本表关联外键字段
        /// </summary>
        public string[] InnerKeys
        {
            get { if (_innerKeys == null)_innerKeys = new string[0]; return _innerKeys; }
            private set { _innerKeys = value; }
        }

        string[] _outerKeys;
        /// <summary>
        /// 外表关联外键字段
        /// </summary>
        public string[] OuterKeys
        {
            get { if (_outerKeys == null)_outerKeys = new string[0]; return _outerKeys; }
            private set { _outerKeys = value; }
        }

        /// <summary>
        /// 初始化 <see cref="ForeignKeyAttribute"/> 的实例
        /// </summary>
        public ForeignKeyAttribute(string key)
            : this(key, key)
        { }

        /// <summary>
        /// 初始化 <see cref="ForeignKeyAttribute"/> 的实例
        /// </summary>
        public ForeignKeyAttribute(string[] keys)
            : this(keys, keys)
        { }

        /// <summary>
        /// 初始化 <see cref="ForeignKeyAttribute"/> 的实例
        /// </summary>
        public ForeignKeyAttribute(string innerKey, string outerKey)
        {
            _innerKeys = new[] { innerKey };
            _outerKeys = new[] { outerKey };
        }

        /// <summary>
        /// 初始化 <see cref="ForeignKeyAttribute"/> 的实例
        /// </summary>
        public ForeignKeyAttribute(string[] innerKeys, string[] outerKeys)
        {
            _innerKeys = innerKeys;
            _outerKeys = outerKeys;
        }
    }
}
