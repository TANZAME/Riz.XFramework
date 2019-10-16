
using System;
using System.Data;
using MySql.Data.MySqlClient;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// 参数类型封装
    /// </summary>
    internal class MySqlDbTypeInfo : DbTypeInfo
    {
        private MySqlDbType? _sqlDbType = null;

        /// <summary>
        /// 其它数据库组件的DbType
        /// </summary>
        public new MySqlDbType? SqlDbType
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
                return this.DbType != null && this.DbType.Value == System.Data.DbType.Time ||
                    this.SqlDbType != null && this.SqlDbType.Value == MySqlDbType.Time;
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
                    this.SqlDbType != null && this.SqlDbType.Value == MySqlDbType.Date;
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
                    this.SqlDbType != null && this.SqlDbType.Value == MySqlDbType.DateTime;
            }
        }

        /// <summary>
        /// 是否日期+时间+精度类型
        /// </summary>
        public override bool IsDateTime2
        {
            get
            {
                return this.IsDateTime;
            }
        }

        /// <summary>
        /// 是否日期+时间+精度+时区类型
        /// </summary>
        public override bool IsDateTimeOffset
        {
            get
            {
                throw new NotSupportedException("MySQL does not support DateTimeOffset type.");
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
        public static MySqlDbTypeInfo Create(object dbType)
        {
            if (dbType == null) return null;
            else if (dbType is DbType) return new MySqlDbTypeInfo((DbType)dbType);
            else if (dbType is MySqlDbType) return new MySqlDbTypeInfo(null, (MySqlDbType)dbType);
            else throw new NotSupportedException(string.Format("{0} is not a {1} or {2} type.", dbType, typeof(DbType).FullName, typeof(MySqlDbType).FullName));
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
        MySqlDbTypeInfo(DbType? dbType = null, MySqlDbType? sqlDbType = null)
        {
            this.DbType = dbType;
            this.SqlDbType = sqlDbType;
        }
    }
}
