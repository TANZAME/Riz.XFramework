using System;
using System.Data;
using System.Data.SQLite;

namespace TZM.XFramework.Data.SqlClient
{
    /// <summary>
    /// SQLite SQL字段值生成器
    /// </summary>
    public class SQLiteDbValue : DbValue
    {
        /// <summary>
        /// SQL字段值生成器实例
        /// </summary>
        public static SQLiteDbValue Instance = new SQLiteDbValue();

        /// <summary>
        /// 实例化 <see cref="SQLiteDbValue"/> 类的新实例
        /// </summary>
        protected SQLiteDbValue()
            : base(SQLiteDbQueryProvider.Instance)
        {

        }

        // 增加一个参数
        protected override IDbDataParameter AddParameter(object value, ResolveToken token,
            object dbType, int? size = null, int? precision = null, int? scale = null, ParameterDirection? direction = null)
        {
            if (value is Guid)
            {
                // 转换一下GUID，否则保存后会出现乱码~
                value = ((Guid)value).ToString();
            }
            else if (value is TimeSpan)
            {
                string result = this.GetSqlValueByTime(value, dbType, scale);
                result = result.Trim('\'');
                value = result;
                dbType = DbType.String;
            }
            else if (value is DateTime && dbType != null && ((DbType)dbType) == DbType.Date)
            {
                value = ((DateTime)value).Date;
            }
            else if (value is DateTimeOffset)
            {
                string result = this.GetSqlValueByDateTimeOffset(value, dbType, scale);
                result = result.Trim('\'');
                value = result;
            }

            // 补充 DbType
            SQLiteParameter parameter = (SQLiteParameter)base.AddParameter(value, token, dbType, size, precision, scale, direction);
            parameter.DbType(dbType);
            return parameter;
        }

        // 获取 byte[] 类型的 SQL 片断
        protected override string GetSqlValueByBytes(object value)
        {
            byte[] bytes = (byte[])value;
            string hex = XfwCommon.BytesToHex(bytes, false, true);
            hex = string.Format(@"X'{0}'", hex);
            return hex;
        }

        // 获取 String 类型的 SQL 片断
        protected override string GetSqlValueByString(object value, object dbType, int? size = null)
        {
            bool unicode = DbTypeUtils.IsUnicode(dbType);
            string result = this.EscapeQuote(value.ToString(), unicode, true);
            return result;
        }

        // 获取 Time 类型的 SQL 片断
        protected override string GetSqlValueByTime(object value, object dbType, int? scale)
        {
            // SQLSERVER 的Time类型范围：00:00:00.0000000 到 23:59:59.9999999
            // https://docs.microsoft.com/zh-cn/sql/t-sql/data-types/time-transact-sql?view=sql-server-2017

            // 默认精度为7
            string format = @"hh\:mm\:ss\.fffffff";
            if (DbTypeUtils.IsTime(dbType))
            {
                string pad = string.Empty;
                if (scale != null && scale.Value > 0) pad = "f".PadLeft(scale.Value > 7 ? 7 : scale.Value, 'f');
                if (!string.IsNullOrEmpty(pad)) format = string.Format(@"hh\:mm\:ss\.{0}", pad);
            }

            string result = this.EscapeQuote(((TimeSpan)value).ToString(format), false, false);
            return result;
        }

        // 获取 DatetTime 类型的 SQL 片断
        protected override string GetSqlValueByDateTime(object value, object dbType, int? scale)
        {
            // 默认精度为3
            string format = "yyyy-MM-dd HH:mm:ss.fff";
            if (DbTypeUtils.IsDate(dbType)) format = "yyyy-MM-dd";
            else if (DbTypeUtils.IsDateTime(dbType)) format = "yyyy-MM-dd HH:mm:ss.fff";
            else if (DbTypeUtils.IsDateTime2(dbType))
            {
                string pad = string.Empty;
                if (scale != null && scale.Value > 0) pad = "f".PadLeft(scale.Value > 7 ? 7 : scale.Value, 'f');
                if (!string.IsNullOrEmpty(pad))
                    format = string.Format("yyyy-MM-dd HH:mm:ss.{0}", pad);
                else
                    format = "yyyy-MM-dd HH:mm:ss.fffffff";
            }

            string result = this.EscapeQuote(((DateTime)value).ToString(format), false, false);
            return result;
        }

        // 获取 DateTimeOffset 类型的 SQL 片断
        protected override string GetSqlValueByDateTimeOffset(object value, object dbType, int? scale)
        {
            // 默认精度为7
            string format = "yyyy-MM-dd HH:mm:ss.fffffff";
            if (DbTypeUtils.IsDateTimeOffset(dbType))
            {
                string pad = string.Empty;
                if (scale != null && scale.Value > 0) pad = "f".PadLeft(scale.Value > 7 ? 7 : scale.Value, 'f');
                if (!string.IsNullOrEmpty(pad)) format = string.Format("yyyy-MM-dd HH:mm:ss.{0}", pad);
            }

            string date = ((DateTimeOffset)value).DateTime.ToString(format);
            string span = ((DateTimeOffset)value).Offset.ToString(@"hh\:mm");
            span = string.Format("{0}{1}", ((DateTimeOffset)value).Offset < TimeSpan.Zero ? '-' : '+', span);

            string result = string.Format("'{0} {1}'", date, span);
            return result;
        }

        /// <summary>
        /// 检查是否Unicode数据类型
        /// </summary>
        public override bool IsUnicode(object dbType)
        {
            return DbTypeUtils.IsUnicode(dbType);
        }


        // 如果用SQL的日期函数进行赋值，DateTime字段类型要用GETDATE()，DateTime2字段类型要用SYSDATETIME()。
        // https://docs.microsoft.com/zh-cn/sql/t-sql/data-types/time-transact-sql?view=sql-server-2017
    }
}
