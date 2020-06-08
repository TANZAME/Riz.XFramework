
using System;
using System.Linq.Expressions;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// 含实体映射信息的SQL命令，用于产生 WITH NOLOCK
    /// </summary>
    internal class SqlServerMappingCommand : MappingCommand
    {
        private TableAlias _aliases = null;
        private SqlClient.SqlServerDbContext _context = null;
        private SqlClient.SqlServerDbQueryProvider _provider = null;

        /// <summary>
        /// 实例化 <see cref="SqlServerMappingCommand"/> 类的新实例
        /// </summary>
        /// <param name="context">数据查询提供者</param>
        /// <param name="aliases">别名</param>
        /// <param name="token">解析上下文参数</param>
        public SqlServerMappingCommand(SqlClient.SqlServerDbContext context, TableAlias aliases, ResolveToken token)
            : base(context.Provider, aliases, token)
        {
            _aliases = aliases;
            _context = context;
            _provider = context.Provider as SqlClient.SqlServerDbQueryProvider;
        }

        // 添加导航属性关联
        protected override void ResoveNavMember()
        {
            if (base.NavMembers == null || base.NavMembers.Count == 0) return;

            // 如果有一对多的导航属性，肯定会产生嵌套查询。那么内层查询别名肯定是t0，所以需要清掉
            if (this.HasMany) _aliases = new TableAlias(_aliases.HoldQty);
            //开始产生LEFT JOIN 子句
            ISqlBuilder builder = this.JoinFragment;
            foreach (var nav in base.NavMembers)
            {
                string key = nav.KeyId;
                MemberExpression m = nav.Expression;
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
                        if (base.NavMembers.Contains(keyLeft)) innerKey = keyLeft;
                        innerAlias = _aliases.GetNavTableAlias(innerKey);
                    }
                }

                string alias1 = !string.IsNullOrEmpty(innerAlias) ? innerAlias : _aliases.GetTableAlias(innerKey);
                string alias2 = _aliases.GetNavTableAlias(outerKey);


                builder.AppendNewLine();
                builder.Append("LEFT JOIN ");
                Type type = m.Type;
                if (type.IsGenericType) type = type.GetGenericArguments()[0];
                var typeRuntime2 = TypeRuntimeInfoCache.GetRuntimeInfo(type);
                builder.AppendMember(typeRuntime2.TableName, !typeRuntime2.IsTemporary);
                builder.Append(" ");
                builder.Append(alias2);

                bool withNoLock = !typeRuntime2.IsTemporary && _context.NoLock && !string.IsNullOrEmpty(_provider.WidthNoLock);
                if (withNoLock)
                {
                    builder.Append(' ');
                    builder.Append(_provider.WidthNoLock);
                }

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

                if (nav.Predicate != null)
                {
                    string alias = _aliases.GetNavTableAlias(nav.KeyId);
                    var visitor = new NavPredicateExpressionVisitor(_provider, _aliases, nav.Predicate, alias);
                    visitor.Write(builder);
                }
            }
        }
    }
}
