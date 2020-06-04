using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// 从库负载均衡提供者（默认使用加权轮询算法）
    /// </summary>
    public class LoadingBalanceProvider : ILoadingBalanceProvider
    {
        // 加权轮询算法
        // 1权重=1节点

        private List<ReplicaConfiguration> _configurations = null;
        private int _index = 0;

        /// <summary>
        /// 实例化 <see cref="LoadingBalanceProvider"/> 类的新实例
        /// </summary>
        /// <param name="configurations"></param>
        public LoadingBalanceProvider(IEnumerable<ReplicaConfiguration> configurations)
        {
            var source = configurations.OrderByDescending(a => a.Weight);
            _configurations = new List<ReplicaConfiguration>();
            foreach (var config in configurations)
            {
                for (int index = 1; index <= config.Weight; index++) _configurations.Add(config);
            }
        }

        /// <summary>
        /// 获取从表配置
        /// </summary>
        /// <returns></returns>
        public ReplicaConfiguration GetReplica()
        {
            _index += 1;
            if (_index >= _configurations.Count) _index = 0;
            return _configurations[_index];
        }
    }
}
