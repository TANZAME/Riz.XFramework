
using System;
using System.Text;
using System.Data.SqlTypes;
using Riz.XFramework.Data;

namespace Riz.XFramework.UnitTest.SqlServer
{
    public class SqlServerModel
    {
        public enum MyEnum
        {
            None = 0,
            DBNull = 127
        }

        [Table(Name = "Sys_Demo")]
        public class SqlServerDemo : Model.Demo
        {
            public SqlServerDemo()
                : base()
            {
            }

            public SqlServerDemo(int demoId, string demoName)
                : base(demoId, demoName)
            {

            }

            public SqlServerDemo(SqlServerDemo model)
                : base(model)
            {

            }

            /// <summary>
            /// demotext_nullable
            /// </summary>
            [Column(DbType = System.Data.SqlDbType.Text)]
            public virtual string DemoText_Nullable { get; set; }

            /// <summary>
            /// demontext_nullable
            /// </summary>
            [Column(DbType = System.Data.SqlDbType.NText)]
            public virtual string DemoNText_Nullable { get; set; }

            /// <summary>
            /// demodate
            /// </summary>
            [Column(DbType = System.Data.SqlDbType.Date)]
            public override DateTime DemoDate { get; set; }

            /// <summary>
            /// demodate_nullable
            /// </summary>
            [Column(DbType = System.Data.SqlDbType.Date)]
            public override Nullable<DateTime> DemoDate_Nullable { get; set; }

            /// <summary>
            /// demodatetime
            /// </summary>
            [Column(DbType = System.Data.SqlDbType.DateTime)]
            public override DateTime DemoDateTime { get; set; }

            /// <summary>
            /// demodatetime_nullable
            /// </summary>
            [Column(DbType = System.Data.SqlDbType.DateTime)]
            public override Nullable<DateTime> DemoDateTime_Nullable { get; set; }

            /// <summary>
            /// demodatetime2
            /// </summary>   
            [Column(DbType = System.Data.SqlDbType.DateTime2, Scale = 7)]
            public override DateTime DemoDateTime2 { get; set; }

            /// <summary>
            /// demodatetime2_nullable
            /// </summary>
            [Column(DbType = System.Data.SqlDbType.DateTime2, Scale = 7)]
            public override Nullable<DateTime> DemoDateTime2_Nullable { get; set; }

            /// <summary>
            /// demolong_nullable
            /// </summary>
            [Column(DbType = System.Data.SqlDbType.Time, Scale = 6)]
            public override Nullable<TimeSpan> DemoTime_Nullable { get; set; }

            /// <summary>
            /// demolong_nullable
            /// </summary>
            [Column(DbType = System.Data.SqlDbType.DateTimeOffset, Scale = 6)]
            public override Nullable<DateTimeOffset> DemoDatetimeOffset_Nullable { get; set; }

            /// <summary>
            /// demobinary_nullable
            /// </summary>
            [Column(DbType = System.Data.DbType.Binary, Size = 128)]
            public virtual byte[] DemoBinary_Nullable { get; set; }

            /// <summary>
            /// demvarbinary_nullable
            /// </summary>
            [Column(DbType = System.Data.SqlDbType.VarBinary, Size = -1)]
            public virtual byte[] DemoVarBinary_Nullable { get; set; }

            /// <summary>
            /// 行版本号
            /// </summary>
            [Column(DbType = System.Data.SqlDbType.Timestamp)]
            public virtual byte[] DemoTimestamp_Nullable { get; set; }

            ///// <summary>
            ///// Xml
            ///// </summary>
            //[Column(DbType = System.Data.DbType.Xml)]
            //public virtual SqlXml DemoXml_Nullable { get; set; }

            /// <summary>
            /// 字符串形式
            /// </summary>
            [Column(NoMapped = true)]
            public string DemoTimestamp_s
            {
                get
                {
                    return "0x" + BitConverter.ToString(this.DemoTimestamp_Nullable).Replace("-", "");
                }
            }

            /// <summary>
            /// long 形式
            /// </summary>
            [Column(NoMapped = true)]
            public long DemoTimestamp_b
            {
                get
                {
                    return Convert.ToInt64(this.DemoTimestamp_s, 16);
                }
            }

            /// <summary>
            /// binary 的字符串形式
            /// </summary>
            [Column(NoMapped = true)]
            public string DemoBinary_s
            {
                get
                {
                    return this.DemoBinary_Nullable != null ? Encoding.UTF8.GetString(this.DemoBinary_Nullable) : null;
                }
            }

            /// <summary>
            /// binary 的字符串形式
            /// </summary>
            [Column(NoMapped = true)]
            public string DemVarBinary_s
            {
                get
                {
                    return this.DemoVarBinary_Nullable != null ? Encoding.UTF8.GetString(this.DemoVarBinary_Nullable) : null;
                }
            }
        }

        /// <summary>
        /// 表变量示例
        /// </summary>
        [Table(Name = "@JoinKey", IsTemporary = true)]
        public class JoinKey
        {
            public int Key1 { get; set; }

            public string Key2 { get; set; }
        }
    }
}
