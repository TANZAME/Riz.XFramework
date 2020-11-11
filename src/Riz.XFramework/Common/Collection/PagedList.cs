
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
namespace Riz.XFramework
{
    /// <summary>
    /// 分页数据列表
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [DataContract]
    public class PagedList<T> : IPagedList
    {
        /// <summary>
        /// 数据项
        /// </summary>
        [DataMember]
        public IList<T> Items { get; set; }

        /// <summary>
        /// 总页数
        /// </summary>
        [DataMember]
        public int Pages { get; set; }

        /// <summary>
        /// 记录总数
        /// </summary>
        [DataMember]
        public int RowCount { get; set; }

        /// <summary>
        /// 当前页码
        /// </summary>
        [DataMember]
        public int PageIndex { get; set; }

        /// <para>
        /// 注意：1024是一个特殊值，表示不分页查询所有
        /// </para>
        [DataMember]
        public int PageSize { get; set; }

        ///// <summary>
        ///// 是否有上一页
        ///// </summary>
        //[DataMember]
        //public bool HasPreviousPage
        //{
        //    get
        //    {
        //        return this.PageIndex > 1;
        //    }
        //}

        ///// <summary>
        ///// 是否有下一页
        ///// </summary>
        //[DataMember]
        //public bool HasNextPage
        //{
        //    get
        //    {
        //        return this.PageIndex < this.Pages;
        //    }
        //}

        /// <summary>
        /// 获取或设置位于指定索引处的元素
        /// </summary>
        /// <param name="index">索引</param>
        /// <returns></returns>
        public T this[int index] => this.Items[index];

        /// <summary>
        /// 初始化 PagedList 类的新实例
        /// </summary>
        public PagedList()
          : this(new T[0], 1, 10, 0)
        {
        }

        /// <summary>
        /// 初始化 PagedList 类的新实例
        /// </summary>
        public PagedList(IList<T> source, int index, int pageSize, int totalCount)
        {
            this.Items = source;
            if (index < 1) index = 1;
            this.PageSize = pageSize;
            this.PageIndex = index;
            this.RowCount = totalCount;
            this.Pages = this.RowCount / this.PageSize;
            if (this.RowCount > this.Pages * this.PageSize) this.Pages = this.Pages + 1;

            if (this.PageIndex > this.RowCount)
            {
                this.PageIndex = this.RowCount;
            }
        }
    }
}
