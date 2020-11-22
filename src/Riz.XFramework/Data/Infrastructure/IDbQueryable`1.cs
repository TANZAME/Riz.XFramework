
using System;
using System.Linq.Expressions;

namespace Riz.XFramework.Data
{
    /// <summary>
    /// 提供对数据类型已知的特定数据源的查询进行计算的功能
    /// </summary>
    /// <typeparam name="TElement">元素类型</typeparam>
    public interface IDbQueryable<TElement> : IDbQueryable
    {
        /// <summary>
        /// 构造一个 <see cref="IDbQueryable"/> 对象，该对象可计算指定表达式树所表示的查询
        /// </summary>
        /// <typeparam name="TResult">返回的元素类型</typeparam>
        /// <param name="dbExpression">查询表达式</param>
        /// <returns></returns>
        IDbQueryable<TResult> CreateQuery<TResult>(DbExpression dbExpression);

        /// <summary>
        /// 构造一个 <see cref="IDbQueryable"/> 对象，该对象可计算指定表达式树所表示的查询
        /// </summary>
        /// <typeparam name="TResult">返回的元素类型</typeparam>
        /// <param name="dbExpressionType">表达式类型</param>
        /// <param name="expression">查询表达式</param>
        /// <returns></returns>
        IDbQueryable<TResult> CreateQuery<TResult>(DbExpressionType dbExpressionType, Expression expression);
    }
}
