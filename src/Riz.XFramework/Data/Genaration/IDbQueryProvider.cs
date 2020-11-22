
using System.Data;
using System.Data.Common;
using System.Collections.Generic;

namespace Riz.XFramework.Data
{
    /// <summary>
    /// 查询语义提供者，用来构建、解析查询语义
    /// </summary>
    public interface IDbQueryProvider
    {
        /// <summary>
        /// <see cref="IDbQueryProvider"/> 实例的名称
        /// </summary>
        string ProviderName { get; }

        /// <summary>
        /// 获取起始字符或字符指定其名称包含空格或保留的标记等字符的数据库对象 （例如，表或列） 时使用
        /// </summary>
        string QuotePrefix { get; }

        /// <summary>
        /// 获取结束字符或字符指定其名称包含空格或保留的标记等字符的数据库对象 （例如，表或列） 时使用。
        /// </summary>
        string QuoteSuffix { get; }

        /// <summary>
        /// 字符串单引号
        /// </summary>
        string SingleQuoteChar { get; }

        /// <summary>
        /// 命令参数前缀
        /// </summary>
        string ParameterPrefix { get; }

        /// <summary>
        /// 表示一组方法，这些方法用于创建提供程序对数据源类的实现的实例。
        /// </summary>
        DbProviderFactory DbProvider { get; }

        /// <summary>
        /// 解析 SQL 命令
        /// </summary>
        /// <param name="dbQueryables">查询语句</param>
        List<DbRawCommand> Translate(List<object> dbQueryables);


        /// <summary>
        /// 解析 SQL 命令
        /// </summary>
        /// <param name="dbQueryable">查询语句</param>
        DbRawCommand Translate<T>(IDbQueryable<T> dbQueryable);
    }
}
