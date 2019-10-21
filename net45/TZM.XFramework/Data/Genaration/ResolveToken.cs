
using System.Data;
using System.Collections.Generic;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// 解析SQL命令时的参数上下文
    /// </summary>
    public class ResolveToken
    {
        bool _isDebug = false;

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
        /// 调试模式，调试模式生成的SQL会有换行
        /// </summary>
        public bool IsDebug
        {
            get { return _isDebug; }
            set
            {
                _isDebug = value;
                this.HasSetIsDebug = true;
            }
        }

        /// <summary>
        /// 是否已经设置 IsDebug 属性
        /// </summary>
        public bool HasSetIsDebug { get; set; }

        /// <summary>
        /// 扩展参数
        /// </summary>
        public IDictionary<string,object> Extendsions { get; set; }
    }
}
