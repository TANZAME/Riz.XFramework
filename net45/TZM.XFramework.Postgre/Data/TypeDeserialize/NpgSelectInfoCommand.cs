using System;
using System.Data;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// DELETE / UPDATE 语句的SelectInfo属性解析器
    /// </summary>
    public sealed class NpgSelectInfoCommand : SelectCommand
    {
        private ITextBuilder _onPhrase = null;
        private bool _convergence = false;
        private TableAliasCache _aliases = null;
        private IDbQueryProvider _provider = null;
        private readonly string _keywordName = string.Empty;
        private NpgCommandType _operationType;

        /// <summary>
        /// 导航属性的Join 表达式的 ON 子句，拼在 WHERE 后面
        /// </summary>
        public ITextBuilder OnPhrase
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
        /// <param name="token">参数列表，NULL 或者 Parameters=NULL 时表示不使用参数化</param>
        public NpgSelectInfoCommand(IDbQueryProvider provider, TableAliasCache aliases, NpgCommandType operationType, ParserToken token)
            : base(provider, aliases, token)
        {
            _provider = provider;
            _aliases = aliases;
            _onPhrase = _provider.CreateSqlBuilder(token);
            _operationType = operationType;

            if (_operationType == NpgCommandType.DELETE) _keywordName = "USING";
            else if (_operationType == NpgCommandType.UPDATE) _keywordName = "FROM";

        }

        // 添加导航属性关联
        protected override void AppendNavigation()
        {
            if (this.NavMembers == null || this.NavMembers.Count == 0) return;

            // 如果有一对多的导航属性，肯定会产生嵌套查询。那么内层查询别名肯定是t0，所以需要清掉
            if (this.HaveManyNavigation) _aliases = new TableAliasCache(_aliases.Declared);
            //开始产生 USING 子句
            ITextBuilder jf = this.JoinFragment;
            int index = -1;
            // 未生成USING子句
            if (_aliases.Declared <= 1)
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
                ForeignKeyAttribute attribute = typeRuntime.GetInvokerAttribute<ForeignKeyAttribute>(m.Member.Name);

                string innerKey = string.Empty;
                string outerKey = key;
                string innerAlias = string.Empty;

                if (!m.Expression.Acceptable())
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
                if (_aliases.Declared > 1 || index > 0) jf.Append("     ");

                Type type = m.Type;
                var typeRumtime2 = TypeRuntimeInfoCache.GetRuntimeInfo(type);
                if (type.IsGenericType) type = type.GetGenericArguments()[0];
                jf.Append(' ');
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
