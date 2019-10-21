
using System;
using System.Linq;
using System.Linq.Expressions;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// UPDATE 表达式解析器
    /// </summary>
    class SQLiteUpdateExpressionVisitor<T> : UpdateExpressionVisitor
    {
        private string _alias = null;
        private Expression _expression = null;
        private IDbQueryProvider _provider = null;
        private DbQueryableInfo_Update<T> _uQueryInfo = null;

        /// <summary>
        /// SQL 命令解析器
        /// </summary>
        internal Func<DbQueryableInfo_Select<T>, int, bool, ResolveToken, Command> ParseCommand { get; set; }

        /// <summary>
        /// 初始化 <see cref="SQLiteUpdateExpressionVisitor"/> 类的新实例
        /// </summary>
        internal SQLiteUpdateExpressionVisitor(IDbQueryProvider provider, TableAliasCache aliases, DbQueryableInfo_Update<T> uQueryInfo, string alias)
            : base(provider, aliases, uQueryInfo.Expression)
        {
            _alias = alias;
            _provider = provider;
            _uQueryInfo = uQueryInfo;
            _expression = base.Expression;
        }

        //{new App() {Id = p.Id}}
        protected override Expression VisitMemberInit(MemberInitExpression node)
        {
            if (node.Bindings == null || node.Bindings.Count == 0)
                throw new XFrameworkException("Update<T> at least update one member.");

            for (int index = 0; index < node.Bindings.Count; ++index)
            {
                MemberAssignment member = node.Bindings[index] as MemberAssignment;
                _builder.AppendMember(member.Member.Name);
                _builder.Append(" = ");

                if (member.Expression.CanEvaluate())
                    _builder.Append(member.Expression.Evaluate().Value, member.Member, node.Type);
                else
                    this.VisitArgument(member.Expression);

                if (index < node.Bindings.Count - 1)
                {
                    _builder.Append(",");
                    _builder.AppendNewLine();
                }
            }
            return node;
        }

        protected override Expression VisitNew(NewExpression node)
        {
            // 匿名类的New
            if (node == null) return node;
            if (node.Arguments == null || node.Arguments.Count == 0)
                throw new XFrameworkException("Update<T> at least update one member.");

            for (int index = 0; index < node.Arguments.Count; index++)
            {
                var member = node.Members[index];
                _builder.AppendMember(member.Name);
                _builder.Append(" = ");

                if (node.Arguments[index].CanEvaluate())
                    _builder.Append(node.Arguments[index].Evaluate().Value, member, node.Type);
                else
                    this.VisitArgument(node.Arguments[index]);

                if (index < node.Arguments.Count - 1)
                {
                    _builder.Append(',');
                    _builder.AppendNewLine();
                }
            }

            return node;
        }

        internal void VisitArgument(Expression exp, bool wasFilter = false)
        {
            var token = _builder.Token;
            _uQueryInfo.SelectInfo.SelectExpression = new DbExpression(DbExpressionType.Select, exp);
            var cmd2 = (MappingCommand)ParseCommand(_uQueryInfo.SelectInfo, 1, false, new ResolveToken
            {
                Parameters = token.Parameters,
                TableAliasName = "s",
                IsDebug = wasFilter ? token.IsDebug : false
            });

            _builder.Append('(');
            _builder.Append(cmd2.CommandText.Trim());

            if (((MappingCommand)cmd2).WhereFragment.Length > 0)
                _builder.Append(" AND ");
            else
                _builder.Append("WHERE ");

            TypeRuntimeInfo typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo<T>();
            foreach (var invoker in typeRuntime.KeyInvokers)
            {
                _builder.AppendMember("s0", invoker.Name);
                _builder.Append(" = ");
                _builder.AppendMember(typeRuntime.TableName);
                _builder.Append('.');
                _builder.AppendMember(invoker.Name);
                _builder.Append(" AND ");
            }
            _builder.Length -= 5;
            _builder.Append(')');
        }
    }
}