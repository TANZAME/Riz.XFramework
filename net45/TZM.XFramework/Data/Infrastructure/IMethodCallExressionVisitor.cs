using System;
using System.Linq.Expressions;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// 定义方法表达式访问器
    /// </summary>
    public interface IMethodCallExressionVisitor
    {
        /// <summary>
        /// 访问表示方法调用的节点
        /// </summary>
        /// <param name="node">方法调用节点</param>
        /// <returns></returns>
        Expression VisitMethodCall(MethodCallExpression node);

        /// <summary>
        /// 访问表示方法调用的节点
        /// </summary>
        /// <param name="node">方法调用节点</param>
        /// <returns></returns>
        Expression VisitMethodCall(BinaryExpression node);

        /// <summary>
        /// 访问表示 null 判断运算的节点 a.Name == null
        /// </summary>
        /// <param name="b">二元表达式节点</param>
        /// <returns></returns>
        Expression VisitEqualNull(BinaryExpression b);

        /// <summary>
        /// 访问表示 null 合并运算的节点 a ?? b
        /// </summary>
        /// <param name="b">二元表达式节点</param>
        /// <returns></returns>
        Expression VisitCoalesce(BinaryExpression b);

        /// <summary>
        /// 访问表示字段或者属性的属性的节点 a.Name.Length
        /// </summary>
        /// <param name="node">字段或者属性节点</param>
        /// <returns></returns>
        Expression VisitMemberMember(MemberExpression node);

        /// <summary>
        /// 访问一元运算符
        /// </summary>
        /// <param name="u">一元运算符表达式</param>
        /// <returns></returns>
        Expression VisitUnary(UnaryExpression u);
    }
}
