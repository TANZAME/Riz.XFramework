
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

namespace Riz.XFramework.Data
{
    /// <summary>
    /// <see cref="IDataReader"/> 转实体映射入口
    /// </summary>
    public sealed class TypeDeserializer
    {
        private IMapInfo _map = null;
        private IDataReader _reader = null;
        private IDbContext _context = null;

        /// <summary>
        /// 实体化 <see cref="TypeDeserializer"/> 类的新实例
        /// </summary>
        /// <param name="context">当前查询上下文</param>
        /// <param name="reader">DataReader</param>
        /// <param name="map">命令描述对象，用于解析实体的外键</param>
        public TypeDeserializer(IDbContext context, IDataReader reader, IMapInfo map)
        {
            _map = map;
            _reader = reader;
            _context = context;
        }


        /// <summary>
        /// 反序列化实体集合
        /// </summary>
        public T Deserialize<T>()
        {
            if (TypeUtils.IsCollectionType(typeof(T)))
                return this.DeserializeCollection<T>();
            else
                return this.DeserializeSingle<T>();
        }

        // 反序列化单个实体
        T DeserializeSingle<T>()
        {
            object prevLine = null;
            bool isThisLine = false;
            T result = default(T);
            int index = 0;
            var deserializer = new TypeDeserializer_Internal(_context, _reader, _map, typeof(T));

            while (_reader.Read())
            {
                object model = deserializer.Deserialize(prevLine, out isThisLine);
                if (!isThisLine)
                {
                    prevLine = model;
                    if (index != 0) break;
                    else
                    {
                        result = (T)model;
                        index += 1;
                    }
                }
            }

            return result;
        }

        // 反序列化实体集合
        T DeserializeCollection<T>()
        {
            object prevLine = null;
            bool isThisLine = false;
            var typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo<T>();
            var modelType = typeRuntime.GenericArguments[0];
            var member = typeRuntime.GetMember("Add");
            var collection = typeRuntime.Constructor.Invoke();
            var deserializer = new TypeDeserializer_Internal(_context, _reader, _map, modelType);

            while (_reader.Read())
            {
                object model = deserializer.Deserialize(prevLine, out isThisLine);
                if (!isThisLine)
                {
                    prevLine = model;
                    member.Invoke(collection, model);
                }
            }

            // 返回结果
            return (T)collection;
        }

        /// <summary>
        /// 反序列化实体集合
        /// <para>
        /// 适用于自增列与结果集混合输出的场景
        /// </para>
        /// </summary>
        public List<T> Deserialize<T>(out List<int> identitys)
        {
            bool isThisLine = false;
            object prevLine = null;
            List<T> collection = null;
            identitys = null;
            TypeDeserializer_Internal deserializer = null;
            TypeDeserializer_Internal deserializer2 = null;
            bool isAutoIncrement = false;
            bool readedName = false;


            while (_reader.Read())
            {
                if (!isAutoIncrement && !readedName)
                {
                    isAutoIncrement = _reader.GetName(0) == AppConst.AUTO_INCREMENT_NAME;
                    readedName = true;
                }

                if (isAutoIncrement)
                {
                    // 输出自增列
                    if (deserializer2 == null) deserializer2 = new TypeDeserializer_Internal(_context, _reader, null, typeof(int));
                    if (identitys == null) identitys = new List<int>(1);
                    object model = deserializer2.Deserialize(prevLine, out isThisLine);
                    identitys.Add((int)model);
                }
                else
                {
                    // 输出指定类型实体
                    if (typeof(T) == typeof(None)) collection = new List<T>(0);
                    else
                    {
                        if (deserializer == null) deserializer = new TypeDeserializer_Internal(_context, _reader, _map, typeof(T));
                        object model = deserializer.Deserialize(prevLine, out isThisLine);
                        if (!isThisLine)
                        {
                            if (collection == null) collection = new List<T>();
                            collection.Add((T)model);
                            prevLine = model;
                        }
                    }
                }
            }

            // 返回结果
            return collection;
        }
    }
}
