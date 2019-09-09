
using System;
using System.Data;
using System.Linq;
using System.Text;
using System.Dynamic;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;
using Oracle.ManagedDataAccess.Client;

namespace TZM.XFramework.Data
{
    public class TypeDeserializer<T>
    {
        // DateTimeOffset 类型
        static Func<IDataRecord, Type, Type, MemberInfo> _getDateTimeOffset = (reader, columnType, memberType) =>
        {
            if (reader == null || reader.GetType() != typeof(OracleDataReader)) return null;

            // ODP.NET => TIMESTAMP WITH TIME ZONE - DateTime
            if (columnType == typeof(DateTime) && memberType == typeof(DateTimeOffset))
            {

            }

            return null;
        };
    }

}