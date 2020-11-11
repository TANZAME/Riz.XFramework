
using System;
using System.Text;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Riz.XFramework.Data
{
    /// <summary>
    /// 默认SQL 语句建造器
    /// </summary>
    public sealed class DefaultSqlBuilder : SqlBuilder
    {
        /// <summary>
        /// 实例化 <see cref="DefaultSqlBuilder"/> 类的新实例
        /// </summary>
        /// <param name="context">解析SQL命令上下文</param>
        public DefaultSqlBuilder(ITranslateContext context)
            : base(context)
        {

        }
    }
}
