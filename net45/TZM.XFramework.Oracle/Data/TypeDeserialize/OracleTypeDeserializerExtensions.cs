
using System;
using System.Data;
using System.Reflection;
using System.Reflection.Emit;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;

namespace TZM.XFramework.Data
{
    class OracleTypeDeserializerExtensions
    {
        static readonly MethodInfo _getValue = typeof(OracleTimeStampTZ).GetMethod("get_Value");
        static readonly MethodInfo _getTimeZoneOffset = typeof(OracleTimeStampTZ).GetMethod("GetTimeZoneOffset");
        static readonly MethodInfo _getDateTimeOffset = typeof(OracleDataReader).GetMethod("GetDateTimeOffset", new Type[] { typeof(int) });
        static readonly MethodInfo _getOracleTimeStampTZ = typeof(OracleDataReader).GetMethod("GetOracleTimeStampTZ", new Type[] { typeof(int) });
        static readonly ConstructorInfo _ctorDateTimeOffset = typeof(DateTimeOffset).GetConstructor(new[] { typeof(DateTime), typeof(TimeSpan) });

        /// <summary>
        /// 应用扩展
        /// </summary>
        public static void UseExtensions()
        {
            // 添加 DateTimeOffset 扩展
            TypeDeserializerExtensions.GetMethodExtensions.Add(delegate(IDataRecord reader, Type myFieldType, Type memberType, ref Type myFieldType2)
            {
                bool isOracle = reader != null && reader.GetType() == typeof(OracleDataReader);
                if (!isOracle) return null;

                // DateReader 返回的类型是 DateTime，转为DateTimeOffset
                bool isTimezone = myFieldType == typeof(DateTime) && memberType == typeof(DateTimeOffset);
                if (!isTimezone) return null;
                else
                {
#if !net40
                    return _getDateTimeOffset;
#endif
#if net40
                    myFieldType2 = typeof(OracleTimeStampTZ);
                    return _getOracleTimeStampTZ;
#endif
                }
            });

#if net40

            // 添加 DateTimeOffset 扩展
            TypeDeserializerExtensions.ConvertBoxExtensions.Add((reader, il, from, to, via) =>
            {
                bool isOracle = reader != null && reader.GetType() == typeof(OracleDataReader);
                if (!isOracle) return false;

                bool isTimezone = from == typeof(OracleTimeStampTZ) && to == typeof(DateTimeOffset);
                if (isTimezone)
                {
                    int valueLocal = -1;
                    int offsetLocal = -1;
                    valueLocal = il.DeclareLocal(typeof(DateTime)).LocalIndex;
                    offsetLocal = il.DeclareLocal(typeof(TimeSpan)).LocalIndex;
                    
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
            });

#endif
        }
    }
}
