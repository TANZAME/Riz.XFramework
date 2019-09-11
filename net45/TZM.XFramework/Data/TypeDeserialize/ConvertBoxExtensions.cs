using System;
using System.Data;
using System.Reflection;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// 数据字段类型和实体属性类型不一致时的转换扩展
    /// </summary>
    public class ConvertBoxExtensions
    {
        private IList<ConvertBoxDelegate> _extensions = null;
        static readonly ConstructorInfo _ctorGuid_string = typeof(Guid).GetConstructor(new[] { typeof(string) });
        static readonly ConstructorInfo _ctorGuid_bytes = typeof(Guid).GetConstructor(new[] { typeof(byte[]) });
        //static ConstructorInfo _ctorXmlReader = typeof(XmlTextReader).GetConstructor(new[] { typeof(string), typeof(XmlNodeType), typeof(XmlParserContext) });
        //static ConstructorInfo _ctorSqlXml = typeof(System.Data.SqlTypes.SqlXml).GetConstructor(new[] { typeof(System.Xml.XmlTextReader) });

        /// <summary>
        /// 类型转换委托
        /// </summary>
        /// <param name="il">中间语言指令</param>
        /// <param name="from">源类型</param>
        /// <param name="to">需转换的类型</param>
        /// <param name="via">via</param>
        /// <returns></returns>
        public delegate bool ConvertBoxDelegate(ILGenerator il, Type from, Type to, Type via);

        /// <summary>
        /// 实例化类 <see cref="GetMethodExtensions"/> 类的新实例
        /// </summary>
        internal ConvertBoxExtensions()
        {
            _extensions = new List<ConvertBoxDelegate>();

            // byte[] => guid
            _extensions.Add((il, from, to, via) =>
            {
                bool isGuid = from == typeof(byte[]) && to == typeof(Guid);
                if (!isGuid) return false;
                else
                {
                    // byte[] => guid
                    il.Emit(OpCodes.Castclass, typeof(byte[]));
                    il.Emit(OpCodes.Newobj, _ctorGuid_bytes);
                    return true;
                }
            });

            // string => guid
            _extensions.Add((il, from, to, via) =>
            {
                bool isGuid = from == typeof(string) && to == typeof(Guid);
                if (!isGuid) return false;
                else
                {
                    // string => guid
                    il.Emit(OpCodes.Castclass, typeof(string));
                    il.Emit(OpCodes.Newobj, _ctorGuid_string);
                    return true;
                }
            });

            ////else if (from == typeof(string) && to == typeof(System.Data.SqlTypes.SqlXml))
            ////{
            ////    // string => SqlXml
            ////    il.Emit(OpCodes.Castclass, typeof(string));
            ////    il.Emit(OpCodes.Ldc_I4, 9);
            ////    il.Emit(OpCodes.Ldnull);
            ////    il.Emit(OpCodes.Newobj, _ctorXmlReader);
            ////    il.Emit(OpCodes.Newobj, _ctorSqlXml);
            ////}
        }

        /// <summary>
        /// 添加一个扩展委托
        /// </summary>
        /// <param name="fn"></param>
        public void Add(ConvertBoxDelegate fn)
        {
            _extensions.Add(fn);
        }

        /// <summary>
        /// 执行类型转换委托
        /// </summary>
        /// <param name="il">中间语言指令</param>
        /// <param name="from">源类型</param>
        /// <param name="to">需转换的类型</param>
        /// <param name="via">via</param>
        /// <returns></returns>
        public bool Convert(ILGenerator il, Type from, Type to, Type via)
        {
            foreach (var fn in _extensions)
            {
                var m = fn(il, from, to, via);
                if (m) return m;
            }

            return false;
        }
    }
}
