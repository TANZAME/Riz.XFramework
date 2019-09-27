
using System;
using System.Data;
using System.Reflection;
using System.Reflection.Emit;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// <see cref="IDataReader"/> 转实体映射
    /// </summary>
    sealed class OracleTypeDeserializerImpl : TypeDeserializerImpl
    {
        static readonly MethodInfo _getValue = typeof(OracleTimeStampTZ).GetMethod("get_Value");
        static readonly MethodInfo _getTimeZoneOffset = typeof(OracleTimeStampTZ).GetMethod("GetTimeZoneOffset");
        static readonly MethodInfo _getDateTimeOffset = typeof(OracleDataReader).GetMethod("GetDateTimeOffset", new Type[] { typeof(int) });
        static readonly MethodInfo _getOracleTimeStampTZ = typeof(OracleDataReader).GetMethod("GetOracleTimeStampTZ", new Type[] { typeof(int) });
        static readonly ConstructorInfo _ctorDateTimeOffset = typeof(DateTimeOffset).GetConstructor(new[] { typeof(DateTime), typeof(TimeSpan) });

        /// <summary>
        /// 单例实现
        /// </summary>
        public static new OracleTypeDeserializerImpl Instance = new OracleTypeDeserializerImpl();

        /// <summary>
        /// 实例化 <see cref="OracleTypeDeserializerImpl"/> 类的新实例
        /// </summary>
        OracleTypeDeserializerImpl() : base()
        {
        }

        // 获取对应每列的读取方法
        protected override MethodInfo GetReaderMethod(IDataRecord reader, Type myFieldType, Type memberType, ref Type myFieldType2)
        {
            // DateTimeOffset 类型时，DataReaer.GetFieldType = DateTime
            // 需要强制转换为 DateTimeOffset 类型
            bool isTimezone = myFieldType == typeof(DateTime) && (memberType == typeof(DateTimeOffset) || memberType == typeof(DateTimeOffset?));
            if (!isTimezone) return base.GetReaderMethod(reader, myFieldType, memberType, ref myFieldType2);
            else
            {
#if !net40
                myFieldType2 = typeof(DateTimeOffset);
                return _getDateTimeOffset;
#endif
#if net40
                myFieldType2 = typeof(OracleTimeStampTZ);
                return _getOracleTimeStampTZ;
#endif
            }
        }

#if net40

        /// <summary>
        /// 自定义类型转换
        /// </summary>
        /// <param name="reader">数据读取器</param>
        /// <param name="il">IL 指令</param>
        /// <param name="from">源类型</param>
        /// <param name="to">目标类型</param>
        /// <param name="via">拆箱类型</param>
        /// <returns></returns>
        protected override bool ConvertBoxExtendsion(IDataRecord reader, ILGenerator il, Type from, Type to, Type via)
        {
            bool isExecuted = base.ConvertBoxExtendsion(reader, il, from, to, via);
            if (isExecuted) return isExecuted;

            bool isTimezone = from == typeof(OracleTimeStampTZ) && (to == typeof(DateTimeOffset) || to == typeof(DateTimeOffset?));
            if (isTimezone)
            {
                int valueLocal = il.DeclareLocal(typeof(DateTime)).LocalIndex;
                int offsetLocal = il.DeclareLocal(typeof(TimeSpan)).LocalIndex;

                il.EmitCall(OpCodes.Call, _getValue, null);
                il.StoreLocal(valueLocal);

                il.EmitCall(OpCodes.Call, _getTimeZoneOffset, null);
                il.StoreLocal(offsetLocal);

                il.LoadLocal(valueLocal);
                il.LoadLocal(offsetLocal);
                il.Emit(OpCodes.Newobj, _ctorDateTimeOffset);

                return true;
            }

            //return new DateTimeOffset(oracleTimeStampTZ.Value, oracleTimeStampTZ.GetTimeZoneOffset());

            return false;
        }

#endif

    }
}
