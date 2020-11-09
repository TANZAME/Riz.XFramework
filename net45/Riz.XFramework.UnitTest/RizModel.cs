
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Riz.XFramework.Data;

namespace Riz.XFramework.UnitTest
{
    // 说明：
    // 1.如果类有 TableAttribute，则用 TableAttribute 指定的名称做为表名，否则用类名称做为表名
    // 2.删除/更新时如果传递的参数是一个实体，必须使用 [Column(IsKey = true)] 指定实体的主键
    // 3.ForeignKeyAttribute 指定外键，一对多关系未实现，但可以用其它方法变通，示例中会给出
    // 4.支持原汁原味的LINQ语法，Lambda表达式

    // [Table]特性说明
    // 若类指定 TableAttribute，则表名取 TableAttribute.Name，否则表名取 类名称

    // [Column]特性说明
    // 若属性指定 ColumnAttribute.NoMapped，则在生成 INSERT 和 UPDATE 语句时会忽略这个字段

    // 代码生成模板说明

    public class RizModel
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
            [Column(IsKey = true, IsIdentity = true, Name = "DemoId")]
            public virtual int RizDemoId { get; set; }

            /// <summary>
            /// democode
            /// </summary>
            [Column(DbType = System.Data.DbType.AnsiString, Size = 32, Name = "DemoCode")]
            public virtual string RizDemoCode { get; set; }

            /// <summary>
            /// demoname
            /// </summary>
            [Column(DbType = System.Data.DbType.String, Size = 32, Name = "DemoName")]
            public virtual string RizDemoName { get; set; }

            /// <summary>
            /// demoboolean
            /// </summary>
            [Column(Name = "DemoBoolean")]
            public virtual bool RizDemoBoolean { get; set; }

            /// <summary>
            /// demoboolean_nullable
            /// </summary>
            [Column(Name = "DemoBoolean_Nullable")]
            public virtual Nullable<bool> RizDemoBoolean_Nullable { get; set; }

            /// <summary>
            /// demochar
            /// </summary>
            [Column(DbType = System.Data.DbType.AnsiStringFixedLength, Size = 1, Name = "DemoChar")]
            public virtual char RizDemoChar { get; set; }

            /// <summary>
            /// demochar
            /// </summary>
            [Column(DbType = System.Data.DbType.StringFixedLength, Size = 1, Name = "DemoNChar")]
            public virtual char RizDemoNChar { get; set; }

            /// <summary>
            /// demochar_nullable
            /// </summary>
            [Column(DbType = System.Data.DbType.AnsiStringFixedLength, Size = 1, Name = "DemoChar_Nullable")]
            public virtual Nullable<char> RizDemoChar_Nullable { get; set; }

            /// <summary>
            /// demobyte
            /// </summary>
            [Column(Name = "DemoByte")]
            public virtual byte RizDemoByte { get; set; }

            /// <summary>
            /// demobyte_nullable
            /// </summary>
            [Column(Name = "DemoByte_Nullable")]
            public virtual Nullable<byte> RizDemoByte_Nullable { get; set; }

            /// <summary>
            /// demodate
            /// </summary>
            [Column(DbType = System.Data.DbType.Date, Name = "DemoDate")]
            public virtual DateTime RizDemoDate { get; set; }

            /// <summary>
            /// demodate_nullable
            /// </summary>
            [Column(DbType = System.Data.DbType.Date, Name = "DemoDate_Nullable")]
            public virtual Nullable<DateTime> RizDemoDate_Nullable { get; set; }

            /// <summary>
            /// demodatetime
            /// </summary>
            [Column(Name = "DemoDateTime")]
            public virtual DateTime RizDemoDateTime { get; set; }

            /// <summary>
            /// demodatetime_nullable
            /// </summary>
            [Column(Name = "DemoDateTime_Nullable")]
            public virtual Nullable<DateTime> RizDemoDateTime_Nullable { get; set; }

            /// <summary>
            /// demodatetime2
            /// </summary>   
            [Column(DbType = System.Data.DbType.DateTime2, Scale = 7, Name = "DemoDateTime2")]
            public virtual DateTime RizDemoDateTime2 { get; set; }

            /// <summary>
            /// demodatetime2_nullable
            /// </summary>
            [Column(DbType = System.Data.DbType.DateTime2, Scale = 7, Name = "DemoDateTime2_Nullable")]
            public virtual Nullable<DateTime> RizDemoDateTime2_Nullable { get; set; }

            /// <summary>
            /// demotime_nullable
            /// </summary>
            [Column(DbType = System.Data.DbType.Time, Scale = 7, Name = "DemoTime_Nullable")]
            public virtual Nullable<TimeSpan> RizDemoTime_Nullable { get; set; }

            /// <summary>
            /// demodatetimeoffset_nullable
            /// </summary>
            [Column(DbType = System.Data.DbType.DateTimeOffset, Scale = 7, Name = "DemoDatetimeOffset_Nullable")]
            public virtual Nullable<DateTimeOffset> RizDemoDatetimeOffset_Nullable { get; set; }

            /// <summary>
            /// demodecimal
            /// </summary>
            [Column(Name = "DemoDecimal")]
            public virtual decimal RizDemoDecimal { get; set; }

            /// <summary>
            /// demodecimal_nullable
            /// </summary>
            [Column(Name = "DemoDecimal_Nullable")]
            public virtual Nullable<decimal> RizDemoDecimal_Nullable { get; set; }

            /// <summary>
            /// demodouble
            /// </summary>
            [Column(Name = "DemoDouble")]
            public virtual double RizDemoDouble { get; set; }

            /// <summary>
            /// demodouble_nullable
            /// </summary>
            [Column(Name = "DemoDouble_Nullable")]
            public virtual Nullable<double> RizDemoDouble_Nullable { get; set; }

            /// <summary>
            /// demofloat
            /// </summary>
            [Column(Name = "DemoFloat")]
            public virtual float RizDemoFloat { get; set; }

            /// <summary>
            /// demofloat_nullable
            /// </summary>
            [Column(Name = "DemoFloat_Nullable")]
            public virtual Nullable<float> RizDemoFloat_Nullable { get; set; }

            /// <summary>
            /// demoguid
            /// </summary>
            [Column(Name = "DemoGuid")]
            public virtual Guid RizDemoGuid { get; set; }

            /// <summary>
            /// demoguid_nullable
            /// </summary>
            [Column(Name = "DemoGuid_Nullable")]
            public virtual Nullable<Guid> RizDemoGuid_Nullable { get; set; }

            /// <summary>
            /// demoshort
            /// </summary>
            [Column(Name = "DemoShort")]
            public virtual short RizDemoShort { get; set; }

            /// <summary>
            /// demoshort_nullable
            /// </summary>
            [Column(Name = "DemoShort_Nullable")]
            public virtual Nullable<short> RizDemoShort_Nullable { get; set; }

            /// <summary>
            /// demoint
            /// </summary>
            [Column(Name = "DemoInt")]
            public virtual int RizDemoInt { get; set; }

            /// <summary>
            /// demoint_nullable
            /// </summary>
            [Column(Name = "DemoInt_Nullable")]
            public virtual Nullable<int> RizDemoInt_Nullable { get; set; }

            /// <summary>
            /// demolong
            /// </summary>
            [Column(Name = "DemoLong")]
            public virtual long RizDemoLong { get; set; }

            /// <summary>
            /// demolong_nullable
            /// </summary>
            [Column(Name = "DemoLong_Nullable")]
            public virtual Nullable<long> RizDemoLong_Nullable { get; set; }

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
                this.RizCloudServerId = 0;
                this.RizQty = 0;
                this.HookConstructor();
            }

            /// <summary>
            /// 初始化 <see cref="Client"/> 类的新实例
            /// </summary>
            public Client(Client model)
            {
                this.RizCloudServerId = 0;
                this.RizQty = 0;
                this.HookConstructor();
            }

            /// <summary>
            /// clientid
            /// </summary>
            [Column(IsKey = true, Name = "ClientId")]
            public virtual int RizClientId { get; set; }

            /// <summary>
            /// clientcode
            /// </summary>
            [Column(Name = "ClientCode")]
            public virtual string RizClientCode { get; set; }

            /// <summary>
            /// clientname
            /// </summary>
            [Column(Name = "ClientName")]
            public virtual string RizClientName { get; set; }

            /// <summary>
            /// cloudserverid
            /// </summary>
            [Column(Default = 0, Name = "CloudServerId")]
            public virtual int RizCloudServerId { get; set; }

            /// <summary>
            /// activedate
            /// </summary>
            [Column(Name = "ActiveDate")]
            public virtual Nullable<DateTime> RizActiveDate { get; set; }

            /// <summary>
            /// qty
            /// </summary>
            [Column(Default = 0, Name = "Qty")]
            public virtual int RizQty { get; set; }

            /// <summary>
            /// state
            /// </summary>
            [Column(Name = "State")]
            public virtual byte RizState { get; set; }

            /// <summary>
            /// remark
            /// </summary>
            [Column(Default = "'默认值'", Name = "Remark")]
            public virtual string RizRemark { get; set; }

            //[ForeignKey("CloudServerId")]
            public virtual CloudServer CloudServer { get; set; }

            [ForeignKey("CloudServerId")]
            public virtual CloudServer LocalServer { get; set; }

            //[ForeignKey("ClientId")]
            public virtual IList<ClientAccount> Accounts { get; set; }

            public virtual ICollection<ClientAccountMarket> Markets { get; set; }

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
            [Column(IsKey = true, Name = "ClientId")]
            public virtual int RizClientId { get; set; }

            /// <summary>
            /// accountid
            /// </summary>
            [Column(IsKey = true, Name = "AccountId")]
            public virtual string RizAccountId { get; set; }

            /// <summary>
            /// accountcode
            /// </summary>
            [Column(Name = "AccountCode")]
            public virtual string RizAccountCode { get; set; }

            /// <summary>
            /// accountname
            /// </summary>
            [Column(Name = "AccountName")]
            public virtual string RizAccountName { get; set; }

            /// <summary>
            /// qty
            /// </summary>
            [Column(Default = 0, Name = "Qty")]
            public virtual int RizQty { get; set; }

            [ForeignKey("ClientId")]
            public virtual Client Client { get; set; }

            [ForeignKey(new[] { "ClientId", "AccountId" }, new[] { "ClientId", "{CONST}'2'" })]
            public virtual IList<ClientAccountMarket> Markets { get; set; }

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
            [Column(IsKey = true, Name = "ClientId")]
            public virtual int RizClientId { get; set; }

            /// <summary>
            /// accountid
            /// </summary>
            [Column(IsKey = true, Name = "AccountId")]
            public virtual string RizAccountId { get; set; }

            /// <summary>
            /// marketid
            /// </summary>
            [Column(IsKey = true, Name = "MarketId")]
            public virtual int RizMarketId { get; set; }

            /// <summary>
            /// marketcode
            /// </summary>
            [Column(Name = "MarketCode")]
            public virtual string RizMarketCode { get; set; }

            /// <summary>
            /// marketname
            /// </summary>
            [Column(Name = "MarketName")]
            public virtual string RizMarketName { get; set; }

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
            [Column(IsKey = true, Name = "CloudServerId")]
            public virtual int RizCloudServerId { get; set; }

            /// <summary>
            /// cloudservercode
            /// </summary>
            [Column(Name = "CloudServerCode")]
            public virtual string RizCloudServerCode { get; set; }

            /// <summary>
            /// cloudservername
            /// </summary>
            [Column(Name = "CloudServerName")]
            public virtual string RizCloudServerName { get; set; }

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
            [Column(IsKey = true, IsIdentity = true, Name = "DemoId")]
            public virtual int RizDemoId { get; set; }

            /// <summary>
            /// democode
            /// </summary>
            [Column(Name = "DemoCode")]
            public virtual string RizDemoCode { get; set; }

            /// <summary>
            /// demoname
            /// </summary>
            [Column(Name = "DemoName")]
            public virtual string RizDemoName { get; set; }

            /// <summary>
            /// demoboolean
            /// </summary>
            [Column(Name = "DemoBoolean")]
            public virtual bool RizDemoBoolean { get; set; }

            /// <summary>
            /// demoboolean_nullable
            /// </summary>
            [Column(Name = "DemoBoolean_Nullable")]
            public virtual Nullable<bool> RizDemoBoolean_Nullable { get; set; }

            /// <summary>
            /// demochar
            /// </summary>
            [Column(Name = "DemoChar")]
            public virtual string RizDemoChar { get; set; }

            /// <summary>
            /// demochar_nullable
            /// </summary>
            [Column(Name = "DemoChar_Nullable")]
            public virtual string RizDemoChar_Nullable { get; set; }

            /// <summary>
            /// demobyte
            /// </summary>
            [Column(Name = "DemoByte")]
            public virtual byte RizDemoByte { get; set; }

            /// <summary>
            /// demobyte_nullable
            /// </summary>
            [Column(Name = "DemoByte_Nullable")]
            public virtual Nullable<byte> RizDemoByte_Nullable { get; set; }

            /// <summary>
            /// demodate
            /// </summary>
            [Column(DbType = System.Data.DbType.Date, Name = "DemoDate")]
            public virtual DateTime RizDemoDate { get; set; }

            /// <summary>
            /// demodate_nullable
            /// </summary>
            [Column(Name = "DemoDate_Nullable")]
            public virtual Nullable<DateTime> RizDemoDate_Nullable { get; set; }

            /// <summary>
            /// demodatetime
            /// </summary>
            [Column(Name = "DemoDateTime")]
            public virtual DateTime RizDemoDateTime { get; set; }

            /// <summary>
            /// demodatetime_nullable
            /// </summary>
            [Column(Name = "DemoDateTime_Nullable")]
            public virtual Nullable<DateTime> RizDemoDateTime_Nullable { get; set; }

            /// <summary>
            /// demodatetime2
            /// </summary>   
            [Column(DbType = System.Data.DbType.DateTime2, Scale = 6, Name = "DemoDateTime2")]
            public virtual DateTime RizDemoDateTime2 { get; set; }

            /// <summary>
            /// demodatetime2_nullable
            /// </summary>
            [Column(Name = "DemoDateTime2_Nullable")]
            public virtual Nullable<DateTime> RizDemoDateTime2_Nullable { get; set; }

            /// <summary>
            /// demodecimal
            /// </summary>
            [Column(Name = "DemoDecimal")]
            public virtual decimal RizDemoDecimal { get; set; }

            /// <summary>
            /// demodecimal_nullable
            /// </summary>
            [Column(Name = "DemoDecimal_Nullable")]
            public virtual Nullable<decimal> RizDemoDecimal_Nullable { get; set; }

            /// <summary>
            /// demodouble
            /// </summary>
            [Column(Name = "DemoDouble")]
            public virtual double RizDemoDouble { get; set; }

            /// <summary>
            /// demodouble_nullable
            /// </summary>
            [Column(Name = "DemoDouble_Nullable")]
            public virtual Nullable<double> RizDemoDouble_Nullable { get; set; }

            /// <summary>
            /// demofloat
            /// </summary>
            [Column(Name = "DemoFloat")]
            public virtual float RizDemoFloat { get; set; }

            /// <summary>
            /// demofloat_nullable
            /// </summary>
            [Column(Name = "DemoFloat_Nullable")]
            public virtual Nullable<float> RizDemoFloat_Nullable { get; set; }

            /// <summary>
            /// demoguid
            /// </summary>
            [Column(Name = "DemoGuid")]
            public virtual Guid RizDemoGuid { get; set; }

            /// <summary>
            /// demoguid_nullable
            /// </summary>
            [Column(Name = "DemoGuid_Nullable")]
            public virtual Nullable<Guid> RizDemoGuid_Nullable { get; set; }

            /// <summary>
            /// demoshort
            /// </summary>
            [Column(Name = "DemoShort")]
            public virtual short RizDemoShort { get; set; }

            /// <summary>
            /// demoshort_nullable
            /// </summary>
            [Column(Name = "DemoShort_Nullable")]
            public virtual Nullable<short> RizDemoShort_Nullable { get; set; }

            /// <summary>
            /// demoint
            /// </summary>
            [Column(Name = "DemoInt")]
            public virtual int RizDemoInt { get; set; }

            /// <summary>
            /// demoint_nullable
            /// </summary>
            [Column(Name = "DemoInt_Nullable")]
            public virtual Nullable<int> RizDemoInt_Nullable { get; set; }

            /// <summary>
            /// demolong
            /// </summary>
            [Column(Name = "DemoLong")]
            public virtual long RizDemoLong { get; set; }

            /// <summary>
            /// demolong_nullable
            /// </summary>
            [Column(Name = "DemoLong_Nullable")]
            public virtual Nullable<long> RizDemoLong_Nullable { get; set; }

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
