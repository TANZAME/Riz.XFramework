using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;

using ICS.XFramework;
using ICS.XFramework.Data;

namespace ICS.XFramework.UnitTest.MySql
{
    public class MySqlTest : TestBase<MySqlModel.MySqlDemo>
    {
        const string connString = "Host=localhost;Database=Inte_XFramework;uid=root;pwd=123456;Pooling=true;Min Pool Size=1;Max Pool Size=1;Connection Lifetime=;";

        public override IDbContext CreateDbContext()
        {
            // 直接用无参构造函数时会使用默认配置项 XFrameworkConnString
            // new SqlDbContext();
            return new MyMySqlDbContext(connString);
        }

        protected override void QueryWithParameterizedConstructor()
        {
            var context = _newContext();
            // 构造函数
            var query =
                 from a in context.GetTable<MySqlModel.MySqlDemo>()
                 where a.DemoId <= 10
                 select new MySqlModel.MySqlDemo(a);
            var r1 = query.ToList();
            query =
               from a in context.GetTable<MySqlModel.MySqlDemo>()
               where a.DemoId <= 10
               select new MySqlModel.MySqlDemo(a.DemoId, a.DemoName);
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
            context.Delete<MySqlModel.MySqlDemo>(x => x.DemoId > 1000000);
            var demos = new List<MySqlModel.MySqlDemo>();
            for (int i = 0; i < 5; i++)
            {
                MySqlModel.MySqlDemo d = new MySqlModel.MySqlDemo
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
                    DemoTime_Nullable = new TimeSpan(10, 9, 9),
                    DemoDatetimeOffset_Nullable = DateTime.Now,
                    DemoTimestamp_Nullable = DateTime.Now,
                    DemoText_Nullable = "TEXT 类型",
                    DemoNText_Nullable = "NTEXT 类型",
                };
                demos.Add(d);
            }
            context.Insert<MySqlModel.MySqlDemo>(demos);
            context.SubmitChanges();

            // byte[]
            var demo = new MySqlModel.MySqlDemo
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
                DemoTime_Nullable = new TimeSpan(10, 9, 9),
                DemoDatetimeOffset_Nullable = DateTime.Now,
                DemoTimestamp_Nullable = DateTime.Now,
                DemoText_Nullable = "TEXT 类型",
                DemoNText_Nullable = "NTEXT 类型",
                DemoBinary_Nullable = Encoding.UTF8.GetBytes("表示时区偏移量（分钟）（如果为整数）的表达式"),
                DemVarBinary_Nullable = Encoding.UTF8.GetBytes("表示时区偏移量（分钟）（如果为整数）的表达式"),
            };
            context.Insert(demo);
            context.SubmitChanges();

            demo = context.GetTable<MySqlModel.MySqlDemo>().FirstOrDefault(x => x.DemoId == demo.DemoId);
        }
    }
}
