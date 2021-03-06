﻿
//using System;
//using System.Data;
//using System.Linq;
//using System.Text;
//using System.Dynamic;
//using System.Reflection;
//using System.Reflection.Emit;
//using System.Collections.Generic;

//namespace Riz.XFramework.Data
//{
//    /// <summary>
//    /// 单个实体反序列化，泛型版本
//    /// </summary>
//    [Obsolete]
//    internal class TypeDeserializer<T>
//    {
//        private IDatabase _database = null;
//        private IDataRecord _reader = null;
//        private IMapping _map = null;
//        private TypeDeserializerImpl _deserializerImpl = null;
//        // 所有反序列化器
//        private IDictionary<string, Func<IDataRecord, object>> _deserializers = null;
//        // 主表反序列化器
//        private Func<IDataRecord, object> _modelDeserializer = null;
//        // 一对多导航属性键
//        private Dictionary<string, HashSet<string>> _manyNavigationKeys = null;
//        // 一对多导航属性数量
//        private int? _manyNavigationNumber = null;

//        private bool? _isPrimitive = null;
//        private bool _isDynamic = false;
//        private TypeRuntimeInfo _typeRuntime = null;

//        /// <summary>
//        /// 实例化<see cref="TypeDeserializer"/> 类的新实例
//        /// </summary>
//        /// <param name="database">DataReader</param>
//        /// <param name="reader">DataReader</param>
//        /// <param name="map">SQL 命令描述</param>
//        internal TypeDeserializer(IDatabase database, IDataReader reader, IMapping map)
//        {
//            _map = map;
//            _reader = reader;
//            _database = database;
//            _deserializerImpl = _database.TypeDeserializerImpl;
//            _deserializers = new Dictionary<string, Func<IDataRecord, object>>(8);
//            _manyNavigationKeys = new Dictionary<string, HashSet<string>>(8);
//            _isDynamic = typeof(T) == typeof(ExpandoObject) || typeof(T) == typeof(object);
//            _typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo<T>();
//        }

//        /// <summary>
//        /// 将 <see cref="IDataRecord"/> 上的当前行反序列化为实体
//        /// </summary>
//        /// <param name="prevModel">前一行数据</param>
//        /// <param name="isThisLine">是否同一行数据</param>
//        internal T Deserialize(object prevModel, out bool isThisLine)
//        {
//            isThisLine = false;

//            #region 基元类型

//            if (_isPrimitive == null) _isPrimitive = TypeUtils.IsPrimitiveType(typeof(T)) || _reader.GetName(0) == Constant.AUTO_INCREMENT_NAME;
//            if (_isPrimitive.Value)
//            {
//                if (_reader.IsDBNull(0)) return default(T);

//                var obj = _reader.GetValue(0);
//                if (obj.GetType() != typeof(T))
//                {
//                    // fix#Nullable<T> issue
//                    if (!typeof(T).IsGenericType) obj = Convert.ChangeType(obj, typeof(T));
//                    else
//                    {
//                        Type g = typeof(T).GetGenericTypeDefinition();
//                        if (g != typeof(Nullable<>)) throw new NotSupportedException(string.Format("type {0} not suppored.", g.FullName));
//                        obj = Convert.ChangeType(obj, Nullable.GetUnderlyingType(typeof(T)));
//                    }
//                }

//                return (T)obj;
//            }

//            #endregion

//            #region 动态类型

//            if (_isDynamic)
//            {
//                ExpandoObject obj = new ExpandoObject();
//                var result = ((IDictionary<string, object>)obj);
//                for (int i = 0; i < _reader.FieldCount; i++)
//                {
//                    var value = _reader.GetValue(i);
//                    if (value == DBNull.Value) value = null;
//                    result.Add(_reader.GetName(i), value);
//                }
//                return (dynamic)obj;
//            }

//            #endregion

//            #region 实体类型

//            T model = default(T);
//            if (_map == null || _map.PickNavDescriptors == null || _map.PickNavDescriptors.Count == 0)
//            {
//                // 没有字段映射说明或者没有导航属性
//                if (_modelDeserializer == null) _modelDeserializer = _deserializerImpl.GetTypeDeserializer(typeof(T), _reader, _map != null ? _map.PickColumns : null, 0);
//                model = (T)_modelDeserializer(_reader);
//            }
//            else
//            {
//                // 第一层
//                if (_modelDeserializer == null) _modelDeserializer = _deserializerImpl.GetTypeDeserializer(typeof(T), _reader, _map.PickColumns, 0, _map.PickNavDescriptors.MinIndex);
//                model = (T)_modelDeserializer(_reader);
//                // 若有 1:n 的导航属性，判断当前行数据与上一行数据是否相同
//                if (prevModel != null && _map.HasMany)
//                {
//                    isThisLine = true;
//                    foreach (var m in _typeRuntime.KeyMembers)
//                    {
//                        var value1 = m.Invoke(prevModel);
//                        var value2 = m.Invoke(model);
//                        isThisLine = isThisLine && value1.Equals(value2);
//                        if (!isThisLine)
//                        {
//                            // Fix issue#换行时清空上一行的导航键缓存
//                            _manyNavigationKeys.Clear();
//                            break;
//                        }
//                    }
//                }

//                // 递归导航属性
//                this.Deserialize_Navigation(isThisLine ? prevModel : null, model, string.Empty, isThisLine);
//            }

//            return model;

//            #endregion
//        }

//        // 反序列化导航属性
//        // @prevLine 前一行数据
//        // @isLine   是否同一行数据<同一父级>
//        void Deserialize_Navigation(object prevModel, object model, string typeName, bool isThisLine)
//        {
//            // CRM_SaleOrder.Client 
//            // CRM_SaleOrder.Client.AccountList
//            Type type = model.GetType();
//            TypeRuntimeInfo typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo(type);
//            if (string.IsNullOrEmpty(typeName)) typeName = type.Name;

//            //foreach (var kvp in _map.Navigations)
//            foreach(var navigation in _map.PickNavDescriptors)
//            {
//                int start = -1;
//                int end = -1;
//                if (navigation.FieldCount > 0)
//                {
//                    start = navigation.StartIndex;
//                    end = navigation.StartIndex + navigation.FieldCount;
//                }

//                string keyName = typeName + "." + navigation.Name;
//                if (keyName != navigation.KeyId) continue;

//                var navAccessor = typeRuntime.GetMember(navigation.Name);
//                if (navAccessor == null) continue;

//                Type navType = navAccessor.DataType;
//                Func<IDataRecord, object> deserializer = null;
//                TypeRuntimeInfo navTypeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo(navType);
//                object navCollection = null;
//                //if (navType.IsGenericType && navTypeRuntime.GenericTypeDefinition == typeof(List<>))
//                if (TypeUtils.IsCollectionType(navType))
//                {
//                    // 1：n关系，导航属性为 List<T>
//                    navCollection = navAccessor.Invoke(model);
//                    if (navCollection == null)
//                    {
//                        // new 一个列表类型，如果导航属性定义为接口，则默认使用List<T>来实例化
//                        TypeRuntimeInfo navTypeRuntime2 = navType.IsInterface
//                            ? TypeRuntimeInfoCache.GetRuntimeInfo(typeof(List<>).MakeGenericType(navTypeRuntime.GenericArguments[0]))
//                            : navTypeRuntime;
//                        navCollection = navTypeRuntime2.Constructor.Invoke();
//                        navAccessor.Invoke(model, navCollection);
//                    }
//                }

//                if (!_deserializers.TryGetValue(keyName, out deserializer))
//                {
//                    deserializer = _deserializerImpl.GetTypeDeserializer(navType.IsGenericType ? navTypeRuntime.GenericArguments[0] : navType, _reader, _map.PickColumns, start, end);
//                    _deserializers[keyName] = deserializer;
//                }

//                // 如果整个导航链中某一个导航属性为空，则跳出递归
//                object navModel = deserializer(_reader);
//                if (navModel != null)
//                {
//                    if (navCollection == null)
//                    {
//                        // 非集合型导航属性
//                        navAccessor.Invoke(model, navModel);
//                        //
//                        //
//                        // 
//                    }
//                    else
//                    {
//                        // 集合型导航属性
//                        if (prevModel != null && isThisLine)
//                        {
//                            #region 合并列表

//                            // 判断如果属于同一个主表，则合并到上一行的当前明细列表
//                            // 例：CRM_SaleOrder.Client.AccountList
//                            string[] keys = keyName.Split('.');
//                            TypeRuntimeInfo curTypeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo<T>();
//                            Type curType = curTypeRuntime.Type;
//                            MemberAccessorBase curAccessor = null;
//                            object curModel = prevModel;

//                            for (int i = 1; i < keys.Length; i++)
//                            {
//                                curAccessor = curTypeRuntime.GetMember(keys[i]);
//                                curModel = curAccessor.Invoke(curModel);
//                                if (curModel == null) continue;

//                                curType = curModel.GetType();
//                                curTypeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo(curType);

//                                // <<<<<<<<<<< 一对多对多关系 >>>>>>>>>>
//                                if (curType.IsGenericType && i != keys.Length - 1)
//                                {
//                                    curTypeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo(curType);
//                                    //if (curTypeRuntime.GenericTypeDefinition == typeof(List<>))
//                                    if (TypeUtils.IsCollectionType(curType))
//                                    {
//                                        var m = curTypeRuntime.GetMember("get_Count");
//                                        int count = Convert.ToInt32(m.Invoke(curModel));      // List.Count
//                                        if (count > 0)
//                                        {
//                                            var m2 = curTypeRuntime.GetMember("get_Item");
//                                            curModel = m2.Invoke(curModel, count - 1);        // List[List.Count-1]
//                                            curTypeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo(curModel.GetType());
//                                        }
//                                        else
//                                        {
//                                            // user.Roles.RoleFuncs=>Roles 列表有可能为空
//                                            curModel = null;
//                                            break;
//                                        }
//                                    }
//                                }
//                            }


//                            if (curModel != null)
//                            {
//                                // 如果有两个以上的一对多关系的导航属性，那么在加入列表之前就需要剔除重复的实体


//                                bool isAny = false;
//                                if (_map.PickNavDescriptors.Count > 1)
//                                {
//                                    if (_manyNavigationNumber == null) _manyNavigationNumber = _map.PickNavDescriptors.Count(x => IsManyNavigation(x.Member));
//                                    if (_manyNavigationNumber != null && _manyNavigationNumber.Value > 1)
//                                    {
//                                        if (!_manyNavigationKeys.ContainsKey(keyName)) _manyNavigationKeys[keyName] = new HashSet<string>();
//                                        curTypeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo(navModel.GetType());
//                                        StringBuilder keyBuilder = new StringBuilder(64);

//                                        foreach (var m in curTypeRuntime.KeyMembers)
//                                        {
//                                            var value = m.Invoke(navModel);
//                                            keyBuilder.AppendFormat("{0}={1};", m.Name, (value ?? string.Empty).ToString());
//                                        }
//                                        string hash = keyBuilder.ToString();
//                                        if (_manyNavigationKeys[keyName].Contains(hash))
//                                        {
//                                            isAny = true;
//                                        }
//                                        else
//                                        {
//                                            _manyNavigationKeys[keyName].Add(hash);
//                                        }
//                                    }
//                                }

//                                if (!isAny)
//                                {
//                                    // 如果列表中不存在，则添加到上一行的相同导航列表中去
//                                    var myAddMethod = navTypeRuntime.GetMember("Add");
//                                    myAddMethod.Invoke(curModel, navModel);
//                                }
//                            }

//                            #endregion
//                        }
//                        else
//                        {
//                            // 此时的 navTypeRuntime 是 List<> 类型的运行时
//                            // 先添加 List 列表
//                            var myAddMethod = navTypeRuntime.GetMember("Add");
//                            myAddMethod.Invoke(navCollection, navModel);

//                            var curTypeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo(navModel.GetType());
//                            StringBuilder keyBuilder = new StringBuilder(64);

//                            foreach (var m in curTypeRuntime.KeyMembers)
//                            {
//                                var value = m.Invoke(navModel);
//                                keyBuilder.AppendFormat("{0}={1};", m.Name, (value ?? string.Empty).ToString());
//                            }
//                            string hash = keyBuilder.ToString();
//                            if (!_manyNavigationKeys.ContainsKey(keyName)) _manyNavigationKeys[keyName] = new HashSet<string>();
//                            if (!_manyNavigationKeys[keyName].Contains(hash)) _manyNavigationKeys[keyName].Add(hash);
//                        }
//                    }

//                    //if (navTypeRuntime.GenericTypeDefinition == typeof(List<>)) navTypeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo(navTypeRuntime.GenericArguments[0]);
//                    if (TypeUtils.IsCollectionType(navTypeRuntime.Type)) navTypeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo(navTypeRuntime.GenericArguments[0]);
//                    if (navTypeRuntime.NavMembers.Count > 0) Deserialize_Navigation(prevModel, navModel, keyName, isThisLine);
//                }
//            }
//        }

//        // 检查当前成员是否是1：N关系的导航属性
//        static bool IsManyNavigation(MemberInfo member)
//        {
//            if (member == null) return false;

//            // 仅支持属性和字段***
//            Type type = null;
//            if (member.MemberType == MemberTypes.Property) type = ((PropertyInfo)member).PropertyType;
//            else if (member.MemberType == MemberTypes.Field) type = ((FieldInfo)member).FieldType;
//            return type == null ? false : TypeUtils.IsCollectionType(type);
//        }
//    }

//}