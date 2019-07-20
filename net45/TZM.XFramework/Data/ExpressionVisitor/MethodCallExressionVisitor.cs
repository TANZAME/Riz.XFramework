
using System;
using System.Linq;
using System.Data;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// <see cref="MethodCallExressionVisitor"/> 表达式访问器
    /// </summary>
    public abstract class MethodCallExressionVisitor : IMethodCallExressionVisitor
    {
        private ISqlBuilder _builder = null;
        private IDbQueryProvider _provider = null;
        private ExpressionVisitorBase _visitor = null;
        private MemberVisitedMark _visitedMark = null;
        private Internal.MethodCallExressionVisitorContainer _internalVisitors = null;

        #region 公开属性

        /// <summary>
        /// 解析方法调用的服务容器
        /// </summary>
        protected virtual Internal.MethodCallExressionVisitorContainer InternalVisitors
        {
            get
            {
                if (_internalVisitors == null)
                {
                    _internalVisitors = new Internal.MethodCallExressionVisitorContainer();
                    _internalVisitors.Add(typeof(string), (provider, visitor) => new Internal.StringMethodCallExpressionVisitor(provider, visitor));
                    _internalVisitors.Add(typeof(SqlMethod), (provider, visitor) => new Internal.SqlMethodMethodCallExpressionVisitor(provider, visitor));
                    _internalVisitors.Add(typeof(IEnumerable), (provider, visitor) => new Internal.EnumerableMethodCall(provider, visitor));
                    _internalVisitors.Add(typeof(IDbQueryable), (provider, visitor) => new Internal.QueryableMethodCallExpressionVisitor(provider, visitor));
                }

                return _internalVisitors;
            }
        }

        #endregion

        #region 构造函数

        /// <summary>
        /// 实例化 <see cref="MethodCallExressionVisitor"/> 类的新实例
        /// </summary>
        public MethodCallExressionVisitor(IDbQueryProvider provider, ExpressionVisitorBase visitor)
        {
            _provider = provider;
            _visitor = visitor;
            _builder = visitor.SqlBuilder;
            _visitedMark = _visitor.VisitedMark;
        }

        #endregion

        #region 接口方法

        /// <summary>
        /// 访问表示 null 判断运算的节点 a.Name == null
        /// </summary>
        public virtual Expression VisitEqualNull(BinaryExpression b)
        {
            // a.Name == null => a.Name Is Null
            Expression left = b.Left.NodeType == ExpressionType.Constant ? b.Right : b.Left;
            Expression right = b.Left.NodeType == ExpressionType.Constant ? b.Left : b.Right;
            string oper = b.NodeType == ExpressionType.Equal ? " IS " : " IS NOT ";

            _visitor.Visit(left);
            _builder.Append(oper);
            _visitor.Visit(right);

            return b;
        }

        /// <summary>
        /// 访问表示 null 合并运算的节点 a ?? b
        /// </summary>
        /// <param name="b">二元表达式节点</param>
        /// <returns></returns>
        public virtual Expression VisitCoalesce(BinaryExpression b)
        {
            // 例： a.Name ?? "TAN" => ISNULL(a.Name,'TAN')
            Expression left = b.Left.NodeType == ExpressionType.Constant ? b.Right : b.Left;
            Expression right = b.Left.NodeType == ExpressionType.Constant ? b.Left : b.Right;

            _builder.Append("ISNULL(");
            _visitor.Visit(left);
            _builder.Append(',');
            _visitor.Visit(right);
            _builder.Append(')');


            return b;
        }

        /// <summary>
        /// 访问表示方法调用的节点
        /// </summary>
        /// <param name="node">方法调用节点</param>
        /// <returns></returns>
        public virtual Expression VisitMethodCall(MethodCallExpression node)
        {
            Type type = node.Method.ReflectedType != null ? node.Method.ReflectedType : node.Method.DeclaringType;
            object visitor = this.InternalVisitors.GetMethodCallVisitor(type, _provider, _visitor);
            TypeRuntimeInfo typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo(visitor.GetType(), true);
            MemberInvokerBase invoker = typeRuntime.GetInvoker("Visit" + node.Method.Name);
            if (invoker == null) throw new XFrameworkException("{0}.{1} is not supported.", node.Method.DeclaringType, node.Method.Name);
            else
            {
                object exp = invoker.Invoke(this, new object[] { node });
                return exp as Expression;
            }
        }

        /// <summary>
        /// 访问表示字段或者属性的属性的节点 a.Name.Length
        /// </summary>
        public virtual Expression VisitMemberMember(MemberExpression node)
        {
            object visitor = this.InternalVisitors.GetMethodCallVisitor(typeof(string), _provider, _visitor);
            TypeRuntimeInfo typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo(visitor.GetType(), true);
            MemberInvokerBase invoker = typeRuntime.GetInvoker("Visit" + node.Member.Name);
            if (invoker == null) throw new XFrameworkException("{0}.{1} is not supported.", node.Member.DeclaringType, node.Member.Name);
            else
            {
                object exp = invoker.Invoke(this, new object[] { node });
                return exp as Expression;
            }
        }

        /// <summary>
        /// 访问一元运算符
        /// </summary>
        /// <param name="node">一元运算符表达式</param>
        /// <returns></returns>
        public virtual Expression VisitUnary(UnaryExpression node)
        {
            //if (node.NodeType == ExpressionType.Convert && node.Type != node.Operand.Type && node.Operand.Type != typeof(char))
            //{

            //}
            _visitor.Visit(node.Operand);
            return node;
        }

        #endregion
    }
}
