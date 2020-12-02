using System;
using System.Data;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using System.Reflection;

namespace Riz.XFramework.Data
{
    /// <summary>
    /// 含实体映射信息的SQL命令
    /// </summary>
    public class DbSelectCommand : DbRawCommand, IMapInfo
    {
        private bool _haveManyNavigation = false;
        private bool _hasCombine = false;
        private ISqlBuilder _joinFragment = null;
        private ISqlBuilder _whereFragment = null;
        private ITranslateContext _context = null;
        private AliasGenerator _ag = null;
        private HashCollection<NavMember> _navMembers = null;

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
                this.TanslateNavMember();
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
        public ColumnDescriptorCollection PickColumns { get; internal set; }

        /// <summary>
        /// 选中字段的文本，给 Contains 表达式用
        /// </summary>
        public string PickColumnText { get; internal set; }

        /// <summary>
        /// 选中的导航属性描述集合
        /// <para>
        /// 用于实体与 <see cref="IDataRecord"/> 做映射
        /// </para>
        /// </summary>
        public NavDescriptorCollection PickNavDescriptors { get; internal set; }

        /// <summary>
        /// 导航属性表达式集合
        /// <para>
        /// 来源于：SELECT、JOIN、WHERE 各个片断
        /// </para>
        /// </summary>
        public virtual HashCollection<NavMember> NavMembers => _navMembers;

        /// <summary>
        /// JOIN（含） 之前的片断
        /// </summary>
        public virtual ISqlBuilder JoinFragment => _joinFragment;

        /// <summary>
        /// Where 之后的片断
        /// </summary>
        public virtual ISqlBuilder WhereFragment => _whereFragment;

        /// <summary>
        /// 实例化 <see cref="DbSelectCommand"/> 类的新实例
        /// </summary>
        /// <param name="context">解析SQL命令上下文</param>
        /// <param name="ag">别名</param>
        public DbSelectCommand(ITranslateContext context, AliasGenerator ag)
            : base(string.Empty, context != null ? context.Parameters : null, System.Data.CommandType.Text)
        {
            _ag = ag;
            _context = context;

            var provider = (DbQueryProvider)_context.DbContext.Provider;
            _joinFragment = provider.CreateSqlBuilder(context);
            _whereFragment = provider.CreateSqlBuilder(context);
        }

        /// <summary>
        /// 合并外键
        /// </summary>
        public void AddNavMembers(HashCollection<NavMember> navMembers)
        {
            if (navMembers != null && navMembers.Count > 0)
            {
                if (_navMembers == null) _navMembers = new HashCollection<NavMember>();
                foreach (var nav in navMembers)
                {
                    if (!_navMembers.Contains(nav.Key)) _navMembers.Add(nav);
                }
            }
        }

        /// <summary>
        /// 添加导航属性关联
        /// </summary>
        protected virtual void TanslateNavMember()
        {
            if (this._navMembers == null || this._navMembers.Count == 0) return;

            // 如果有一对多的导航属性，肯定会产生嵌套查询。那么内层查询别名肯定是t0，所以需要清掉
            if (this.HasMany) _ag = new AliasGenerator(_ag.ReserveQty);
            //开始产生LEFT JOIN 子句
            ISqlBuilder builder = this.JoinFragment;
            foreach (var nav in _navMembers)
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
                    string name = TypeRuntimeInfoCache.GetRuntimeInfo(mLeft.Type).TableName;
                    innerAlias = _ag.GetJoinTableAlias(name);

                    if (string.IsNullOrEmpty(innerAlias))
                    {
                        string keyLeft = mLeft.GetKeyWidthoutAnonymous();
                        if (_navMembers.Contains(keyLeft)) innerKey = keyLeft;
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
                builder.AppendMember(typeRuntime2.TableName, !typeRuntime2.IsTemporary);
                builder.Append(" ");
                builder.Append(alias2);
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
