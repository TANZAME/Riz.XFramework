
using System;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// DELETE / UPDATE 语句的SelectInfo属性解析器
    /// </summary>
    public sealed class OracleCommand_SelectInfo : NavigationCommand
    {
        private ITextBuilder _onPhrase = null;
        private bool _hasCombine = false;
        private TableAliasCache _aliases = null;
        private IDbQueryProvider _provider = null;

        /// <summary>
        /// 导航属性的Join 表达式的 ON 子句
        /// </summary>
        public ITextBuilder OnPhrase
        {
            get { return _onPhrase; }
        }

        /// <summary>
        /// 合并外键、WHERE、JOIN
        /// </summary>
        public override void CombineFragments()
        {
            if (!_hasCombine)
            {
                this.AppendNavigation();
                this.JoinFragment
                    .AppendNewLine()
                    .Append("WHERE ")
                    .Append(_onPhrase);
                if (this.WhereFragment.Length > 0)
                {
                    if (_onPhrase.Length > 0) this.JoinFragment.Append(" AND ");
                    this.JoinFragment.Append(this.WhereFragment);
                }
                _hasCombine = true;
            }
        }

        /// <summary>
        /// 实例化 <see cref="Builder" /> 的新实例
        /// </summary>
        public OracleCommand_SelectInfo(IDbQueryProvider provider, TableAliasCache aliases, ResolveToken token)
            : base(provider, aliases, token)
        {
            _provider = provider;
            _aliases = aliases;
            _onPhrase = _provider.CreateSqlBuilder(token);
        }

        // 添加导航属性关联
        protected override void AppendNavigation()
        {
            if (this.NavMembers == null || this.NavMembers.Count == 0) return;
            throw new NotSupportedException("OracleDbQueryProvider not support associate DELETE / UPDATE.  ");
        }
    }
}
