
using System;
using System.Text;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// SQL 语句建造器
    /// </summary>
    public abstract class SqlBuilder : ISqlBuilder
    {
        private string _escCharLeft;
        private string _escCharRight;
        private string _escCharQuote;
        private StringBuilder _innerBuilder = null;
        private IDbQueryProvider _provider = null;
        private ResolveToken _token = null;
        private DbValue _dbValue = null;

        /// <summary>
        /// TAB 制表符
        /// </summary>
        public const string TAB = "    ";

        /// <summary>
        /// 内部可变字符串
        /// </summary>
        protected StringBuilder InnerBuilder { get { return _innerBuilder; } }

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
        public ResolveToken Token
        {
            get { return _token; }
        }

        /// <summary>
        /// 实例化 <see cref="SqlBuilder"/> 类的新实例
        /// </summary>
        /// <param name="provider">查询语义提供者</param>
        /// <param name="token">解析上下文参数</param>
        public SqlBuilder(IDbQueryProvider provider, ResolveToken token)
        {
            _provider = provider;
            _dbValue = provider.DbValue;
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
        /// <param name="node">列名表达式</param>
        /// <returns>返回解析到的表别名</returns>
        public string AppendMember(TableAliasCache aliases, Expression node)
        {
            Expression expression = node;
            LambdaExpression lambdaExpression = expression as LambdaExpression;
            if (lambdaExpression != null) expression = lambdaExpression.Body;

            if (node.CanEvaluate())
            {
                ConstantExpression c = node.Evaluate();
                string value = _dbValue.GetSqlValue(c.Value, _token);
                _innerBuilder.Append(value);
                return value;
            }
            else
            {
                MemberExpression m = node as MemberExpression;
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
            if (value != null)
            {
                SqlBuilder self = value as SqlBuilder;
                if (self != null)
                    _innerBuilder.Append(self.InnerBuilder);
                else
                    _innerBuilder.Append(value);
            }
            return this;
        }

        /// <summary>
        /// 在此实例的结尾追加指定字符串的副本。
        /// </summary>
        public ISqlBuilder Append(object value, MemberVisitedMark.VisitedMember m)
        {
            var sql = _dbValue.GetSqlValue(value, _token, m);
            return this.Append(sql);
        }

        /// <summary>
        /// 在此实例的结尾追加回车符
        /// </summary>
        public ISqlBuilder AppendNewLine()
        {
            if (_token != null && !_token.IsDebug)
            {
                if (this.Length == 0) _innerBuilder.Append(' ');
                else
                {
                    char @char = this[this.Length - 1];
                    if (!(@char == ' ' || @char == ',' || @char == ';')) _innerBuilder.Append(' ');
                }
            }
            else
            {
                _innerBuilder.Append(Environment.NewLine);
                if (this.Indent > 0)
                {
                    for (int i = 1; i <= this.Indent; i++) this.AppendTab();
                }
            }

            return this;
        }

        /// <summary>
        /// 在此实例的结尾追加回车符
        /// </summary>
        public ISqlBuilder AppendNewLine(string value)
        {
            this.Append(value);
            this.AppendNewLine();
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
        public ISqlBuilder AppendTab()
        {
            if (_token == null || _token.IsDebug) _innerBuilder.Append(SqlBuilder.TAB);
            else
            {
                if (this.Length == 0) _innerBuilder.Append(' ');
                else
                {
                    char @char = this[this.Length - 1];
                    if (!(@char == ' ' || @char == ',' || @char == ';')) _innerBuilder.Append(' ');
                }
            }
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
        /// 将此实例的指定段中的字符复制到目标 System.Char 数组的指定段中
        /// </summary>
        /// <param name="sourceIndex">此实例中开始复制字符的位置。 索引是从零开始的</param>
        /// <param name="destination">将从中复制字符的数组</param>
        /// <param name="destinationIndex">destination 中将从其开始复制字符的起始位置。 索引是从零开始的</param>
        /// <param name="count">要复制的字符数。</param>
        public void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count)
        {
            _innerBuilder.CopyTo(sourceIndex, destination, destinationIndex, count);
        }

        /// <summary>
        /// 去掉尾部的空白字符
        /// </summary>
        public ISqlBuilder TrimEnd(params char[] @params)
        {
            char[] chars = new char[Environment.NewLine.Length + (@params != null ? @params.Length : 0)];
            for (int i = 0; i < Environment.NewLine.Length; i++) chars[i] = Environment.NewLine[i];

            for (var i = 0; i < @params.Length; i++)
            {
                chars[Environment.NewLine.Length + i] = @params[i];
            }

            int trim = 0;
            int index = this.Length - 1;
            while (index > 0)
            {
                char @char = this[index];
                if (!chars.Contains(@char)) break;
                else
                {
                    index--;
                    trim++;
                }
            }
            if (trim > 0) this.Length -= trim;
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
    }
}
