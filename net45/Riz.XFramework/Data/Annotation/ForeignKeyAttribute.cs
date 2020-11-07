using System;

namespace Riz.XFramework.Data
{
    /// <summary>
    /// 外键标识
    /// <para>
    /// 如果关联键中有常量，则 keys 中必须以 {CONST} 打头，如 [ForeignKey(new[] { "ClientId", "AccountId" }, new[] { "ClientId", "{CONST}2" })]。
    /// 这时组件会解析成 a.ClientId=b.ClientId AND a.AccountId=2
    /// </para>
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
        /// 初始化 <see cref="ForeignKeyAttribute"/> 的实例 => a.key=b.key
        /// </summary>
        /// <param name="key">两表关联使用的键（字段）</param>
        public ForeignKeyAttribute(string key)
            : this(key, key)
        { }

        /// <summary>
        /// 初始化 <see cref="ForeignKeyAttribute"/> 的实例 => a.key0=b.key0 AND a.key1=b.key1 ...
        /// </summary>
        /// <param name="keys">两表关联使用的键（字段）</param>
        public ForeignKeyAttribute(string[] keys)
            : this(keys, keys)
        { }

        /// <summary>
        /// 初始化 <see cref="ForeignKeyAttribute"/> 的实例 => a.innerKey=b.outerKey
        /// </summary>
        /// <param name="innerKey">内表关联键（字段）</param>
        /// <param name="outerKey">外表关联键（字段）</param>
        public ForeignKeyAttribute(string innerKey, string outerKey)
        {
            _innerKeys = new[] { innerKey };
            _outerKeys = new[] { outerKey };
        }

        /// <summary>
        /// 初始化 <see cref="ForeignKeyAttribute"/> 的实例 => a.innerKey0=b.outerKey0 AND a.innerKey1=b.outerKey1 ...
        /// <para>
        /// 如果关联键中有常量，则 keys 中必须以 {CONST} 打头，如 [ForeignKey(new[] { "ClientId", "AccountId" }, new[] { "ClientId", "{CONST}2" })]。
        /// 这时组件会解析成 a.ClientId=b.ClientId AND a.AccountId=2
        /// </para>
        /// </summary>
        /// <param name="innerKeys">内表关联键（字段）</param>
        /// <param name="outerKeys">外表关联键（字段）</param>
        public ForeignKeyAttribute(string[] innerKeys, string[] outerKeys)
        {
            _innerKeys = innerKeys;
            _outerKeys = outerKeys;
        }
    }
}
