
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
        /// SQL值片断生成器
        /// </summary>
        DbValueResolver DbResolver { get; }

        /// <summary>
        /// <see cref="IDataReader"/> 转实体映射器
        /// </summary>
        TypeDeserializerImpl TypeDeserializerImpl { get; }

        /// <summary>
        /// 创建解析命令上下文
        /// </summary>
        /// <param name="context">当前查询上下文</param>
        /// <returns></returns>
        ITranslateContext CreateTranslateContext(IDbContext context);

        /// <summary>
        /// 创建 SQL 构造器
        /// </summary>
        /// <param name="context">解析SQL命令上下文</param>
        /// <returns></returns>
        ISqlBuilder CreateSqlBuilder(ITranslateContext context);

        /// <summary>
        /// 创建方法表达式访问器
        /// </summary>
        /// <param name="visitor">表达式解析器（装饰）</param>
        /// <returns></returns>
        MethodCallExpressionVisitor CreateMethodCallVisitor(LinqExpressionVisitor visitor);

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

        /// <summary>
        /// 创建 SQL 命令
        /// </summary>
        /// <param name="dbQueryable">查询语句</param>
        /// <param name="indent">缩进</param>
        /// <param name="isOutQuery">是否最外层，内层查询不需要结束符(;)</param>
        /// <param name="context">解析SQL命令上下文</param>
        /// <returns></returns>
        DbRawCommand Translate<T>(IDbQueryable<T> dbQueryable, int indent, bool isOutQuery, ITranslateContext context);
    }
}
