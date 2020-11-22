
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
        /// 字符串表示形式。无参数，主要用于调式
        /// </summary>
        string Sql { get; }

        /// <summary>
        /// 解析成 SQL 命令
        /// </summary>
        DbRawCommand Translate();
    }
}
