using System;
using System.Data;
using System.Data.SQLite;

namespace Riz.XFramework.Data.SqlClient
{
    /// <summary>
    /// SQLite 值转SQL表达式解析器
    /// </summary>
    public class SQLiteDbFuncletizer : SQLParser
    {
        /// <summary>
        /// 值转SQL表达式解析器实例
        /// </summary>
        public static SQLiteDbFuncletizer Instance = new SQLiteDbFuncletizer();

        /// <summary>
        /// 实例化 <see cref="SQLiteDbFuncletizer"/> 类的新实例
        /// </summary>
        protected SQLiteDbFuncletizer()
            : base(SQLiteDbQueryProvider.Instance)
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
            if (value is Guid)
            {
                // 转换一下GUID，否则保存后会出现乱码~
                value = ((Guid)value).ToString();
            }
            else if (value is TimeSpan || value is DateTime || value is DateTimeOffset)
            {
                string result = null;
                if (value is TimeSpan)
                    result = this.GetSqlValueOfTime(value, dbType, scale);
                else if (value is DateTime)
                    result = this.GetSqlValueOfDateTime(value, dbType, scale);
                else if (value is DateTimeOffset)
                    result = this.GetSqlValueOfDateTimeOffset(value, dbType, scale);

                result = result.Trim('\'');
                value = result;
                dbType = DbType.String;
            }

            // 补充 DbType
            SQLiteParameter sqliteParameter = (SQLiteParameter)base.CreateParameter(value, context, dbType, size, precision, scale, direction);
            sqliteParameter.DbType(dbType);
            return sqliteParameter;
        }

        /// <summary>
        /// 获取 byte[] 类型的 SQL 片断
        /// </summary>
        /// <param name="value">SQL值</param>
        protected override string GetSqlValueOfBytes(object value)
        {
            byte[] bytes = (byte[])value;
            string hex = Common.BytesToHex(bytes, false, true);
            hex = string.Format(@"X'{0}'", hex);
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
            bool unicode = DbTypeUtils.IsUnicode(dbType);
            string result = this.EscapeQuote(value.ToString(), unicode, true);
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
            // SQLSERVER 的Time类型范围：00:00:00.0000000 到 23:59:59.9999999
            // https://docs.microsoft.com/zh-cn/sql/t-sql/data-types/time-transact-sql?view=sql-server-2017

            // 默认精度为7
            string format = @"hh\:mm\:ss\.fffffff";
            if (DbTypeUtils.IsTime(dbType))
            {
                string s = string.Empty;
                if (scale != null && scale.Value > 0) s = string.Empty.PadLeft(scale.Value > 7 ? 7 : scale.Value, 'f');
                if (!string.IsNullOrEmpty(s)) format = string.Format(@"hh\:mm\:ss\.{0}", s);
            }

            string result = this.EscapeQuote(((TimeSpan)value).ToString(format), false, false);
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
            // 默认精度为3
            string format = "yyyy-MM-dd HH:mm:ss.fff";
            if (DbTypeUtils.IsDate(dbType)) format = "yyyy-MM-dd";
            else if (DbTypeUtils.IsDateTime(dbType)) format = "yyyy-MM-dd HH:mm:ss.fff";
            else if (DbTypeUtils.IsDateTime2(dbType))
            {
                string s = string.Empty;
                format = "yyyy-MM-dd HH:mm:ss.fffffff";
                if (scale != null && scale.Value > 0) s = string.Empty.PadLeft(scale.Value > 7 ? 7 : scale.Value, 'f');
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
        protected override string GetSqlValueOfDateTimeOffset(object value, object dbType, int? scale)
        {
            // 默认精度为7
            string format = "yyyy-MM-dd HH:mm:ss.fffffff";
            if (DbTypeUtils.IsDateTimeOffset(dbType))
            {
                string s = string.Empty;
                if (scale != null && scale.Value > 0) s = string.Empty.PadLeft(scale.Value > 7 ? 7 : scale.Value, 'f');
                if (!string.IsNullOrEmpty(s)) format = string.Format("yyyy-MM-dd HH:mm:ss.{0}", s);
            }

            string myDateTime = ((DateTimeOffset)value).DateTime.ToString(format);
            string myOffset = ((DateTimeOffset)value).Offset.ToString(@"hh\:mm");
            myOffset = string.Format("{0}{1}", ((DateTimeOffset)value).Offset < TimeSpan.Zero ? '-' : '+', myOffset);

            string result = string.Format("'{0} {1}'", myDateTime, myOffset);
            return result;
        }


        // 如果用SQL的日期函数进行赋值，DateTime字段类型要用GETDATE()，DateTime2字段类型要用SYSDATETIME()。
        // https://docs.microsoft.com/zh-cn/sql/t-sql/data-types/time-transact-sql?view=sql-server-2017
    }
}
