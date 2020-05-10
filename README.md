<p align="center">
  <img height="100" src="https://images.cnblogs.com/cnblogs_com/yiting/816517/o_200509140030Cgp3O1eXACaAGPjjAAI1Ux2fNtk168.png"/>
</p>

TZM.XFramewrok 是一款基于.NET的轻量级高性能 ORM 框架，参考 EntiryFramework 的设计思想，保留大量原汁原味的微软API。支持 Fx 4.0+ 和 .NETCore3.0，支持批量增删改查、导航属性、链式查询（点标记）和查询表达式等等。

[![NuGet](https://img.shields.io/nuget/vpre/TZM.XFramework.svg)](https://www.nuget.org/packages/TZM.XFramework)
[![GitHub license](https://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/TANZAME/TZM.XFramework/blob/master/LICENSE.txt)

## 亮点
- 原生.NET语法，零学习成本
- 支持LINQ查询、拉姆达表达式
- 支持批量增删改查和多表关联更新
- 支持 SqlServer、MySql、Postgre、Oracle
- 支持无限级一对一、一对多导航属性
- 实体字段类型不必与数据库的类型一致
- 提供原生Ado.NET操作
- 支持临时表、表变量操作
- 更多####

## 联系方式
- Email：tian_naixiang@sina.com 
- 技术交流QQ群：816425449
***
#### 快速开始
- ###### 安装
```
PM> Install-Package TZM.XFramework
```

```
.net 4.5
SqlServer => TZM.XFramework
MySql     => TZM.XFramework.MySql
Oracle    => TZM.XFramework.Oracle
Postgre   => TZM.XFramework.Postgre
SQLite    => TZM.XFramework.SQLite
```

```
.net core
SqlServer => TZM.XFrameworkCore
MySql     => TZM.XFrameworkCore.MySql
Oracle    => TZM.XFrameworkCore.Oracle
Postgre   => TZM.XFrameworkCore.Postgre
```
- ###### 实体定义

```
[Table(Name = "Bas_Client")]
public partial class Client
{
    public Client()
    {
    }

    public Client(Client model)
    {
    }

    [Column(IsKey = true)]
    public virtual int ClientId { get; set; }

    [Column(DbType = System.Data.DbType.AnsiString, Size = 32)]
    public virtual string ClientCode { get; set; }

    [Column(Default = 0)]
    public virtual int CloudServerId { get; set; }

    [Column(Default = "'默认值'")]
    public virtual string Remark { get; set; }

    [ForeignKey("CloudServerId")]
    public virtual CloudServer LocalServer { get; set; }

    [ForeignKey("ClientId")]
    public virtual List<ClientAccount> Accounts { get; set; }
}
```
    > 如果类有 TableAttribute，则用 TableAttribute 指定的名称做为表名，否则用类名称做为表名
    > 实体的字段可以指定 ColumnAttribute 特性来说明实体字段与表字段的对应关系，删除/更新时如果传递的参数是一个实体，必须使用 [Column(IsKey = true)] 指定实体的主键
    > ForeignKeyAttribute 指定外键，一对多外键时类型必须是 IList<T> 或者 List<T>
    > ColumnAttribute.DataType 用来指定表字段类型。以SQLSERVER为例，System.String 默认对应 nvarchar 类型。若是varchar类型，需要指定[Column(DbType= DbType.AnsiString)]
    > 使用[Column(Default = '默认值')]来指定字段的默认值，这在插入数据时非常有用
- ###### 实例化上下文
```
-- 声明数据库链接字符串
string connString = "Server=.;Database=***;uid=**;pwd=**;pooling=true;connect timeout=10;";
-- 实例化数据上下文
-- SqlServer
var context = new SqlServerDbContext(connString);
-- MySql 需引用 TZM.XFramework.MySql
var context = new MySqlDbContext(connString);
-- Oracle 需引用 TZM.XFramework.Oracle
var context = new OracleDbContext(connString);
-- Postgre 需引用 TZM.XFramework.Postgre
var context = new NpgDbContext(connString);
-- SQLite 需引用 TZM.XFramework.SQLite
var context = new SQLiteDbContext(connString);
```
- ###### 查询

```
// 基本查询
var query1 = context.GetTable<TDemo>().Where(a => a.DemoId <= 10).ToList();
// 关联查询
var query2 = context.GetTable<TDemo>().Skip(10).Take(10).ToList();
// 关联查询
var query3 =
    from a in context.GetTable<Model.Client>()
    join b in context.GetTable<Model.CloudServer>() on a.CloudServerId equals b.CloudServerId
    where a.ClientId > 0
    select a;
var result3 = query.ToList();
// 导航属性
var query4 = context.GetTable<Model.Client>().Include(a => a.Accounts).ToList()
// 分组查询
var query5 =
    from a in context.GetTable<Model.Client>()
    group a by a.ClientId into g
    select new
    {
        ClientId = g.Key,
        Qty = g.Sum(a => a.Qty)
    };
var result5 = query5.ToList()
// 聚合函数，支持 Max,Min,Average,Sum,Count。这5个函数同样适用于分组查询 
var query6 = context.GetTable<Model.Client>().Max(a => a.ClientId);
// 子查询
var query7 =
        from a in context.GetTable<Model.Client>()
        join b in context.GetTable<Model.CloudServer>() on a.CloudServerId equals b.CloudServerId into u_c
        from b in u_c.DefaultIfEmpty()
        select a;
query7 = query7.OrderBy(a => a.ClientId).Skip(10).Take(10).AsSubQuery();
var query8 = from a in query7
        join b in context.GetTable<Model.Client>() on a.ClientId equals b.ClientId
        select a;
var result8 = query8.ToList();
```
- ###### 删除

```
// 单个删除
var demo = new TDemo { DemoId = 1 };
context.Delete(demo);
context.SubmitChanges();
// 批量删除
context.Delete<TDemo>(a => a.DemoId == 2 || a.DemoId == 3 || a.DemoName == "N0000004");
context.SubmitChanges();
// 关联删除
var query9 =
    from a in context.GetTable<Model.Client>()
    join b in context.GetTable<Model.ClientAccount>() on a.ClientId equals b.ClientId
    join c in context.GetTable<Model.ClientAccountMarket>() on new { b.ClientId, b.AccountId } equals new { c.ClientId, c.AccountId }
    where c.ClientId == 5 && c.AccountId == "1" && c.MarketId == 1
select a;
context.Delete<Model.Client>(query9);
```
- ###### 更新

```
// 单个更新
demo.DemoName = "001'.N";
context.Update(demo);
context.SubmitChanges();

// 批量更新
context.Update<TDemo>(x => new TDemo
{
    DemoDateTime2 = DateTime.UtcNow,
    DemoDateTime2_Nullable = null
}, x => x.DemoName == "001'.N");
context.SubmitChanges();

// 关联更新
// 更新本表值等于从表的字段值
query =
    from a in context.GetTable<Model.Client>()
    join b in context.GetTable<Model.CloudServer>() on a.CloudServerId equals b.CloudServerId
    join c in context.GetTable<Model.ClientAccount>() on a.ClientId equals c.ClientId
    where c.AccountId == "12"
    select a;
context.Update<Model.Client, Model.CloudServer>((a, b) => new Model.Client
{
    CloudServerId = b.CloudServerId,
    Remark = "001.TAN"
}, query);
context.SubmitChanges();
```
- ###### 新增

```
// 新增单条记录
var demo = new TDemo
{
    DemoCode = "D0000001",
    DemoName = "N0000001"
};
context.Insert(demo);
context.SubmitChanges();
// 批量新增
var demos = new List<TDemo>();
for (int i = 0; i < 1002; i++)
{
    TDemo d = new TDemo
    {
        DemoCode = "D0000001",
        DemoName = "N0000001"
    };
    demos.Add(d);
}
context.Insert<TDemo>(demos);
context.SubmitChanges();
// 关联查询新增
nextId ++;
var query =
    from a in context.GetTable<Model.Client>()
    join b in context.GetTable<Model.CloudServer>() on a.CloudServerId equals b.CloudServerId
    where a.ClientId <= 5
    select new Model.Client
    {
        ClientId = SqlMethod.RowNumber<int>(x => a.ClientId) + nextId,
        ClientCode = "ABC2"
    };
context.Insert(query);
context.SubmitChanges();
```

- ###### 事务

```
try
{
    using (var transaction = context.Database.BeginTransaction())
    {
        var result = context.GetTable<Model.Client>().FirstOrDefault(x => x.ClientId <= 10);
        context.Update<Model.Client>(x => new Model.Client
        {
            ClientName = "事务1"
        }, x => x.ClientId == result.ClientId);
        context.SubmitChanges();

        context.Update<Model.Client>(x => new Model.Client
        {
            ClientName = "事务2"
        }, x => x.ClientId == result.ClientId);
        context.SubmitChanges();

        transaction.Commit();
    }
}
finally
{
    context.Dispose();
}
```
- ###### AOP

```
// 配置全局拦截器，比如在 Global.asax 里配置
var interceptor = new DbCommandInterceptor
{
    OnExecuting = cmd =>
    {
        // TODO
    },
    OnExecuted = cmd => 
    {
        // TODO
    }
};
DbInterception.Add(interceptor);
```


#### 更多实例
参看 [基于.NET的轻量级高性能 ORM - TZM.XFramework](https://www.cnblogs.com/yiting/p/10952302.html)
或下载源码
