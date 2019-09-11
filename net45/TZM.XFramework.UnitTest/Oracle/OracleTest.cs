using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;

using TZM.XFramework;
using TZM.XFramework.Data;
using TZM.XFramework.Data.SqlClient;
using Oracle.ManagedDataAccess.Client;

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
            // new SqlDbContext();
            return new OracleDbContext(connString);
        }

        protected override void QueryWithParameterizedConstructor()
        {
            var context = _newContext();

            var reader = context.Database.ExecuteReader(@"
SELECT
    t0.DemoId AS DemoId,
    t0.DemoCode AS DemoCode,
    t0.DemoName AS DemoName,
    t0.DemoDate AS DemoDate,
    t0.DemoDate_Nullable AS DemoDate_Nullable,
    t0.DemoDateTime AS DemoDateTime,
    t0.DemoDateTime_Nullable AS DemoDateTime_Nullable,
    t0.DemoDateTime2 AS DemoDateTime2,
    t0.DemoDateTime2_Nullable AS DemoDateTime2_Nullable,
    t0.DemoTime_Nullable AS DemoTime_Nullable,
    t0.DemoText_Nullable AS DemoText_Nullable,
    t0.DemoNText_Nullable AS DemoNText_Nullable,
    t0.DemoDatetimeOffset_Nullable AS DemoDatetimeOffset_Nullable,
    t0.DemoTimestamp_Nullable AS DemoTimestamp_Nullable,
    t0.DemoBinary_Nullable AS DemoBinary_Nullable,
    t0.DemVarBinary_Nullable AS DemVarBinary_Nullable,
    t0.DemoBoolean AS DemoBoolean,
    t0.DemoBoolean_Nullable AS DemoBoolean_Nullable,
    t0.DemoChar AS DemoChar,
    t0.DemoNChar AS DemoNChar,
    t0.DemoChar_Nullable AS DemoChar_Nullable,
    t0.DemoByte AS DemoByte,
    t0.DemoByte_Nullable AS DemoByte_Nullable,
    t0.DemoDecimal AS DemoDecimal,
    t0.DemoDecimal_Nullable AS DemoDecimal_Nullable,
    t0.DemoDouble AS DemoDouble,
    t0.DemoDouble_Nullable AS DemoDouble_Nullable,
    t0.DemoFloat AS DemoFloat,
    t0.DemoFloat_Nullable AS DemoFloat_Nullable,
    t0.DemoGuid AS DemoGuid,
    t0.DemoGuid_Nullable AS DemoGuid_Nullable,
    t0.DemoShort AS DemoShort,
    t0.DemoShort_Nullable AS DemoShort_Nullable,
    t0.DemoInt AS DemoInt,
    t0.DemoInt_Nullable AS DemoInt_Nullable,
    t0.DemoLong AS DemoLong,
    t0.DemoLong_Nullable AS DemoLong_Nullable FROM SYS_Demo t0 WHERE  DemoID=1") as OracleDataReader;
            while (reader.Read())
            {
                var tz = reader.GetOracleTimeStampTZ(12);
                var tz_of =Convert.ChangeType(tz, typeof(DateTimeOffset));
                var result = new DateTimeOffset(tz.Value, tz.GetTimeZoneOffset());

                var ltz = reader.GetOracleTimeStampLTZ(13);
                var ltz1 = reader.GetValue(13);
                var ltz2 = reader.GetDateTime(13);

                var f1 = reader.GetFieldType(12);
                var f2 = reader.GetFieldType(13);
            }

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

            var modelsssss = context.GetTable<OracleModel.OracleDemo>().OrderByDescending(a => a.DemoId).FirstOrDefault();

            // 批量增加
            // 产生 INSERT INTO VALUES(),(),()... 语法。注意这种批量增加的方法并不能给自增列自动赋值
            context.Delete<OracleModel.OracleDemo>(x => x.DemoId > 1000000);
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
                    DemoDatetimeOffset_Nullable = DateTime.Now,
                    DemoTimestamp_Nullable = DateTime.Now,
                    DemoText_Nullable = "TEXT 类型",
                    DemoNText_Nullable = "NTEXT 类型"
                };
                demos.Add(d);
            }
            context.Insert<OracleModel.OracleDemo>(demos);
            context.SubmitChanges();

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
                DemoDatetimeOffset_Nullable = DateTime.Now,
                DemoTimestamp_Nullable = DateTime.Now,
                DemoText_Nullable = "TEXT 类型",
                DemoNText_Nullable = "NTEXT 类型",
                DemoBinary_Nullable = Encoding.UTF8.GetBytes("表示时区偏移量（分钟）（如果为整数）的表达式"),
                DemVarBinary_Nullable = Encoding.UTF8.GetBytes("表示时区偏移量（分钟）（如果为整数）的表达式")
            };
            context.Insert(demo);
            context.SubmitChanges();

            demo = context.GetTable<OracleModel.OracleDemo>().FirstOrDefault(x => x.DemoId == demo.DemoId);
        }
    }
}
