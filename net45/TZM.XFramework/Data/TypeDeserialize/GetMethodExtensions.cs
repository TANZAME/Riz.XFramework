using System;
using System.Data;
using System.Reflection;
using System.Collections.Generic;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// 获取 DataReader 方法成员扩展类
    /// </summary>
    public class GetMethodExtensions
    {
        private IList<GetMethodDelegate> _extensions = null;

        /// <summary>
        /// 获取运行行方法委托
        /// </summary>
        /// <param name="reader">DataReader</param>
        /// <param name="myFieldType">数据字段类型</param>
        /// <param name="memberType">实体属性类型</param>
        /// <param name="myFieldType2">数据字段类型</param>
        /// <returns></returns>
        public delegate MethodInfo GetMethodDelegate(IDataRecord reader, Type myFieldType, Type memberType, ref Type myFieldType2);

        /// <summary>
        /// 实例化类 <see cref="GetMethodExtensions"/> 类的新实例
        /// </summary>
        internal GetMethodExtensions()
        {
            _extensions = new List<GetMethodDelegate>();
        }

        /// <summary>
        /// 添加一个扩展委托
        /// </summary>
        /// <param name="fn"></param>
        public void Add(GetMethodDelegate fn)
        {
            _extensions.Add(fn);
        }

        /// <summary>
        /// 执行获取运行行方法委托
        /// <para>
        /// 返回第一个不为空的运行时方法
        /// </para>
        /// </summary>
        /// <param name="reader">DataReader</param>
        /// <param name="myFieldType">数据字段类型</param>
        /// <param name="memberType">实体属性类型</param>
        /// <param name="myFieldType2">数据字段类型</param>
        /// <returns></returns>
        public MethodInfo TryGetMethod(IDataRecord reader, Type myFieldType, Type memberType, ref Type myFieldType2)
        {
            foreach (var fn in _extensions)
            {
                var m = fn(reader, myFieldType, memberType, ref myFieldType2);
                if (m != null) return m;
            }

            return null;
        }
    }
}
