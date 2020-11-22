using System;
using System.Linq;
using System.Reflection;
//using System.Collections.Specialized;

namespace Riz.XFramework.Data
{
    /// <summary>
    /// 底层使用 IL 实现的成员访问器
    /// </summary>
    public abstract class MemberAccessorBase
    {
        private MemberInfo _member = null;
        private object[] _attributes = null;
        private string _memberName = null;

        /// <summary>
        /// 成员元数据（构造函数、方法、属性、字段）
        /// </summary>
        public virtual MemberInfo Member => _member;

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
        public string FullName => string.Concat(_member.ReflectedType, ".", _member.Name);

        /// <summary>
        /// 成员类型
        /// </summary>
        public MemberTypes MemberType => _member.MemberType;

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
        public abstract object Invoke(object target, params object[] parameters);

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
