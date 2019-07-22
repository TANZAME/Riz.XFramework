
using System.Data;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// 数据上下文，表示 Xfw 框架的主入口点
    /// </summary>
    public partial interface IInernalDbContext
    {
        /// <summary>
        /// 当前上下文的参数
        /// </summary>
        List<IDbDataParameter> Parameters { get; set; }

        /// <summary>
        /// 表别名缓存
        /// </summary>
        TableAliasCache TableAlias { get; set; }
    }
}
