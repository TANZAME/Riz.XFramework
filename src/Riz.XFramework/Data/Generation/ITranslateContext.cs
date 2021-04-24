
using System.Data;
using System.Collections.Generic;

namespace Riz.XFramework.Data
{
    /// <summary>
    /// 解析SQL命令上下文接口
    /// </summary>
    public interface ITranslateContext
    {
        /// <summary>
        /// 当前正在翻译的表达式类型
        /// </summary>
        DbExpressionType? DbExpressionType { get; set; }

        /// <summary>
        /// 当前正在翻译最外层查询。 
        /// SELECT 语义最外层需要区别翻译 MemberInfo.Name 和 ColumnAttribute.Name => ColumnAttribute.Name As [MemberInfo.Name]
        /// </summary>
        bool? IsOutermostQuery { get; set; }

        /// <summary>
        /// 参数列表
        /// </summary>
        IList<IDbDataParameter> Parameters { get; set; }

        /// <summary>
        /// 表别名前缀
        /// <para>用于翻译 IDbQueryable.Contains 语法</para>
        /// </summary>
        string AliasPrefix { get; set; }

        /// <summary>
        /// 当前查询上下文
        /// </summary>
        IDbContext DbContext { get; }

        /// <summary>
        /// 查询语义提供者。代理 DbContext 的 Provider
        /// </summary>
        IDbQueryProvider Provider { get; }

        /// <summary>
        /// 复制一个实例，简化创建代码 
        /// 注意默认不复制 AliasPrefix 属性
        /// </summary>
        /// <param name="newPrefix">指定一个新的表别名前缀，如果不是嵌套，传 null</param>
        /// <returns></returns>
        ITranslateContext Clone(string newPrefix);
    }
}
