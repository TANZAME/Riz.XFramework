
namespace ICS.XFramework.Data
{
    /// <summary>
    /// 表达式类型
    /// </summary>
    public enum DbExpressionType
    {
        None,
        GetTable,
        All,
        Any,
        Average,
        Count,
        DefaultIfEmpty,
        Distinct,
        First,
        FirstOrDefault,
        
        /// <summary>
        /// 分组
        /// </summary>
        GroupBy,

        /// <summary>
        /// 左联
        /// </summary>
        GroupJoin,
        
        /// <summary>
        /// 右联
        /// </summary>
        GroupRightJoin,

        /// <summary>
        /// 内联
        /// </summary>
        Join,
        
        /// <summary>
        /// 包含外表
        /// </summary>
        Include,
        Max,
        Min,
        OrderBy,
        OrderByDescending,
        Select,
        SelectMany,
        Single,
        SingleOrDefault,
        Skip,
        Sum,
        Take,
        ThenBy,
        ThenByDescending,
        
        /// <summary>
        /// 过滤条件
        /// </summary>
        Where,
        
        /// <summary>
        /// 合集
        /// </summary>
        Union,
        
        /// <summary>
        /// 插入
        /// </summary>
        Insert,
        
        /// <summary>
        /// 删除
        /// </summary>
        Delete,
        
        /// <summary>
        /// 更新
        /// </summary>
        Update,
        
        /// <summary>
        /// 显示指定会子查询
        /// </summary>
        AsSubQuery,
    }
}
