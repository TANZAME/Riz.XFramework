using System;
using System.Data;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace TZM.XFramework.Data.SqlClient
{
    /// <summary>
    /// MSSQL 语句建造器
    /// <para>非线程安全</para>
    /// </summary>
    public class SqlBuilder : SqlBuilderBase
    {
        /// <summary>
        /// 实例化 <see cref="SqlBuilder"/> 类的新实例
        /// </summary>
        /// <param name="provider">提供者</param>
        /// <param name="parameter">参数列表，NULL 或者 Parameters=NULL 时表示不使用参数化</param>
        public SqlBuilder(IDbQueryProvider provider, ParserParameter parameter)
            : base(provider, parameter)
        {

        }

        // 增加一个参数
        protected override IDbDataParameter AddParameter(object value, object dbType, int? size = null, int? precision = null, int? scale = null, ParameterDirection? direction = null)
        {
            SqlParameter parameter = (SqlParameter)base.AddParameter(value, dbType, size, precision, scale, direction);

            // 补充 DbType
            SqlDbTypeInfo dbTypeInfo = SqlDbTypeInfo.Create(dbType);
            if (dbTypeInfo != null && dbTypeInfo.DbType != null)
            {
                parameter.DbType = dbTypeInfo.DbType.Value;
                if (dbTypeInfo.DbType.Value == DbType.Time) parameter.SqlDbType = SqlDbType.Time;
            }
            else if (dbTypeInfo != null && dbTypeInfo.SqlDbType != null) parameter.SqlDbType = dbTypeInfo.SqlDbType.Value;

            if (size != null && (size.Value > 0 || size.Value == -1)) parameter.Size = size.Value;
            if (precision != null && precision.Value > 0) parameter.Precision = (byte)precision.Value;
            if (scale != null && scale.Value > 0) parameter.Scale = (byte)scale.Value;

            return parameter;
        }

        // 获取 String 类型的 SQL 片断
        protected override string GetSqlValueByString(object value, object dbType, int? size = null)
        {
            bool unicode = SqlDbTypeInfo.IsUnicode2(dbType);
            string result = this.EscapeQuote(value.ToString(), unicode, true);
            return result;
        }

        // 获取 Time 类型的 SQL 片断
        protected override string GetSqlValueByTime(object value, object dbType, int? precision)
        {
            // 默认精度为7
            string format = @"hh\:mm\:ss\.fffffff";
            SqlDbTypeInfo dbTypeInfo = SqlDbTypeInfo.Create(dbType);

            if (dbTypeInfo != null && dbTypeInfo.IsTime)
            {
                string pad = string.Empty;
                if (precision != null && precision.Value > 0) pad = "f".PadLeft(precision.Value > 7 ? 7 : precision.Value, 'f');
                if (!string.IsNullOrEmpty(pad)) format = string.Format(@"hh\:mm\:ss\.{0}", pad);
            }

            string result = this.EscapeQuote(((TimeSpan)value).ToString(format), false, false);
            return result;
        }

        // 获取 DatetTime 类型的 SQL 片断
        protected override string GetSqlValueByDateTime(object value, object dbType, int? precision)
        {
            // 默认精度为3
            string format = "yyyy-MM-dd HH:mm:ss.fff";
            SqlDbTypeInfo dbTypeInfo = SqlDbTypeInfo.Create(dbType);

            if (dbTypeInfo != null && dbTypeInfo.IsDate) format = "yyyy-MM-dd";
            else if (dbTypeInfo != null && dbTypeInfo.IsDateTime) format = "yyyy-MM-dd HH:mm:ss.fff";
            else if (dbTypeInfo != null && dbTypeInfo.IsDateTime2)
            {
                string pad = string.Empty;
                if (precision != null && precision.Value > 0) pad = "f".PadLeft(precision.Value > 7 ? 7 : precision.Value, 'f');
                if (!string.IsNullOrEmpty(pad))
                    format = string.Format("yyyy-MM-dd HH:mm:ss.{0}", pad);
                else
                    format = "yyyy-MM-dd HH:mm:ss.fffffff";
            }

            string result = this.EscapeQuote(((DateTime)value).ToString(format), false, false);
            return result;
        }

        // 获取 DateTimeOffset 类型的 SQL 片断
        protected override string GetSqlValueByDateTimeOffset(object value, object dbType, int? precision)
        {
            // 默认精度为7
            string format = "yyyy-MM-dd HH:mm:ss.fffffff";
            SqlDbTypeInfo dbTypeInfo = SqlDbTypeInfo.Create(dbType);

            if (dbTypeInfo != null && dbTypeInfo.IsDateTimeOffset)
            {
                string pad = string.Empty;
                if (precision != null && precision.Value > 0) pad = "f".PadLeft(precision.Value > 7 ? 7 : precision.Value, 'f');
                if (!string.IsNullOrEmpty(pad)) format = string.Format("yyyy-MM-dd HH:mm:ss.{0}", pad);
            }

            string date = this.EscapeQuote(((DateTimeOffset)value).DateTime.ToString(format), false, false);
            string span = ((DateTimeOffset)value).Offset.ToString(@"hh\:mm");
            span = string.Format("{0}{1}", ((DateTimeOffset)value).Offset.Hours >= 0 ? '+' : '-', span);
            span = this.EscapeQuote(span, false, false);

            string result = string.Format("TODATETIMEOFFSET({0},{1})", date, span);
            return result;
        }
    }
}
