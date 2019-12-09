
using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// 类型运行时元数据
    /// </summary>
    public class TypeRuntimeInfo
    {
        private Type _type = null;
        private bool _isAnonymousType = false;
        private object[] _attributes;
        private ConstructorInfo[] _ctors = null;
        private ConstructorAccessor _ctor = null;
        private Type[] _genericArguments = null;
        private Type _genericTypeDefinition = null;
        private bool? _isCompilerGenerated = null;
        private bool? _isPrimitiveType = null;
        private bool? _isCollectionType = null;

        private object _lock = new object();
        private bool _isInitialize = false;
        private bool _includePrivates = false;

        private int _dataFieldNumber = 0;
        private TableAttribute _attribute = null;
        private MemberAccessorCollection _members = null;
        private MemberAccessorCollection _navMembers = null;
        private MemberAccessorCollection _keyMembers = null;
        private MemberAccessorBase _identityMember = null;
        private bool _hasIdentityMember = false;

        /// <summary>
        /// 类型对应的数据表
        /// </summary>
        public TableAttribute Table
        {
            get
            {
                if (_attribute == null) _attribute = GetCustomAttribute<TableAttribute>();
                return _attribute;
            }
        }

        /// <summary>
        /// 类型对应的数据表名，如果没有指定Table特性，则使用类型名称做为表名
        /// </summary>
        public string TableName
        {
            get
            {
                return this.Table != null && !string.IsNullOrEmpty(Table.Name) ? Table.Name : this._type.Name;
            }
        }

        /// <summary>
        /// 是否临时表
        /// </summary>
        public bool IsTemporary
        {
            get
            {
                return this.Table != null ? this.Table.IsTemporary : false;
            }
        }

        /// <summary>
        /// 数据字段（即对应数据库字段的属性）数量
        /// </summary>
        public int DataFieldNumber
        {
            get
            {
                var members = this.Members;
                return _dataFieldNumber;
            }
        }

        /// <summary>
        /// 成员反射器集合
        /// </summary>
        public MemberAccessorCollection Members
        {
            get
            {
                if (_members == null) _members = this.Initialize(_type);
                return _members;
            }
        }

        /// <summary>
        /// 导航属性成员
        /// </summary>
        public MemberAccessorCollection NavMembers
        {
            get
            {
                if (_navMembers == null)
                {
                    var navMembers = new MemberAccessorCollection();
                    foreach (var m in this.Members)
                    {
                        if (m.ForeignKey != null) navMembers.Add(m);
                    }

                    _navMembers = navMembers;
                }

                return _navMembers;
            }
        }

        /// <summary>
        /// 主键属性成员
        /// </summary>
        public MemberAccessorCollection KeyMembers
        {
            get
            {
                if (_keyMembers == null)
                {
                    var keyMembers = new MemberAccessorCollection();
                    foreach (var m in this.Members)
                    {
                        if (m.Column != null && m.Column.IsKey) keyMembers.Add(m);
                    }
                    _keyMembers = keyMembers;
                }

                return _keyMembers;
            }
        }

        /// <summary>
        /// 自增属性成员
        /// </summary>
        public MemberAccessorBase Identity
        {
            get
            {
                if (!_hasIdentityMember)
                {
                    _identityMember = this.Members.FirstOrDefault(x => x.Column != null && x.Column.IsIdentity);
                    _hasIdentityMember = true;
                }

                return _identityMember;
            }
        }

        /// <summary>
        /// 类型声明
        /// </summary>
        public Type Type
        {
            get { return _type; }
        }

        /// <summary>
        /// 泛型参数列表
        /// </summary>
        public Type[] GenericArguments
        {
            get
            {
                if (_genericArguments == null && _type.IsGenericType) _genericArguments = _type.GetGenericArguments();
                return _genericArguments;
            }
        }

        /// <summary>
        /// 泛型类型的类型
        /// </summary>
        public Type GenericTypeDefinition
        {
            get
            {
                if (_type.IsGenericType && _genericTypeDefinition == null) _genericTypeDefinition = _type.GetGenericTypeDefinition();
                return _genericTypeDefinition;
            }
        }

        /// <summary>
        /// 是否为匿名类
        /// </summary>
        public bool IsAnonymousType
        {
            get { return _isAnonymousType; }
        }

        /// <summary>
        ///  获取一个值，该值指示当前类型是否是泛型类型。
        /// </summary>
        public bool IsGenericType
        {
            get { return _type.IsGenericType; }
        }

        /// <summary>
        /// 判断当前类型是否编译生成的类型
        /// </summary>
        public bool IsCompilerGenerated
        {
            get
            {
                if (_isCompilerGenerated == null) _isCompilerGenerated = TypeUtils.IsCompilerGenerated(_type);
                return _isCompilerGenerated.Value;
            }
        }

        /// <summary>
        /// 判断当前类型是否是ORM支持的基元类型
        /// </summary>
        public bool IsPrimitiveType
        {
            get
            {
                if (_isPrimitiveType == null) _isPrimitiveType = TypeUtils.IsPrimitiveType(_type);
                return _isPrimitiveType.Value;
            }
        }

        /// <summary>
        /// 判断当前类型是否是ORM支持的集合类型
        /// </summary>
        public bool IsCollectionType
        {
            get
            {
                if (_isCollectionType == null) _isCollectionType = TypeUtils.IsCollectionType(_type);
                return _isCollectionType.Value;
            }
        }

        /// <summary>
        /// 构造函数调用器，返回最少参数的构造函数
        /// </summary>
        public ConstructorAccessor Constructor
        {
            get
            {
                if (_ctor == null)
                {
                    var ctor = this.GetConstructor();
                    _ctor = new ConstructorAccessor(ctor);

                }
                return _ctor;
            }
        }

        /// <summary>
        /// 初始化 <see cref="TypeRuntimeInfo"/> 类的新实例
        /// </summary>
        /// <param name="type">类型声明</param>
        internal TypeRuntimeInfo(Type type)
            : this(type, false)
        {
        }

        /// <summary>
        /// 初始化 <see cref="TypeRuntimeInfo"/> 类的新实例
        /// </summary>
        /// <param name="type">类型声明</param>
        /// <param name="includePrivates">包含私有成员</param>
        internal TypeRuntimeInfo(Type type, bool includePrivates)
        {
            _type = type;
            _isAnonymousType = TypeUtils.IsAnonymousType(_type);
            _includePrivates = includePrivates;
        }

        /// <summary>
        /// 获取成员反射器
        /// </summary>
        /// <param name="memberName">成员名称</param>
        /// <returns></returns>
        public MemberAccessorBase GetMember(string memberName)
        {
            MemberAccessorBase result = null;
            this.Members.TryGetValue(memberName, out result);
            return result;
        }

        /// <summary>
        /// 获取方法反射器，适用于有多个签名的情况
        /// </summary>
        /// <param name="memberName">成员名称</param>
        /// <param name="parameters">方法的参数列表</param>
        /// <returns></returns>
        public MemberAccessorBase GetMethod(string memberName, Type[] parameters)
        {
            return this.Members.FirstOrDefault(x => x.Member.Name == memberName && this.CheckParameters((MethodInfo)x.Member, parameters));
        }

        /// <summary>
        /// 获取成员包装器的自定义特性。
        /// </summary>
        /// <typeparam name="TAttribute">自定义特性</typeparam>
        /// <returns></returns>
        public TAttribute GetMemberAttribute<TAttribute>(string memberName) where TAttribute : Attribute
        {
            MemberAccessorBase m = this.GetMember(memberName);
            return m != null ? m.GetCustomAttribute<TAttribute>() : null;
        }

        /// <summary>
        /// 访问成员
        /// </summary>
        /// <param name="target">实例</param>
        /// <param name="memberName">成员名称</param>
        /// <param name="parameters">参数列表</param>
        /// <returns></returns>
        public object Invoke(object target, string memberName, params object[] parameters)
        {
            MemberAccessorBase m = null;
            this.Members.TryGetValue(memberName, out m);

            if (m == null) throw new XFrameworkException("{0}.{1} Not Found.", _type.Name, memberName);
            return m.Invoke(target, parameters);
        }

        /// <summary>
        /// 获取指定的自定义特性。
        /// </summary>
        /// <typeparam name="TAttribute">自定义特性</typeparam>
        /// <returns></returns>
        public TAttribute GetCustomAttribute<TAttribute>() where TAttribute : Attribute
        {
            if (_attributes == null) _attributes = _type.GetCustomAttributes(false);
            return _attributes.FirstOrDefault(x => x is TAttribute) as TAttribute;
        }

        /// <summary>
        /// 初始化成员集合
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        protected virtual MemberAccessorCollection Initialize(Type type)
        {
            // fix issue#多线程下导致 FieldCount 不正确
            // 单个实例只初始化一次

            //Fixed issue#匿名类的属性不可写
            //匿名类：new{ClientId=a.ClientId}

            if (!_isInitialize)
            {
                lock (_lock)
                {
                    if (!_isInitialize)
                    {
                        var result = new MemberAccessorCollection();
                        var sources = this.GetTypeMembers(type, _includePrivates).Select(x => MemberAccessorBase.Create(x));

                        foreach (var m in sources)
                        {
                            if (m.GetCustomAttribute<System.Runtime.CompilerServices.CompilerGeneratedAttribute>() != null) continue;

                            // 判断当前成员是否重复
                            if (result.Contains(m.Name))
                            {
                                // 属性和字段不允许重复
                                if (m.Member.MemberType != MemberTypes.Method) continue;
                                else
                                {
                                    // 方法成员考虑到有重载的情况，允许重复
                                    int dup = result.Count(x => x.Member.Name == m.Name);
                                    m.Name = string.Format("{0}{1}", m.Name, dup);
                                }
                            }
                            // 添加成员
                            result.Add(m);

                            // 累计数据字段，即与数据库一一对应的字段
                            bool isDataField = !(m.Column != null && m.Column.NoMapped || m.ForeignKey != null || m.Member.MemberType == MemberTypes.Method);
                            if (isDataField) _dataFieldNumber += 1;
                        }


                        _members = result;
                        _isInitialize = true;
                    }
                }
            }


            return _members;
        }

        /// <summary>
        /// 获取当前类型的所有成员
        /// </summary>
        protected virtual IEnumerable<MemberInfo> GetTypeMembers(Type type, bool @private)
        {
            return TypeUtils.GetMembers(type, @private);
        }

        /// <summary>
        /// 获取构造函数
        /// 优先顺序与参数数量成反比
        /// </summary>
        /// <returns></returns>
        protected virtual ConstructorInfo GetConstructor()
        {
            if(_ctors == null) _ctors = _type.GetConstructors();
            if (_isAnonymousType) return _ctors[0];

            for (int i = 0; i < 10; i++)
            {
                ConstructorInfo ctor = _ctors.FirstOrDefault(x => x.GetParameters().Length == i);
                if (ctor != null) return ctor;
            }

            return _ctors[0];
        }

        /// <summary>
        /// 获取构造函数
        /// </summary>
        /// <returns></returns>
        public ConstructorInfo GetConstructor(Type[] types)
        {
            if (_ctors == null) _ctors = _type.GetConstructors();
            if (_isAnonymousType) return _ctors[0];

            XFrameworkException.Check.NotNull(types, "types");
            foreach (var ctor in _ctors)
            {
                var parameters = ctor.GetParameters();
                if (parameters.Length != types.Length) continue;

                for (int i = 0; i < parameters.Length; i++)
                {
                    if (parameters[i].ParameterType != types[i]) break;
                }
            }

            throw new XFrameworkException("not such constructor.");
        }

        private bool CheckParameters(MethodInfo method, Type[] types)
        {
            XFrameworkException.Check.NotNull(types, "types");
            ParameterInfo[] parameters = method.GetParameters();
            if (parameters.Length != types.Length) return false;
            else
            {
                for (int i = 0; i < parameters.Length; i++)
                {
                    if (parameters[i].ParameterType != types[i]) return false;
                }

                return true;
            }
        }

    }
}
