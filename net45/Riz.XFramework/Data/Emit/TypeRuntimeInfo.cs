
using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Riz.XFramework.Data
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

        private TableAttribute _attribute = null;
        private MemberAccessorCollection _members = null;
        private MemberAccessorCollection _navMembers = null;
        private MemberAccessorCollection _keyMembers = null;
        private FieldAccessorBase _identityMember = null;

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
        /// 成员反射器集合
        /// </summary>
        public MemberAccessorCollection Members
        {
            get
            {
                if (_members == null) this.Initialize(_type);
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
                if (_navMembers == null) this.Initialize(_type);
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
                if (_keyMembers == null) this.Initialize(_type);
                return _keyMembers;
            }
        }

        /// <summary>
        /// 自增属性成员
        /// </summary>
        public FieldAccessorBase Identity
        {
            get
            {
                if (_identityMember == null) this.Initialize(_type);
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

            if (result != null && (result is MethodAccessor) && ((MethodAccessor)result).Overrides != null && ((MethodAccessor)result).Overrides.Count > 0)
            {
                throw new XFrameworkException("{0} have multi overrides,please try {TypeRuntimeInfo.GetMethod}.", memberName);
            }

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
            var result = this.Members.FirstOrDefault(a => a.Member.Name == memberName);
            if (result == null || ((MethodAccessor)result).Overrides == null) return result;

            var accessor = result as MethodAccessor;
            if (parameters == null) parameters = Type.EmptyTypes;

            // 先看当前的方法是否匹配
            ParameterInfo[] types = accessor.Member.GetParameters();
            if (parameters.Length == types.Length)
            {
                bool isMatch = true;
                for (var index = 0; index < types.Length; index++)
                {
                    if (types[index].ParameterType != parameters[index]) isMatch = false;
                }
                if (isMatch) return accessor;
            }

            // 再看它的重载是否匹配
            for (int m = 0; m < accessor.Overrides.Count; m++)
            {
                accessor = accessor.Overrides[m];

                types = accessor.Member.GetParameters();
                if (parameters.Length == types.Length)
                {
                    bool isMatch = true;
                    for (var index = 0; index < types.Length; index++)
                    {
                        if (types[index].ParameterType != parameters[index]) isMatch = false;
                    }
                    if (isMatch) return accessor;
                }
            }

            return null;
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
        /// 获取成员包装器的外键特性
        /// </summary>
        /// <param name="memberName">实体成员名称（字段/属性）</param>
        /// <returns></returns>
        public ForeignKeyAttribute GetForeignKeyAttribute(string memberName)
        {
            // 找出指定名称的成员
            MemberAccessorBase item = null;
            this.Members.TryGetValue(memberName, out item);
            if (item == null) throw new XFrameworkException("Member {0}.{1} not found.", _type.Name, memberName);

            // 成员必须是字段/属性成员
            var m = item as FieldAccessorBase;
            var attribute = m.GetCustomAttribute<ForeignKeyAttribute>();
            if (attribute == null )
            {
                // 如果是属性，要求标记为 virtual
                var p = m as PropertyAccessor;
                if (p != null && p.Member.GetGetMethod(true) != null && !p.Member.GetGetMethod().IsVirtual) return null;

                // 区分一对一和一对多导航属性
                var navTypeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo(m.MemberCLRType.IsGenericType ? m.MemberCLRType.GetGenericArguments()[0] : m.MemberCLRType);
                if (TypeUtils.IsCollectionType(m.MemberCLRType))
                {
                    // 1:n 关系，外键实体必须持有当前实体的所有主键属性

                    int index = 0;
                    string[] innerKeys = null;
                    string[] outerKeys = null;
                    foreach (FieldAccessorBase inner in this.KeyMembers)
                    {
                        if (!navTypeRuntime.Members.Contains(inner.Name))
                        {
                            innerKeys = null;
                            outerKeys = null;
                            break;
                        }
                        else
                        {
                            var outer = navTypeRuntime.GetMember(inner.Name) as FieldAccessorBase;
                            if (outer == null)
                            {
                                innerKeys = null;
                                outerKeys = null;
                                break;
                            }
                            else
                            {
                                if (innerKeys == null) innerKeys = new string[this.KeyMembers.Count];
                                if (outerKeys == null) outerKeys = new string[this.KeyMembers.Count];
                                innerKeys[index] = inner.Column != null && !string.IsNullOrEmpty(inner.Column.Name) ? inner.Column.Name : inner.Name;
                                outerKeys[index] = inner.Column != null && !string.IsNullOrEmpty(inner.Column.Name) ? inner.Column.Name : inner.Name;
                            }
                        }

                        index += 1;
                    }

                    if (innerKeys != null) attribute = new ForeignKeyAttribute(innerKeys, outerKeys);
                }
                else
                {
                    // 1:1 关系，分两种情况
                    // 1. 当前实体持有外键实体的主键属性
                    // 2. 外键实体持有当前实体的主键属性

                    if (navTypeRuntime.KeyMembers.Count > 0)
                    {
                        int index = 0;
                        string[] innerKeys = null;
                        string[] outerKeys = null;
                        foreach (FieldAccessorBase outer in navTypeRuntime.KeyMembers)
                        {
                            if (!this.Members.Contains(outer.Name))
                            {
                                innerKeys = null;
                                outerKeys = null;
                                break;
                            }
                            else
                            {
                                var inner = this.GetMember(outer.Name) as FieldAccessorBase;
                                if (inner == null)
                                {
                                    innerKeys = null;
                                    outerKeys = null;
                                    break;
                                }
                                else
                                {
                                    if (innerKeys == null) innerKeys = new string[navTypeRuntime.KeyMembers.Count];
                                    if (outerKeys == null) outerKeys = new string[navTypeRuntime.KeyMembers.Count];
                                    innerKeys[index] = inner.Column != null && !string.IsNullOrEmpty(inner.Column.Name) ? inner.Column.Name : inner.Name;
                                    outerKeys[index] = outer.Column != null && !string.IsNullOrEmpty(outer.Column.Name) ? outer.Column.Name : outer.Name;
                                }
                            }

                            index += 1;
                        }

                        if (innerKeys != null) attribute = new ForeignKeyAttribute(innerKeys, outerKeys);
                    }

                    if (attribute == null)
                    {
                        int index = 0;
                        string[] innerKeys = null;
                        string[] outerKeys = null;
                        foreach (FieldAccessorBase inner in this.KeyMembers)
                        {
                            if (!navTypeRuntime.Members.Contains(inner.Name))
                            {
                                innerKeys = null;
                                outerKeys = null;
                                break;
                            }
                            else
                            {
                                var outer = navTypeRuntime.GetMember(inner.Name) as FieldAccessorBase;
                                if (outer == null)
                                {
                                    innerKeys = null;
                                    outerKeys = null;
                                    break;
                                }
                                else
                                {
                                    if (innerKeys == null) innerKeys = new string[this.KeyMembers.Count];
                                    if (outerKeys == null) outerKeys = new string[this.KeyMembers.Count];
                                    innerKeys[index] = inner.Column != null && !string.IsNullOrEmpty(inner.Column.Name) ? inner.Column.Name : inner.Name;
                                    outerKeys[index] = inner.Column != null && !string.IsNullOrEmpty(inner.Column.Name) ? inner.Column.Name : inner.Name;
                                }
                            }

                            index += 1;
                        }

                        if (innerKeys != null) attribute = new ForeignKeyAttribute(innerKeys, outerKeys);
                    }
                }
            }

            return attribute;
        }

        /// <summary>
        /// 获取成员对应的数据库字段名称
        /// </summary>
        /// <param name="memberName">实体成员名称（字段/属性）</param>
        /// <returns></returns>
        public string GetFieldName(string memberName)
        {
            ColumnAttribute column = this.GetMemberAttribute<ColumnAttribute>(memberName);
            return column != null && !string.IsNullOrEmpty(column.Name) ? column.Name : memberName;
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
            // Fix issue#多线程下导致 FieldCount 不正确
            // 单个实例只初始化一次

            //Fixed issue#匿名类的属性不可写
            //匿名类：new{ClientId=a.ClientId}

            if (!_isInitialize)
            {
                lock (_lock)
                {
                    if (!_isInitialize)
                    {
                        var members = new MemberAccessorCollection();
                        var navMembers = new MemberAccessorCollection();
                        var keyMembers = new MemberAccessorCollection();
                        FieldAccessorBase identityMember = null;
                        var sources = this.GetTypeMembers(type, _includePrivates).Select(a => MemberAccessorBase.Create(a));

                        foreach (var m in sources)
                        {
                            if (m.GetCustomAttribute<System.Runtime.CompilerServices.CompilerGeneratedAttribute>() != null) continue;

                            // 添加成员
                            members.Add(m);

                            var field = m as FieldAccessorBase;
                            if (field != null)
                            {
                                if (field.IsKey) keyMembers.Add(field);
                                if (field.IsIdentity) identityMember = field;
                                if (field.IsNavigation) navMembers.Add(field);
                            }
                        }

                        _members = members;
                        _navMembers = navMembers;
                        _keyMembers = keyMembers;
                        _identityMember = identityMember;
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
            if (_ctors == null) _ctors = _type.GetConstructors();
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
    }
}
