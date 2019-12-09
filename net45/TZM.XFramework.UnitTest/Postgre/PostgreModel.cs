
using System;
using System.Text;
using System.Data.SqlTypes;

using TZM.XFramework.Data;
using NpgsqlTypes;

namespace TZM.XFramework.UnitTest.Postgre
{
    public class PostgreModel
    {
        [Table(Name = "Sys_Demo")]
        public class PostgreDemo : Model.Demo
        {
            public PostgreDemo()
                : base()
            {
            }

            public PostgreDemo(int demoId, string demoName)
                : base(demoId, demoName)
            {

            }

            public PostgreDemo(PostgreDemo model)
                : base(model)
            {

            }

            /// <summary>
            /// democode
            /// </summary>        
            [Column(DbType = NpgsqlDbType.Varchar, Size = 32)]
            public override string DemoCode { get; set; }

            /// <summary>
            /// demoname
            /// </summary>
            [Column(DbType = NpgsqlDbType.Varchar, Size = 32)]
            public override string DemoName { get; set; }

            /// <summary>
            /// demodate
            /// </summary>        
            [Column(DbType = NpgsqlDbType.Date)]
            public override DateTime DemoDate { get; set; }

            /// <summary>
            /// demodate_nullable
            /// </summary>        
            [Column(DbType = NpgsqlDbType.Date)]
            public override Nullable<DateTime> DemoDate_Nullable { get; set; }

            /// <summary>
            /// demodatetime2
            /// </summary>    
            [Column(DbType = NpgsqlDbType.Timestamp, Scale = 6)]
            public override DateTime DemoDateTime2 { get; set; }

            /// <summary>
            /// demodatetime2_nullable
            /// </summary>        
            [Column(DbType = NpgsqlDbType.Timestamp, Scale = 6)]
            public override Nullable<DateTime> DemoDateTime2_Nullable { get; set; }

            // ############################### 美丽的分隔线 ###################################

            /// <summary>
            /// Time 类型，映射到 .NET DateTime类型
            /// <para>如果映射到TimeSpan类型会报错</para>
            /// </summary>
            [Column(DbType = NpgsqlDbType.Time, Scale = 2)]
            public override Nullable<TimeSpan> DemoTime_Nullable { get; set; }

            /// <summary>
            /// demotext_nullable
            /// </summary>
            [Column(DbType = NpgsqlDbType.Text)]
            public virtual string DemoText_Nullable { get; set; }

            /// <summary>
            /// demontext_nullable
            /// </summary>
            [Column(DbType = NpgsqlDbType.Text)]
            public virtual string DemoNText_Nullable { get; set; }

#if netcore
            
            /// <summary>
            /// demolong_nullable
            /// </summary>
            [Column(DbType = NpgsqlDbType.TimestampTz, Scale = 6)]
            public override Nullable<DateTimeOffset> DemoDatetimeOffset_Nullable { get; set; }

#endif
#if !netcore

            // <summary>
            /// demolong_nullable
            /// </summary>
            [Column(DbType = NpgsqlDbType.TimestampTZ, Scale = 6)]
            public override Nullable<DateTimeOffset> DemoDatetimeOffset_Nullable { get; set; }

#endif


            /// <summary>
            /// demobinary_nullable
            /// </summary>
            [Column(DbType = NpgsqlDbType.Bytea)]
            public virtual byte[] DemoBinary_Nullable { get; set; }

            /// <summary>
            /// demvarbinary_nullable
            /// </summary>
            [Column(DbType = NpgsqlDbType.Bytea)]
            public virtual byte[] DemoVarBinary_Nullable { get; set; }

            /// <summary>
            /// demotimestamp_nullable
            /// </summary>
            [Column(DbType = NpgsqlDbType.Timestamp)]
            public virtual Nullable<DateTime> DemoTimestamp_Nullable { get; set; }

            ///// <summary>
            ///// Xml Npg .NET 不支持xml类型
            ///// </summary>
            //[Column(DbType = NpgsqlDbType.Xml)]
            //public virtual SqlXml DemoXml_Nullable { get; set; }

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
    }
}
