
using System;
using System.Data;
using System.Reflection;
using System.Reflection.Emit;

namespace Riz.XFramework.Data
{
    /// <summary>
    /// <see cref="IDataReader"/> 转实体映射
    /// </summary>
    internal sealed class SQLiteTypeDeserializerImpl : TypeDeserializerImpl
    {
        static readonly MethodInfo _dateTimeOffsetParse = typeof(DateTimeOffset).GetMethod("Parse", new Type[] { typeof(string) });
        static readonly MethodInfo _timeSpanParse = typeof(TimeSpan).GetMethod("Parse", new Type[] { typeof(string) });

        /// <summary>
        /// 单例实现
        /// </summary>
        public static new SQLiteTypeDeserializerImpl Instance = new SQLiteTypeDeserializerImpl();

        /// <summary>
        /// 实例化 <see cref="SQLiteTypeDeserializerImpl"/> 类的新实例
        /// </summary>
        SQLiteTypeDeserializerImpl() : base()
        {
        }

        /// <summary>
        /// 自定义类型转换
        /// </summary>
        /// <param name="il">(MSIL)指令</param>
        /// <param name="from">来源类型</param>
        /// <param name="to">目标类型</param>
        /// <param name="via">来源类型</param>
        /// <returns></returns>
        protected override bool ConvertBoxedStackExtension(ILGenerator il, Type from, Type to, Type via)
        {
            bool isExecuted = base.ConvertBoxedStackExtension(il, from, to, via);
            if (isExecuted) return isExecuted;

            bool isTimezone = from == typeof(string) && (to == typeof(DateTimeOffset) || to == typeof(DateTimeOffset?));
            bool isTimeSpan = from == typeof(string) && (to == typeof(TimeSpan) || to == typeof(TimeSpan?));
            MethodInfo method = null;
            if (isTimezone)
                method = _dateTimeOffsetParse;
            else if (isTimeSpan) method = _timeSpanParse;

            if (method != null)
            {
                il.EmitCall(OpCodes.Call, method, null);
                return true;
            }

            //return new DateTimeOffset(oracleTimeStampTZ.Value, oracleTimeStampTZ.GetTimeZoneOffset());

            return false;
        }

    }
}
