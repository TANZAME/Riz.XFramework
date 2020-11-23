
using System.Data;
namespace Riz.XFramework.Data
{
    /// <summary>
    /// 原始 SQL 参数
    /// </summary>
    public sealed class DbRawParameter
    {
        /// <summary>
        /// 实例化 <see cref="DbRawParameter"/> 类的新实例
        /// </summary>
        public DbRawParameter()
        {

        }

        /// <summary>
        /// 实例化 <see cref="DbRawParameter"/> 类的新实例
        /// </summary>
        /// <param name="name">参数名称</param>
        /// <param name="value">参数值</param>
        /// <param name="dbType">数据类型</param>
        /// <param name="size">数据大小</param>
        /// <param name="precision">参数精度</param>
        /// <param name="scale">小数位数</param>
        /// <param name="direction">指示参数是只可输入、只可输出、双向还是存储过程返回值参数</param>
        public DbRawParameter(string name, object value,
            object dbType, int? size = null, int? precision = null, int? scale = null, ParameterDirection? direction = null)
        {
            this.ParameterName = name;
            this.Value = value;
            if (dbType != null) this.DbType = dbType;
            if (size != null) this.Size = size;
            if (precision != null) this.Precision = (byte)precision.Value;
            if (scale != null) this.Scale = (byte)scale.Value;
            if (direction != null) this.Direction = direction.Value;
        }

        /// <summary>
        /// 获取或设置参数的名称
        /// </summary>
        public string ParameterName { get; set; }

        /// <summary>
        /// 获取或设置该参数的值
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        /// 获取或设置参数的 System.Data.DbType。
        /// </summary>
        public object DbType { get; set; }

        /// <summary>
        /// 获取或设置一个值，该值指示参数是只可输入、只可输出、双向还是存储过程返回值参数
        /// </summary>
        public ParameterDirection? Direction { get; set; }

        /// <summary>
        /// 获取或设置一个值，该值指示参数是否接受 null 值
        /// </summary>
        public bool? IsNullable { get; set; }

        /// <summary>
        /// 指示数值参数的精度。
        /// </summary>
        public byte? Precision { get; set; }

        /// <summary>
        /// 指示数值参数的小数位数
        /// </summary>
        public byte? Scale { get; set; }

        /// <summary>
        /// 获取或设置列中数据的最大大小（以字节为单位）
        /// </summary>
        public int? Size { get; set; }
    }
}
