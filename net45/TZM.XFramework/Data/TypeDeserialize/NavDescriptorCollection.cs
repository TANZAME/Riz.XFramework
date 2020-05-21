
using System.Collections;
using System.Collections.Generic;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// 导航属性描述集合
    /// </summary>
    public class NavDescriptorCollection : HashCollection<NavDescriptor>
    {
        private int? _minIndex;
        /// <summary>
        /// 所有导航属性的最小开始索引
        /// </summary>
        public int MinIndex { get { return _minIndex == null ? 0 : _minIndex.Value; } }

        /// <summary>
        /// 在集合中添加一个带有所提供的键和值的元素
        /// </summary>
        public override void Add(NavDescriptor nav)
        {
            base.Add(nav);
            if (nav != null && nav.FieldCount != 0)
            {
                if (_minIndex == null)
                {
                    _minIndex = nav.StartIndex;
                }
                else
                {
                    if (nav.StartIndex < _minIndex.Value) _minIndex = nav.StartIndex;
                }
            }
        }
    }
}
