
using System.Data;
using System.Reflection;
using System.Linq.Expressions;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// 导航属性
    /// </summary>
    public class NavMember
    {
        private string _keyName;
        private MemberExpression _expression;
        private Expression _predicate;

        /// <summary>
        /// 全名称
        /// </summary>
        public string KeyName { get { return _keyName; } }

        /// <summary>
        /// 访问属性或字段的表达式
        /// </summary>
        public MemberExpression Expression { get { return _expression; } }

        /// <summary>
        /// 筛选谓词（在 LEFT JOIN 片断中使用，其它地方忽略）
        /// </summary>
        public Expression Predicate { get { return _predicate; } }

        /// <summary>
        /// 实例化 <see cref="NavMember"/> 类的新实例
        /// </summary>
        /// <param name="key">导航属性唯一键</param>
        /// <param name="expression">字段表达式</param>
        public NavMember(string key, MemberExpression expression)
            : this(key, expression, null)
        {
        }

        /// <summary>
        /// 实例化 <see cref="NavMember"/> 类的新实例
        /// </summary>
        /// <param name="key">导航属性唯一键</param>
        /// <param name="expression">字段表达式</param>
        /// <param name="predicate">筛选谓词</param>
        public NavMember(string key, MemberExpression expression, Expression predicate)
        {
            this._keyName = key;
            this._expression = expression;
            this._predicate = predicate;
        }
    }
}
