
using System;
using System.Linq;
using System.Linq.Expressions;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// UPDATE 表达式解析器
    /// </summary>
    class SQLiteUpdateExpressionVisitor : UpdateExpressionVisitor
    {
        private string _alias = null;
        private Expression _expression = null;
        private IDbQueryProvider _provider = null;
        private IDbQueryableInfo_Update _dbQuery = null;

        /// <summary>
        /// SQL 命令解析器
        /// </summary>
        internal Func<IDbQueryableInfo_Select, int, bool, ResolveToken, Command> ParseCommand { get; set; }

        /// <summary>
        /// 初始化 <see cref="SQLiteUpdateExpressionVisitor"/> 类的新实例
        /// </summary>
        /// <param name="provider">查询语义提供者</param>
        /// <param name="aliases">表别名集合</param>
        /// <param name="dbQuery">更新语义</param>
        /// <param name="alias">指定的表达式别名</param>
        internal SQLiteUpdateExpressionVisitor(IDbQueryProvider provider, TableAliasCache aliases, IDbQueryableInfo_Update dbQuery, string alias)
            : base(provider, aliases, dbQuery.Expression)
        {
            _alias = alias;
            _provider = provider;
            _dbQuery = dbQuery;
            _expression = base.Expression;
        }

        /// <summary>
        /// 访问成员初始化表达式，如 => new App() {Id = p.Id}
        /// </summary>
        /// <param name="node">要访问的成员初始化表达式</param>
        /// <returns></returns>
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

        /// <summary>
        /// 访问构造函数表达式，如 =>new  {Id = p.Id}}
        /// </summary>
        /// <param name="node">构造函数调用的表达式</param>
        /// <returns></returns>
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

        /// <summary>
        /// 访问参数列表
        /// </summary>
        /// <param name="expression">将访问的表达式</param>
        /// <param name="isFilter">是否过滤条件</param>
        internal void VisitArgument(Expression expression, bool isFilter = false)
        {
            var token = _builder.Token;
            _dbQuery.Query.Select = new DbExpression(DbExpressionType.Select, expression);
            var cmd2 = ParseCommand(_dbQuery.Query, 1, false, new ResolveToken
            {
                Parameters = token.Parameters,
                AliasPrefix = "s",
                DbContext = _builder.Token.DbContext
            }) as MapperCommand;

            _builder.Append('(');
            _builder.Append(cmd2.CommandText.Trim());

            if (((MapperCommand)cmd2).WhereFragment.Length > 0)
                _builder.Append(" AND ");
            else
                _builder.Append("WHERE ");

            var typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo(_dbQuery.Entity != null ? _dbQuery.Entity.GetType() : _dbQuery.Query.FromType);
            foreach (var m in typeRuntime.KeyMembers)
            {
                _builder.AppendMember("s0", m.Name);
                _builder.Append(" = ");
                _builder.AppendMember(typeRuntime.TableName);
                _builder.Append('.');
                _builder.AppendMember(m.Name);
                _builder.Append(" AND ");
            }
            _builder.Length -= 5;
            _builder.Append(')');
        }
    }
}