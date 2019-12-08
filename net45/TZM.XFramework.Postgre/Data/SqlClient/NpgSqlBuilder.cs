
using System;
using System.Data;
using System.Collections.Generic;
using Npgsql;
using NpgsqlTypes;
using System.Net;

namespace TZM.XFramework.Data.SqlClient
{
    /// <summary>
    /// SQL 语句构造器
    /// </summary>
    public class NpgSqlBuilder : SqlBuilder
    {
        private bool _caseSensitive = false;
        private string _escCharLeft;
        private string _escCharRight;
        private IDbQueryProvider _provider = null;

        /// <summary>
        /// 是否使用双引号，POSTGRE 只有最外层才需要区分大小
        /// </summary>
        public bool UseQuote { get; set; }

        /// <summary>
        /// 实例化 <see cref="NpgSqlBuilder"/> 类的新实例
        /// </summary>
        /// <param name="provider">提供者</param>
        /// <param name="token">参数列表，NULL 或者 Parameters=NULL 时表示不使用参数化</param>
        public NpgSqlBuilder(IDbQueryProvider provider, ResolveToken token)
            : base(provider, token)
        {
            _provider = provider;
            _escCharLeft = _provider.QuotePrefix;
            _escCharRight = _provider.QuoteSuffix;
            _caseSensitive = token != null && token.DbContext != null ? ((NpgDbContext)token.DbContext).CaseSensitive : false;
        }

        /// <summary>
        /// 追加成员名称
        /// </summary>
        /// <param name="name">成员名称</param>
        /// <param name="quote">使用安全符号括起来，临时表不需要括</param>
        /// <returns></returns>
        public override ISqlBuilder AppendMember(string name, bool quote)
        {
            if (this._caseSensitive) base.InnerBuilder.Append(_escCharLeft);
            base.InnerBuilder.Append(name);
            if (this._caseSensitive) base.InnerBuilder.Append(_escCharRight);
            return this;
        }

        /// <summary>
        /// 在此实例的结尾追加 AS
        /// </summary>
        public override ISqlBuilder AppendAs(string name)
        {
            base.InnerBuilder.Append(" AS ");
            if (this._caseSensitive || this.UseQuote) base.InnerBuilder.Append(_escCharLeft);
            base.InnerBuilder.Append(name);
            if (this._caseSensitive || this.UseQuote) base.InnerBuilder.Append(_escCharRight);
            return this;
        }
    }
}
