using System;
using System.Xml;
using System.Text;
using System.Data.SqlTypes;
using System.Collections.Generic;

using TZM.XFramework.Data;
using TZM.XFramework.Data.SqlClient;

namespace TZM.XFramework.UnitTest.Postgre
{
    public class PostgreTest : TestBase<PostgreModel.PostgreDemo>
    {
        const string connString = "Host=localhost;Database=Inte_XFramework;uid=postgres;pwd=123456;pooling=true;minpoolsize=1;maxpoolsize=1;";

        public override IDbContext CreateDbContext()
        {
            // 直接用无参构造函数时会使用默认配置项 XFrameworkConnString
            // new SqlDbContext();
            return new NpgDbContext(connString);
        }

        protected override void QueryWithParameterizedConstructor()
        {
            var context = _newContext();
            // 构造函数
            var query =
                 from a in context.GetTable<PostgreModel.PostgreDemo>()
                 where a.DemoId <= 10
                 select new PostgreModel.PostgreDemo(a);
            var r1 = query.ToList();
            query =
               from a in context.GetTable<PostgreModel.PostgreDemo>()
               where a.DemoId <= 10
               select new PostgreModel.PostgreDemo(a.DemoId, a.DemoName);
            r1 = query.ToList();
            //SQL=> 
            //SELECT 
            //t0.[DemoId] AS [DemoId],
            //t0.[DemoName] AS [DemoName]
            //FROM [Sys_Demo] t0 
        }

        protected override void API()
        {
            base.API();

            var context = _newContext();
            DateTime sDate = new DateTime(2007, 6, 10, 0, 0, 0);
            DateTimeOffset sDateOffset = new DateTimeOffset(sDate, new TimeSpan(-7, 0, 0));

            // 批量增加
            // 产生 INSERT INTO VALUES(),(),()... 语法。注意这种批量增加的方法并不能给自增列自动赋值
            context.Delete<PostgreModel.PostgreDemo>(x => x.DemoId > 1000000);
            var demos = new List<PostgreModel.PostgreDemo>();
            for (int i = 0; i < 5; i++)
            {
                PostgreModel.PostgreDemo d = new PostgreModel.PostgreDemo
                {
                    DemoCode = "D0000001",
                    DemoName = "N0000001",
                    DemoBoolean = true,
                    DemoChar = 'A',
                    DemoNChar = 'B',
                    DemoByte = 64,
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
#if netcore
                    DemoTime_Nullable = new TimeSpan(10, 9, 9),
#endif
#if !netcore
                    DemoTime_Nullable = DateTime.Now,
#endif
                    DemoDatetimeOffset_Nullable = sDateOffset,
                    DemoTimestamp_Nullable = DateTime.Now,
                    DemoText_Nullable = "TEXT 类型",
                    DemoNText_Nullable = "NTEXT 类型"
                };
                demos.Add(d);
            }
            context.Insert<PostgreModel.PostgreDemo>(demos);
            context.SubmitChanges();

            // byte[]
            var demo = new PostgreModel.PostgreDemo
            {
                DemoCode = "D0000001",
                DemoName = "N0000001",
                DemoBoolean = true,
                DemoChar = 'A',
                DemoNChar = 'B',
                DemoByte = 64,
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
#if netcore
                DemoTime_Nullable = new TimeSpan(10, 9, 9),
#endif
#if !netcore
                DemoTime_Nullable = DateTime.Now,
#endif
                DemoDatetimeOffset_Nullable = DateTimeOffset.Now,
                DemoTimestamp_Nullable = DateTime.Now,
                DemoText_Nullable = "TEXT 类型",
                DemoNText_Nullable = "NTEXT 类型",
                DemoBinary_Nullable = Encoding.UTF8.GetBytes("表示时区偏移量（分钟）（如果为整数）的表达式"),
                DemVarBinary_Nullable = Encoding.UTF8.GetBytes("表示时区偏移量（分钟）（如果为整数）的表达式")
            };
            context.Insert(demo);
            context.SubmitChanges();

            demo = context.GetTable<PostgreModel.PostgreDemo>().FirstOrDefault(x => x.DemoId == demo.DemoId);
        }
    }
}
