
using System.Data;

namespace ICS.XFramework.Data
{
    /// <summary>
    /// 参数类型封装
    /// </summary>
    public class DbTypeInfo
    {
        /// <summary>
        /// .NET 自带DbType
        /// </summary>
        public DbType? DbType { get; set; }

        /// <summary>
        /// 其它数据库组件的DbType
        /// </summary>
        public object SqlDbType { get; set; }

        /// <summary>
        /// 是否时间类型
        /// </summary>
        public virtual bool IsTime
        {
            get { return false; }
        }

        /// <summary>
        /// 是否日期类型
        /// </summary>
        public virtual bool IsDate
        {
            get { return false; }
        }

        /// <summary>
        /// 是否日期+时间类型
        /// </summary>
        public virtual bool IsDateTime
        {
            get { return false; }
        }

        /// <summary>
        /// 是否日期+时间+精度类型
        /// </summary>
        public virtual bool IsDateTime2
        {
            get { return false; }
        }

        /// <summary>
        /// 是否日期+时间+精度+时区类型
        /// </summary>
        public virtual bool IsDateTimeOffset
        {
            get { return false; }
        }

        /// <summary>
        /// 检查是否Unicode数据类型
        /// </summary>
        public virtual bool IsUnicode
        {
            get { return false; }
        }


    }
}
