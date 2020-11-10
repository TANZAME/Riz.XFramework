
namespace Riz.XFramework
{
    /// <summary>
    /// 分页列表接口
    /// </summary>
    public interface IPagedList
    {
        /// <summary>
        /// 当前页码
        /// </summary>
        int PageIndex { get; set; }

        /// <summary>
        /// 页长
        /// <para>
        /// 注意：1024是一个特殊值，表示不分页查询所有
        /// </para>
        /// </summary>
        int PageSize { get; set; }

        /// <summary>
        /// 记录总数
        /// </summary>
        int RowCount { get; set; }

        /// <summary>
        /// 总页数
        /// </summary>
        int Pages { get; set; }

        ///// <summary>
        ///// 能否进行上一次查询
        ///// </summary>
        //bool HasPreviousPage { get; }

        ///// <summary>
        ///// 能否进行下一页查询
        ///// </summary>
        //bool HasNextPage { get; }
    }
}
