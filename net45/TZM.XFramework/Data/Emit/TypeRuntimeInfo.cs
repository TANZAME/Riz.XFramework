
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
        private ConstructorInvoker _ctorInvoker = null;
        private Type[] _genericArguments = null;
        private Type _genericTypeDefinition = null;
        private bool? _lazyIsCompilerGenerated = null;
        private bool? _lazyIsPrimitiveType = null;
        private bool? _lazyIsCollectionType = null;

        private object _lock = new object();
        private bool _isInitialize = false;

        private int _dataFieldCount = 0;
        private TableAttribute _attribute = null;
        private Dictionary<string, MemberInvokerBase> _invokers = null;
        private IDictionary<string, MemberInvokerBase> _navInvokers = null;
        private Dictionary<string, MemberInvokerBase> _keyInvokers = null;

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
        /// 数据字段（即对应数据库字段的属性）的个数
        /// </summary>
        public int DataFieldCount
        {
            get
            {
                var invokers = this.Invokers;
                return _dataFieldCount;
            }
        }

        /// <summary>
        /// 成员反射器集合
        /// </summary>
        public Dictionary<string, MemberInvokerBase> Invokers
        {
            get
            {
                if (_invokers == null) _invokers = this.InitializeInvoker(_type);
                return _invokers;
            }
        }

        /// <summary>
        /// 导航属性成员
        /// </summary>
        public IDictionary<string, MemberInvokerBase> NavInvokers
        {
            get
            {
                if (_navInvokers == null)
                {
                    Dictionary<string, MemberInvokerBase> navInvokers = new Dictionary<string, MemberInvokerBase>();
                    foreach (var kvp in this.Invokers)
                    {
                        MemberInvokerBase invoker = kvp.Value;
                        if (invoker.ForeignKey != null) navInvokers.Add(kvp.Key, invoker);
                    }

                    _navInvokers = navInvokers;
                }

                return _navInvokers;
            }
        }

        /// <summary>
        /// 主键属性成员
        /// </summary>
        public IDictionary<string, MemberInvokerBase> KeyInvokers
        {
            get
            {
                if (_keyInvokers == null)
                {
                    Func<MemberInvokerBase, bool> predicate = x => x != null && x.Column != null && x.Column.IsKey;
                    Dictionary<string, MemberInvokerBase> keyInvokers = new Dictionary<string, MemberInvokerBase>();
                    foreach (var kvp in this.Invokers)
                    {
                        MemberInvokerBase invoker = kvp.Value;
                        if (predicate(invoker)) keyInvokers.Add(kvp.Key, invoker);
                    }
                    _keyInvokers = keyInvokers;
                }

                return _keyInvokers;
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
                if (_lazyIsCompilerGenerated == null) _lazyIsCompilerGenerated = TypeUtils.IsCompilerGenerated(_type);
                return _lazyIsCompilerGenerated.Value;
            }
        }

        /// <summary>
        /// 判断当前类型是否是ORM支持的基元类型
        /// </summary>
        public bool IsPrimitiveType
        {
            get
            {
                if (_lazyIsPrimitiveType == null) _lazyIsPrimitiveType = TypeUtils.IsPrimitiveType(_type);
                return _lazyIsPrimitiveType.Value;
            }
        }

        /// <summary>
        /// 判断当前类型是否是ORM支持的集合类型
        /// </summary>
        public bool IsCollectionType
        {
            get
            {
                if (_lazyIsCollectionType == null) _lazyIsCollectionType = TypeUtils.IsCollectionType(_type);
                return _lazyIsCollectionType.Value;
            }
        }

        /// <summary>
        /// 构造函数调用器
        /// </summary>
        public ConstructorInvoker ConstructInvoker
        {
            get
            {
                if (_ctorInvoker == null)
                {
                    var ctor = this.GetConstructor();
                    _ctorInvoker = new ConstructorInvoker(ctor);

                }
                return _ctorInvoker;
            }
        }

        /// <summary>
        /// 初始化 <see cref="TypeRuntimeInfo"/> 类的新实例
        /// </summary>
        /// <param name="type">类型声明</param>
        internal TypeRuntimeInfo(Type type)
        {
            _type = type;
            _isAnonymousType = TypeUtils.IsAnonymousType(_type);
        }

        /// <summary>
        /// 获取成员反射器
        /// </summary>
        /// <param name="memberName">成员名称</param>
        /// <returns></returns>
        public MemberInvokerBase GetInvoker(string memberName)
        {
            MemberInvokerBase invoker = null;
            this.Invokers.TryGetValue(memberName, out invoker);

            return invoker;
        }

        /// <summary>
        /// 获取成员包装器的自定义特性。
        /// </summary>
        /// <typeparam name="TAttribute">自定义特性</typeparam>
        /// <returns></returns>
        public TAttribute GetInvokerAttribute<TAttribute>(string memberName) where TAttribute : Attribute
        {
            MemberInvokerBase invoker = this.GetInvoker(memberName);
            return invoker != null ? invoker.GetCustomAttribute<TAttribute>() : null;
        }

        /// <summary>
        /// 访问成员
        /// </summary>
        /// <param name="memberName">成员名称</param>
        /// <returns></returns>
        public object Invoke(object target, string memberName, params object[] parameters)
        {
            MemberInvokerBase invoker = null;
            this.Invokers.TryGetValue(memberName, out invoker);

            if (invoker == null) throw new XFrameworkException("{0}.{1} Not Found.", _type.Name, memberName);
            return invoker.Invoke(target, parameters);
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
        /// 初始化成员包装器集合
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        protected virtual Dictionary<string, MemberInvokerBase> InitializeInvoker(Type type)
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
                        Dictionary<string, MemberInvokerBase> invokers = new Dictionary<string, MemberInvokerBase>();
                        var collection = this.GetMembers(type).Select(x => MemberInvokerBase.Create(x));

                        foreach (MemberInvokerBase invoker in collection)
                        {
                            if (invoker.GetCustomAttribute<System.Runtime.CompilerServices.CompilerGeneratedAttribute>() != null) continue;
                            if (!invokers.ContainsKey(invoker.Member.Name)) invokers.Add(invoker.Member.Name, invoker);
                            if (!(invoker.Column != null && invoker.Column.NoMapped || invoker.ForeignKey != null || invoker.Member.MemberType == MemberTypes.Method)) _dataFieldCount += 1;
                        }


                        _invokers = invokers;
                        _isInitialize = true;
                    }
                }
            }


            return _invokers;
        }

        /// <summary>
        /// 获取当前类型的成员
        /// </summary>
        protected virtual IEnumerable<MemberInfo> GetMembers(Type type)
        {
            return TypeUtils.GetMembers(type);
        }

        /// <summary>
        /// 获取构造函数
        /// 优先顺序与参数数量成反比
        /// </summary>
        /// <returns></returns>
        protected virtual ConstructorInfo GetConstructor()
        {
            ConstructorInfo[] ctors = _type.GetConstructors();
            if (_isAnonymousType) return ctors[0];

            for (int i = 0; i < 10; i++)
            {
                ConstructorInfo ctor = ctors.FirstOrDefault(x => x.GetParameters().Length == i);
                if (ctor != null) return ctor;
            }

            return ctors[0];
        }

        /// <summary>
        /// 获取构造函数
        /// 优先顺序与参数数量成反比
        /// </summary>
        /// <returns></returns>
        public ConstructorInfo GetConstructor(Type[] types)
        {
            ConstructorInfo[] ctors = _type.GetConstructors();
            if (_isAnonymousType) return ctors[0];

            if (types != null && types.Length > 0)
            {
                foreach (var ctor in ctors)
                {
                    var parameters = ctor.GetParameters();
                    if (parameters != null && parameters.Length == types.Length)
                    {
                        bool match = true;
                        for (int i = 0; i < parameters.Length; i++) match = parameters[i].ParameterType == types[i];
                        if (match) return ctor;
                    }
                }
            }

            throw new XFrameworkException("not such constructor.");
        }
    }
}
