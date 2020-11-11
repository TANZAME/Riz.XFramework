using System;
using System.Xml;
using System.Text;
using System.Diagnostics;
using System.Data.SqlTypes;
using System.Collections.Generic;

using Riz.XFramework.Data;
using Riz.XFramework.Data.SqlClient;

namespace Riz.XFramework.UnitTest.Postgre
{
    public class RizPostgreTest : RizTestBase<RizPostgreModel.RizPostgreDemo>
    {
        const string connString = "Host=localhost;Database=TZM_XFramework;uid=postgres;pwd=123456;pooling=true;minpoolsize=1;maxpoolsize=1;";

        public override IDbContext CreateDbContext()
        {
            // 直接用无参构造函数时会使用默认配置项 XFrameworkConnString
            // new NpgDbContext();
            var context = new NpgDbContext(connString)
            {
                IsDebug = base.IsDebug,
                CaseSensitive = base.CaseSensitive
            };
            return context;
        }

        protected override void Parameterized()
        {
            var context = _newContext();
            // 构造函数
            var query =
                 from a in context.GetTable<RizPostgreModel.RizPostgreDemo>()
                 where a.RizDemoId <= 10
                 select new RizPostgreModel.RizPostgreDemo(a);
            var r1 = query.ToList();
            query =
               from a in context.GetTable<RizPostgreModel.RizPostgreDemo>()
               where a.RizDemoId <= 10
               select new RizPostgreModel.RizPostgreDemo(a.RizDemoId, a.RizDemoName);
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
            string fileName = new System.IO.DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.Parent.FullName + @"\长文本.txt";
            string text = System.IO.File.ReadAllText(fileName, Encoding.GetEncoding("GB2312"));
            //            string fileName = new System.IO.DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.Parent.Parent.FullName + @"\net45\Riz.XFramework.UnitTest\长文本.txt";
            //#if netcore

            //            fileName = new System.IO.DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.Parent.Parent.Parent.FullName + @"\net45\Riz.XFramework.UnitTest\长文本.txt";
            //            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            //#endif
            //            string text = System.IO.File.ReadAllText(fileName, Encoding.GetEncoding("GB2312"));

            // 批量增加
            // 产生 INSERT INTO VALUES(),(),()... 语法。注意这种批量增加的方法并不能给自增列自动赋值
            var demos = new List<RizPostgreModel.RizPostgreDemo>();
            for (int i = 0; i < 5; i++)
            {
                RizPostgreModel.RizPostgreDemo d = new RizPostgreModel.RizPostgreDemo
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
                    RizDemoDatetimeOffset_Nullable = sDateOffset,
                    DemoTimestamp_Nullable = DateTime.Now,
                    DemoText_Nullable = "TEXT 类型",
                    DemoNText_Nullable = "NTEXT 类型",
                    DemoBinary_Nullable = i % 2 == 0 ? Encoding.UTF8.GetBytes("表示时区偏移量（分钟）（如果为整数）的表达式") : null,
                    DemoVarBinary_Nullable = i % 2 == 0 ? Encoding.UTF8.GetBytes(text) : new byte[0],
                };
                demos.Add(d);
            }
            context.Insert<RizPostgreModel.RizPostgreDemo>(demos);
            context.SubmitChanges();
            var myList = context
                .GetTable<RizPostgreModel.RizPostgreDemo>()
                .OrderByDescending(x => x.RizDemoId)
                .Take(5).ToList();
            Debug.Assert(myList[0].DemVarBinary_s == text);

            // byte[]
            var demo = new RizPostgreModel.RizPostgreDemo
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
                RizDemoTime_Nullable = new TimeSpan(0, 10, 10, 10) + TimeSpan.FromTicks(456789 * 10),
                RizDemoDatetimeOffset_Nullable = sDateOffset,//DateTimeOffset.Now,
                DemoTimestamp_Nullable = DateTime.Now,
                DemoText_Nullable = "TEXT 类型",
                DemoNText_Nullable = "NTEXT 类型",
                DemoBinary_Nullable = Encoding.UTF8.GetBytes("表示时区偏移量（分钟）（如果为整数）的表达式"),
                DemoVarBinary_Nullable = Encoding.UTF8.GetBytes(text),
            };
            context.Insert(demo);
            context.SubmitChanges();

            demo = context.GetTable<RizPostgreModel.RizPostgreDemo>().FirstOrDefault(x => x.RizDemoId == demo.RizDemoId);
            Debug.Assert(demo.DemVarBinary_s == text);
            var hex = context
                .GetTable<RizPostgreModel.RizPostgreDemo>()
                .Where(x => x.RizDemoId == demo.RizDemoId)
                .Select(x => x.DemoVarBinary_Nullable.ToString())
                .FirstOrDefault();
        }
    }
}
