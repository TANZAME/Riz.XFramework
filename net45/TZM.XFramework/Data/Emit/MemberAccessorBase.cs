using System;
using System.Linq;
using System.Reflection;
//using System.Collections.Specialized;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// 成员反射基类定义
    /// <para>
    /// 底层使用 Emit IL 实现
    /// </para>
    /// </summary>
    public abstract class MemberAccessorBase
    {
        private Type _dataType = null;
        private MemberInfo _member = null;
        private object[] _attributes = null;
        private ColumnAttribute _column = null;
        private ForeignKeyAttribute _foreignKey = null;
        private string _memberName = null;
        //private HybridDictionary _attributes = null;

        /// <summary>
        /// 列特性
        /// </summary>
        public ColumnAttribute Column
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
        public ForeignKeyAttribute ForeignKey
        {
            get
            {
                if (_foreignKey == null) _foreignKey = this.GetCustomAttribute<ForeignKeyAttribute>();
                return _foreignKey;
            }
        }

        /// <summary>
        /// 成员
        /// <para>
        /// 可能是方法，属性或者字段
        /// </para>
        /// </summary>
        public MemberInfo Member { get { return _member; } }

        /// <summary>
        /// 成员名称
        /// </summary>
        public string Name
        {
            get
            {
                if (_memberName == null) _memberName = this.Member.Name;
                return _memberName;
            }
            internal set
            {
                _memberName = value;
            }
        }

        /// <summary>
        /// 成员长名称
        /// </summary>
        public string FullName
        {
            get
            {
                return string.Concat(_member.ReflectedType, ".", _member.Name);
            }
        }

        /// <summary>
        /// 成员类型
        /// </summary>
        public MemberTypes MemberType
        {
            get
            {
                return _member.MemberType;
            }
        }

        /// <summary>
        /// 获取此成员对象对应数据库字段的类型
        /// </summary>
        public Type DataType
        {
            get
            {
                if (_dataType == null)
                {
                    if (this.MemberType == MemberTypes.Property) _dataType = ((PropertyInfo)_member).PropertyType;
                    else if (this.MemberType == MemberTypes.Field) _dataType = ((FieldInfo)_member).FieldType;
                }

                return _dataType;
            }
        }

        /// <summary>
        /// 初始化 <see cref="MemberAccessorBase"/> 类的新实例
        /// </summary>
        /// <param name="member">成员元数据</param>
        public MemberAccessorBase(MemberInfo member)
        {
            XFrameworkException.Check.NotNull<MemberInfo>(member, "member");
            _member = member;
        }

        /// <summary>
        /// 动态访问成员
        /// </summary>
        /// <param name="target">拥有该成员的类实例</param>
        /// <param name="parameters">方法参数</param>
        /// <returns></returns>
        public virtual object Invoke(object target, params object[] parameters)
        {
            throw new XFrameworkException("{0}.Invoke not supported.", this.FullName);
        }

        /// <summary>
        /// 获取指定的自定义特性。
        /// </summary>
        /// <typeparam name="TAttribute">自定义特性</typeparam>
        /// <returns></returns>
        public TAttribute GetCustomAttribute<TAttribute>() where TAttribute : Attribute
        {
            if (_attributes == null) _attributes = _member.GetCustomAttributes(false);
            return _attributes.FirstOrDefault(x => x is TAttribute) as TAttribute;
        }

        /// <summary>
        /// 返回表示当前对象的字符串
        /// </summary>
        public override string ToString()
        {
            return this.FullName;
        }

        /// <summary>
        /// 创建成员反射器
        /// </summary>
        /// <param name="member">元数据</param>
        /// <returns></returns>
        public static MemberAccessorBase Create(MemberInfo member)
        {
            MemberAccessorBase m = null;
            if (member.MemberType == MemberTypes.Property) m = new PropertyAccessor((PropertyInfo)member);
            else if (member.MemberType == MemberTypes.Field) m = new FieldAccessor((FieldInfo)member);
            else if (member.MemberType == MemberTypes.Method) m = new MethodAccessor((MethodInfo)member);
            
            if (m == null) throw new XFrameworkException("{0}.{1} not supported", member.ReflectedType, member.Name);
            return m;
        }
    }
}
