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
    public class SqlBuilder : TextBuilder
    {
        /// <summary>
        /// 无参数化 Builder
        /// </summary>
        internal static SqlBuilder NonTokenBuilder = null;

        /// <summary>
        /// 实例化 <see cref="SqlBuilder"/> 类的新实例
        /// </summary>
        /// <param name="provider">提供者</param>
        /// <param name="token">参数列表，NULL 或者 Parameters=NULL 时表示不使用参数化</param>
        public SqlBuilder(IDbQueryProvider provider, ParserToken token)
            : base(provider, token)
        {

        }

        // 增加一个参数
        protected override IDbDataParameter AddParameter(object value, object dbType, int? size = null, int? precision = null, int? scale = null, ParameterDirection? direction = null)
        {
            SqlParameter parameter = (SqlParameter)base.AddParameter(value, dbType, size, precision, scale, direction);
            // 补充 DbType
            parameter.PrepareDbType(dbType);

            return parameter;
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

            string date = this.EscapeQuote(((DateTimeOffset)value).DateTime.ToString(format), false, false);
            string span = ((DateTimeOffset)value).Offset.ToString(@"hh\:mm");
            span = string.Format("{0}{1}", ((DateTimeOffset)value).Offset < TimeSpan.Zero ? '-' : '+', span);
            span = this.EscapeQuote(span, false, false);

            string result = string.Format("TODATETIMEOFFSET({0},{1})", date, span);
            return result;
        }


        // 如果用SQL的日期函数进行赋值，DateTime字段类型要用GETDATE()，DateTime2字段类型要用SYSDATETIME()。
        // https://docs.microsoft.com/zh-cn/sql/t-sql/data-types/time-transact-sql?view=sql-server-2017
    }
}
