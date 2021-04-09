
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Riz.XFramework.Data
{
    /// <summary>
    /// UPDATE 表达式解析器
    /// </summary>
    internal class SQLiteUpdateExpressionVisitor : UpdateExpressionVisitor
    {
        private string _alias = null;
        private ISqlBuilder _builder = null;
        private DbQueryUpdateTree _tree = null;
        private MemberVisitedStack _visitedStack = null;

        /// <summary>
        /// SQL 命令解析器
        /// </summary>
        internal Func<DbQuerySelectTree, int, bool, ITranslateContext, DbRawCommand> Translator { get; set; }

        /// <summary>
        /// 初始化 <see cref="SQLiteUpdateExpressionVisitor"/> 类的新实例
        /// </summary>
        /// <param name="ag">表别名解析器</param>
        /// <param name="builder">SQL 语句生成器</param>
        /// <param name="tree">更新语义</param>
        /// <param name="alias">指定的表达式别名</param>
        internal SQLiteUpdateExpressionVisitor(AliasGenerator ag, ISqlBuilder builder, DbQueryUpdateTree tree, string alias)
            : base(ag, builder)
        {
            _tree = tree;
            _alias = alias;
            _builder = builder;
            _visitedStack = base.VisitedStack;
        }

        /// <summary>
        /// 访问成员初始化表达式，如 => new App() { Id = p.Id }
        /// </summary>
        /// <param name="node">要访问的成员初始化表达式</param>
        /// <returns></returns>
        protected override Expression VisitMemberInit(MemberInitExpression node)
        {
            if (node.Bindings == null || node.Bindings.Count == 0)
                throw new XFrameworkException("The Update<T> method requires at least one field to be updated.");

            for (int index = 0; index < node.Bindings.Count; ++index)
            {
                var m = node.Bindings[index] as MemberAssignment;
                _builder.AppendMember(null, m.Member, node.Type);
                _builder.Append(" = ");

                if (m.Expression.CanEvaluate())
                    this.VisitWithoutStack(_ => this.VisitObjectMember(node.Type, m.Member, m.Expression.Evaluate()));
                else
                    this.VisitArgument(m.Expression);

                if (index < node.Bindings.Count - 1)
                {
                    _builder.Append(",");
                    _builder.AppendNewLine();
                }
            }
            return node;
        }

        /// <summary>
        /// 访问构造函数表达式，如 =>new  { Id = p.Id }
        /// </summary>
        /// <param name="node">构造函数调用的表达式</param>
        /// <returns></returns>
        protected override Expression VisitNew(NewExpression node)
        {
            // 匿名类的New
            if (node == null) return node;
            if (node.Arguments == null || node.Arguments.Count == 0 || node.Members.Count == 0)
                throw new XFrameworkException("The Update<T> method requires at least one field to be updated.");

            for (int index = 0; index < node.Arguments.Count; index++)
            {
                var m = node.Members[index];
                _builder.AppendMember(null, m, node.Type);
                _builder.Append(" = ");

                if (node.Arguments[index].CanEvaluate())
                    this.VisitWithoutStack(_ => this.VisitObjectMember(node.Type, node.Members[index], node.Arguments[index].Evaluate()));
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
            ITranslateContext context = _builder.TranslateContext;
            _tree.Query.Select = new DbExpression(DbExpressionType.Select, expression);
            var cmd = Translator.Invoke(_tree.Query, 1, false, context.Clone("s")) as DbSelectCommand;

            _builder.Append('(');
            _builder.Append(cmd.CommandText.Trim());

            if (((DbSelectCommand)cmd).WhereFragment.Length > 0)
                _builder.Append(" AND ");
            else
                _builder.Append("WHERE ");

            var typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo(_tree.Entity != null ? _tree.Entity.GetType() : _tree.Query.From);
            foreach (var m in typeRuntime.KeyMembers)
            {
                _builder.AppendMember("s0", m.Member, typeRuntime.Type);
                _builder.Append(" = ");
                _builder.AppendTable(typeRuntime.TableSchema, typeRuntime.TableName, typeRuntime.IsTemporary);
                _builder.Append('.');
                _builder.AppendMember(null, m.Member, typeRuntime.Type);
                _builder.Append(" AND ");
            }
            _builder.Length -= 5;
            _builder.Append(')');
        }

        // 访问对象成员
        private Expression VisitObjectMember(Type newType, MemberInfo member, Expression expression)
        {
            // 先添加当前字段的访问痕迹标记
            _visitedStack.Add(member, newType);
            return base.Visit(expression);
        }
    }
}