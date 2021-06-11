
using System;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.Reflection;

namespace Riz.XFramework
{
    /// <summary>
    /// 扩展帮助类
    /// </summary>
    public static class XFrameworkExtensions
    {
        private static readonly MemberInfo _dateTimeNow = typeof(DateTime).GetMember("Now", MemberTypes.Property, BindingFlags.Public | BindingFlags.Static)[0];
        private static readonly MemberInfo _dateTimeUtcNow = typeof(DateTime).GetMember("UtcNow", MemberTypes.Property, BindingFlags.Public | BindingFlags.Static)[0];
        private static readonly MemberInfo _dateTimeToday = typeof(DateTime).GetMember("Today", MemberTypes.Property, BindingFlags.Public | BindingFlags.Static)[0];

        #region 表达式树

        /// <summary>
        /// 返回真表达式
        /// </summary>
        public static Expression<Func<T, bool>> True<T>() where T : class => f => true;

        /// <summary>
        /// 返回假表达式
        /// </summary>
        public static Expression<Func<T, bool>> False<T>() where T : class => f => false;

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
                bool isDateTime = memberExpression.Type == typeof(DateTime) &&
                    (memberExpression.Member == _dateTimeNow || memberExpression.Member == _dateTimeUtcNow || memberExpression.Member == _dateTimeToday);
                return !isDateTime;
            }
            if (memberExpression.Expression.NodeType == ExpressionType.Constant) return true;

            return memberExpression.Expression.CanEvaluate();
        }

        /// <summary>
        /// 计算表达式的值
        /// </summary>
        public static ConstantExpression Evaluate(this Expression node)
        {
            // TODO 缓存常量表达式

            ConstantExpression constantExpression = null;
            if (node.NodeType == ExpressionType.Constant) constantExpression = node as ConstantExpression;
            else
            {
                LambdaExpression lambda = node is LambdaExpression ? Expression.Lambda(((LambdaExpression)node).Body) : Expression.Lambda(node);
                Delegate fn = lambda.Compile();
                constantExpression = Expression.Constant(fn.DynamicInvoke(null), node is LambdaExpression ? ((LambdaExpression)node).Body.Type : node.Type);
            }

            // 枚举要转成 INT
            if (constantExpression.Type.IsEnum)
                constantExpression = Expression.Constant(Convert.ToInt32(constantExpression.Value));
            // 返回最终处理的常量表达式s
            return constantExpression;
        }

        /// <summary>
        /// 判断是否是访问 List`1 类的索引的表达式
        /// </summary>
        /// <param name="node">表示对静态方法或实例方法的调用表达式</param>
        /// <returns></returns>
        internal static bool IsCollectionIndex(this MethodCallExpression node)
        {
            if (node == null) return false;
            Expression objExpression = node.Object;
            bool result = objExpression != null && Data.TypeUtils.IsCollectionType(objExpression.Type) && node.Method.Name == "get_Item";
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
        /// 对集合中的每个元素执行指定操作
        /// </summary>
        public static void ForEach<T>(this IEnumerable<T> collection, Action<T> action)
        {
            if (action == null) return;
            foreach (var item in collection) action.Invoke(item);
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
        public static List<T> ToList<T>(this IEnumerable<T> source, Func<T, bool> predicate)
        {
            if (predicate != null) source = source.Where(predicate);
            return source.ToList();
        }

        /// <summary>
        /// 列表转换扩展
        /// </summary>
        public static List<TResult> ToList<T, TResult>(this IEnumerable<T> source, Func<T, bool> predicate, Func<T, TResult> selector)
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
