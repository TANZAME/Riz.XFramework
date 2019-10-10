
using System;
using System.Data;
using MySql.Data.MySqlClient;

namespace TZM.XFramework.Data.SqlClient
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
        public static void DbType(this MySqlParameter parameter, object dbType)
        {
            if (dbType != null)
            {
                if (dbType is DbType)
                    parameter.DbType = (DbType)dbType;
                else if (dbType is MySqlDbType)
                    parameter.MySqlDbType = (MySqlDbType)dbType;
                else
                    DbTypeUtils.ThrowException(dbType);
            }
        }

        /// <summary>
        /// 是否时间类型
        /// </summary>
        public static bool IsTime(object dbType)
        {
            if (dbType == null)
                return false;
            else if (dbType is DbType)
                return ((DbType)dbType) == System.Data.DbType.Time;
            else if (dbType is MySqlDbType)
                return ((MySqlDbType)dbType) == MySqlDbType.Time;
            else
                return DbTypeUtils.ThrowException(dbType);
        }

        /// <summary>
        /// 是否日期类型
        /// </summary>
        public static bool IsDate(object dbType)
        {
            if (dbType == null)
                return false;
            else if (dbType is DbType)
                return ((DbType)dbType) == System.Data.DbType.Date;
            else if (dbType is MySqlDbType)
                return ((MySqlDbType)dbType) == MySqlDbType.Date;
            else
                return DbTypeUtils.ThrowException(dbType);
        }

        /// <summary>
        /// 是否日期+时间类型
        /// </summary>
        public static bool IsDateTime(object dbType)
        {
            if (dbType == null)
                return false;
            else if (dbType is DbType)
                return ((DbType)dbType) == System.Data.DbType.DateTime;
            else if (dbType is MySqlDbType)
                return ((MySqlDbType)dbType) == MySqlDbType.DateTime;
            else
                return DbTypeUtils.ThrowException(dbType);
        }

        /// <summary>
        /// 是否日期+时间+精度类型
        /// </summary>
        public static bool IsDateTime2(object dbType)
        {
            return DbTypeUtils.IsDateTime(dbType);
        }

        /// <summary>
        /// 是否日期+时间+精度+时区类型
        /// </summary>
        public static bool IsDateTimeOffset(object dbType)
        {
            throw new NotSupportedException("MySQL does not support DateTimeOffset DbType.");
        }

        /// <summary>
        /// 检查是否Unicode数据类型
        /// </summary>
        public static bool IsUnicode(object dbType)
        {
            return false;
        }

        // 抛出异常
        static bool ThrowException(object dbType)
        {
            throw new NotSupportedException(string.Format("{0} is not a {1} or {2} type.", dbType, typeof(DbType).FullName, typeof(MySqlDbType).FullName));
        }
    }
}
