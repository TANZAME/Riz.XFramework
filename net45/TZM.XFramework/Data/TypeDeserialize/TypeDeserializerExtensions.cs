using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TZM.XFramework.Data
{
    public class TypeDeserializerExtensions
    {
        /// <summary>
        /// 获取 DataReader 方法成员扩展类
        /// </summary>
        public static readonly GetMethodExtensions GetMethodExtensions = new GetMethodExtensions();

        /// <summary>
        /// 数据字段类型和实体属性类型不一致时的转换扩展
        /// </summary>
        public static readonly ConvertBoxExtensions ConvertBoxExtensions = new ConvertBoxExtensions();
    }
}
