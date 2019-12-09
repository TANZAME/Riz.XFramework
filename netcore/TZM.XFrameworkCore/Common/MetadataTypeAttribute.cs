using System;
using System.Collections.Generic;
using System.Text;

namespace TZM.XFramework
{
    /// <summary>
    /// 元数据类型
    /// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class MetadataTypeAttribute : Attribute
    {
        private Type _metadataClassType;

        /// <summary>
        /// Gets the metadata class that is associated with a data-model partial class.
        /// </summary>
        public Type MetadataClassType
        {
            get
            {
                if (this._metadataClassType == null) throw new InvalidOperationException("Meta Class type is null");
                return this._metadataClassType;
            }
        }

        /// <summary>
        /// 实例化 <see cref="MetadataTypeAttribute"/> 类的新实例
        /// </summary>
        /// <param name="metadataClassType">需要指定元数据的类型</param>
        public MetadataTypeAttribute(Type metadataClassType)
        {
            this._metadataClassType = metadataClassType;
        }
    }
}
