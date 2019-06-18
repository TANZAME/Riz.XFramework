using System;
using System.Data;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// DELETE / UPDATE 语句的SelectInfo属性解析器
    /// </summary>
    public sealed class OracleDeleteDbCommandDefinition : SelectDbCommandDefinition
    {
        private ISqlBuilder _onPhrase = null;
        private bool _convergence = false;
        private TableAliasCache _aliases = null;
        private IDbQueryProvider _provider = null;

        /// <summary>
        /// 导航属性的Join 表达式的 ON 子句
        /// </summary>
        public ISqlBuilder OnPhrase
        {
            get { return _onPhrase; }
        }

        /// <summary>
        /// 合并外键、WHERE、JOIN
        /// </summary>
        public override void Convergence()
        {
            if (!_convergence)
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
                _convergence = true;
            }
        }

        /// <summary>
        /// 实例化 <see cref="Builder" /> 的新实例
        /// </summary>
        public OracleDeleteDbCommandDefinition(IDbQueryProvider provider, TableAliasCache aliases, List<IDbDataParameter> parameters)
            : base(provider, aliases, parameters)
        {
            _provider = provider;
            _aliases = aliases;
            _onPhrase = _provider.CreateSqlBuilder(parameters);
        }

        // 添加导航属性关联
        protected override void AppendNavigation()
        {
            if (this.NavMembers == null || this.NavMembers.Count == 0) return;
            throw new NotSupportedException("OracleDbQueryProvider not support associate DELETE / UPDATE.  ");            
        }
    }
}
