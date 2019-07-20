using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace TZM.XFramework.Data.Internal
{
    /// <summary>
    /// 字符串类型方法解析服务
    /// </summary>
    public class QueryableMethodCallExpressionVisitor
    {
        private ISqlBuilder _builder = null;
        private IDbQueryProvider _provider = null;
        private ExpressionVisitorBase _visitor = null;
        private MemberVisitedMark _visitedMark = null;

        /// <summary>
        /// 实例化 <see cref="QueryableMethodCallExpressionVisitor"/> 类的新实例
        /// </summary>
        public QueryableMethodCallExpressionVisitor(IDbQueryProvider provider, ExpressionVisitorBase visitor)
        {
            _provider = provider;
            _visitor = visitor;
            _builder = visitor.SqlBuilder;
            _visitedMark = _visitor.VisitedMark;
        }
    }
}
