using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Riz.XFramework.Data
{
    /// <summary>
    /// 从库负载均衡提供者
    /// </summary>
    public interface ILoadingBalanceProvider
    {
        // 加权轮询算法
        // 1权重=1节点

        /// <summary>
        /// 获取从表配置
        /// </summary>
        /// <returns></returns>
        ReplicaConfiguration GetReplica();
    }
}
