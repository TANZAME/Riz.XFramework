
using System;
using System.Data;
using System.Reflection;
using System.Reflection.Emit;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;

namespace Riz.XFramework.Data
{
    /// <summary>
    /// <see cref="IDataReader"/> 转实体映射
    /// </summary>
    sealed class OracleTypeDeserializerImpl : TypeDeserializerImpl
    {
        static readonly MethodInfo _getValue = typeof(OracleTimeStampTZ).GetProperty("Value", BindingFlags.Public | BindingFlags.Instance).GetGetMethod();
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

        /// <summary>
        /// 获取对应每列的读取方法
        /// </summary>
        /// <param name="readerFieldType">dataReader 里的字段类型</param>
        /// <param name="entityFieldType">实体定义的字段类型</param>
        /// <param name="realReaderFieldType">字段类型（引用传递）</param>
        /// <returns></returns>
        protected override MethodInfo GetReaderMethod(Type readerFieldType, Type entityFieldType, ref Type realReaderFieldType)
        {
            // DateTimeOffset 类型时，DataReaer.GetFieldType = DateTime
            // 需要强制转换为 DateTimeOffset 类型
            bool isTimezone = readerFieldType == typeof(DateTime) && (entityFieldType == typeof(DateTimeOffset) || entityFieldType == typeof(DateTimeOffset?));
            if (!isTimezone) return base.GetReaderMethod(readerFieldType, entityFieldType, ref realReaderFieldType);
            else
            {
#if !net40
                realReaderFieldType = typeof(DateTimeOffset);
                return _getDateTimeOffset;
#endif
#if net40
                realReaderFieldType = typeof(OracleTimeStampTZ);
                return _getOracleTimeStampTZ;
#endif
            }
        }

#if net40

        /// <summary>
        /// 自定义类型转换
        /// </summary>
        /// <param name="il">IL 指令</param>
        /// <param name="from">源类型</param>
        /// <param name="to">目标类型</param>
        /// <param name="via">拆箱类型</param>
        /// <returns></returns>
        protected override bool ConvertBoxedStackExtension(ILGenerator il, Type from, Type to, Type via)
        {
            bool isExecuted = base.ConvertBoxedStackExtension(il, from, to, via);
            if (isExecuted) return isExecuted;

            bool isTimezone = from == typeof(OracleTimeStampTZ) && (to == typeof(DateTimeOffset) || to == typeof(DateTimeOffset?));
            if (isTimezone)
            {
                int localIndex = il.DeclareLocal(typeof(OracleTimeStampTZ)).LocalIndex;
                il.StoreLocal(localIndex);

                // OracleTimeStampTZ.Value
                il.LoadLocalAddress(localIndex);
                il.Emit(OpCodes.Call, _getValue);

                // OracleTimeStampTZ.GetTimeZoneOffset
                il.LoadLocalAddress(localIndex);
                il.Emit(OpCodes.Call, _getTimeZoneOffset);


                il.Emit(OpCodes.Newobj, _ctorDateTimeOffset);

                return true;
            }

            //return new DateTimeOffset(oracleTimeStampTZ.Value, oracleTimeStampTZ.GetTimeZoneOffset());

            return false;
        }

#endif

    }
}
