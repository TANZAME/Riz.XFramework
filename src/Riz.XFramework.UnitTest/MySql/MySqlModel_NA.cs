
using System;
using Riz.XFramework.Data;
using System.Text;
#if net40
using MySql.Data.MySqlClient;
#else
using MySqlConnector;
#endif

namespace Riz.XFramework.UnitTest.MySql
{
    public class MySqlModel_NA
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
            [Column(DbType = MySqlDbType.VarChar, Size = 40, Name = "DemoCode")]
            public override string RizDemoCode { get; set; }

            /// <summary>
            /// demoname
            /// </summary>
            [Column(DbType = MySqlDbType.VarChar, Size = 32, Name = "DemoName")]
            public override string RizDemoName { get; set; }

            /// <summary>
            /// demochar
            /// </summary>
            [Column(DbType = System.Data.DbType.String, Size = 1, Name = "DemoChar")]
            public override char RizDemoChar { get; set; }

            /// <summary>
            /// demochar
            /// </summary>
            [Column(DbType = System.Data.DbType.String, Size = 1, Name = "DemoNChar")]
            public override char RizDemoNChar { get; set; }

            /// <summary>
            /// demochar_nullable
            /// </summary>
            [Column(DbType = System.Data.DbType.String, Size = 1, Name = "DemoChar_Nullable")]
            public override Nullable<char> RizDemoChar_Nullable { get; set; }

            /// <summary>
            /// demodate
            /// </summary>        
            [Column(DbType = MySqlDbType.Date, Name = "DemoDate")]
            public override DateTime RizDemoDate { get; set; }

            /// <summary>
            /// demodate_nullable
            /// </summary>        
            [Column(DbType = MySqlDbType.Date, Name = "DemoDate_Nullable")]
            public override Nullable<DateTime> RizDemoDate_Nullable { get; set; }

            /// <summary>
            /// demodatetime2
            /// </summary>    
            [Column(DbType = MySqlDbType.DateTime, Scale = 6, Name = "DemoDateTime2")]
            public override DateTime RizDemoDateTime2 { get; set; }

            /// <summary>
            /// demodatetime2_nullable
            /// </summary>        
            [Column(DbType = MySqlDbType.DateTime, Scale = 6, Name = "DemoDateTime2_Nullable")]
            public override Nullable<DateTime> RizDemoDateTime2_Nullable { get; set; }

            /// <summary>
            /// demotext_nullable
            /// </summary>
            [Column(DbType = MySqlDbType.MediumText)]
            public virtual string DemoText_Nullable { get; set; }

            /// <summary>
            /// demontext_nullable
            /// </summary>
            [Column(DbType = MySqlDbType.LongText)]
            public virtual string DemoNText_Nullable { get; set; }


            /// <summary>
            /// demolong_nullable
            /// </summary>
            [Column(DbType = MySqlDbType.Time, Scale = 5, Name = "DemoTime_Nullable")]
            public override Nullable<TimeSpan> RizDemoTime_Nullable { get; set; }

            /// <summary>
            /// demolong_nullable
            /// </summary>
            [Column(DbType = MySqlDbType.DateTime, Name = "DemoDatetimeOffset_Nullable")]
            public new Nullable<DateTime> RizDemoDatetimeOffset_Nullable { get; set; }

            /// <summary>
            /// demobinary_nullable
            /// </summary>
            [Column(DbType = MySqlDbType.MediumBlob)]
            public virtual byte[] DemoBinary_Nullable { get; set; }

            /// <summary>
            /// demvarbinary_nullable
            /// </summary>
            [Column(DbType = MySqlDbType.LongBlob)]
            public virtual byte[] DemoVarBinary_Nullable { get; set; }

            /// <summary>
            /// demotimestamp_nullable
            /// </summary>
            [Column(DbType = MySqlDbType.Timestamp, Scale = 6)]
            public virtual Nullable<DateTime> DemoTimestamp_Nullable { get; set; }

            ///// <summary>
            ///// MYSQL 没有xml，可用字符串代替
            ///// </summary>
            //[Column(DbType = System.Data.DbType.Xml)]
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
