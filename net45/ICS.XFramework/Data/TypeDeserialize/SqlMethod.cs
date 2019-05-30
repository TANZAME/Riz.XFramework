using System;
using System.Linq.Expressions;

namespace ICS.XFramework.Data
{
    /// <summary>
    /// 定义 ORM 所支持的 SQL 方法
    /// </summary>
    public class SqlMethod
    {
        /// <summary>
        /// 解析成行号
        /// </summary>
        public static T RowNumber<T>(Expression<Func<object, object>> order)
        {
            return default(T);
        }
    }
}
