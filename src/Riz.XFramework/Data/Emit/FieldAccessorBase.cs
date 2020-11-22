using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Riz.XFramework.Data
{
    /// <summary>
    /// 字段成员访问器基类（适用于字段/属性成员）
    /// </summary>
    public abstract class FieldAccessorBase : MemberAccessorBase
    {
        private Type _fieldType = null;
        private ColumnAttribute _column = null;
        private ForeignKeyAttribute _foreignKey = null;

        /// <summary>
        /// 列特性
        /// </summary>
        public virtual ColumnAttribute Column
        {
            get
            {
                if (_column == null) _column = this.GetCustomAttribute<ColumnAttribute>();
                return _column;
            }
        }

        /// <summary>
        /// 外键列特性
        /// </summary>
        public virtual ForeignKeyAttribute ForeignKey
        {
            get
            {
                if (_foreignKey == null) _foreignKey = this.GetCustomAttribute<ForeignKeyAttribute>();
                return _foreignKey;
            }
        }

        /// <summary>
        /// 实体定义的字段对应 CLR 类型
        /// </summary>
        public Type MemberCLRType
        {
            get
            {
                if (_fieldType == null)
                {
                    if (this.MemberType == MemberTypes.Property) _fieldType = ((PropertyInfo)this.Member).PropertyType;
                    else if (this.MemberType == MemberTypes.Field) _fieldType = ((FieldInfo)this.Member).FieldType;
                }

                return _fieldType;
            }
        }

        /// <summary>
        /// 是否是主键成员
        /// </summary>
        public virtual bool IsKey => this.Column != null && this.Column.IsKey;

        /// <summary>
        /// 是否是自增成员
        /// </summary>
        public virtual bool IsIdentity => this.Column != null && this.Column.IsIdentity;

        /// <summary>
        /// 是否是外键成员
        /// </summary>
        public virtual bool IsNavigation
        {
            get { return this.ForeignKey != null || ((this.Column == null || !this.Column.NoMapped) && !TypeUtils.IsPrimitive(this.Member)); }
        }

        /// <summary>
        /// 是否是基元类的数据字段。
        /// 即与数据库字段一一对应的基础字段，不含外键
        /// </summary>
        public virtual bool IsDbField => !this.IsNavigation && (this.Column == null || !this.Column.NoMapped);

        /// <summary>
        /// 初始化 <see cref="FieldAccessorBase"/> 类的新实例
        /// </summary>
        /// <param name="member">成员元数据</param>
        public FieldAccessorBase(MemberInfo member)
            : base(member)
        {
        }
    }
}
