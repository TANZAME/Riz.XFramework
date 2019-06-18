
using System;
using System.Data;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// 数据上下文，表示 Xfw 框架的主入口点
    /// </summary>
    public partial class DbContextBase : IDisposable
    {
        /// <summary>
        /// 计算要插入、更新或删除的已修改对象的集，并执行相应命令以实现对数据库的更改
        /// </summary>
        /// <returns></returns>
        public virtual async Task<int> SubmitChangesAsync()
        {
            int rowCount = _dbQueryables.Count;
            if (rowCount == 0) return 0;

            List<DbCommandDefinition> sqlList = _provider.Resolve(_dbQueryables);
            List<int> identitys = await _database.SubmitAsync(sqlList);
            // 回写自增列的ID值
            SetAutoIncrementValue(_dbQueryables, identitys);
            this.InternalDispose();

            return rowCount;
        }
    }
}
