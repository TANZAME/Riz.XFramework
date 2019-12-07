using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// 提供对数据类型未知的特定数据源进行 &lt;查&gt; 操作的语义表示
    /// </summary>
    internal class OracleDbQueryableInfo_Select<T> : DbQueryableInfo_Select<T>
    {
        /// <summary>
        /// 显式附加的列名集合
        /// </summary>
        public string AppendColumns { get; set; }

        /// <summary>
        /// 初始化 <see cref="OracleDbQueryableInfo_Select"/> 类的新实例
        /// </summary>
        public OracleDbQueryableInfo_Select(DbQueryableInfo_Select<T> source, string columns)
            : base()
        {
            this.AppendColumns = columns;
            if (source != null)
            {
                this.Joins = source.Joins;
                this.OrderBys = source.OrderBys;
                this.Includes = source.Includes;
                this.HasDistinct = source.HasDistinct;
                this.HasAny = source.HasAny;
                this.HasMany = source.HasMany;
                this.Skip = source.Skip;
                this.Take = source.Take;
                this.PickType = source.PickType;
                this.Select = source.Select;
                this.Where = source.Where;
                this.Having = source.Having;
                this.Aggregate = source.Aggregate;
                this.GroupBy = source.GroupBy;
                this.SubQueryInfo = source.SubQueryInfo;
                this.IsParsedByMany = source.IsParsedByMany;
                this.Unions = source.Unions;
            }
        }
    }
}