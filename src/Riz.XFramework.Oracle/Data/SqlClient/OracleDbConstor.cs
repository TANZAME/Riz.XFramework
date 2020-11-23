
using System;
using System.Data;
using System.Collections.Generic;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;

namespace Riz.XFramework.Data.SqlClient
{
    /// <summary>
    /// SQL 语句构造器
    /// </summary>
    internal class OracleDbConstor : DbConstor
    {
        /// <summary>
        /// 常量值转SQL表达式解析器实例
        /// </summary>
        public static OracleDbConstor Instance = new OracleDbConstor();

        /// <summary>
        /// 实例化 <see cref="OracleDbConstor"/> 类的新实例
        /// </summary>
        protected OracleDbConstor()
            : base(OracleDbQueryProvider.Instance)
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
            if (value is bool) value = ((bool)value) ? 1 : 0;
            else if (value is Guid)
            {
                value = ((Guid)value).ToByteArray();
                dbType = OracleDbType.Raw;
            }
            else if (value is DateTimeOffset)
            {
                var dto = (DateTimeOffset)value;
                var zone = (dto.Offset < TimeSpan.Zero ? "-" : "+") + dto.Offset.ToString("hh\\:mm");
                value = new OracleTimeStampTZ(dto.DateTime, zone);
            }

            // 补充 DbType
            OracleParameter oracleParameter = (OracleParameter)base.CreateParameter(value, context, dbType, size, precision, scale, direction);
            oracleParameter.DbType(dbType);
            return oracleParameter;
        }

        /// <summary>
        /// 获取 byte[] 类型的 SQL 片断
        /// </summary>
        /// <param name="value">SQL值</param>
        protected override string GetSqlValueOfBytes(object value)
        {
            byte[] bytes = (byte[])value;
            string hex = Common.BytesToHex(bytes, false, true);
            string result = string.Empty;
            if (string.IsNullOrEmpty(hex)) 
                result = "EMPTY_BLOB()";
            else
                result = string.Format("TO_BLOB(HEXTORAW('{0}'))", hex);

            return result;
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
            bool unicode = OracleUtils.IsUnicode(dbType);
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
            // https://docs.oracle.com/en/database/oracle/oracle-database/12.2/nlspg/datetime-data-types-and-time-zone-support.html#GUID-FD8C41B7-8CDC-4D02-8E6B-5250416BC17D

            TimeSpan ts = (TimeSpan)value;
            // 默认精度为6
            string format = @"hh\:mm\:ss\.ffffff";
            if (OracleUtils.IsTime(dbType))
            {
                string s = string.Empty;
                if (scale != null && scale.Value > 0) s = string.Empty.PadLeft(scale.Value > 7 ? 7 : scale.Value, 'f');
                if (!string.IsNullOrEmpty(s)) format = string.Format(@"hh\:mm\:ss\.{0}", s);
            }

            string result = ts.ToString(format);
            result = string.Format("{0} {1}", ts.Days, result);
            result = this.EscapeQuote(result, false, false);
            result = string.Format("TO_DSINTERVAL({0})", result);
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
            DateTime date = (DateTime)value;

            // 默认精度6
            string format = "yyyy-MM-dd HH:mm:ss.ffffff";
            if (OracleUtils.IsDate(dbType)) format = "yyyy-MM-dd";
            else if (OracleUtils.IsDateTime(dbType) || OracleUtils.IsDateTime2(dbType))
            {
                string s = string.Empty;
                format = "yyyy-MM-dd HH:mm:ss.ffffff";
                if (scale != null && scale.Value > 0) s = string.Empty.PadLeft(scale.Value > 7 ? 7 : scale.Value, 'f');
                if (!string.IsNullOrEmpty(s)) format = string.Format("yyyy-MM-dd HH:mm:ss.{0}", s);
            }

            string result = this.EscapeQuote(((DateTime)value).ToString(format), false, false);
            result = string.Format("TO_TIMESTAMP({0},'yyyy-mm-dd hh24:mi:ss.ff')", result);
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
            // 默认精度为6
            string format = "yyyy-MM-dd HH:mm:ss.ffffff";
            if (OracleUtils.IsDateTimeOffset(dbType))
            {
                string s = string.Empty;
                if (scale != null && scale.Value > 0) s = string.Empty.PadLeft(scale.Value > 7 ? 7 : scale.Value, 'f');
                if (!string.IsNullOrEmpty(s)) format = string.Format("yyyy-MM-dd HH:mm:ss.{0}", s);
            }

            string myDateTime = ((DateTimeOffset)value).DateTime.ToString(format);
            string myOffset = ((DateTimeOffset)value).Offset.ToString(@"hh\:mm");
            myOffset = string.Format("{0}{1}", ((DateTimeOffset)value).Offset < TimeSpan.Zero ? '-' : '+', myOffset);

            string result = string.Format("TO_TIMESTAMP_TZ('{0} {1}','yyyy-mm-dd hh24:mi:ss.ff tzh:tzm')", myDateTime, myOffset);
            return result;
        }

        /// <summary>
        /// 获取 Guid 类型的 SQL 片断
        /// </summary>
        /// <param name="value">值</param>
        /// <returns></returns>
        protected override string GetSqlValueOfGuid(object value)
        {
            string b = BitConverter.ToString(((Guid)value).ToByteArray()).Replace("-", "");
            return this.EscapeQuote(b, false, false);
        }


        // 官方数据类型和.NET数据类型映射关系
        // https://docs.oracle.com/database/121/ODPNT/featTypes.htm#ODPNT281
        // https://docs.oracle.com/en/database/oracle/oracle-database/12.2/sqlrf/Data-Types.html#GUID-7B72E154-677A-4342-A1EA-C74C1EA928E6
    }
}
