
using System;
using System.Data;
using System.Collections.Generic;
using Npgsql;
using NpgsqlTypes;
using System.Net;

namespace TZM.XFramework.Data.SqlClient
{
    /// <summary>
    /// Porstgre SQL字段值生成器
    /// </summary>
    public class NpgDbValue : DbValue
    {
        /// <summary>
        /// SQL字段值生成器实例
        /// </summary>
        public static NpgDbValue Instance = new NpgDbValue();

        /// <summary>
        /// 实例化 <see cref="NpgDbValue"/> 类的新实例
        /// </summary>
        protected NpgDbValue()
            : base(NpgDbQueryProvider.Instance)
        {

        }

        // 增加一个参数
        protected override IDbDataParameter AddParameter(object value, ResolveToken token, 
            object dbType, int? size = null, int? precision = null, int? scale = null, ParameterDirection? direction = null)
        {

#if !netcore

            if (value is TimeSpan)
            {
                // 如果不是 netcore，需要将timespan转为datetime类型才可以保存
                value = new DateTime(((TimeSpan)value).Ticks);
                dbType = NpgsqlDbType.Timestamp;
            }

#endif

            // 补充 DbType
            NpgsqlParameter parameter = (NpgsqlParameter)base.AddParameter(value, token, dbType, size, precision, scale, direction);
            parameter.DbType(dbType);
            return parameter;
        }

        // 获取 byte[] 类型的 SQL 片断
        protected override string GetSqlValueByBytes(object value)
        {
            byte[] bytes = (byte[])value;
            string hex = XfwCommon.BytesToHex(bytes, false, true);
            hex = string.Format(@"'\x{0}'", hex);
            return hex;
        }

        // 获取 String 类型的 SQL 片断
        protected override string GetSqlValueByString(object value, object dbType, int? size = null)
        {
            string result = this.EscapeQuote(value.ToString(), false, true);
            return result;
        }

        // 获取 Time 类型的 SQL 片断
        protected override string GetSqlValueByTime(object value, object dbType, int? scale)
        {
            // 默认精度6
            string format = @"hh\:mm\:ss\.ffffff";
            if (DbTypeUtils.IsTime(dbType))
            {
                string s = string.Empty;
                if (scale != null && scale.Value > 0) s = string.Empty.PadLeft(scale.Value > 6 ? 6 : scale.Value, 'f');
                if (!string.IsNullOrEmpty(s)) format = string.Format(@"hh\:mm\:ss\.{0}", s);
            }

            string date = ((TimeSpan)value).ToString(format);
            string result = string.Format("'{0}'::TIME", date);
            return result;
        }

        // 获取 DatetTime 类型的 SQL 片断
        protected override string GetSqlValueByDateTime(object value, object dbType, int? scale)
        {
            // 默认精度6
            string format = "yyyy-MM-dd HH:mm:ss.ffffff";
            if (DbTypeUtils.IsDate(dbType)) format = "yyyy-MM-dd";
            else if (DbTypeUtils.IsDateTime(dbType) || DbTypeUtils.IsDateTime2(dbType))
            {
                string s = string.Empty;
                if (scale != null && scale.Value > 0) s = string.Empty.PadLeft(scale.Value > 6 ? 6 : scale.Value, 'f');
                if (!string.IsNullOrEmpty(s)) format = string.Format("yyyy-MM-dd HH:mm:ss.{0}", s);
            }

            string date = ((DateTime)value).ToString(format);
            string result = string.Format("'{0}'::TIMESTAMP", date);
            return result;
        }

        // 获取 DateTimeOffset 类型的 SQL 片断
        protected override string GetSqlValueByDateTimeOffset(object value, object dbType, int? scale)
        {
            // 默认精度6
            string format = "yyyy-MM-dd HH:mm:ss.ffffff";
            if (DbTypeUtils.IsDateTimeOffset(dbType))
            {
                string s = string.Empty;
                if (scale != null && scale.Value > 0) s = string.Empty.PadLeft(scale.Value > 6 ? 6 : scale.Value, 'f');
                if (!string.IsNullOrEmpty(s)) format = string.Format("yyyy-MM-dd HH:mm:ss.{0}", s);
            }

            string myDateTime = ((DateTimeOffset)value).DateTime.ToString(format);
            string myOffset = ((DateTimeOffset)value).Offset.ToString(@"hh\:mm");
            myOffset = string.Format("{0}{1}", ((DateTimeOffset)value).Offset < TimeSpan.Zero ? '-' : '+', myOffset);

            string result = string.Format("'{0}{1}'::TIMESTAMPTZ ", myDateTime, myOffset);
            return result;

            // Npgsql 的显示都是以本地时区显示的？###
        }

        // 获取 Boolean 类型的 SQL 片断
        protected override string GetSqlValueByBoolean(object value, object dbType)
        {
            return ((bool)value) ? "TRUE" : "FALSE";
        }

        /// <summary>
        /// 检查是否Unicode数据类型
        /// </summary>
        public override bool IsUnicode(object dbType)
        {
            return DbTypeUtils.IsUnicode(dbType);
        }

        // https://www.postgresql.org/docs/
        // http://www.npgsql.org/doc/index.html
        // http://shouce.jb51.net/postgresql/ postgre 文档
        // postgresql中没有NCHAR VARCHAR2 NVARCHAR2数据类型。
        // https://blog.csdn.net/pg_hgdb/article/details/79018366
    }
}
