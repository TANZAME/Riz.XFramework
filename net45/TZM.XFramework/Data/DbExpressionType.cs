
namespace TZM.XFramework.Data
{
    /// <summary>
    /// 表达式类型
    /// </summary>
    public enum DbExpressionType
    {
        /// <summary>
        /// 未指定表达式类型
        /// </summary>
        None,

        /// <summary>
        /// 返回特定类型的对象的集合
        /// </summary>
        GetTable,

        /// <summary>
        /// 确定序列中的所有元素是否都满足条件
        /// </summary>
        All,

        /// <summary>
        /// 确定序列是否包含任何元素
        /// </summary>
        Any,

        /// <summary>
        /// 对序列应用累加器函数
        /// </summary>
        Average,

        /// <summary>
        /// 返回序列中的元素数量
        /// </summary>
        Count,

        /// <summary>
        /// 返回序列中的元素数量（不立即执行）
        /// </summary>
        LazyCount,

        /// <summary>
        /// 返回指定序列中的元素；如果序列为空，则返回单一实例集合中的类型参数的默认值
        /// </summary>
        DefaultIfEmpty,

        /// <summary>
        /// 通过使用默认的相等比较器对值进行比较，返回序列中的非重复元素
        /// </summary>
        Distinct,

        /// <summary>
        /// 返回序列中的第一个元素
        /// </summary>
        First,

        /// <summary>
        /// 返回序列中的第一个元素；如果序列中不包含任何元素，则返回默认值
        /// </summary>
        FirstOrDefault,

        /// <summary>
        /// 根据指定的键选择器函数对序列中的元素进行分组，并且通过使用指定的函数对每个组中的元素进行投影
        /// </summary>
        GroupBy,

        /// <summary>
        /// 基于键值等同性对两个序列的元素进行关联，并对结果进行分组。 使用默认的相等比较器对键进行比较（左关联）
        /// </summary>
        GroupJoin,

        /// <summary>
        /// 基于键值等同性对两个序列的元素进行关联，并对结果进行分组。 使用默认的相等比较器对键进行比较（右关联）
        /// </summary>
        GroupJoin2,

        /// <summary>
        /// 基于匹配键对两个序列的元素进行关联。 使用默认的相等比较器对键进行比较
        /// </summary>
        Join,

        /// <summary>
        /// 要在查询结果中返回的相关对象列表
        /// </summary>
        Include,

        /// <summary>
        /// 返回值序列中的最大值
        /// </summary>
        Max,

        /// <summary>
        /// 返回值序列中的最小值
        /// </summary>
        Min,

        /// <summary>
        /// 根据键按升序对序列的元素进行排序
        /// </summary>
        OrderBy,

        /// <summary>
        /// 根据键按降序对序列的元素进行排序
        /// </summary>
        OrderByDescending,

        /// <summary>
        /// 通过合并元素的索引，将序列的每个元素投影到新集合中
        /// </summary>
        Select,

        /// <summary>
        /// 将序列的每个元素投影到结果集，并将结果序列合并为一个序列，并对其中每个元素调用结果选择器函数
        /// </summary>
        SelectMany,

        /// <summary>
        /// 返回序列的唯一元素；如果该序列并非恰好包含一个元素，则会引发异常
        /// </summary>
        Single,

        /// <summary>
        /// 返回序列中的唯一元素；如果该序列为空，则返回默认值；如果该序列包含多个元素，此方法将引发异常
        /// </summary>
        SingleOrDefault,

        /// <summary>
        /// 跳过序列中指定数量的元素，然后返回剩余的元素
        /// </summary>
        Skip,

        /// <summary>
        /// 计算值序列的总和
        /// </summary>
        Sum,

        /// <summary>
        /// 从序列的开头返回指定数量的相邻元素
        /// </summary>
        Take,

        /// <summary>
        /// 根据某个键按升序对序列中的元素执行后续排序
        /// </summary>
        ThenBy,

        /// <summary>
        /// 使用指定的比较器按降序对序列中的元素执行后续排序
        /// </summary>
        ThenByDescending,

        /// <summary>
        /// 基于谓词筛选值序列
        /// </summary>
        Where,

        /// <summary>
        /// 通过使用默认的相等比较器，生成两个序列的并集
        /// </summary>
        Union,
        
        /// <summary>
        /// 插入记录
        /// </summary>
        Insert,

        /// <summary>
        /// 删除记录
        /// </summary>
        Delete,

        /// <summary>
        /// 更新记录
        /// </summary>
        Update,
        
        /// <summary>
        /// 显式指定会子查询
        /// </summary>
        AsSubQuery,

        /// <summary>
        /// 通过使用默认的相等比较器确定序列是否包含指定的元素
        /// </summary>
        Contains
    }
}
