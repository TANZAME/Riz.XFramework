
using System;
using System.Linq.Expressions;

namespace Riz.XFramework.Data
{
    /// <summary>
    /// Npg 含实体映射信息的SQL命令
    /// </summary>
    internal sealed class NpgDbSelectCommand : DbSelectCommand
    {
        private ISqlBuilder _onPhrase = null;
        private bool _hasCombine = false;
        private AliasGenerator _aliasGenerator = null;
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
                this.TanslateNavMember();
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
        /// 实例化 <see cref="NpgDbSelectCommand" /> 的新实例
        /// </summary>
        /// <param name="context">解析SQL命令上下文</param>
        /// <param name="aliasGenerator">别名</param>
        /// <param name="dbExpressionType">表达式类型</param>
        /// <param name="hasMany">是否包含一对多导航属性</param>
        public NpgDbSelectCommand(ITranslateContext context, AliasGenerator aliasGenerator, DbExpressionType dbExpressionType, bool hasMany)
            : base(context, aliasGenerator, hasMany)
        {
            _aliasGenerator = aliasGenerator;
            _onPhrase = ((DbQueryProvider)context.Provider).CreateSqlBuilder(context);
            _dbExpressionType = dbExpressionType;

            if (_dbExpressionType == DbExpressionType.Delete) _keywordName = "USING ";
            else if (_dbExpressionType == DbExpressionType.Update) _keywordName = "FROM ";
            _pad = "".PadLeft(_keywordName.Length, ' ');
        }

        /// <summary>
        /// 添加导航属性关联
        /// </summary>
        protected override void TanslateNavMember()
        {
            if (this.NavMembers == null || this.NavMembers.Count == 0) return;

            // 如果有一对多的导航属性，肯定会产生嵌套查询。那么内层查询别名肯定是t0，所以需要清掉
            if (this.HasMany) _aliasGenerator = new AliasGenerator(_aliasGenerator.ReserveQty);
            //开始产生 USING 子句
            ISqlBuilder jf = this.JoinFragment;
            int index = -1;
            // 未生成USING子句
            if (_aliasGenerator.ReserveQty <= 1)
            {
                jf.AppendNewLine();
                jf.Append(_keywordName);
            }
            else
            {
                jf.Append(',');
                jf.AppendNewLine();
            }

            foreach (var nav in this.NavMembers)
            {
                index++;
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
                    innerAlias = _aliasGenerator.GetJoinAlias(name);

                    if (string.IsNullOrEmpty(innerAlias))
                    {
                        string keyLeft = mLeft.GetKeyWidthoutAnonymous();
                        if (this.NavMembers.Contains(keyLeft)) innerKey = keyLeft;
                        innerAlias = _aliasGenerator.GetNavAlias(innerKey);
                    }
                }

                string alias1 = !string.IsNullOrEmpty(innerAlias) ? innerAlias : _aliasGenerator.GetAlias(innerKey);
                string alias2 = _aliasGenerator.GetNavAlias(outerKey);

                // 补充与USING字符串同等间距的空白
                if (_aliasGenerator.ReserveQty > 1 || index > 0) jf.Append(_pad);

                Type type = m.Type;
                var typeRumtime2 = TypeRuntimeInfoCache.GetRuntimeInfo(type);
                if (type.IsGenericType) type = type.GetGenericArguments()[0];
                jf.AppendTable(typeRumtime2.TableSchema, typeRumtime2.TableName, typeRumtime2.IsTemporary);
                jf.Append(' ');
                jf.Append(alias2);

                if (_onPhrase.Length > 0) _onPhrase.Append(" AND ");
                for (int i = 0; i < attribute.InnerKeys.Length; i++)
                {
                    if (attribute.InnerKeys[i].StartsWith(AppConst.CONSTANT_FOREIGNKEY, StringComparison.Ordinal)) _onPhrase.Append(attribute.InnerKeys[i].Substring(7));
                    else
                    {
                        _onPhrase.Append(alias1);
                        _onPhrase.Append('.');
                        _onPhrase.AppendMember(attribute.InnerKeys[i]);
                    }

                    _onPhrase.Append(" = ");

                    if (attribute.OuterKeys[i].StartsWith(AppConst.CONSTANT_FOREIGNKEY, StringComparison.Ordinal)) _onPhrase.Append(attribute.OuterKeys[i].Substring(7));
                    else
                    {
                        _onPhrase.Append(alias2);
                        _onPhrase.Append('.');
                        _onPhrase.AppendMember(attribute.OuterKeys[i]);
                    }
                }

                if (nav.Predicate != null)
                {
                    string alias = _aliasGenerator.GetNavAlias(nav.Key);
                    var visitor = new NavPredicateExpressionVisitor(_aliasGenerator, _onPhrase, alias);
                    visitor.Visit(nav.Predicate);
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
