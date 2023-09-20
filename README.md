## :cn: Riz.XFramework

[![NuGet](https://img.shields.io/nuget/vpre/Riz.XFramework.svg)](https://www.nuget.org/packages/Riz.XFramework)
[![stats](https://img.shields.io/nuget/dt/Riz.XFramework?style=flat-square)](https://www.nuget.org/stats/packages/Riz.XFramework?groupby=Version) 
[![GitHub license](https://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/TANZAME/Riz.XFramework/blob/master/LICENSE.txt)

Riz.XFramework 是一款基于.NET的轻量级高性能 ORM 框架，参考 EntiryFramework 的设计思想，保留大量原汁原味的微软API，支持 Fx 4.0+ 和 DotNetCore3.1+。

## 功能特点
- 原生 EF 语法，完整的代码注释，零门槛上手
- 支持 Linq 查询、Lambda 表达式
- 支持丰富的表达式函数、链式查询
- 支持批量增删改查和多表关联更新
- 支持 SqlServer、MySql、Postgre、Oracle、SQLite 等多种数据库
- 支持 Postgre、Oracle 大小写敏感
- 支持无限级一对一、一对多导航属性、延迟加载
- 支持实体属性类型与数据库字段不同类型、不同名称映射
- 支持多架构(schema)设计
- 支持原生 Ado.Net、临时表、表变量、AOP操作

## 联系方式
- Email：tian_naixiang@sina.com 
- .NET技术交流QQ群：816425449
***
#### 快速开始
> #### 安装
```
PM> Install-Package Riz.XFramework
```
| Database   | Package                |
| --------   | -----                  |
| SqlServer  | Riz.XFramework         |
| MySql      | Riz.XFramework.MySql   |
| Oracle     | Riz.XFramework.Oracle  |
| Postgre    | Riz.XFramework.Postgre |
| SQLite     | Riz.XFramework.SQLite  |
> #### 定义实体
```C#
[Table(Name = "Bas_Client")]
public partial class Client
{
    [Column(IsKey = true)]
    public virtual int ClientId { get; set; }
    public virtual string ClientCode { get; set; }
    public virtual string ClientName { get; set; }
    public virtual int CloudServerId { get; set; }
    [ForeignKey("CloudServerId")]
    public IList<CloudServer> CloudServer { get; set; }
}

[Table(Name = "Sys_CloudServer")]
public partial class CloudServer
{
    [Column(IsKey = true)]
    public virtual int CloudServerId { get; set; }
    public virtual string CloudServerCode { get; set; }
    public virtual string CloudServerName { get; set; }
}
```
> #### 基本用法
```C#
// 声明数据库链接字符串
const string connString = "Server=.;Database=***;uid=**;pwd=**;pooling=true;connect timeout=10;";
// 实例化数据上下文
// SqlServer
var context = new SqlServerDbContext(connString);
var data = context.GetTable<Client>().Where(a => a.ClientId <= 10).ToList();
data = context
    .GetTable<Client>()
    .Include(a => a.CloudServer, a => new
    {
        CloudServerCode = a.CloudServer.CloudServerCode
    })
    .ToList();
```
#### :pushpin: 更详细教程 [Wiki](https://github.com/TANZAME/Riz.XFramework/wiki)
> #### 支持作者
:thumbsup:

