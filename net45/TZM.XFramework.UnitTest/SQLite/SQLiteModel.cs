
using System;
using System.Text;
using System.Data.SqlTypes;
using TZM.XFramework.Data;

namespace TZM.XFramework.UnitTest.SQLite
{
    public class SQLiteModel
    {
        [Table(Name = "Sys_Demo")]
        public class SQLiteDemo : Model.Demo
        {
            public SQLiteDemo()
                : base()
            {
            }

            public SQLiteDemo(int demoId, string demoName)
                : base(demoId, demoName)
            {

            }

            public SQLiteDemo(SQLiteDemo model)
                : base(model)
            {

            }

            /// <summary>
            /// demotext_nullable
            /// </summary>
            public virtual string DemoText_Nullable { get; set; }

            /// <summary>
            /// demontext_nullable
            /// </summary>
            public virtual string DemoNText_Nullable { get; set; }

            /// <summary>
            /// demolong_nullable
            /// </summary>
            [Column(DbType = System.Data.DbType.DateTimeOffset, Scale = 7)]
            public virtual Nullable<DateTimeOffset> DemoDatetimeOffset_Nullable { get; set; }

            /// <summary>
            /// demobinary_nullable
            /// </summary>
            [Column(DbType = System.Data.DbType.Binary, Size = 128)]
            public virtual byte[] DemoBinary_Nullable { get; set; }

            /// <summary>
            /// demvarbinary_nullable
            /// </summary>
            [Column(DbType = System.Data.DbType.Binary, Size = 128)]
            public virtual byte[] DemVarBinary_Nullable { get; set; }

            /// <summary>
            /// demotimestamp_nullable
            /// </summary>
            [Column(DbType = System.Data.DbType.DateTime)]
            public virtual Nullable<DateTime> DemoTimestamp_Nullable { get; set; }

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
