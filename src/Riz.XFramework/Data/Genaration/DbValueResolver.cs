using System;
using System.Data;
using System.Reflection;
using System.Collections;
using System.Linq.Expressions;
using System.Collections.Generic;

namespace Riz.XFramework.Data
{
    /// <summary>
    /// SQL字段值解析器
    /// </summary>
    public abstract class DbValueResolver
    {
        private string _escCharLeft;
        private string _escCharRight;
        private string _escCharQuote;
        private IDbQueryProvider _provider = null;

        /// <summary>
        /// 实例化 <see cref="DbValueResolver"/> 类的新实例
        /// </summary>
        /// <param name="provider">查询语义提供者</param>
        public DbValueResolver(IDbQueryProvider provider)
        {
            _provider = provider;
            _escCharLeft = _provider.QuotePrefix;
            _escCharRight = _provider.QuoteSuffix;
            _escCharQuote = _provider.SingleQuoteChar;
        }

        /// <summary>
        /// 生成 value 对应的 SQL 片断
        /// </summary>
        /// <param name="value">SQL值</param>
        /// <param name="context">解析SQL命令时的参数上下文</param>
        /// <returns></returns>
        public string GetSqlValue(object value, ITranslateContext context)
        {
            return this.GetSqlValue(value, context, (ColumnAttribute)null);
        }

        /// <summary>
        /// 生成 value 对应的 SQL 片断
        /// </summary>
        /// <param name="value">SQL值</param>
        /// <param name="token">解析SQL命令上下文</param>
        /// <param name="m">value 对应的成员</param>
        /// <returns></returns>
        public string GetSqlValue(object value, ITranslateContext token, MemberVisitedStack.VisitedMember m)
        {
            ColumnAttribute column = m != null ? TypeUtils.GetColumnAttribute(m.Member, m.ReflectedType) : null;
            return this.GetSqlValue(value, token, column);
        }

        /// <summary>
        /// 生成 value 对应的 SQL 片断
        /// <para>因为会使用到默认值，故此重载仅限 INSERT / UPDATE 时使用</para>
        /// </summary>
        /// <param name="value">SQL值</param>
        /// <param name="context">解析SQL命令时的参数上下文</param>
        /// <param name="column">数据列特性</param>
        /// <returns></returns>
        public string GetSqlValueWidthDefault(object value, ITranslateContext context, ColumnAttribute column)
        {
            if (value == null && column != null && column.Default != null)
            {
                // 插入值为空时使用默认值
                return column.Default.ToString();
            }
            if (value != null && column != null && column.Default != null)
            {
                // 值不为空，但只有.net默认值时也使用数据库默认值
                Type t = value.GetType();
                bool useDefault = false;
                if (t == typeof(DateTime) && (((DateTime)value) == DateTime.MinValue || ((DateTime)value).ToString("yyyy-MM-dd") == "0001-01-01")) useDefault = true;
                // 插入值为空时使用默认值
                if (useDefault) return column.Default.ToString();
            }

            return this.GetSqlValue(value, context, column);
        }

        /// <summary>
        /// 生成 value 对应的 SQL 片断
        /// </summary>
        /// <param name="value">SQL值</param>
        /// <param name="context">解析SQL命令时的参数上下文</param>
        /// <param name="column">数据列特性</param>
        /// <returns></returns>
        public string GetSqlValue(object value, ITranslateContext context, ColumnAttribute column)
        {
            return column == null
                ? this.GetSqlValue(value, context, null, null, null, null, null)
                : this.GetSqlValue(value, context, column.HasSetDbType ? column.DbType : null, column.HasSetSize ? new Nullable<int>(column.Size) : null,
                        column.HasSetPrecision ? new Nullable<int>(column.Precision) : null, column.HasSetScale ? new Nullable<int>(column.Scale) : null,
                        column.HasSetDirection ? new Nullable<ParameterDirection>(column.Direction) : null);
        }

        /// <summary>
        /// 生成 value 对应的 SQL 片断
        /// </summary>
        /// <param name="value">SQL值</param>
        /// <param name="context">解析SQL命令时的参数上下文</param>
        /// <param name="dbType">数据类型</param>
        /// <param name="size">长度</param>
        /// <param name="precision">精度</param>
        /// <param name="scale">小数位</param>
        /// <param name="direction">查询参数类型</param>
        /// <returns></returns>
        public string GetSqlValue(object value, ITranslateContext context,
            object dbType, int? size = null, int? precision = null, int? scale = null, ParameterDirection? direction = null)
        {
            // https://docs.microsoft.com/zh-cn/sql/t-sql/data-types/datetime2-transact-sql?view=sql-server-2017
            // 1.Date 3个字节,DateTime 8个字节 DateTime2 <=4 6个字节 其它8个字节，外加1个字节存储精度
            // 2.如果转换时没有指定数据类型的长度，则SQServer自动提供长度为30
            // 3.隐式转换优先级 https://docs.microsoft.com/zh-cn/sql/t-sql/data-types/data-type-precedence-transact-sql?view=sql-server-2017
            // 4.参数化查询只需要重写 CreateParameter

            if (value == null) return "NULL";
            else if (context != null && context.Parameters != null)
            {
                // 参数化 ##########
                if (!(value is string) && !(value is byte[]) && (value is IEnumerable))
                    return this.GetSqlValue(value as IEnumerable, context, dbType, size, precision, scale);
                else
                    return this.CreateParameter(value, context, dbType, size, precision, scale, direction).ParameterName;
            }
            else
            {
                // 非参数化 ##########

                Type type = value.GetType();
                // 枚举类型
                if (type.IsEnum)
                    return this.GetSqlValueOfEnum(value);
                // Guid 类型
                else if (value is Guid)
                    return this.GetSqlValueOfGuid(value);
                // 数据类型
                else if (TypeUtils.IsNumberType(type))
                    return this.GetSqlValueOfNumber(value);
                // byte[] 类型
                else if (value is byte[])
                    return this.GetSqlValueOfBytes(value);
                // 布尔类型
                else if (value is bool)
                    return this.GetSqlValueOfBoolean(value, dbType);
                // 字符类型
                else if (value is char || value is string)
                    return this.GetSqlValueOfString(value, dbType, size);
                // 时间类型
                else if (value is TimeSpan)
                    return this.GetSqlValueOfTime(value, dbType, scale);
                // 日期类型
                else if (value is DateTime)
                    return this.GetSqlValueOfDateTime(value, dbType, scale);
                // 日期类型（带时区）
                else if (value is DateTimeOffset)
                    return this.GetSqlValueOfDateTimeOffset(value, dbType, scale);
                // 集合类型
                else if (value is IEnumerable)
                    return this.GetSqlValue(value as IEnumerable, context, dbType, size, precision, scale);
                else
                    throw new NotSupportedException(string.Format("type {0} not supported serialize to string", type.FullName));

            }
        }

        /// <summary>
        /// 单引号转义
        /// </summary>
        /// <param name="s">源字符串</param>
        /// <param name="isUnicode">是否需要加N</param>
        /// <param name="isReplace">单引号替换成双引号</param>
        /// <param name="useQuote">前后两端是否加引号</param>
        /// <returns></returns>
        public virtual string EscapeQuote(string s, bool isUnicode = false, bool isReplace = false, bool useQuote = true)
        {
            string escCharQuoteDouble = string.Format("{0}{0}", _escCharQuote);
            if (isReplace) s = s.Replace(_escCharQuote, escCharQuoteDouble);
            return string.Format("{0}{1}{2}{1}", isUnicode ? "N" : string.Empty, useQuote ? _escCharQuote : string.Empty, s);
        }

        /// <summary>
        /// 获取枚举类型的 SQL 片断
        /// </summary>
        /// <param name="value">值</param>
        /// <returns></returns>
        protected virtual string GetSqlValueOfEnum(object value)
        {
            return Convert.ToUInt64(value).ToString();
        }

        /// <summary>
        /// 获取 Guid 类型的 SQL 片断
        /// </summary>
        /// <param name="value">值</param>
        /// <returns></returns>
        protected virtual string GetSqlValueOfGuid(object value)
        {
            return this.EscapeQuote(value.ToString(), false, false);
        }

        /// <summary>
        /// 获取数字类型的 SQL 片断
        /// </summary>
        /// <param name="value">值</param>
        /// <returns></returns>
        protected virtual string GetSqlValueOfNumber(object value)
        {
            var result = value.ToString();
            // 补足小数位
            if (TypeUtils.IsNumericType(value.GetType()) && !result.Contains("."))
            {
                result = string.Format("{0}.00", result);
            }
            return result;
        }

        /// <summary>
        /// 获取 byte[] 类型的 SQL 片断
        /// </summary>
        /// <param name="value">SQL值</param>
        protected virtual string GetSqlValueOfBytes(object value)
        {
            byte[] bytes = (byte[])value;
            string hex = Common.BytesToHex(bytes, true, true);
            return hex;
        }

        /// <summary>
        /// 获取 Boolean 类型的 SQL 片断
        /// </summary>
        /// <param name="value">SQL值</param>
        /// <param name="dbType">数据类型</param>
        protected virtual string GetSqlValueOfBoolean(object value, object dbType)
        {
            return ((bool)value) ? "1" : "0";
        }

        /// <summary>
        /// 获取 String 类型的 SQL 片断
        /// </summary>
        /// <param name="value">SQL值</param>
        /// <param name="dbType">数据类型</param>
        /// <param name="size">字符串长度</param>
        /// <returns></returns>
        protected abstract string GetSqlValueOfString(object value, object dbType, int? size = null);

        /// <summary>
        ///  获取 Time 类型的 SQL 片断
        /// </summary>
        /// <param name="value">SQL值</param>
        /// <param name="dbType">数据类型</param>
        /// <param name="scale">小数位</param>
        /// <returns></returns>
        protected abstract string GetSqlValueOfTime(object value, object dbType, int? scale = null);

        /// <summary>
        ///  获取 DateTime 类型的 SQL 片断
        /// </summary>
        /// <param name="value">SQL值</param>
        /// <param name="dbType">数据类型</param>
        /// <param name="scale">小数位</param>
        /// <returns></returns>
        protected abstract string GetSqlValueOfDateTime(object value, object dbType, int? scale = null);

        /// <summary>
        ///  获取 DateTimeOffset 类型的 SQL 片断
        /// </summary>
        /// <param name="value">SQL值</param>
        /// <param name="dbType">数据类型</param>
        /// <param name="scale">小数位</param>
        /// <returns></returns>
        protected abstract string GetSqlValueOfDateTimeOffset(object value, object dbType, int? scale = null);

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
        protected virtual IDbDataParameter CreateParameter(object value, ITranslateContext context,
            object dbType, int? size = null, int? precision = null, int? scale = null, ParameterDirection? direction = null)
        {
            string name = string.Format("{0}{1}{2}", _provider.ParameterPrefix, AppConst.PARAMETER_NAME_PRIFIX, context.Parameters.Count);
            var dbParameter = _provider.DbProvider.CreateParameter(name, value, null, size, precision, scale, direction);
            context.Parameters.Add(dbParameter);
            return dbParameter;
        }

        // 生成集合对应的 SQL 片断
        private string GetSqlValue(IEnumerable value, ITranslateContext context, object dbType, int? size, int? precision, int? scale, ParameterDirection? direction = null)
        {
            if (value == null) return "NULL";

            var iterator = value.GetEnumerator();
            List<string> sqlValues = new List<string>();
            while (iterator.MoveNext())
            {
                string text = this.GetSqlValue(iterator.Current, context, dbType, size, precision, scale, direction);
                sqlValues.Add(text);
            }

            // =>a,b,c
            string sql = string.Join(",", sqlValues);
            return sql;
        }
    }
}
