
using System;
using System.Linq.Expressions;
using Riz.XFramework.Data.SqlClient;

namespace Riz.XFramework.Data
{
    /// <summary>
    /// 含实体映射信息的SQL命令，用于产生 WITH NOLOCK
    /// </summary>
    public class SqlServerDbSelectCommand : DbSelectCommand
    {
        private bool _isNoLock = false;
        private string _withNoLock = string.Empty;
        private AliasGenerator _ag = null;

        /// <summary>
        /// 实例化 <see cref="SqlServerDbSelectCommand"/> 类的新实例
        /// </summary>
        /// <param name="context">解析SQL命令上下文</param>
        /// <param name="ag">表别名解析器</param>
        /// <param name="hasMany">是否包含一对多导航属性</param>
        public SqlServerDbSelectCommand(ITranslateContext context, AliasGenerator ag, bool hasMany)
            : base(context, ag, hasMany)
        {
            _ag = ag;

            var dc = context.DbContext;
            _isNoLock = ((SqlServerDbContext)dc).NoLock;
            _withNoLock = ((SqlServerDbQueryProvider)dc.Provider).WidthNoLock;
        }

        /// <summary>
        /// 添加导航属性关联
        /// </summary>
        protected override void TanslateNavMember()
        {
            if (base.NavMembers == null || base.NavMembers.Count == 0) return;

            // 如果有一对多的导航属性，肯定会产生嵌套查询。那么内层查询别名肯定是t0，所以需要清掉
            if (this.HasMany) _ag = new AliasGenerator(_ag.ReserveQty);
            //开始产生LEFT JOIN 子句
            ISqlBuilder builder = this.JoinFragment;
            foreach (var nav in base.NavMembers)
            {
                string key = nav.Key;
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
                    string name = TypeRuntimeInfoCache.GetRuntimeInfo(mLeft.Type).TableFullName;
                    innerAlias = _ag.GetJoinTableAlias(name);

                    if (string.IsNullOrEmpty(innerAlias))
                    {
                        string keyLeft = mLeft.GetKeyWidthoutAnonymous();
                        if (base.NavMembers.Contains(keyLeft)) innerKey = keyLeft;
                        innerAlias = _ag.GetNavTableAlias(innerKey);
                    }
                }

                string alias1 = !string.IsNullOrEmpty(innerAlias) ? innerAlias : _ag.GetTableAlias(innerKey);
                string alias2 = _ag.GetNavTableAlias(outerKey);


                builder.AppendNewLine();
                builder.Append("LEFT JOIN ");
                Type type = m.Type;
                if (type.IsGenericType) type = type.GetGenericArguments()[0];
                var typeRuntime2 = TypeRuntimeInfoCache.GetRuntimeInfo(type);
                builder.AppendTable(typeRuntime2.TableSchema, typeRuntime2.TableFullName, typeRuntime2.IsTemporary);
                builder.Append(" ");
                builder.Append(alias2);

                bool withNoLock = !typeRuntime2.IsTemporary && _isNoLock && !string.IsNullOrEmpty(_withNoLock);
                if (withNoLock)
                {
                    builder.Append(' ');
                    builder.Append(_withNoLock);
                }

                builder.Append(" ON ");
                for (int i = 0; i < attribute.InnerKeys.Length; i++)
                {
                    if (attribute.InnerKeys[i].StartsWith(AppConst.CONSTANT_FOREIGNKEY, StringComparison.Ordinal)) builder.Append(attribute.InnerKeys[i].Substring(7));
                    else
                    {
                        builder.Append(alias1);
                        builder.Append('.');
                        builder.AppendMember(attribute.InnerKeys[i]);
                    }

                    builder.Append(" = ");

                    if (attribute.OuterKeys[i].StartsWith(AppConst.CONSTANT_FOREIGNKEY, StringComparison.Ordinal)) builder.Append(attribute.OuterKeys[i].Substring(7));
                    else
                    {
                        builder.Append(alias2);
                        builder.Append('.');
                        builder.AppendMember(attribute.OuterKeys[i]);
                    }

                    if (i < attribute.InnerKeys.Length - 1) builder.Append(" AND ");
                }

                if (nav.Predicate != null)
                {
                    string alias = _ag.GetNavTableAlias(nav.Key);
                    var visitor = new NavPredicateExpressionVisitor(_ag, builder, alias);
                    visitor.Visit(nav.Predicate);
                }
            }
        }
    }
}
