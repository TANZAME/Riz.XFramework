
using System.Data;
using System.Collections.Generic;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// 解析上下文
    /// </summary>
    public class ResolveScope
    {
        /// <summary>
        /// 参数列表
        /// </summary>
        public List<IDbDataParameter> Parameters { get; set; }

        /// <summary>
        /// 已使用别名数量
        /// </summary>
        public int AliasTaked { get; set; }
    }
}
