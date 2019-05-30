
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ICS.XFramework.Data
{
    /// <summary>
    /// <see cref="IDataReader"/> 转实体反序列化器
    /// </summary>
    public partial class TypeDeserializer
    {
        /// <summary>
        /// 异步反序列化实体集合
        /// </summary>
        public async Task<List<T>> DeserializeAsync<T>()
        {
            bool isThisLine = false;
            object prevLine = null;
            List<T> collection = new List<T>();

            TypeDeserializer<T> deserializer = new TypeDeserializer<T>(_reader, _definition);
            while (await (_reader as DbDataReader).ReadAsync())
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
    }
}
