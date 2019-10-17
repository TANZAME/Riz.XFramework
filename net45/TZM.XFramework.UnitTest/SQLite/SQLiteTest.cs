
using System;
using System.Text;
using System.Collections.Generic;
using TZM.XFramework.Data;
using TZM.XFramework.Data.SqlClient;

namespace TZM.XFramework.UnitTest.SQLite
{
    public class SQLiteTest : TestBase<SQLiteModel.SQLiteDemo>
    {
        static string connString =
            "DataSource=" +
            new System.IO.DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.FullName +
            @"\SQLite\Inte_XFramework.db;Version=3;Pooling=False;Max Pool Size=100;";

        public SQLiteTest()
            : base()
        {
            // 初始化数据~~
        }

        public override IDbContext CreateDbContext()
        {
            // 直接用无参构造函数时会使用默认配置项 XFrameworkConnString
            var context = new SQLiteDbContext(connString);
            return context;
            // LAST_INSERT_ROWID()
            
        }

        protected override void QueryWithParameterizedConstructor()
        {
            var context = _newContext();
            // 构造函数
            var query =
                 from a in context.GetTable<Model.Demo>()
                 where a.DemoId <= 10
                 select new Model.Demo(a);
            var r1 = query.ToList();
            //SQL=> 
            //SELECT 
            //t0.[DemoId] AS [DemoId],
            //t0.[DemoCode] AS [DemoCode],
            //t0.[DemoName] AS [DemoName],
            //...
            //FROM [Sys_Demo] t0 
            //WHERE t0.[DemoId] <= 10
            query =
               from a in context.GetTable<Model.Demo>()
               where a.DemoId <= 10
               select new Model.Demo(a.DemoId, a.DemoName);
            r1 = query.ToList();
            //SQL=>
            //SELECT 
            //t0.[DemoId] AS [DemoId],
            //t0.[DemoName] AS [DemoName]
            //FROM [Sys_Demo] t0 
            //WHERE t0.[DemoId] <= 10

        }

        //protected override void API()
        //{
        //    base.API();

        //    var context = _newContext();
        //    DateTime sDate = new DateTime(2007, 6, 10, 0, 0, 0);
        //    DateTimeOffset sDateOffset = new DateTimeOffset(sDate, new TimeSpan(-7, 0, 0));
        //    //Model.CloudServer server = new Model.CloudServer 
        //    //{
        //    //    CloudServerId = 1,
        //    //    CloudServerCode = "Server01",
        //    //    CloudServerName = "1号服务器'--"
        //    //};
        //    //string xml = SerializeHelper.SerializeToXml(server);
        //    //SqlXml newXml = new SqlXml(new XmlTextReader(xml, XmlNodeType.Document, null));

        //    // 批量增加
        //    // 产生 INSERT INTO VALUES(),(),()... 语法。注意这种批量增加的方法并不能给自增列自动赋值
        //    context.Delete<SQLiteModel.SQLiteDemo>(x => x.DemoId > 1000000);
        //    var demos = new List<SQLiteModel.SQLiteDemo>();
        //    for (int i = 0; i < 5; i++)
        //    {
        //        SQLiteModel.SQLiteDemo d = new SQLiteModel.SQLiteDemo
        //        {
        //            DemoCode = "D0000001",
        //            DemoName = "N0000001",
        //            DemoBoolean = true,
        //            DemoChar = 'A',
        //            DemoNChar = 'B',
        //            DemoByte = 64,
        //            DemoDate = DateTime.Now,
        //            DemoDateTime = DateTime.Now,
        //            DemoDateTime2 = DateTime.Now,
        //            DemoDecimal = 64,
        //            DemoDouble = 64,
        //            DemoFloat = 64,
        //            DemoGuid = Guid.NewGuid(),
        //            DemoShort = 64,
        //            DemoInt = 64,
        //            DemoLong = 64,
        //            DemoTime_Nullable = new TimeSpan(10, 9, 9),
        //            DemoDatetimeOffset_Nullable = sDateOffset,
        //            DemoText_Nullable = "TEXT 类型",
        //            DemoNText_Nullable = "NTEXT 类型",
        //            //DemoXml_Nullable = newXml
        //        };
        //        demos.Add(d);
        //    }
        //    context.Insert<SQLiteModel.SQLiteDemo>(demos);
        //    context.SubmitChanges();

        //    // byte[]
        //    var demo = new SQLiteModel.SQLiteDemo
        //    {
        //        DemoCode = "D0000001",
        //        DemoName = "N0000001",
        //        DemoBoolean = true,
        //        DemoChar = 'A',
        //        DemoNChar = 'B',
        //        DemoByte = 64,
        //        DemoDate = DateTime.Now,
        //        DemoDateTime = DateTime.Now,
        //        DemoDateTime2 = DateTime.Now,
        //        DemoDecimal = 64,
        //        DemoDouble = 64,
        //        DemoFloat = 64,
        //        DemoGuid = Guid.NewGuid(),
        //        DemoShort = 64,
        //        DemoInt = 64,
        //        DemoLong = 64,
        //        DemoTime_Nullable = new TimeSpan(10, 9, 9),
        //        DemoDatetimeOffset_Nullable = DateTimeOffset.Now,
        //        DemoText_Nullable = "TEXT 类型",
        //        DemoNText_Nullable = "NTEXT 类型",
        //        DemoBinary_Nullable = Encoding.UTF8.GetBytes("表示时区偏移量（分钟）（如果为整数）的表达式"),
        //        DemVarBinary_Nullable = Encoding.UTF8.GetBytes("表示时区偏移量（分钟）（如果为整数）的表达式"),
        //        //DemoXml_Nullable = newXml
        //    };
        //    context.Insert(demo);
        //    context.SubmitChanges();

        //    demo = context.GetTable<SQLiteModel.SQLiteDemo>().FirstOrDefault(x => x.DemoId == demo.DemoId);

        //    context.Delete<Model.Client>(x => x.ClientId >= 2000);
        //    context.SubmitChanges();
        //    var query =
        //        from a in context.GetTable<Model.Client>()
        //        where a.ClientId <= 10
        //        select a;
        //    var table = query.ToDataTable<Model.Client>();

        //    table.TableName = "Bas_Client";
        //    table.Rows.Clear();
        //    int maxId = context.GetTable<Model.Client>().Max(x => x.ClientId);
        //    for (int i = 1; i <= 1000000; i++)
        //    {
        //        var row = table.NewRow();
        //        row["ClientId"] = maxId + i;
        //        row["ClientCode"] = "C" + i;
        //        row["ClientName"] = "N" + i;
        //        row["CloudServerId"] = 0;
        //        row["ActiveDate"] = DateTime.Now;
        //        row["Qty"] = 0;
        //        row["State"] = 1;
        //        row["Remark"] = string.Empty;
        //        table.Rows.Add(row);
        //    }
        //    table.AcceptChanges();

        //    DateTime sDate2 = DateTime.Now;
        //    ((SqlDbContext)context).BulkCopy(table);
        //    var ms = (DateTime.Now - sDate2).TotalMilliseconds;
        //    // 10w   300ms
        //    // 100w  4600ms
        //}
    }
}
