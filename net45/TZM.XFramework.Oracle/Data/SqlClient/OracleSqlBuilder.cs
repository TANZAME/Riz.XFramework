
using System;
using System.Data;
using System.Collections.Generic;
using Oracle.ManagedDataAccess.Client;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// SQL 语句构造器
    /// </summary>
    public class OracleSqlBuilder : SqlBuilderBase
    {
        // https://docs.oracle.com/en/database/oracle/oracle-database/12.2/sqlrf/Data-Types.html#GUID-7B72E154-677A-4342-A1EA-C74C1EA928E6

        /// <summary>
        /// 是否最外层查询
        /// oracle 只有最外层才需要区分大小
        /// </summary>
        public bool IsOuter { get; set; }

        /// <summary>
        /// 实例化 <see cref="OracleSqlBuilder"/> 类的新实例
        /// </summary>
        /// <param name="provider">提供者</param>
        /// <param name="token">参数列表，NULL 或者 Parameters=NULL 时表示不使用参数化</param>
        public OracleSqlBuilder(IDbQueryProvider provider, ParserToken token)
            : base(provider, token)
        {

        }

        /// <summary>
        /// 增加一个参数
        /// </summary>
        protected override IDbDataParameter AddParameter(object value, object dbType, int? size = null, int? precision = null, int? scale = null, ParameterDirection? direction = null)
        {
            if (value is bool ) value = ((bool)value) ? 1 : 0;
            else if (value is Guid)
            {
                value = ((Guid)value).ToByteArray();
                dbType = OracleDbType.Raw;
            }
            //else if (value is DateTimeOffset)
            //{
            //    value = new Oracle.ManagedDataAccess.Types.OracleTimeStampTZ(((DateTimeOffset)value).LocalDateTime, TimeZone.CurrentTimeZone.StandardName);
            //}

            OracleParameter parameter = (OracleParameter)base.AddParameter(value, dbType, size, precision, scale, direction);
            // 补充 DbType
            parameter.SetDbType(dbType);
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
        protected override string GetSqlValueByTime(object value, object dbType, int? precision)
        {
            // https://docs.oracle.com/en/database/oracle/oracle-database/12.2/nlspg/datetime-data-types-and-time-zone-support.html#GUID-FD8C41B7-8CDC-4D02-8E6B-5250416BC17D
            // throw new NotImplementedException("Oracle [Time] must map to .NET [DateTime] type.");
            DbTypeUtils.IsTime(dbType);
            return null;
        }

        /// <summary>
        /// 获取 DatetTime 类型的 SQL 片断
        /// </summary>
        protected override string GetSqlValueByDateTime(object value, object dbType, int? precision)
        {
            // 默认精度6
            string format = "yyyy-MM-dd HH:mm:ss";
            bool isTimestamp = false;

            if (DbTypeUtils.IsDateTime(dbType) || DbTypeUtils.IsDateTime2(dbType))
            {
                format = "yyyy-MM-dd HH:mm:ss.ffffff";
                isTimestamp = true;
                string pad = string.Empty;
                if (precision != null && precision.Value > 0) pad = "f".PadLeft(precision.Value > 7 ? 7 : precision.Value, 'f');
                if (!string.IsNullOrEmpty(pad)) format = string.Format("yyyy-MM-dd HH:mm:ss.{0}", pad);
            }

            string result = this.EscapeQuote(((DateTime)value).ToString(format), false, false);
            result = isTimestamp
                ? string.Format("TO_TIMESTAMP({0},'yyyy-mm-dd hh24:mi:ss.ff')", result)
                : string.Format("TO_DATE({0},'yyyy-mm-dd hh24:mi:ss')", result);
            return result;
        }

        // 获取 DateTimeOffset 类型的 SQL 片断
        protected override string GetSqlValueByDateTimeOffset(object value, object dbType, int? precision)
        {
            DbTypeUtils.IsDateTimeOffset(dbType);
            return null;
            //string format = "yyyy-MM-dd HH:mm:ss.ffffff";
            //_OracleDbType dbTypeInfo = _OracleDbType.Create(dbType);

            //if (dbTypeInfo != null && dbTypeInfo.IsDateTimeOffset)
            //{
            //    string pad = string.Empty;
            //    if (precision != null && precision.Value > 0) pad = "f".PadLeft(precision.Value > 7 ? 7 : precision.Value, 'f');
            //    if (!string.IsNullOrEmpty(pad)) format = string.Format("yyyy-MM-dd HH:mm:ss.{0}", pad);
            //}

            //string date = ((DateTimeOffset)value).DateTime.ToString(format);
            //string span = ((DateTimeOffset)value).Offset.ToString(@"hh\:mm");
            //span = string.Format("{0}{1}", ((DateTimeOffset)value).Offset.Hours >= 0 ? '+' : '-', span);

            //string result = string.Format("(TIMESTAMP '{0}{1}')", date, span);
            //return result;

            //c#中向oracle中操作timestamp 必须间接的转timestamp 进行操作：
            //SELECT * FROM T_TABLE WHERE createDate Between TO_TIMESTAMP('2015-09-15','yyyy-mm-dd hh24:mi:ss') AND TO_TIMESTAMP('2015-09-25','yyyy-mm-dd hh24:mi:ss') 
            ///或者:
            //SELECT * FROM T_TABLE WHERE createDate Between TO_TIMESTAMP_TZ('2013-12-09','YYYY-MM-DD HH24:MI:SS.FF TZH:TZM') AND TO_TIMESTAMP_TZ('2015-12-09','YYYY-MM-DD HH24:MI:SS.FF TZH:TZM') 
        }

        // 获取 Guid 类型的 SQL 片断
        protected override string GetSqlValueByGuid(object value)
        {
            string b = BitConverter.ToString(((Guid)value).ToByteArray()).Replace("-", "");
            return this.EscapeQuote(b, false, false);
        }

        /// <summary>
        /// 追加成员名称
        /// </summary>
        /// <param name="name">成员名称</param>
        /// <param name="quote">使用安全符号括起来，临时表不需要括</param>
        /// <returns></returns>
        public override ISqlBuilder AppendMember(string name, bool quote)
        {
            _innerBuilder.Append(name);
            return this;
        }

        /// <summary>
        /// 在此实例的结尾追加 AS
        /// </summary>
        public override ISqlBuilder AppendAs(string name)
        {
            _innerBuilder.Append(" AS ");
            if (this.IsOuter) _innerBuilder.Append(_escCharLeft);
            _innerBuilder.Append(name);
            if (this.IsOuter) _innerBuilder.Append(_escCharRight);
            return this;
        }
    }
}
