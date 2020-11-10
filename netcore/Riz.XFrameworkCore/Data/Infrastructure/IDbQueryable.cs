
using System.Data;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Riz.XFramework.Data
{
    /// <summary>
    /// 提供对未指定数据类型的特定数据源的查询进行计算的功能
    /// </summary>
    public interface IDbQueryable
    {
        /// <summary>
        /// 数据查询提供者
        /// </summary>
        IDbQueryProvider Provider { get; }

        /// <summary>
        /// 查询表达式
        /// </summary>
        ReadOnlyCollection<DbExpression> DbExpressions { get; }

        /// <summary>
        /// 批量插入信息
        /// </summary>
        BulkInsertInfo Bulk { get; set; }

        /// <summary>
        /// 当前查询上下文
        /// </summary>
        IDbContext DbContext { get; }

        /// <summary>
        /// 获取或设置该查询是否需要参数化 
        /// 此属性用于设置单条查询语义在解析时是否需要参数化
        /// </summary>
        /// <remarks>
        /// 批量插入数据不需要参数化
        /// </remarks>
        bool Parameterized { get; set; }

        /// <summary>
        /// Parameterized 属性是否已被设置
        /// </summary>
        bool HasSetParameterized { get; }

        /// <summary>
        /// 解析成 SQL 命令
        /// </summary>
        DbRawCommand Translate();

        /// <summary>
        /// 解析成 SQL 命令
        /// </summary>
        /// <param name="indent">缩进</param>
        /// <param name="isOutQuery">是否最外层，内层查询不需要结束符(;)</param>
        /// <param name="context">解析SQL命令上下文</param>
        /// <returns></returns>
        DbRawCommand Translate(int indent, bool isOutQuery, ITranslateContext context);

        /// <summary>
        /// 执行查询
        /// </summary>
        /// <returns></returns>
        TResult Execute<TResult>();

#if !net40

        /// <summary>
        /// 执行查询
        /// </summary>
        /// <returns></returns>
        Task<TResult> ExecuteAsync<TResult>();

#endif

        /// <summary>
        /// 解析查询语义
        /// </summary>
        DbQueryTree Parse();
    }
}
