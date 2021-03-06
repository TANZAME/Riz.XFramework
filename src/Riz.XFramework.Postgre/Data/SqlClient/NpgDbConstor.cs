﻿
using System;
using System.Data;
using System.Collections.Generic;
using Npgsql;
using NpgsqlTypes;
using System.Net;

namespace Riz.XFramework.Data.SqlClient
{
    /// <summary>
    /// Porstgre 常量值转SQL表达式解析器
    /// </summary>
    internal class NpgDbConstor : DbConstor
    {
        /// <summary>
        /// 常量值转SQL表达式解析器实例
        /// </summary>
        public static NpgDbConstor Instance = new NpgDbConstor();

        /// <summary>
        /// 实例化 <see cref="NpgDbConstor"/> 类的新实例
        /// </summary>
        protected NpgDbConstor()
            : base(NpgDbQueryProvider.Instance)
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

#if !netcore

            if (value is TimeSpan)
            {
                // 如果不是 netcore，需要将timespan转为datetime类型才可以保存
                value = new DateTime(((TimeSpan)value).Ticks);
                dbType = NpgsqlDbType.Timestamp;
            }

#endif

            // 补充 DbType
            NpgsqlParameter parameter = (NpgsqlParameter)base.CreateParameter(value, context, dbType, size, precision, scale, direction);
            parameter.DbType(dbType);
            return parameter;
        }

        /// <summary>
        /// 获取 byte[] 类型的 SQL 片断
        /// </summary>
        /// <param name="value">SQL值</param>
        protected override string GetSqlValueOfBytes(object value)
        {
            byte[] bytes = (byte[])value;
            string hex = Common.BytesToHex(bytes, false, true);
            hex = string.Format(@"'\x{0}'", hex);
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
            // 默认精度6
            string format = @"hh\:mm\:ss\.ffffff";
            if (NpgUtils.IsTime(dbType))
            {
                string s = string.Empty;
                if (scale != null && scale.Value > 0) s = string.Empty.PadLeft(scale.Value > 6 ? 6 : scale.Value, 'f');
                if (!string.IsNullOrEmpty(s)) format = string.Format(@"hh\:mm\:ss\.{0}", s);
            }

            string date = ((TimeSpan)value).ToString(format);
            string result = string.Format("'{0}'::TIME", date);
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
            // 默认精度6
            string format = "yyyy-MM-dd HH:mm:ss.ffffff";
            if (NpgUtils.IsDate(dbType)) format = "yyyy-MM-dd";
            else if (NpgUtils.IsDateTime(dbType) || NpgUtils.IsDateTime2(dbType))
            {
                string s = string.Empty;
                if (scale != null && scale.Value > 0) s = string.Empty.PadLeft(scale.Value > 6 ? 6 : scale.Value, 'f');
                if (!string.IsNullOrEmpty(s)) format = string.Format("yyyy-MM-dd HH:mm:ss.{0}", s);
            }

            string date = ((DateTime)value).ToString(format);
            string result = string.Format("'{0}'::TIMESTAMP", date);
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
            // 默认精度6
            string format = "yyyy-MM-dd HH:mm:ss.ffffff";
            if (NpgUtils.IsDateTimeOffset(dbType))
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

        /// <summary>
        /// 获取 Boolean 类型的 SQL 片断
        /// </summary>
        /// <param name="value">SQL值</param>
        /// <param name="dbType">数据类型</param>
        protected override string GetSqlValueOfBoolean(object value, object dbType)
        {
            return ((bool)value) ? "TRUE" : "FALSE";
        }

        // https://www.postgresql.org/docs/
        // http://www.npgsql.org/doc/index.html
        // http://shouce.jb51.net/postgresql/ postgre 文档
        // postgresql中没有NCHAR VARCHAR2 NVARCHAR2数据类型。
        // https://blog.csdn.net/pg_hgdb/article/details/79018366
    }
}
