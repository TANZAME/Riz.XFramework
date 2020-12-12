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
    public class MySqlTest : TestBase<MySqlModel.Demo>
    {
#if net40
        const string connString = "Host=localhost;Database=Riz_XFramework;uid=root;pwd=123456;Pooling=true;Min Pool Size=1;Max Pool Size=1;Connection Lifetime=;";
#else
        // https://mysqlconnector.net/api/mysqlconnector/mysqlbulkcopytype/
        const string connString = "Host=localhost;Database=Riz_XFramework;uid=root;pwd=123456;Pooling=true;Min Pool Size=1;Max Pool Size=1;Connection Lifetime=;AllowLoadLocalInfile=True;";
#endif

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
                 from a in context.GetTable<MySqlModel.Demo>()
                 where a.DemoId <= 10
                 select new MySqlModel.Demo(a);
            var r1 = query.ToList();
            query =
               from a in context.GetTable<MySqlModel.Demo>()
               where a.DemoId <= 10
               select new MySqlModel.Demo(a.DemoId, a.DemoName);
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
            var demos = new List<MySqlModel.Demo>();
            for (int i = 0; i < 5; i++)
            {
                MySqlModel.Demo d = new MySqlModel.Demo
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
                    DemoTime_Nullable = i % 2 == 0 ? new TimeSpan(-34, -22, -59, -59) : new TimeSpan(34, 22, 59, 59),
                    DemoDatetimeOffset_Nullable = DateTime.Now,
                    DemoTimestamp_Nullable = DateTime.Now,
                    DemoText_Nullable = "TEXT 类型",
                    DemoNText_Nullable = "NTEXT 类型",
                    DemoBinary_Nullable = i % 2 == 0 ? Encoding.UTF8.GetBytes("表示时区偏移量（分钟）（如果为整数）的表达式") : null,
                    DemoVarBinary_Nullable = i % 2 == 0 ? Encoding.UTF8.GetBytes(LongText.LONGTEXT) : new byte[0],
                };
                demos.Add(d);
            }
            context.Insert<MySqlModel.Demo>(demos);
            context.SubmitChanges();
            var myList = context
                .GetTable<MySqlModel.Demo>()
                .OrderByDescending(x => x.DemoId)
                .Take(5).ToList();
            Debug.Assert(myList[0].DemVarBinary_s == LongText.LONGTEXT);

            // byte[]
            var demo = new MySqlModel.Demo
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
                DemoTime_Nullable = new TimeSpan(0, 10, 10, 10) + TimeSpan.FromTicks(456789 * 10),
                DemoDatetimeOffset_Nullable = DateTime.Now,
                DemoTimestamp_Nullable = DateTime.Now,
                DemoText_Nullable = "TEXT 类型",
                DemoNText_Nullable = "NTEXT 类型",
                DemoBinary_Nullable = Encoding.UTF8.GetBytes("表示时区偏移量（分钟）（如果为整数）的表达式"),
                DemoVarBinary_Nullable = Encoding.UTF8.GetBytes(LongText.LONGTEXT),
            };
            context.Insert(demo);
            context.SubmitChanges();

            demo = context.GetTable<MySqlModel.Demo>().FirstOrDefault(x => x.DemoId == demo.DemoId);
            Debug.Assert(demo.DemVarBinary_s == LongText.LONGTEXT);
            var hex = context
                .GetTable<MySqlModel.Demo>()
                .Where(x => x.DemoId == demo.DemoId)
                .Select(x => x.DemoVarBinary_Nullable.ToString())
                .FirstOrDefault();

#if !net40
            //context.Delete<Model.Client>(x => x.ClientId >= 2000);
            //context.SubmitChanges();
            //var query =
            //    from a in context.GetTable<Model.Client>()
            //    where a.ClientId <= 10
            //    select a;

            //var table = query.ToDataTable<Model.Client>();
            //table.TableName = "Bas_Client";
            //table.Rows.Clear();
            //int maxId = context.GetTable<Model.Client>().Max(x => x.ClientId);
            //for (int i = 1; i <= 10; i++)
            //{
            //    var row = table.NewRow();
            //    row["ClientId"] = maxId + i;
            //    row["ClientCode"] = "C" + i;
            //    row["ClientName"] = "N" + i;
            //    row["CloudServerId"] = 0;
            //    row["ActiveDate"] = DateTime.Now;
            //    row["Qty"] = 0;
            //    row["State"] = 1;
            //    row["Remark"] = string.Empty;
            //    table.Rows.Add(row);
            //}
            //table.AcceptChanges();

            //DateTime sDate2 = DateTime.Now;
            //((MySqlDbContext)context).BulkCopy(table);
            //var ms = (DateTime.Now - sDate2).TotalMilliseconds;
            //// 10w   300ms
            //// 100w  4600ms
#endif
        }
    }
}
