
using System;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Linq.Expressions;
using System.Collections.Generic;

namespace Riz.XFramework
{
    /// <summary>
    /// 扩展帮助类
    /// </summary>
    public static class XFrameworkExtensions
    {
        #region 表达式树

        private static readonly string _anonymousName = "<>h__TransparentIdentifier";
        private static Func<Type, bool> _isGrouping = t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IGrouping<,>);
        private static Func<string, bool> _isAnonymous = name => !string.IsNullOrEmpty(name) && name.StartsWith(_anonymousName, StringComparison.Ordinal);
        private static MemberInfo _now = typeof(DateTime).GetMember("Now", MemberTypes.Property, BindingFlags.Public | BindingFlags.Static)[0];
        private static MemberInfo _utcNow = typeof(DateTime).GetMember("UtcNow", MemberTypes.Property, BindingFlags.Public | BindingFlags.Static)[0];
        private static MemberInfo _today = typeof(DateTime).GetMember("Today", MemberTypes.Property, BindingFlags.Public | BindingFlags.Static)[0];

        /// <summary>
        /// 返回真表达式
        /// </summary>
        public static Expression<Func<T, bool>> True<T>()
            where T : class
        {
            return f => true;
        }

        /// <summary>
        /// 返回假表达式
        /// </summary>
        public static Expression<Func<T, bool>> False<T>()
            where T : class
        {
            return f => false;
        }

        /// <summary>
        /// 拼接真表达式
        /// </summary>
        public static Expression<Func<T, bool>> AndAlso<T>(this Expression<Func<T, bool>> left, Expression<Func<T, bool>> right) where T : class
        {
            if (left == null) return right;
            if (right == null) return left;

            var expression = Expression.Invoke(right, left.Parameters);
            return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(left.Body, expression), left.Parameters);
        }

        /// <summary>
        /// 拼接假表达式
        /// </summary>
        public static Expression<Func<T, bool>> OrElse<T>(this Expression<Func<T, bool>> left,
            Expression<Func<T, bool>> right) where T : class
        {
            if (left == null) return right;
            if (right == null) return left;

            var expression = Expression.Invoke(right, left.Parameters);
            return Expression.Lambda<Func<T, bool>>(Expression.OrElse(left.Body, expression), left.Parameters);
        }

        /// <summary>
        /// 去掉一元表达式的操作符
        /// </summary>
        /// <returns></returns>
        public static Expression ReduceUnary(this Expression exp)
        {
            var unaryExpression = exp as UnaryExpression;
            return unaryExpression != null
                ? unaryExpression.Operand.ReduceUnary()
                : exp;
        }

        /// <summary>
        /// 判断表达式链是否能通过动态计算，计算出它的值
        /// </summary>
        public static bool CanEvaluate(this Expression node)
        {
            // => 5
            // => a.ActiveDate == DateTime.Now
            // => a.State == (byte)state
            // => a.Accounts[0].Markets[0].MarketId

            if (node == null) return false;
            if (node.NodeType == ExpressionType.Constant) return true;
            if (node.NodeType == ExpressionType.ArrayIndex) return true;
            if (node.NodeType == ExpressionType.Call)
            {
                // List<int>{0}[]
                // => a.Accounts[0].Markets[0].MarketId
                MethodCallExpression methodExpression = node as MethodCallExpression;
                bool isIndex = methodExpression.IsCollectionIndex();
                if (isIndex) node = methodExpression.Object;
            }

            if (node.NodeType == ExpressionType.ListInit) return true;
            if (node.NodeType == ExpressionType.NewArrayInit) return true;
            if (node.NodeType == ExpressionType.NewArrayBounds) return true;
            if (node.NodeType != ExpressionType.MemberAccess) return false;

            var memberExpression = node as MemberExpression;
            if (memberExpression == null) return false;
            if (memberExpression.Expression == null)
            {
                // 排除 DateTime 的几个常量
                bool isDateTime = memberExpression.Type == typeof(DateTime) && (memberExpression.Member == _now || memberExpression.Member == _utcNow || memberExpression.Member == _today);
                return !isDateTime;
            }
            if (memberExpression.Expression.NodeType == ExpressionType.Constant) return true;

            return memberExpression.Expression.CanEvaluate();
        }

        /// <summary>
        /// 判断是否是访问 List`1 类的索引的表达式
        /// </summary>
        /// <param name="node">表示对静态方法或实例方法的调用表达式</param>
        /// <returns></returns>
        public static bool IsCollectionIndex(this MethodCallExpression node)
        {
            if (node == null) return false;
            Expression objExpression = node.Object;
            bool result = objExpression != null && Data.TypeUtils.IsCollectionType(objExpression.Type) && node.Method.Name == "get_Item";
            return result;
        }

        /// <summary>
        /// 计算表达式的值
        /// </summary>
        public static ConstantExpression Evaluate(this Expression node)
        {
            ConstantExpression constantExpression = null;
            if (node.NodeType == ExpressionType.Constant) constantExpression = node as ConstantExpression;
            else
            {
                LambdaExpression lambda = node is LambdaExpression ? Expression.Lambda(((LambdaExpression)node).Body) : Expression.Lambda(node);
                Delegate fn = lambda.Compile();
                constantExpression = Expression.Constant(fn.DynamicInvoke(null), node is LambdaExpression ? ((LambdaExpression)node).Body.Type : node.Type);
            }

            // 枚举要转成 INT
            if (constantExpression.Type.IsEnum) constantExpression = Expression.Constant(Convert.ToInt32(constantExpression.Value));
            // 返回最终处理的常量表达式s
            return constantExpression;
        }

        /// <summary>
        /// 判断属性访问表达式是否有系统动态生成前缀
        /// <code>
        /// h__TransparentIdentifier.a.CompanyName
        /// </code>
        /// </summary>
        public static bool IsAnonymous(this Expression node)
        {
            // <>h__TransparentIdentifier => h__TransparentIdentifier.a.CompanyName
            Expression exp = node;
            ParameterExpression parameterExpression = exp.NodeType == ExpressionType.Lambda
                ? (node as LambdaExpression).Parameters[0]
                : exp as ParameterExpression;
            if (parameterExpression != null) return _isAnonymous(parameterExpression.Name);

            // <>h__TransparentIdentifier.a.CompanyName
            if (exp.NodeType == ExpressionType.MemberAccess)
            {
                MemberExpression memberExpression = exp as MemberExpression;
                if (_isAnonymous(memberExpression.Member.Name)) return true;

                return IsAnonymous(memberExpression.Expression);
            }

            return false;
        }

        /// <summary>
        /// 判断是否是分组表达式
        /// </summary>
        public static bool IsGrouping(this Expression node)
        {
            //g.Key
            //g.Key.CompanyName
            //g.Max()
            //g=>g.xxx
            //g.Key.CompanyId.Length
            //g.Key.Length 

            // g | g=>g.xx
            Expression exp = node;
            ParameterExpression parameterExpression = exp.NodeType == ExpressionType.Lambda
                ? (node as LambdaExpression).Parameters[0]
                : exp as ParameterExpression;
            if (parameterExpression != null) return _isGrouping(parameterExpression.Type);

            // g.Max
            MethodCallExpression methodExpression = exp as MethodCallExpression;
            if (methodExpression != null) return _isGrouping(methodExpression.Arguments[0].Type);


            MemberExpression memExpression = exp as MemberExpression;
            if (memExpression != null)
            {
                // g.Key
                var g = memExpression.Member.Name == "Key" && _isGrouping(memExpression.Expression.Type);
                if (g) return g;

                // g.Key.Length | g.Key.Company | g.Key.CompanyId.Length
                memExpression = memExpression.Expression as MemberExpression;
                if (memExpression != null)
                {
                    g = memExpression.Member.Name == "Key" && _isGrouping(memExpression.Expression.Type);// && memExpression.Type.Namespace == null; //匿名类没有命令空间
                    if (g) return g;
                }
            }

            return false;
        }

        /// <summary>
        /// 确定节点是否能够被继续递归访问
        /// </summary>
        public static bool Visitable(this Expression node)
        {
            // a 
            // <>h__TransparentIdentifier.a
            // <>h__TransparentIdentifier0.<>h__TransparentIdentifier1.a

            if (node.NodeType == ExpressionType.Parameter) return false;

            var m = node as MemberExpression;
            if (m != null)
            {
                if (m.Expression == null) return false;

                if (m.Expression.NodeType == ExpressionType.Parameter)
                {
                    string name = (m.Expression as ParameterExpression).Name;
                    if (_isAnonymous(name)) return false;
                }

                if (m.Expression.NodeType == ExpressionType.MemberAccess)
                {
                    string name = (m.Expression as MemberExpression).Member.Name;
                    if (_isAnonymous(name)) return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 取剔除掉系统动态生成前缀后的表达式
        /// </summary>
        public static string GetKeyWidthoutAnonymous(this MemberExpression node, bool isDescriptor = false)
        {
            List<string> segs = new List<string>();
            segs.Add(node.Member.Name);

            Expression expression = node.Expression;
            while (expression.Visitable())
            {
                MemberExpression memberExpression = null;
                if (expression.NodeType == ExpressionType.MemberAccess) memberExpression = (MemberExpression)expression;
                else if (expression.NodeType == ExpressionType.Call) memberExpression = (expression as MethodCallExpression).Object as MemberExpression;

                segs.Add(memberExpression.Member.Name);
                expression = memberExpression.Expression;
            }

            // 如果读取
            if (expression.NodeType == ExpressionType.Parameter) segs.Add(isDescriptor ? expression.Type.Name : (expression as ParameterExpression).Name);
            else if (expression.NodeType == ExpressionType.MemberAccess) segs.Add((expression as MemberExpression).Member.Name);

            segs.Reverse();
            string result = string.Join(".", segs);
            return result;
        }

        #endregion

        #region 列表扩展

        /// <summary>
        /// 取指定列表中符合条件的元素索引
        /// </summary>
        public static int IndexOf<T>(this IEnumerable<T> collection, Func<T, bool> predicate)
        {
            int i = -1;
            foreach (T value in collection)
            {
                i++;
                if (predicate(value)) return i;
            }

            return -1;
        }

        /// <summary>
        /// 创建一个集合
        /// </summary>
        public static List<TResult> ToList<T, TResult>(this IEnumerable<T> collection, Func<T, TResult> selector)
        {
            return collection.Select(selector).ToList();
        }

        /// <summary>
        /// 列表转换扩展
        /// </summary>
        public static List<TResult> ToList<T, TResult>(this IEnumerable<T> source, Func<T, TResult> selector, Func<T, bool> predicate = null)
        {
            if (predicate != null) source = source.Where(predicate);
            return source.Select(selector).ToList();
        }

        /// <summary>
        /// 计算总页码
        /// </summary>
        /// <param name="collection">数据集合</param>
        /// <param name="pageSize">页码</param>
        /// <returns></returns>
        public static int Page<T>(this IEnumerable<T> collection, int pageSize)
        {
            int rowCount = 0;
            if ((collection as ICollection<T>) != null) rowCount = (collection as ICollection<T>).Count;
            else if ((collection as T[]) != null) rowCount = (collection as T[]).Length;
            else rowCount = collection.Count();

            return ~~((rowCount - 1) / pageSize) + 1;
        }

        /// <summary>
        /// 批量添加命令参数
        /// </summary>
        public static void AddRange(this IDataParameterCollection sources, IEnumerable<IDbDataParameter> parameters)
        {
            if (parameters != null)
            {
                foreach (var p in parameters) sources.Add(p);
            }
        }

        #endregion
    }
}
