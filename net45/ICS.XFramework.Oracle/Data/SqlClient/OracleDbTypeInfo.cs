
using System;
using System.Data;
using Oracle.ManagedDataAccess.Client;

namespace ICS.XFramework.Data
{
    /// <summary>
    /// 参数类型封装
    /// </summary>
    internal class OracleDbTypeInfo : DbTypeInfo
    {
        private OracleDbType? _sqlDbType = null;

        /// <summary>
        /// 其它数据库组件的DbType
        /// </summary>
        public new OracleDbType? SqlDbType
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
                throw new NotSupportedException("Oracle does not support Time type.");
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
                    this.SqlDbType != null && this.SqlDbType.Value == OracleDbType.Date;
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
                    this.SqlDbType != null && this.SqlDbType.Value == OracleDbType.TimeStamp;
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
                    this.SqlDbType != null && this.SqlDbType.Value == OracleDbType.TimeStamp;
            }
        }

        /// <summary>
        /// 是否日期+时间+精度+时区类型
        /// </summary>
        public override bool IsDateTimeOffset
        {
            get
            {
                //return this.DbType != null && this.DbType.Value == System.Data.DbType.DateTimeOffset ||
                //    this.SqlDbType != null && this.SqlDbType.Value == OracleDbType.TimeStampTZ;
                throw new NotSupportedException("Oracle does not support DateTimeOffset type.");
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
                else if (this.SqlDbType != null) return this.SqlDbType.Value != OracleDbType.Char && this.SqlDbType.Value != OracleDbType.Varchar2;
                return true;
            }
        }

        /// <summary>
        /// 生成 SqlServer的 DbType 元组
        /// </summary>
        /// <param name="dbType"></param>
        /// <returns></returns>
        public static OracleDbTypeInfo Create(object dbType)
        {
            if (dbType == null) return null;
            else if (dbType is DbType) return new OracleDbTypeInfo((DbType)dbType);
            else if (dbType is OracleDbType) return new OracleDbTypeInfo(null, (OracleDbType)dbType);
            else throw new NotSupportedException(string.Format("{0} is not a {1} or {2} type.", dbType, typeof(DbType).FullName, typeof(OracleDbType).FullName));
        }

        /// <summary>
        /// 检查是否Unicode数据类型
        /// </summary>
        public static bool IsUnicode2(object dbType)
        {
            if (dbType == null) return true;
            OracleDbTypeInfo dbTypeInfo = OracleDbTypeInfo.Create(dbType);
            return dbTypeInfo != null ? dbTypeInfo.IsUnicode : true;
        }

        /// <summary>
        /// 实例化 <see cref="OracleDbTypeInfo"/> 类的新实例
        /// </summary>
        /// <param name="dbType">.NET 自带DbType</param>
        /// <param name="sqlDbType">其它数据库组件的DbType</param>
        OracleDbTypeInfo(DbType? dbType = null, OracleDbType? sqlDbType = null)
        {
            this.DbType = dbType;
            this.SqlDbType = sqlDbType;
        }
    }
}
