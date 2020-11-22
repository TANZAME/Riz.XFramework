using System;
using System.Data;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace Riz.XFramework.Data.SqlClient
{
    /// <summary>
    /// MSSQL 值转SQL表达式解析器
    /// </summary>
    internal class SqlServerDbFuncletizer : DbSQLParser
    {
        /// <summary>
        /// 值转SQL表达式解析器实例
        /// </summary>
        internal static SqlServerDbFuncletizer Instance = new SqlServerDbFuncletizer();

        /// <summary>
        /// 实例化 <see cref="SqlServerDbFuncletizer"/> 类的新实例
        /// </summary>
        protected SqlServerDbFuncletizer()
            : base(SqlServerDbQueryProvider.Instance)
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
            SqlParameter sqlParameter = (SqlParameter)base.CreateParameter(value, context, dbType, size, precision, scale, direction);
            sqlParameter.DbType(dbType);
            return sqlParameter;
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
            bool isUnicode = DbTypeUtils.IsUnicode(dbType);
            string result = this.EscapeQuote(value.ToString(), isUnicode, true);
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

            string myDateTime = this.EscapeQuote(((DateTimeOffset)value).DateTime.ToString(format), false, false);
            string myOffset = ((DateTimeOffset)value).Offset.ToString(@"hh\:mm");
            myOffset = string.Format("{0}{1}", ((DateTimeOffset)value).Offset < TimeSpan.Zero ? '-' : '+', myOffset);
            myOffset = this.EscapeQuote(myOffset, false, false);

            string result = string.Format("TODATETIMEOFFSET({0},{1})", myDateTime, myOffset);
            return result;
        }

        // 如果用SQL的日期函数进行赋值，DateTime字段类型要用GETDATE()，DateTime2字段类型要用SYSDATETIME()。
        // https://docs.microsoft.com/zh-cn/sql/t-sql/data-types/time-transact-sql?view=sql-server-2017
        // 数据类型转换优先级： https://docs.microsoft.com/zh-cn/sql/t-sql/data-types/data-type-precedence-transact-sql?redirectedfrom=MSDN&view=sql-server-ver15
    }
}
