
using System;
using System.Text;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Riz.XFramework.Data
{
    /// <summary>
    /// SQL 语句建造器
    /// </summary>
    internal class SqlBuilder : ISqlBuilder
    {
        private string _escCharLeft;
        private string _escCharRight;
        private string _escCharQuote;
        private StringBuilder _innerBuilder = null;
        private ITranslateContext _context = null;
        private DbSQLParser _funcletizer = null;

        /// <summary>
        /// TAB 制表符
        /// </summary>
        public const string TAB = "    ";

        /// <summary>
        /// 内部可变字符串
        /// </summary>
        protected StringBuilder InnerBuilder => _innerBuilder;

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
        public char this[int index] => _innerBuilder[index];

        /// <summary>
        /// 缩进
        /// </summary>
        public int Indent { get; set; }

        /// <summary>
        /// 参数化
        /// </summary>
        public virtual bool Parameterized => _context != null && _context.Parameters != null;

        /// <summary>
        /// 解析SQL命令上下文
        /// </summary>
        public ITranslateContext TranslateContext => _context;

        /// <summary>
        /// 实例化 <see cref="SqlBuilder"/> 类的新实例
        /// </summary>
        /// <param name="context">解析SQL命令上下文</param>
        public SqlBuilder(ITranslateContext context)
        {
            _context = context;
            _innerBuilder = new StringBuilder(128);

            var provider = _context.Provider;
            _funcletizer = provider.Funcletizer;
            _escCharLeft = provider.QuotePrefix;
            _escCharRight = provider.QuoteSuffix;
            _escCharQuote = provider.SingleQuoteChar;
        }

        /// <summary>
        /// 追加列名
        /// </summary>
        /// <param name="aliasGenerator">表别名</param>
        /// <param name="node">列名表达式</param>
        /// <returns>返回解析到的表别名</returns>
        public string AppendMember(AliasGenerator aliasGenerator, Expression node)
        {
            Expression expression = node;
            LambdaExpression lambdaExpression = expression as LambdaExpression;
            if (lambdaExpression != null) expression = lambdaExpression.Body;

            if (node.CanEvaluate())
            {
                ConstantExpression c = node.Evaluate();
                string value = _funcletizer.GetSqlValue(c.Value, _context);
                _innerBuilder.Append(value);
                return value;
            }
            else
            {
                MemberExpression m = node.ReduceUnary() as MemberExpression;
                string alias = aliasGenerator == null ? null : aliasGenerator.GetTableAlias(m);
                this.AppendMember(alias, m.Member, m.Expression != null ? m.Expression.Type : null);
                return alias;
            }
        }

        /// <summary>
        /// 追加列名
        /// </summary>
        /// <param name="alias">所在表名</param>
        /// <param name="member">成员元数据</param>
        /// <param name="reflectedType">访问字段或者属性的实际类型</param>
        /// <returns></returns>
        public ISqlBuilder AppendMember(string alias, MemberInfo member, Type reflectedType)
        {
            ColumnAttribute column = TypeUtils.GetColumnAttribute(member, reflectedType ?? (member.ReflectedType ?? member.DeclaringType));
            string memberName = column != null && !string.IsNullOrEmpty(column.Name) ? column.Name : member.Name;
            return this.AppendMember(alias, memberName);
        }

        /// <summary>
        /// 追加列名
        /// </summary>
        /// <param name="alias">所在表名</param>
        /// <param name="fieldName">成员名称（如果实体字段与数据库字段不一致，这里表示数据库字段名）</param>
        /// <returns></returns>
        public ISqlBuilder AppendMember(string alias, string fieldName)
        {
            if (!string.IsNullOrEmpty(alias))
            {
                _innerBuilder.Append(alias);
                _innerBuilder.Append('.');
            }
            return this.AppendMember(fieldName);
        }

        /// <summary>
        /// 追加成员名称
        /// </summary>
        /// <param name="name">成员名称</param>
        /// <param name="quoteByEscChar">使用安全符号括起来，临时表不需要括</param>
        /// <returns></returns>
        public virtual ISqlBuilder AppendMember(string name, bool quoteByEscChar = true)
        {
            if (quoteByEscChar) _innerBuilder.Append(_escCharLeft);
            _innerBuilder.Append(name);
            if (quoteByEscChar) _innerBuilder.Append(_escCharRight);
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
        public ISqlBuilder Append(object value, MemberVisitedStack.VisitedMember m)
        {
            var sqlExpression = _funcletizer.GetSqlValue(value, _context, m);
            return this.Append(sqlExpression);
        }

        /// <summary>
        /// 在此实例的结尾追加回车符
        /// </summary>
        public ISqlBuilder AppendNewLine()
        {
            if (_context != null && !_context.DbContext.IsDebug)
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
            if (_context == null || _context.DbContext.IsDebug) _innerBuilder.Append(SqlBuilder.TAB);
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
        public override string ToString() => _innerBuilder.ToString();
    }
}
