
using System;
using System.Data;
using System.Linq;
using System.Text;
using System.Dynamic;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;

namespace Riz.XFramework.Data
{
    /// <summary>
    /// 单个实体反序列化，非泛型版本
    /// </summary>
    internal class TypeDeserializer_Internal
    {
        private IDbContext _context = null;
        private IDataRecord _reader = null;
        private IMapDescriptor _map = null;
        private TypeDeserializerImpl _deserializerImpl = null;
        // 所有反序列化器
        private IDictionary<string, Func<IDataRecord, object>> _deserializers = null;
        // 主表反序列化器
        private Func<IDataRecord, object> _entityDeserializer = null;
        // 一对多导航属性键
        private Dictionary<string, HashSet<string>> _manyNavigationKeys = null;
        // 一对多导航属性数量
        private int? _manyNavigationNumber = null;

        private bool? _isPrimitive = null;
        private bool _isDynamic = false;
        private Type _entityType = null;
        private TypeRuntimeInfo _typeRuntime = null;

        /// <summary>
        /// 实例化<see cref="TypeDeserializer"/> 类的新实例
        /// </summary>
        /// <param name="context">当前查询上下文</param>
        /// <param name="reader">DataReader</param>
        /// <param name="map">SQL 命令描述</param>
        /// <param name="entityType">单个实体类型</param>
        internal TypeDeserializer_Internal(IDbContext context, IDataReader reader, IMapDescriptor map, Type entityType)
        {
            _map = map;
            _reader = reader;
            _context = context;
            _deserializers = new Dictionary<string, Func<IDataRecord, object>>(8);
            _manyNavigationKeys = new Dictionary<string, HashSet<string>>(8);
            _entityType = entityType;
            _isDynamic = _entityType == typeof(ExpandoObject) || _entityType == typeof(object);
            _typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo(entityType);
            _deserializerImpl = ((DbQueryProvider)context.Provider).TypeDeserializerImpl;
        }

        /// <summary>
        /// 将 <see cref="IDataRecord"/> 上的当前行反序列化为实体
        /// </summary>
        /// <param name="prevModel">前一行数据</param>
        /// <param name="isThisLine">是否同一行数据</param>
        /// <returns></returns>
        internal object Deserialize(object prevModel, out bool isThisLine)
        {
            isThisLine = false;

            #region 基元类型

            if (_isPrimitive == null) _isPrimitive = TypeUtils.IsPrimitiveType(_entityType) || _reader.GetName(0) == AppConst.AUTO_INCREMENT_NAME;
            if (_isPrimitive.Value)
            {
                if (_reader.IsDBNull(0)) return TypeUtils.GetNullValue(_entityType); //default(T);

                var obj = _reader.GetValue(0);
                if (obj.GetType() != _entityType)
                {
                    // fix#Nullable<T> issue
                    if (!_entityType.IsGenericType) obj = Convert.ChangeType(obj, _entityType);
                    else
                    {
                        Type type2 = _entityType.GetGenericTypeDefinition();
                        if (type2 != typeof(Nullable<>)) throw new NotSupportedException(string.Format("type {0} not suppored.", type2.FullName));
                        obj = Convert.ChangeType(obj, Nullable.GetUnderlyingType(_entityType));
                    }
                }

                return obj;
            }

            #endregion

            #region 动态类型

            if (_isDynamic)
            {
                ExpandoObject obj = new ExpandoObject();
                var result = ((IDictionary<string, object>)obj);
                for (int i = 0; i < _reader.FieldCount; i++)
                {
                    var value = _reader.GetValue(i);
                    if (value == DBNull.Value) value = null;
                    result.Add(_reader.GetName(i), value);
                }
                return (dynamic)obj;
            }

            #endregion

            #region 实体类型

            object model = null;
            if (_map == null || _map.SelectedNavs == null || _map.SelectedNavs.Count == 0)
            {
                // 没有字段映射说明或者没有导航属性
                if (_entityDeserializer == null) _entityDeserializer = _deserializerImpl.GetTypeDeserializer(_entityType, _reader, _map != null ? _map.SelectedColumns : null, 0);
                model = _entityDeserializer(_reader);
            }
            else
            {
                // 第一层
                if (_entityDeserializer == null) _entityDeserializer = _deserializerImpl.GetTypeDeserializer(_entityType, _reader, _map.SelectedColumns, 0, _map.SelectedNavs.MinIndex);
                model = _entityDeserializer(_reader);
                // 若有 1:n 的导航属性，判断当前行数据与上一行数据是否相同
                if (prevModel != null && _map.HasMany)
                {
                    isThisLine = true;
                    foreach (var m in _typeRuntime.KeyMembers)
                    {
                        var value1 = m.Invoke(prevModel);
                        var value2 = m.Invoke(model);
                        isThisLine = isThisLine && value1.Equals(value2);
                        if (!isThisLine)
                        {
                            // Fix issue#换行时清空上一行的导航键缓存
                            _manyNavigationKeys.Clear();
                            break;
                        }
                    }
                }

                // 递归导航属性
                this.Deserialize_Navigation(isThisLine ? prevModel : null, model, string.Empty, isThisLine);
            }

            return model;

            #endregion
        }

        // 反序列化导航属性
        // @prevLine 前一行数据
        // @isLine   是否同一行数据<同一父级>
        void Deserialize_Navigation(object prevModel, object model, string typeName, bool isThisLine)
        {
            // CRM_SaleOrder.Client 
            // CRM_SaleOrder.Client.AccountList
            Type type = model.GetType();
            TypeRuntimeInfo typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo(type);
            if (string.IsNullOrEmpty(typeName)) typeName = type.Name;

            //foreach (var kvp in _map.Navigations)
            foreach (var navigation in _map.SelectedNavs)
            {
                int start = -1;
                int end = -1;
                if (navigation.FieldCount > 0)
                {
                    start = navigation.StartIndex;
                    end = navigation.StartIndex + navigation.FieldCount;
                }

                string keyName = typeName + "." + navigation.Name;
                if (keyName != navigation.Key) continue;

                var navMember = typeRuntime.GetMember<FieldAccessorBase>(navigation.Name);
                if (navMember == null) continue;

                Type navType = navMember.CLRType;
                Func<IDataRecord, object> deserializer = null;
                TypeRuntimeInfo navTypeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo(navType);
                object navCollection = null;
                if (TypeUtils.IsCollectionType(navType))
                {
                    // 1：n关系，导航属性为 List<T>
                    navCollection = navMember.Invoke(model);
                    if (navCollection == null)
                    {
                        // new 一个列表类型，如果导航属性定义为接口，则默认使用List<T>来实例化
                        TypeRuntimeInfo navTypeRuntime2 = navType.IsInterface
                            ? TypeRuntimeInfoCache.GetRuntimeInfo(typeof(List<>).MakeGenericType(navTypeRuntime.GenericArguments[0]))
                            : navTypeRuntime;
                        navCollection = navTypeRuntime2.Constructor.Invoke();
                        navMember.Invoke(model, navCollection);
                    }
                }

                if (!_deserializers.TryGetValue(keyName, out deserializer))
                {
                    deserializer = _deserializerImpl.GetTypeDeserializer(navType.IsGenericType ? navTypeRuntime.GenericArguments[0] : navType, _reader, _map.SelectedColumns, start, end);
                    _deserializers[keyName] = deserializer;
                }

                // 如果整个导航链中某一个导航属性为空，则跳出递归
                object navModel = deserializer(_reader);
                if (navModel != null)
                {
                    if (navCollection == null)
                    {
                        // 非集合型导航属性
                        navMember.Invoke(model, navModel);
                        //
                        //
                        // 
                    }
                    else
                    {
                        // 集合型导航属性
                        if (prevModel != null && isThisLine)
                        {
                            #region 合并列表

                            // 判断如果属于同一个主表，则合并到上一行的当前明细列表
                            // 例：CRM_SaleOrder.Client.AccountList
                            string[] keys = keyName.Split('.');
                            TypeRuntimeInfo curTypeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo(_entityType);
                            Type curType = curTypeRuntime.Type;
                            MemberAccessorBase curAccessor = null;
                            object curModel = prevModel;

                            for (int i = 1; i < keys.Length; i++)
                            {
                                curAccessor = curTypeRuntime.GetMember(keys[i]);
                                curModel = curAccessor.Invoke(curModel);
                                if (curModel == null) continue;

                                curType = curModel.GetType();
                                curTypeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo(curType);

                                // <<<<<<<<<<< 一对多对多关系 >>>>>>>>>>
                                if (curType.IsGenericType && i != keys.Length - 1)
                                {
                                    curTypeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo(curType);
                                    //if (curTypeRuntime.GenericTypeDefinition == typeof(List<>))
                                    if (TypeUtils.IsCollectionType(curType))
                                    {
                                        var member_Count = curTypeRuntime.GetMember("get_Count");
                                        int count = Convert.ToInt32(member_Count.Invoke(curModel));      // List.Count
                                        if (count > 0)
                                        {
                                            var member_Index = curTypeRuntime.GetMember("get_Item");
                                            curModel = member_Index.Invoke(curModel, count - 1);        // List[List.Count-1]
                                            curTypeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo(curModel.GetType());
                                        }
                                        else
                                        {
                                            // user.Roles.RoleFuncs=>Roles 列表有可能为空
                                            curModel = null;
                                            break;
                                        }
                                    }
                                }
                            }


                            if (curModel != null)
                            {
                                // 如果有两个以上的一对多关系的导航属性，那么在加入列表之前就需要剔除重复的实体


                                bool isAny = false;
                                if (_map.SelectedNavs.Count > 1)
                                {
                                    if (_manyNavigationNumber == null) _manyNavigationNumber = _map.SelectedNavs.Count(x => HasMany(x.Member));
                                    if (_manyNavigationNumber != null && _manyNavigationNumber.Value > 1)
                                    {
                                        if (!_manyNavigationKeys.ContainsKey(keyName)) _manyNavigationKeys[keyName] = new HashSet<string>();
                                        curTypeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo(navModel.GetType());
                                        StringBuilder keyBuilder = new StringBuilder(64);

                                        foreach (var m in curTypeRuntime.KeyMembers)
                                        {
                                            var value = m.Invoke(navModel);
                                            keyBuilder.AppendFormat("{0}={1};", m.Name, (value ?? string.Empty).ToString());
                                        }
                                        string hash = keyBuilder.ToString();
                                        if (_manyNavigationKeys[keyName].Contains(hash))
                                        {
                                            isAny = true;
                                        }
                                        else
                                        {
                                            _manyNavigationKeys[keyName].Add(hash);
                                        }
                                    }
                                }

                                if (!isAny)
                                {
                                    // 如果列表中不存在，则添加到上一行的相同导航列表中去
                                    var member_Add = navTypeRuntime.GetMember("Add");
                                    member_Add.Invoke(curModel, navModel);
                                }
                            }

                            #endregion
                        }
                        else
                        {
                            // 此时的 navTypeRuntime 是 List<> 类型的运行时
                            // 先添加 List 列表
                            var member_Add = navTypeRuntime.GetMember("Add");
                            member_Add.Invoke(navCollection, navModel);

                            var curTypeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo(navModel.GetType());
                            StringBuilder keyBuilder = new StringBuilder(64);

                            foreach (var m in curTypeRuntime.KeyMembers)
                            {
                                var value = m.Invoke(navModel);
                                keyBuilder.AppendFormat("{0}={1};", m.Name, (value ?? string.Empty).ToString());
                            }
                            string hash = keyBuilder.ToString();
                            if (!_manyNavigationKeys.ContainsKey(keyName)) _manyNavigationKeys[keyName] = new HashSet<string>();
                            if (!_manyNavigationKeys[keyName].Contains(hash)) _manyNavigationKeys[keyName].Add(hash);
                        }
                    }

                    if (TypeUtils.IsCollectionType(navTypeRuntime.Type)) navTypeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo(navTypeRuntime.GenericArguments[0]);
                    if (navTypeRuntime.NavMembers.Count > 0) Deserialize_Navigation(prevModel, navModel, keyName, isThisLine);
                }
            }
        }

        // 检查当前成员是否是1：N关系的导航属性
        static bool HasMany(MemberInfo member)
        {
            if (member == null) return false;

            // 仅支持属性和字段***
            Type type = null;
            if (member.MemberType == MemberTypes.Property) type = ((PropertyInfo)member).PropertyType;
            else if (member.MemberType == MemberTypes.Field) type = ((FieldInfo)member).FieldType;
            return type == null ? false : TypeUtils.IsCollectionType(type);
        }
    }

}