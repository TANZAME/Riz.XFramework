using System;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;

using TZM.XFramework;
using TZM.XFramework.Data;
using TZM.XFramework.Data.SqlClient;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;

namespace TZM.XFramework.UnitTest.Oracle
{
    public class OracleTest : TestBase<OracleModel.OracleDemo>
    {
        // 如果在尝试进行 COM 上下文转换期间检测到一个死锁，将激活 contextSwitchDeadlock 托管调试助手 (MDA)。
        // https://docs.microsoft.com/zh-cn/dotnet/framework/debug-trace-profile/contextswitchdeadlock-mda



        const string connString = "User Id=c##sa;Password=123456;Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST=localhost)(PORT=1521)))(CONNECT_DATA=(SERVICE_NAME=ORACLE)))";

        public override IDbContext CreateDbContext()
        {
            // 直接用无参构造函数时会使用默认配置项 XFrameworkConnString
            // new OracleDbContext();
            var context = new OracleDbContext(connString)
            {
                IsDebug = base.IsDebug,
                CaseSensitive = base.CaseSensitive
            };

            return context;
        }

        protected override void Parameterized()
        {
            var context = _newContext();


            //var result2 = context.GetTable<Oracle.OracleModel.vwOracleDemo>().ToList();

            //DateTime sDate = new DateTime(2007, 6, 10, 0, 0, 0);
            //DateTimeOffset sDateOffset = new DateTimeOffset(sDate, new TimeSpan(-7, 0, 0));
            //string text = "大概就600个字符串";
            //var demos = new List<OracleModel.vwOracleDemo>();
            //for (int i = 0; i < 5; i++)
            //{
            //    OracleModel.vwOracleDemo d = new OracleModel.vwOracleDemo
            //    {
            //        DemoCode = "viewDemo",
            //        DemoName = "viewDemo",
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
            //        DemoTime_Nullable = new TimeSpan(-9, 10, 10, 10) + TimeSpan.FromTicks(456789 * 10),
            //        DemoDatetimeOffset_Nullable = sDateOffset,
            //        DemoTimestamp_Nullable = DateTime.Now,
            //        DemoText_Nullable = "TEXT 类型",
            //        DemoNText_Nullable = "NTEXT 类型",
            //        DemoBinary_Nullable = i % 2 == 0 ? Encoding.UTF8.GetBytes("表示时区偏移量（分钟）（如果为整数）的表达式") : null,
            //        DemoVarBinary_Nullable = i % 2 == 0 ? Encoding.UTF8.GetBytes(text) : new byte[0],
            //    };
            //    demos.Add(d);
            //}
            //context.Insert<OracleModel.vwOracleDemo>(demos);
            //context.SubmitChanges();

            // 构造函数
            var query =
                 from a in context.GetTable<OracleModel.OracleDemo>()
                 where a.DemoId <= 10
                 select new OracleModel.OracleDemo(a);
            var r1 = query.ToList();
            query =
               from a in context.GetTable<OracleModel.OracleDemo>()
               where a.DemoId <= 10
               select new OracleModel.OracleDemo(a.DemoId, a.DemoName);
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
            string fileName = new System.IO.DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.Parent.Parent.FullName + @"\net45\TZM.XFramework.UnitTest\长文本.txt";
#if netcore

            fileName = new System.IO.DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.Parent.Parent.Parent.FullName + @"\net45\TZM.XFramework.UnitTest\长文本.txt";

#endif
            string text = System.IO.File.ReadAllText(fileName, Encoding.GetEncoding("GB2312")).Substring(0, 600); // 大概就600个字符串;

            // 批量增加
            // 产生 INSERT INTO VALUES(),(),()... 语法。注意这种批量增加的方法并不能给自增列自动赋值
            var demos = new List<OracleModel.OracleDemo>();
            for (int i = 0; i < 5; i++)
            {
                OracleModel.OracleDemo d = new OracleModel.OracleDemo
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
                    DemoTime_Nullable = new TimeSpan(-9, 10, 10, 10) + TimeSpan.FromTicks(456789 * 10),
                    DemoDatetimeOffset_Nullable = sDateOffset,
                    DemoTimestamp_Nullable = DateTime.Now,
                    DemoText_Nullable = "TEXT 类型",
                    DemoNText_Nullable = "NTEXT 类型",
                    DemoBinary_Nullable = i % 2 == 0 ? Encoding.UTF8.GetBytes("表示时区偏移量（分钟）（如果为整数）的表达式") : null,
                    DemoVarBinary_Nullable = i % 2 == 0 ? Encoding.UTF8.GetBytes(text) : new byte[0],
                };
                demos.Add(d);
            }
            context.Insert<OracleModel.OracleDemo>(demos);
            context.SubmitChanges();
            var myList = context
                .GetTable<OracleModel.OracleDemo>()
                .OrderByDescending(x => x.DemoId)
                .Take(5).ToList();
            Debug.Assert(myList[0].DemVarBinary_s == text);

            // byte[]
            var demo = new OracleModel.OracleDemo
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
                DemoTime_Nullable = new TimeSpan(59, 10, 10, 10) + TimeSpan.FromTicks(456789 * 10),
                DemoDatetimeOffset_Nullable = DateTimeOffset.Now,
                DemoTimestamp_Nullable = DateTime.Now,
                DemoText_Nullable = "TEXT 类型",
                DemoNText_Nullable = "NTEXT 类型",
                DemoBinary_Nullable = Encoding.UTF8.GetBytes("表示时区偏移量（分钟）（如果为整数）的表达式"),
                DemoVarBinary_Nullable = Encoding.UTF8.GetBytes(text),
            };
            context.Insert(demo);
            context.SubmitChanges();

            demo = context.GetTable<OracleModel.OracleDemo>().FirstOrDefault(x => x.DemoId == demo.DemoId);
            Debug.Assert(demo.DemVarBinary_s == text);
            var hex = context
                .GetTable<OracleModel.OracleDemo>()
                .Where(x => x.DemoId == demo.DemoId)
                .Select(x => x.DemoVarBinary_Nullable.ToString())
                .FirstOrDefault();
        }
    }
}
