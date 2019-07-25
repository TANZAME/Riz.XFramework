
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TZM.XFramework.Data;

namespace TZM.XFramework.UnitTest
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
            [Column(DbType = System.Data.DbType.DateTime2, Precision = 6)]
            public virtual DateTime DemoDateTime2 { get; set; }

            /// <summary>
            /// demodatetime2_nullable
            /// </summary>
            [Column(DbType = System.Data.DbType.DateTime2, Precision = 6)]
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
            public virtual IList<ClientAccount> Accounts { get; set; }

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
