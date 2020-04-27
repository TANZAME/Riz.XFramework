
using System.Data;
using System.Collections.Generic;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// 解析SQL命令时的参数上下文
    /// </summary>
    public class ResolveToken
    {
        private bool _parameterized = false;
        private bool _hasSetParameterized = false;

        /// <summary>
        /// 获取或设置该查询是否需要参数化
        /// </summary>
        /// <remarks>
        /// 批量插入数据不需要参数化
        /// </remarks>
        public bool Parameterized
        {
            get { return _parameterized; }
            set
            {
                _parameterized = value;
                _hasSetParameterized = true;
            }
        }

        /// <summary>
        /// Parameterized 属性是否已被设置
        /// </summary>
        public bool HasSetParameterized
        {
            get { return _hasSetParameterized; }
        }

        /// <summary>
        /// 参数列表
        /// </summary>
        public List<IDbDataParameter> Parameters { get; set; }

        /// <summary>
        /// 表别名前缀
        /// <para>用于翻译 IDbQueryable.Contains 语法</para>
        /// </summary>
        public string AliasPrefix { get; set; }

        /// <summary>
        /// 调试模式，调试模式生成的SQL会有换行
        /// </summary>
        public bool IsDebug { get { return this.DbContext.IsDebug; } }

        /// <summary>
        /// 当前查询上下文
        /// </summary>
        public IDbContext DbContext { get; set; }

        /// <summary>
        /// 扩展参数
        /// </summary>
        public IDictionary<string, object> Extendsions { get; set; }
    }
}
