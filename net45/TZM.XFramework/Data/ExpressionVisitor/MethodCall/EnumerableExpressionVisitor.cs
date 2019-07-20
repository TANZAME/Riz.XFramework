
using System.Linq.Expressions;

namespace TZM.XFramework.Data.Internal
{
    /// <summary>
    /// 迭代类型方法解析服务
    /// </summary>
    public class EnumerableMethodCall
    {
        private ISqlBuilder _builder = null;
        private IDbQueryProvider _provider = null;
        private ExpressionVisitorBase _visitor = null;
        private MemberVisitedMark _visitedMark = null;

        /// <summary>
        /// 实例化 <see cref="EnumerableMethodCall"/> 类的新实例
        /// </summary>
        public EnumerableMethodCall(IDbQueryProvider provider, ExpressionVisitorBase visitor)
        {
            _provider = provider;
            _visitor = visitor;
            _builder = visitor.SqlBuilder;
            _visitedMark = _visitor.VisitedMark;
        }

        /// <summary>
        /// 访问 Contains 方法
        /// </summary>
        protected virtual Expression VisitContains(MethodCallExpression m)
        {
            if (m == null) return m;

            _visitedMark.ClearImmediately = false;
            _visitor.Visit(m.Arguments[m.Arguments.Count - 1]);
            _builder.Append(" IN(");

            Expression exp = m.Object != null ? m.Object : m.Arguments[0];
            if (exp.NodeType == ExpressionType.Constant)
            {
                _visitor.Visit(exp);
            }
            else if (exp.NodeType == ExpressionType.MemberAccess)
            {
                _visitor.Visit(exp.Evaluate());
            }
            else if (exp.NodeType == ExpressionType.NewArrayInit)
            {
                // => new[] { 1, 2, 3 }.Contains(a.DemoId)
                var expressions = (exp as NewArrayExpression).Expressions;
                for (int i = 0; i < expressions.Count; i++)
                {
                    _visitor.Visit(expressions[i]);
                    if (i < expressions.Count - 1) _builder.Append(",");
                    else if (i == expressions.Count - 1) _visitedMark.ClearImmediately = true;
                }
            }
            else if (exp.NodeType == ExpressionType.ListInit)
            {
                // => new List<int> { 1, 2, 3 }.Contains(a.DemoId)
                var initializers = (exp as ListInitExpression).Initializers;
                for (int i = 0; i < initializers.Count; i++)
                {
                    foreach (var args in initializers[i].Arguments) _visitor.Visit(args);

                    if (i < initializers.Count - 1) _builder.Append(",");
                    else if (i == initializers.Count - 1) _visitedMark.ClearImmediately = true;
                }
            }
            else if (exp.NodeType == ExpressionType.New)
            {
                // => new List<int>(_demoIdList).Contains(a.DemoId)
                var arguments = (exp as NewExpression).Arguments;
                for (int i = 0; i < arguments.Count; i++)
                {
                    if (i == arguments.Count - 1) _visitedMark.ClearImmediately = true;
                    _visitor.Visit(arguments[i]);
                }
            }

            _visitedMark.ClearImmediately = true;
            _builder.Append(")");

            return m;
        }
    }
}
