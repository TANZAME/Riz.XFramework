using System;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Linq.Expressions;
using System.Collections.Generic;

namespace Riz.XFramework.Data
{
    /// <summary>
    /// 表达式扩展方法
    /// </summary>
    internal static class ExpressionExtensions
    {
        private static readonly string _anonymousName = "<>h__TransparentIdentifier";
        private static readonly Func<Type, bool> _isGrouping = t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IGrouping<,>);
        private static readonly Func<string, bool> _isAnonymous = name => !string.IsNullOrEmpty(name) && name.StartsWith(_anonymousName, StringComparison.Ordinal);

        /// <summary>
        /// 聚合函数方法
        /// </summary>
        public static IDictionary<DbExpressionType, string> Aggregates = new Dictionary<DbExpressionType, string>
        {
            { DbExpressionType.Count,"COUNT" },
            { DbExpressionType.Max,"MAX" },
            { DbExpressionType.Min,"MIN" },
            { DbExpressionType.Average,"AVG" },
            { DbExpressionType.Sum,"SUM" }
        };

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
        /// 确定节点是否能够被继续递归访问（非根节点）
        /// </summary>
        public static bool IsChildNode(this Expression node)
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
        public static string GetKeyWidthoutAnonymous(this MemberExpression node, bool isInclude = false)
        {
            List<string> members = new List<string>();
            members.Add(node.Member.Name);

            Expression expression = node.Expression;
            while (expression.IsChildNode())
            {
                MemberExpression m = null;
                if (expression.NodeType == ExpressionType.MemberAccess) m = (MemberExpression)expression;
                else if (expression.NodeType == ExpressionType.Call) m = (expression as MethodCallExpression).Object as MemberExpression;

                members.Add(m.Member.Name);
                expression = m.Expression;
            }

            // 如果读取
            if (expression.NodeType == ExpressionType.Parameter) 
                members.Add(isInclude ? expression.Type.Name : (expression as ParameterExpression).Name);
            else if (expression.NodeType == ExpressionType.MemberAccess) 
                members.Add((expression as MemberExpression).Member.Name);

            members.Reverse();
            string result = string.Join(".", members);
            return result;
        }
    }
}
