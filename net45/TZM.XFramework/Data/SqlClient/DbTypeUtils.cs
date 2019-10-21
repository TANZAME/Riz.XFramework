using System;
using System.Data;
using System.Data.SqlClient;

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
        public static void DbType(this SqlParameter parameter, object dbType)
        {
            if (dbType != null)
            {
                if (dbType is DbType)
                    parameter.DbType = (DbType)dbType;
                else if (dbType is SqlDbType)
                    parameter.SqlDbType = (SqlDbType)dbType;
                else
                    DbTypeUtils.ThrowException(dbType);
            }
            else if (parameter.Value != null && parameter.Value is DateTime && dbType == null)
            {
                // 如果 DateTime 没有指定 DbType，则需要转为 DateTime2才能保持原有的精度
                parameter.DbType = System.Data.DbType.DateTime2;
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
            else if (dbType is SqlDbType)
                return ((SqlDbType)dbType) == SqlDbType.Time;
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
            else if (dbType is SqlDbType)
                return ((SqlDbType)dbType) == SqlDbType.Date;
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
            else if (dbType is SqlDbType)
                return ((SqlDbType)dbType) == SqlDbType.DateTime;
            else
                return DbTypeUtils.ThrowException(dbType);
        }

        /// <summary>
        /// 是否日期+时间+精度类型
        /// </summary>
        public static bool IsDateTime2(object dbType)
        {
            if (dbType == null)
                return false;
            else if (dbType is DbType)
                return ((DbType)dbType) == System.Data.DbType.DateTime2;
            else if (dbType is SqlDbType)
                return ((SqlDbType)dbType) == SqlDbType.DateTime2;
            else
                return DbTypeUtils.ThrowException(dbType);
        }

        /// <summary>
        /// 是否日期+时间+精度+时区类型
        /// </summary>
        public static bool IsDateTimeOffset(object dbType)
        {
            if (dbType == null)
                return false;
            else if (dbType is DbType)
                return ((DbType)dbType) == System.Data.DbType.DateTimeOffset;
            else if (dbType is SqlDbType)
                return ((SqlDbType)dbType) == SqlDbType.DateTimeOffset;
            else
                return DbTypeUtils.ThrowException(dbType);
        }

        /// <summary>
        /// 检查是否Unicode数据类型
        /// </summary>
        public static bool IsUnicode(object dbType)
        {
            if (dbType == null)
                return false;
            else if (dbType is DbType)
                return ((DbType)dbType) == System.Data.DbType.String || ((DbType)dbType) == System.Data.DbType.StringFixedLength;
            else if (dbType is SqlDbType)
                return ((SqlDbType)dbType) == SqlDbType.NVarChar || ((SqlDbType)dbType) == SqlDbType.NChar || ((SqlDbType)dbType) == SqlDbType.NText;
            else
                return DbTypeUtils.ThrowException(dbType);
        }

        // 抛出异常
        static bool ThrowException(object dbType)
        {
            throw new NotSupportedException(string.Format("{0} is not a {1} or {2} type.", dbType, typeof(DbType).FullName, typeof(SqlDbType).FullName));
        }
    }
}
