using System;
using System.Linq;
using System.Reflection;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// 成员反射基类定义
    /// <para>
    /// 底层使用 Emit IL 实现
    /// </para>
    /// </summary>
    public abstract class MemberInvokerBase
    {
        private Type _dataType = null;
        private object[] _attributes = null;
        private MemberInfo _member;
        private ColumnAttribute _column = null;
        private ForeignKeyAttribute _foreignKey = null;
        private BindingFlags? _flags = null;

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
        public MemberInfo Member
        {
            get
            {
                return _member;
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
        /// 成员数据类型
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
        /// 指定控制绑定和由反射执行的成员和类型搜索方法的标志。
        /// </summary>
        public BindingFlags BindingFlags
        {
            get
            {
                if (_flags == null)
                {
                    //MemberInvokerBase invoker = null;
                    //if (this.MemberType == MemberTypes.Property)
                    //{
                    //    if (_propertyBindingFlagInvoker == null)
                    //    {
                    //        Type type = this.Member.GetType();
                    //        MemberInfo[] list = type.GetMember("BindingFlags", BindingFlags.Instance | BindingFlags.NonPublic);
                    //        _propertyBindingFlagInvoker = MemberInvokerBase.Create(list[0]);
                    //    }

                    //    invoker = _propertyBindingFlagInvoker;
                    //}
                    //else if (this.MemberType == MemberTypes.Field && _fieldBindingFlagInvoker == null)
                    //{
                    //    Type type = this.Member.GetType();
                    //    MemberInfo[] list = type.GetMember("BindingFlags", BindingFlags.Instance | BindingFlags.NonPublic);

                    //    _fieldBindingFlagInvoker = MemberInvokerBase.Create(list[0]);
                    //    invoker = _fieldBindingFlagInvoker;
                    //}
                    //else if (this.MemberType == MemberTypes.Method && _methodBindingFlagInvoker == null)
                    //{
                    //    Type type = this.Member.GetType();
                    //    MemberInfo[] list = type.GetMember("BindingFlags", BindingFlags.Instance | BindingFlags.NonPublic);

                    //    _methodBindingFlagInvoker = MemberInvokerBase.Create(list[0]);
                    //    invoker = _methodBindingFlagInvoker;
                    //}

                    //if (invoker != null)
                    //{
                    //    var obj = invoker.Invoke(this.Member);
                    //    if (obj != null) _flags = (BindingFlags)obj;
                    //}
                    Type type = this.Member.GetType();
                    MemberInfo[] list = type.GetMember("BindingFlags", BindingFlags.Instance | BindingFlags.NonPublic);
                    if (list != null && list.Length > 0)
                    {
                        var invoker = MemberInvokerBase.Create(list[0]);
                        var obj = invoker.Invoke(this.Member);
                        if (obj != null) _flags = (BindingFlags)obj;
                    }
                }

                return _flags != null ? _flags.Value : BindingFlags.Default;
            }
        }

        /// <summary>
        /// 初始化 <see cref="MemberInvokerBase"/> 类的新实例
        /// </summary>
        /// <param name="member">成员元数据</param>
        public MemberInvokerBase(MemberInfo member)
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
        public static MemberInvokerBase Create(MemberInfo member)
        {
            MemberInvokerBase invoker = null;
            if (member.MemberType == MemberTypes.Property) invoker = new PropertyInvoker((PropertyInfo)member);
            if (member.MemberType == MemberTypes.Field) invoker = new FieldInvoker((FieldInfo)member);
            if (member.MemberType == MemberTypes.Method) invoker = new MethodInvoker((MethodInfo)member);
            if (invoker == null) throw new XFrameworkException("{0}.{1} not supported", member.ReflectedType, member.Name);
            return invoker;
        }
    }
}
