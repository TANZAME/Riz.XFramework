
using System;
using System.Data;
using NpgsqlTypes;

namespace ICS.XFramework.Data
{
    /// <summary>
    /// 参数类型封装
    /// </summary>
    internal class NpgDbTypeInfo : DbTypeInfo
    {
        private NpgsqlDbType? _sqlDbType = null;

        /// <summary>
        /// 其它数据库组件的DbType
        /// </summary>
        public new NpgsqlDbType? SqlDbType
        {
            get { return _sqlDbType; }
            set
            {
                _sqlDbType = value;
                base.SqlDbType = value;
            }
        }

        /// <summary>
        /// 是否时间类型
        /// </summary>
        public override bool IsTime
        {
            get
            {
#if netcore
                return this.DbType != null && this.DbType.Value == System.Data.DbType.Time ||
                    this.SqlDbType != null && this.SqlDbType.Value == NpgsqlDbType.Time;
#endif
#if !netcore
                throw new NotSupportedException("Npgsql does not support Time type.");
#endif
            }
        }

        /// <summary>
        /// 是否日期类型
        /// </summary>
        public override bool IsDate
        {
            get
            {
                return this.DbType != null && this.DbType.Value == System.Data.DbType.Date ||
                    this.SqlDbType != null && this.SqlDbType.Value == NpgsqlDbType.Date;
            }
        }

        /// <summary>
        /// 是否日期+时间类型
        /// </summary>
        public override bool IsDateTime
        {
            get
            {
                return this.DbType != null && this.DbType.Value == System.Data.DbType.DateTime ||
                    this.SqlDbType != null && this.SqlDbType.Value == NpgsqlDbType.Timestamp;
            }
        }

        /// <summary>
        /// 是否日期+时间+精度类型
        /// </summary>
        public override bool IsDateTime2
        {
            get
            {
                return this.DbType != null && this.DbType.Value == System.Data.DbType.DateTime2 ||
                    this.SqlDbType != null && this.SqlDbType.Value == NpgsqlDbType.Timestamp;
            }
        }

        /// <summary>
        /// 是否日期+时间+精度+时区类型
        /// </summary>
        public override bool IsDateTimeOffset
        {
            get
            {
#if netcore
                return this.DbType != null && this.DbType.Value == System.Data.DbType.DateTimeOffset ||
                    this.SqlDbType != null && this.SqlDbType.Value == NpgsqlDbType.TimestampTz;
#endif
#if !netcore
                return this.DbType != null && this.DbType.Value == System.Data.DbType.DateTimeOffset ||
                    this.SqlDbType != null && this.SqlDbType.Value == NpgsqlDbType.TimestampTZ;
#endif
            }
        }

        /// <summary>
        /// 检查是否Unicode数据类型
        /// </summary>
        public override bool IsUnicode
        {
            get { return false; }
        }

        /// <summary>
        /// 生成 SqlServer的 DbType 元组
        /// </summary>
        /// <param name="dbType"></param>
        /// <returns></returns>
        public static NpgDbTypeInfo Create(object dbType)
        {
            if (dbType == null) return null;
            else if (dbType is DbType) return new NpgDbTypeInfo((DbType)dbType);
            else if (dbType is NpgsqlDbType) return new NpgDbTypeInfo(null, (NpgsqlDbType)dbType);
            else throw new NotSupportedException(string.Format("{0} is not a {1} or {2} type.", dbType, typeof(DbType).FullName, typeof(NpgsqlDbType).FullName));
        }

        /// <summary>
        /// 检查是否Unicode数据类型
        /// </summary>
        public static bool IsUnicode2(object dbType)
        {
            return false;
        }

        /// <summary>
        /// 实例化 <see cref="_SqlDbType"/> 类的新实例
        /// </summary>
        /// <param name="dbType">.NET 自带DbType</param>
        /// <param name="sqlDbType">其它数据库组件的DbType</param>
        NpgDbTypeInfo(DbType? dbType = null, NpgsqlDbType? sqlDbType = null)
        {
            this.DbType = dbType;
            this.SqlDbType = sqlDbType;
        }
    }
}
