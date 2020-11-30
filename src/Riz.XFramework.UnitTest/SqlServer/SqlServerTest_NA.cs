
using System;
using System.Text;
using System.Diagnostics;
using System.Data.SqlClient;
using System.Collections.Generic;
using Riz.XFramework.Data;
using Riz.XFramework.Data.SqlClient;
using System.Linq;
using System.Data;

namespace Riz.XFramework.UnitTest.SqlServer
{
    public class SqlServerTest_NA : TestBase_NA<SqlServerModel_NA.Demo>
    {
        const string connString = "Server=.;Database=Riz_XFramework;uid=sa;pwd=123456;pooling=true;max pool size=1;min pool size=1;connect timeout=10;";

        public override IDbContext CreateDbContext()
        {
            // 直接用无参构造函数时会使用默认配置项 XFrameworkConnString
            // new SqlDbContext();
            var context = new SqlServerDbContext(connString)
            {
                IsDebug = base.IsDebug,
                NoLock = false,
                IsolationLevel = System.Data.IsolationLevel.ReadCommitted
            };
            return context;
        }

        public override void Run(DatabaseType dbType)
        {
            var context = _newContext();

            // 声明表变量
            var typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo<SqlServerModel_NA.JoinKey>();
            context.AddQuery(string.Format("DECLARE {0} [{1}]", typeRuntime.TableName, typeRuntime.TableName.TrimStart('@')));
            List<SqlServerModel_NA.JoinKey> keys = new List<SqlServerModel_NA.JoinKey>
            {
                new SqlServerModel_NA.JoinKey{ Key1 = 2 },
                new SqlServerModel_NA.JoinKey{ Key1 = 3 },
            };
            // 向表变量写入数据
            context.Insert<SqlServerModel_NA.JoinKey>(keys);
            // 像物理表一样操作表变量
            var query =
                from a in context.GetTable<Model_NA.Client>()
                join b in context.GetTable<SqlServerModel_NA.JoinKey>() on a.RizClientId equals b.Key1
                select a;
            context.AddQuery(query);
            // 提交查询结果
            List<Model_NA.Client> result = null;
            context.SubmitChanges(out result);


            base.Run(dbType);
        }

        protected override void Parameterized()
        {
            var context = _newContext();
            // 构造函数
            var query =
                 from a in context.GetTable<Model_NA.Demo>()
                 where a.RizDemoId <= 10
                 select new Model_NA.Demo(a);
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
               from a in context.GetTable<Model_NA.Demo>()
               where a.RizDemoId <= 10
               select new Model_NA.Demo(a.RizDemoId, a.RizDemoName);
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

            // 单个插入
            var demo = new SqlServerModel_NA.Demo
            {
                RizDemoCode = "D0000001",
                RizDemoName = "N0000001",
                RizDemoBoolean = true,
                RizDemoChar = 'A',
                RizDemoNChar = 'B',
                RizDemoByte = 128,
                RizDemoDate = DateTime.Now,
                RizDemoDateTime = DateTime.Now,
                RizDemoDateTime2 = DateTime.Now,
                RizDemoDecimal = 64,
                RizDemoDouble = 64,
                RizDemoFloat = 64,
                RizDemoGuid = Guid.NewGuid(),
                RizDemoShort = 64,
                RizDemoInt = 64,
                RizDemoLong = 64,
                RizDemoTime_Nullable = new TimeSpan(0, 10, 10, 10) + TimeSpan.FromTicks(456789 * 10),
                RizDemoDatetimeOffset_Nullable = DateTimeOffset.Now,
                DemoText_Nullable = "TEXT 类型",
                DemoNText_Nullable = "NTEXT 类型",
                DemoBinary_Nullable = Encoding.UTF8.GetBytes("表示时区偏移量（分钟）（如果为整数）的表达式"),
                DemoVarBinary_Nullable = Encoding.UTF8.GetBytes(LongText.LONGTEXT),
            };
            context.Insert(demo);
            context.SubmitChanges();

            demo = context.GetTable<SqlServerModel_NA.Demo>().FirstOrDefault(x => x.RizDemoId == demo.RizDemoId);
            Debug.Assert(demo.DemVarBinary_s == LongText.LONGTEXT);
            var hex = context
                .GetTable<SqlServerModel_NA.Demo>()
                .Where(x => x.RizDemoId == demo.RizDemoId)
                .Select(x => x.DemoVarBinary_Nullable.ToString())
                .FirstOrDefault();

            // 批量增加
            // 产生 INSERT INTO VALUES(),(),()... 语法。注意这种批量增加的方法并不能给自增列自动赋值
            var models = new List<SqlServerModel_NA.Demo>();
            for (int i = 0; i < 5; i++)
            {
                SqlServerModel_NA.Demo d = new SqlServerModel_NA.Demo
                {
                    RizDemoCode = string.Format("D000000{0}", i + 1),
                    RizDemoName = string.Format("N000000{0}", i + 1),
                    RizDemoBoolean = true,
                    RizDemoChar = 'A',
                    RizDemoNChar = 'B',
                    RizDemoByte = 127,
                    RizDemoDate = DateTime.Now,
                    RizDemoDateTime = DateTime.Now,
                    RizDemoDateTime2 = DateTime.Now,
                    RizDemoDecimal = 64,
                    RizDemoDouble = 64,
                    RizDemoFloat = 64,
                    RizDemoGuid = Guid.NewGuid(),
                    RizDemoShort = 64,
                    RizDemoInt = 64,
                    RizDemoLong = 64,
                    RizDemoTime_Nullable = new TimeSpan(0, 10, 10, 10) + TimeSpan.FromTicks(456789 * 10),
                    RizDemoDatetimeOffset_Nullable = sDateOffset,
                    DemoText_Nullable = "TEXT 类型",
                    DemoNText_Nullable = "NTEXT 类型",
                    DemoBinary_Nullable = i % 2 == 0 ? Encoding.UTF8.GetBytes("表示时区偏移量（分钟）（如果为整数）的表达式") : null,
                    DemoVarBinary_Nullable = i % 2 == 0 ? Encoding.UTF8.GetBytes(LongText.LONGTEXT) : new byte[0],
                };
                models.Add(d);
            }
            // 批量插入
            context.Insert<SqlServerModel_NA.Demo>(models);
            // 写入数据的同时再查出数据
            var query1 = context
                .GetTable<SqlServerModel_NA.Demo>()
                .Where(a => a.RizDemoId > 2)
                .OrderBy(a => a.RizDemoId)
                .Take(20);
            context.AddQuery(query1);
            // 单个插入
            var demo1 = new SqlServerModel_NA.Demo
            {
                RizDemoCode = "D0000006",
                RizDemoName = "N0000006",
                RizDemoBoolean = true,
                RizDemoChar = 'A',
                RizDemoNChar = 'B',
                RizDemoByte = 128,
                RizDemoDate = DateTime.Now,
                RizDemoDateTime = DateTime.Now,
                RizDemoDateTime2 = DateTime.Now,
                RizDemoDecimal = 64,
                RizDemoDouble = 64,
                RizDemoFloat = 64,
                RizDemoGuid = Guid.NewGuid(),
                RizDemoShort = 64,
                RizDemoInt = 64,
                RizDemoLong = 64,
                RizDemoTime_Nullable = new TimeSpan(0, 10, 10, 10) + TimeSpan.FromTicks(456789 * 10),
                RizDemoDatetimeOffset_Nullable = DateTimeOffset.Now,
                DemoText_Nullable = "TEXT 类型",
                DemoNText_Nullable = "NTEXT 类型",
                DemoBinary_Nullable = Encoding.UTF8.GetBytes("表示时区偏移量（分钟）（如果为整数）的表达式"),
                DemoVarBinary_Nullable = Encoding.UTF8.GetBytes(LongText.LONGTEXT),
            };
            context.Insert(demo1);
            context.Insert(demo1);
            // 提交修改并查出数据
            List<SqlServerModel_NA.Demo> result1 = null;
            context.SubmitChanges(out result1);

            // 断言
            var myList = context
                .GetTable<SqlServerModel_NA.Demo>()
                .OrderByDescending(a => a.RizDemoId)
                .Take(7)
                .OrderBy(a => a.RizDemoId)
                .ToList();
            Debug.Assert(myList[0].DemVarBinary_s == LongText.LONGTEXT);
            Debug.Assert(myList[0].RizDemoId == demo.RizDemoId + 1);
            Debug.Assert(myList[6].RizDemoId == demo.RizDemoId + 7);



            context.Delete<Model_NA.Client>(x => x.RizClientId >= 2000);
            context.SubmitChanges();
            var query =
                from a in context.GetTable<Model_NA.Client>()
                where a.RizClientId <= 10
                select a;

            int maxId = context.GetTable<Model_NA.Client>().Max(x => x.RizClientId);

            var table = query.ToDataTable<Model_NA.Client>();
            table.TableName = "Bas_Client";
            table.Rows.Clear();
            var maps = new List<SqlBulkCopyColumnMapping>();
            var typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo<Model_NA.Client>();
            foreach (DataColumn c in table.Columns)
            {
                var m = typeRuntime.GetMember(c.ColumnName);
                string fieldName = TypeUtils.GetFieldName(m.Member, typeRuntime.Type);
                maps.Add(new SqlBulkCopyColumnMapping(c.ColumnName, fieldName));
            }

            for (int i = 1; i <= 10; i++)
            {
                var row = table.NewRow();
                row["RizClientId"] = maxId + i;
                row["RizClientCode"] = "C" + i;
                row["RizClientName"] = "N" + i;
                row["RizCloudServerId"] = 0;
                row["RizActiveDate"] = DateTime.Now;
                row["RizQty"] = 0;
                row["RizState"] = 1;
                row["RizRemark"] = string.Empty;
                table.Rows.Add(row);
            }
            table.AcceptChanges();

            DateTime sDate2 = DateTime.Now;
            ((SqlServerDbContext)context).BulkCopy(table, maps);
            var ms = (DateTime.Now - sDate2).TotalMilliseconds;
            // 10w   300ms
            // 100w  4600ms
        }
    }
}
