using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Riz.XFramework.Data
{
    /// <summary>
    /// 字段表达式
    /// </summary>
    public class PropertyExpression
    {
        /// <summary>
        /// 实例参数
        /// </summary>
        public ParameterExpression Parameter { get; set; }

        /// <summary>
        /// 字段名称
        /// </summary>
        public string PropertyName { get; set; }

        /// <summary>
        /// 实例化 <see cref="PropertyExpression"/> 类的新实例
        /// </summary>
        /// <param name="parameter">参数</param>
        /// <param name="propertyName">字段名</param>
        public PropertyExpression(ParameterExpression parameter, string propertyName)
        {
            this.Parameter = parameter;
            this.PropertyName = propertyName;
        }
    }
}
