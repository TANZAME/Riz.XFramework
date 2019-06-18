using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// <see cref="IDataReader"/> 转实体映射
    /// </summary>
    public partial class TypeDeserializer
    {
        private IDataReader _reader = null;
        private SelectDbCommandDefinition _definition = null;

        /// <summary>
        /// 实体化 <see cref="TypeDeserializer"/> 类的新实例
        /// </summary>
        /// <param name="reader">DataReader</param>
        /// <param name="definition">命令描述对象，用于解析实体的外键</param>
        public TypeDeserializer(IDataReader reader, SelectDbCommandDefinition definition)
        {
            _definition = definition;
            _reader = reader;
        }

        /// <summary>
        /// 反序列化实体集合
        /// <para>
        /// 适用于单一结果集的场景
        /// </para>
        /// </summary>
        public List<T> Deserialize<T>()
        {
            bool isThisLine = false;
            object prevLine = null;
            List<T> collection = new List<T>();

            TypeDeserializer<T> deserializer = new TypeDeserializer<T>(_reader, _definition);
            while (_reader.Read())
            {
                T model = deserializer.Deserialize(prevLine, out isThisLine);
                if (!isThisLine)
                {
                    collection.Add(model);
                    prevLine = model;
                }
            }

            // 返回结果
            return collection;
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
            TypeDeserializer<T> deserializer = null;
            TypeDeserializer<int> deserializer2 = null;
            bool isAutoIncrement = false;
            bool readedName = false;


            while (_reader.Read())
            {
                if (!isAutoIncrement && !readedName)
                {
                    isAutoIncrement = _reader.GetName(0) == Constant.AUTOINCREMENTNAME;
                    readedName = true;
                }

                if (isAutoIncrement)
                {
                    // 输出自增列
                    if (deserializer2 == null) deserializer2 = new TypeDeserializer<int>(_reader, null);
                    if (identitys == null) identitys = new List<int>(1);
                    int model = deserializer2.Deserialize(prevLine, out isThisLine);
                    identitys.Add(model);
                }
                else
                {
                    // 输出指定类型实体
                    if (deserializer == null) deserializer = new TypeDeserializer<T>(_reader, _definition);
                    T model = deserializer.Deserialize(prevLine, out isThisLine);
                    if (!isThisLine)
                    {
                        if (collection == null) collection = new List<T>();
                        collection.Add(model);
                        prevLine = model;
                    }
                }
            }

            // 返回结果
            return collection;
        }
    }
}
