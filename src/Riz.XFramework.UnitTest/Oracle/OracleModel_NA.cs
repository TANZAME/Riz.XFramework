
using System;
using System.Text;

using Riz.XFramework.Data;
using Oracle.ManagedDataAccess.Client;

namespace Riz.XFramework.UnitTest.Oracle
{
    /// <summary>
    /// 属性名称与数据库字段名称不一致的实体
    /// </summary>
    public class OracleModel_NA
    {
        [Table(Name = "Sys_Demo")]
        public class Demo : Model_NA.Demo
        {
            public Demo()
                : base()
            {
            }

            public Demo(int demoId, string demoName)
                : base(demoId, demoName)
            {

            }

            public Demo(Demo model)
                : base(model)
            {

            }

            /// <summary>
            /// democode
            /// </summary>        
            [OracleColumn(IsKey = true, SEQName = "Sys_Demo_DemoId_Seq", Name = "DemoId")]
            public override int RizDemoId { get; set; }

            /// <summary>
            /// democode
            /// </summary>        
            [Column(DbType = OracleDbType.Varchar2, Size = 32, Name = "DemoCode")]
            public override string RizDemoCode { get; set; }

            /// <summary>
            /// demoname
            /// </summary>
            [Column(DbType = OracleDbType.NVarchar2, Size = 32, Name = "DemoName")]
            public override string RizDemoName { get; set; }

            /// <summary>
            /// demochar
            /// </summary>
            [Column(DbType = OracleDbType.Char, Size = 1, Name = "DemoChar")]
            public override char RizDemoChar { get; set; }

            /// <summary>
            /// demochar
            /// </summary>
            [Column(DbType = OracleDbType.NChar, Size = 1, Name = "DemoNChar")]
            public override char RizDemoNChar { get; set; }

            /// <summary>
            /// demochar_nullable
            /// </summary>
            [Column(DbType = OracleDbType.Char, Size = 1, Name = "DemoChar_Nullable")]
            public override Nullable<char> RizDemoChar_Nullable { get; set; }

            /// <summary>
            /// demodate
            /// </summary>        
            [Column(DbType = OracleDbType.Date, Name = "DemoDate")]
            public override DateTime RizDemoDate { get; set; }

            /// <summary>
            /// demodate_nullable
            /// </summary>        
            [Column(DbType = OracleDbType.Date, Name = "DemoDate_Nullable")]
            public override Nullable<DateTime> RizDemoDate_Nullable { get; set; }

            /// <summary>
            /// demodatetime
            /// </summary>
            [Column(DbType = OracleDbType.TimeStamp, Scale = 6, Name = "DemoDateTime")]
            public override DateTime RizDemoDateTime { get; set; }

            /// <summary>
            /// demodatetime_nullable
            /// </summary> 
            [Column(DbType = OracleDbType.TimeStamp, Scale = 6, Name = "DemoDateTime_Nullable")]
            public override Nullable<DateTime> RizDemoDateTime_Nullable { get; set; }

            /// <summary>
            /// demodatetime2
            /// </summary>    
            [Column(DbType = OracleDbType.TimeStamp, Scale = 7, Name = "DemoDateTime2")]
            public override DateTime RizDemoDateTime2 { get; set; }

            /// <summary>
            /// demodatetime2_nullable
            /// </summary>        
            [Column(DbType = OracleDbType.TimeStamp, Scale = 7, Name = "DemoDateTime2_Nullable")]
            public override Nullable<DateTime> RizDemoDateTime2_Nullable { get; set; }

            /// <summary>
            /// Time 类型
            /// </summary>
            [Column(DbType = OracleDbType.IntervalDS, Scale = 7, Name = "DemoTime_Nullable")]
            public override Nullable<TimeSpan> RizDemoTime_Nullable { get; set; }

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
            [Column(DbType = OracleDbType.TimeStampTZ, Scale = 7, Name = "DemoDatetimeOffset_Nullable")]
            public override Nullable<DateTimeOffset> RizDemoDatetimeOffset_Nullable { get; set; }

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
    }
}
