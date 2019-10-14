
using System;
using System.Data;
using System.Collections.Generic;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;

namespace TZM.XFramework.Data.SqlClient
{
    /// <summary>
    /// SQL 语句构造器
    /// </summary>
    public class OracleValueGenerator : ValueGenerator
    {
        /// <summary>
        /// SQL字段值生成器实例
        /// </summary>
        public static OracleValueGenerator Instance = new OracleValueGenerator();

        /// <summary>
        /// 实例化 <see cref="OracleValueGenerator"/> 类的新实例
        /// </summary>
        protected OracleValueGenerator()
            : base(OracleDbQueryProvider.Instance)
        {

        }

        // 增加一个参数
        protected override IDbDataParameter AddParameter(object value, ResolveToken token, 
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
            OracleParameter parameter = (OracleParameter)base.AddParameter(value, token, dbType, size, precision, scale, direction);
            parameter.DbType(dbType);
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
            // https://docs.oracle.com/en/database/oracle/oracle-database/12.2/nlspg/datetime-data-types-and-time-zone-support.html#GUID-FD8C41B7-8CDC-4D02-8E6B-5250416BC17D

            TimeSpan ts = (TimeSpan)value;
            // 默认精度为7
            string format = @"hh\:mm\:ss\.fffffff";
            if (DbTypeUtils.IsTime(dbType))
            {
                string pad = string.Empty;
                if (scale != null && scale.Value > 0) pad = "f".PadLeft(scale.Value > 7 ? 7 : scale.Value, 'f');
                if (!string.IsNullOrEmpty(pad)) format = string.Format(@"hh\:mm\:ss\.{0}", pad);
            }

            string m = ts.ToString(format);
            m = string.Format("{0} {1}", ts.Days, m);
            m = this.EscapeQuote(m, false, false);
            m = string.Format("TO_DSINTERVAL({0})", m);
            return m;
        }

        /// <summary>
        /// 获取 DatetTime 类型的 SQL 片断
        /// </summary>
        protected override string GetSqlValueByDateTime(object value, object dbType, int? scale)
        {
            DateTime date = (DateTime)value;

            // 默认精度6
            string format = "yyyy-MM-dd HH:mm:ss";
            bool isTimestamp = false;

            if (DbTypeUtils.IsDateTime(dbType) || DbTypeUtils.IsDateTime2(dbType))
            {
                format = "yyyy-MM-dd HH:mm:ss.ffffff";
                isTimestamp = true;
                string pad = string.Empty;
                if (scale != null && scale.Value > 0) pad = "f".PadLeft(scale.Value > 7 ? 7 : scale.Value, 'f');
                if (!string.IsNullOrEmpty(pad)) format = string.Format("yyyy-MM-dd HH:mm:ss.{0}", pad);
            }

            string result = this.EscapeQuote(((DateTime)value).ToString(format), false, false);
            result = isTimestamp
                ? string.Format("TO_TIMESTAMP({0},'yyyy-mm-dd hh24:mi:ss.ff')", result)
                : string.Format("TO_DATE({0},'yyyy-mm-dd hh24:mi:ss')", result);
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

            string result = string.Format("TO_TIMESTAMP_TZ('{0} {1}','yyyy-mm-dd hh24:mi:ss.ff tzh:tzm')", date, span);
            return result;
        }

        // 获取 Guid 类型的 SQL 片断
        protected override string GetSqlValueByGuid(object value)
        {
            string b = BitConverter.ToString(((Guid)value).ToByteArray()).Replace("-", "");
            return this.EscapeQuote(b, false, false);
        }

        /// <summary>
        /// 检查是否Unicode数据类型
        /// </summary>
        public override bool IsUnicode(object dbType)
        {
            return DbTypeUtils.IsUnicode(dbType);
        }


        // 官方数据类型和.NET数据类型映射关系
        // https://docs.oracle.com/database/121/ODPNT/featTypes.htm#ODPNT281
        // https://docs.oracle.com/en/database/oracle/oracle-database/12.2/sqlrf/Data-Types.html#GUID-7B72E154-677A-4342-A1EA-C74C1EA928E6
    }
}
