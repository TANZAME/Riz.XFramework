
using System;
using System.Data;
using System.Collections.Generic;

namespace Riz.XFramework.Data
{
    /// <summary>
    /// 默认解析SQL命令上下文
    /// </summary>
    public sealed class DefaultTranslateContext : TranslateContext
    {
        /// <summary>
        /// 实例化 <see cref="DefaultTranslateContext"/> 类的新实例
        /// </summary>
        /// <param name="context">当前查询上下文</param>
        public DefaultTranslateContext(IDbContext context)
            : base(context)
        {

        }
    }
}
