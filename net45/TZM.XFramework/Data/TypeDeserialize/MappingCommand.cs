using System;
using System.Data;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// 含实体映射信息的SQL命令
    /// </summary>
    public class MappingCommand : Command, IMapping
    {
        private bool _haveManyNavigation = false;
        private bool _hasCombine = false;
        private ISqlBuilder _joinFragment = null;
        private ISqlBuilder _whereFragment = null;
        private TableAliasCache _aliases = null;
        private IDbQueryProvider _provider = null;
        private IDictionary<string, MemberExpression> _navMembers = null;

        /// <summary>
        /// 表达式是否包含 一对多 类型的导航属性
        /// </summary>
        public bool HasMany
        {
            get { return _haveManyNavigation; }
            set { _haveManyNavigation = value; }
        }

        /// <summary>
        /// 合并外键、WHERE、JOIN
        /// </summary>
        public virtual void CombineFragments()
        {
            if (!_hasCombine)
            {
                this.AppendNavigation();
                _joinFragment.Append(_whereFragment);
                _hasCombine = true;
            }
        }

        /// <summary>
        /// SQL 命令
        /// </summary>
        public override string CommandText
        {
            get
            {
                if (!_hasCombine) this.CombineFragments();
                string commandText = _joinFragment.ToString();
                return commandText;
            }
        }

        /// <summary>
        /// 选择字段范围
        /// </summary>
        /// <remarks>INSERT 表达式可能用这些字段</remarks>
        public ColumnCollection Columns { get; set; }

        /// <summary>
        /// 导航属性描述集合
        /// <para>
        /// 用于实体与 <see cref="IDataRecord"/> 做映射
        /// </para>
        /// </summary>
        public NavigationCollection Navigations { get; set; }

        /// <summary>
        /// 导航属性表达式集合
        /// </summary>
        public virtual IDictionary<string, MemberExpression> NavMembers { get { return _navMembers; } }

        /// <summary>
        /// JOIN（含） 之前的片断
        /// </summary>
        public virtual ISqlBuilder JoinFragment { get { return _joinFragment; } }

        /// <summary>
        /// Where 之后的片断
        /// </summary>
        public virtual ISqlBuilder WhereFragment { get { return _whereFragment; } }

        /// <summary>
        /// 实例化 <see cref="MappingCommand"/> 类的新实例
        /// </summary>
        /// <param name="provider">数据查询提供者</param>
        /// <param name="aliases">别名</param>
        /// <param name="token">解析上下文参数</param>
        public MappingCommand(IDbQueryProvider provider, TableAliasCache aliases, ResolveToken token)
            : base(string.Empty, token != null ? token.Parameters : null, System.Data.CommandType.Text)
        {
            _provider = provider;
            _aliases = aliases;
            _navMembers = new Dictionary<string, MemberExpression>();

            _joinFragment = provider.CreateSqlBuilder(token);
            _whereFragment = provider.CreateSqlBuilder(token);
        }

        /// <summary>
        /// 合并外键
        /// </summary>
        public void AddNavMembers(IDictionary<string, MemberExpression> navMembers)
        {
            if (navMembers != null && navMembers.Count > 0)
            {
                foreach (var kvp in navMembers)
                {
                    if (!_navMembers.ContainsKey(kvp.Key)) _navMembers.Add(kvp);
                }
            }
        }

        // 添加导航属性关联
        protected virtual void AppendNavigation()
        {
            if (this._navMembers == null || this._navMembers.Count == 0) return;

            // 如果有一对多的导航属性，肯定会产生嵌套查询。那么内层查询别名肯定是t0，所以需要清掉
            if (this.HasMany) _aliases = new TableAliasCache(_aliases.Declared);
            //开始产生LEFT JOIN 子句
            ISqlBuilder builder = this.JoinFragment;
            foreach (var kvp in _navMembers)
            {
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
                        if (_navMembers.ContainsKey(keyLeft)) innerKey = keyLeft;
                        innerAlias = _aliases.GetNavigationTableAlias(innerKey);
                    }
                }

                string alias1 = !string.IsNullOrEmpty(innerAlias) ? innerAlias : _aliases.GetTableAlias(innerKey);
                string alias2 = _aliases.GetNavigationTableAlias(outerKey);


                builder.AppendNewLine();
                builder.Append("LEFT JOIN ");
                Type type = m.Type;
                if (type.IsGenericType) type = type.GetGenericArguments()[0];
                var typeRuntime2 = TypeRuntimeInfoCache.GetRuntimeInfo(type);
                builder.AppendMember(typeRuntime2.TableName, !typeRuntime2.IsTemporary);
                builder.Append(" ");
                builder.Append(alias2);
                builder.Append(" ON ");
                for (int i = 0; i < attribute.InnerKeys.Length; i++)
                {
                    builder.Append(alias1);
                    builder.Append('.');
                    builder.AppendMember(attribute.InnerKeys[i]);
                    builder.Append(" = ");
                    builder.Append(alias2);
                    builder.Append('.');
                    builder.AppendMember(attribute.OuterKeys[i]);

                    if (i < attribute.InnerKeys.Length - 1) builder.Append(" AND ");
                }
            }
        }
    }
}
