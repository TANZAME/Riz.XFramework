
using System;
using System.Linq;
using System.Text;
using System.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// 参数类型封装
    /// </summary>
    internal class SqlDbTypeInfo : DbTypeInfo
    {
        private SqlDbType? _sqlDbType = null;

        /// <summary>
        /// 其它数据库组件的DbType
        /// </summary>
        public new SqlDbType? SqlDbType
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
                    this.SqlDbType != null && this.SqlDbType.Value == System.Data.SqlDbType.Time;
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
                    this.SqlDbType != null && this.SqlDbType.Value == System.Data.SqlDbType.Date;
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
                    this.SqlDbType != null && this.SqlDbType.Value == System.Data.SqlDbType.DateTime;
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
                    this.SqlDbType != null && this.SqlDbType.Value == System.Data.SqlDbType.DateTime2;
            }
        }

        /// <summary>
        /// 是否日期+时间+精度+时区类型
        /// </summary>
        public override bool IsDateTimeOffset
        {
            get
            {
                return this.DbType != null && this.DbType.Value == System.Data.DbType.DateTimeOffset ||
                    this.SqlDbType != null && this.SqlDbType.Value == System.Data.SqlDbType.DateTimeOffset;
            }
        }

        /// <summary>
        /// 检查是否Unicode数据类型
        /// </summary>
        public override bool IsUnicode
        {
            get
            {
                if (this.DbType != null) return this.DbType.Value != System.Data.DbType.AnsiString && this.DbType.Value != System.Data.DbType.AnsiStringFixedLength;
                else if (this.SqlDbType != null) return this.SqlDbType.Value != System.Data.SqlDbType.VarChar && this.SqlDbType != System.Data.SqlDbType.Char && this.SqlDbType.Value != System.Data.SqlDbType.Text;
                return true;
            }
        }

        /// <summary>
        /// 生成 SqlServer的 DbType 元组
        /// </summary>
        /// <param name="dbType"></param>
        /// <returns></returns>
        public static SqlDbTypeInfo Create(object dbType)
        {
            if (dbType == null) return null;
            else if (dbType is DbType) return new SqlDbTypeInfo((DbType)dbType);
            else if (dbType is SqlDbType) return new SqlDbTypeInfo(null, (SqlDbType)dbType);
            else throw new NotSupportedException(string.Format("{0} is not a {1} or {2} type.", dbType, typeof(DbType).FullName, typeof(SqlDbType).FullName));
        }

        /// <summary>
        /// 检查是否Unicode数据类型
        /// </summary>
        public static bool IsUnicode2(object dbType)
        {
            if (dbType == null) return true;
            SqlDbTypeInfo dbTypeInfo = SqlDbTypeInfo.Create(dbType);
            return dbTypeInfo != null ? dbTypeInfo.IsUnicode : true;
        }

        /// <summary>
        /// 实例化 <see cref="SqlDbTypeInfo"/> 类的新实例
        /// </summary>
        /// <param name="dbType">.NET 自带DbType</param>
        /// <param name="sqlDbType">其它数据库组件的DbType</param>
        SqlDbTypeInfo(DbType? dbType = null, SqlDbType? sqlDbType = null)
        {
            this.DbType = dbType;
            this.SqlDbType = sqlDbType;
        }
    }
}
