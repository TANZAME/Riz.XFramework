
namespace TZM.XFramework.Data
{
    /// <summary>
    /// 提供对数据类型未知的特定数据源进行 &lt;查&gt; 操作的语义表示
    /// </summary>
    internal class SQLiteDbQueryableInfo_Select : DbQueryableInfo_Select, IWidthRowId
    {
        /// <summary>
        /// 初始化 <see cref="SQLiteDbQueryableInfo_Select"/> 类的新实例
        /// </summary>
        public SQLiteDbQueryableInfo_Select(IDbQueryableInfo_Select source)
            : base()
        {
            this.Joins = source.Joins;
            this.OrderBys = source.OrderBys;
            this.Includes = source.Includes;
            this.HasDistinct = source.HasDistinct;
            this.HasAny = source.HasAny;
            this.HasMany = source.HasMany;
            this.Skip = source.Skip;
            this.Take = source.Take;
            this.FromType = source.FromType;
            this.Select = source.Select;
            this.Where = source.Where;
            this.Having = source.Having;
            this.Aggregate = source.Aggregate;
            this.GroupBy = source.GroupBy;
            this.Subquery = source.Subquery;
            this.IsParsedByMany = source.IsParsedByMany;
            this.Unions = source.Unions;
        }
    }
}