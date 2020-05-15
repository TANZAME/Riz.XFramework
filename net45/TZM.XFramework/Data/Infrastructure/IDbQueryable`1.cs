
using System;
using System.Linq.Expressions;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// 提供对数据类型已知的特定数据源的查询进行计算的功能
    /// </summary>
    public interface IDbQueryable<TElement> : IDbQueryable
    {
        /// <summary>
        /// 构造一个 <see cref="IDbQueryable"/> 对象，该对象可计算指定表达式树所表示的查询
        /// </summary>
        IDbQueryable<TSource> CreateQuery<TSource>(DbExpressionType dbExpressionType, Expression expression = null);

        /// <summary>
        /// 构造一个 <see cref="IDbQueryable"/> 对象，该对象可计算指定表达式树所表示的查询
        /// </summary>
        IDbQueryable<TSource> CreateQuery<TSource>(DbExpression exp = null);
    }
}
