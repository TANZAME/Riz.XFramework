
using System;
using System.Data;
using System.Collections.Generic;

namespace Riz.XFramework.Data
{
    /// <summary>
    /// 解析SQL命令上下文
    /// </summary>
    public class TranslateContext : ITranslateContext
    {
        private readonly IDbContext _context = null;
        private readonly DbQueryProvider _provider = null;

        /// <summary>
        /// 当前正在翻译的表达式类型
        /// </summary>
        public DbExpressionType? CurrentExpressionType { get; set; }

        /// <summary>
        /// 当前正在翻译最外层查询。 
        /// SELECT 语义最外层需要区别翻译 MemberInfo.Name 和 ColumnAttribute.Name => ColumnAttribute.Name As [MemberInfo.Name]
        /// </summary>
        public bool? CurrentIsOutermost { get; set; }

        /// <summary>
        /// 参数列表
        /// </summary>
        public IList<IDbDataParameter> Parameters { get; set; }

        /// <summary>
        /// 表别名前缀
        /// <para>用于翻译 IDbQueryable.Contains 语法</para>
        /// </summary>
        public string AliasPrefix { get; set; }

        /// <summary>
        /// 当前查询上下文
        /// </summary>
        public IDbContext DbContext => _context;

        /// <summary>
        ///  查询语义提供者。代理 DbContext 的 Provider
        /// </summary>
        public IDbQueryProvider Provider => _provider;

        /// <summary>
        /// 实例化 <see cref="TranslateContext"/> 类的新实例
        /// </summary>
        /// <param name="context">当前查询上下文</param>
        public TranslateContext(IDbContext context)
        {
            XFrameworkException.Check.NotNull(context, nameof(context));
            _context = context;
            _provider = (DbQueryProvider)_context.Provider;
        }

        /// <summary>
        /// 使用新的别名拷贝一个新的上下文
        /// </summary>
        /// <param name="source"></param>
        /// <param name="prifix"></param>
        /// <returns></returns>
        public static ITranslateContext Copy(ITranslateContext source, string prifix)
        {
            var context = source.DbContext;
            var provider = source.DbContext.Provider;

            var result = ((DbQueryProvider)provider).CreateTranslateContext(context);
            result.Parameters = source.Parameters;
            result.AliasPrefix = prifix;
            result.CurrentExpressionType = source.CurrentExpressionType;
            result.CurrentIsOutermost = source.CurrentIsOutermost;

            return result;
        }
    }
}
