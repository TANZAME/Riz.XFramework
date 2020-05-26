
using System;
using System.Text;

using TZM.XFramework.Data;
using Oracle.ManagedDataAccess.Client;

namespace TZM.XFramework.UnitTest.Oracle
{
    public class OracleModel
    {
        [Table(Name = "Sys_Demo")]
        public class OracleDemo : Model.Demo
        {
            public OracleDemo()
                : base()
            {
            }

            public OracleDemo(int demoId, string demoName)
                : base(demoId, demoName)
            {

            }

            public OracleDemo(OracleDemo model)
                : base(model)
            {

            }

            /// <summary>
            /// democode
            /// </summary>        
            [OracleColumn(IsKey = true, SEQName = "SYS_DEMO_DEMOID_SEQ")]
            public override int DemoId { get; set; }

            /// <summary>
            /// democode
            /// </summary>        
            [Column(DbType = OracleDbType.Varchar2, Size = 32)]
            public override string DemoCode { get; set; }

            /// <summary>
            /// demoname
            /// </summary>
            [Column(DbType = OracleDbType.NVarchar2, Size = 32)]
            public override string DemoName { get; set; }

            /// <summary>
            /// demodate
            /// </summary>        
            [Column(DbType = OracleDbType.Date)]
            public override DateTime DemoDate { get; set; }

            /// <summary>
            /// demodate_nullable
            /// </summary>        
            [Column(DbType = OracleDbType.Date)]
            public override Nullable<DateTime> DemoDate_Nullable { get; set; }

            /// <summary>
            /// demodatetime
            /// </summary>
            [Column(DbType = OracleDbType.TimeStamp, Scale = 6)]
            public override DateTime DemoDateTime { get; set; }

            /// <summary>
            /// demodatetime_nullable
            /// </summary> 
            [Column(DbType = OracleDbType.TimeStamp, Scale = 6)]
            public override Nullable<DateTime> DemoDateTime_Nullable { get; set; }

            /// <summary>
            /// demodatetime2
            /// </summary>    
            [Column(DbType = OracleDbType.TimeStamp, Scale = 7)]
            public override DateTime DemoDateTime2 { get; set; }

            /// <summary>
            /// demodatetime2_nullable
            /// </summary>        
            [Column(DbType = OracleDbType.TimeStamp, Scale = 7)]
            public override Nullable<DateTime> DemoDateTime2_Nullable { get; set; }

            // ############################### 美丽的分隔线 ###################################


            /// <summary>
            /// Time 类型
            /// </summary>
            [Column(DbType = OracleDbType.IntervalDS, Scale = 7)]
            public override Nullable<TimeSpan> DemoTime_Nullable { get; set; }

            /// <summary>
            /// demotext_nullable
            /// </summary>
            [Column(DbType = OracleDbType.Clob)]
            public virtual string DemoText_Nullable { get; set; }

            /// <summary>
            /// demontext_nullable
            /// </summary>
            [Column(DbType = OracleDbType.NClob)]
            public virtual string DemoNText_Nullable { get; set; }

            /// <summary>
            /// DateTimeOffset 类型，映射到 .NET DateTime类型
            /// </summary>
            [Column(DbType = OracleDbType.TimeStampTZ, Scale = 7)]
            public override Nullable<DateTimeOffset> DemoDatetimeOffset_Nullable { get; set; }

            /// <summary>
            /// demobinary_nullable
            /// </summary>
            [Column(DbType = OracleDbType.Blob)]
            public virtual byte[] DemoBinary_Nullable { get; set; }

            /// <summary>
            /// demvarbinary_nullable
            /// </summary>
            [Column(DbType = OracleDbType.Blob)]
            public virtual byte[] DemoVarBinary_Nullable { get; set; }

            /// <summary>
            /// demotimestamp_nullable
            /// </summary>
            [Column(DbType = OracleDbType.TimeStampLTZ)]
            public virtual Nullable<DateTime> DemoTimestamp_Nullable { get; set; }

            ///// <summary>
            ///// demotimestamp_nullable
            ///// </summary>
            //[Column(DbType = NpgsqlDbType.Inet)]
            //public virtual IPAddress DemoIPAddress_Nullable { get; set; }

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

        [Table(Name = "vw_Demo")]
        public class vwOracleDemo : Model.Demo
        {
            public vwOracleDemo()
                : base()
            {
            }

            public vwOracleDemo(int demoId, string demoName)
                : base(demoId, demoName)
            {

            }

            public vwOracleDemo(OracleDemo model)
                : base(model)
            {

            }

            /// <summary>
            /// democode
            /// </summary>        
            [OracleColumn(IsKey = true, SEQName = "SYS_DEMO_DEMOID_SEQ")]
            public override int DemoId { get; set; }

            /// <summary>
            /// democode
            /// </summary>        
            [Column(DbType = OracleDbType.Varchar2, Size = 32)]
            public override string DemoCode { get; set; }

            /// <summary>
            /// demoname
            /// </summary>
            [Column(DbType = OracleDbType.NVarchar2, Size = 32)]
            public override string DemoName { get; set; }

            /// <summary>
            /// demodate
            /// </summary>        
            [Column(DbType = OracleDbType.Date)]
            public override DateTime DemoDate { get; set; }

            /// <summary>
            /// demodate_nullable
            /// </summary>        
            [Column(DbType = OracleDbType.Date)]
            public override Nullable<DateTime> DemoDate_Nullable { get; set; }

            /// <summary>
            /// demodatetime
            /// </summary>
            [Column(DbType = OracleDbType.TimeStamp, Scale = 6)]
            public override DateTime DemoDateTime { get; set; }

            /// <summary>
            /// demodatetime_nullable
            /// </summary> 
            [Column(DbType = OracleDbType.TimeStamp, Scale = 6)]
            public override Nullable<DateTime> DemoDateTime_Nullable { get; set; }

            /// <summary>
            /// demodatetime2
            /// </summary>    
            [Column(DbType = OracleDbType.TimeStamp, Scale = 7)]
            public override DateTime DemoDateTime2 { get; set; }

            /// <summary>
            /// demodatetime2_nullable
            /// </summary>        
            [Column(DbType = OracleDbType.TimeStamp, Scale = 7)]
            public override Nullable<DateTime> DemoDateTime2_Nullable { get; set; }

            // ############################### 美丽的分隔线 ###################################


            /// <summary>
            /// Time 类型
            /// </summary>
            [Column(DbType = OracleDbType.IntervalDS, Scale = 7)]
            public override Nullable<TimeSpan> DemoTime_Nullable { get; set; }

            /// <summary>
            /// demotext_nullable
            /// </summary>
            [Column(DbType = OracleDbType.Clob)]
            public virtual string DemoText_Nullable { get; set; }

            /// <summary>
            /// demontext_nullable
            /// </summary>
            [Column(DbType = OracleDbType.NClob)]
            public virtual string DemoNText_Nullable { get; set; }

            /// <summary>
            /// DateTimeOffset 类型，映射到 .NET DateTime类型
            /// </summary>
            [Column(DbType = OracleDbType.TimeStampTZ, Scale = 7)]
            public override Nullable<DateTimeOffset> DemoDatetimeOffset_Nullable { get; set; }

            /// <summary>
            /// demobinary_nullable
            /// </summary>
            [Column(DbType = OracleDbType.Blob)]
            public virtual byte[] DemoBinary_Nullable { get; set; }

            /// <summary>
            /// demvarbinary_nullable
            /// </summary>
            [Column(DbType = OracleDbType.Blob)]
            public virtual byte[] DemoVarBinary_Nullable { get; set; }

            /// <summary>
            /// demotimestamp_nullable
            /// </summary>
            [Column(DbType = OracleDbType.TimeStampLTZ)]
            public virtual Nullable<DateTime> DemoTimestamp_Nullable { get; set; }

            ///// <summary>
            ///// demotimestamp_nullable
            ///// </summary>
            //[Column(DbType = NpgsqlDbType.Inet)]
            //public virtual IPAddress DemoIPAddress_Nullable { get; set; }

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

        [Table(Name = "bas_post")]
        public class bas_post
        {
            public int id { get; set; }

            public int post_id { get; set; }
        }
    }
}
