using System;
using System.Collections.Generic;
using System.Text;

namespace ICS.XFramework
{
    /// <summary>
    /// <summary>Specifies the metadata class to associate with a data model class.
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

        public MetadataTypeAttribute(Type metadataClassType)
        {
            this._metadataClassType = metadataClassType;
        }
    }
}
