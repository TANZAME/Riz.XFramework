
using System;
using System.Text;

using Riz.XFramework.Data;
using Oracle.ManagedDataAccess.Client;

namespace Riz.XFramework.UnitTest.Oracle
{
    public class OracleModelS
    {
        [Table(Name = "sys_demo_s")]
        public partial class Demo : OracleModel.Demo
        {
            /// <summary>
            /// 初始化 <see cref="Demo"/> 类的新实例
            /// </summary>
            public Demo()
                : base()
            {
            }

            /// <summary>
            /// 初始化 <see cref="Demo"/> 类的新实例
            /// </summary>
            public Demo(int demoId, string demoName)
                : base(demoId, demoName)
            {
            }

            /// <summary>
            /// 初始化 <see cref="Demo"/> 类的新实例
            /// </summary>
            public Demo(Demo model)
                : base(model)
            {
            }

            /// <summary>
            /// demoid
            /// </summary>
            [OracleColumn(IsKey = true, SEQName = "sys_demo_demoid_seq_s", Name = "demoid")]
            public override int DemoId { get; set; }

            /// <summary>
            /// democode
            /// </summary>
            [Column(DbType = OracleDbType.Varchar2, Size = 32, Name = "democode")]
            public override string DemoCode { get; set; }

            /// <summary>
            /// demoname
            /// </summary>
            [Column(DbType = OracleDbType.NVarchar2, Size = 32, Name = "demoname")]
            public override string DemoName { get; set; }

            /// <summary>
            /// demoboolean
            /// </summary>
            [Column(Name = "demoboolean")]
            public override bool DemoBoolean { get; set; }

            /// <summary>
            /// demoboolean_nullable
            /// </summary>
            [Column(Name = "demoboolean_nullable")]
            public override Nullable<bool> DemoBoolean_Nullable { get; set; }

            /// <summary>
            /// demochar
            /// </summary>
            [Column(DbType = OracleDbType.Char, Size = 1, Name = "demochar")]
            public override char DemoChar { get; set; }

            /// <summary>
            /// demochar
            /// </summary>
            [Column(DbType = System.Data.DbType.StringFixedLength, Size = 1, Name = "demonchar")]
            public override char DemoNChar { get; set; }

            /// <summary>
            /// demochar_nullable
            /// </summary>
            [Column(DbType = System.Data.DbType.AnsiStringFixedLength, Size = 1, Name = "demochar_nullable")]
            public override Nullable<char> DemoChar_Nullable { get; set; }

            /// <summary>
            /// demobyte
            /// </summary>
            [Column(Name = "demobyte")]
            public override byte DemoByte { get; set; }

            /// <summary>
            /// demobyte_nullable
            /// </summary>
            [Column(Name = "demobyte_nullable")]
            public override Nullable<byte> DemoByte_Nullable { get; set; }

            /// <summary>
            /// demodate
            /// </summary>
            [Column(DbType = OracleDbType.Date, Name = "demodate")]
            public override DateTime DemoDate { get; set; }

            /// <summary>
            /// demodate_nullable
            /// </summary>
            [Column(DbType = OracleDbType.Date, Name = "demodate_nullable")]
            public override Nullable<DateTime> DemoDate_Nullable { get; set; }

            /// <summary>
            /// demodatetime
            /// </summary>
            [Column(DbType = OracleDbType.TimeStamp, Scale = 6, Name = "demodatetime")]
            public override DateTime DemoDateTime { get; set; }

            /// <summary>
            /// demodatetime_nullable
            /// </summary>
            [Column(DbType = OracleDbType.TimeStamp, Scale = 6, Name = "demodatetime_nullable")]
            public override Nullable<DateTime> DemoDateTime_Nullable { get; set; }

            /// <summary>
            /// demodatetime2
            /// </summary>   
            [Column(DbType = OracleDbType.TimeStamp, Scale = 7, Name = "demodatetime2")]
            public override DateTime DemoDateTime2 { get; set; }

            /// <summary>
            /// demodatetime2_nullable
            /// </summary>
            [Column(DbType = OracleDbType.TimeStamp, Scale = 7, Name = "demodatetime2_nullable")]
            public override Nullable<DateTime> DemoDateTime2_Nullable { get; set; }

            /// <summary>
            /// demotime_nullable
            /// </summary>
            [Column(DbType = OracleDbType.IntervalDS, Scale = 7, Name = "demotime_nullable")]
            public override Nullable<TimeSpan> DemoTime_Nullable { get; set; }

            /// <summary>
            /// demodatetimeoffset_nullable
            /// </summary>
            [Column(DbType = OracleDbType.TimeStampTZ, Scale = 7, Name = "demodatetimeoffset_nullable")]
            public override Nullable<DateTimeOffset> DemoDatetimeOffset_Nullable { get; set; }

            /// <summary>
            /// demotext_nullable
            /// </summary>
            [Column(DbType = OracleDbType.Clob, Name = "demotext_nullable")]
            public override string DemoText_Nullable { get; set; }

            /// <summary>
            /// demontext_nullable
            /// </summary>
            [Column(DbType = OracleDbType.NClob, Name = "demontext_nullable")]
            public override string DemoNText_Nullable { get; set; }

            /// <summary>
            /// demobinary_nullable
            /// </summary>
            [Column(DbType = OracleDbType.Blob, Name = "demobinary_nullable")]
            public override byte[] DemoBinary_Nullable { get; set; }

            /// <summary>
            /// demvarbinary_nullable
            /// </summary>
            [Column(DbType = OracleDbType.Blob, Name = "demovarbinary_nullable")]
            public override byte[] DemoVarBinary_Nullable { get; set; }

            /// <summary>
            /// demotimestamp_nullable
            /// </summary>
            [Column(DbType = OracleDbType.TimeStampLTZ, Name = "demotimestamp_nullable")]
            public override Nullable<DateTime> DemoTimestamp_Nullable { get; set; }

            /// <summary>
            /// demodecimal
            /// </summary>
            [Column(Name = "demodecimal")]
            public override decimal DemoDecimal { get; set; }

            /// <summary>
            /// demodecimal_nullable
            /// </summary>
            [Column(Name = "demodecimal_nullable")]
            public override Nullable<decimal> DemoDecimal_Nullable { get; set; }

            /// <summary>
            /// demodouble
            /// </summary>
            [Column(Name = "demodouble")]
            public override double DemoDouble { get; set; }

            /// <summary>
            /// demodouble_nullable
            /// </summary>
            [Column(Name = "demodouble_nullable")]
            public override Nullable<double> DemoDouble_Nullable { get; set; }

            /// <summary>
            /// demofloat
            /// </summary>
            [Column(Name = "demofloat")]
            public override float DemoFloat { get; set; }

            /// <summary>
            /// demofloat_nullable
            /// </summary>
            [Column(Name = "demofloat_nullable")]
            public override Nullable<float> DemoFloat_Nullable { get; set; }

            /// <summary>
            /// demoguid
            /// </summary>
            [Column(Name = "demoguid")]
            public override Guid DemoGuid { get; set; }

            /// <summary>
            /// demoguid_nullable
            /// </summary>
            [Column(Name = "demoguid_nullable")]
            public override Nullable<Guid> DemoGuid_Nullable { get; set; }

            /// <summary>
            /// demoshort
            /// </summary>
            [Column(Name = "demoshort")]
            public override short DemoShort { get; set; }

            /// <summary>
            /// demoshort_nullable
            /// </summary>
            [Column(Name = "demoshort_nullable")]
            public override Nullable<short> DemoShort_Nullable { get; set; }

            /// <summary>
            /// demoint
            /// </summary>
            [Column(Name = "demoint")]
            public override int DemoInt { get; set; }

            /// <summary>
            /// demoint_nullable
            /// </summary>
            [Column(Name = "demoint_nullable")]
            public override Nullable<int> DemoInt_Nullable { get; set; }

            /// <summary>
            /// demolong
            /// </summary>
            [Column(Name = "demolong")]
            public override long DemoLong { get; set; }

            /// <summary>
            /// demolong_nullable
            /// </summary>
            [Column(Name = "demolong_nullable")]
            public override Nullable<long> DemoLong_Nullable { get; set; }
        }

        [Table(Name = "bas_client_s")]
        public partial class Client : Model.Client
        {
            /// <summary>
            /// 初始化 <see cref="Client"/> 类的新实例
            /// </summary>
            public Client()
                : base()
            {
            }

            /// <summary>
            /// 初始化 <see cref="Client"/> 类的新实例
            /// </summary>
            public Client(Client model)
                : base(model)
            {
            }

            /// <summary>
            /// clientid
            /// </summary>
            [Column(IsKey = true, Name = "clientid")]
            public override int ClientId { get; set; }

            /// <summary>
            /// clientcode
            /// </summary>
            [Column(Name = "clientcode")]
            public override string ClientCode { get; set; }

            /// <summary>
            /// clientname
            /// </summary>
            [Column(Name = "clientname")]
            public override string ClientName { get; set; }

            /// <summary>
            /// cloudserverid
            /// </summary>
            [Column(Default = 0, Name = "cloudserverid")]
            public override int CloudServerId { get; set; }

            /// <summary>
            /// activedate
            /// </summary>
            [Column(Name = "activedate")]
            public override Nullable<DateTime> ActiveDate { get; set; }

            /// <summary>
            /// qty
            /// </summary>
            [Column(Default = 0, Name = "qty")]
            public override int Qty { get; set; }

            /// <summary>
            /// state
            /// </summary>
            [Column(Name = "state")]
            public override byte State { get; set; }

            /// <summary>
            /// remark
            /// </summary>
            [Column(Default = "'默认值'", Name = "remark")]
            public override string Remark { get; set; }
        }

        [Table(Name = "bas_clientaccount_s")]
        public partial class Account : Model.Account
        {
            /// <summary>
            /// 初始化 <see cref="Account"/> 类的新实例
            /// </summary>
            public Account()
                : base()
            {
            }

            /// <summary>
            /// 初始化 <see cref="Account"/> 类的新实例
            /// </summary>
            public Account(Account model)
                : base(model)
            {
            }

            /// <summary>
            /// clientid
            /// </summary>
            [Column(IsKey = true, Name = "clientid")]
            public override int ClientId { get; set; }

            /// <summary>
            /// accountid
            /// </summary>
            [Column(IsKey = true, Name = "accountid")]
            public override string AccountId { get; set; }

            /// <summary>
            /// accountcode
            /// </summary>
            [Column(Name = "accountcode")]
            public override string AccountCode { get; set; }

            /// <summary>
            /// accountname
            /// </summary>
            [Column(Name = "accountname")]
            public override string AccountName { get; set; }

            /// <summary>
            /// qty
            /// </summary>
            [Column(Default = 0, Name = "qty")]
            public override int Qty { get; set; }
        }

        [Table(Name = "bas_clientaccountmarket_s")]
        public partial class Market : Model.Market
        {
            /// <summary>
            /// 初始化 <see cref="Market"/> 类的新实例
            /// </summary>
            public Market()
                : base()
            {
            }

            /// <summary>
            /// 初始化 <see cref="Market"/> 类的新实例
            /// </summary>
            public Market(Market model)
                : base(model)
            {
            }

            /// <summary>
            /// clientid
            /// </summary>
            [Column(IsKey = true, Name = "clientid")]
            public override int ClientId { get; set; }

            /// <summary>
            /// accountid
            /// </summary>
            [Column(IsKey = true, Name = "accountid")]
            public override string AccountId { get; set; }

            /// <summary>
            /// marketid
            /// </summary>
            [Column(IsKey = true, Name = "marketid")]
            public override int MarketId { get; set; }

            /// <summary>
            /// marketcode
            /// </summary>
            [Column(Name = "marketcode")]
            public override string MarketCode { get; set; }

            /// <summary>
            /// marketname
            /// </summary>
            [Column(Name = "marketname")]
            public override string MarketName { get; set; }
        }

        [Table(Name = "sys_cloudserver_s")]
        public partial class Server : Model.Server
        {
            /// <summary>
            /// 初始化 <see cref="Server"/> 类的新实例
            /// </summary>
            public Server()
            {
            }

            /// <summary>
            /// 初始化 <see cref="Server"/> 类的新实例
            /// </summary>
            public Server(Server model)
            {
            }

            /// <summary>
            /// cloudserverid
            /// </summary>
            [Column(IsKey = true, Name = "cloudserverid")]
            public override int CloudServerId { get; set; }

            /// <summary>
            /// cloudservercode
            /// </summary>
            [Column(Name = "cloudservercode")]
            public override string CloudServerCode { get; set; }

            /// <summary>
            /// cloudservername
            /// </summary>
            [Column(Name = "cloudservername")]
            public override string CloudServerName { get; set; }
        }
    }
}
