
using System.Data;
using System.Reflection;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// 导航属性描述信息，用于字段-实体映射
    /// <para>
    /// 包括：导航属性名称以及在<see cref="IDataRecord"/>中的索引范围
    /// </para>
    /// </summary>
    public class NavDescriptor
    {
        /// <summary>
        /// 导航属性名称
        /// </summary>
        public string Name
        {
            get
            {
                return this.Member.Name;
            }
        }

        private string _keyName;
        /// <summary>
        /// 全名称
        /// </summary>
        public string KeyName { get { return _keyName; } }

        private MemberInfo _navMember = null;
        /// <summary>
        /// 导航属性对应
        /// </summary>
        public MemberInfo Member
        {
            get { return _navMember; }
        }

        /// <summary>
        /// 对应 <see cref="IDataRecord"/> 的索引
        /// <para>
        /// 表示从这个位置开始到 End 位置的所有字段都是属于该导航属性
        /// </para>
        /// </summary>
        public int StartIndex { get; set; }

        /// <summary>
        /// 导航属性的字段个数
        /// </summary>
        public int FieldCount { get; set; }

        /// <summary>
        /// 实例化<see cref="NavDescriptor"/>类的新实例
        /// </summary>
        public NavDescriptor(string keyName, MemberInfo member)
        {
            _keyName = keyName;
            _navMember = member;
        }
    }
}
