using System;
using System.Text;
using System.Data;
using System.Reflection;
using System.Collections;
using System.Linq.Expressions;
using System.Collections.Generic;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// SQL 语句建造器
    /// </summary>
    public abstract class SqlBuilderBase : ISqlBuilder
    {
        protected string _escCharLeft;
        protected string _escCharRight;
        protected string _escCharQuote;
        protected StringBuilder _innerBuilder = null;
        protected IDbQueryProvider _provider = null;
        private ParserToken _token = null;

        /// <summary>
        /// TAB 制表符
        /// </summary>
        public const string TAB = "    ";

        /// <summary>
        /// 获取或设置当前 <see cref="ISqlBuilder"/> 对象的长度。
        /// </summary>
        public int Length
        {
            get { return _innerBuilder.Length; }
            set { _innerBuilder.Length = value; }
        }

        /// <summary>
        /// 获取或设置此实例中指定字符位置处的字符。
        /// </summary>
        public char this[int index] { get { return _innerBuilder[index]; } }

        /// <summary>
        /// 缩进
        /// </summary>
        public int Indent { get; set; }

        /// <summary>
        /// 参数化
        /// </summary>
        public virtual bool Parameterized
        {
            get { return _token != null && _token.Parameters != null; }
        }

        /// <summary>
        /// 解析上下文参数
        /// </summary>
        public ParserToken Token
        {
            get { return _token; }
            set { _token = value; }
        }

        /// <summary>
        /// 实例化 <see cref="SqlBuilderBase"/> 类的新实例
        /// </summary>
        /// <param name="provider">查询语义提供者</param>
        /// <param name="token">解析上下文参数</param>
        public SqlBuilderBase(IDbQueryProvider provider, ParserToken token)
        {
            _provider = provider;
            _token = token;
            _innerBuilder = new StringBuilder(128);
            _escCharLeft = _provider.QuotePrefix;
            _escCharRight = _provider.QuoteSuffix;
            _escCharQuote = _provider.SingleQuoteChar;
        }

        /// <summary>
        /// 追加列名
        /// </summary>
        /// <param name="aliases">表别名</param>
        /// <param name="expression">列名表达式</param>
        /// <returns>返回解析到的表别名</returns>
        public string AppendMember(TableAliasCache aliases, Expression expression)
        {
            Expression exp = expression;
            LambdaExpression lambdaExpression = exp as LambdaExpression;
            if (lambdaExpression != null) exp = lambdaExpression.Body;

            if (expression.CanEvaluate())
            {
                ConstantExpression c = expression.Evaluate();
                string value = this.GetSqlValue(c.Value);
                _innerBuilder.Append(value);
                return value;
            }
            else
            {
                MemberExpression m = expression as MemberExpression;
                string alias = aliases == null ? null : aliases.GetTableAlias(m);
                this.AppendMember(alias, m.Member.Name);
                return alias;
            }
        }

        /// <summary>
        /// 追加列名
        /// </summary>
        /// <param name="expression">列名表达式</param>
        /// <param name="aliases">表别名</param>
        /// <returns>返回解析到的表别名</returns>
        public Expression AppendMember(Expression expression, TableAliasCache aliases)
        {
            this.AppendMember(aliases, expression);
            return expression;
        }

        /// <summary>
        /// 追加列名
        /// </summary>
        public ISqlBuilder AppendMember(string alias, string name)
        {
            if (!string.IsNullOrEmpty(alias))
            {
                _innerBuilder.Append(alias);
                _innerBuilder.Append('.');
            }
            return this.AppendMember(name);
        }

        /// <summary>
        /// 追加成员名称
        /// </summary>
        /// <param name="name">成员名称</param>
        /// <param name="quote">使用安全符号括起来，临时表不需要括</param>
        /// <returns></returns>
        public virtual ISqlBuilder AppendMember(string name, bool quote = true)
        {
            if (quote) _innerBuilder.Append(_escCharLeft);
            _innerBuilder.Append(name);
            if (quote) _innerBuilder.Append(_escCharRight);
            return this;
        }

        /// <summary>
        /// 在此实例的结尾追加 AS
        /// </summary>
        public virtual ISqlBuilder AppendAs(string name)
        {
            _innerBuilder.Append(" AS ");
            return this.AppendMember(name);
        }

        /// <summary>
        /// 在此实例的结尾追加指定字符串的副本。
        /// </summary>
        public ISqlBuilder Append(string value)
        {
            _innerBuilder.Append(value);
            return this;
        }

        /// <summary>
        /// 将字符串插入到此实例中的指定字符位置。
        /// </summary>
        public ISqlBuilder Insert(int index, string value)
        {
            _innerBuilder.Insert(index, value);
            return this;
        }

        /// <summary>
        /// 将字符串插入到此实例中的指定字符位置。
        /// </summary>
        public ISqlBuilder Insert(int index, object value)
        {
            _innerBuilder.Insert(index, value);
            return this;
        }

        /// <summary>
        /// 在此实例的结尾追加指定字符串的副本。
        /// </summary>
        public ISqlBuilder Append(int value)
        {
            _innerBuilder.Append(value);
            return this;
        }

        /// <summary>
        /// 在此实例的结尾追加指定字符串的副本。
        /// </summary>
        public ISqlBuilder Append(char value)
        {
            _innerBuilder.Append(value);
            return this;
        }

        /// <summary>
        /// 在此实例的结尾追加指定字符串的副本。
        /// </summary>
        public ISqlBuilder Append(object value)
        {
            if (value != null) _innerBuilder.Append(value);
            return this;
        }

        /// <summary>
        /// 在此实例的结尾追加指定字符串的副本。
        /// </summary>
        public ISqlBuilder Append(object value, MemberExpression m)
        {
            var sql = this.GetSqlValue(value, m);
            return this.Append(sql);
        }

        /// <summary>
        /// 在此实例的结尾追加指定字符串的副本。
        /// </summary>
        public ISqlBuilder Append(object value, System.Reflection.MemberInfo m, Type declareType)
        {
            var sql = this.GetSqlValue(value, m, declareType);
            return this.Append(sql);
        }

        /// <summary>
        /// 在此实例的结尾追加回车符
        /// </summary>
        public ISqlBuilder AppendNewLine()
        {
            _innerBuilder.Append(Environment.NewLine);
            if (this.Indent > 0)
            {
                for (int i = 1; i <= this.Indent; i++) this.AppendNewTab();
            }
            return this;
        }

        /// <summary>
        /// 在此实例的结尾追加回车符
        /// </summary>
        public ISqlBuilder AppendNewLine(string value)
        {
            _innerBuilder.AppendLine(value);
            return this;
        }

        /// <summary>
        /// 将通过处理复合格式字符串（包含零个或零个以上格式项）返回的字符串追加到此实例。每个格式项都替换为形参数组中相应实参的字符串表示形式。
        /// </summary>
        public ISqlBuilder AppendFormat(string value, params object[] args)
        {
            _innerBuilder.AppendFormat(value, args);
            return this;
        }

        /// <summary>
        /// 在此实例的结尾追加制表符
        /// </summary>
        public ISqlBuilder AppendNewTab()
        {
            _innerBuilder.Append(SqlBuilderBase.TAB);
            return this;
        }

        /// <summary>
        /// 将此实例中所有指定字符串的匹配项替换为其他指定字符串。
        /// </summary>
        public ISqlBuilder Replace(string oldValue, string newValue)
        {
            _innerBuilder.Replace(oldValue, newValue);
            return this;
        }

        /// <summary>
        /// 将此值实例转换成 <see cref="string"/>
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return _innerBuilder.ToString();
        }

        /// <summary>
        /// 生成 value 对应的 SQL 片断
        /// </summary>
        /// <param name="value">SQL值</param>
        /// <param name="node">成员访问表达式</param>
        /// <returns></returns>
        public string GetSqlValue(object value, MemberExpression node = null)
        {
            return this.GetSqlValue(value, node != null ? node.Member : null, node != null ? node.Expression.Type : null);
        }

        /// <summary>
        /// 生成 value 对应的 SQL 片断
        /// </summary>
        /// <param name="value">SQL值</param>
        /// <param name="member">成员</param>
        /// <param name="runtimeType">成员所在类型</param>
        /// <returns></returns>
        public string GetSqlValue(object value, MemberInfo member, Type runtimeType)
        {
            ColumnAttribute attribute = this.GetColumnAttribute(member, runtimeType);
            return this.GetSqlValue(value, attribute);
        }

        /// <summary>
        /// 生成 value 对应的 SQL 片断
        /// <para>因为会使用到默认值，故此重载仅限 INSERT / UPDATE 时使用</para>
        /// </summary>
        public string GetSqlValueWidthDefault(object value, ColumnAttribute attribute)
        {
            if (value == null && attribute != null && attribute.Default != null)
            {
                // 插入值为空时使用默认值
                return attribute.Default.ToString();
            }
            if (value != null && attribute != null && attribute.Default != null)
            {
                // 值不为空，但只有.net默认值时也使用数据库默认值
                Type t = value.GetType();
                bool useDefault = false;
                if (t == typeof(DateTime) && (((DateTime)value) == DateTime.MinValue || ((DateTime)value).ToString("yyyy-MM-dd") == "0001-01-01")) useDefault = true;
                //else if (t == typeof(bool) && ((bool)value) == false) useDefault = true;
                //else if (t == typeof(Guid) && ((Guid)value) == Guid.Empty) useDefault = true;
                //else if (t == typeof(byte) && ((byte)value) == 0) useDefault = true;
                //else if (t == typeof(decimal) && ((decimal)value) == 0) useDefault = true;
                //else if (t == typeof(double) && ((double)value) == 0) useDefault = true;
                //else if (t == typeof(short) && ((short)value) == 0) useDefault = true;
                //else if (t == typeof(int) && ((int)value) == 0) useDefault = true;
                //else if (t == typeof(long) && ((long)value) == 0) useDefault = true;
                //else if (t == typeof(sbyte) && ((sbyte)value) == 0) useDefault = true;
                //else if (t == typeof(float) && ((float)value) == 0) useDefault = true;
                //else if (t == typeof(ushort) && ((ushort)value) == 0) useDefault = true;
                //else if (t == typeof(uint) && ((uint)value) == 0) useDefault = true;
                //else if (t == typeof(ulong) && ((ulong)value) == 0) useDefault = true;

                // 插入值为空时使用默认值
                if (useDefault) return attribute.Default.ToString();
            }

            return this.GetSqlValue(value, attribute);
        }

        /// <summary>
        /// 生成 value 对应的 SQL 片断
        /// </summary>
        /// <param name="value">SQL值</param>
        /// <param name="column">数据列特性</param>
        /// <returns></returns>
        public string GetSqlValue(object value, ColumnAttribute column)
        {
            return column == null
                ? this.GetSqlValue(value, null, null, null, null, null)
                : this.GetSqlValue(value, column.HasSetDbType ? column.DbType : null, column.HasSetSize ? new Nullable<int>(column.Size) : null,
                        column.HasSetPrecision ? new Nullable<int>(column.Precision) : null, column.HasSetScale ? new Nullable<int>(column.Scale) : null,
                        column.HasSetDirection ? new Nullable<ParameterDirection>(column.Direction) : null);
        }

        /// <summary>
        /// 生成 value 对应的 SQL 片断
        /// </summary>
        public string GetSqlValue(object value, object dbType, int? size = null, int? precision = null, int? scale = null, ParameterDirection? direction = null)
        {
            // https://docs.microsoft.com/zh-cn/sql/t-sql/data-types/datetime2-transact-sql?view=sql-server-2017
            // 1.Date 3个字节,DateTime 8个字节 DateTime2 <=4 6个字节 其它8个字节，外加1个字节存储精度
            // 2.如果转换时没有指定数据类型的长度，则SQServer自动提供长度为30
            // 3.隐式转换优先级 https://docs.microsoft.com/zh-cn/sql/t-sql/data-types/data-type-precedence-transact-sql?view=sql-server-2017
            // 4.参数化查询只需要重写 CreateParameter

            if (value == null) return "NULL";

            Type type = value.GetType();
            if (this.Parameterized)
            {
                // 参数化 ##########
                if (!(value is string) && !(value is byte[]) && (value is IEnumerable))
                    return this.GetSqlValue(value as IEnumerable, dbType, size, precision, scale);
                else
                    return this.AddParameter(value, dbType, size, precision, scale, direction).ParameterName;
            }
            else
            {
                // 非参数化 ##########

                // Guid 类型
                if (value is Guid)
                    return this.GetSqlValueByGuid(value);
                // 布尔类型
                else if (value is bool)
                    return this.GetSqlValueByBoolean(value, dbType);
                // 字符类型
                else if (value is char || value is string)
                    return GetSqlValueByString(value, dbType, size);
                // 时间类型
                else if (value is TimeSpan)
                    return this.GetSqlValueByTime(value, dbType, precision);
                // 日期类型
                else if (value is DateTime)
                    return this.GetSqlValueByDateTime(value, dbType, precision);
                // 日期类型（带时区）
                else if (value is DateTimeOffset)
                    return this.GetSqlValueByDateTimeOffset(value, dbType, precision);
                //// xml类型
                //else if (value is SqlXml)
                //    return this.GetSqlValueByXml(value, dbType);
                // 集合类型
                else if (value is IEnumerable)
                    return this.GetSqlValue(value as IEnumerable, dbType, size, precision, scale);
                // 其它 <int byte long etc.>
                else
                    return this.GetSqlValueByOther(value, dbType, size, precision, scale, direction);
            }
        }

        /// <summary>
        /// 单引号转义
        /// </summary>
        /// <param name="s">源字符串</param>
        /// <param name="unicode">是否需要加N</param>
        /// <param name="replace">单引号替换成双引号</param>
        /// <param name="quote">前后两端是否加引号</param>
        /// <returns></returns>
        public virtual string EscapeQuote(string s, bool unicode = false, bool replace = false, bool quote = true)
        {
            string escCharQuoteDouble = string.Format("{0}{0}", _escCharQuote);
            if (replace) s = s.Replace(_escCharQuote, escCharQuoteDouble);
            return string.Format("{0}{1}{2}{1}", unicode ? "N" : string.Empty, quote ? _escCharQuote : string.Empty, s);
        }

        /// <summary>
        /// 获取指定成员的 <see cref="ColumnAttribute"/>
        /// </summary>
        public virtual ColumnAttribute GetColumnAttribute(MemberInfo member, Type runtimeType)
        {
            Type dataType = TypeUtils.GetDataType(member);
            if (dataType == null) return null;

            ColumnAttribute column = null;
            Type type = runtimeType != null ? runtimeType : (member.ReflectedType != null ? member.ReflectedType : member.DeclaringType);
            if (type != null && !TypeUtils.IsAnonymousType(type))
            {
                TypeRuntimeInfo typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo(type);
                var invoker = typeRuntime.GetInvoker(member.Name);
                if (invoker != null) column = invoker.Column;
            }

            return column;
        }

        /// <summary>
        /// 增加一个参数
        /// </summary>
        protected virtual IDbDataParameter AddParameter(object value, object dbType, int? size = null, int? precision = null, int? scale = null, ParameterDirection? direction = null)
        {
            string parameterName = string.Format("{0}p{1}", _provider.ParameterPrefix, this.Token.Parameters.Count);

            IDbDataParameter parameter = _provider.DbProviderFactory.CreateParameter();
            parameter.ParameterName = parameterName;
            parameter.Value = value;

            if (size != null && (size.Value > 0 || size.Value == -1)) parameter.Size = size.Value;
            if (precision != null && precision.Value > 0) parameter.Precision = (byte)precision.Value;
            if (scale != null && scale.Value > 0) parameter.Scale = (byte)scale.Value;
            if (direction != null) parameter.Direction = direction.Value;
            else parameter.Direction = ParameterDirection.Input;

            // 补充字符串的长度
            if (value != null && value.GetType() == typeof(string) && size == null)
            {
                string s = value.ToString();
                if (dbType == null) parameter.DbType = DbType.String;
                if (parameter.DbType == DbType.String || parameter.DbType == DbType.StringFixedLength ||
                    parameter.DbType == DbType.AnsiString || parameter.DbType == DbType.AnsiStringFixedLength)
                {
                    if (s.Length <= 256) parameter.Size = 256;
                    else if (s.Length <= 512) parameter.Size = 512;
                    else if (s.Length <= 1024) parameter.Size = 1024;
                    else if (s.Length <= 4000) parameter.Size = 4000;
                    else if (s.Length <= 8000) parameter.Size = 8000;
                    else parameter.Size = -1;
                }
            }

            this.Token.Parameters.Add(parameter);
            return parameter;
        }

        /// <summary>
        /// 获取 Time 类型的 SQL 片断
        /// </summary>
        protected abstract string GetSqlValueByTime(object value, object dbType, int? precision = null);

        /// <summary>
        /// 获取 DatetTime 类型的 SQL 片断
        /// </summary>
        protected abstract string GetSqlValueByDateTime(object value, object dbType, int? precision = null);

        /// <summary>
        /// 获取 DateTimeOffset 类型的 SQL 片断
        /// </summary>
        protected abstract string GetSqlValueByDateTimeOffset(object value, object dbType, int? precision = null);

        /// <summary>
        /// 获取 String 类型的 SQL 片断
        /// </summary>
        protected abstract string GetSqlValueByString(object value, object dbType, int? size = null);

        /// <summary>
        /// 获取 Boolean 类型的 SQL 片断
        /// </summary>
        protected virtual string GetSqlValueByBoolean(object value, object dbType)
        {
            return ((bool)value) ? "1" : "0";
        }

        /// <summary>
        /// 获取 Guid 类型的 SQL 片断
        /// </summary>
        protected virtual string GetSqlValueByGuid(object value)
        {
            return this.EscapeQuote(value.ToString(), false, false);
        }

        ///// <summary>
        ///// 获取XML类型的 SQL 片断
        ///// </summary>
        //protected virtual string GetSqlValueByXml(object value, object dbType, int? size = null, int? precision = null, int? scale = null, ParameterDirection? direction = null)
        //{
        //    return this.EscapeQuote(((SqlXml)value).Value, false, true);
        //}

        /// <summary>
        /// 获取其它类型的 SQL 片断
        /// </summary>
        protected virtual string GetSqlValueByOther(object value, object dbType, int? size = null, int? precision = null, int? scale = null, ParameterDirection? direction = null)
        {
            if (value is byte[]) throw new NotSupportedException("System.Byte[] does not support serialization into strings.");

            if (TypeUtils.IsPrimitiveType(value.GetType()))
                return value.ToString();
            else
                return this.EscapeQuote(value.ToString(), false, false);
        }

        // 生成集合对应的 SQL 片断
        private string GetSqlValue(IEnumerable value, object dbType, int? size, int? precision, int? scale, ParameterDirection? direction = null)
        {
            if (value == null) return "NULL";

            var iterator = value.GetEnumerator();
            List<string> sqlValues = new List<string>();
            while (iterator.MoveNext())
            {
                string text = this.GetSqlValue(iterator.Current, dbType, size, precision, scale, direction);
                sqlValues.Add(text);
            }

            // =>a,b,c
            string sql = string.Join(",", sqlValues);
            return sql;
        }
    }
}
