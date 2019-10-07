
using System;
using System.Data;
using System.Collections.Generic;
using Npgsql;
using NpgsqlTypes;
using System.Net;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// SQL 语句构造器
    /// </summary>
    public class NpgSqlBuilder : TextBuilder
    {
        /// <summary>
        /// 是否最外层查询
        /// pgsql 只有最外层才需要区分大小
        /// </summary>
        public bool IsOuter { get; set; }

        /// <summary>
        /// 实例化 <see cref="NpgSqlBuilder"/> 类的新实例
        /// </summary>
        /// <param name="provider">提供者</param>
        /// <param name="token">参数列表，NULL 或者 Parameters=NULL 时表示不使用参数化</param>
        public NpgSqlBuilder(IDbQueryProvider provider, ResolveToken token)
            : base(provider, token)
        {

        }

        protected override IDbDataParameter AddParameter(object value, object dbType, int? size = null, int? precision = null, int? scale = null, ParameterDirection? direction = null)
        {
#if !netcore

            if (value is TimeSpan)
            {
                value = new DateTime(((TimeSpan)value).Ticks);
                dbType = NpgsqlDbType.Timestamp;
            }

#endif

            NpgsqlParameter parameter = (NpgsqlParameter)base.AddParameter(value, dbType, size, precision, scale, direction);
            // 补充 DbType
            parameter.PrepareDbType(dbType);
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
            // 默认精度6
            string format = @"hh\:mm\:ss\.ffffff";
            if (DbTypeUtils.IsTime(dbType))
            {
                string pad = string.Empty;
                if (precision != null && precision.Value > 0) pad = "f".PadLeft(precision.Value > 6 ? 6 : precision.Value, 'f');
                if (!string.IsNullOrEmpty(pad)) format = string.Format(@"hh\:mm\:ss\.{0}", pad);
            }

            string result = this.EscapeQuote(((TimeSpan)value).ToString(format), false, false);
            return result;
        }

        // 获取 DatetTime 类型的 SQL 片断
        protected override string GetSqlValueByDateTime(object value, object dbType, int? precision)
        {
            // 默认精度6
            string format = "yyyy-MM-dd HH:mm:ss.ffffff";
            if (DbTypeUtils.IsDate(dbType)) format = "yyyy-MM-dd";
            else if (DbTypeUtils.IsDateTime(dbType) || DbTypeUtils.IsDateTime2(dbType))
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
            if (DbTypeUtils.IsDateTimeOffset(dbType))
            {
                string pad = string.Empty;
                if (precision != null && precision.Value > 0) pad = "f".PadLeft(precision.Value > 7 ? 7 : precision.Value, 'f');
                if (!string.IsNullOrEmpty(pad)) format = string.Format("yyyy-MM-dd HH:mm:ss.{0}", pad);
            }

            string date = ((DateTimeOffset)value).DateTime.ToString(format);
            string span = ((DateTimeOffset)value).Offset.ToString(@"hh");
            span = string.Format("{0}{1}", ((DateTimeOffset)value).Offset < TimeSpan.Zero ? '-' : '+', span);

            string result = string.Format("(TIMESTAMP WITH TIME ZONE '{0}{1}')", date, span);
            return result;

            // Npgsql 的显示都是以本地时区显示的？###
        }

        // 获取 Boolean 类型的 SQL 片断
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
        public override ITextBuilder AppendMember(string name, bool quote)
        {
            _innerBuilder.Append(name);
            return this;
        }

        /// <summary>
        /// 在此实例的结尾追加 AS
        /// </summary>
        public override ITextBuilder AppendAs(string name)
        {
            _innerBuilder.Append(" AS ");
            if (this.IsOuter) _innerBuilder.Append(_escCharLeft);
            _innerBuilder.Append(name);
            if (this.IsOuter) _innerBuilder.Append(_escCharRight);
            return this;
        }

        // http://www.npgsql.org/doc/index.html
        // http://shouce.jb51.net/postgresql/ postgre 文档
        // postgresql中没有NCHAR VARCHAR2 NVARCHAR2数据类型。
        // https://blog.csdn.net/pg_hgdb/article/details/79018366
    }
}
