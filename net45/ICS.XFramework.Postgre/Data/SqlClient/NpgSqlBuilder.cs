
using System;
using System.Data;
using System.Collections.Generic;
using Npgsql;
using NpgsqlTypes;
using System.Net;

namespace ICS.XFramework.Data
{
    /// <summary>
    /// SQL 语句构造器
    /// </summary>
    public class NpgSqlBuilder : SqlBuilderBase
    {
        // postgresql中没有NCHAR VARCHAR2 NVARCHAR2数据类型。
        // https://blog.csdn.net/pg_hgdb/article/details/79018366

        /// <summary>
        /// 是否最外层查询
        /// pgsql 只有最外层才需要区分大小
        /// </summary>
        public bool IsOuter { get; set; }

        /// <summary>
        /// 实例化 <see cref="NpgSqlBuilder"/> 类的新实例
        /// </summary>
        /// <param name="provider">提供者</param>
        /// <param name="parameters">参数化</param>
        public NpgSqlBuilder(IDbQueryProvider provider, List<IDbDataParameter> parameters = null)
            : base(provider, parameters)
        {

        }

        protected override IDbDataParameter AddParameter(object value, object dbType, int? size = null, int? precision = null, int? scale = null, ParameterDirection? direction = null)
        {
            NpgsqlParameter parameter = (NpgsqlParameter)base.AddParameter(value, dbType, size, precision, scale, direction);

            // 补充 DbType
            NpgDbTypeInfo dbTypeInfo = NpgDbTypeInfo.Create(dbType);
            if (dbTypeInfo != null && dbTypeInfo.DbType != null) parameter.DbType = dbTypeInfo.DbType.Value;
            else if (dbTypeInfo != null && dbTypeInfo.SqlDbType != null) parameter.NpgsqlDbType = dbTypeInfo.SqlDbType.Value;

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
#if netcore
            // 默认精度6
            string format = @"hh\:mm\:ss\.ffffff";
            NpgDbTypeInfo dbTypeInfo = NpgDbTypeInfo.Create(dbType);

            if (dbTypeInfo != null && dbTypeInfo.IsTime)
            {
                string pad = string.Empty;
                if (precision != null && precision.Value > 0) pad = "f".PadLeft(precision.Value > 6 ? 6 : precision.Value, 'f');
                if (!string.IsNullOrEmpty(pad)) format = string.Format(@"hh\:mm\:ss\.{0}", pad);
            }

            string result = this.EscapeQuote(((TimeSpan)value).ToString(format), false, false);
            return result;
#endif

#if !netcore
            throw new NotSupportedException("Oracle does not support Time type.");
#endif
        }

        // 获取 DatetTime 类型的 SQL 片断
        protected override string GetSqlValueByDateTime(object value, object dbType, int? precision)
        {
            // 默认精度6
            string format = "yyyy-MM-dd HH:mm:ss.ffffff";
            NpgDbTypeInfo dbTypeInfo = NpgDbTypeInfo.Create(dbType);

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
        protected override string GetSqlValueByDateTimeOffset(object value, object dbType, int? precision)
        {
            // 默认精度6
            string format = "yyyy-MM-dd HH:mm:ss.ffffff";
            NpgDbTypeInfo dbTypeInfo = NpgDbTypeInfo.Create(dbType);

            if (dbTypeInfo != null && dbTypeInfo.IsDateTimeOffset)
            {
                string pad = string.Empty;
                if (precision != null && precision.Value > 0) pad = "f".PadLeft(precision.Value > 7 ? 7 : precision.Value, 'f');
                if (!string.IsNullOrEmpty(pad)) format = string.Format("yyyy-MM-dd HH:mm:ss.{0}", pad);
            }

            string date = ((DateTimeOffset)value).DateTime.ToString(format);
            string span = ((DateTimeOffset)value).Offset.ToString(@"hh");
            span = string.Format("{0}{1}", ((DateTimeOffset)value).Offset.Hours >= 0 ? '+' : '-', span);

            string result = string.Format("(TIMESTAMPTZ '{0}{1}')", date, span);
            return result;

            // Npgsql 的显示都是以本地时区显示的？###
        }

        /// <summary>
        /// 获取 Boolean 类型的 SQL 片断
        /// </summary>
        /// <returns></returns>
        protected override string GetSqlValueByBoolean(object value, object dbType)
        {
            return ((bool)value) ? "TRUE" : "FALSE";
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
