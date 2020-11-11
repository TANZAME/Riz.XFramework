
using System;
using System.Linq;
using System.Collections.Generic;

using Riz.XFramework.Data;
using Riz.XFramework.Data.SqlClient;
using System.Text;

namespace Riz.XFramework.UnitTest.SQLite
{
    public class RizSQLiteTest : RizTestBase<RizSQLiteModel.RizSQLiteDemo>
    {
        // SQLite 需要将包里的 SQLite.Interop.dll 文件拷到运行目录下

#if net40

        static string connString =
            "DataSource=" +
            new System.IO.DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.Parent.Parent.FullName +
            @"\net45\Riz.XFramework.UnitTest\SQLite\TZM_XFramework.db;Version=3;Pooling=False;Max Pool Size=100;";

#endif


#if net45

        static string connString =
            "DataSource=" +
            new System.IO.DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.Parent.Parent.FullName +
            @"\net45\Riz.XFramework.UnitTest\SQLite\TZM_XFramework.db;Version=3;Pooling=False;Max Pool Size=100;";

#endif

#if netcore

        static string connString =
            "DataSource=" +
            new System.IO.DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.Parent.Parent.Parent.FullName +
            @"\net45\Riz.XFramework.UnitTest\SQLite\TZM_XFramework.db;Version=3;Pooling=False;Max Pool Size=100;";

#endif

        public RizSQLiteTest()
            : base()
        {
            // 初始化数据~~
        }

        public override IDbContext CreateDbContext()
        {
            // 直接用无参构造函数时会使用默认配置项 XFrameworkConnString
            var context = new SQLiteDbContext(connString)
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
                 from a in context.GetTable<RizSQLiteModel.RizSQLiteDemo>()
                 where a.RizDemoId <= 10
                 select new RizSQLiteModel.RizSQLiteDemo(a);
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
               from a in context.GetTable<RizSQLiteModel.RizSQLiteDemo>()
               where a.RizDemoId <= 10
               select new RizSQLiteModel.RizSQLiteDemo(a.RizDemoId, a.RizDemoName);
            r1 = query.ToList();
            //SQL=>
            //SELECT 
            //t0.[DemoId] AS [DemoId],
            //t0.[DemoName] AS [DemoName]
            //FROM [Sys_Demo] t0 
            //WHERE t0.[DemoId] <= 10

        }

        protected override void DbFunction()
        {
            // sqlite 暂时不支持各种函数
            //base.DbFunc();
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

            //#endif
            //            string text = System.IO.File.ReadAllText(fileName, Encoding.GetEncoding("GB2312"));

            // 批量增加
            // 产生 INSERT INTO VALUES(),(),()... 语法。注意这种批量增加的方法并不能给自增列自动赋值
            var demos = new List<RizSQLiteModel.RizSQLiteDemo>();
            for (int i = 0; i < 5; i++)
            {
                RizSQLiteModel.RizSQLiteDemo d = new RizSQLiteModel.RizSQLiteDemo
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
                    DemVarBinary_Nullable = i % 2 == 0 ? Encoding.UTF8.GetBytes(text) : new byte[0],
                };
                demos.Add(d);
            }
            context.Insert<RizSQLiteModel.RizSQLiteDemo>(demos);
            context.SubmitChanges();
            var myList = context
                .GetTable<RizSQLiteModel.RizSQLiteDemo>()
                .OrderByDescending(x => x.RizDemoId)
                .Take(5).ToList();

            // byte[]
            var demo = new RizSQLiteModel.RizSQLiteDemo
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
                DemVarBinary_Nullable = Encoding.UTF8.GetBytes(text),
            };
            context.Insert(demo);
            context.SubmitChanges();

            demo = context.GetTable<RizSQLiteModel.RizSQLiteDemo>().FirstOrDefault(x => x.RizDemoId == demo.RizDemoId);
        }
    }
}
