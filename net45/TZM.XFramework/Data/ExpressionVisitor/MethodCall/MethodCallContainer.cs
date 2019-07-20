using System;
using System.Collections.Generic;

namespace TZM.XFramework.Data.Internal
{
    /// <summary>
    /// 解析方法调用的服务容器
    /// </summary>
    public class MethodCallExressionVisitorContainer
    {
        private IDictionary<Type, Func<IDbQueryProvider, ExpressionVisitorBase, object>> _container = null;

        /// <summary>
        /// 添加一个解析类
        /// </summary>
        public void Add(Type declareType, Func<IDbQueryProvider, ExpressionVisitorBase, object> func)
        {
            if (_container == null) _container = new Dictionary<Type, Func<IDbQueryProvider, ExpressionVisitorBase, object>>(6);
            _container.Add(declareType, func);
        }

        /// <summary>
        /// 替换一个解析类
        /// </summary>
        public void Replace(Type declareType, Func<IDbQueryProvider, ExpressionVisitorBase, object> func)
        {
            if (_container == null) _container = new Dictionary<Type, Func<IDbQueryProvider, ExpressionVisitorBase, object>>(6);
            _container[declareType] = func;
        }

        /// <summary>
        /// 获取解析服务
        /// </summary>
        /// <param name="declareType"></param>
        public object GetMethodCallVisitor(Type declareType, IDbQueryProvider provider, ExpressionVisitorBase visitor)
        {
            return _container == null ? null : _container[declareType](provider, visitor);
        }
    }
}
