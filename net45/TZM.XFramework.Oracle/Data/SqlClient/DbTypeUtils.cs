
using System;
using System.Data;
using Oracle.ManagedDataAccess.Client;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// 数据类型公用方法
    /// </summary>
    static class DbTypeUtils
    {
        /// <summary>
        /// 设置命令参数对象的 DbType属性
        /// </summary>
        /// <param name="parameter">命令参数对象</param>
        /// <param name="dbType">DbType属性</param>
        public static void PrepareDbType(this OracleParameter parameter, object dbType)
        {
            if (dbType != null)
            {
                if (dbType is DbType) parameter.DbType = (DbType)dbType;
                else if (dbType is OracleDbType) parameter.OracleDbType = (OracleDbType)dbType;
                else DbTypeUtils.ThrowException(dbType);
            }
        }

        /// <summary>
        /// 是否时间类型
        /// </summary>
        public static bool IsTime(object dbType)
        {
            throw new NotSupportedException("Oracle does not support Time DbType.");
        }

        /// <summary>
        /// 是否日期类型
        /// </summary>
        public static bool IsDate(object dbType)
        {
            if (dbType == null) return false;
            else if (dbType is DbType) return ((DbType)dbType) == DbType.Date;
            else if (dbType is OracleDbType) return ((OracleDbType)dbType) == OracleDbType.Date;
            else return DbTypeUtils.ThrowException(dbType);
        }

        /// <summary>
        /// 是否日期+时间类型
        /// </summary>
        public static bool IsDateTime(object dbType)
        {
            if (dbType == null) return false;
            else if (dbType is DbType) return ((DbType)dbType) == DbType.DateTime;
            else if (dbType is OracleDbType) return ((OracleDbType)dbType) == OracleDbType.TimeStamp;
            else return DbTypeUtils.ThrowException(dbType);
        }

        /// <summary>
        /// 是否日期+时间+精度类型
        /// </summary>
        public static bool IsDateTime2(object dbType)
        {
            if (dbType == null) return false;
            else if (dbType is DbType) return ((DbType)dbType) == DbType.DateTime2;
            else if (dbType is OracleDbType) return ((OracleDbType)dbType) == OracleDbType.TimeStamp;
            else return DbTypeUtils.ThrowException(dbType);
        }

        /// <summary>
        /// 是否日期+时间+精度+时区类型
        /// </summary>
        public static bool IsDateTimeOffset(object dbType)
        {
            //if (dbType == null) return false;
            //else if (dbType is DbType) return ((DbType)dbType) == DbType.DateTimeOffset;
            //else if (dbType is OracleDbType) return ((OracleDbType)dbType) == OracleDbType.TimeStampTZ;
            //else return DbTypeUtils.ThrowException(dbType);
            throw new NotSupportedException("Oracle does not support DateTimeOffset DbType.");
        }

        /// <summary>
        /// 检查是否Unicode数据类型
        /// </summary>
        public static bool IsUnicode(object dbType)
        {
            if (dbType == null) return true;
            else if (dbType is DbType) return ((DbType)dbType) == DbType.String || ((DbType)dbType) == DbType.StringFixedLength;
            else if (dbType is OracleDbType) return ((OracleDbType)dbType) == OracleDbType.NVarchar2 || ((OracleDbType)dbType) == OracleDbType.NChar || ((OracleDbType)dbType) == OracleDbType.NClob;
            else return DbTypeUtils.ThrowException(dbType);
        }

        // 抛出异常
        static bool ThrowException(object dbType)
        {
            throw new NotSupportedException(string.Format("{0} is not a {1} or {2} type.", dbType, typeof(DbType).FullName, typeof(OracleDbType).FullName));
        }
    }
}
