using System.Linq.Expressions;
using System.Collections.Generic;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// 统计函数 表达式解析器
    /// </summary>
    public class StatisExpressionVisitor : ExpressionVisitorBase
    {
        private IDbQueryProvider _provider = null;
        private TableAliasCache _aliases = null;
        private DbExpression _statis = null;
        private DbExpression _groupBy = null;
        private static IDictionary<DbExpressionType, string> _statisMethods = null;
        private string _alias = null;
        private string _columnName = string.Empty;

        /// <summary>
        /// 统计的列名 在嵌套统计时使用
        /// </summary>
        public string ColumnName { get { return _columnName; } }

        static StatisExpressionVisitor()
        {
            _statisMethods = new Dictionary<DbExpressionType, string>
            {
                { DbExpressionType.Count,"COUNT" },
                { DbExpressionType.Max,"MAX" },
                { DbExpressionType.Min,"MIN" },
                { DbExpressionType.Average,"AVG" },
                { DbExpressionType.Sum,"SUM" }
            };
        }

        /// <summary>
        /// 初始化 <see cref="StatisExpressionVisitor"/> 类的新实例
        /// </summary>
        public StatisExpressionVisitor(IDbQueryProvider provider, TableAliasCache aliases, DbExpression statis, DbExpression groupBy = null, string alias = null)
            : base(provider, aliases, statis.Expressions != null ? statis.Expressions[0] : null, false)
        {
            _provider = provider;
            _aliases = aliases;
            _statis = statis;
            _groupBy = groupBy;
            _alias = alias;
        }

        /// <summary>
        /// 将表达式所表示的SQL片断写入SQL构造器
        /// </summary>
        public override void Write(ISqlBuilder builder)
        {
            base._builder = builder;
            if (base._methodVisitor == null) base._methodVisitor = _provider.CreateMethodCallVisitor(this);

            if (_statis != null)
            {

                Expression exp = _statis.DbExpressionType == DbExpressionType.Count ? Expression.Constant(1) : base.Expression;
                if (exp.NodeType == ExpressionType.Lambda) exp = (exp as LambdaExpression).Body;

                // q.Average(a => a);
                // 这种情况下g.Key 一定是单个字段，否则解析出来的SQL执行不了
                if (exp.NodeType == ExpressionType.Parameter) exp = _groupBy.Expressions[0];

                builder.Append(_statisMethods[_statis.DbExpressionType]);
                builder.Append("(");
                base.Visit(exp);
                builder.Append(")");
            }

            //base.Visit();
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            if (node == null) return node;

            _columnName = node.Member.Name;
            // Group By 解析
            if (_groupBy != null && node.IsGrouping())
            {
                string memberName = node.Member.Name;

                // CompanyName = g.Key.Name
                LambdaExpression keySelector = _groupBy.Expressions[0] as LambdaExpression;
                Expression exp = null;
                Expression body = keySelector.Body;


                if (body.NodeType == ExpressionType.MemberAccess)
                {
                    // group xx by a.CompanyName
                    exp = body;
                    //
                    //
                }
                else if (body.NodeType == ExpressionType.New)
                {
                    // group xx by new { Name = a.CompanyName  }
                    NewExpression newExp = body as NewExpression;
                    int index = newExp.Members.IndexOf(x => x.Name == memberName);
                    exp = newExp.Arguments[index];
                }

                return this.Visit(exp);
            }

            if (!string.IsNullOrEmpty(_alias))
            {
                // 嵌套
                _builder.AppendMember(_alias, node.Member.Name);
                return node;
            }
            else
            {
                return base.VisitMember(node);
            }
        }
    }
}
