
using System;
using System.Data;
using System.Linq;
using System.Diagnostics;
using TZM.XFramework.Data;

namespace TZM.XFramework.UnitTest
{
    public class Program
    {
        // 包还原失败参考
        // https://docs.microsoft.com/zh-cn/nuget/consume-packages/package-restore-troubleshooting#missing

        [MTAThread]
        //[STAThread]
        public static void Main(string[] args)
        {
            var stop = new Stopwatch();
            string connString = "Server=.;Database=TZM_XFramework;uid=sa;pwd=123456;pooling=true;max pool size=1;min pool size=1;connect timeout=10;";
            var context = new TZM.XFramework.Data.SqlClient.SqlServerDbContext(connString);

            var query =
                from a in
                    context
                    .GetTable<Model.Client>()
                    .Include(a => a.CloudServer)
                    .Include(a => a.Accounts)
                    .Include(a => a.Accounts[0].Markets)
                    .Include(a => a.Accounts[0].Markets[0].Client)
                group a by new { a.ClientId, a.ClientCode, a.ClientName, a.CloudServer.CloudServerId } into g
                select new Model.Client
                {
                    ClientId = g.Key.ClientId,
                    ClientCode = g.Key.ClientCode,
                    ClientName = g.Key.ClientName,
                    CloudServerId = g.Key.CloudServerId,
                    Qty = g.Sum(a => a.Qty)
                };
            query = query
                .Where(a => a.ClientId > 0)
                .OrderBy(a => a.ClientId)
                .Skip(10)
                .Take(20);
            //var result1 = query1.ToList();

            stop = new Stopwatch();
            stop.Start();
            DateTime sDate2 = DateTime.Now;
            for (int i = 0; i < 1000000; i++)
            {
                var cmd = query.Resolve();
            }
            stop.Stop();
            Console.WriteLine(string.Format("解析 100w 次，用时：{0}", stop.Elapsed));
            Console.ReadLine();
        }


        public class Model
        {
            [Table(Name = "Sys_Demo")]
            public partial class Demo
            {
                /// <summary>
                /// 初始化 <see cref="Demo"/> 类的新实例
                /// </summary>
                public Demo()
                {
                    this.HookConstructor();
                }

                /// <summary>
                /// 初始化 <see cref="Demo"/> 类的新实例
                /// </summary>
                public Demo(int demoId, string demoName)
                {

                }

                /// <summary>
                /// 初始化 <see cref="Demo"/> 类的新实例
                /// </summary>
                public Demo(Demo model)
                {
                    this.HookConstructor();
                }

                /// <summary>
                /// demoid
                /// </summary>
                [Column(IsKey = true, IsIdentity = true)]
                public virtual int DemoId { get; set; }

                /// <summary>
                /// democode
                /// </summary>
                [Column(DbType = System.Data.DbType.AnsiString, Size = 32)]
                public virtual string DemoCode { get; set; }

                /// <summary>
                /// demoname
                /// </summary>
                [Column(DbType = System.Data.DbType.String, Size = 32)]
                public virtual string DemoName { get; set; }

                /// <summary>
                /// demoboolean
                /// </summary>
                public virtual bool DemoBoolean { get; set; }

                /// <summary>
                /// demoboolean_nullable
                /// </summary>
                public virtual Nullable<bool> DemoBoolean_Nullable { get; set; }

                /// <summary>
                /// demochar
                /// </summary>
                [Column(DbType = System.Data.DbType.AnsiStringFixedLength, Size = 1)]
                public virtual char DemoChar { get; set; }

                /// <summary>
                /// demochar
                /// </summary>
                [Column(DbType = System.Data.DbType.StringFixedLength, Size = 1)]
                public virtual char DemoNChar { get; set; }

                /// <summary>
                /// demochar_nullable
                /// </summary>
                [Column(DbType = System.Data.DbType.AnsiStringFixedLength, Size = 1)]
                public virtual Nullable<char> DemoChar_Nullable { get; set; }

                /// <summary>
                /// demobyte
                /// </summary>
                public virtual byte DemoByte { get; set; }

                /// <summary>
                /// demobyte_nullable
                /// </summary>
                public virtual Nullable<byte> DemoByte_Nullable { get; set; }

                /// <summary>
                /// demodate
                /// </summary>
                [Column(DbType = System.Data.DbType.Date)]
                public virtual DateTime DemoDate { get; set; }

                /// <summary>
                /// demodate_nullable
                /// </summary>
                [Column(DbType = System.Data.DbType.Date)]
                public virtual Nullable<DateTime> DemoDate_Nullable { get; set; }

                /// <summary>
                /// demodatetime
                /// </summary>
                public virtual DateTime DemoDateTime { get; set; }

                /// <summary>
                /// demodatetime_nullable
                /// </summary>
                public virtual Nullable<DateTime> DemoDateTime_Nullable { get; set; }

                /// <summary>
                /// demodatetime2
                /// </summary>   
                [Column(DbType = System.Data.DbType.DateTime2, Scale = 7)]
                public virtual DateTime DemoDateTime2 { get; set; }

                /// <summary>
                /// demodatetime2_nullable
                /// </summary>
                [Column(DbType = System.Data.DbType.DateTime2, Scale = 7)]
                public virtual Nullable<DateTime> DemoDateTime2_Nullable { get; set; }

                /// <summary>
                /// demotime_nullable
                /// </summary>
                [Column(DbType = System.Data.DbType.Time, Scale = 7)]
                public virtual Nullable<TimeSpan> DemoTime_Nullable { get; set; }

                /// <summary>
                /// demodatetimeoffset_nullable
                /// </summary>
                [Column(DbType = System.Data.DbType.DateTimeOffset, Scale = 7)]
                public virtual Nullable<DateTimeOffset> DemoDatetimeOffset_Nullable { get; set; }

                /// <summary>
                /// demodecimal
                /// </summary>
                public virtual decimal DemoDecimal { get; set; }

                /// <summary>
                /// demodecimal_nullable
                /// </summary>
                public virtual Nullable<decimal> DemoDecimal_Nullable { get; set; }

                /// <summary>
                /// demodouble
                /// </summary>
                public virtual double DemoDouble { get; set; }

                /// <summary>
                /// demodouble_nullable
                /// </summary>
                public virtual Nullable<double> DemoDouble_Nullable { get; set; }

                /// <summary>
                /// demofloat
                /// </summary>
                public virtual float DemoFloat { get; set; }

                /// <summary>
                /// demofloat_nullable
                /// </summary>
                public virtual Nullable<float> DemoFloat_Nullable { get; set; }

                /// <summary>
                /// demoguid
                /// </summary>
                public virtual Guid DemoGuid { get; set; }

                /// <summary>
                /// demoguid_nullable
                /// </summary>
                public virtual Nullable<Guid> DemoGuid_Nullable { get; set; }

                /// <summary>
                /// demoshort
                /// </summary>
                public virtual short DemoShort { get; set; }

                /// <summary>
                /// demoshort_nullable
                /// </summary>
                public virtual Nullable<short> DemoShort_Nullable { get; set; }

                /// <summary>
                /// demoint
                /// </summary>
                public virtual int DemoInt { get; set; }

                /// <summary>
                /// demoint_nullable
                /// </summary>
                public virtual Nullable<int> DemoInt_Nullable { get; set; }

                /// <summary>
                /// demolong
                /// </summary>
                public virtual long DemoLong { get; set; }

                /// <summary>
                /// demolong_nullable
                /// </summary>
                public virtual Nullable<long> DemoLong_Nullable { get; set; }

                /// <summary>
                /// 构造函数勾子
                /// </summary>
                partial void HookConstructor();
            }

            [Table(Name = "Bas_Client")]
            public partial class Client
            {
                /// <summary>
                /// 初始化 <see cref="Client"/> 类的新实例
                /// </summary>
                public Client()
                {
                    this.CloudServerId = 0;
                    this.Qty = 0;
                    this.HookConstructor();
                }

                /// <summary>
                /// 初始化 <see cref="Client"/> 类的新实例
                /// </summary>
                public Client(Client model)
                {
                    this.CloudServerId = 0;
                    this.Qty = 0;
                    this.HookConstructor();
                }

                /// <summary>
                /// clientid
                /// </summary>
                [Column(IsKey = true)]
                public virtual int ClientId { get; set; }

                /// <summary>
                /// clientcode
                /// </summary>
                public virtual string ClientCode { get; set; }

                /// <summary>
                /// clientname
                /// </summary>
                public virtual string ClientName { get; set; }

                /// <summary>
                /// cloudserverid
                /// </summary>
                [Column(Default = 0)]
                public virtual int CloudServerId { get; set; }

                /// <summary>
                /// activedate
                /// </summary>
                public virtual Nullable<DateTime> ActiveDate { get; set; }

                /// <summary>
                /// qty
                /// </summary>
                [Column(Default = 0)]
                public virtual int Qty { get; set; }

                /// <summary>
                /// state
                /// </summary>
                public virtual byte State { get; set; }

                /// <summary>
                /// remark
                /// </summary>
                [Column(Default = "'默认值'")]
                public virtual string Remark { get; set; }

                [ForeignKey("CloudServerId")]
                public virtual CloudServer CloudServer { get; set; }

                [ForeignKey("CloudServerId")]
                public virtual CloudServer LocalServer { get; set; }

                [ForeignKey("ClientId")]
                public virtual System.Collections.Generic.IList<ClientAccount> Accounts { get; set; }

                /// <summary>
                /// 构造函数勾子
                /// </summary>
                partial void HookConstructor();
            }

            [Table(Name = "Bas_ClientAccount")]
            public partial class ClientAccount
            {
                /// <summary>
                /// 初始化 <see cref="ClientAccount"/> 类的新实例
                /// </summary>
                public ClientAccount()
                {
                    this.HookConstructor();
                }

                /// <summary>
                /// 初始化 <see cref="ClientAccount"/> 类的新实例
                /// </summary>
                public ClientAccount(ClientAccount model)
                {
                    this.HookConstructor();
                }

                /// <summary>
                /// clientid
                /// </summary>
                [Column(IsKey = true)]
                public virtual int ClientId { get; set; }

                /// <summary>
                /// accountid
                /// </summary>
                [Column(IsKey = true)]
                public virtual string AccountId { get; set; }

                /// <summary>
                /// accountcode
                /// </summary>
                public virtual string AccountCode { get; set; }

                /// <summary>
                /// accountname
                /// </summary>
                public virtual string AccountName { get; set; }

                /// <summary>
                /// qty
                /// </summary>
                [Column(Default = 0)]
                public virtual int Qty { get; set; }

                [ForeignKey("ClientId")]
                public virtual Client Client { get; set; }

                [ForeignKey(new[] { "ClientId", "AccountId" })]
                public virtual System.Collections.Generic.IList<ClientAccountMarket> Markets { get; set; }

                /// <summary>
                /// 构造函数勾子
                /// </summary>
                partial void HookConstructor();
            }

            [Table(Name = "Bas_ClientAccountMarket")]
            public partial class ClientAccountMarket
            {
                /// <summary>
                /// 初始化 <see cref="ClientAccountMarket"/> 类的新实例
                /// </summary>
                public ClientAccountMarket()
                {
                    this.HookConstructor();
                }

                /// <summary>
                /// 初始化 <see cref="ClientAccountMarket"/> 类的新实例
                /// </summary>
                public ClientAccountMarket(ClientAccountMarket model)
                {
                    this.HookConstructor();
                }

                /// <summary>
                /// clientid
                /// </summary>
                [Column(IsKey = true)]
                public virtual int ClientId { get; set; }

                /// <summary>
                /// accountid
                /// </summary>
                [Column(IsKey = true)]
                public virtual string AccountId { get; set; }

                /// <summary>
                /// marketid
                /// </summary>
                [Column(IsKey = true)]
                public virtual int MarketId { get; set; }

                /// <summary>
                /// marketcode
                /// </summary>
                public virtual string MarketCode { get; set; }

                /// <summary>
                /// marketname
                /// </summary>
                public virtual string MarketName { get; set; }

                [ForeignKey("ClientId")]
                public virtual Client Client { get; set; }

                /// <summary>
                /// 构造函数勾子
                /// </summary>
                partial void HookConstructor();
            }

            [Table(Name = "Sys_CloudServer")]
            public partial class CloudServer
            {
                /// <summary>
                /// 初始化 <see cref="CloudServer"/> 类的新实例
                /// </summary>
                public CloudServer()
                {
                    this.HookConstructor();
                }

                /// <summary>
                /// 初始化 <see cref="CloudServer"/> 类的新实例
                /// </summary>
                public CloudServer(CloudServer model)
                {
                    this.HookConstructor();
                }

                /// <summary>
                /// cloudserverid
                /// </summary>
                [Column(IsKey = true)]
                public virtual int CloudServerId { get; set; }

                /// <summary>
                /// cloudservercode
                /// </summary>
                public virtual string CloudServerCode { get; set; }

                /// <summary>
                /// cloudservername
                /// </summary>
                public virtual string CloudServerName { get; set; }

                /// <summary>
                /// 构造函数勾子
                /// </summary>
                partial void HookConstructor();
            }

            [Table(Name = "Sys_Rabbit")]
            public partial class Rabbit
            {
                /// <summary>
                /// 初始化 <see cref="Demo"/> 类的新实例
                /// </summary>
                public Rabbit()
                {
                    this.HookConstructor();
                }

                /// <summary>
                /// 初始化 <see cref="Demo"/> 类的新实例
                /// </summary>
                public Rabbit(int demoId, string demoName)
                {

                }

                /// <summary>
                /// 初始化 <see cref="Demo"/> 类的新实例
                /// </summary>
                public Rabbit(Rabbit model)
                {
                    this.HookConstructor();
                }

                /// <summary>
                /// demoid
                /// </summary>
                [Column(IsKey = true, IsIdentity = true)]
                public virtual int DemoId { get; set; }

                /// <summary>
                /// democode
                /// </summary>
                public virtual string DemoCode { get; set; }

                /// <summary>
                /// demoname
                /// </summary>
                public virtual string DemoName { get; set; }

                /// <summary>
                /// demoboolean
                /// </summary>
                public virtual bool DemoBoolean { get; set; }

                /// <summary>
                /// demoboolean_nullable
                /// </summary>
                public virtual Nullable<bool> DemoBoolean_Nullable { get; set; }

                /// <summary>
                /// demochar
                /// </summary>
                public virtual string DemoChar { get; set; }

                /// <summary>
                /// demochar_nullable
                /// </summary>
                public virtual string DemoChar_Nullable { get; set; }

                /// <summary>
                /// demobyte
                /// </summary>
                public virtual byte DemoByte { get; set; }

                /// <summary>
                /// demobyte_nullable
                /// </summary>
                public virtual Nullable<byte> DemoByte_Nullable { get; set; }

                /// <summary>
                /// demodate
                /// </summary>
                [Column(DbType = System.Data.DbType.Date)]
                public virtual DateTime DemoDate { get; set; }

                /// <summary>
                /// demodate_nullable
                /// </summary>
                public virtual Nullable<DateTime> DemoDate_Nullable { get; set; }

                /// <summary>
                /// demodatetime
                /// </summary>
                public virtual DateTime DemoDateTime { get; set; }

                /// <summary>
                /// demodatetime_nullable
                /// </summary>
                public virtual Nullable<DateTime> DemoDateTime_Nullable { get; set; }

                /// <summary>
                /// demodatetime2
                /// </summary>   
                [Column(DbType = System.Data.DbType.DateTime2, Scale = 6)]
                public virtual DateTime DemoDateTime2 { get; set; }

                /// <summary>
                /// demodatetime2_nullable
                /// </summary>
                public virtual Nullable<DateTime> DemoDateTime2_Nullable { get; set; }

                /// <summary>
                /// demodecimal
                /// </summary>
                public virtual decimal DemoDecimal { get; set; }

                /// <summary>
                /// demodecimal_nullable
                /// </summary>
                public virtual Nullable<decimal> DemoDecimal_Nullable { get; set; }

                /// <summary>
                /// demodouble
                /// </summary>
                public virtual double DemoDouble { get; set; }

                /// <summary>
                /// demodouble_nullable
                /// </summary>
                public virtual Nullable<double> DemoDouble_Nullable { get; set; }

                /// <summary>
                /// demofloat
                /// </summary>
                public virtual float DemoFloat { get; set; }

                /// <summary>
                /// demofloat_nullable
                /// </summary>
                public virtual Nullable<float> DemoFloat_Nullable { get; set; }

                /// <summary>
                /// demoguid
                /// </summary>
                public virtual Guid DemoGuid { get; set; }

                /// <summary>
                /// demoguid_nullable
                /// </summary>
                public virtual Nullable<Guid> DemoGuid_Nullable { get; set; }

                /// <summary>
                /// demoshort
                /// </summary>
                public virtual short DemoShort { get; set; }

                /// <summary>
                /// demoshort_nullable
                /// </summary>
                public virtual Nullable<short> DemoShort_Nullable { get; set; }

                /// <summary>
                /// demoint
                /// </summary>
                public virtual int DemoInt { get; set; }

                /// <summary>
                /// demoint_nullable
                /// </summary>
                public virtual Nullable<int> DemoInt_Nullable { get; set; }

                /// <summary>
                /// demolong
                /// </summary>
                public virtual long DemoLong { get; set; }

                /// <summary>
                /// demolong_nullable
                /// </summary>
                public virtual Nullable<long> DemoLong_Nullable { get; set; }

                /// <summary>
                /// 构造函数勾子
                /// </summary>
                partial void HookConstructor();
            }

            public enum State
            {
                Executing = 0,
                Complete = 1
            }
        }
    }
}