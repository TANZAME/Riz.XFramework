
using System.Data;
using System.Collections.Generic;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// 解析上下文携带参数
    /// </summary>
    public class ParserToken
    {
        /// <summary>
        /// 参数列表
        /// </summary>
        public List<IDbDataParameter> Parameters { get; set; }

        /// <summary>
        /// 表别名
        /// <para>用于翻译 IDbQueryable.Contains 语法</para>
        /// </summary>
        public string TableAliasName { get; set; }

        /// <summary>
        /// 表别名缓存
        /// </summary>
        public TableAliasCache TableAlias { get; set; }
    }
}
