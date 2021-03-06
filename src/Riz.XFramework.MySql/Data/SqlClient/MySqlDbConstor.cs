﻿
using System;
using System.Data;
#if net40
using MySql.Data.MySqlClient;
#else
using MySqlConnector;
#endif

namespace Riz.XFramework.Data.SqlClient
{
    /// <summary>
    /// SQL 语句构造器
    /// </summary>
    internal class MySqlDbConstor : DbConstor
    {
        /// <summary>
        /// 常量值转SQL表达式解析器实例
        /// </summary>
        public static MySqlDbConstor Instance = new MySqlDbConstor();

        /// <summary>
        /// 实例化 <see cref="MySqlDbConstor"/> 类的新实例
        /// </summary>
        protected MySqlDbConstor()
            : base(MySqlDbQueryProvider.Instance)
        {

        }

        /// <summary>
        /// 增加一个SQL参数
        /// </summary>
        /// <param name="value">值</param>
        /// <param name="context">解析SQL命令时的参数上下文</param>
        /// <param name="dbType">数据类型</param>
        /// <param name="size">长度</param>
        /// <param name="precision">精度</param>
        /// <param name="scale">小数位</param>
        /// <param name="direction">查询参数类型</param>
        /// <returns></returns>
        protected override IDbDataParameter CreateParameter(object value, ITranslateContext context,
            object dbType, int? size = null, int? precision = null, int? scale = null, ParameterDirection? direction = null)
        {
            // 补充 DbType
            MySqlParameter mySqlParameter = (MySqlParameter)base.CreateParameter(value, context, dbType, size, precision, scale, direction);
            mySqlParameter.DbType(dbType);
            return mySqlParameter;
        }

        /// <summary>
        /// 获取 byte[] 类型的 SQL 片断
        /// </summary>
        /// <param name="value">SQL值</param>
        protected override string GetSqlValueOfBytes(object value)
        {
            string hex = base.GetSqlValueOfBytes(value);
            if (hex == "0x") hex = "_binary''";
            return hex;
        }

        /// <summary>
        /// 获取 String 类型的 SQL 片断
        /// </summary>
        /// <param name="value">值</param>
        /// <param name="dbType">数据类型</param>
        /// <param name="size">长度</param>
        /// <returns></returns>
        protected override string GetSqlValueOfString(object value, object dbType, int? size = null)
        {
            string result = this.EscapeQuote(value.ToString(), false, true);
            return result;
        }

        /// <summary>
        /// 获取 Time 类型的 SQL 片断
        /// </summary>
        /// <param name="value">值</param>
        /// <param name="dbType">数据类型</param>
        /// <param name="scale">小数位</param>
        /// <returns></returns>
        protected override string GetSqlValueOfTime(object value, object dbType, int? scale)
        {
            // the range is '-838:59:59.000000' to '838:59:59.000000' new TimeSpan(-34, -22, -59, -59)~new TimeSpan(34, 22, 59, 59);
            // https://dev.mysql.com/doc/refman/8.0/en/time.html

            TimeSpan ts = (TimeSpan)value;
            int hours = (int)ts.TotalHours;
            // 默认精度为7
            string format = @"mm\:ss\.ffffff";
            if (MySqlUtils.IsTime(dbType))
            {
                string s = string.Empty;
                if (scale != null && scale.Value > 0) s = string.Empty.PadLeft(scale.Value > 6 ? 6 : scale.Value, 'f');
                if (!string.IsNullOrEmpty(s)) format = string.Format(@"mm\:ss\.{0}", s);
            }

            string result = ts.ToString(format);
            result = string.Format("{0}:{1}", hours, result);
            result = this.EscapeQuote(result, false, false);
            return result;
        }

        /// <summary>
        /// 获取 DatetTime 类型的 SQL 片断
        /// </summary>
        /// <param name="value">值</param>
        /// <param name="dbType">数据类型</param>
        /// <param name="scale">小数位</param>
        /// <returns></returns>
        protected override string GetSqlValueOfDateTime(object value, object dbType, int? scale)
        {
            // 默认精度为0
            string format = "yyyy-MM-dd HH:mm:ss";
            if (MySqlUtils.IsDate(dbType)) format = "yyyy-MM-dd";
            else if (MySqlUtils.IsDateTime(dbType))
            {
                string s = string.Empty;
                if (scale != null && scale.Value > 0) s = string.Empty.PadLeft(scale.Value > 6 ? 6 : scale.Value, 'f');
                if (!string.IsNullOrEmpty(s)) format = string.Format("yyyy-MM-dd HH:mm:ss.{0}", s);
            }

            string result = this.EscapeQuote(((DateTime)value).ToString(format), false, false);
            return result;
        }

        /// <summary>
        /// 获取 DateTimeOffset 类型的 SQL 片断
        /// </summary>
        /// <param name="value">值</param>
        /// <param name="dbType">数据类型</param>
        /// <param name="scale">小数位</param>
        /// <returns></returns>
        protected override string GetSqlValueOfDateTimeOffset(object value, object dbType, int? scale = null)
        {
            MySqlUtils.IsDateTimeOffset(dbType);
            return null;
        }

        // MySQL 8.0以上版本没有 nvarchar,nchar 的类型
        // https://dev.mysql.com/doc/refman/8.0/en/charset-national.html

        // timestamp：
        // yyyymmddhhmmss格式表示的时间戳值，TIMESTAMP列用于INSERT或UPDATE操作时记录日期和时间。如果你不分配一个值，表中的第一个TIMESTAMP列自动设置为最近操作的日期和时间。
        // 也可以通过分配一个NULL值，将TIMESTAMP列设置为当前的日期和时间。TIMESTAMP值返回后显示为’YYYY-MM-DD HH:MM:SS’格式的字符串，显示宽度固定为19个字符。
        // 如果想要获得数字值，应在TIMESTAMP 列添加+0。

        //在MySQL中：
        //DATETIME ：长度8字节，用来标识包含日期和时间部分的值，MySQL以‘YYYY-MM-DD HH:MM:SS’格式检索并显示DATETIME类型字段。
        //支持的范围是‘1000-01-01 00:00:00’ to ‘9999-12-31 23:59:59’.

        //TIMESTAMP ：长度4字节，用来标识包含日期和时间部分的值，
        //支持的范围是 ‘1970-01-01 00:00:01’ （标准时间） to ‘2038-01-19 03:14:07’ （标准时间）。
        //https://dev.mysql.com/doc/refman/8.0/en/datetime.html

        //DATETIME 与TIMESTAMP 的不同：
        //MySQL将TIMESTAMP类型的值转换为UTC时间存储，当然检索的时候以当前时区的时间返回，下面具体举例，而DATETIME则不会发生这种情况。
    }
}
