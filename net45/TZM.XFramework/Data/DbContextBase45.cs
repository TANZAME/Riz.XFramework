
using System;
using System.Data;
using System.Data.Common;
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

            List<Command> sqlList = this.Provider.Resolve(_dbQueryables);
            List<int> identitys = new List<int>();
            IDataReader reader = null;

            try
            {
                Func<IDbCommand, Task<object>> func = async cmd =>
                {
                    reader = await this.Database.ExecuteReaderAsync(cmd);
                    TypeDeserializer deserializer = new TypeDeserializer(reader, null);
                    do
                    {
                        List<int> result = null;
                        deserializer.Deserialize<int>(out result);
                        if (result != null && result.Count > 0) identitys.AddRange(result);
                    }
                    while (reader.NextResult());

                    // 释放当前的reader
                    if (reader != null) reader.Dispose();

                    return null;
                };

                await this.Database.ExecuteAsync<object>(sqlList, func);
                this.SetAutoIncrementValue(_dbQueryables, identitys);
                return rowCount;
            }
            finally
            {
                if (reader != null) reader.Dispose();
                this.InternalDispose();
            }
        }
    }
}
