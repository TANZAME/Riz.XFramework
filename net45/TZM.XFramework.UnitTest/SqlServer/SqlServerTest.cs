
using System;
using System.Text;
using System.Data.SqlClient;
using System.Collections.Generic;
using TZM.XFramework.Data;
using TZM.XFramework.Data.SqlClient;

namespace TZM.XFramework.UnitTest.SqlServer
{
    public class SqlServerTest : TestBase<SqlServerModel.SqlServerDemo>
    {
        const string connString = "Server=.;Database=Inte_XFramework;uid=sa;pwd=123456;pooling=true;max pool size=1;min pool size=1;connect timeout=10;";

        public override IDbContext CreateDbContext()
        {
            // 直接用无参构造函数时会使用默认配置项 XFrameworkConnString
            // new SqlDbContext();
            return new SqlDbContext(connString);
        }

        public override void Run(DatabaseType dbType)
        {
            var context = _newContext();

            var demo = context.GetTable<SqlServerModel.SqlServerDemo>().FirstOrDefault(x => x.DemoId == 152243);

            // 声明表变量
            var typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo<SqlServerModel.JoinKey>();
            context.AddQuery(string.Format("DECLARE {0} [{1}]", typeRuntime.TableName, typeRuntime.TableName.TrimStart('@')));
            List<SqlServerModel.JoinKey> keys = new List<SqlServerModel.JoinKey>
            {
                new SqlServerModel.JoinKey{ Key1 = 2 },
                new SqlServerModel.JoinKey{ Key1 = 3 },
            };
            // 向表变量写入数据
            context.Insert<SqlServerModel.JoinKey>(keys);
            // 像物理表一样操作表变量
            var query =
                from a in context.GetTable<Model.Client>()
                join b in context.GetTable<SqlServerModel.JoinKey>() on a.ClientId equals b.Key1
                select a;
            context.AddQuery(query);
            // 提交查询结果
            List<Model.Client> result = null;
            context.SubmitChanges(out result);


            base.Run(dbType);
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

        protected override void API()
        {
            base.API();

            var context = _newContext();
            DateTime sDate = new DateTime(2007, 6, 10, 0, 0, 0);
            DateTimeOffset sDateOffset = new DateTimeOffset(sDate, new TimeSpan(-7, 0, 0));
            //Model.CloudServer server = new Model.CloudServer 
            //{
            //    CloudServerId = 1,
            //    CloudServerCode = "Server01",
            //    CloudServerName = "1号服务器'--"
            //};
            //string xml = SerializeHelper.SerializeToXml(server);
            //SqlXml newXml = new SqlXml(new XmlTextReader(xml, XmlNodeType.Document, null));

            // 批量增加
            // 产生 INSERT INTO VALUES(),(),()... 语法。注意这种批量增加的方法并不能给自增列自动赋值
            context.Delete<SqlServerModel.SqlServerDemo>(x => x.DemoId > 1000000);
            var demos = new List<SqlServerModel.SqlServerDemo>();
            for (int i = 0; i < 5; i++)
            {
                SqlServerModel.SqlServerDemo d = new SqlServerModel.SqlServerDemo
                {
                    DemoCode = "D0000001",
                    DemoName = "N0000001",
                    DemoBoolean = true,
                    DemoChar = 'A',
                    DemoNChar = 'B',
                    DemoByte =  127,
                    DemoDate = DateTime.Now,
                    DemoDateTime = DateTime.Now,
                    DemoDateTime2 = DateTime.Now,
                    DemoDecimal = 64,
                    DemoDouble = 64,
                    DemoFloat = 64,
                    DemoGuid = Guid.NewGuid(),
                    DemoShort = 64,
                    DemoInt = 64,
                    DemoLong = 64,
                    DemoTime_Nullable = new TimeSpan(0, 10, 10, 10) + TimeSpan.FromTicks(456789 * 10),
                    DemoDatetimeOffset_Nullable = sDateOffset,
                    DemoText_Nullable = "TEXT 类型",
                    DemoNText_Nullable = "NTEXT 类型",
                    //DemoXml_Nullable = newXml
                };
                demos.Add(d);
            }
            context.Insert<SqlServerModel.SqlServerDemo>(demos);
            context.SubmitChanges();

            // byte[]
            var demo = new SqlServerModel.SqlServerDemo
            {
                DemoCode = "D0000001",
                DemoName = "N0000001",
                DemoBoolean = true,
                DemoChar = 'A',
                DemoNChar = 'B',
                DemoByte =  128,
                DemoDate = DateTime.Now,
                DemoDateTime = DateTime.Now,
                DemoDateTime2 = DateTime.Now,
                DemoDecimal = 64,
                DemoDouble = 64,
                DemoFloat = 64,
                DemoGuid = Guid.NewGuid(),
                DemoShort = 64,
                DemoInt = 64,
                DemoLong = 64,
                DemoTime_Nullable = new TimeSpan(0, 10, 10, 10) + TimeSpan.FromTicks(456789 * 10),
                DemoDatetimeOffset_Nullable = DateTimeOffset.Now,
                DemoText_Nullable = "TEXT 类型",
                DemoNText_Nullable = "NTEXT 类型",
                DemoBinary_Nullable = Encoding.UTF8.GetBytes("表示时区偏移量（分钟）（如果为整数）的表达式"),
                DemVarBinary_Nullable = Encoding.UTF8.GetBytes("表示时区偏移量（分钟）（如果为整数）的表达式"),
                //DemoXml_Nullable = newXml
            };
            context.Insert(demo);
            context.SubmitChanges();

            demo = context.GetTable<SqlServerModel.SqlServerDemo>().FirstOrDefault(x => x.DemoId == demo.DemoId);

            context.Delete<Model.Client>(x => x.ClientId >= 2000);
            context.SubmitChanges();
            var query =
                from a in context.GetTable<Model.Client>()
                where a.ClientId <= 10
                select a;
            var table = query.ToDataTable<Model.Client>();

            table.TableName = "Bas_Client";
            table.Rows.Clear();
            int maxId = context.GetTable<Model.Client>().Max(x => x.ClientId);
            for (int i = 1; i <= 10; i++)
            {
                var row = table.NewRow();
                row["ClientId"] = maxId + i;
                row["ClientCode"] = "C" + i;
                row["ClientName"] = "N" + i;
                row["CloudServerId"] = 0;
                row["ActiveDate"] = DateTime.Now;
                row["Qty"] = 0;
                row["State"] = 1;
                row["Remark"] = string.Empty;
                table.Rows.Add(row);
            }
            table.AcceptChanges();

            DateTime sDate2 = DateTime.Now;
            ((SqlDbContext)context).BulkCopy(table);
            var ms = (DateTime.Now - sDate2).TotalMilliseconds;
            // 10w   300ms
            // 100w  4600ms
        }
    }
}
