
using System;
using System.Linq.Expressions;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// Npg 含实体映射信息的SQL命令
    /// </summary>
    public sealed class NpgMapperDbCommand : MapperCommand
    {
        private ISqlBuilder _onPhrase = null;
        private bool _hasCombine = false;
        private TableAliasCache _aliases = null;
        private IDbQueryProvider _provider = null;
        private readonly string _keywordName = string.Empty;
        private DbExpressionType _dbExpressionType = DbExpressionType.None;
        private string _pad = "";

        /// <summary>
        /// 导航属性的Join 表达式的 ON 子句，拼在 WHERE 后面
        /// </summary>
        public ISqlBuilder OnPhrase
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
        /// 实例化 <see cref="NpgMapperDbCommand" /> 的新实例
        /// </summary>
        /// <param name="provider">数据查询提供者</param>
        /// <param name="aliases">别名</param>
        /// <param name="dbExpressionType">表达式类型</param>
        /// <param name="token">解析上下文参数</param>
        public NpgMapperDbCommand(IDbQueryProvider provider, TableAliasCache aliases, DbExpressionType dbExpressionType, ResolveToken token)
            : base(provider, aliases, token)
        {
            _provider = provider;
            _aliases = aliases;
            _onPhrase = _provider.CreateSqlBuilder(token);
            _dbExpressionType = dbExpressionType;

            if (_dbExpressionType == DbExpressionType.Delete) _keywordName = "USING ";
            else if (_dbExpressionType == DbExpressionType.Update) _keywordName = "FROM ";
            _pad = "".PadLeft(_keywordName.Length, ' ');
        }

        /// <summary>
        /// 添加导航属性关联
        /// </summary>
        protected override void AppendNavigation()
        {
            if (this.NavMembers == null || this.NavMembers.Count == 0) return;

            // 如果有一对多的导航属性，肯定会产生嵌套查询。那么内层查询别名肯定是t0，所以需要清掉
            if (this.HasMany) _aliases = new TableAliasCache(_aliases.HoldQty);
            //开始产生 USING 子句
            ISqlBuilder jf = this.JoinFragment;
            int index = -1;
            // 未生成USING子句
            if (_aliases.HoldQty <= 1)
            {
                jf.AppendNewLine();
                jf.Append(_keywordName);
            }
            else
            {
                jf.Append(',');
                jf.AppendNewLine();
            }

            foreach (var kvp in this.NavMembers)
            {
                index++;
                string key = kvp.Key;
                MemberExpression m = kvp.Value;
                TypeRuntimeInfo typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo(m.Expression.Type);
                ForeignKeyAttribute attribute = typeRuntime.GetMemberAttribute<ForeignKeyAttribute>(m.Member.Name);

                string innerKey = string.Empty;
                string outerKey = key;
                string innerAlias = string.Empty;

                if (!m.Expression.Visitable())
                {
                    innerKey = m.Expression.NodeType == ExpressionType.Parameter
                        ? (m.Expression as ParameterExpression).Name
                        : (m.Expression as MemberExpression).Member.Name;
                }
                else
                {
                    MemberExpression mLeft = null;
                    if (m.Expression.NodeType == ExpressionType.MemberAccess) mLeft = m.Expression as MemberExpression;
                    else if (m.Expression.NodeType == ExpressionType.Call) mLeft = (m.Expression as MethodCallExpression).Object as MemberExpression;
                    string name = TypeRuntimeInfoCache.GetRuntimeInfo(mLeft.Type).TableName;
                    innerAlias = _aliases.GetJoinTableAlias(name);

                    if (string.IsNullOrEmpty(innerAlias))
                    {
                        string keyLeft = mLeft.GetKeyWidthoutAnonymous();
                        if (this.NavMembers.ContainsKey(keyLeft)) innerKey = keyLeft;
                        innerAlias = _aliases.GetNavigationTableAlias(innerKey);
                    }
                }

                string alias1 = !string.IsNullOrEmpty(innerAlias) ? innerAlias : _aliases.GetTableAlias(innerKey);
                string alias2 = _aliases.GetNavigationTableAlias(outerKey);

                // 补充与USING字符串同等间距的空白
                if (_aliases.HoldQty > 1 || index > 0) jf.Append(_pad);

                Type type = m.Type;
                var typeRumtime2 = TypeRuntimeInfoCache.GetRuntimeInfo(type);
                if (type.IsGenericType) type = type.GetGenericArguments()[0];
                jf.AppendMember(typeRumtime2.TableName, typeRumtime2.IsTemporary);
                jf.Append(' ');
                jf.Append(alias2);

                if (_onPhrase.Length > 0) _onPhrase.Append(" AND ");
                for (int i = 0; i < attribute.InnerKeys.Length; i++)
                {
                    _onPhrase.Append(alias1);
                    _onPhrase.Append('.');
                    _onPhrase.AppendMember(attribute.InnerKeys[i]);
                    _onPhrase.Append(" = ");
                    _onPhrase.Append(alias2);
                    _onPhrase.Append('.');
                    _onPhrase.AppendMember(attribute.OuterKeys[i]);
                }

                if (index < this.NavMembers.Count - 1)
                {
                    jf.Append(',');
                    jf.AppendNewLine();
                }
            }
        }
    }
}
