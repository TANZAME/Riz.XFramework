
using System;
using System.Data;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using Oracle.ManagedDataAccess.Client;

namespace TZM.XFramework.Data
{
    class OracleTypeDeserializerExtensions
    {
        static readonly MethodInfo _getDateTimeOffset = typeof(OracleDataReader).GetMethod("GetDateTimeOffset", new Type[] { typeof(int) });
        static readonly MethodInfo _getOracleTimeStampTZ = typeof(OracleDataReader).GetMethod("GetOracleTimeStampTZ", new Type[] { typeof(int) });

        /// <summary>
        /// 应用扩展
        /// </summary>
        public static void UseExtensions()
        {
            // 添加 DateTimeOffset 扩展
            TypeDeserializerExtensions.GetMethodExtensions.Add(delegate (IDataRecord reader, Type columnType, Type memberType, ref Type columnType2)
            {
                bool isOracle = reader != null && reader.GetType() == typeof(OracleDataReader);
                if (!isOracle) return null;

                bool isTimezone = columnType == typeof(DateTime) && memberType == typeof(DateTimeOffset);
                if (!isTimezone) return null;
                else
                {
#if !net40
                    return _getDateTimeOffset;
#endif
#if net40
                   columnType2 = typeof(OracleTimeStampTZ);
                    return _getOracleTimeStampTZ;
#endif
                }
            });
        }
    }
}
