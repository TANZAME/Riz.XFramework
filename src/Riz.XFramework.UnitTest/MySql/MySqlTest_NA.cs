using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;

using Riz.XFramework;
using Riz.XFramework.Data;
using Riz.XFramework.Data.SqlClient;

namespace Riz.XFramework.UnitTest.MySql
{
    public class MySqlTest_NA : TestBase_NA<MySqlModel_NA.Demo>
    {
        const string connString = "Host=localhost;Database=Riz_XFramework;uid=root;pwd=123456;Pooling=true;Min Pool Size=1;Max Pool Size=1;Connection Lifetime=;";

        public override IDbContext CreateDbContext()
        {
            // 直接用无参构造函数时会使用默认配置项 XFrameworkConnString
            // new MySqlDbContext();
            var context = new MySqlDbContext(connString)
            {
                IsDebug = base.IsDebug
            };
            return context;
        }

        protected override void Parameterized()
        {
            var context = _newContext();

            // 构造函数
            var query =
                 from a in context.GetTable<MySqlModel_NA.Demo>()
                 where a.RizDemoId <= 10
                 select new MySqlModel_NA.Demo(a);
            var r1 = query.ToList();
            query =
               from a in context.GetTable<MySqlModel_NA.Demo>()
               where a.RizDemoId <= 10
               select new MySqlModel_NA.Demo(a.RizDemoId, a.RizDemoName);
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

            // 批量增加
            // 产生 INSERT INTO VALUES(),(),()... 语法。注意这种批量增加的方法并不能给自增列自动赋值
            var demos = new List<MySqlModel_NA.Demo>();
            for (int i = 0; i < 5; i++)
            {
                MySqlModel_NA.Demo d = new MySqlModel_NA.Demo
                {
                    RizDemoCode = "D0000001",
                    RizDemoName = "N0000001",
                    RizDemoBoolean = true,
                    RizDemoChar = 'A',
                    RizDemoNChar = 'B',
                    RizDemoByte = 64,
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
                    RizDemoTime_Nullable = i % 2 == 0 ? new TimeSpan(-34, -22, -59, -59) : new TimeSpan(34, 22, 59, 59),
                    RizDemoDatetimeOffset_Nullable = DateTime.Now,
                    DemoTimestamp_Nullable = DateTime.Now,
                    DemoText_Nullable = "TEXT 类型",
                    DemoNText_Nullable = "NTEXT 类型",
                    DemoBinary_Nullable = i % 2 == 0 ? Encoding.UTF8.GetBytes("表示时区偏移量（分钟）（如果为整数）的表达式") : null,
                    DemoVarBinary_Nullable = i % 2 == 0 ? Encoding.UTF8.GetBytes(LongText.LONGTEXT) : new byte[0],
                };
                demos.Add(d);
            }
            context.Insert<MySqlModel_NA.Demo>(demos);
            context.SubmitChanges();
            var myList = context
                .GetTable<MySqlModel_NA.Demo>()
                .OrderByDescending(x => x.RizDemoId)
                .Take(5).ToList();
            Debug.Assert(myList[0].DemVarBinary_s == LongText.LONGTEXT);

            // byte[]
            var demo = new MySqlModel_NA.Demo
            {
                RizDemoCode = "D0000001",
                RizDemoName = "N0000001",
                RizDemoBoolean = true,
                RizDemoChar = 'A',
                RizDemoNChar = 'B',
                RizDemoByte = 64,
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
                RizDemoDatetimeOffset_Nullable = DateTime.Now,
                DemoTimestamp_Nullable = DateTime.Now,
                DemoText_Nullable = "TEXT 类型",
                DemoNText_Nullable = "NTEXT 类型",
                DemoBinary_Nullable = Encoding.UTF8.GetBytes("表示时区偏移量（分钟）（如果为整数）的表达式"),
                DemoVarBinary_Nullable = Encoding.UTF8.GetBytes(LongText.LONGTEXT),
            };
            context.Insert(demo);
            context.SubmitChanges();

            demo = context.GetTable<MySqlModel_NA.Demo>().FirstOrDefault(x => x.RizDemoId == demo.RizDemoId);
            Debug.Assert(demo.DemVarBinary_s == LongText.LONGTEXT);
            var hex = context
                .GetTable<MySqlModel_NA.Demo>()
                .Where(x => x.RizDemoId == demo.RizDemoId)
                .Select(x => x.DemoVarBinary_Nullable.ToString())
                .FirstOrDefault();
        }
    }
}
