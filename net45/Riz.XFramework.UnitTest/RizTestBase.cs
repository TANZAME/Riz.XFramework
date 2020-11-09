using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

using Riz.XFramework.Data;
using System.Data;
using System.Reflection;

namespace Riz.XFramework.UnitTest
{
    public abstract class RizTestBase<TDemo> : ITest where TDemo : RizModel.Demo, new()
    {
        private string _demoName = "002F";
        private int[] _demoIdList = new int[] { 2, 3 };
        private string[] _demoNameList = new string[] { "A", "B", "C" };
        private DatabaseType _databaseType = DatabaseType.None;
        // 参数化查询语句数量@@
        protected Func<IDbContext> _newContext = null;

        /// <summary>
        /// 调式模式，调式模式下产生的SQL会换行，方便阅读
        /// </summary>
        public bool IsDebug { get; set; }

        /// <summary>
        /// 大小写敏感
        /// </summary>
        public bool CaseSensitive { get; set; }

        public RizTestBase()
        {
            _newContext = this.CreateDbContext;

            DateTime dateTime = DateTime.Parse("2019-10-27 23:59:59.1234567");
            long result = dateTime.Ticks;
            var r2 = Math.Log(100);
        }

        public abstract IDbContext CreateDbContext();

        public virtual void Run(DatabaseType dbType)
        {
            _databaseType = dbType;
            Query();
            DbFunction();
            Join();
            Insert();
            Update();
            Delete();
            API();
            Rabbit();
        }

        // 单表查询
        protected virtual void Query()
        {
            Console.WriteLine("***** Query *****");
            var context = _newContext();

            // 查询表达式 <注：Date,DateTime,DateTime2的支持>
            DateTime sDate = DateTime.Now.AddYears(-9);
            Nullable<DateTime> sDate_null = new Nullable<DateTime>(sDate);

            //// 匿名类
            var guid = Guid.NewGuid();
            var dynamicQuery =
                from a in context.GetTable<TDemo>()
                where a.RizDemoId <= 10
                select new
                {
                    RizDemoId = 12,
                    DemoCode = a.RizDemoCode,
                    DemoName = a.RizDemoName,
                    DemoDateTime_Nullable = a.RizDemoDateTime_Nullable,
                    DemoDate = sDate,
                    DemoDateTime = DateTime.Now,
                    DemoDateTime2 = sDate_null,
                    DemoGuid = guid,
                    DemoEnum = RizModel.State.Complete,
                    DemoEnum2 = RizModel.State.Executing,
                };
            var result0 = dynamicQuery.ToList();
            context.Database.ExecuteNonQuery(dynamicQuery.ToString());
            //SQL=>
            //SELECT 
            //12 AS [RizDemoId],
            //t0.[DemoCode] AS [DemoCode],
            //t0.[DemoName] AS [DemoName],
            //t0.[DemoDateTime_Nullable] AS [DemoDateTime_Nullable],
            //'2010-04-13 22:50:38.827' AS [DemoDate],
            //'2010-04-13 22:50:38.827' AS [DemoDateTime],
            //'2010-04-13 22:50:38.827' AS [DemoDateTime2],
            //'d748f165-56e1-4ade-8018-1244e1b0439d' AS [DemoGuid],
            //1 AS [DemoEnum],
            //0 AS [DemoEnum2]
            //FROM [Sys_Demo] t0 
            //WHERE t0.[RizDemoId] <= 10
            // 点标记
            dynamicQuery = context
                .GetTable<TDemo>()
                .Where(a => a.RizDemoId <= 10)
                .Select(a => new
                {
                    RizDemoId = 13,
                    DemoCode = a.RizDemoCode,
                    DemoName = a.RizDemoName,
                    DemoDateTime_Nullable = a.RizDemoDateTime_Nullable,
                    DemoDate = sDate,
                    DemoDateTime = DateTime.Now,
                    DemoDateTime2 = sDate_null,
                    DemoGuid = Guid.NewGuid(),
                    DemoEnum = RizModel.State.Complete,
                    DemoEnum2 = RizModel.State.Executing
                });
            result0 = dynamicQuery.ToList();
            context.Database.ExecuteNonQuery(dynamicQuery.ToString());
#if !net40
            result0 = dynamicQuery.ToListAsync().Result;
#endif
            //SQL=>
            //SELECT 
            //13 AS [RizDemoId],
            //t0.[DemoCode] AS [DemoCode],
            //t0.[DemoName] AS [DemoName],
            //t0.[DemoDateTime_Nullable] AS [DemoDateTime_Nullable],
            //'2010-04-13 22:50:38.827' AS [DemoDate],
            //'2010-04-13 22:50:38.827' AS [DemoDateTime],
            //'2010-04-13 22:50:38.827' AS [DemoDateTime2],
            //NEWID() AS [DemoGuid],
            //1 AS [DemoEnum],
            //0 AS [DemoEnum2]
            //FROM [Sys_Demo] t0 
            //WHERE t0.[RizDemoId] <= 10

            var result5 = context.GetTable<TDemo>().Where(x => x.RizDemoId <= 10).Select<TDemo, dynamic>().ToList();
            if (!this.CaseSensitive) result5 = context.Database.Execute<List<dynamic>>("SELECT * FROM Sys_Demo WHERE DemoId <= 10");

            // Date,DateTime,DateTime2 支持
            var query =
                from a in context.GetTable<TDemo>()
                where a.RizDemoId <= 10 && a.RizDemoDate > sDate && a.RizDemoDateTime >= sDate && a.RizDemoDateTime2 > sDate
                select a;
            var result1 = query.ToList();
            context.Database.ExecuteNonQuery(query.ToString());
            // 点标记
            query = context
                .GetTable<TDemo>()
                .Where(a => a.RizDemoId <= 10 && a.RizDemoDate > sDate && a.RizDemoDateTime >= sDate && a.RizDemoDateTime2 > sDate);
            result1 = query.ToList();
#if !net40
            result1 = query.ToListAsync().Result;
#endif
            //SQL=> 
            //SELECT 
            //t0.[RizDemoId] AS [RizDemoId],
            //t0.[DemoCode] AS [DemoCode],
            //t0.[DemoName] AS [DemoName],
            //****
            //FROM [Sys_Demo] t0 
            //WHERE t0.[RizDemoId] > 0 AND t0.[DemoDate] > '2010-04-03' AND t0.[DemoDateTime] >= '2010-04-03 17:12:03.378' AND t0.[DemoDateTime2] > '2010-04-03 17:12:03.378697'

            // 指定字段
            query = from a in context.GetTable<TDemo>()
                    where a.RizDemoId <= 10
                    select new TDemo
                    {
                        RizDemoId = (int)a.RizDemoId,
                        RizDemoCode = (a.RizDemoCode ?? "C0000001"),
                        RizDemoName = a.RizDemoId.ToString(),
                        RizDemoDateTime_Nullable = a.RizDemoDateTime_Nullable,
                        RizDemoDate = sDate,
                        RizDemoDateTime = sDate,
                        RizDemoDateTime2 = sDate
                    };
            result1 = query.ToList();
            context.Database.ExecuteNonQuery(query.ToString());
            // 点标记
            query = context
                .GetTable<TDemo>()
                .Where(a => a.RizDemoCode != a.RizDemoId.ToString() && a.RizDemoName != a.RizDemoId.ToString() && a.RizDemoChar == (a.RizDemoId == 0 ? 'A' : 'A') && a.RizDemoNChar == 'B')
                .Select(a => new TDemo
                {
                    RizDemoDate = a.RizDemoDateTime2_Nullable.Value,
                    RizDemoId = a.RizDemoId,
                    RizDemoCode = Data.DbFunction.CaseWhen(a.RizDemoName == "张1", "李1").When(a.RizDemoName == "张2", "李2").End("李李"), //a.DemoName == "张三" ? "李四" : "王五",
                    RizDemoName = a.RizDemoCode == "张三" ? "李四" : "王五",
                    RizDemoChar = 'A',
                    RizDemoNChar = 'B',
                    RizDemoDateTime_Nullable = a.RizDemoDateTime_Nullable,
                    //DemoDate = sDate,
                    RizDemoDateTime = sDate,
                    RizDemoDateTime2 = sDate,
                });
            result1 = query.ToList();
            context.Database.ExecuteNonQuery(query.ToString());
            //SQL=> 
            //SELECT 
            //t0.[RizDemoId] AS [RizDemoId],
            //ISNULL(t0.[DemoCode],N'N001') AS [DemoCode],
            //CAST(t0.[RizDemoId] AS NVARCHAR(max)) AS [DemoName],
            //t0.[DemoDateTime_Nullable] AS [DemoDateTime_Nullable],
            //'2010-04-13' AS [DemoDate],
            //'2010-04-13 22:50:38.827' AS [DemoDateTime],
            //'2010-04-13 22:50:38.827401' AS [DemoDateTime2]
            //FROM [Sys_Demo] t0 
            //WHERE t0.[RizDemoId] <= 10

            query = context.GetTable<TDemo>().Where(a => a.RizDemoId <= 10 && context.GetTable<TDemo>().Select(e => e.RizDemoName).Contains(a.RizDemoName));
            result1 = query.ToList();
            context.Database.ExecuteNonQuery(query.ToString());

            var queryFilters = context.GetTable<TDemo>().Where(a => a.RizDemoBoolean && a.RizDemoByte != 2).Select(a => a.RizDemoName);
            query = context.GetTable<TDemo>().Where(a => a.RizDemoId <= 10 && !queryFilters.Contains(a.RizDemoName));
            result1 = query.ToList();
            context.Database.ExecuteNonQuery(query.ToString());

            // 带参数构造函数
            Parameterized();
            //query =
            //     from a in context.GetTable<TDemo>()
            //     where a.RizDemoId <= 10
            //     select new TDemo(a);   
            //r1 = query.ToList();
            //query =
            //   from a in context.GetTable<TDemo>()
            //   where a.RizDemoId <= 10
            //   select new TDemo(a.RizDemoId, a.DemoName);
            //r1 = query.ToList();
            //SQL=> 
            //SELECT 
            //t0.[RizDemoId] AS [RizDemoId],
            //t0.[DemoName] AS [DemoName]
            //FROM [Sys_Demo] t0 

            var query6 = context.GetTable<TDemo>().Select(a => a.RizDemoCode ?? "N");
            var result6 = query6.ToList();
            context.Database.ExecuteNonQuery(query6.ToString());

            query6 = context.GetTable<TDemo>().Select(a => a.RizDemoCode + a.RizDemoName);
            result6 = query6.ToList();
            context.Database.ExecuteNonQuery(query6.ToString());

            //分页查询（非微软api）
            query = from a in context.GetTable<TDemo>() select a;
            var result2 = query.ToPagedList(1, 20);
#if !net40
            result2 = query.ToPagedListAsync(1, 20).Result;
#endif
            //SQL=>
            //SELECT TOP(20)
            //t0.[RizDemoId] AS [RizDemoId],
            //t0.[DemoCode] AS [DemoCode],
            //t0.[DemoName] AS [DemoName],
            //...
            //t0.[DemoLong_Nullable] AS [DemoLong_Nullable]
            //FROM [Sys_Demo] t0 

            // 如果页码大于1，必须指定 OrderBy ###
            query = context.GetTable<TDemo>();
            result2 = query.OrderBy(a => a.RizDemoDecimal).ToPagedList(2, 1);
            //SQL=>
            //SELECT
            //t0.[RizDemoId] AS[RizDemoId],
            //t0.[DemoCode] AS[DemoCode],
            //t0.[DemoName] AS[DemoName],
            //...
            //FROM[Sys_Demo] t0
            //ORDER BY t0.[DemoDecimal]
            //OFFSET 1 ROWS FETCH NEXT 1 ROWS ONLY

            // 分页查询
            // 1.不是查询第一页的内容时，必须先OrderBy再分页，OFFSET ... Fetch Next 分页语句要求有 OrderBy
            // 2.OrderBy表达式里边的参数必须跟query里边的变量名一致，如此例里的 a。SQL解析时根据此变更生成表别名
            query = from a in context.GetTable<TDemo>()
                    orderby a.RizDemoCode
                    select a;
            query = query.Skip(1).Take(18);
            result1 = query.ToList();
            context.Database.ExecuteNonQuery(query.ToString());
            // 点标记
            query = context
                .GetTable<TDemo>()
                .OrderBy(a => a.RizDemoCode)
                .Skip(1)
                .Take(18);
            result1 = query.ToList();
            //SQL=>
            //SELECT 
            //t0.[RizDemoId] AS [RizDemoId],
            //t0.[DemoCode] AS [DemoCode],
            //t0.[DemoName] AS [DemoName],
            //...
            //t0.[DemoLong_Nullable] AS [DemoLong_Nullable]
            //FROM [Sys_Demo] t0 
            //ORDER BY t0.[DemoCode]
            //OFFSET 1 ROWS FETCH NEXT 18 ROWS ONLY

            query =
                from a in context.GetTable<TDemo>()
                where a.RizDemoId <= 10
                orderby a.RizDemoCode
                select a;
            query = query.Skip(1);
            result1 = query.ToList();
            //SQL=>
            //SELECT 
            //t0.[RizDemoId] AS [RizDemoId],
            //t0.[DemoCode] AS [DemoCode],
            //t0.[DemoName] AS [DemoName],
            //...
            //t0.[DemoLong_Nullable] AS [DemoLong_Nullable]
            //FROM [Sys_Demo] t0 
            //ORDER BY t0.[DemoCode]
            //OFFSET 1 ROWS

            query =
                from a in context.GetTable<TDemo>()
                orderby a.RizDemoCode
                select a;
            query = query.Take(1);
            result1 = query.ToList();
            //SQL=>
            //SELECT TOP(1)
            //t0.[RizDemoId] AS [RizDemoId],
            //t0.[DemoCode] AS [DemoCode],
            //t0.[DemoName] AS [DemoName],
            //...
            //t0.[DemoLong_Nullable] AS [DemoLong_Nullable]
            //FROM [Sys_Demo] t0 
            //ORDER BY t0.[DemoCode]

            // 分页后查查询，结果会产生嵌套查询
            query =
                from a in context.GetTable<TDemo>()
                orderby a.RizDemoCode
                select a;
            query = query.Skip(1);
            query = query.Where(a => a.RizDemoId <= 10);
            query = query.OrderBy(a => a.RizDemoCode).Skip(1).Take(1);
            result1 = query.ToList();
            context.Database.ExecuteNonQuery(query.ToString());
            //SQL=>
            //SELECT 
            //t0.[RizDemoId] AS [RizDemoId],
            //t0.[DemoCode] AS [DemoCode],
            //t0.[DemoName] AS [DemoName],
            //...
            //t0.[DemoLong_Nullable] AS [DemoLong_Nullable]
            //FROM (
            //    SELECT 
            //    t0.[RizDemoId] AS [RizDemoId],
            //    t0.[DemoCode] AS [DemoCode],
            //    t0.[DemoName] AS [DemoName],
            //    ...
            //    t0.[DemoLong_Nullable] AS [DemoLong_Nullable]
            //    FROM [Sys_Demo] t0 
            //    ORDER BY t0.[DemoCode]
            //    OFFSET 1 ROWS
            //) t0 
            //WHERE t0.[RizDemoId] > 0
            //ORDER BY t0.[DemoCode]
            //OFFSET 1 ROWS FETCH NEXT 1 ROWS ONLY 

            // 过滤条件
            query = from a in context.GetTable<TDemo>()
                    where a.RizDemoName == "N0000002" || a.RizDemoCode == "C0000002" && a.RizDemoByte_Nullable.Value > 0
                    select a;
            result1 = query.ToList();
            // 点标记
            query = context.GetTable<TDemo>().Where(a => a.RizDemoName == "N0000002" || a.RizDemoCode == "C0000002");
            result1 = query.ToList();
            query = context.GetTable<TDemo>().Where(a => a.RizDemoName.Contains("00"));
            result1 = query.ToList();
            Debug.Assert(result1.Count != 0);

            query = context.GetTable<TDemo>().Where(a => a.RizDemoCode.StartsWith("C0000009"));
            result1 = query.ToList();
            Debug.Assert(result1.Count != 0);

            query = context.GetTable<TDemo>().Where(a => a.RizDemoCode.EndsWith("C0000009") &&
                a.RizDemoCode.Contains(a.RizDemoName) && a.RizDemoCode.StartsWith(a.RizDemoName) && a.RizDemoCode.EndsWith(a.RizDemoName));
            result1 = query.ToList();

            query = context.GetTable<TDemo>().Where(a => (a.RizDemoId + 2) * 12 == 12 && a.RizDemoId + a.RizDemoByte * 12 == 12);
            result1 = query.ToList();
            query = context.GetTable<TDemo>().Where(a =>
                a.RizDemoCode.StartsWith(a.RizDemoName ?? "C0000009") || a.RizDemoCode.StartsWith(a.RizDemoName.Length > 0 ? "C0000009" : "C0000010"));
            result1 = query.ToList();
            context.Database.ExecuteNonQuery(query.ToString());
            //SQL=>
            //SELECT 
            //t0.[RizDemoId] AS [RizDemoId],
            //t0.[DemoCode] AS [DemoCode],
            //t0.[DemoName] AS [DemoName],
            //...
            //t0.[DemoLong_Nullable] AS [DemoLong_Nullable]
            //FROM [Sys_Demo] t0 
            //WHERE (t0.[DemoName] = N'D0000002') OR (t0.[DemoCode] = 'D0000002')

            // 支持的查询条件
            // 区分 nvarchar,varchar,date,datetime,datetime2 字段类型
            // 支持的字符串操作=> Trim | TrimStart | TrimEnd | ToString | Length 等

            int m_byte = 16;
            RizModel.State state = RizModel.State.Complete;

            // 点标记
            query = context.GetTable<TDemo>().Where(a =>
                        new[] { 1, 2, 3 }.Contains(a.RizDemoId) &&                         // IN(1,2,3)
                        new List<int> { 1, 2, 3 }.Contains(a.RizDemoId) &&                 // IN(1,2,3)
                        new List<int>(_demoIdList).Contains(a.RizDemoId) &&                // IN(1,2,3)
                        a.RizDemoId == new List<int> { 1, 2, 3 }[0] &&                     // IN(1,2,3)
                        _demoIdList.Contains(a.RizDemoId) &&                               // IN(1,2,3)
                        a.RizDemoName == _demoName &&
                        a.RizDemoCode == (a.RizDemoCode ?? "CODE") &&
                        new List<string> { "A", "B", "C" }.Contains(a.RizDemoCode) &&
                        new List<string> { "A", "B", "C" }.Contains(a.RizDemoName) &&
                        _demoNameList.Contains(a.RizDemoCode) &&
                        _demoNameList.Contains(a.RizDemoName) &&
                        a.RizDemoByte == (byte)m_byte &&
                        a.RizDemoByte == (byte)RizModel.State.Complete ||
                        a.RizDemoInt == (int)RizModel.State.Complete ||
                        a.RizDemoInt == (int)state ||
                        (a.RizDemoName == "STATE" && a.RizDemoName == "REMARK" && a.RizDemoName == _demoNameList[0]));               // OR 查询

            result1 = query.ToList();
            context.Database.ExecuteNonQuery(query.ToString());

            //SQL=>            
            //SELECT
            //t0.[RizDemoId] AS[RizDemoId],
            //t0.[DemoCode] AS[DemoCode],
            //t0.[DemoName] AS[DemoName],
            //...
            //FROM[Sys_Demo] t0
            //WHERE (((t0.[DemoCode] = '002' AND t0.[DemoName] = N'002' AND t0.[DemoCode] LIKE '%TAN%' AND t0.[DemoName] LIKE N'%TAN%' AND 
            //t0.[DemoCode] LIKE 'TAN%' AND t0.[DemoCode] LIKE '%TAN' AND LEN(t0.[DemoCode]) = 12 AND LTRIM(t0.[DemoCode]) = 'TF' AND 
            //RTRIM(t0.[DemoCode]) = 'TF' AND RTRIM(t0.[DemoCode]) = 'TF' AND SUBSTRING(t0.[DemoCode],0 + 1,LEN(t0.[DemoCode])) = 'TF' AND 
            //t0.[DemoDate] = '2019-04-13' AND t0.[DemoDateTime] = '2019-04-13 22:50:39.340' AND t0.[DemoDateTime2] = '2019-04-13 22:50:39.340744' AND 
            //t0.[DemoName] = (CASE WHEN t0.[DemoDateTime_Nullable] IS NULL THEN N'NULL' ELSE N'NOT NULL' END) AND 
            //t0.[DemoName] = ISNULL(t0.[DemoName],t0.[DemoCode]) AND t0.[RizDemoId] IN(1,2,3) AND t0.[RizDemoId] IN(1,2,3) AND 
            //t0.[RizDemoId] IN(2,3) AND t0.[RizDemoId] = 1 AND t0.[RizDemoId] IN(2,3) AND t0.[DemoName] = N'002F' AND 
            //t0.[DemoCode] = ISNULL(t0.[DemoCode],'CODE') AND t0.[DemoCode] IN('A','B','C') AND t0.[DemoByte] = 9 AND 
            //t0.[DemoByte] = 1) OR (t0.[DemoInt] = 1)) OR (t0.[DemoInt] = 1)) OR (t0.[DemoName] = N'STATE' AND t0.[DemoName] = N'REMARK')

            // 行号
            var query1 =
                context
                .GetTable<TDemo>()
                .Select(a => new
                {
                    RowNumber = Data.DbFunction.RowNumber(a.RizDemoCode)
                });
            var reuslt1 = query1.ToList();
            Debug.Assert(reuslt1[0].RowNumber == 1 && (reuslt1.Count > 1 ? reuslt1[1].RowNumber == 2 : true));
            //SQL=>
            //SELECT 
            //ROW_NUMBER() Over(Order By t0.[DemoCode]) AS [RowNumber]
            //FROM [Sys_Demo] t0 

            var query2 =
                 context
                .GetTable<TDemo>()
                .Select(a => new
                {
                    RowNumber = Data.DbFunction.RowNumber<long, string>(a.RizDemoCode, OrderBy.DESC)
                });
            var result2_0 = query2.ToList();

            query1 =
            context
            .GetTable<RizModel.ClientAccount>()
            .Select(a => new
            {
                RowNumber = Data.DbFunction.PartitionRowNumber(a.RizClientId, a.RizAccountId)
            });
            reuslt1 = query1.ToList();
            context.Database.ExecuteNonQuery(query1.ToString());
            query2 =
                context
            .GetTable<RizModel.ClientAccount>()
                .Select(a => new
                {
                    RowNumber = Data.DbFunction.PartitionRowNumber<long, int, string>(a.RizClientId, a.RizAccountId, OrderBy.DESC)
                });
            result2_0 = query2.ToList();

            // DataTable
            query = from a in context.GetTable<TDemo>()
                    orderby a.RizDemoCode
                    select a;
            query = query.Take(18);
            var result3 = query.Execute<DataTable>(); //context.Database.ExecuteDataTable(query);
#if !net40
            query = from a in context.GetTable<TDemo>()
                    orderby a.RizDemoCode
                    select a;
            query = query.Take(18);
            result3 = query.ExecuteAsync<DataTable>().Result; //context.Database.ExecuteDataTableAsync(query).Result;
#endif
            // DataSet
            //var cmd = query.Resolve();
            List<DbRawCommand> sqlList = context.Provider.Translate(new List<object> { query, query, query }); //new List<RawCommand> { cmd, cmd, cmd };
            var result4 = context.Database.Execute<DataSet>(sqlList);
#if !net40
            ////cmd = query.Resolve();
            //sqlList = new List<RawCommand> { cmd, cmd, cmd };
            sqlList = context.Provider.Translate(new List<object> { query, query, query });
            result4 = context.Database.ExecuteAsync<DataSet>(sqlList).Result;
#endif

        }

        // 数据库函数支持
        protected virtual void DbFunction()
        {
            Console.WriteLine("***** DbFunction *****");
            var context = _newContext();
            int m_byte = 16;
            RizModel.State state = RizModel.State.Complete;
            TimeSpan ts = new TimeSpan(1000000000);
            var myDemo = context.GetTable<TDemo>().FirstOrDefault(x => x.RizDemoId == 1);

            #region 字符类型

            // 字符串操作
            var query = from a in context.GetTable<TDemo>()
                        where
                            string.IsNullOrEmpty(a.RizDemoCode) &&
                            string.IsNullOrEmpty(a.RizDemoName) &&
                            string.Concat(a.RizDemoCode, a.RizDemoName, a.RizDemoChar) == "O" &&
                            string.Concat(a.RizDemoCode, a.RizDemoName, a.RizDemoChar) == "1" + "2" + "3" &&
                            a.RizDemoCode.TrimStart() == "TF" &&
                            a.RizDemoCode.TrimEnd() == "TF" &&
                            a.RizDemoCode.Trim() == "TF" &&
                            a.RizDemoCode.Substring(0) == "TF" &&
                            a.RizDemoCode.Substring(0, 4) == "TF" &&
                            a.RizDemoCode.Contains("TAN") &&                                   // LIKE '%%'
                            a.RizDemoCode.StartsWith("TAN") &&                                 // LIKE 'K%'
                            a.RizDemoCode.EndsWith("TAN") &&                                   // LIKE '%K'
                            !a.RizDemoCode.EndsWith("TAN") &&                                  // NOT LIKE '%K'
                            a.RizDemoCode.Length == 12 &&                                      // LENGTH
                            a.RizDemoCode == (a.RizDemoCode ?? "C0000009") &&
                            a.RizDemoName.ToUpper() == "FF" &&
                            a.RizDemoName.ToLower() == "ff" &&
                            a.RizDemoName.Replace('A', 'B') == "ff" &&
                            a.RizDemoName.IndexOf('B') == 2 &&
                            a.RizDemoName.IndexOf('B', 2) == 2 &&
                            a.RizDemoName.PadLeft(5) == "F0" &&
                            a.RizDemoName.PadRight(5, 'F') == "F0" &&
                            a.RizDemoName.Contains("TAN") &&                                   // LIKE '%%'
                            a.RizDemoName == (a.RizDemoName ?? a.RizDemoCode) &&                     // 二元表达式
                            a.RizDemoName == (
                                a.RizDemoDateTime_Nullable == null ? "NULL" : "NOT NULL")      // 三元表达式
                        select a;
            var result = query.ToList();
            context.Database.ExecuteNonQuery(query.ToString());
            var query1 =
                context
                .GetTable<TDemo>()
                .Select(a => new
                {
                    DemoId = a.RizDemoId,
                    DemoCode = a.RizDemoCode.PadLeft(20),
                    DemoName = a.RizDemoCode.PadLeft(20, 'N'),
                    LowerName = a.RizDemoName.ToLower(),
                    Index1 = a.RizDemoCode.IndexOf('0'),
                    Index2 = a.RizDemoCode.IndexOf('1', 5),
                    Concat1 = string.Concat(a.RizDemoCode),
                    Concat2 = string.Concat(a.RizDemoCode, a.RizDemoName, a.RizDemoChar),
                    Concat3 = string.Concat("C"),
                    Concat4 = string.Concat("C", "00000", 0, 2),
                    Guid = a.RizDemoGuid.ToString(),
                    Time = a.RizDemoTime_Nullable.ToString(),
                    DemoDateTime2 = a.RizDemoDateTime2.ToString(),
                    DateTimeOffset = a.RizDemoDatetimeOffset_Nullable.ToString()
                });
            var obj1 = query1.FirstOrDefault(a => a.DemoId == 1);
            context.Database.ExecuteNonQuery(query1.ToString());
            Debug.Assert(obj1.DemoCode.Length == 20);
            Debug.Assert(obj1.LowerName == myDemo.RizDemoName.ToLower());
            Debug.Assert(obj1.Index1 > 0);
            Debug.Assert(obj1.Concat4 == "C0000002");
            Debug.Assert(obj1.Guid.ToLower() == myDemo.RizDemoGuid.ToString());
            //Debug.Assert(obj1.Time == myDemo.DemoTime_Nullable.Value.ToString(@"hh\:mm\:ss\.fffffff"));
            //Debug.Assert(obj1.DemoDateTime2 == myDemo.DemoDateTime2.ToString("yyyy-MM-dd HH:mm:ss.fffffff"));
            //Debug.Assert(obj1.DateTimeOffset == myDemo.DemoDatetimeOffset_Nullable.Value.ToString("yyyy-MM-dd HH:mm:ss.fffffff"));

            #endregion

            #region 数字类型

            // 数值型操作
            query = from a in context.GetTable<TDemo>()
                    where
                        a.RizDemoId % 2 == 10 &&
                        a.RizDemoId / 2 == 10 &&
                        Math.Abs(a.RizDemoDecimal) == 12 &&
                        Math.Acos((double)a.RizDemoDecimal) == 12 &&
                        Math.Asin((double)a.RizDemoDecimal) == 12 &&
                        Math.Atan((double)a.RizDemoDecimal) == 12 &&
                        Math.Atan2((double)a.RizDemoDecimal, a.RizDemoDouble) == 12 &&
                        Math.Ceiling(a.RizDemoDecimal) == 12 &&
                        Math.Cos((double)a.RizDemoDecimal) == 12 &&
                        Math.Exp((double)a.RizDemoDecimal) == 12 &&
                        Math.Floor((double)a.RizDemoDecimal) == 12 &&
                        Math.Log((double)a.RizDemoDecimal) == 12 &&
                        Math.Log((double)a.RizDemoDecimal, 5) == 12 &&
                        Math.Log10((double)a.RizDemoDecimal) == 12 &&
                        Math.PI == 12 &&
                        Math.Pow((double)a.RizDemoDecimal, a.RizDemoDouble) == 12 &&
                        Math.Round((double)a.RizDemoDecimal, 2) == 12 &&
                        Math.Sign(a.RizDemoDecimal) == 12 &&
                        Math.Sqrt((double)a.RizDemoDecimal) == 12 &&
                        Math.Tan((double)a.RizDemoDecimal) == 12 &&
                        Math.Truncate(a.RizDemoDecimal) == 12 &&
                        a.RizDemoByte == (byte)m_byte &&
                        a.RizDemoByte == (byte)RizModel.State.Complete ||
                        a.RizDemoInt == 409600000 ||
                        a.RizDemoInt == (int)state
                    select a;
            result = query.ToList();
            context.Database.ExecuteNonQuery(query.ToString());
            var query2 =
                context
                .GetTable<TDemo>()
                .Select(a => new
                {
                    DemoId = a.RizDemoId,
                    Mod = a.RizDemoId % 2,
                    Divide = Data.DbFunction.Cast<decimal>(a.RizDemoId, "decimal") / 2,
                    Abs = Math.Abs(a.RizDemoDecimal),
                    Acos = Math.Acos(a.RizDemoId / 2.00),
                    Asin = Math.Asin(a.RizDemoId / 2.00),
                    Atan = Math.Atan((double)a.RizDemoDecimal),
                    Atan2 = Math.Atan2((double)a.RizDemoByte, (double)a.RizDemoDecimal),
                    Ceiling = Math.Ceiling(a.RizDemoDecimal),
                    Cos = Math.Cos((double)a.RizDemoDecimal),
                    Exp = Math.Exp((double)a.RizDemoByte),
                    Floor = Math.Floor(a.RizDemoDecimal),
                    Log = Math.Log((double)a.RizDemoDecimal),
                    Log5 = Math.Log((double)a.RizDemoDecimal, 5),
                    Log10 = Math.Log10((double)a.RizDemoDecimal),
                    Pow = Math.Pow((double)a.RizDemoByte, 3),
                    Round = Math.Round(a.RizDemoDecimal),
                    Round1 = Math.Round(a.RizDemoDecimal, 1),
                    Sign = Math.Sign(a.RizDemoDecimal),
                    Sqrt = Math.Sqrt((double)a.RizDemoDecimal),
                    Tan = Math.Tan((double)a.RizDemoDecimal),
                    Truncate = Math.Truncate(a.RizDemoDecimal)
                });
            var obj2 = query2.FirstOrDefault(a => a.DemoId == 1);
            context.Database.ExecuteNonQuery(query2.Where(a => a.DemoId == 1).ToString());
            Debug.Assert(myDemo.RizDemoId % 2 == obj2.Mod);
            Debug.Assert(Math.Acos(myDemo.RizDemoId / 2.00) == obj2.Acos);
            Debug.Assert(Math.Asin(myDemo.RizDemoId / 2.00) == obj2.Asin);
            Debug.Assert(Math.Atan((double)myDemo.RizDemoDecimal) == obj2.Atan);
            Debug.Assert(Math.Atan2((double)myDemo.RizDemoByte, (double)myDemo.RizDemoDecimal) == obj2.Atan2);
            Debug.Assert(Math.Ceiling(myDemo.RizDemoDecimal) == obj2.Ceiling);
            Debug.Assert(Math.Cos((double)myDemo.RizDemoDecimal) == obj2.Cos);
            Debug.Assert(Math.Exp((double)myDemo.RizDemoByte) == obj2.Exp);
            Debug.Assert(Math.Floor(myDemo.RizDemoDecimal) == obj2.Floor);
            Debug.Assert(Math.Log((double)myDemo.RizDemoDecimal) == obj2.Log);
            Debug.Assert(Math.Log((double)myDemo.RizDemoDecimal, 5) == obj2.Log5);
            Debug.Assert(Math.Log10((double)myDemo.RizDemoDecimal) == obj2.Log10);
            Debug.Assert(Math.Pow((double)myDemo.RizDemoByte, 3) == obj2.Pow);
            Debug.Assert(Math.Round(myDemo.RizDemoDecimal) == obj2.Round);
            Debug.Assert(Math.Round(myDemo.RizDemoDecimal, 1) == obj2.Round1);
            Debug.Assert(Math.Sign(myDemo.RizDemoDecimal) == obj2.Sign);
            Debug.Assert(Math.Sqrt((double)myDemo.RizDemoDecimal) == obj2.Sqrt);
            Debug.Assert(Math.Tan((double)myDemo.RizDemoDecimal) == obj2.Tan);
            Debug.Assert(Math.Truncate(myDemo.RizDemoDecimal) == obj2.Truncate);

            #endregion

            #region 日期类型

            // 日期类型操作
            #region 条件

            query = from a in context.GetTable<TDemo>()
                    where a.RizDemoDate == DateTime.Now && !DateTime.IsLeapYear(2019)
                    select a;
            result = query.ToList();

            query = from a in context.GetTable<TDemo>()
                    where
                        a.RizDemoDate == DateTime.Now &&
                        a.RizDemoDateTime == DateTime.UtcNow &&
                        a.RizDemoDateTime2 == DateTime.Today &&
                        a.RizDemoDateTime2 == DateTime.MinValue &&
                        a.RizDemoDateTime2 == DateTime.MaxValue &&
                        DateTime.DaysInMonth(2019, 12) == 12 &&
                        DateTime.IsLeapYear(2019) &&
                        //a.DemoDate.Add(ts) == DateTime.Now &&
                        //a.DemoDate_Nullable.Value.Subtract(a.DemoDateTime) == ts &&
                        //a.DemoDate_Nullable.Value - a.DemoDateTime == ts &&
                        a.RizDemoDate.AddYears(12) == a.RizDemoDateTime &&
                        a.RizDemoDateTime.AddYears(12) == a.RizDemoDateTime &&
                        a.RizDemoDateTime2.AddYears(12) == a.RizDemoDateTime &&
                        a.RizDemoDate.AddMonths(12) == a.RizDemoDateTime &&
                        a.RizDemoDateTime.AddMonths(12) == a.RizDemoDateTime &&
                        a.RizDemoDateTime2.AddMonths(12) == a.RizDemoDateTime &&
                        a.RizDemoDate_Nullable.Value.AddDays(2) == a.RizDemoDateTime &&
                        a.RizDemoDate.AddDays(2) == a.RizDemoDateTime &&
                        a.RizDemoDateTime2.AddDays(2) == a.RizDemoDateTime &&
                        //a.DemoDate.AddHours(2) == a.DemoDateTime_Nullable.Value &&
                        //DbFunction.Cast<DateTime, DateTime>(a.DemoDate, "DATETIME2").AddHours(2) == a.DemoDateTime_Nullable.Value &&
                        a.RizDemoDateTime.AddHours(2) == a.RizDemoDateTime_Nullable.Value &&
                        a.RizDemoDateTime2.AddHours(2) == a.RizDemoDateTime_Nullable.Value &&
                        //a.DemoDate.AddMinutes(2) == a.DemoDateTime_Nullable.Value &&
                        a.RizDemoDateTime.AddMinutes(12) == a.RizDemoDateTime &&
                        a.RizDemoDateTime2.AddMinutes(12) == a.RizDemoDateTime &&
                        //a.DemoDate.AddSeconds(2) == a.DemoDateTime_Nullable.Value &&
                        a.RizDemoDateTime.AddSeconds(12) == a.RizDemoDateTime &&
                        a.RizDemoDateTime2.AddSeconds(12) == a.RizDemoDateTime &&
                        //a.DemoDate.AddMilliseconds(2) == a.DemoDateTime_Nullable.Value &&
                        a.RizDemoDateTime.AddMilliseconds(12) == a.RizDemoDateTime &&
                        a.RizDemoDateTime2.AddMilliseconds(12) == a.RizDemoDateTime &&
                        //a.DemoDate.AddTicks(2) == a.DemoDateTime_Nullable.Value &&
                        a.RizDemoDateTime.AddTicks(12) == a.RizDemoDateTime &&
                        a.RizDemoDateTime2.AddTicks(12) == a.RizDemoDateTime &&
                        a.RizDemoDate.Day == 12 &&
                        a.RizDemoDateTime.Day == 12 &&
                        a.RizDemoDateTime2.Day == 12 &&
                        a.RizDemoDate.DayOfWeek == DayOfWeek.Monday &&
                        a.RizDemoDateTime.DayOfWeek == DayOfWeek.Monday &&
                        a.RizDemoDateTime2.DayOfWeek == DayOfWeek.Monday &&
                        a.RizDemoDate.DayOfYear == 12 &&
                        a.RizDemoDateTime.DayOfYear == 12 &&
                        a.RizDemoDateTime2.DayOfYear == 12 &&
                        a.RizDemoDate.Year == 12 &&
                        a.RizDemoDate.Month == 12 &&
                        a.RizDemoDateTime.Month == 12 &&
                        a.RizDemoDateTime2.Month == 12 &&
                        a.RizDemoDate.Date == DateTime.Now.Date &&
                        a.RizDemoDateTime.Date == DateTime.Now.Date &&
                        a.RizDemoDateTime2.Date == DateTime.Now.Date &&
                        //a.DemoDate.Hour == 12 &&
                        a.RizDemoDateTime.Hour == 12 &&
                        a.RizDemoDateTime2.Hour == 12 &&
                        //a.DemoDate.Minute == 12 &&
                        a.RizDemoDateTime.Minute == 12 &&
                        a.RizDemoDateTime2.Minute == 12 &&
                        //a.DemoDate.Second == 12 &&
                        a.RizDemoDateTime.Second == 12 &&
                        a.RizDemoDateTime2.Second == 12 &&
                        //a.DemoDate.Millisecond == 12 &&
                        a.RizDemoDateTime.Millisecond == 12 &&
                        a.RizDemoDateTime2.Millisecond == 12 &&
                        //a.DemoDate.Ticks == 12 &&
                        a.RizDemoDateTime.Ticks == 12 &&
                        a.RizDemoDateTime2.Ticks == 12 &&
                        ts.Ticks == 12 &&
                        a.RizDemoDate.TimeOfDay == ts &&
                        DateTime.Now.Ticks == 12 &&
                        a.RizDemoDateTime.ToString() == "" &&
                        DateTime.Now.ToString() == ""
                    select a;
            result = query.ToList();
            context.Database.ExecuteNonQuery(query.ToString());

            #endregion

            var query3 =
                context
                .GetTable<TDemo>()
                .Select(a => new
                {
                    DemoId = a.RizDemoId,
                    Now = DateTime.Now,
                    UtcNow = DateTime.UtcNow,
                    Today = DateTime.Today,
                    MinValue = DateTime.MinValue,
                    MaxValue = DateTime.MaxValue,
                    DaysInMonth = DateTime.DaysInMonth(2019, 12),
                    //IsLeapYear = DateTime.IsLeapYear(2019),
                    AddYears = a.RizDemoDate.AddYears(12),
                    AddYears2 = a.RizDemoDateTime.AddYears(12),
                    AddYears3 = a.RizDemoDateTime2.AddYears(12),
                    AddMonths = a.RizDemoDate.AddMonths(12),
                    AddMonths2 = a.RizDemoDateTime.AddMonths(12),
                    AddMonths3 = a.RizDemoDateTime2.AddMonths(12),
                    AddDays = a.RizDemoDate.AddDays(12),
                    AddDays2 = a.RizDemoDateTime.AddDays(12),
                    AddDays3 = a.RizDemoDateTime2.AddDays(12),
                    //AddHours = a.DemoDate.AddHours(12),
                    AddHours2 = a.RizDemoDateTime.AddHours(12),
                    AddHours3 = a.RizDemoDateTime2.AddHours(12),
                    //AddMinutes = a.DemoDate.AddMinutes(12),
                    AddMinutes2 = a.RizDemoDateTime.AddMinutes(12),
                    AddMinutes3 = a.RizDemoDateTime2.AddMinutes(12),
                    //AddSeconds = a.DemoDate.AddSeconds(12),
                    AddSeconds2 = a.RizDemoDateTime.AddSeconds(12),
                    AddSeconds3 = a.RizDemoDateTime2.AddSeconds(12),
                    //AddMilliseconds = a.DemoDate.AddMilliseconds(12),
                    //AddMilliseconds2 = a.DemoDateTime.AddMilliseconds(12),
                    AddMilliseconds3 = a.RizDemoDateTime2.AddMilliseconds(12),
                    //AddTicks = a.DemoDate.AddTicks(12),
                    //AddTicks2 = a.DemoDate.AddTicks(12),
                    // MYSQL，POSTGRE 仅支持到 6 位精度
                    AddTicks3 = a.RizDemoDateTime2.AddTicks(10),
                    Year = a.RizDemoDate.Year,
                    Year2 = a.RizDemoDateTime.Year,
                    Year3 = a.RizDemoDateTime2.Year,
                    Month = a.RizDemoDate.Month,
                    Month2 = a.RizDemoDateTime.Month,
                    Month3 = a.RizDemoDateTime2.Month,
                    Date0 = a.RizDemoDate.Date,
                    Date2 = a.RizDemoDateTime.Date,
                    Date3 = a.RizDemoDateTime2.Date,
                    Day = a.RizDemoDate.Day,
                    Day2 = a.RizDemoDateTime.Day,
                    Day3 = a.RizDemoDateTime2.Day,
                    DayOfWeek = a.RizDemoDate.DayOfWeek,
                    DayOfWeek2 = a.RizDemoDateTime.DayOfWeek,
                    DayOfWeek3 = a.RizDemoDateTime2.DayOfWeek,
                    DayOfYear = a.RizDemoDate.DayOfYear,
                    DayOfYear2 = a.RizDemoDateTime.DayOfYear,
                    DayOfYear3 = a.RizDemoDateTime2.DayOfYear,
                    //Hour = a.DemoDate.Hour,
                    Hour2 = a.RizDemoDateTime.Hour,
                    Hour3 = a.RizDemoDateTime2.Hour,
                    //Minute = a.DemoDate.Minute,
                    Minute2 = a.RizDemoDateTime.Minute,
                    Minute3 = a.RizDemoDateTime2.Minute,
                    //Second = a.DemoDate.Second,
                    Second2 = a.RizDemoDateTime.Second,
                    Second3 = a.RizDemoDateTime2.Second,
                    //Millisecond = a.DemoDate.Millisecond,
                    Millisecond2 = a.RizDemoDateTime.Millisecond,
                    Millisecond3 = a.RizDemoDateTime2.Millisecond,
                    //Ticks = a.DemoDate.Ticks,
                    Ticks2 = a.RizDemoDateTime.Ticks,
                    Ticks3 = a.RizDemoDateTime2.Ticks,
                    Ticks4 = ts.Ticks,
                    Ticks5 = DateTime.Now.Ticks,
                    TimeOfDay = a.RizDemoDateTime.TimeOfDay,
                    ToString2 = a.RizDemoDateTime.ToString(),
                    ToString3 = DateTime.Now.ToString(),
                });
            var obj3 = query3.FirstOrDefault(a => a.DemoId == 1);
            context.Database.ExecuteNonQuery(query3.Where(a => a.DemoId == 1).ToString());
            Debug.Assert(obj3.AddYears == myDemo.RizDemoDate.AddYears(12));
            Debug.Assert(obj3.AddYears2 == myDemo.RizDemoDateTime.AddYears(12));
            Debug.Assert(obj3.AddYears3 == myDemo.RizDemoDateTime2.AddYears(12));
            Debug.Assert(obj3.AddMonths == myDemo.RizDemoDate.AddMonths(12));
            Debug.Assert(obj3.AddMonths2 == myDemo.RizDemoDateTime.AddMonths(12));
            Debug.Assert(obj3.AddMonths3 == myDemo.RizDemoDateTime2.AddMonths(12));
            Debug.Assert(obj3.AddDays == myDemo.RizDemoDate.AddDays(12));
            Debug.Assert(obj3.AddDays2 == myDemo.RizDemoDateTime.AddDays(12));
            Debug.Assert(obj3.AddDays3 == myDemo.RizDemoDateTime2.AddDays(12));
            //Debug.Assert(obj3.AddHours == myDemo.DemoDate.AddHours(12));
            Debug.Assert(obj3.AddHours2 == myDemo.RizDemoDateTime.AddHours(12));
            Debug.Assert(obj3.AddHours3 == myDemo.RizDemoDateTime2.AddHours(12));
            //Debug.Assert(obj3.AddMinutes == myDemo.DemoDate.AddMinutes(12));
            Debug.Assert(obj3.AddMinutes2 == myDemo.RizDemoDateTime.AddMinutes(12));
            Debug.Assert(obj3.AddMinutes3 == myDemo.RizDemoDateTime2.AddMinutes(12));
            //Debug.Assert(obj3.AddSeconds == myDemo.DemoDate.AddSeconds(12));
            Debug.Assert(obj3.AddSeconds2 == myDemo.RizDemoDateTime.AddSeconds(12));
            Debug.Assert(obj3.AddSeconds3 == myDemo.RizDemoDateTime2.AddSeconds(12));
            //Debug.Assert(obj3.AddMilliseconds == myDemo.DemoDate.AddMilliseconds(12));
            //Debug.Assert(obj3.AddMilliseconds2 == myDemo.DemoDateTime.AddMilliseconds(12));
            Debug.Assert(obj3.AddMilliseconds3 == myDemo.RizDemoDateTime2.AddMilliseconds(12));
            //Debug.Assert(obj3.AddTicks == myDemo.DemoDate.AddTicks(12));
            //Debug.Assert(obj3.AddTicks2 == myDemo.DemoDate.AddTicks(12));
            Debug.Assert(obj3.AddTicks3 == myDemo.RizDemoDateTime2.AddTicks(10));
            Debug.Assert(obj3.Year == myDemo.RizDemoDate.Year);
            Debug.Assert(obj3.Year2 == myDemo.RizDemoDateTime.Year);
            Debug.Assert(obj3.Year3 == myDemo.RizDemoDateTime2.Year);
            Debug.Assert(obj3.Month == myDemo.RizDemoDate.Month);
            Debug.Assert(obj3.Month2 == myDemo.RizDemoDateTime.Month);
            Debug.Assert(obj3.Month3 == myDemo.RizDemoDateTime2.Month);
            Debug.Assert(obj3.Date0 == myDemo.RizDemoDate.Date);
            Debug.Assert(obj3.Date2 == myDemo.RizDemoDateTime.Date);
            Debug.Assert(obj3.Date3 == myDemo.RizDemoDateTime2.Date);
            Debug.Assert(obj3.Day == myDemo.RizDemoDate.Day);
            Debug.Assert(obj3.Day2 == myDemo.RizDemoDateTime.Day);
            Debug.Assert(obj3.Day3 == myDemo.RizDemoDateTime2.Day);
            Debug.Assert(obj3.DayOfWeek == myDemo.RizDemoDate.DayOfWeek);
            Debug.Assert(obj3.DayOfWeek2 == myDemo.RizDemoDateTime.DayOfWeek);
            Debug.Assert(obj3.DayOfWeek3 == myDemo.RizDemoDateTime2.DayOfWeek);
            Debug.Assert(obj3.DayOfYear == myDemo.RizDemoDate.DayOfYear);
            Debug.Assert(obj3.DayOfYear2 == myDemo.RizDemoDateTime.DayOfYear);
            Debug.Assert(obj3.DayOfYear3 == myDemo.RizDemoDateTime2.DayOfYear);
            //Debug.Assert(obj3.Hour == myDemo.DemoDate.Hour);
            Debug.Assert(obj3.Hour2 == myDemo.RizDemoDateTime.Hour);
            Debug.Assert(obj3.Hour3 == myDemo.RizDemoDateTime2.Hour);
            //Debug.Assert(obj3.Minute == myDemo.DemoDate.Minute);
            Debug.Assert(obj3.Minute2 == myDemo.RizDemoDateTime.Minute);
            Debug.Assert(obj3.Minute3 == myDemo.RizDemoDateTime2.Minute);
            //Debug.Assert(obj3.Second == myDemo.DemoDate.Second);
            Debug.Assert(obj3.Second2 == myDemo.RizDemoDateTime.Second);
            Debug.Assert(obj3.Second3 == myDemo.RizDemoDateTime2.Second);
            //Debug.Assert(obj3.Millisecond == myDemo.DemoDate.Millisecond);
            Debug.Assert(obj3.Millisecond2 == myDemo.RizDemoDateTime.Millisecond);
            Debug.Assert(obj3.Millisecond3 == myDemo.RizDemoDateTime2.Millisecond);
            //Debug.Assert(obj3.Ticks == myDemo.DemoDate.Ticks);
            //Debug.Assert(obj3.Ticks2 == myDemo.DemoDateTime.Ticks);
            Debug.Assert(obj3.Ticks3 == myDemo.RizDemoDateTime2.Ticks);
            Debug.Assert(obj3.Ticks4 == ts.Ticks);
            Debug.Assert(obj3.TimeOfDay == myDemo.RizDemoDateTime.TimeOfDay);

            #endregion


            var queryFilters = context.GetTable<TDemo>().Where(a => a.RizDemoBoolean && a.RizDemoByte != 2).Select(a => a.RizDemoName);
            var newQuery =
                from a in context.GetTable<TDemo>()
                where
                    a.RizDemoName.StartsWith("5") && !a.RizDemoName.StartsWith("5") &&
                    queryFilters.Contains(a.RizDemoName) && !queryFilters.Contains(a.RizDemoName) &&
                    DateTime.IsLeapYear(a.RizDemoByte) && !DateTime.IsLeapYear(a.RizDemoByte)
                select new
                {
                    StartsWith = a.RizDemoName.StartsWith("5") ? true : false,
                    StartsWith2 = a.RizDemoName.StartsWith("5") ? true : false,
                    Contains = queryFilters.Contains(a.RizDemoName) ? true : false,
                    Contains2 = queryFilters.Contains(a.RizDemoName) ? true : false,
                    IsLeapYear = DateTime.IsLeapYear(a.RizDemoByte) ? true : false,
                    IsLeapYear2 = DateTime.IsLeapYear(a.RizDemoByte) ? true : false,
                };
        }

        // 多表查询
        protected virtual void Join()
        {
            Console.WriteLine("***** Join *****");
            var context = _newContext();

            // INNER JOIN
            var query =
                from a in context.GetTable<RizModel.Client>()
                join b in context.GetTable<RizModel.CloudServer>() on a.RizCloudServerId equals b.RizCloudServerId
                where a.RizClientId > 0
                select a;
            var result = query.ToList();
            // 点标记
            query = context
                .GetTable<RizModel.Client>()
                .Join(context.GetTable<RizModel.CloudServer>(), a => a.RizCloudServerId, b => b.RizCloudServerId, (a, b) => a)
                .Where(a => a.RizClientId > 0);
            result = query.ToList();
            query =
                from a in context.GetTable<RizModel.Client>()
                join b in context.GetTable<RizModel.Client, RizModel.CloudServer>(a => a.CloudServer) on a.RizCloudServerId equals b.RizCloudServerId
                where a.RizClientId > 0
                select a;
            result = query.ToList();
            //SQL=>
            //SELECT
            //t0.[RizClientId] AS[RizClientId],
            //t0.[RizClientCode] AS[RizClientCode],
            //t0.[RizClientName] AS[RizClientName],
            //t0.[RizRemark] AS[RizRemark],
            //t0.[State] AS[State],
            //t0.[RizActiveDate] AS[RizActiveDate],
            //t0.[RizCloudServerId] AS[RizCloudServerId]
            //FROM[Bas_Client] t0
            //INNER JOIN[Sys_CloudServer] t1 ON t0.[RizCloudServerId] = t1.[RizCloudServerId]
            //WHERE t0.[RizClientId] > 0


            // 更简单的赋值方式 
            // 适用场景：在显示列表时只想显示外键表的一两个字段
            query =
                from a in context.GetTable<RizModel.Client>()
                select new RizModel.Client(a)
                {
                    CloudServer = a.CloudServer,
                    LocalServer = new RizModel.CloudServer
                    {
                        RizCloudServerId = a.RizCloudServerId,
                        RizCloudServerCode = "LocalCode",
                        RizCloudServerName = a.LocalServer.RizCloudServerName,
                    }
                };
            result = query.ToList();
            result = query.OrderBy(a => a.RizClientCode).ToList();
            //SQL=>
            //SELECT
            //t0.[RizClientId] AS[RizClientId],
            //***
            //t1.[RizCloudServerId] AS[CloudServerId1],
            //t1.[RizCloudServerCode] AS[RizCloudServerCode],
            //t1.[RizCloudServerName] AS[RizCloudServerName],
            //CASE WHEN t1.[RizCloudServerId] IS NULL THEN NULL ELSE t1.[RizCloudServerId] END AS[NULL],
            //t0.[RizCloudServerId] AS[CloudServerId2],
            //t2.[RizCloudServerName] AS[CloudServerName1]
            //FROM[Bas_Client] t0
            //LEFT JOIN[Sys_CloudServer] t1 ON t0.[RizCloudServerId] = t1.[RizCloudServerId]
            //LEFT JOIN[Sys_CloudServer] t2 ON t0.[RizCloudServerId] = t2.[RizCloudServerId]

            // 1：1关系，1：n关系
            query =
                from a in context.GetTable<RizModel.Client>()
                where a.RizClientId > 0
                orderby a.RizClientId
                select new RizModel.Client(a)
                {
                    CloudServer = a.CloudServer,
                    Accounts = a.Accounts,
                    RizClientCode = a.RizClientCode
                };
            result = query.ToList();
            var single = query.FirstOrDefault();

            query = context.GetTable<RizModel.Client>()
                .Include(a => a.Accounts)
                .Include(a => a.Accounts[0].Markets2);
            result = query.ToList();

            //SQL=>
            //SELECT
            //t0.[RizClientId] AS[RizClientId],
            //***
            //t1.[RizCloudServerId] AS[CloudServerId1],
            //t1.[RizCloudServerCode] AS[RizCloudServerCode],
            //t1.[RizCloudServerName] AS[RizCloudServerName],
            //CASE WHEN t1.[RizCloudServerId] IS NULL THEN NULL ELSE t1.[RizCloudServerId]
            //        END AS[NULL],
            //t2.[RizClientId] AS[ClientId1],
            //t2.[RizAccountId] AS[RizAccountId],
            //t2.[RizAccountCode] AS[RizAccountCode],
            //t2.[RizAccountName] AS[RizAccountName],
            //CASE WHEN t2.[RizClientId] IS NULL THEN NULL ELSE t2.[RizClientId] END AS [NULL1]
            //FROM (
            //    SELECT
            //    t0.[RizClientId] AS[RizClientId],
            //    ***
            //    FROM [Bas_Client] t0
            //    WHERE t0.[RizClientId] > 0
            //) t0
            //LEFT JOIN[Sys_CloudServer] t1 ON t0.[RizCloudServerId] = t1.[RizCloudServerId]
            //LEFT JOIN [Bas_ClientAccount] t2 ON t0.[RizClientId] = t2.[RizClientId]
            //ORDER BY t0.[RizClientId]

            //// Include 语法
            query =
               context
               .GetTable<RizModel.Client>()
               .Include(a => a.CloudServer);
            result = query.ToList();
            query =
            context
            .GetTable<RizModel.Client>()
            .Include(a => a.CloudServer, a => new 
            {
                RizCloudServerId = a.CloudServer.RizCloudServerId,
                RizCloudServerCode = "5",
                RizCloudServerName = _demoName
            });
            result = query.ToList();
            query =
                context
                .GetTable<RizModel.Client>()
                .Include(a => a.CloudServer, a => new
                {
                    Ok = _demoName,
                    CloudServerId = "5",
                    CloudServerCode = a.CloudServer.RizCloudServerCode
                });
            result = query.ToList();
            query =
            context
            .GetTable<RizModel.Client>()
            .Include(a => a.CloudServer, a => new
            {
                a.CloudServer.RizCloudServerCode
            });
            result = query.ToList();
            query =
                context
                .GetTable<RizModel.Client>()
                .Include(a => a.CloudServer, a => new RizModel.CloudServer
                {
                    RizCloudServerId = a.RizCloudServerId,
                    RizCloudServerCode = a.CloudServer.RizCloudServerCode
                });
            result = query.ToList();
            query =
                context
                .GetTable<RizModel.Client>()
                .Include(a => a.CloudServer, a => new RizModel.CloudServer
                {
                    RizCloudServerId = a.RizCloudServerId,
                    RizCloudServerCode = a.CloudServer.RizCloudServerCode
                }, a => a.CloudServer.RizCloudServerCode.Contains("188") && true);
            result = query.ToList();
            query =
                context
                .GetTable<RizModel.Client>()
                .Include(a => a.CloudServer, a => new RizModel.CloudServer
                {
                    RizCloudServerId = a.RizCloudServerId,
                    RizCloudServerCode = a.CloudServer.RizCloudServerCode
                })
                .Include(a => a.Accounts, a => new RizModel.ClientAccount
                {
                    RizAccountId = a.Accounts[0].RizAccountId,
                    RizAccountCode = a.Accounts[0].RizAccountCode,
                });
            result = query.ToList();
            query =
                context
                .GetTable<RizModel.Client>()
                .Include(a => a.CloudServer, a => new RizModel.CloudServer
                {
                    RizCloudServerId = a.RizCloudServerId,
                    RizCloudServerCode = a.CloudServer.RizCloudServerCode
                })
                .Include(a => a.Accounts, a => new RizModel.ClientAccount
                {
                    RizAccountId = a.Accounts[0].RizAccountId,
                    RizAccountCode = a.Accounts[0].RizAccountCode,
                }, a => a.Accounts[0].RizAccountName.Contains("2"));
            result = query.ToList();

            query =
                context
                .GetTable<RizModel.Client>()
                .Include(a => a.CloudServer);
            query =
                from a in query
                join b in context.GetTable<RizModel.CloudServer>() on a.RizCloudServerId equals b.RizCloudServerId
                orderby a.RizClientId
                select new RizModel.Client(a)
                {
                    CloudServer = a.CloudServer
                };
            result = query.ToList();
            //SQL=>
            //SELECT 
            //t0.[RizClientId] AS [RizClientId],
            //***
            //t0.[RizRemark] AS [RizRemark],
            //t1.[RizCloudServerId] AS [CloudServerId1],
            //t1.[RizCloudServerCode] AS [RizCloudServerCode],
            //t1.[RizCloudServerName] AS [RizCloudServerName],
            //CASE WHEN t1.[RizCloudServerId] IS NULL THEN NULL ELSE t1.[RizCloudServerId] END AS [NULL],
            //t1.[RizCloudServerId] AS [CloudServerId2],
            //t1.[RizCloudServerCode] AS [CloudServerCode1],
            //t1.[RizCloudServerName] AS [CloudServerName1],
            //CASE WHEN t1.[RizCloudServerId] IS NULL THEN NULL ELSE t1.[RizCloudServerId] END AS [NULL1]
            //FROM [Bas_Client] t0 
            //INNER JOIN [Sys_CloudServer] t1 ON t0.[RizCloudServerId] = t1.[RizCloudServerId]
            //ORDER BY t0.[RizClientId]

            // 还是Include，无限主从孙 ### 
            query =
            from a in context
                .GetTable<RizModel.Client>()
                .Include(a => a.Accounts)
                //.Include(a => a.Accounts[0].Markets)
                .Include(a => a.Accounts[0].Markets[0].Client)
            where a.RizClientId > 0
            orderby a.RizClientId
            select a;
            result = query.ToList();
            query =
                from a in context
                    .GetTable<RizModel.Client>()
                    .Include(a => a.Accounts)
                    .Include(a => a.Accounts[0].Markets)
                    .Include(a => a.Accounts[0].Markets[0].Client)
                where a.RizClientId > 0
                orderby a.RizClientId, a.Accounts[0].Markets[0].RizMarketId
                select a;
            result = query.ToList();
            context.Database.ExecuteNonQuery(query.ToString());
            //SQL=>
            //SELECT
            //t0.[RizClientId] AS[RizClientId],
            //t0.[RizClientCode] AS[RizClientCode],
            //t0.[RizClientName] AS[RizClientName],
            //***
            //FROM (
            //    SELECT
            //    t0.[RizClientId] AS[RizClientId],
            //    ***
            //    FROM [Bas_Client] t0
            //    WHERE t0.[RizClientId] > 0
            //) t0
            //LEFT JOIN[Bas_ClientAccount] t1 ON t0.[RizClientId] = t1.[RizClientId]
            //LEFT JOIN [Bas_ClientAccountMarket] t2 ON t1.[RizClientId] = t2.[RizClientId] AND t1.[RizAccountId] = t2.[RizAccountId]
            //LEFT JOIN [Bas_Client] t3 ON t2.[RizClientId] = t3.[RizClientId]
            //ORDER BY t0.[RizClientId]

            // Include 分页
            query =
            from a in context
                .GetTable<RizModel.Client>()
                .Include(a => a.Accounts)
                .Include(a => a.Accounts[0].Markets)
                .Include(a => a.Accounts[0].Markets[0].Client)
            where a.RizClientId > 0
            orderby a.RizClientId
            select a;
            query = query
                .Where(a => a.RizClientId > 0 && a.CloudServer.RizCloudServerId > 0)
                .Skip(10)
                .Take(20);
            result = query.ToList();
            Debug.Assert(result.Count <= 20);
            // Include 分页
            query =
            from a in context
                .GetTable<RizModel.Client>()
                .Include(a => a.Accounts)
                .Include(a => a.Accounts[0].Markets)
                .Include(a => a.Accounts[0].Markets[0].Client)
            where a.RizClientId > 0
            orderby a.RizClientId, a.Accounts[0].RizAccountId descending, a.Accounts[0].Markets[0].RizMarketId, a.CloudServer.RizCloudServerId ascending
            select a;
            query = query
                .Where(a => a.RizClientId > 0 && a.CloudServer.RizCloudServerId > 0)
                .Skip(10)
                .Take(20);
            result = query.ToList();
            query =
            from a in context
                .GetTable<RizModel.Client>()
                .Include(a => a.Accounts)
                .Include(a => a.Accounts[0].Markets)
                .Include(a => a.Accounts[0].Markets[0].Client)
            where a.RizClientId > 0
            orderby a.Accounts[0].RizAccountId descending, a.Accounts[0].Markets[0].RizMarketId, a.CloudServer.RizCloudServerId ascending, a.RizClientId
            select a;
            query = query
                .Where(a => a.RizClientId > 0 && a.CloudServer.RizCloudServerId > 0)
                .Skip(10)
                .Take(20);
            result = query.ToList();
            context.Database.ExecuteNonQuery(query.ToString());
            //SQL=>
            //SELECT 
            //t0.[RizClientId] AS [RizClientId],
            //t0.[RizClientCode] AS [RizClientCode],
            //t0.[RizClientName] AS [RizClientName],
            //***
            //CASE WHEN t3.[RizClientId] IS NULL THEN NULL ELSE t3.[RizClientId] END AS [NULL2]
            //FROM (
            //    SELECT 
            //    t0.[RizClientId] AS [RizClientId],
            //    t0.[RizClientCode] AS [RizClientCode],
            //    t0.[RizClientName] AS [RizClientName],
            //    ***
            //    FROM [Bas_Client] t0 
            //    LEFT JOIN [Sys_CloudServer] t1 ON t0.[RizCloudServerId] = t1.[RizCloudServerId]
            //    WHERE t0.[RizClientId] > 0 AND t0.[RizClientId] > 0 AND t1.[RizCloudServerId] > 0
            //    ORDER BY t0.[RizClientId]
            //    OFFSET 10 ROWS FETCH NEXT 20 ROWS ONLY 
            //) t0 
            //LEFT JOIN [Bas_ClientAccount] t1 ON t0.[RizClientId] = t1.[RizClientId]
            //LEFT JOIN [Bas_ClientAccountMarket] t2 ON t1.[RizClientId] = t2.[RizClientId] AND t1.[RizAccountId] = t2.[RizAccountId]
            //LEFT JOIN [Bas_Client] t3 ON t2.[RizClientId] = t3.[RizClientId]

            query =
               from a in context
                   .GetTable<RizModel.Client>()
                   .Include(a => a.CloudServer)
                   .Include(a => a.Accounts)
               where a.RizClientId > 0
               select a;
            query = query.OrderBy(a => a.RizClientId);
            result = query.ToList();
            //SQL=>
            //SELECT 
            //t0.[RizClientId] AS [RizClientId],
            //t0.[RizClientCode] AS [RizClientCode],
            //t0.[RizClientName] AS [RizClientName],
            //***
            //FROM (
            //    SELECT 
            //    t0.[RizClientId] AS [RizClientId],
            //    t0.[RizClientCode] AS [RizClientCode],
            //    ***
            //    FROM [Bas_Client] t0 
            //    WHERE t0.[RizClientId] > 0
            //) t0 
            //LEFT JOIN [Sys_CloudServer] t1 ON t0.[RizCloudServerId] = t1.[RizCloudServerId]
            //LEFT JOIN [Bas_ClientAccount] t2 ON t0.[RizClientId] = t2.[RizClientId]
            //ORDER BY t0.[RizClientId]

            // Include 语法查询 主 从 孙 关系<注：相同的导航属性不能同时用include和join>
            var query1 =
                from a in
                    context
                    .GetTable<RizModel.Client>()
                    .Include(a => a.CloudServer)
                    .Include(a => a.Accounts)
                    .Include(a => a.Accounts[0].Markets)
                    .Include(a => a.Accounts[0].Markets[0].Client)
                group a.RizQty by new { RizClientId = a.RizClientId, a.RizClientCode, a.RizClientName, a.CloudServer.RizCloudServerId } into g
                select new RizModel.Client
                {
                    RizClientId = g.Key.RizClientId,
                    RizClientCode = g.Key.RizClientCode,
                    RizClientName = g.Key.RizClientName,
                    RizCloudServerId = g.Key.RizCloudServerId,
                    //Qty = g.Sum(a => a.Qty),
                    //State = (byte)(g.Count())
                    RizQty = g.Sum(a => a),
                };
            query1 = query1
                .Where(a => a.RizClientId > 0)
                .OrderBy(a => a.RizClientId)
                .Skip(10)
                .Take(20)
                ;
            var result1 = query1.ToList();
            context.Database.ExecuteNonQuery(query1.ToString());

            query1 =
                from a in
                    context
                    .GetTable<RizModel.Client>()
                    .Include(a => a.CloudServer)
                    .Include(a => a.Accounts)
                    .Include(a => a.Accounts[0].Markets)
                    .Include(a => a.Accounts[0].Markets[0].Client)
                group a by new { RizClientId = a.RizClientId, a.RizClientCode, a.RizClientName, a.CloudServer.RizCloudServerId } into g
                select new RizModel.Client
                {
                    RizClientId = g.Key.RizClientId,
                    RizClientCode = g.Key.RizClientCode,
                    RizClientName = g.Key.RizClientName,
                    RizCloudServerId = g.Key.RizCloudServerId,
                    RizQty = g.Sum(a => a.RizQty),
                    RizState = (byte)(g.Count())
                    //Qty = g.Sum(a => a),
                };
            result1 = query1.ToList();

            //SQL=>
            //SELECT 
            //t0.[RizClientId] AS [RizClientId],
            //t0.[RizClientCode] AS [RizClientCode],
            //t0.[RizClientName] AS [RizClientName],
            //***
            //FROM (
            //    SELECT 
            //    t0.[RizClientId] AS [RizClientId],
            //    t0.[RizClientCode] AS [RizClientCode],
            //    ***
            //    SUM(t0.[RizQty]) AS [RizQty]
            //    FROM [Bas_Client] t0 
            //    LEFT JOIN [Sys_CloudServer] t1 ON t0.[RizCloudServerId] = t1.[RizCloudServerId]
            //    GROUP BY t0.[RizClientId],t0.[RizClientCode],t0.[RizClientName],t1.[RizCloudServerId]
            //    Having t0.[RizClientId] > 0
            //    ORDER BY t0.[RizClientId]
            //    OFFSET 10 ROWS FETCH NEXT 20 ROWS ONLY 
            //) t0 
            //LEFT JOIN [Sys_CloudServer] t1 ON t0.[RizCloudServerId] = t1.[RizCloudServerId]
            //LEFT JOIN [Bas_ClientAccount] t2 ON t0.[RizClientId] = t2.[RizClientId]
            //LEFT JOIN [Bas_ClientAccountMarket] t3 ON t2.[RizClientId] = t3.[RizClientId] AND t2.[RizAccountId] = t3.[RizAccountId]
            //LEFT JOIN [Bas_Client] t4 ON t3.[RizClientId] = t4.[RizClientId]
            var max1 = query1.Max(a => a.RizQty);
            //SQL=>
            //SELECT 
            //MAX(t0.[RizQty])
            //FROM (
            //    SELECT 
            //    t0.[RizClientId] AS [RizClientId],
            //    t0.[RizClientCode] AS [RizClientCode],
            //    t0.[RizClientName] AS [RizClientName],
            //    ***
            //    FROM (
            //        SELECT 
            //        t0.[RizClientId] AS [RizClientId],
            //        ***
            //        t1.[RizCloudServerId] AS [RizCloudServerId],
            //        SUM(t0.[RizQty]) AS [RizQty]
            //        FROM [Bas_Client] t0 
            //        LEFT JOIN [Sys_CloudServer] t1 ON t0.[RizCloudServerId] = t1.[RizCloudServerId]
            //        GROUP BY t0.[RizClientId],t0.[RizClientCode],t0.[RizClientName],t1.[RizCloudServerId]
            //        Having t0.[RizClientId] > 0
            //        ORDER BY t0.[RizClientId]
            //        OFFSET 10 ROWS FETCH NEXT 20 ROWS ONLY 
            //    ) t0 
            //    LEFT JOIN [Sys_CloudServer] t1 ON t0.[RizCloudServerId] = t1.[RizCloudServerId]
            //    LEFT JOIN [Bas_ClientAccount] t2 ON t0.[RizClientId] = t2.[RizClientId]
            //    LEFT JOIN [Bas_ClientAccountMarket] t3 ON t2.[RizClientId] = t3.[RizClientId] AND t2.[RizAccountId] = t3.[RizAccountId]
            //    LEFT JOIN [Bas_Client] t4 ON t3.[RizClientId] = t4.[RizClientId]
            //) t0 

            // 分组后再统计
            var query2 =
                from a in context.GetTable<RizModel.Client>()
                group a by a.RizClientId into g
                select new
                {
                    RizClientId = g.Key,
                    RizQty = g.Sum(a => a.RizQty)
                };
            var result2 = query2.Max(a => a.RizClientId);
            //SQL=>
            //SELECT
            //MAX(t0.[RizClientId])
            //FROM(
            //    SELECT
            //    t0.[RizClientId] AS[RizClientId],
            //    FROM[Bas_Client] t0
            //    GROUP BY t0.[RizClientId]
            // ) t0
            var result9 = query2.Sum(a => a.RizQty);
            //SQL=>
            //SELECT 
            //SUM(t0.[RizQty])
            //FROM ( 
            //    SELECT 
            //    t0.[RizClientId] AS [RizClientId],
            //    SUM(t0.[RizQty]) AS [RizQty]
            //    FROM [Bas_Client] t0 
            //    GROUP BY t0.[RizClientId]
            // ) t0

            var query3 =
                from a in context.GetTable<RizModel.Client>()
                join b in context.GetTable<RizModel.ClientAccount>() on a.RizClientId equals b.RizClientId
                group new { ClientId2 = a.RizClientId, b.RizAccountId } by new { ClientId = a.RizClientId, b.RizAccountId } into g
                select new
                {
                    ClientId = g.Key.ClientId,
                    RizAccountId = g.Key.RizAccountId,
                    Max = g.Max(b => b.RizAccountId)
                };
            var result3 = query3.ToList();
            //SQL=>
            //SELECT
            //t0.[RizClientId] AS[RizClientId],
            //t1.[RizAccountId] AS[RizAccountId],
            //MAX(t1.[RizAccountId]) AS[Max]
            //FROM[Bas_Client] t0
            //INNER JOIN[Bas_ClientAccount] t1 ON t0.[RizClientId] = t1.[RizClientId]
            //GROUP BY t0.[RizClientId],t1.[RizAccountId]

            // CROSS JOIN
            var query4 =
                context
                .GetTable<RizModel.Client>()
                .Where(a => a.RizClientId <= 10)
                .SelectMany(a => context.GetTable<RizModel.Client>(), (a, b) => new
                {
                    ClientId = a.RizClientId,
                    ClientName = b.RizClientName
                });
            var result4 = query4.ToList();
            //SQL=>
            //SELECT
            //t0.[RizDemoId] AS[RizDemoId],
            //t1.[DemoName] AS[DemoName]
            //FROM[Sys_Demo] t0
            //CROSS JOIN[Sys_Demo] t1

            // LEFT JOIN
            query =
                  from a in context.GetTable<RizModel.Client>()
                  join b in context.GetTable<RizModel.CloudServer>() on a.RizCloudServerId equals b.RizCloudServerId into u_b
                  from b in u_b.DefaultIfEmpty()
                  select a;
            query = query.Where(a => a.CloudServer.RizCloudServerName != null);
            result = query.ToList();

            // LEFT JOIN
            query =
                  from a in context.GetTable<RizModel.Client>()
                  join b in context.GetTable<RizModel.CloudServer>() on new { a.RizCloudServerId, RizCloudServerCode = "567" } equals new { b.RizCloudServerId, b.RizCloudServerCode } into u_b
                  from b in u_b.DefaultIfEmpty()
                  select a;
            query = query.Where(a => a.CloudServer.RizCloudServerName != null);
            result = query.ToList();
            //SQL=>
            //SELECT
            //t0.[RizClientId] AS[RizClientId],
            //***
            //FROM[Bas_Client] t0
            //LEFT JOIN[Sys_CloudServer] t1 ON t0.[RizCloudServerId] = t1.[RizCloudServerId]
            //WHERE t1.[RizCloudServerName] IS NOT NULL

            // LEFT JOIN + CROSS JOIN
            query =
                 from a in context.GetTable<RizModel.Client>()
                 join b in context.GetTable<RizModel.CloudServer>() on a.RizCloudServerId equals b.RizCloudServerId into u_c
                 from b in u_c.DefaultIfEmpty()
                 select a;
            var query5 =
                query.SelectMany(c => context.GetTable<RizModel.CloudServer>(), (a, c) => new
                {
                    ClientId = a.RizClientId,
                    CloudServerName = a.CloudServer.RizCloudServerName,
                    CloudServerCode = c.RizCloudServerCode
                });
            var result5 = query5.ToList();
            context.Database.ExecuteNonQuery(query5.ToString());
            //SQL=>
            //SELECT
            //t0.[RizClientId] AS[RizClientId],
            //t1.[RizCloudServerName] AS[RizCloudServerName],
            //t2.[RizCloudServerCode] AS[RizCloudServerCode]
            //FROM[Bas_Client] t0
            //LEFT JOIN[Sys_CloudServer] t1 ON t0.[RizCloudServerId] = t1.[RizCloudServerId]
            //CROSS JOIN[Sys_CloudServer] t2

            // SQLite 不支持RIGHT OUTER JOIN
            if (_databaseType != DatabaseType.SQLite)
            {
                // RIGHT JOIN
                query =
                      from a in context.GetTable<RizModel.CloudServer>()
                      join b in context.GetTable<RizModel.Client>() on a.RizCloudServerId equals b.RizCloudServerId into u_b
                      from b in u_b.DefaultIfEmpty(true)
                      where a.RizCloudServerName == null
                      select b;
                result = query.ToList();
                //SQL=>
                //SELECT 
                //t1.[RizClientId] AS [RizClientId],
                //***
                //FROM [Sys_CloudServer] t0 
                //RIGHT JOIN [Bas_Client] t1 ON t0.[RizCloudServerId] = t1.[RizCloudServerId]
                //WHERE t0.[RizCloudServerName] IS NULL
            }

            // UNION 注意UNION分页的写法，仅支持写在最后
            var q1 = context.GetTable<RizModel.Client>().Where(x => x.RizClientId <= 10);
            var q2 = context.GetTable<RizModel.Client>().Where(x => x.RizClientId <= 10);
            var q3 = context.GetTable<RizModel.Client>().Where(x => x.RizClientId <= 10);
            var query6 = q1.Union(q2).Union(q3);
            var result6 = query6.ToList();
            result6 = query6.OrderBy(a => a.RizActiveDate).ToList();
            result6 = query6.Take(2).ToList();
            result6 = query6.OrderBy(a => a.RizClientId).Skip(2).ToList();
            query6 = query6.Take(2);
            result6 = query6.ToList();
            query6 = query6.OrderBy(a => a.RizClientId).Skip(1).Take(2);
            result6 = query6.ToList();
            //SQL=>
            //SELECT
            //t0.[RizClientId] AS[RizClientId],
            //t0.[RizClientCode] AS[RizClientCode],
            //t0.[RizClientName] AS[RizClientName],
            //...
            //FROM[Bas_Client] t0
            //WHERE t0.[RizClientId] = 1
            //UNION ALL
            //SELECT
            //t0.[RizClientId] AS[RizClientId],
            //t0.[RizClientCode] AS[RizClientCode],
            //t0.[RizClientName] AS[RizClientName],
            //...
            //FROM[Bas_Client] t0
            //WHERE t0.[RizClientId] = 1
            //UNION ALL
            //SELECT
            //t0.[RizClientId] AS[RizClientId],
            //t0.[RizClientCode] AS[RizClientCode],
            //t0.[RizClientName] AS[RizClientName],
            //...
            //FROM[Bas_Client] t0
            //WHERE t0.[RizClientId] = 1

            // UNION 注意UNION分页的写法，仅支持写在最后
            var q4 = context.GetTable<RizModel.Client>().Where(x => x.RizClientId <= 10).AsSubquery();//.OrderBy(x => x.RizClientName)
            var q5 = context.GetTable<RizModel.Client>().Where(x => x.RizClientId <= 10 && x.Accounts[0].RizAccountId != null).OrderBy(x => x.CloudServer.RizCloudServerId).Skip(5).AsSubquery();
            var q6 = context.GetTable<RizModel.Client>().Where(x => x.RizClientId <= 10 && x.Accounts[0].RizAccountId != null).OrderBy(x => x.CloudServer.RizCloudServerId).Skip(1).Take(2).AsSubquery();
            query6 = q4.Union(q5).Union(q6);
            result6 = query6.ToList();
            result6 = query6.Take(2).ToList();
            result6 = query6.OrderBy(a => a.RizClientId).Skip(2).ToList();
            query6 = query6.Take(2);
            result6 = query6.ToList();
            query6 = query6.OrderBy(a => a.RizClientId).Skip(1).Take(2);
            result6 = query6.ToList();

            // Any
            var isAny = context.GetTable<RizModel.Client>().Any();
            isAny = context.GetTable<RizModel.Client>().Any(a => a.RizActiveDate == DateTime.Now);
            isAny = context.GetTable<RizModel.Client>().Distinct().Any(a => a.RizActiveDate == DateTime.Now);
            isAny = context.GetTable<RizModel.Client>().OrderBy(a => a.RizClientId).Skip(2).Take(5).Any(a => a.RizActiveDate == DateTime.Now);
            //SQL=> 
            //IF EXISTS(
            //    SELECT TOP 1 1
            //    FROM[Bas_Client] t0
            //   WHERE t0.[RizActiveDate] = '2018-08-15 14:07:09.784'
            //) SELECT 1 ELSE SELECT 0

            // FirstOrDefault
            var f = context.GetTable<RizModel.Client>().FirstOrDefault();
            //SQL=> 
            //SELECT TOP(1)
            //t0.[RizClientId] AS[RizClientId],
            //t0.[RizClientCode] AS[RizClientCode],
            //t0.[RizClientName] AS[RizClientName],
            //t0.[State] AS[State],
            //t0.[RizActiveDate] AS[RizActiveDate],
            //t0.[RizCloudServerId] AS[RizCloudServerId]
            //FROM[Bas_Client] t0

            // Max,Count,Min,Avg,Sum
            var max = context.GetTable<RizModel.Client>().Where(a => a.RizClientId < -9).Max(a => a.RizClientId);
            //SQL=> 
            //SELECT
            //MAX(t0.[RizClientId])
            //FROM[Bas_Client] t0
            //WHERE t0.[RizClientId] < -9

            // GROUP BY
            var query7 =
                 from a in context.GetTable<RizModel.Client>()
                 where a.RizClientName == "TAN"
                 group a by new { RizClientId = a.RizClientId, a.RizClientName } into g
                 where g.Key.RizClientId > 0
                 orderby g.Key.RizClientName
                 select new
                 {
                     Id = g.Key.RizClientId,
                     Name = g.Min(a => a.RizClientId)
                 };
            var result7 = query7.ToList();
            // GROUP BY
            query7 =
                 from a in context.GetTable<RizModel.Client>()
                 where a.RizClientName == "TAN"
                 group a by new RizModel.Client { RizClientId = a.RizClientId, RizClientName = a.RizClientName } into g
                 where g.Key.RizClientId > 0
                 orderby g.Key.RizClientName
                 select new
                 {
                     Id = g.Key.RizClientId,
                     Name = g.Min(a => a.RizClientId)
                 };
            result7 = query7.ToList();
            //SQL=> 
            //SELECT
            //t0.[RizClientId] AS[Id],
            //MIN(t0.[RizClientId]) AS[Name]
            //FROM[Bas_Client] t0
            //WHERE t0.[RizClientName] = N'TAN'
            //GROUP BY t0.[RizClientId],t0.[RizClientName]
            //Having t0.[RizClientId] > 0
            //ORDER BY t0.[RizClientName]

            // 分组后再分页
            var query8 =
                 from a in context.GetTable<RizModel.Client>()
                 where a.RizClientName == "XFramework1"
                 group a by new { RizClientId = a.RizClientId, a.RizClientName } into g
                 where g.Key.RizClientId > 0
                 orderby new { g.Key.RizClientName, g.Key.RizClientId }
                 select new
                 {
                     Id = g.Key.RizClientId,
                     Name = g.Min(a => a.RizClientId)
                 };
            query8 = query8.Skip(2).Take(3);
            var result8 = query8.ToList();
            context.Database.ExecuteNonQuery(query8.ToString());
            //SQL=> 
            //SELECT
            //t0.[RizClientId] AS[Id],
            //MIN(t0.[RizClientId]) AS[Name]
            //FROM[Bas_Client] t0
            //WHERE t0.[RizClientName] = N'TAN'
            //GROUP BY t0.[RizClientId],t0.[RizClientName]
            //Having t0.[RizClientId] > 0
            //ORDER BY t0.[RizClientName],t0.[RizClientId]
            //OFFSET 2 ROWS FETCH NEXT 3 ROWS ONLY

            // DISTINCT 分组
            query =
                context
                .GetTable<RizModel.Client>()
                .Distinct()
                .Select(a => new RizModel.Client
                {
                    RizClientId = a.RizClientId,
                    RizClientName = a.RizClientName
                });
            var min = query.Min(a => a.RizClientId);
            //SQL=> 
            //SELECT
            //MIN(t0.[RizClientId])
            //FROM(
            //    SELECT DISTINCT
            //    t0.[RizClientId] AS[RizClientId],
            //    ***
            //    FROM[Bas_Client] t0
            // ) t0

            // 强制子查询
            query =
                  from a in context.GetTable<RizModel.Client>()
                  join b in context.GetTable<RizModel.CloudServer>() on a.RizCloudServerId equals b.RizCloudServerId into u_c
                  from b in u_c.DefaultIfEmpty()
                  select a;
            query = query.OrderBy(a => a.RizClientId).Skip(10).Take(10).AsSubquery();
            result = query.ToList();
            query = from a in query
                    join b in context.GetTable<RizModel.Client>() on a.RizClientId equals b.RizClientId
                    select a;
            result = query.ToList();
            context.Database.ExecuteNonQuery(query.ToString());

            var subQuery3 =
                from a in context.GetTable<RizModel.Client>()
                join b in context.GetTable<RizModel.ClientAccount>() on a.RizClientId equals b.RizClientId
                select new
                {
                    RizClientId = a.RizClientId,
                    RizClientName = a.RizClientName,
                    RizQty = a.RizQty
                };
            subQuery3.AsSubquery(a => new { a.RizQty }).ToList();

            //SQL=> 
            //SELECT 
            //t0.[RizClientId] AS [RizClientId],
            //t0.[RizClientCode] AS [RizClientCode],
            //***
            //FROM (
            //    SELECT 
            //    t0.[RizClientId] AS [RizClientId],
            //    t0.[RizClientCode] AS [RizClientCode],
            //    t0.[RizClientName] AS [RizClientName]
            //    ***
            //    FROM [Bas_Client] t0 
            //    LEFT JOIN [Sys_CloudServer] t1 ON t0.[RizCloudServerId] = t1.[RizCloudServerId]
            //    ORDER BY t0.[RizClientId]
            //    OFFSET 10 ROWS FETCH NEXT 10 ROWS ONLY 
            //) t0 
            //INNER JOIN [Bas_Client] t1 ON t0.[RizClientId] = t1.[RizClientId]

            var subQuery =
                from a in context.GetTable<RizModel.Client>()
                join b in context.GetTable<RizModel.ClientAccount>() on a.RizClientId equals b.RizClientId
                select new
                {
                    RizClientId = a.RizClientId,
                    RizClientName = a.RizClientName,
                    RizQty = a.RizQty
                };
            subQuery = subQuery.AsSubquery();

            query =
                from a in subQuery
                group a by a.RizClientId into g
                select new RizModel.Client
                {
                    RizClientId = g.Key,
                    RizClientName = g.Max(a => a.RizClientName),
                    RizQty = g.Sum(a => a.RizQty)
                };
            query = query.AsSubquery();
            query = query.Select(a => new RizModel.Client { RizClientId = a.RizClientId, RizClientName = a.RizClientName, RizQty = a.RizQty }).OrderBy(a => a.RizQty);
            result = query.ToList();
            context.Database.ExecuteNonQuery(query.ToString());
            //var result10 = query.ToPagedList(1, 20);
        }

        // 新增记录
        protected virtual void Insert()
        {
            Console.WriteLine("***** Insert *****");
            var context = _newContext();

            // 带自增列
            var demo = new TDemo
            {
                RizDemoCode = "C0000101",
                RizDemoName = "N0000101",
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
                RizDemoLong = 64
            };
            context.Insert(demo);
            context.SubmitChanges();

            var demo2 = new TDemo
            {
                RizDemoCode = "C0000102",
                RizDemoName = "N0000102",
                RizDemoBoolean = true,
                RizDemoChar = 'A',
                RizDemoNChar = 'B',
                RizDemoByte = 65,
                RizDemoDate = DateTime.Now,
                RizDemoDateTime = DateTime.Now,
                RizDemoDateTime2 = DateTime.Now,
                RizDemoDecimal = 65,
                RizDemoDouble = 65,
                RizDemoFloat = 65,
                RizDemoGuid = Guid.NewGuid(),
                RizDemoShort = 65,
                RizDemoInt = 65,
                RizDemoLong = 65
            };
            context.Insert(demo2);

            var demo3 = new TDemo
            {
                RizDemoCode = "C0000103",
                RizDemoName = "N0000103",
                RizDemoBoolean = true,
                RizDemoChar = 'A',
                RizDemoNChar = 'B',
                RizDemoByte = 66,
                RizDemoDate = DateTime.Now,
                RizDemoDateTime = DateTime.Now,
                RizDemoDateTime2 = DateTime.Now,
                RizDemoDecimal = 66,
                RizDemoDouble = 66,
                RizDemoFloat = 66,
                RizDemoGuid = Guid.NewGuid(),
                RizDemoShort = 66,
                RizDemoInt = 66,
                RizDemoLong = 66
            };
            context.Insert(demo2);
            context.Insert(demo3);
            context.SubmitChanges();


            // 适用场景：在新增/修改/删除数据的同时查出数据集合
            context.Insert(demo);
            context.Update<TDemo>(a => new TDemo
            {
                RizDemoCode = "C0000102"
            }, a => a.RizDemoId == demo.RizDemoId);
            var cQuery = context.GetTable<RizModel.Client>().Where(x => x.RizClientId <= 100);
            context.AddQuery(cQuery);
            context.Insert(demo2);
            context.Update<TDemo>(a => new TDemo
            {
                RizDemoCode = "C0000'102"
            }, a => a.RizDemoId == demo2.RizDemoId);
            context.Insert(demo3);
            List<RizModel.Client> result = null;
            context.SubmitChanges(out result);
            Debug.Assert(result.Count <= 100);

            context.Insert(demo);
            cQuery = context.GetTable<RizModel.Client>().Where(x => x.RizClientId <= 100);
            context.AddQuery(cQuery);
            context.Insert(demo2);
            context.Update<TDemo>(a => new TDemo
            {
                RizDemoCode = "C0000102"
            }, a => a.RizDemoId == demo2.RizDemoId);
            context.Update<TDemo>(a => new TDemo
            {
                RizDemoCode = "C0000'102"
            }, a => a.RizDemoId == demo3.RizDemoId);
            context.Insert(demo3);
            var cQuery2 = context.GetTable<TDemo>().Where(x => x.RizDemoId <= 20);
            context.AddQuery(cQuery2);
            var cQuery3 = context.GetTable<TDemo>().Where(x => x.RizDemoId > 100);
            context.AddQuery(cQuery3);
            List<RizModel.Client> result1 = null;
            List<TDemo> result2 = null;
            context.SubmitChanges(out result1, out result2);

            // 参数超过1000个，自动分批执行
            List<TDemo> demos = new List<TDemo>();
            for (var index = 0; index < 205; index++)
            {
                var demo4 = new TDemo
                {
                    RizDemoCode = "C0000205",
                    RizDemoName = "N0000205",
                    RizDemoBoolean = true,
                    RizDemoChar = 'A',
                    RizDemoNChar = 'B',
                    RizDemoByte = 65,
                    RizDemoDate = DateTime.Now,
                    RizDemoDateTime = DateTime.Now,
                    RizDemoDateTime2 = DateTime.Now,
                    RizDemoDecimal = 65,
                    RizDemoDouble = 65,
                    RizDemoFloat = 65,
                    RizDemoGuid = Guid.NewGuid(),
                    RizDemoShort = 65,
                    RizDemoInt = 65,
                    RizDemoLong = 65
                };
                demos.Add(demo4);
                if (index == 10)
                {
                    var query2 = context.GetTable<RizModel.Demo>().Where(x => x.RizDemoId < 100);
                    context.AddQuery(query2);
                }
                context.Insert(demo4);
            }
            context.SubmitChanges();

            // 指定ID，默认值支持
            int maxClientId = context.GetTable<RizModel.Client>().Max(x => x.RizClientId);
            context.Delete<RizModel.Client>(x => x.RizClientId > maxClientId);
            context.Delete<RizModel.ClientAccount>(x => x.RizClientId > maxClientId);
            context.Delete<RizModel.ClientAccountMarket>(x => x.RizClientId > maxClientId);
            context.SubmitChanges();

            RizModel.Client client = new RizModel.Client
            {
                RizClientId = maxClientId + 1,
                RizClientCode = "ABC",
                RizClientName = "啊啵呲",
                RizRemark = "在批处理、名称作用域和数据库上下文方面，sp_executesql 与 EXECUTE 的行为相同。",
                RizCloudServerId = 3,
                RizState = 1
            };
            context.Insert<RizModel.Client>(client);

            var account = new RizModel.ClientAccount
            {
                RizClientId = maxClientId + 1,
                RizAccountId = "1",
                RizAccountCode = "ABC+",
                RizAccountName = "ABC+",
                RizQty = 2
            };
            context.Insert(account);

            var market = new RizModel.ClientAccountMarket
            {
                RizClientId = maxClientId + 1,
                RizAccountId = "1",
                RizMarketId = 1,
                RizMarketCode = "ABC+",
                RizMarketName = "ABC+",
            };
            context.Insert(market);


            // Query 关联新增
            var query =
                from a in context.GetTable<RizModel.Client>()
                join b in context.GetTable<RizModel.CloudServer>() on a.RizCloudServerId equals b.RizCloudServerId
                where a.RizClientId <= 5
                select new RizModel.Client
                {
                    RizClientId = Data.DbFunction.RowNumber<int>(a.RizClientId) + (maxClientId + 2),
                    RizClientCode = "ABC2",
                    RizClientName = "啊啵呲2",
                    RizCloudServerId = 3,
                    RizState = 2,
                    RizActiveDate = DateTime.Now
                };
            context.Insert(query);
            context.SubmitChanges();

            // 子查询增
            var sum =
                from a in context.GetTable<RizModel.ClientAccount>()
                where a.RizClientId > 0
                group a by new { a.RizClientId } into g
                select new RizModel.Client
                {
                    RizClientId = g.Key.RizClientId,
                    RizQty = g.Sum(a => a.RizQty)
                };
            sum = sum.AsSubquery();

            maxClientId = context.GetTable<RizModel.Client>().Max(x => x.RizClientId);
            var nQuery =
                from a in sum
                join b in context.GetTable<RizModel.Client>() on a.RizClientId equals b.RizClientId into u_b
                from b in u_b.DefaultIfEmpty()
                where b.RizClientId == null
                select new RizModel.Client
                {
                    RizClientId = Data.DbFunction.RowNumber<int>(a.RizClientId) + (maxClientId + 1),
                    RizClientCode = "XFramework100+",
                    RizClientName = "XFramework100+",
                    RizCloudServerId = 3,
                    RizState = 3,
                    RizQty = a.RizQty,
                };
            context.Insert(nQuery);
            context.Database.ExecuteNonQuery(nQuery.ToString());

            // 批量增加
            // 产生 INSERT INTO VALUES(),(),()... 语法。注意这种批量增加的方法并不能给自增列自动赋值
            demos = new List<TDemo>();
            for (int i = 0; i < 1002; i++)
            {
                TDemo d = new TDemo
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
                    RizDemoLong = 64
                };
                demos.Add(d);
            }
            context.Insert<TDemo>(demos);
            context.SubmitChanges();
            ////SQL=>
            //INSERT INTO [Bas_Client]
            //([RizClientId],[RizClientCode],[RizClientName],[RizCloudServerId],[RizActiveDate],[RizQty],[State],[RizRemark])
            //VALUES
            //(2019,N'ABC',N'啊啵呲',11,NULL,0,1,'默认值')
            //INSERT INTO [Bas_Client]([RizClientId],[RizClientCode],[RizClientName],[RizCloudServerId],[State])
            //SELECT 
            //ROW_NUMBER() Over(Order By t0.[RizClientId]) + 2020 AS [RizClientId],
            //N'ABC2' AS [RizClientCode],
            //N'啊啵呲2' AS [RizClientName],
            //11 AS [RizCloudServerId],
            //1 AS [State]
            //FROM [Bas_Client] t0 
            //INNER JOIN [Sys_CloudServer] t1 ON t0.[RizCloudServerId] = t1.[RizCloudServerId]
            //WHERE t0.[RizClientId] <= 5
            //DELETE t0 FROM [Sys_Demo] t0 
            //WHERE t0.[RizDemoId] > 1000000
            //INSERT INTO[Sys_Demo]
            //([DemoCode],[DemoName],[DemoChar],[DemoChar_Nullable],[DemoByte],[DemoByte_Nullable],[DemoDateTime],[DemoDateTime_Nullable],[DemoDecimal],[DemoDecimal_Nullable],[DemoFloat],[DemoFloat_Nullable],[DemoReal],[Demo_Nullable],[DemoGuid],[DemoGuid_Nullable],[DemoShort],[DemoShort_Nullable],[DemoInt],[DemoInt_Nullable],[DemoLong],[DemoLong_Nullable])
            //VALUES(...),(),()...

            // 指定ID，无自增列批量增加
            maxClientId = context.GetTable<RizModel.Client>().Max(x => x.RizClientId);
            List<RizModel.Client> clients = new List<RizModel.Client>();
            for (int index = 0; index < 1002; index++)
            {
                maxClientId++;
                client = new RizModel.Client
                {
                    RizClientId = maxClientId,
                    RizClientCode = "XFramework1000+",
                    RizClientName = "XFramework1000+",
                    RizRemark = "在批处理、名称作用域和数据库上下文方面，sp_executesql 与 EXECUTE 的行为相同。",
                    RizCloudServerId = 3,
                    RizState = 1
                };
                clients.Add(client);

                for (var j = 1; j <= 2; j++)
                {
                    var account2 = new RizModel.ClientAccount
                    {
                        RizClientId = maxClientId,
                        RizAccountId = j.ToString(),
                        RizAccountCode = "XFrameworkAccount1000+",
                        RizAccountName = "XFrameworkAccount1000+",
                        RizQty = index + j
                    };
                    context.Insert(account2);

                    for (int m = 1; m <= 2; m++)
                    {
                        var market2 = new RizModel.ClientAccountMarket
                        {
                            RizClientId = maxClientId,
                            RizAccountId = j.ToString(),
                            RizMarketId = m,
                            RizMarketCode = "XFrameworkAccountMarket1000+",
                            RizMarketName = "XFrameworkAccountMarket1000+",
                        };
                        context.Insert(market2);
                    }
                }
            }
            context.Insert<RizModel.Client>(clients);
            context.SubmitChanges();
            Debug.Assert(context.GetTable<RizModel.Client>().Max(x => x.RizClientId) == maxClientId);


        }

        // 更新记录
        protected virtual void Update()
        {
            Console.WriteLine("***** Update *****");
            var context = _newContext();
            // 整个实体更新
            var demo = context.GetTable<TDemo>().FirstOrDefault(x => x.RizDemoId > 0);
            if (demo != null)
            {
                demo.RizDemoByte = demo.RizDemoByte >= 100 && demo.RizDemoByte < 126 ? (byte)(demo.RizDemoByte + 1) : (byte)(demo.RizDemoByte - 1);
                context.Update(demo);
                context.SubmitChanges();

                int @byte = demo.RizDemoByte;
                demo = context.GetTable<TDemo>().FirstOrDefault(x => x.RizDemoId == demo.RizDemoId);
                Debug.Assert(demo.RizDemoByte == @byte);
            }

            // 2.WHERE 条件批量更新
            context.Update<TDemo>(x => new TDemo
            {
                RizDemoDateTime2 = DateTime.UtcNow,
                RizDemoDateTime2_Nullable = null,
                RizDemoByte = x.RizDemoByte >= 100 && demo.RizDemoByte < 126 ? (byte)(x.RizDemoByte - 1) : (byte)(x.RizDemoByte + 1)
            }, x => x.RizDemoName == "N0000001" || x.RizDemoCode == "C0000001");
            context.SubmitChanges();

            // 3.Query 关联批量更新
            var query =
                from a in context.GetTable<RizModel.Client>()
                where a.CloudServer.RizCloudServerId > 0
                select a;
            context.Update<RizModel.Client>(a => new
            {
                Qty = -1,
                Remark = a.RizClientCode + "Re'mark"
            }, query);
            context.SubmitChanges();
            var result = context.GetTable<RizModel.Client>().Where(x => x.CloudServer.RizCloudServerId > 0).ToList();
            // 断言更新成功
            Debug.Assert(result.All(x => x.RizRemark == x.RizClientCode + "Re'mark"));
            Debug.Assert(result.All(x => x.RizQty == -1));
            //SQL=> 
            //UPDATE t0 SET
            //t0.[DemoCode] = 'Code0000004',
            //t0.[DemoName] = N'001''.N',
            //***
            //t0.[DemoLong] = 8192000000000,
            //t0.[DemoLong_Nullable] = 8192000000000
            //FROM [Sys_Demo] t0
            //WHERE t0.[RizDemoId] = 4
            //UPDATE t0 SET
            //t0.[DemoDateTime2] = '2019-04-13 15:19:59.758789',
            //t0.[DemoDateTime2_Nullable] = NULL
            //FROM [Sys_Demo] AS [t0]
            //WHERE t0.[RizDemoId] = 4
            //UPDATE t0 SET
            //t0.[DemoDateTime2] = '2019-04-13 15:19:59.758789',
            //t0.[DemoDateTime2_Nullable] = NULL
            //FROM [Sys_Demo] AS [t0]
            //WHERE (t0.[DemoName] = N'001''.N') OR (t0.[DemoCode] = '001''.N')
            //UPDATE t0 SET
            //t0.[RizRemark] = N'001.TAN'
            //FROM [Bas_Client] AS [t0]
            //LEFT JOIN [Sys_CloudServer] t1 ON t0.[RizCloudServerId] = t1.[RizCloudServerId]
            //WHERE t1.[RizCloudServerId] <> 0

            // 更新本表值等于从表的字段值
            query =
                from a in context.GetTable<RizModel.Client>()
                join b in context.GetTable<RizModel.CloudServer>() on a.RizCloudServerId equals b.RizCloudServerId
                join c in context.GetTable<RizModel.ClientAccount>() on a.RizClientId equals c.RizClientId
                where c.RizAccountId == "1"
                select a;
            context.Update<RizModel.Client, RizModel.CloudServer>((a, b) => new RizModel.Client
            {
                RizCloudServerId = b.RizCloudServerId
            }, query);

            // 更新本表值等于从表的字段值
            query =
                from a in context.GetTable<RizModel.Client>()
                join b in context.GetTable<RizModel.CloudServer>() on a.RizCloudServerId equals b.RizCloudServerId
                join c in context.GetTable<RizModel.ClientAccount>() on a.RizClientId equals c.RizClientId
                where c.RizAccountId == "1"
                select a;
            context.Update<RizModel.Client, RizModel.CloudServer, RizModel.ClientAccount>((a, b, c) => new
            {
                CloudServerId = b.RizCloudServerId,
                Qty = c.RizQty > 0 ? c.RizQty : 1,
            }, query);
            context.SubmitChanges();
            result = query.ToList();
            Debug.Assert(result.All(x => x.RizQty >= 1));

            //SQL=>
            //UPDATE t0 SET
            //t0.[RizCloudServerId] = t1.[RizCloudServerId],
            //t0.[RizRemark] = N'001.TAN'
            //FROM [Bas_Client] AS [t0]
            //INNER JOIN [Sys_CloudServer] t1 ON t0.[RizCloudServerId] = t1.[RizCloudServerId]
            //INNER JOIN [Bas_ClientAccount] t2 ON t0.[RizClientId] = t2.[RizClientId]
            //WHERE t2.[RizAccountId] = N'12'

            // 子查询更新
            var sum =
                from a in context.GetTable<RizModel.ClientAccount>()
                where a.RizClientId > 0
                group a by new { a.RizClientId } into g
                select new RizModel.Client
                {
                    RizClientId = g.Key.RizClientId,
                    RizQty = g.Sum(a => a.RizQty)
                };
            var uQuery =
               from a in context.GetTable<RizModel.Client>()
               join b in sum on a.RizClientId equals b.RizClientId
               where a.RizClientId > 0 && b.RizClientId > 0
               select a;
            context.Update<RizModel.Client, RizModel.Client>((a, b) => new RizModel.Client
            {
                RizQty = b.RizQty
            }, uQuery);
            //SQL =>
            //UPDATE t0 SET
            //t0.[RizQty] = t1.[RizQty]
            //FROM [Bas_Client] AS [t0]
            //INNER JOIN (
            //    SELECT 
            //    t0.[RizClientId] AS [RizClientId],
            //    SUM(t0.[RizQty]) AS [RizQty]
            //    FROM [Bas_ClientAccount] t0 
            //    WHERE t0.[RizClientId] > 0
            //    GROUP BY t0.[RizClientId]
            //) t1 ON t0.[RizClientId] = t1.[RizClientId]
            //WHERE t1.[RizClientId] > 0

            var client = context.GetTable<RizModel.Client>().FirstOrDefault();
            if (client != null) context.Update(client);
            // 一次性提交，里面自带事务
            context.SubmitChanges();
            // SQL=>
            //UPDATE t0 SET
            //t0.[RizQty] = t1.[RizQty]
            //FROM [Bas_Client] AS [t0]
            //INNER JOIN (
            //SELECT 
            //t0.[RizClientId] AS [RizClientId],
            //SUM(t0.[RizQty]) AS [RizQty]
            //FROM [Bas_ClientAccount] t0 
            //WHERE t0.[RizClientId] > 0
            //GROUP BY t0.[RizClientId]
            //) t1 ON t0.[RizClientId] = t1.[RizClientId]
            //WHERE t1.[RizClientId] > 0
            //UPDATE t0 SET
            //t0.[RizClientId] = 1,
            //t0.[RizClientCode] = N'XFramework1',
            //t0.[RizClientName] = N'XFramework1',
            //t0.[RizCloudServerId] = 3,
            //t0.[RizActiveDate] = '2019-04-13 22:31:27.323',
            //t0.[RizQty] = 0,
            //t0.[State] = 1,
            //t0.[RizRemark] = N'001.TAN'
            //FROM [Bas_Client] t0
            //WHERE t0.[RizClientId] = 1
        }

        // 删除记录
        protected virtual void Delete()
        {
            Console.WriteLine("***** Delete *****");
            var context = _newContext();

            // 1. 删除单个记录
            var demo = new TDemo { RizDemoId = 101 };
            context.Delete(demo);
            context.SubmitChanges();
            //SQL=> 
            //DELETE t0 FROM [Sys_Demo] t0 
            //WHERE t0.[RizDemoId] = 101
#if !net40
            demo = new TDemo { RizDemoId = 101 };
            context.Delete(demo);
            var rowCount = context.SubmitChangesAsync().Result;
#endif

            // 多主键删除
            var account = context.GetTable<RizModel.ClientAccount>().FirstOrDefault(x => x.RizClientId > 100);
            if (account != null)
            {
                context.Delete(account);
                context.SubmitChanges();
            }

            // 2.WHERE 条件批量删除
            context.Delete<TDemo>(a => a.RizDemoId == 101 || a.RizDemoId == 102 || a.RizDemoName == "N0000101");
            context.SubmitChanges();

            var qeury =
                context
                .GetTable<TDemo>()
                .Where(a => a.RizDemoId > 100);
            // 2.WHERE 条件批量删除
            context.Delete<TDemo>(qeury);
            context.SubmitChanges();
            Debug.Assert(context.GetTable<TDemo>().Count(a => a.RizDemoId > 100) == 0);

            // 3.Query 关联批量删除
            var query1 =
                context
                .GetTable<RizModel.Client>()
                .SelectMany(a => context.GetTable<RizModel.ClientAccount>(), (a, b) => a)
                .Where(a => a.RizClientId == 200);
            context.Delete<RizModel.Client>(query1);
            // 删除不完整的数据
            query1 =
                 from a in context.GetTable<RizModel.Client>()
                 join b in context.GetTable<RizModel.ClientAccount>() on a.RizClientId equals b.RizClientId into u_b
                 from b in u_b.DefaultIfEmpty()
                 join c in context.GetTable<RizModel.ClientAccountMarket>() on new { b.RizClientId, b.RizAccountId } equals new { c.RizClientId, c.RizAccountId } into u_c
                 from c in u_c.DefaultIfEmpty()
                 where a.RizClientId > 100 && (b.RizClientId == null || c.RizClientId == null)
                 select a;
            context.Delete<RizModel.Client>(query1);

            query1 =
                from a in context.GetTable<RizModel.Client>()
                join b in context.GetTable<RizModel.ClientAccount>() on a.RizClientId equals b.RizClientId
                join c in context.GetTable<RizModel.ClientAccountMarket>() on new { b.RizClientId, b.RizAccountId } equals new { c.RizClientId, c.RizAccountId }
                where c.RizClientId > 100 && c.RizAccountId == "1" && c.RizMarketId == 1
                select a;
            context.Delete<RizModel.Client>(query1);
            context.SubmitChanges();
            // 断言
            Debug.Assert(query1.Count() == 0);

            // 3.Query contains
            var query3 =
                from a in context.GetTable<RizModel.Client>()
                join b in context.GetTable<RizModel.ClientAccount>() on a.RizClientId equals b.RizClientId
                where a.RizClientId > 90 && a.CloudServer.RizCloudServerId >= 3 && a.LocalServer.RizCloudServerId >= 3
                select a.RizClientId;
            context.Delete<RizModel.Client>(a => query3.Contains(a.RizClientId));
            context.SubmitChanges();
            Debug.Assert(query3.Count() == 0);

            // 4.Query 关联批量删除
            var query4 =
                from a in context.GetTable<RizModel.Client>()
                where a.RizClientId > 80 && a.CloudServer.RizCloudServerId >= 3 && a.LocalServer.RizCloudServerId >= 3
                select a;
            context.Delete<RizModel.Client>(query4);
            context.SubmitChanges();
            Debug.Assert(query4.Count() == 0);

            // 5.子查询批量删除
            // 子查询更新
            var subquery =
                from a in context.GetTable<RizModel.ClientAccount>()
                where a.RizClientId > 70
                group a by new { a.RizClientId } into g
                select new RizModel.Client
                {
                    RizClientId = g.Key.RizClientId,
                    RizQty = g.Sum(a => a.RizQty)
                };
            var query5 =
                from a in context.GetTable<RizModel.Client>()
                join b in context.GetTable<RizModel.CloudServer>() on a.RizCloudServerId equals b.RizCloudServerId
                join c in context.GetTable<RizModel.CloudServer>() on a.RizCloudServerId equals c.RizCloudServerId
                join d in subquery on a.RizClientId equals d.RizClientId
                where a.RizClientId > 70 && a.RizCloudServerId > 0
                select a;
            context.Delete<RizModel.Client>(query5);
            context.SubmitChanges();
            Debug.Assert(query5.Count() == 0);

            // 一次性保存，uow ~~
            context.Delete<TDemo>(x => x.RizDemoId > 100);
            context.Delete<RizModel.Client>(x => x.RizClientId > 100);
            context.Delete<RizModel.ClientAccount>(x => x.RizClientId > 100);
            context.Delete<RizModel.ClientAccountMarket>(x => x.RizClientId > 100);

            // 提交的同时查出数据
            // 适用场景：批量导入数据
            // 1.先插入数据到表变量
            // 2.提交并查出当批数据
            // 3.或者将存储过程/脚本插在当前上下文一起执行

            context.AddQuery(subquery);
            // context.AddQuery('Exec #存储过程#');
            // context.AddQuery('#文本脚本#');
            List<RizModel.Client> result0 = null;
            context.SubmitChanges(out result0);
            //SQL=> 
            //DELETE t0 FROM [Sys_Demo] t0 
            //WHERE ((t0.[RizDemoId] = 2) OR (t0.[RizDemoId] = 3)) OR (t0.[DemoName] = N'N0000004')
            //DELETE t0 FROM [Sys_Demo] t0 
            //WHERE ((t0.[RizDemoId] = 2) OR (t0.[RizDemoId] = 3)) OR (t0.[DemoName] = N'N0000004')
            //DELETE t0 FROM [Bas_Client] t0 
            //INNER JOIN [Bas_ClientAccount] t1 ON t0.[RizClientId] = t1.[RizClientId]
            //INNER JOIN [Bas_ClientAccountMarket] t2 ON t1.[RizClientId] = t2.[RizClientId] AND t1.[RizAccountId] = t2.[RizAccountId]
            //WHERE t2.[RizClientId] = 5 AND t2.[RizAccountId] = N'1' AND t2.[RizMarketId] = 1
            //DELETE t0 FROM [Bas_Client] t0 
            //INNER JOIN [Bas_ClientAccount] t1 ON t0.[RizClientId] = t1.[RizClientId]
            //LEFT JOIN [Sys_CloudServer] t2 ON t0.[RizCloudServerId] = t2.[RizCloudServerId]
            //LEFT JOIN [Sys_CloudServer] t3 ON t0.[RizCloudServerId] = t3.[RizCloudServerId]
            //WHERE t2.[RizCloudServerId] = 20 AND t3.[RizCloudServerId] = 2
        }

        protected virtual void API()
        {
            Console.WriteLine("***** API *****");
            var context = _newContext();

            var any = context.GetTable<RizModel.Client>().Any();
            any = context.GetTable<RizModel.Client>().Any(x => x.RizClientCode.Contains("XF"));

            var count = context.GetTable<RizModel.Client>().Count();
            count = context.GetTable<RizModel.Client>().Count(x => x.RizClientCode.Contains("XF"));
#if !net40
            count = context.GetTable<RizModel.Client>().CountAsync().Result;
            count = context.GetTable<RizModel.Client>().CountAsync(x => x.RizClientCode.Contains("XF")).Result;
#endif

            var firstOrDefault = context.GetTable<RizModel.Client>().FirstOrDefault();
            firstOrDefault = context.GetTable<RizModel.Client>().FirstOrDefault(x => x.RizClientCode.Contains("XF"));
#if !net40
            firstOrDefault = context.GetTable<RizModel.Client>().FirstOrDefaultAsync(x => x.RizClientCode.Contains("XF")).Result;
#endif

            // 适用于需要产生NULL的场景
            var max = context.GetTable<RizModel.Client>().Where(a => a.RizClientId == -1).Max(a => (Nullable<int>)a.RizClientId);
            // 适用于不需要产生NULL的场景
            max = context.GetTable<RizModel.Client>().Where(a => a.RizClientCode.Contains("XF")).Max(a => a.RizClientId);
            // 不需要忽略空值
            max = context.GetTable<RizModel.Client>().Where(a => a.RizClientCode.Contains("XF")).Max(a => (Nullable<int>)a.RizClientId ?? 0);

            var min = context.GetTable<RizModel.Client>().Min(a => a.RizClientId);
            min = context.GetTable<RizModel.Client>().Where(a => a.RizClientCode.Contains("XF")).Min(a => a.RizClientId);

            var avg = context.GetTable<RizModel.Client>().Average(a => (double)a.RizQty);
            var avg2 = context.GetTable<RizModel.Client>().Average(a => (decimal)a.RizQty);
            avg = (double)context.GetTable<RizModel.Client>().Where(a => a.RizClientCode.Contains("XF")).Average(a => (float)a.RizClientId);

            var sum = context.GetTable<RizModel.Client>().Sum(a => (long)a.RizQty);
            sum = context.GetTable<RizModel.Client>().Where(a => a.RizClientCode.Contains("XF")).Sum(a => a.RizClientId);

            var toArray = context.GetTable<RizModel.Client>().ToArray();
            toArray = context.GetTable<RizModel.Client>().OrderBy(a => a.RizClientId).ToArray(2, 10);

            var dataTalbe = context.GetTable<RizModel.Client>().ToDataTable();
            var dataSet = context.GetTable<RizModel.Client>().ToDataSet();

            var cQuery = context.GetTable<RizModel.Client>().Where(x => x.RizClientId > 100);
            int rowCount = context.Database.ExecuteNonQuery(cQuery);

            cQuery = context.GetTable<RizModel.Client>().Where(x => x.RizClientId > 100);
            object obj = context.Database.ExecuteScalar(cQuery);

            context.Update<RizModel.Client>(x => new RizModel.Client
            {
                RizClientName = "蒙3"
            }, x => x.RizClientId == 3);
            var query =
                from a in context.GetTable<RizModel.Client>()
                where a.RizClientId == 1
                select 5;
            context.AddQuery(query);
            List<int> result1 = null;
            context.SubmitChanges(out result1);

            context.Update<RizModel.Client>(x => new RizModel.Client
            {
                RizClientName = "蒙4"
            }, x => x.RizClientId == 4);
            query =
                from a in context.GetTable<RizModel.Client>()
                where a.RizClientId == 1
                select 5;
            context.AddQuery(query);
            var query2 =
                from a in context.GetTable<RizModel.Client>()
                where a.RizClientId == 1
                select 6;
            context.AddQuery(query2);
            result1 = null;
            List<int> result2 = null;
            context.SubmitChanges(out result1, out result2);


            // 一性加载多个列表 ****
            var query3 =
               from a in context.GetTable<RizModel.Client>()
               where a.RizClientId >= 1 && a.RizClientId <= 10
               select 5;
            var query4 =
                from a in context.GetTable<RizModel.Client>()
                where a.RizClientId >= 1 && a.RizClientId <= 10
                select 6;
            var tuple = context.Database.Execute<int, int>(query3, query4);

            query3 =
               from a in context.GetTable<RizModel.Client>()
               where a.RizClientId >= 1 && a.RizClientId <= 10
               select 5;
            query4 =
                from a in context.GetTable<RizModel.Client>()
                where a.RizClientId >= 1 && a.RizClientId <= 10
                select 6;
            var query5 =
                 from a in context.GetTable<RizModel.Client>()
                 where a.RizClientId >= 1 && a.RizClientId <= 10
                 select 7;
            var tuple2 = context.Database.Execute<int, int, int>(query3, query4, query5);
#if !net40
            query3 =
               from a in context.GetTable<RizModel.Client>()
               where a.RizClientId >= 1 && a.RizClientId <= 10
               select 5;
            query4 =
                from a in context.GetTable<RizModel.Client>()
                where a.RizClientId >= 1 && a.RizClientId <= 10
                select 6;
            tuple = context.Database.ExecuteAsync<int, int>(query3, query4).Result;

            query3 =
               from a in context.GetTable<RizModel.Client>()
               where a.RizClientId >= 1 && a.RizClientId <= 10
               select 5;
            query4 =
                from a in context.GetTable<RizModel.Client>()
                where a.RizClientId >= 1 && a.RizClientId <= 10
                select 6;
            query5 =
                 from a in context.GetTable<RizModel.Client>()
                 where a.RizClientId >= 1 && a.RizClientId <= 10
                 select 6;
            tuple2 = context.Database.ExecuteAsync<int, int, int>(query3, query4, query4).Result;
#endif

            // 事务1. 上下文独立事务
            try
            {
                using (var transaction = context.Database.BeginTransaction())
                {
                    var result = context.GetTable<RizModel.Client>().FirstOrDefault(x => x.RizClientId <= 10);
                    context.Update<RizModel.Client>(x => new RizModel.Client
                    {
                        RizClientName = "事务1"
                    }, x => x.RizClientId == result.RizClientId);
                    context.SubmitChanges();
                    result = context.GetTable<RizModel.Client>().FirstOrDefault(x => x.RizClientId == result.RizClientId);

                    context.Update<RizModel.Client>(x => new RizModel.Client
                    {
                        RizClientName = "事务2"
                    }, x => x.RizClientId == result.RizClientId);
                    context.SubmitChanges();
                    result = context.GetTable<RizModel.Client>().FirstOrDefault(x => x.RizClientId == result.RizClientId);

                    //throw new Exception("假装异常");
                    //transaction.Rollback();
                    transaction.Commit();
                }
            }
            finally
            {
                // 开启事务后必需显式释放资源
                context.Dispose();
            }

            // 事务2. 使用其它的事务
            IDbTransaction transaction2 = null;
            IDbConnection connection = null;
            try
            {
                connection = context.Provider.DbProvider.CreateConnection();
                connection.ConnectionString = context.Database.ConnectionString;
                if (connection.State != ConnectionState.Open) connection.Open();
                transaction2 = connection.BeginTransaction();

                // 指定事务
                context.Database.Transaction = transaction2;

                var result = context.GetTable<RizModel.Client>().FirstOrDefault(x => x.RizClientId <= 10);
                context.Update<RizModel.Client>(x => new RizModel.Client
                {
                    RizClientName = "事务3"
                }, x => x.RizClientId == result.RizClientId);
                context.SubmitChanges();
                result = context.GetTable<RizModel.Client>().FirstOrDefault(x => x.RizClientId == result.RizClientId);

                context.Update<RizModel.Client>(x => new RizModel.Client
                {
                    RizClientName = "事务4"
                }, x => x.RizClientId == result.RizClientId);
                result = context.GetTable<RizModel.Client>().FirstOrDefault(x => x.RizClientId == result.RizClientId);

                if (!this.CaseSensitive)
                {
                    context.AddQuery(@"UPDATE Bas_Client SET ClientName = {0} WHERE ClientID={1}; UPDATE Bas_Client SET ClientName = {2} WHERE ClientID={3};",
                       "事务4", 4, "事务5", 5);
                }
                context.SubmitChanges();


                transaction2.Commit();
            }
            catch
            {
                if (transaction2 != null) transaction2.Rollback();
                throw;
            }
            finally
            {
                if (transaction2 != null) transaction2.Dispose();
                if (connection != null) connection.Close();
                if (connection != null) connection.Dispose();

                context.Dispose();
            }
        }

        // 性能测试
        protected virtual void Rabbit()
        {
            Console.WriteLine("***** Rabbit *****");
            Stopwatch stop = new Stopwatch();
            var context = _newContext();

            stop = new Stopwatch();
            stop.Start();
            for (int i = 0; i < 10; i++)
            {
                DateTime sDate = DateTime.Now;
                var result = context
                    .GetTable<RizModel.Rabbit>()
                    .ToList();

                //                string sql = @"
                //SELECT 
                //t0.[RizDemoId] AS [RizDemoId],
                //t0.[DemoCode] AS [DemoCode],
                //t0.[DemoName] AS [DemoName],
                //t0.[DemoBoolean] AS [DemoBoolean],
                //t0.[DemoBoolean_Nullable] AS [DemoBoolean_Nullable],
                //t0.[DemoChar] AS [DemoChar],
                //t0.[DemoChar_Nullable] AS [DemoChar_Nullable],
                //t0.[DemoByte] AS [DemoByte],
                //t0.[DemoByte_Nullable] AS [DemoByte_Nullable],
                //t0.[DemoDate] AS [DemoDate],
                //t0.[DemoDate_Nullable] AS [DemoDate_Nullable],
                //t0.[DemoDateTime] AS [DemoDateTime],
                //t0.[DemoDateTime_Nullable] AS [DemoDateTime_Nullable],
                //t0.[DemoDateTime2] AS [DemoDateTime2],
                //t0.[DemoDateTime2_Nullable] AS [DemoDateTime2_Nullable],
                //t0.[DemoDecimal] AS [DemoDecimal],
                //t0.[DemoDecimal_Nullable] AS [DemoDecimal_Nullable],
                //t0.[DemoDouble] AS [DemoDouble],
                //t0.[DemoDouble_Nullable] AS [DemoDouble_Nullable],
                //t0.[DemoFloat] AS [DemoFloat],
                //t0.[DemoFloat_Nullable] AS [DemoFloat_Nullable],
                //t0.[DemoGuid] AS [DemoGuid],
                //t0.[DemoGuid_Nullable] AS [DemoGuid_Nullable],
                //t0.[DemoShort] AS [DemoShort],
                //t0.[DemoShort_Nullable] AS [DemoShort_Nullable],
                //t0.[DemoInt] AS [DemoInt],
                //t0.[DemoInt_Nullable] AS [DemoInt_Nullable],
                //t0.[DemoLong] AS [DemoLong],
                //t0.[DemoLong_Nullable] AS [DemoLong_Nullable]
                //FROM [Sys_Rabbit] t0 
                //";
                //                List<RizModel.Rabbit> result = new List<RizModel.Rabbit>();
                //                var P_0 = context.Database.ExecuteReader(sql);
                //                List<RizModel.Rabbit> m2 = new List<RizModel.Rabbit>();
                //                while (P_0.Read())
                //                {
                //                    int index = 0;
                //                    RizModel.Rabbit rabbit = new RizModel.Rabbit();
                //                    object val = default(object);
                //                    try
                //                    {
                //                        RizModel.Rabbit rabbit2 = rabbit;
                //                        //index = 0;
                //                        //val = null;
                //                        rabbit2.RizDemoId = P_0.GetInt32(0);
                //                        //index = 1;
                //                        //val = null;
                //                        rabbit2.DemoCode = P_0.GetString(1);
                //                        //index = 2;
                //                        //val = null;
                //                        rabbit2.DemoName = P_0.GetString(2);
                //                        //index = 3;
                //                        //val = null;
                //                        rabbit2.DemoBoolean = P_0.GetBoolean(3);
                //                        //index = 4;
                //                        //val = null;
                //                        if (!P_0.IsDBNull(4))
                //                        {
                //                            rabbit2.DemoBoolean_Nullable = P_0.GetBoolean(4);
                //                        }
                //                        //index = 5;
                //                        //val = null;
                //                        rabbit2.DemoChar = P_0.GetString(5);
                //                        //index = 6;
                //                        //val = null;
                //                        if (!P_0.IsDBNull(6))
                //                        {
                //                            rabbit2.DemoChar_Nullable = P_0.GetString(6);
                //                        }
                //                        //index = 7;
                //                        //val = null;
                //                        rabbit2.DemoByte = P_0.GetByte(7);
                //                        //index = 8;
                //                        //val = null;
                //                        if (!P_0.IsDBNull(8))
                //                        {
                //                            rabbit2.DemoByte_Nullable = P_0.GetByte(8);
                //                        }
                //                        //index = 9;
                //                        //val = null;
                //                        rabbit2.DemoDate = P_0.GetDateTime(9);
                //                        //index = 10;
                //                        //val = null;
                //                        if (!P_0.IsDBNull(10))
                //                        {
                //                            rabbit2.DemoDate_Nullable = P_0.GetDateTime(10);
                //                        }
                //                        //index = 11;
                //                        //val = null;
                //                        rabbit2.DemoDateTime = P_0.GetDateTime(11);
                //                        //index = 12;
                //                        //val = null;
                //                        if (!P_0.IsDBNull(12))
                //                        {
                //                            rabbit2.DemoDateTime_Nullable = P_0.GetDateTime(12);
                //                        }
                //                        //index = 13;
                //                        //val = null;
                //                        rabbit2.DemoDateTime2 = P_0.GetDateTime(13);
                //                        //index = 14;
                //                        //val = null;
                //                        if (!P_0.IsDBNull(14))
                //                        {
                //                            rabbit2.DemoDateTime2_Nullable = P_0.GetDateTime(14);
                //                        }
                //                        //index = 15;
                //                        //val = null;
                //                        rabbit2.DemoDecimal = P_0.GetDecimal(15);
                //                        //index = 16;
                //                        //val = null;
                //                        if (!P_0.IsDBNull(16))
                //                        {
                //                            rabbit2.DemoDecimal_Nullable = P_0.GetDecimal(16);
                //                        }
                //                        //index = 17;
                //                        //val = null;
                //                        rabbit2.DemoDouble = P_0.GetDouble(17);
                //                        //index = 18;
                //                        //val = null;
                //                        if (!P_0.IsDBNull(18))
                //                        {
                //                            rabbit2.DemoDouble_Nullable = P_0.GetDouble(18);
                //                        }
                //                        //index = 19;
                //                        //val = null;
                //                        rabbit2.DemoFloat = P_0.GetFloat(19);
                //                        //index = 20;
                //                        //val = null;
                //                        if (!P_0.IsDBNull(20))
                //                        {
                //                            rabbit2.DemoFloat_Nullable = P_0.GetFloat(20);
                //                        }
                //                        //index = 21;
                //                        //val = null;
                //                        rabbit2.DemoGuid = P_0.GetGuid(21);
                //                        //index = 22;
                //                        //val = null;
                //                        if (!P_0.IsDBNull(22))
                //                        {
                //                            rabbit2.DemoGuid_Nullable = P_0.GetGuid(22);
                //                        }
                //                        //index = 23;
                //                        //val = null;
                //                        rabbit2.DemoShort = P_0.GetInt16(23);
                //                        //index = 24;
                //                        //val = null;
                //                        if (!P_0.IsDBNull(24))
                //                        {
                //                            rabbit2.DemoShort_Nullable = P_0.GetInt16(24);
                //                        }
                //                        //index = 25;
                //                        //val = null;
                //                        rabbit2.DemoInt = P_0.GetInt32(25);
                //                        //index = 26;
                //                        //val = null;
                //                        if (!P_0.IsDBNull(26))
                //                        {
                //                            rabbit2.DemoInt_Nullable = P_0.GetInt32(26);
                //                        }
                //                        //index = 27;
                //                        //val = null;
                //                        rabbit2.DemoLong = P_0.GetInt64(27);
                //                        //index = 28;
                //                        //val = null;
                //                        if (!P_0.IsDBNull(28))
                //                        {
                //                            rabbit2.DemoLong_Nullable = P_0.GetInt64(28);
                //                        }
                //                        rabbit = rabbit2;
                //                        //return rabbit;
                //                        result.Add(rabbit);
                //                    }
                //                    catch (Exception ex)
                //                    {
                //                        //TypeDeserializerImpl.ThrowDataException(ex, index, val, P_0);
                //                        //return rabbit;
                //                    }
                //                }
                //P_0.Dispose();
                Console.WriteLine(string.Format("第 {0} 次，用时：{1}", (i + 1), (DateTime.Now - sDate).TotalMilliseconds / 1000.0));

                // 100w 数据量明显，清掉后内存会及时释放
                result.Clear();
                result = null;
                //reader.Close();
            }

            stop.Stop();
            Console.WriteLine(string.Format("运行 10 次 100w 行单表数据，用时：{0}", stop.Elapsed));
            //Console.ReadLine();

            stop = new Stopwatch();
            stop.Start();
            for (int i = 0; i < 100; i++)
            {
                DateTime sDate = DateTime.Now;
                var result = context
                    .GetTable<RizModel.Client>()
                    .Include(a => a.Accounts)
                    .ToList();
            }
            stop.Stop();
            Console.WriteLine(string.Format("运行 100 次 2000 行主从数据，用时：{0}", stop.Elapsed));

            stop = new Stopwatch();
            stop.Start();
            for (int i = 0; i < 100; i++)
            {
                DateTime sDate = DateTime.Now;
                var result = context
                    .GetTable<RizModel.Client>()
                    .Include(a => a.Accounts)
                    .Include(a => a.Accounts[0].Markets)
                    .ToList();
            }
            stop.Stop();
            Console.WriteLine(string.Format("运行 100 次 2000 行主从孙数据，用时：{0}", stop.Elapsed));
            //Console.ReadLine();
        }

        /// <summary>
        /// 有参构造函数查询
        /// </summary>
        protected virtual void Parameterized()
        {

        }
    }
}
