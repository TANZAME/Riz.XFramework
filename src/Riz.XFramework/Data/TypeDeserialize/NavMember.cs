
using System.Data;
using System.Reflection;
using System.Linq.Expressions;

namespace Riz.XFramework.Data
{
    /// <summary>
    /// 导航属性
    /// </summary>
    public class NavMember : IStringKey
    {
        private string _keyId;
        private MemberExpression _expression;

        /// <summary>
        /// 唯一键
        /// </summary>
        public string Key { get { return _keyId; } }

        /// <summary>
        /// 访问属性或字段的表达式
        /// </summary>
        public MemberExpression Expression { get { return _expression; } }

        /// <summary>
        /// 筛选谓词（在 LEFT JOIN 片断中使用，其它地方忽略）
        /// </summary>
        public Expression Predicate { get; set; }

        /// <summary>
        /// 实例化 <see cref="NavMember"/> 类的新实例
        /// </summary>
        /// <param name="key">导航属性唯一键</param>
        /// <param name="expression">字段表达式</param>
        public NavMember(string key, MemberExpression expression)
        {
            this._keyId = key;
            this._expression = expression;
        }
    }
}
