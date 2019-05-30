
using System;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Collections.Generic;

namespace ICS.XFramework
{
    public static class XFrameworkExtensions
    {
        #region 表达式树

        private static readonly string _anonymousName = "<>h__TransparentIdentifier";
        private static Func<Type, bool> _isGrouping = t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IGrouping<,>);
        private static Func<string, bool> _isAnonymous = name => !string.IsNullOrEmpty(name) && name.StartsWith(_anonymousName, StringComparison.Ordinal);

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
        public static Expression<Func<T, bool>> AndAlso<T>(this Expression<Func<T, bool>> TExp1,
            Expression<Func<T, bool>> TExp2) where T : class
        {
            if (TExp1 == null) return TExp2;
            if (TExp2 == null) return TExp1;

            var invokeExp = System.Linq.Expressions.Expression.Invoke(TExp2, TExp1.Parameters.Cast<System.Linq.Expressions.Expression>());
            return System.Linq.Expressions.Expression.Lambda<Func<T, bool>>
                  (System.Linq.Expressions.Expression.AndAlso(TExp1.Body, invokeExp), TExp1.Parameters);
        }

        /// <summary>
        /// 拼接假表达式
        /// </summary>
        public static Expression<Func<T, bool>> OrElse<T>(this Expression<Func<T, bool>> TExp1,
            Expression<Func<T, bool>> TExp2) where T : class
        {
            if (TExp1 == null) return TExp2;
            if (TExp2 == null) return TExp1;

            var invokeExp = System.Linq.Expressions.Expression.Invoke(TExp2, TExp1.Parameters.Cast<System.Linq.Expressions.Expression>());
            return System.Linq.Expressions.Expression.Lambda<Func<T, bool>>
                  (System.Linq.Expressions.Expression.OrElse(TExp1.Body, invokeExp), TExp1.Parameters);
        }

        /// <summary>
        /// 去掉一元表达式的操作符
        /// </summary>
        /// <returns></returns>
        public static Expression ReduceUnary(this Expression exp)
        {
            UnaryExpression unaryExpression = exp as UnaryExpression;
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

            if (node == null) return false;
            if (node.NodeType == ExpressionType.Constant) return true;
            if (node.NodeType == ExpressionType.ArrayIndex) return true;
            if (node.NodeType == ExpressionType.Call)
            {
                // List<int>{0}[]
                MethodCallExpression methodCallExpression = node as MethodCallExpression;
                Expression expression = methodCallExpression.Object;
                //if (expression != null && expression.Type.IsGenericType && expression.Type.GetGenericTypeDefinition() == typeof(List<>) && methodCallExpression.Method.Name == "get_Item")
                if (expression != null && Data.TypeUtils.IsCollectionType(expression.Type) && methodCallExpression.Method.Name == "get_Item")
                {
                    return true;
                }
            }

            var member = node as MemberExpression;
            if (member == null) return false;
            if (member.Expression == null) return true;
            if (member.Expression.NodeType == ExpressionType.Constant) return true;

            return member.Expression.CanEvaluate();
        }

        /// <summary>
        /// 计算表达式的值
        /// </summary>
        public static ConstantExpression Evaluate(this Expression e)
        {
            ConstantExpression node = null;
            if (e.NodeType == ExpressionType.Constant)
            {
                node = e as ConstantExpression;
            }
            else
            {
                LambdaExpression lambda = e is LambdaExpression ? Expression.Lambda(((LambdaExpression)e).Body) : Expression.Lambda(e);
                Delegate fn = lambda.Compile();

                node = Expression.Constant(fn.DynamicInvoke(null), e is LambdaExpression ? ((LambdaExpression)e).Body.Type : e.Type);
            }

            // 枚举要转成 INT
            if (node.Type.IsEnum) node = Expression.Constant(Convert.ToInt32(node.Value));

            // 返回最终处理的常量表达式s
            return node;
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
            ParameterExpression paramExp = exp.NodeType == ExpressionType.Lambda
                ? (node as LambdaExpression).Parameters[0]
                : exp as ParameterExpression;
            if (paramExp != null) return _isAnonymous(paramExp.Name);

            if (exp.NodeType == ExpressionType.MemberAccess)    // <>h__TransparentIdentifier.a.CompanyName
            {
                MemberExpression memExp = exp as MemberExpression;
                if (_isAnonymous(memExp.Member.Name)) return true;

                return IsAnonymous(memExp.Expression);
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
            ParameterExpression paramExp = exp.NodeType == ExpressionType.Lambda
                ? (node as LambdaExpression).Parameters[0]
                : exp as ParameterExpression;
            if (paramExp != null) return _isGrouping(paramExp.Type);

            // g.Max
            MethodCallExpression callExp = exp as MethodCallExpression;
            if (callExp != null) return _isGrouping(callExp.Arguments[0].Type);


            MemberExpression memExp = exp as MemberExpression;
            if (memExp != null)
            {
                // g.Key
                var g1 = memExp.Member.Name == "Key" && _isGrouping(memExp.Expression.Type);
                if (g1) return g1;

                // g.Key.Length | g.Key.Company | g.Key.CompanyId.Length
                memExp = memExp.Expression as MemberExpression;
                if (memExp != null)
                {
                    g1 = memExp.Member.Name == "Key" && _isGrouping(memExp.Expression.Type) && memExp.Type.Namespace == null; //匿名类没有命令空间
                    if (g1) return g1;
                }
            }

            return false;
        }

        /// <summary>
        /// 在递归访问 MemberAccess 表达式时，判定节点是否能够被继续递归访问
        /// </summary>
        public static bool Acceptable(this Expression node)
        {
            // a 
            // <>h__TransparentIdentifier.a
            // <>h__TransparentIdentifier0.<>h__TransparentIdentifier1.a

            if (node.NodeType == ExpressionType.Parameter) return false;

            if (node.NodeType == ExpressionType.MemberAccess)
            {
                MemberExpression m = node as MemberExpression;
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
        public static string GetKeyWidthoutAnonymous(this MemberExpression node, bool isDesciptor = false)
        {
            List<string> segs = new List<string>();
            segs.Add(node.Member.Name);

            Expression expression = node.Expression;
            while (expression.Acceptable())
            {
                MemberExpression memberExpression = null;
                if (expression.NodeType == ExpressionType.MemberAccess) memberExpression = (MemberExpression)expression;
                else if (expression.NodeType == ExpressionType.Call) memberExpression = (expression as MethodCallExpression).Object as MemberExpression;

                segs.Add(memberExpression.Member.Name);
                expression = memberExpression.Expression;
            }

            // 如果读取
            if (expression.NodeType == ExpressionType.Parameter) segs.Add(isDesciptor ? expression.Type.Name : (expression as ParameterExpression).Name);
            if (expression.NodeType == ExpressionType.MemberAccess) segs.Add((expression as MemberExpression).Member.Name);

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
        /// 根据页长计算总页码
        /// </summary>
        /// <param name="collection">数据集合</param>
        /// <param name="pageSize">页码</param>
        /// <returns></returns>
        public static int Page<T>(this IEnumerable<T> collection, int pageSize)
        {
            int count = 0;
            if ((collection as ICollection<T>) != null) count = (collection as ICollection<T>).Count;
            else if ((collection as T[]) != null) count = (collection as T[]).Length;
            else count = collection.Count();

            int page = count % pageSize == 0 ? count / pageSize : (count / pageSize + 1);
            return page;
        }

        /// <summary>
        /// 批量添加命令参数
        /// </summary>
        public static void AddRange(this IDataParameterCollection source, IEnumerable<IDbDataParameter> parameters)
        {
            if (parameters != null)
            {
                foreach (var p in parameters) source.Add(p);
            }
        }

        #endregion
    }
}
