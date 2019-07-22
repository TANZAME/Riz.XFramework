
using System;
using System.Data;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// SQL 语句构造器
    /// </summary>
    public class MySqlBuilder : SqlBuilderBase
    {
        // MySQL 8.0以上版本没有 nvarchar,nchar 的类型
        // https://dev.mysql.com/doc/refman/8.0/en/charset-national.html

        //timestamp：
        //yyyymmddhhmmss格式表示的时间戳值，TIMESTAMP列用于INSERT或UPDATE操作时记录日期和时间。如果你不分配一个值，表中的第一个TIMESTAMP列自动设置为最近操作的日期和时间。
        //也可以通过分配一个NULL值，将TIMESTAMP列设置为当前的日期和时间。TIMESTAMP值返回后显示为’YYYY-MM-DD HH:MM:SS’格式的字符串，显示宽度固定为19个字符。如果想要获得数字值，应在TIMESTAMP 列添加+0。

        /// <summary>
        /// 实例化 <see cref="MySqlBuilder"/> 类的新实例
        /// </summary>
        /// <param name="provider">提供者</param>
        /// <param name="parameter">参数列表，NULL 或者 Parameters=NULL 时表示不使用参数化</param>
        public MySqlBuilder(IDbQueryProvider provider, ParserParameter parameter = null)
            : base(provider, parameter)
        {

        }

        // 增加一个参数
        protected override IDbDataParameter AddParameter(object value, object dbType, int? size = null, int? precision = null, int? scale = null, ParameterDirection? direction = null)
        {
            MySqlParameter parameter = (MySqlParameter)base.AddParameter(value, dbType, size, precision, scale, direction);

            // 补充 DbType
            MySqlDbTypeInfo dbTypeInfo = MySqlDbTypeInfo.Create(dbType);
            if (dbTypeInfo != null && dbTypeInfo.DbType != null) parameter.DbType = dbTypeInfo.DbType.Value;
            else if (dbTypeInfo != null && dbTypeInfo.SqlDbType != null) parameter.MySqlDbType = dbTypeInfo.SqlDbType.Value;

            if (size != null && (size.Value > 0 || size.Value == -1)) parameter.Size = size.Value;
            if (precision != null && precision.Value > 0) parameter.Precision = (byte)precision.Value;
            if (scale != null && scale.Value > 0) parameter.Scale = (byte)scale.Value;

            return parameter;
        }

        // 获取 String 类型的 SQL 片断
        protected override string GetSqlValueByString(object value, object dbType, int? size = null)
        {
            string result = this.EscapeQuote(value.ToString(), false, true);
            return result;
        }

        // 获取 Time 类型的 SQL 片断
        protected override string GetSqlValueByTime(object value, object dbType, int? precision)
        {
            string format = @"hh\:mm\:ss";
            string result = this.EscapeQuote(((TimeSpan)value).ToString(format), false, false);
            return result;
        }

        // 获取 DatetTime 类型的 SQL 片断（包括DateTime和TimeStamp)
        protected override string GetSqlValueByDateTime(object value, object dbType, int? precision)
        {
            // 默认精度为0
            string format = "yyyy-MM-dd HH:mm:ss";
            MySqlDbTypeInfo dbTypeInfo = MySqlDbTypeInfo.Create(dbType);

            if (dbTypeInfo != null && dbTypeInfo.IsDate) format = "yyyy-MM-dd";
            else if (dbTypeInfo != null && (dbTypeInfo.IsDateTime || dbTypeInfo.IsDateTime2))
            {
                string pad = string.Empty;
                if (precision != null && precision.Value > 0) pad = "f".PadLeft(precision.Value > 6 ? 6 : precision.Value, 'f');
                if (!string.IsNullOrEmpty(pad)) format = string.Format("yyyy-MM-dd HH:mm:ss.{0}", pad);
            }

            string result = this.EscapeQuote(((DateTime)value).ToString(format), false, false);
            return result;
        }

        // 获取 DateTimeOffset 类型的 SQL 片断
        protected override string GetSqlValueByDateTimeOffset(object value, object dbType, int? precision = null)
        {
            throw new NotSupportedException("MySQL does not support DateTimeOffset type.");
        }
    }
}
