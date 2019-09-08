
using System;

using TZM.XFramework.Data;
using MySql.Data.MySqlClient;
using System.Text;

namespace TZM.XFramework.UnitTest.MySql
{
    public class MySqlModel
    {
        [Table(Name = "Sys_Demo")]
        public class MySqlDemo : Model.Demo
        {
            public MySqlDemo()
                : base()
            {
            }

            public MySqlDemo(int demoId, string demoName)
                : base(demoId, demoName)
            {

            }

            public MySqlDemo(MySqlDemo model)
                : base(model)
            {

            }

            /// <summary>
            /// democode
            /// </summary>        
            [Column(DbType = MySqlDbType.VarChar, Size = 32)]
            public override string DemoCode { get; set; }

            /// <summary>
            /// demoname
            /// </summary>
            [Column(DbType = MySqlDbType.VarChar, Size = 32)]
            public override string DemoName { get; set; }

            /// <summary>
            /// demodate
            /// </summary>        
            [Column(DbType = MySqlDbType.Date)]
            public override DateTime DemoDate { get; set; }

            /// <summary>
            /// demodate_nullable
            /// </summary>        
            [Column(DbType = MySqlDbType.Date)]
            public override Nullable<DateTime> DemoDate_Nullable { get; set; }

            /// <summary>
            /// demodatetime2
            /// </summary>    
            [Column(DbType = MySqlDbType.DateTime, Scale = 6)]
            public override DateTime DemoDateTime2 { get; set; }

            /// <summary>
            /// demodatetime2_nullable
            /// </summary>        
            [Column(DbType = MySqlDbType.DateTime, Scale = 6)]
            public override Nullable<DateTime> DemoDateTime2_Nullable { get; set; }

            // ############################### 美丽的分隔线 ###################################

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
            [Column(DbType = MySqlDbType.Time, Scale = 5)]
            public virtual Nullable<TimeSpan> DemoTime_Nullable { get; set; }

            /// <summary>
            /// demolong_nullable
            /// </summary>
            [Column(DbType = MySqlDbType.DateTime)]
            public virtual Nullable<DateTime> DemoDatetimeOffset_Nullable { get; set; }

            /// <summary>
            /// demobinary_nullable
            /// </summary>
            [Column(DbType = MySqlDbType.MediumBlob)]
            public virtual byte[] DemoBinary_Nullable { get; set; }

            /// <summary>
            /// demvarbinary_nullable
            /// </summary>
            [Column(DbType = MySqlDbType.LongBlob)]
            public virtual byte[] DemVarBinary_Nullable { get; set; }

            /// <summary>
            /// demotimestamp_nullable
            /// </summary>
            [Column(DbType = MySqlDbType.Timestamp, Scale = 5)]
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
                    return this.DemVarBinary_Nullable != null ? Encoding.UTF8.GetString(this.DemVarBinary_Nullable) : null;
                }
            }
        }
    }
}
