
namespace TZM.XFramework.Data
{
    /// <summary>
    /// 选择列集合
    /// </summary>
    public class DbColumnCollection : HashCollection<DbColumn>
    {
        /// <summary>
        /// 添加项，如果字段存在，则产生一个新名称
        /// </summary>
        /// <param name="name">字段名称</param>
        /// <returns></returns>
        public string Add(string name)
        {
            // ATTENTION：此方法不能在 VisitMember 方法里调用
            // 因为 VisitMember 方法不一定是最后SELECT的字段
            // 返回最终确定的唯一的列名

            string newName = name;
            int dup = 0;
            while (this.Contains(newName))
            {
                var column = this[newName];
                column.DupCount += 1;

                newName = newName + column.DupCount.ToString();
                dup = column.DupCount;
            }

            this.Add(new DbColumn
            {
                Name = name,
                NewName = newName,
                DupCount = dup
            });
            return newName;
        }

        /// <summary>
        /// 添加一个带有所提供的键和值的元素。
        /// </summary>
        public void Add(DbColumn column)
        {
            base.Add(column.NewName, column);
        }
    }
}
