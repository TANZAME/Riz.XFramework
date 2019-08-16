using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

using TZM.XFramework.Data;
using System.Data;

namespace TZM.XFramework.UnitTest
{

    public class TestBase<TDemo> : ITest where TDemo : Model.Demo, new()
    {
        private string _demoName = "002F";
        private int[] _demoIdList = new int[] { 2, 3 };
        private DatabaseType _databaseType = DatabaseType.None;
        // 参数化查询语句数量@@
        protected Func<IDbContext> _newContext = null;

        public TestBase()
        {
            _newContext = this.CreateDbContext;
        }

        public virtual IDbContext CreateDbContext()
        {
            return null;
        }

        public virtual void Run(DatabaseType dbType)
        {
            _databaseType = dbType;
            Query();
            Join();
            Delete();
            Update();
            Insert();
            API();
            Rabbit();
        }

        // 单表查询
        void Query()
        {
            var context = _newContext();

            // 查询表达式 <注：Date,DateTime,DateTime2的支持>
            DateTime sDate = DateTime.Now.AddYears(-9);
            Nullable<DateTime> sDate_null = new Nullable<DateTime>(sDate);

            //// 匿名类
            var guid = Guid.NewGuid();
            var dynamicQuery =
                from a in context.GetTable<TDemo>()
                where a.DemoId <= 10
                select new
                {
                    DemoId = 12,
                    DemoCode = a.DemoCode,
                    DemoName = a.DemoName,
                    DemoDateTime_Nullable = a.DemoDateTime_Nullable,
                    DemoDate = sDate,
                    DemoDateTime = sDate,
                    DemoDateTime2 = sDate_null,
                    DemoGuid = guid,
                    DemoEnum = Model.State.Complete,
                    DemoEnum2 = Model.State.Executing,
                };
            var result0 = dynamicQuery.ToList();
            //SQL=>
            //SELECT 
            //12 AS [DemoId],
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
            //WHERE t0.[DemoId] <= 10
            // 点标记
            dynamicQuery = context
                .GetTable<TDemo>()
                .Where(a => a.DemoId <= 10)
                .Select(a => new
                {
                    DemoId = 13,
                    DemoCode = a.DemoCode,
                    DemoName = a.DemoName,
                    DemoDateTime_Nullable = a.DemoDateTime_Nullable,
                    DemoDate = sDate,
                    DemoDateTime = sDate,
                    DemoDateTime2 = sDate_null,
                    DemoGuid = Guid.NewGuid(),
                    DemoEnum = Model.State.Complete,
                    DemoEnum2 = Model.State.Executing
                });
            result0 = dynamicQuery.ToList();
#if !net40
            result0 = dynamicQuery.ToListAsync().Result;
#endif
            //SQL=>
            //SELECT 
            //13 AS [DemoId],
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
            //WHERE t0.[DemoId] <= 10

            var result5 = context.GetTable<TDemo>().Select<TDemo, dynamic>().ToList();
            result5 = context.Database.ExecuteList<dynamic>("SELECT * FROM Sys_Demo");

            // Date,DateTime,DateTime2 支持
            var query =
                from a in context.GetTable<TDemo>()
                where a.DemoId <= 10 && a.DemoDate > sDate && a.DemoDateTime >= sDate && a.DemoDateTime2 > sDate
                select a;
            var result1 = query.ToList();
            // 点标记
            query = context
                .GetTable<TDemo>()
                .Where(a => a.DemoId <= 10 && a.DemoDate > sDate && a.DemoDateTime >= sDate && a.DemoDateTime2 > sDate);
            result1 = query.ToList();
#if !net40
            result1 = query.ToListAsync().Result;
#endif
            //SQL=> 
            //SELECT 
            //t0.[DemoId] AS [DemoId],
            //t0.[DemoCode] AS [DemoCode],
            //t0.[DemoName] AS [DemoName],
            //****
            //FROM [Sys_Demo] t0 
            //WHERE t0.[DemoId] > 0 AND t0.[DemoDate] > '2010-04-03' AND t0.[DemoDateTime] >= '2010-04-03 17:12:03.378' AND t0.[DemoDateTime2] > '2010-04-03 17:12:03.378697'

            // 指定字段
            query = from a in context.GetTable<TDemo>()
                    where a.DemoId <= 10
                    select new TDemo
                    {
                        DemoId = (int)a.DemoId,
                        DemoCode = (a.DemoCode ?? "N001"),
                        DemoName = a.DemoId.ToString(),
                        DemoDateTime_Nullable = a.DemoDateTime_Nullable,
                        DemoDate = sDate,
                        DemoDateTime = sDate,
                        DemoDateTime2 = sDate
                    };
            result1 = query.ToList();
            // 点标记
            query = context
                .GetTable<TDemo>()
                .Where(a => a.DemoCode != a.DemoId.ToString() && a.DemoName != a.DemoId.ToString() && a.DemoChar == 'A' && a.DemoNChar == 'B')
                .Select(a => new TDemo
                {
                    DemoId = a.DemoId,
                    DemoCode = a.DemoName == "张三" ? "李四" : "王五",
                    DemoName = a.DemoCode == "张三" ? "李四" : "王五",
                    DemoChar = 'A',
                    DemoNChar = 'B',
                    DemoDateTime_Nullable = a.DemoDateTime_Nullable,
                    DemoDate = sDate,
                    DemoDateTime = sDate,
                    DemoDateTime2 = sDate
                });
            result1 = query.ToList();
            //SQL=> 
            //SELECT 
            //t0.[DemoId] AS [DemoId],
            //ISNULL(t0.[DemoCode],N'N001') AS [DemoCode],
            //CAST(t0.[DemoId] AS NVARCHAR(max)) AS [DemoName],
            //t0.[DemoDateTime_Nullable] AS [DemoDateTime_Nullable],
            //'2010-04-13' AS [DemoDate],
            //'2010-04-13 22:50:38.827' AS [DemoDateTime],
            //'2010-04-13 22:50:38.827401' AS [DemoDateTime2]
            //FROM [Sys_Demo] t0 
            //WHERE t0.[DemoId] <= 10

            var linq = context.GetTable<TDemo>().Select(e => e.DemoName);
            query = context.GetTable<TDemo>().Where(a => a.DemoId <= 100 && linq.Contains(a.DemoName));
            result1 = query.ToList();

            linq = context.GetTable<TDemo>().Where(a => a.DemoBoolean && a.DemoByte != 2).Select(a => a.DemoName);
            query = context.GetTable<TDemo>().Where(a => a.DemoId <= 100 && linq.Contains(a.DemoName));
            result1 = query.ToList();

            // 带参数构造函数
            QueryWithParameterizedConstructor();
            //query =
            //     from a in context.GetTable<TDemo>()
            //     where a.DemoId <= 10
            //     select new TDemo(a);   
            //r1 = query.ToList();
            //query =
            //   from a in context.GetTable<TDemo>()
            //   where a.DemoId <= 10
            //   select new TDemo(a.DemoId, a.DemoName);
            //r1 = query.ToList();
            //SQL=> 
            //SELECT 
            //t0.[DemoId] AS [DemoId],
            //t0.[DemoName] AS [DemoName]
            //FROM [Sys_Demo] t0 


            //分页查询（非微软api）
            query = from a in context.GetTable<TDemo>()
                    select a;
            var result2 = query.ToPagedList(1, 20);
#if !net40
            result2 = query.ToPagedListAsync(1, 20).Result;
#endif
            //SQL=>
            //SELECT TOP(20)
            //t0.[DemoId] AS [DemoId],
            //t0.[DemoCode] AS [DemoCode],
            //t0.[DemoName] AS [DemoName],
            //...
            //t0.[DemoLong_Nullable] AS [DemoLong_Nullable]
            //FROM [Sys_Demo] t0 

            // 如果页码大于1，必须指定 OrderBy ###
            query = context.GetTable<TDemo>();
            result2 = query.OrderBy(a => a.DemoDecimal).ToPagedList(2, 1);
            //SQL=>
            //SELECT
            //t0.[DemoId] AS[DemoId],
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
                    orderby a.DemoCode
                    select a;
            query = query.Skip(1).Take(18);
            result1 = query.ToList();
            // 点标记
            query = context
                .GetTable<TDemo>()
                .OrderBy(a => a.DemoCode)
                .Skip(1)
                .Take(18);
            result1 = query.ToList();
            //SQL=>
            //SELECT 
            //t0.[DemoId] AS [DemoId],
            //t0.[DemoCode] AS [DemoCode],
            //t0.[DemoName] AS [DemoName],
            //...
            //t0.[DemoLong_Nullable] AS [DemoLong_Nullable]
            //FROM [Sys_Demo] t0 
            //ORDER BY t0.[DemoCode]
            //OFFSET 1 ROWS FETCH NEXT 18 ROWS ONLY

            // Mysql 不支持 limit n,-1 语法 ###
            query =
                from a in context.GetTable<TDemo>()
                where a.DemoId <= 10
                orderby a.DemoCode
                select a;
            query = query.Skip(1);
            result1 = query.ToList();
            //SQL=>
            //SELECT 
            //t0.[DemoId] AS [DemoId],
            //t0.[DemoCode] AS [DemoCode],
            //t0.[DemoName] AS [DemoName],
            //...
            //t0.[DemoLong_Nullable] AS [DemoLong_Nullable]
            //FROM [Sys_Demo] t0 
            //ORDER BY t0.[DemoCode]
            //OFFSET 1 ROWS

            query =
                from a in context.GetTable<TDemo>()
                orderby a.DemoCode
                select a;
            query = query.Take(1);
            result1 = query.ToList();
            //SQL=>
            //SELECT TOP(1)
            //t0.[DemoId] AS [DemoId],
            //t0.[DemoCode] AS [DemoCode],
            //t0.[DemoName] AS [DemoName],
            //...
            //t0.[DemoLong_Nullable] AS [DemoLong_Nullable]
            //FROM [Sys_Demo] t0 
            //ORDER BY t0.[DemoCode]

            // 分页后查查询，结果会产生嵌套查询
            query =
                from a in context.GetTable<TDemo>()
                orderby a.DemoCode
                select a;
            query = query.Skip(1);
            query = query.Where(a => a.DemoId <= 10);
            query = query.OrderBy(a => a.DemoCode).Skip(1).Take(1);
            result1 = query.ToList();
            //SQL=>
            //SELECT 
            //t0.[DemoId] AS [DemoId],
            //t0.[DemoCode] AS [DemoCode],
            //t0.[DemoName] AS [DemoName],
            //...
            //t0.[DemoLong_Nullable] AS [DemoLong_Nullable]
            //FROM (
            //    SELECT 
            //    t0.[DemoId] AS [DemoId],
            //    t0.[DemoCode] AS [DemoCode],
            //    t0.[DemoName] AS [DemoName],
            //    ...
            //    t0.[DemoLong_Nullable] AS [DemoLong_Nullable]
            //    FROM [Sys_Demo] t0 
            //    ORDER BY t0.[DemoCode]
            //    OFFSET 1 ROWS
            //) t0 
            //WHERE t0.[DemoId] > 0
            //ORDER BY t0.[DemoCode]
            //OFFSET 1 ROWS FETCH NEXT 1 ROWS ONLY 

            // 过滤条件
            query = from a in context.GetTable<TDemo>()
                    where a.DemoName == "D0000002" || a.DemoCode == "D0000002" && a.DemoByte_Nullable.Value > 0
                    select a;
            result1 = query.ToList();
            // 点标记
            query = context.GetTable<TDemo>().Where(a => a.DemoName == "D0000002" || a.DemoCode == "D0000002");
            result1 = query.ToList();
            query = context.GetTable<TDemo>().Where(a => a.DemoName.Contains("004"));
            result1 = query.ToList();
            query = context.GetTable<TDemo>().Where(a => a.DemoCode.StartsWith("Code000036"));
            result1 = query.ToList();
            query = context.GetTable<TDemo>().Where(a => a.DemoCode.EndsWith("004"));
            result1 = query.ToList();
            //SQL=>
            //SELECT 
            //t0.[DemoId] AS [DemoId],
            //t0.[DemoCode] AS [DemoCode],
            //t0.[DemoName] AS [DemoName],
            //...
            //t0.[DemoLong_Nullable] AS [DemoLong_Nullable]
            //FROM [Sys_Demo] t0 
            //WHERE (t0.[DemoName] = N'D0000002') OR (t0.[DemoCode] = 'D0000002')

            // 支持的查询条件
            // 区分 nvarchar,varchar,date,datetime,datetime2 字段类型
            // 支持的字符串操作=> Trim | TrimStart | TrimEnd | ToString | Length
            int m_byte = 9;
            Model.State state = Model.State.Complete;
            query = from a in context.GetTable<TDemo>()
                    where
                        a.DemoCode == "002" &&
                        a.DemoName == "002" &&
                        a.DemoCode.Contains("TAN") &&                                   // LIKE '%%'
                        a.DemoName.Contains("TAN") &&                                   // LIKE '%%'
                        a.DemoCode.StartsWith("TAN") &&                                 // LIKE 'K%'
                        a.DemoCode.EndsWith("TAN") &&                                   // LIKE '%K'
                        a.DemoCode.Length == 12 &&                                      // LENGTH
                        a.DemoCode.TrimStart() == "TF" &&
                        a.DemoCode.TrimEnd() == "TF" &&
                        a.DemoCode.TrimEnd() == "TF" &&
                        a.DemoCode.Substring(0) == "TF" &&
                        a.DemoDate == DateTime.Now &&
                        a.DemoDateTime == DateTime.Now &&
                        a.DemoDateTime2 == DateTime.Now &&
                        a.DemoName == (
                            a.DemoDateTime_Nullable == null ? "NULL" : "NOT NULL") &&   // 三元表达式
                        a.DemoName == (a.DemoName ?? a.DemoCode) &&                     // 二元表达式
                        new[] { 1, 2, 3 }.Contains(a.DemoId) &&                         // IN(1,2,3)
                        new List<int> { 1, 2, 3 }.Contains(a.DemoId) &&                 // IN(1,2,3)
                        new List<int>(_demoIdList).Contains(a.DemoId) &&                // IN(1,2,3)
                        a.DemoId == new List<int> { 1, 2, 3 }[0] &&                     // IN(1,2,3)
                        _demoIdList.Contains(a.DemoId) &&                          // IN(1,2,3)
                        a.DemoName == _demoName &&
                        a.DemoCode == (a.DemoCode ?? "CODE") &&
                        new List<string> { "A", "B", "C" }.Contains(a.DemoCode) &&
                        a.DemoByte == (byte)m_byte &&
                        a.DemoByte == (byte)Model.State.Complete ||
                        a.DemoInt == (int)Model.State.Complete ||
                        a.DemoInt == (int)state ||
                        (a.DemoName == "STATE" && a.DemoName == "REMARK")// OR 查询
                    select a;
            result1 = query.ToList();
            // 点标记
            query = context.GetTable<TDemo>().Where(a =>
                        a.DemoCode == "002" &&
                        a.DemoName == "002" &&
                        a.DemoCode.Contains("TAN") &&                                   // LIKE '%%'
                        a.DemoName.Contains("TAN") &&                                   // LIKE '%%'
                        a.DemoCode.StartsWith("TAN") &&                                 // LIKE 'K%'
                        a.DemoCode.EndsWith("TAN") &&                                   // LIKE '%K'
                        a.DemoCode.Length == 12 &&                                      // LENGTH
                        a.DemoCode.TrimStart() == "TF" &&
                        a.DemoCode.TrimEnd() == "TF" &&
                        a.DemoCode.TrimEnd() == "TF" &&
                        a.DemoCode.Substring(0) == "TF" &&
                        a.DemoDate == DateTime.Now &&
                        a.DemoDateTime == DateTime.Now &&
                        a.DemoDateTime2 == DateTime.Now &&
                        a.DemoName == (
                            a.DemoDateTime_Nullable == null ? "NULL" : "NOT NULL") &&   // 三元表达式
                        a.DemoName == (a.DemoName ?? a.DemoCode) &&                     // 二元表达式
                        new[] { 1, 2, 3 }.Contains(a.DemoId) &&                         // IN(1,2,3)
                        new List<int> { 1, 2, 3 }.Contains(a.DemoId) &&                 // IN(1,2,3)
                        a.DemoId == new List<int> { 1, 2, 3 }[0] &&                     // IN(1,2,3)
                        _demoIdList.Contains(a.DemoId) &&                          // IN(1,2,3)
                        a.DemoName == _demoName &&
                        a.DemoByte == (byte)m_byte &&
                        a.DemoByte == (byte)Model.State.Complete ||
                        a.DemoInt == (int)Model.State.Complete ||
                        a.DemoInt == (int)state ||
                        (a.DemoName == "STATE" && a.DemoName == "REMARK")               // OR 查询
                );
            //SQL=>            
            //SELECT
            //t0.[DemoId] AS[DemoId],
            //t0.[DemoCode] AS[DemoCode],
            //t0.[DemoName] AS[DemoName],
            //...
            //FROM[Sys_Demo] t0
            //WHERE (((t0.[DemoCode] = '002' AND t0.[DemoName] = N'002' AND t0.[DemoCode] LIKE '%TAN%' AND t0.[DemoName] LIKE N'%TAN%' AND 
            //t0.[DemoCode] LIKE 'TAN%' AND t0.[DemoCode] LIKE '%TAN' AND LEN(t0.[DemoCode]) = 12 AND LTRIM(t0.[DemoCode]) = 'TF' AND 
            //RTRIM(t0.[DemoCode]) = 'TF' AND RTRIM(t0.[DemoCode]) = 'TF' AND SUBSTRING(t0.[DemoCode],0 + 1,LEN(t0.[DemoCode])) = 'TF' AND 
            //t0.[DemoDate] = '2019-04-13' AND t0.[DemoDateTime] = '2019-04-13 22:50:39.340' AND t0.[DemoDateTime2] = '2019-04-13 22:50:39.340744' AND 
            //t0.[DemoName] = (CASE WHEN t0.[DemoDateTime_Nullable] IS NULL THEN N'NULL' ELSE N'NOT NULL' END) AND 
            //t0.[DemoName] = ISNULL(t0.[DemoName],t0.[DemoCode]) AND t0.[DemoId] IN(1,2,3) AND t0.[DemoId] IN(1,2,3) AND 
            //t0.[DemoId] IN(2,3) AND t0.[DemoId] = 1 AND t0.[DemoId] IN(2,3) AND t0.[DemoName] = N'002F' AND 
            //t0.[DemoCode] = ISNULL(t0.[DemoCode],'CODE') AND t0.[DemoCode] IN('A','B','C') AND t0.[DemoByte] = 9 AND 
            //t0.[DemoByte] = 1) OR (t0.[DemoInt] = 1)) OR (t0.[DemoInt] = 1)) OR (t0.[DemoName] = N'STATE' AND t0.[DemoName] = N'REMARK')

            // 行号
            var query1 =
                context
                .GetTable<TDemo>()
                .Select(a => new
                {
                    RowNumber = DbFunction.RowNumber<long>(x => a.DemoCode, false)
                });
            var reuslt1 = query1.ToList();
            //SQL=>
            //SELECT 
            //ROW_NUMBER() Over(Order By t0.[DemoCode]) AS [RowNumber]
            //FROM [Sys_Demo] t0 

            query1 =
            context
            .GetTable<Model.ClientAccount>()
            .Select(a => new
            {
                RowNumber = DbFunction.PartitionRowNumber<long>(x => a.ClientId, x => a.AccountId, true)
            });
            reuslt1 = query1.ToList();

            // DataTable
            query = from a in context.GetTable<TDemo>()
                    orderby a.DemoCode
                    select a;
            query = query.Take(18);
            var result3 = context.Database.ExecuteDataTable(query);
#if !net40
            query = from a in context.GetTable<TDemo>()
                    orderby a.DemoCode
                    select a;
            query = query.Take(18);
            result3 = context.Database.ExecuteDataTableAsync(query).Result;
#endif

            // ORACLE 不支持同时跑多个 SELECT并返回DataSet
            if (_databaseType != DatabaseType.Oracle)
            {
                // DataSet
                var cmd = query.Resolve();
                List<Command> sqlList = new List<Command> { cmd, cmd, cmd };
                var result4 = context.Database.ExecuteDataSet(sqlList);
#if !net40
                cmd = query.Resolve();
                sqlList = new List<Command> { cmd, cmd, cmd };
                result4 = context.Database.ExecuteDataSetAsync(sqlList).Result;
#endif
            }
        }

        // 多表查询
        void Join()
        {
            var context = _newContext();

            // INNER JOIN
            var query =
                from a in context.GetTable<Model.Client>()
                join b in context.GetTable<Model.CloudServer>() on a.CloudServerId equals b.CloudServerId
                where a.ClientId > 0
                select a;
            var result = query.ToList();
            // 点标记
            query = context
                .GetTable<Model.Client>()
                .Join(context.GetTable<Model.CloudServer>(), a => a.CloudServerId, b => b.CloudServerId, (a, b) => a)
                .Where(a => a.ClientId > 0);
            result = query.ToList();
            //SQL=>
            //SELECT
            //t0.[ClientId] AS[ClientId],
            //t0.[ClientCode] AS[ClientCode],
            //t0.[ClientName] AS[ClientName],
            //t0.[Remark] AS[Remark],
            //t0.[State] AS[State],
            //t0.[ActiveDate] AS[ActiveDate],
            //t0.[CloudServerId] AS[CloudServerId]
            //FROM[Bas_Client] t0
            //INNER JOIN[Sys_CloudServer] t1 ON t0.[CloudServerId] = t1.[CloudServerId]
            //WHERE t0.[ClientId] > 0


            // 更简单的赋值方式 
            // 适用场景：在显示列表时只想显示外键表的一两个字段
            query =
                from a in context.GetTable<Model.Client>()
                select new Model.Client(a)
                {
                    CloudServer = a.CloudServer,
                    LocalServer = new Model.CloudServer
                    {
                        CloudServerId = a.CloudServerId,
                        CloudServerName = a.LocalServer.CloudServerName
                    }
                };
            result = query.ToList();
            //SQL=>
            //SELECT
            //t0.[ClientId] AS[ClientId],
            //***
            //t1.[CloudServerId] AS[CloudServerId1],
            //t1.[CloudServerCode] AS[CloudServerCode],
            //t1.[CloudServerName] AS[CloudServerName],
            //CASE WHEN t1.[CloudServerId] IS NULL THEN NULL ELSE t1.[CloudServerId] END AS[NULL],
            //t0.[CloudServerId] AS[CloudServerId2],
            //t2.[CloudServerName] AS[CloudServerName1]
            //FROM[Bas_Client] t0
            //LEFT JOIN[Sys_CloudServer] t1 ON t0.[CloudServerId] = t1.[CloudServerId]
            //LEFT JOIN[Sys_CloudServer] t2 ON t0.[CloudServerId] = t2.[CloudServerId]

            // 1：1关系，1：n关系
            query =
                from a in context.GetTable<Model.Client>()
                where a.ClientId > 0
                orderby a.ClientId
                select new Model.Client(a)
                {
                    CloudServer = a.CloudServer,
                    Accounts = a.Accounts
                };
            result = query.ToList();
            //SQL=>
            //SELECT
            //t0.[ClientId] AS[ClientId],
            //***
            //t1.[CloudServerId] AS[CloudServerId1],
            //t1.[CloudServerCode] AS[CloudServerCode],
            //t1.[CloudServerName] AS[CloudServerName],
            //CASE WHEN t1.[CloudServerId] IS NULL THEN NULL ELSE t1.[CloudServerId]
            //        END AS[NULL],
            //t2.[ClientId] AS[ClientId1],
            //t2.[AccountId] AS[AccountId],
            //t2.[AccountCode] AS[AccountCode],
            //t2.[AccountName] AS[AccountName],
            //CASE WHEN t2.[ClientId] IS NULL THEN NULL ELSE t2.[ClientId] END AS [NULL1]
            //FROM (
            //    SELECT
            //    t0.[ClientId] AS[ClientId],
            //    ***
            //    FROM [Bas_Client] t0
            //    WHERE t0.[ClientId] > 0
            //) t0
            //LEFT JOIN[Sys_CloudServer] t1 ON t0.[CloudServerId] = t1.[CloudServerId]
            //LEFT JOIN [Bas_ClientAccount] t2 ON t0.[ClientId] = t2.[ClientId]
            //ORDER BY t0.[ClientId]

            // Include 语法
            query =
                context
                .GetTable<Model.Client>()
                .Include(a => a.CloudServer);
            query =
                from a in query
                join b in context.GetTable<Model.CloudServer>() on a.CloudServerId equals b.CloudServerId
                orderby a.ClientId
                select new Model.Client(a)
                {
                    CloudServer = a.CloudServer
                };
            result = query.ToList();
            //SQL=>
            //SELECT 
            //t0.[ClientId] AS [ClientId],
            //***
            //t0.[Remark] AS [Remark],
            //t1.[CloudServerId] AS [CloudServerId1],
            //t1.[CloudServerCode] AS [CloudServerCode],
            //t1.[CloudServerName] AS [CloudServerName],
            //CASE WHEN t1.[CloudServerId] IS NULL THEN NULL ELSE t1.[CloudServerId] END AS [NULL],
            //t1.[CloudServerId] AS [CloudServerId2],
            //t1.[CloudServerCode] AS [CloudServerCode1],
            //t1.[CloudServerName] AS [CloudServerName1],
            //CASE WHEN t1.[CloudServerId] IS NULL THEN NULL ELSE t1.[CloudServerId] END AS [NULL1]
            //FROM [Bas_Client] t0 
            //INNER JOIN [Sys_CloudServer] t1 ON t0.[CloudServerId] = t1.[CloudServerId]
            //ORDER BY t0.[ClientId]

            // 还是Include，无限主从孙 ### 
            query =
            from a in context
                .GetTable<Model.Client>()
                .Include(a => a.Accounts)
                .Include(a => a.Accounts[0].Markets)
                .Include(a => a.Accounts[0].Markets[0].Client)
            where a.ClientId > 0
            orderby a.ClientId
            select a;
            result = query.ToList();
            query =
                from a in context
                    .GetTable<Model.Client>()
                    .Include(a => a.Accounts)
                    .Include(a => a.Accounts[0].Markets)
                    .Include(a => a.Accounts[0].Markets[0].Client)
                where a.ClientId > 0
                orderby a.ClientId, a.Accounts[0].Markets[0].MarketId
                select a;
            result = query.ToList();
            //SQL=>
            //SELECT
            //t0.[ClientId] AS[ClientId],
            //t0.[ClientCode] AS[ClientCode],
            //t0.[ClientName] AS[ClientName],
            //***
            //FROM (
            //    SELECT
            //    t0.[ClientId] AS[ClientId],
            //    ***
            //    FROM [Bas_Client] t0
            //    WHERE t0.[ClientId] > 0
            //) t0
            //LEFT JOIN[Bas_ClientAccount] t1 ON t0.[ClientId] = t1.[ClientId]
            //LEFT JOIN [Bas_ClientAccountMarket] t2 ON t1.[ClientId] = t2.[ClientId] AND t1.[AccountId] = t2.[AccountId]
            //LEFT JOIN [Bas_Client] t3 ON t2.[ClientId] = t3.[ClientId]
            //ORDER BY t0.[ClientId]

            // Include 分页
            query =
            from a in context
                .GetTable<Model.Client>()
                .Include(a => a.Accounts)
                .Include(a => a.Accounts[0].Markets)
                .Include(a => a.Accounts[0].Markets[0].Client)
            where a.ClientId > 0
            orderby a.ClientId
            select a;
            query = query
                .Where(a => a.ClientId > 0 && a.CloudServer.CloudServerId > 0)
                .Skip(10)
                .Take(20);
            result = query.ToList();
            // Include 分页
            query =
            from a in context
                .GetTable<Model.Client>()
                .Include(a => a.Accounts)
                .Include(a => a.Accounts[0].Markets)
                .Include(a => a.Accounts[0].Markets[0].Client)
            where a.ClientId > 0
            orderby a.ClientId, a.Accounts[0].AccountId descending, a.Accounts[0].Markets[0].MarketId
            select a;
            query = query
                .Where(a => a.ClientId > 0 && a.CloudServer.CloudServerId > 0)
                .Skip(10)
                .Take(20);
            result = query.ToList();
            //SQL=>
            //SELECT 
            //t0.[ClientId] AS [ClientId],
            //t0.[ClientCode] AS [ClientCode],
            //t0.[ClientName] AS [ClientName],
            //***
            //CASE WHEN t3.[ClientId] IS NULL THEN NULL ELSE t3.[ClientId] END AS [NULL2]
            //FROM (
            //    SELECT 
            //    t0.[ClientId] AS [ClientId],
            //    t0.[ClientCode] AS [ClientCode],
            //    t0.[ClientName] AS [ClientName],
            //    ***
            //    FROM [Bas_Client] t0 
            //    LEFT JOIN [Sys_CloudServer] t1 ON t0.[CloudServerId] = t1.[CloudServerId]
            //    WHERE t0.[ClientId] > 0 AND t0.[ClientId] > 0 AND t1.[CloudServerId] > 0
            //    ORDER BY t0.[ClientId]
            //    OFFSET 10 ROWS FETCH NEXT 20 ROWS ONLY 
            //) t0 
            //LEFT JOIN [Bas_ClientAccount] t1 ON t0.[ClientId] = t1.[ClientId]
            //LEFT JOIN [Bas_ClientAccountMarket] t2 ON t1.[ClientId] = t2.[ClientId] AND t1.[AccountId] = t2.[AccountId]
            //LEFT JOIN [Bas_Client] t3 ON t2.[ClientId] = t3.[ClientId]

            query =
               from a in context
                   .GetTable<Model.Client>()
                   .Include(a => a.CloudServer)
                   .Include(a => a.Accounts)
               where a.ClientId > 0
               select a;
            query = query.OrderBy(a => a.ClientId);
            result = query.ToList();
            //SQL=>
            //SELECT 
            //t0.[ClientId] AS [ClientId],
            //t0.[ClientCode] AS [ClientCode],
            //t0.[ClientName] AS [ClientName],
            //***
            //FROM (
            //    SELECT 
            //    t0.[ClientId] AS [ClientId],
            //    t0.[ClientCode] AS [ClientCode],
            //    ***
            //    FROM [Bas_Client] t0 
            //    WHERE t0.[ClientId] > 0
            //) t0 
            //LEFT JOIN [Sys_CloudServer] t1 ON t0.[CloudServerId] = t1.[CloudServerId]
            //LEFT JOIN [Bas_ClientAccount] t2 ON t0.[ClientId] = t2.[ClientId]
            //ORDER BY t0.[ClientId]

            // Include 语法查询 主 从 孙 关系<注：相同的导航属性不能同时用include和join>
            var query1 =
                from a in
                    context
                    .GetTable<Model.Client>()
                    .Include(a => a.CloudServer)
                    .Include(a => a.Accounts)
                    .Include(a => a.Accounts[0].Markets)
                    .Include(a => a.Accounts[0].Markets[0].Client)
                group a by new { a.ClientId, a.ClientCode, a.ClientName, a.CloudServer.CloudServerId } into g
                select new Model.Client
                {
                    ClientId = g.Key.ClientId,
                    ClientCode = g.Key.ClientCode,
                    ClientName = g.Key.ClientName,
                    CloudServerId = g.Key.CloudServerId,
                    Qty = g.Sum(a => a.Qty)
                };
            query1 = query1
                .Where(a => a.ClientId > 0)
                .OrderBy(a => a.ClientId)
                .Skip(10)
                .Take(20)
                ;
            var result1 = query1.ToList();
            //SQL=>
            //SELECT 
            //t0.[ClientId] AS [ClientId],
            //t0.[ClientCode] AS [ClientCode],
            //t0.[ClientName] AS [ClientName],
            //***
            //FROM (
            //    SELECT 
            //    t0.[ClientId] AS [ClientId],
            //    t0.[ClientCode] AS [ClientCode],
            //    ***
            //    SUM(t0.[Qty]) AS [Qty]
            //    FROM [Bas_Client] t0 
            //    LEFT JOIN [Sys_CloudServer] t1 ON t0.[CloudServerId] = t1.[CloudServerId]
            //    GROUP BY t0.[ClientId],t0.[ClientCode],t0.[ClientName],t1.[CloudServerId]
            //    Having t0.[ClientId] > 0
            //    ORDER BY t0.[ClientId]
            //    OFFSET 10 ROWS FETCH NEXT 20 ROWS ONLY 
            //) t0 
            //LEFT JOIN [Sys_CloudServer] t1 ON t0.[CloudServerId] = t1.[CloudServerId]
            //LEFT JOIN [Bas_ClientAccount] t2 ON t0.[ClientId] = t2.[ClientId]
            //LEFT JOIN [Bas_ClientAccountMarket] t3 ON t2.[ClientId] = t3.[ClientId] AND t2.[AccountId] = t3.[AccountId]
            //LEFT JOIN [Bas_Client] t4 ON t3.[ClientId] = t4.[ClientId]
            var max1 = query1.Max(a => a.Qty);
            //SQL=>
            //SELECT 
            //MAX(t0.[Qty])
            //FROM (
            //    SELECT 
            //    t0.[ClientId] AS [ClientId],
            //    t0.[ClientCode] AS [ClientCode],
            //    t0.[ClientName] AS [ClientName],
            //    ***
            //    FROM (
            //        SELECT 
            //        t0.[ClientId] AS [ClientId],
            //        ***
            //        t1.[CloudServerId] AS [CloudServerId],
            //        SUM(t0.[Qty]) AS [Qty]
            //        FROM [Bas_Client] t0 
            //        LEFT JOIN [Sys_CloudServer] t1 ON t0.[CloudServerId] = t1.[CloudServerId]
            //        GROUP BY t0.[ClientId],t0.[ClientCode],t0.[ClientName],t1.[CloudServerId]
            //        Having t0.[ClientId] > 0
            //        ORDER BY t0.[ClientId]
            //        OFFSET 10 ROWS FETCH NEXT 20 ROWS ONLY 
            //    ) t0 
            //    LEFT JOIN [Sys_CloudServer] t1 ON t0.[CloudServerId] = t1.[CloudServerId]
            //    LEFT JOIN [Bas_ClientAccount] t2 ON t0.[ClientId] = t2.[ClientId]
            //    LEFT JOIN [Bas_ClientAccountMarket] t3 ON t2.[ClientId] = t3.[ClientId] AND t2.[AccountId] = t3.[AccountId]
            //    LEFT JOIN [Bas_Client] t4 ON t3.[ClientId] = t4.[ClientId]
            //) t0 

            // 分组后再统计
            var query2 =
                from a in context.GetTable<Model.Client>()
                group a by a.ClientId into g
                select new
                {
                    ClientId = g.Key,
                    Qty = g.Sum(a => a.Qty)
                };
            query2 = query2.OrderBy(a => a.ClientId).ThenBy(a => a.Qty);
            var result2 = query2.Max(a => a.ClientId);
            //SQL=>
            //SELECT
            //MAX(t0.[ClientId])
            //FROM(
            //    SELECT
            //    t0.[ClientId] AS[ClientId],
            //    FROM[Bas_Client] t0
            //    GROUP BY t0.[ClientId]
            // ) t0
            var result9 = query2.Sum(a => a.Qty);
            //SQL=>
            //SELECT 
            //SUM(t0.[Qty])
            //FROM ( 
            //    SELECT 
            //    t0.[ClientId] AS [ClientId],
            //    SUM(t0.[Qty]) AS [Qty]
            //    FROM [Bas_Client] t0 
            //    GROUP BY t0.[ClientId]
            // ) t0

            var query3 =
                from a in context.GetTable<Model.Client>()
                join b in context.GetTable<Model.ClientAccount>() on a.ClientId equals b.ClientId
                group new { a.ClientId, b.AccountId } by new { a.ClientId, b.AccountId } into g
                select new
                {
                    ClientId = g.Key.ClientId,
                    AccountId = g.Key.AccountId,
                    Max = g.Max(b => b.AccountId)
                };
            var result3 = query3.ToList();
            //SQL=>
            //SELECT
            //t0.[ClientId] AS[ClientId],
            //t1.[AccountId] AS[AccountId],
            //MAX(t1.[AccountId]) AS[Max]
            //FROM[Bas_Client] t0
            //INNER JOIN[Bas_ClientAccount] t1 ON t0.[ClientId] = t1.[ClientId]
            //GROUP BY t0.[ClientId],t1.[AccountId]

            // CROSS JOIN
            var query4 =
                context
                .GetTable<Model.Client>()
                .Where(a => a.ClientId <= 10)
                .SelectMany(a => context.GetTable<Model.Client>(), (a, b) => new
                {
                    a.ClientId,
                    b.ClientName
                });
            var result4 = query4.ToList();
            //SQL=>
            //SELECT
            //t0.[DemoId] AS[DemoId],
            //t1.[DemoName] AS[DemoName]
            //FROM[Sys_Demo] t0
            //CROSS JOIN[Sys_Demo] t1

            // LEFT JOIN
            query =
                  from a in context.GetTable<Model.Client>()
                  join b in context.GetTable<Model.CloudServer>() on a.CloudServerId equals b.CloudServerId into u_b
                  from b in u_b.DefaultIfEmpty()
                  select a;
            query = query.Where(a => a.CloudServer.CloudServerName != null);
            result = query.ToList();

            // LEFT JOIN
            query =
                  from a in context.GetTable<Model.Client>()
                  join b in context.GetTable<Model.CloudServer>() on new { a.CloudServerId, CloudServerCode = "567" } equals new { b.CloudServerId, b.CloudServerCode } into u_b
                  from b in u_b.DefaultIfEmpty()
                  select a;
            query = query.Where(a => a.CloudServer.CloudServerName != null);
            result = query.ToList();
            //SQL=>
            //SELECT
            //t0.[ClientId] AS[ClientId],
            //***
            //FROM[Bas_Client] t0
            //LEFT JOIN[Sys_CloudServer] t1 ON t0.[CloudServerId] = t1.[CloudServerId]
            //WHERE t1.[CloudServerName] IS NOT NULL

            // LEFT JOIN + CROSS JOIN
            query =
                 from a in context.GetTable<Model.Client>()
                 join b in context.GetTable<Model.CloudServer>() on a.CloudServerId equals b.CloudServerId into u_c
                 from b in u_c.DefaultIfEmpty()
                 select a;
            var query5 =
                query.SelectMany(c => context.GetTable<Model.CloudServer>(), (a, c) => new
                {
                    ClientId = a.ClientId,
                    CloudServerName = a.CloudServer.CloudServerName,
                    CloudServerCode = c.CloudServerCode
                });
            var result5 = query5.ToList();
            //SQL=>
            //SELECT
            //t0.[ClientId] AS[ClientId],
            //t1.[CloudServerName] AS[CloudServerName],
            //t2.[CloudServerCode] AS[CloudServerCode]
            //FROM[Bas_Client] t0
            //LEFT JOIN[Sys_CloudServer] t1 ON t0.[CloudServerId] = t1.[CloudServerId]
            //CROSS JOIN[Sys_CloudServer] t2

            // SQLite 不支持RIGHT OUTER JOIN
            if (_databaseType != DatabaseType.SQLite)
            {
                // RIGHT JOIN
                query =
                      from a in context.GetTable<Model.CloudServer>()
                      join b in context.GetTable<Model.Client>() on a.CloudServerId equals b.CloudServerId into u_b
                      from b in u_b.DefaultIfEmpty(true)
                      where a.CloudServerName == null
                      select b;
                result = query.ToList();
                //SQL=>
                //SELECT 
                //t1.[ClientId] AS [ClientId],
                //***
                //FROM [Sys_CloudServer] t0 
                //RIGHT JOIN [Bas_Client] t1 ON t0.[CloudServerId] = t1.[CloudServerId]
                //WHERE t0.[CloudServerName] IS NULL
            }

            // UNION 注意UNION分页的写法，仅支持写在最后
            var q1 = context.GetTable<Model.Client>().Where(x => x.ClientId == 0);
            var q2 = context.GetTable<Model.Client>().Where(x => x.ClientId == 0);
            var q3 = context.GetTable<Model.Client>().Where(x => x.ClientId == 0);
            var query6 = q1.Union(q2).Union(q3);
            var result6 = query6.ToList();
            result6 = query6.Take(2).ToList();
            result6 = query6.OrderBy(a => a.ClientId).Skip(2).ToList();
            query6 = query6.Take(2);
            result6 = query6.ToList();
            query6 = query6.OrderBy(a => a.ClientId).Skip(1).Take(2);
            result6 = query6.ToList();
            //SQL=>
            //SELECT
            //t0.[ClientId] AS[ClientId],
            //t0.[ClientCode] AS[ClientCode],
            //t0.[ClientName] AS[ClientName],
            //...
            //FROM[Bas_Client] t0
            //WHERE t0.[ClientId] = 1
            //UNION ALL
            //SELECT
            //t0.[ClientId] AS[ClientId],
            //t0.[ClientCode] AS[ClientCode],
            //t0.[ClientName] AS[ClientName],
            //...
            //FROM[Bas_Client] t0
            //WHERE t0.[ClientId] = 1
            //UNION ALL
            //SELECT
            //t0.[ClientId] AS[ClientId],
            //t0.[ClientCode] AS[ClientCode],
            //t0.[ClientName] AS[ClientName],
            //...
            //FROM[Bas_Client] t0
            //WHERE t0.[ClientId] = 1

            // Any
            var isAny = context.GetTable<Model.Client>().Any();
            isAny = context.GetTable<Model.Client>().Any(a => a.ActiveDate == DateTime.Now);
            isAny = context.GetTable<Model.Client>().Distinct().Any(a => a.ActiveDate == DateTime.Now);
            isAny = context.GetTable<Model.Client>().OrderBy(a => a.ClientId).Skip(2).Take(5).Any(a => a.ActiveDate == DateTime.Now);
            //SQL=> 
            //IF EXISTS(
            //    SELECT TOP 1 1
            //    FROM[Bas_Client] t0
            //   WHERE t0.[ActiveDate] = '2018-08-15 14:07:09.784'
            //) SELECT 1 ELSE SELECT 0

            // FirstOrDefault
            var f = context.GetTable<Model.Client>().FirstOrDefault();
            //SQL=> 
            //SELECT TOP(1)
            //t0.[ClientId] AS[ClientId],
            //t0.[ClientCode] AS[ClientCode],
            //t0.[ClientName] AS[ClientName],
            //t0.[State] AS[State],
            //t0.[ActiveDate] AS[ActiveDate],
            //t0.[CloudServerId] AS[CloudServerId]
            //FROM[Bas_Client] t0

            // Max,Count,Min,Avg,Sum
            var max = context.GetTable<Model.Client>().Where(a => a.ClientId < -9).Max(a => a.ClientId);
            //SQL=> 
            //SELECT
            //MAX(t0.[ClientId])
            //FROM[Bas_Client] t0
            //WHERE t0.[ClientId] < -9

            // GROUP BY
            var query7 =
                 from a in context.GetTable<Model.Client>()
                 where a.ClientName == "TAN"
                 group a by new { a.ClientId, a.ClientName } into g
                 where g.Key.ClientId > 0
                 orderby g.Key.ClientName
                 select new
                 {
                     Id = g.Key.ClientId,
                     Name = g.Min(a => a.ClientId)
                 };
            var result7 = query7.ToList();
            //SQL=> 
            //SELECT
            //t0.[ClientId] AS[Id],
            //MIN(t0.[ClientId]) AS[Name]
            //FROM[Bas_Client] t0
            //WHERE t0.[ClientName] = N'TAN'
            //GROUP BY t0.[ClientId],t0.[ClientName]
            //Having t0.[ClientId] > 0
            //ORDER BY t0.[ClientName]

            // 分组后再分页
            var query8 =
                 from a in context.GetTable<Model.Client>()
                 where a.ClientName == "TAN"
                 group a by new { a.ClientId, a.ClientName } into g
                 where g.Key.ClientId > 0
                 orderby new { g.Key.ClientName, g.Key.ClientId }
                 select new
                 {
                     Id = g.Key.ClientId,
                     Name = g.Min(a => a.ClientId)
                 };
            query8 = query8.Skip(2).Take(3);
            var result8 = query8.ToList();
            //SQL=> 
            //SELECT
            //t0.[ClientId] AS[Id],
            //MIN(t0.[ClientId]) AS[Name]
            //FROM[Bas_Client] t0
            //WHERE t0.[ClientName] = N'TAN'
            //GROUP BY t0.[ClientId],t0.[ClientName]
            //Having t0.[ClientId] > 0
            //ORDER BY t0.[ClientName],t0.[ClientId]
            //OFFSET 2 ROWS FETCH NEXT 3 ROWS ONLY

            // DISTINCT 分组
            query =
                context
                .GetTable<Model.Client>()
                .Distinct()
                .Select(a => new Model.Client
                {
                    ClientId = a.ClientId,
                    ClientName = a.ClientName
                });
            var min = query.Min(a => a.ClientId);
            //SQL=> 
            //SELECT
            //MIN(t0.[ClientId])
            //FROM(
            //    SELECT DISTINCT
            //    t0.[ClientId] AS[ClientId],
            //    ***
            //    FROM[Bas_Client] t0
            // ) t0

            // 强制子查询
            query =
                  from a in context.GetTable<Model.Client>()
                  join b in context.GetTable<Model.CloudServer>() on a.CloudServerId equals b.CloudServerId into u_c
                  from b in u_c.DefaultIfEmpty()
                  select a;
            query = query.OrderBy(a => a.ClientId).Skip(10).Take(10).AsSubQuery();
            query = from a in query
                    join b in context.GetTable<Model.Client>() on a.ClientId equals b.ClientId
                    select a;
            result = query.ToList();
            //SQL=> 
            //SELECT 
            //t0.[ClientId] AS [ClientId],
            //t0.[ClientCode] AS [ClientCode],
            //***
            //FROM (
            //    SELECT 
            //    t0.[ClientId] AS [ClientId],
            //    t0.[ClientCode] AS [ClientCode],
            //    t0.[ClientName] AS [ClientName]
            //    ***
            //    FROM [Bas_Client] t0 
            //    LEFT JOIN [Sys_CloudServer] t1 ON t0.[CloudServerId] = t1.[CloudServerId]
            //    ORDER BY t0.[ClientId]
            //    OFFSET 10 ROWS FETCH NEXT 10 ROWS ONLY 
            //) t0 
            //INNER JOIN [Bas_Client] t1 ON t0.[ClientId] = t1.[ClientId]

            var subQuery =
                from a in context.GetTable<Model.Client>()
                join b in context.GetTable<Model.ClientAccount>() on a.ClientId equals b.ClientId
                select new
                {
                    ClientId = a.ClientId,
                    ClientName = a.ClientName,
                    Qty = a.Qty
                };
            subQuery = subQuery.AsSubQuery();

            query =
                from a in subQuery
                group a by a.ClientId into g
                select new Model.Client
                {
                    ClientId = g.Key,
                    ClientName = g.Max(a => a.ClientName),
                    Qty = g.Sum(a => a.Qty)
                };
            query = query.AsSubQuery();
            query = query.Select(a => new Model.Client { ClientId = a.ClientId, ClientName = a.ClientName, Qty = a.Qty }).OrderBy(a => a.Qty);
            result = query.ToList();
            //var result10 = query.ToPagedList(1, 20);
        }

        // 删除记录
        void Delete()
        {
            var context = _newContext();

            // 1. 删除单个记录
            var demo = new TDemo { DemoId = 1 };
            context.Delete(demo);
            context.SubmitChanges();
#if !net40
            demo = new TDemo { DemoId = 1 };
            context.Delete(demo);
            var rowCount = context.SubmitChangesAsync().Result;
#endif
            //SQL=> 
            //DELETE t0 FROM [Sys_Demo] t0 
            //WHERE t0.[DemoId] = 1

            // 2.WHERE 条件批量删除
            context.Delete<TDemo>(a => a.DemoId == 2 || a.DemoId == 3 || a.DemoName == "N0000004");
            var qeury =
                context
                .GetTable<TDemo>()
                .Where(a => a.DemoId == 2 || a.DemoId == 3 || a.DemoName == "N0000004");
            // 2.WHERE 条件批量删除
            context.Delete<TDemo>(qeury);

            if (_databaseType != DatabaseType.Oracle)
            {
                // 3.Query 关联批量删除
                var query1 =
                    from a in context.GetTable<Model.Client>()
                    join b in context.GetTable<Model.ClientAccount>() on a.ClientId equals b.ClientId
                    join c in context.GetTable<Model.ClientAccountMarket>() on new { b.ClientId, b.AccountId } equals new { c.ClientId, c.AccountId }
                    where c.ClientId == 5 && c.AccountId == "1" && c.MarketId == 1
                    select a;
                context.Delete<Model.Client>(query1);

                // oracle 不支持导航属性关联删除
                // 3.Query 关联批量删除
                var query2 =
                    from a in context.GetTable<Model.Client>()
                    join b in context.GetTable<Model.ClientAccount>() on a.ClientId equals b.ClientId
                    where a.CloudServer.CloudServerId == 20 && a.LocalServer.CloudServerId == 2
                    select a;
                context.Delete<Model.Client>(query2);
                // 4.Query 关联批量删除
                var query3 =
                    from a in context.GetTable<Model.Client>()
                    where a.CloudServer.CloudServerId == 20 && a.LocalServer.CloudServerId == 2
                    select a;
                context.Delete<Model.Client>(query3);
            }

            // 5.子查询批量删除
            // 子查询更新
            var sum =
                from a in context.GetTable<Model.ClientAccount>()
                where a.ClientId <= 20
                group a by new { a.ClientId } into g
                select new Model.Client
                {
                    ClientId = g.Key.ClientId,
                    Qty = g.Sum(a => a.Qty)
                };
            var query4 =
                from a in context.GetTable<Model.Client>()
                join b in context.GetTable<Model.CloudServer>() on a.CloudServerId equals b.CloudServerId
                join c in context.GetTable<Model.CloudServer>() on a.CloudServerId equals c.CloudServerId
                join d in sum on a.ClientId equals d.ClientId
                where a.ClientId > 10 && a.CloudServerId < 0
                select a;
            context.Delete<Model.Client>(query4);

            // 提交的同时查出数据
            // 适用场景：批量导入数据
            // 1.先插入数据到表变量
            // 2.提交并查出当批数据
            // 3.或者将存储过程/脚本插在当前上下文一起执行
            // 4.oracle 不支持~


            if (_databaseType != DatabaseType.Oracle)
            {
                context.AddQuery(sum);
                // context.AddQuery('Exec #存储过程#');
                // context.AddQuery('#文本脚本#');
                List<Model.Client> result0 = null;
                context.SubmitChanges(out result0);
            }
            else context.SubmitChanges();
            //// 一次性提交
            //context.SubmitChanges();
            //SQL=> 
            //DELETE t0 FROM [Sys_Demo] t0 
            //WHERE ((t0.[DemoId] = 2) OR (t0.[DemoId] = 3)) OR (t0.[DemoName] = N'N0000004')
            //DELETE t0 FROM [Sys_Demo] t0 
            //WHERE ((t0.[DemoId] = 2) OR (t0.[DemoId] = 3)) OR (t0.[DemoName] = N'N0000004')
            //DELETE t0 FROM [Bas_Client] t0 
            //INNER JOIN [Bas_ClientAccount] t1 ON t0.[ClientId] = t1.[ClientId]
            //INNER JOIN [Bas_ClientAccountMarket] t2 ON t1.[ClientId] = t2.[ClientId] AND t1.[AccountId] = t2.[AccountId]
            //WHERE t2.[ClientId] = 5 AND t2.[AccountId] = N'1' AND t2.[MarketId] = 1
            //DELETE t0 FROM [Bas_Client] t0 
            //INNER JOIN [Bas_ClientAccount] t1 ON t0.[ClientId] = t1.[ClientId]
            //LEFT JOIN [Sys_CloudServer] t2 ON t0.[CloudServerId] = t2.[CloudServerId]
            //LEFT JOIN [Sys_CloudServer] t3 ON t0.[CloudServerId] = t3.[CloudServerId]
            //WHERE t2.[CloudServerId] = 20 AND t3.[CloudServerId] = 2
        }

        // 更新记录
        void Update()
        {
            var context = _newContext();

            var demo = context
                .GetTable<TDemo>()
                .FirstOrDefault(x => x.DemoId > 0);

            // 整个实体更新
            demo.DemoName = "001'.N";
            context.Update(demo);
            context.SubmitChanges();

            // 2.WHERE 条件批量更新
            context.Update<TDemo>(x => new TDemo
            {
                DemoDateTime2 = DateTime.UtcNow,
                DemoDateTime2_Nullable = null,
                //DemoTime_Nullable = ts
            }, x => x.DemoName == "001'.N" || x.DemoCode == "001'.N");

            if (_databaseType != DatabaseType.Oracle)
            {
                // 3.Query 关联批量更新
                var query =
                    from a in context.GetTable<Model.Client>()
                    where a.CloudServer.CloudServerId != 0
                    select a;
                context.Update<Model.Client>(a => new // Model.Client
                {
                    Remark = "001.TAN"
                }, query);
                //SQL=> 
                //UPDATE t0 SET
                //t0.[DemoCode] = 'Code0000004',
                //t0.[DemoName] = N'001''.N',
                //***
                //t0.[DemoLong] = 8192000000000,
                //t0.[DemoLong_Nullable] = 8192000000000
                //FROM [Sys_Demo] t0
                //WHERE t0.[DemoId] = 4
                //UPDATE t0 SET
                //t0.[DemoDateTime2] = '2019-04-13 15:19:59.758789',
                //t0.[DemoDateTime2_Nullable] = NULL
                //FROM [Sys_Demo] AS [t0]
                //WHERE t0.[DemoId] = 4
                //UPDATE t0 SET
                //t0.[DemoDateTime2] = '2019-04-13 15:19:59.758789',
                //t0.[DemoDateTime2_Nullable] = NULL
                //FROM [Sys_Demo] AS [t0]
                //WHERE (t0.[DemoName] = N'001''.N') OR (t0.[DemoCode] = '001''.N')
                //UPDATE t0 SET
                //t0.[Remark] = N'001.TAN'
                //FROM [Bas_Client] AS [t0]
                //LEFT JOIN [Sys_CloudServer] t1 ON t0.[CloudServerId] = t1.[CloudServerId]
                //WHERE t1.[CloudServerId] <> 0

                // 更新本表值等于从表的字段值
                query =
                    from a in context.GetTable<Model.Client>()
                    join b in context.GetTable<Model.CloudServer>() on a.CloudServerId equals b.CloudServerId
                    join c in context.GetTable<Model.ClientAccount>() on a.ClientId equals c.ClientId
                    where c.AccountId == "12"
                    select a;
                context.Update<Model.Client, Model.CloudServer>((a, b) => new Model.Client
                {
                    CloudServerId = b.CloudServerId,
                    Remark = "001.TAN"
                }, query);

                // 更新本表值等于从表的字段值
                query =
                    from a in context.GetTable<Model.Client>()
                    join b in context.GetTable<Model.CloudServer>() on a.CloudServerId equals b.CloudServerId
                    join c in context.GetTable<Model.ClientAccount>() on a.ClientId equals c.ClientId
                    where c.AccountId == "12"
                    select a;
                context.Update<Model.Client, Model.CloudServer, Model.ClientAccount>((a, b, c) => new
                {
                    CloudServerId = b.CloudServerId,
                    Qty = c.Qty,
                    Remark = "001.TAN"
                }, query);
            }

            context.SubmitChanges();
            //SQL=>
            //UPDATE t0 SET
            //t0.[CloudServerId] = t1.[CloudServerId],
            //t0.[Remark] = N'001.TAN'
            //FROM [Bas_Client] AS [t0]
            //INNER JOIN [Sys_CloudServer] t1 ON t0.[CloudServerId] = t1.[CloudServerId]
            //INNER JOIN [Bas_ClientAccount] t2 ON t0.[ClientId] = t2.[ClientId]
            //WHERE t2.[AccountId] = N'12'

            // 子查询更新
            var sum =
                from a in context.GetTable<Model.ClientAccount>()
                where a.ClientId > 0
                group a by new { a.ClientId } into g
                select new Model.Client
                {
                    ClientId = g.Key.ClientId,
                    Qty = g.Sum(a => a.Qty)
                };
            if (_databaseType == DatabaseType.SqlServer || _databaseType == DatabaseType.MySql)
            {
                var uQuery =
                   from a in context.GetTable<Model.Client>()
                   join b in sum on a.ClientId equals b.ClientId
                   where a.ClientId > 0 && b.ClientId > 0
                   select a;
                context.Update<Model.Client, Model.Client>((a, b) => new Model.Client { Qty = b.Qty }, uQuery);
            }
            else
            {
                // npg oracle 翻译成 EXISTS,更新字段的值不支持来自子查询
                var uQuery =
                    from a in context.GetTable<Model.Client>()
                    join b in sum on a.ClientId equals b.ClientId
                    where a.ClientId > 0 // b.ClientId > 0
                    select a;
                context.Update<Model.Client>(a => new Model.Client { Qty = 9 }, uQuery);
            }
            //SQL =>
            //UPDATE t0 SET
            //t0.[Qty] = t1.[Qty]
            //FROM [Bas_Client] AS [t0]
            //INNER JOIN (
            //    SELECT 
            //    t0.[ClientId] AS [ClientId],
            //    SUM(t0.[Qty]) AS [Qty]
            //    FROM [Bas_ClientAccount] t0 
            //    WHERE t0.[ClientId] > 0
            //    GROUP BY t0.[ClientId]
            //) t1 ON t0.[ClientId] = t1.[ClientId]
            //WHERE t1.[ClientId] > 0

            var client = context.GetTable<Model.Client>().FirstOrDefault();
            context.Update(client);
            // 一次性提交，里面自带事务
            context.SubmitChanges();
            // SQL=>
            //UPDATE t0 SET
            //t0.[Qty] = t1.[Qty]
            //FROM [Bas_Client] AS [t0]
            //INNER JOIN (
            //SELECT 
            //t0.[ClientId] AS [ClientId],
            //SUM(t0.[Qty]) AS [Qty]
            //FROM [Bas_ClientAccount] t0 
            //WHERE t0.[ClientId] > 0
            //GROUP BY t0.[ClientId]
            //) t1 ON t0.[ClientId] = t1.[ClientId]
            //WHERE t1.[ClientId] > 0
            //UPDATE t0 SET
            //t0.[ClientId] = 1,
            //t0.[ClientCode] = N'XFramework1',
            //t0.[ClientName] = N'XFramework1',
            //t0.[CloudServerId] = 3,
            //t0.[ActiveDate] = '2019-04-13 22:31:27.323',
            //t0.[Qty] = 0,
            //t0.[State] = 1,
            //t0.[Remark] = N'001.TAN'
            //FROM [Bas_Client] t0
            //WHERE t0.[ClientId] = 1
        }

        // 新增记录
        void Insert()
        {
            var context = _newContext();

            // 带自增列
            var demo = new TDemo
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
                DemoLong = 64
            };
            context.Insert(demo);
            context.SubmitChanges();

            var demo2 = new TDemo
            {
                DemoCode = "D0000002",
                DemoName = "N0000002",
                DemoBoolean = true,
                DemoChar = 'A',
                DemoNChar = 'B',
                DemoByte = 65,
                DemoDate = DateTime.Now,
                DemoDateTime = DateTime.Now,
                DemoDateTime2 = DateTime.Now,
                DemoDecimal = 65,
                DemoDouble = 65,
                DemoFloat = 65,
                DemoGuid = Guid.NewGuid(),
                DemoShort = 65,
                DemoInt = 65,
                DemoLong = 65
            };
            context.Insert(demo2);

            var demo3 = new TDemo
            {
                DemoCode = "D0000003",
                DemoName = "N0000003",
                DemoBoolean = true,
                DemoChar = 'A',
                DemoNChar = 'B',
                DemoByte = 66,
                DemoDate = DateTime.Now,
                DemoDateTime = DateTime.Now,
                DemoDateTime2 = DateTime.Now,
                DemoDecimal = 66,
                DemoDouble = 66,
                DemoFloat = 66,
                DemoGuid = Guid.NewGuid(),
                DemoShort = 66,
                DemoInt = 66,
                DemoLong = 66
            };
            context.Insert(demo3);
            context.Insert(demo);
            context.SubmitChanges();


            // 适用场景：在新增/修改/删除数据的同时查出数据集合
            context.Insert(demo);
            var cQuery = context.GetTable<Model.Client>().Where(x => x.ClientId <= 200);
            context.AddQuery(cQuery);
            context.Insert(demo2);
            context.Update<TDemo>(a => new TDemo
            {
                DemoCode = "D0000012"
            }, a => a.DemoId == demo2.DemoId);
            context.Update<TDemo>(a => new TDemo
            {
                DemoCode = "D0000112"
            }, a => a.DemoId == demo2.DemoId);
            context.Insert(demo3);
            List<Model.Client> result = null;
            context.SubmitChanges(out result);

            context.Insert(demo);
            cQuery = context.GetTable<Model.Client>().Where(x => x.ClientId <= 200);
            context.AddQuery(cQuery);
            context.Insert(demo2);
            context.Update<TDemo>(a => new TDemo
            {
                DemoCode = "D0000012"
            }, a => a.DemoId == demo2.DemoId);
            context.Update<TDemo>(a => new TDemo
            {
                DemoCode = "D0000112"
            }, a => a.DemoId == demo2.DemoId);
            context.Insert(demo3);
            var cQuery2 = context.GetTable<TDemo>().Where(x => x.DemoId <= 200);
            context.AddQuery(cQuery2);
            var cQuery3 = context.GetTable<TDemo>().Where(x => x.DemoId <= 20);
            context.AddQuery(cQuery3);
            List<Model.Client> result1 = null;
            List<TDemo> result2 = null;
            context.SubmitChanges(out result1, out result2);

            // 参数超过1000个，自动分批执行
            List<TDemo> demos = new List<TDemo>();
            for (var index = 0; index < 205; index++)
            {
                var demo4 = new TDemo
                {
                    DemoCode = "D0000002",
                    DemoName = "N0000002",
                    DemoBoolean = true,
                    DemoChar = 'A',
                    DemoNChar = 'B',
                    DemoByte = 65,
                    DemoDate = DateTime.Now,
                    DemoDateTime = DateTime.Now,
                    DemoDateTime2 = DateTime.Now,
                    DemoDecimal = 65,
                    DemoDouble = 65,
                    DemoFloat = 65,
                    DemoGuid = Guid.NewGuid(),
                    DemoShort = 65,
                    DemoInt = 65,
                    DemoLong = 65
                };
                demos.Add(demo4);
                if (index == 10 && _databaseType != DatabaseType.Oracle)
                {
                    var query22 = context.GetTable<Model.Demo>().Where(x => x.DemoId <= 20);
                    context.AddQuery(query22);
                }
                context.Insert(demo4);
            }
            context.SubmitChanges();

            // 指定ID，默认值支持
            int maxId = context.GetTable<Model.Client>().Max(x => x.ClientId);
            int nextId = maxId + 1;
            Model.Client client = new Model.Client
            {
                ClientId = nextId,
                ClientCode = "ABC",
                ClientName = "啊啵呲",
                Remark = "在批处理、名称作用域和数据库上下文方面，sp_executesql 与 EXECUTE 的行为相同。",
                CloudServerId = 11,
                State = 1
            };
            context.Insert<Model.Client>(client);

            // Query 关联新增
            nextId = nextId + 1;
            var query =
                from a in context.GetTable<Model.Client>()
                join b in context.GetTable<Model.CloudServer>() on a.CloudServerId equals b.CloudServerId
                where a.ClientId <= 5
                select new Model.Client
                {
                    ClientId = DbFunction.RowNumber<int>(x => a.ClientId) + nextId,
                    ClientCode = "ABC2",
                    ClientName = "啊啵呲2",
                    CloudServerId = 11,
                    State = 2
                };
            context.Insert(query);
            context.SubmitChanges();

            // 子查询增
            var sum =
                from a in context.GetTable<Model.ClientAccount>()
                where a.ClientId > 0
                group a by new { a.ClientId } into g
                select new Model.Client
                {
                    ClientId = g.Key.ClientId,
                    Qty = g.Sum(a => a.Qty)
                };
            sum = sum.AsSubQuery();

            maxId = context.GetTable<Model.Client>().Max(x => x.ClientId);
            nextId = maxId + 1;
            var nQuery =
                from a in sum
                join b in context.GetTable<Model.Client>() on a.ClientId equals b.ClientId into u_b
                from b in u_b.DefaultIfEmpty()
                where b.ClientId == null
                select new Model.Client
                {
                    ClientId = DbFunction.RowNumber<int>(x => a.ClientId) + nextId,
                    ClientCode = "ABC3",
                    ClientName = "啊啵呲3",
                    CloudServerId = 11,
                    State = 3,
                    Qty = a.Qty,
                };
            context.Insert(nQuery);

            // 批量增加
            // 产生 INSERT INTO VALUES(),(),()... 语法。注意这种批量增加的方法并不能给自增列自动赋值
            context.Delete<TDemo>(x => x.DemoId > 1000000);
            demos = new List<TDemo>();
            for (int i = 0; i < 1002; i++)
            {
                TDemo d = new TDemo
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
                    DemoLong = 64
                };
                demos.Add(d);
            }
            context.Insert<TDemo>(demos);
            context.SubmitChanges();
            ////SQL=>
            //INSERT INTO [Bas_Client]
            //([ClientId],[ClientCode],[ClientName],[CloudServerId],[ActiveDate],[Qty],[State],[Remark])
            //VALUES
            //(2019,N'ABC',N'啊啵呲',11,NULL,0,1,'默认值')
            //INSERT INTO [Bas_Client]([ClientId],[ClientCode],[ClientName],[CloudServerId],[State])
            //SELECT 
            //ROW_NUMBER() Over(Order By t0.[ClientId]) + 2020 AS [ClientId],
            //N'ABC2' AS [ClientCode],
            //N'啊啵呲2' AS [ClientName],
            //11 AS [CloudServerId],
            //1 AS [State]
            //FROM [Bas_Client] t0 
            //INNER JOIN [Sys_CloudServer] t1 ON t0.[CloudServerId] = t1.[CloudServerId]
            //WHERE t0.[ClientId] <= 5
            //DELETE t0 FROM [Sys_Demo] t0 
            //WHERE t0.[DemoId] > 1000000
            //INSERT INTO[Sys_Demo]
            //([DemoCode],[DemoName],[DemoChar],[DemoChar_Nullable],[DemoByte],[DemoByte_Nullable],[DemoDateTime],[DemoDateTime_Nullable],[DemoDecimal],[DemoDecimal_Nullable],[DemoFloat],[DemoFloat_Nullable],[DemoReal],[Demo_Nullable],[DemoGuid],[DemoGuid_Nullable],[DemoShort],[DemoShort_Nullable],[DemoInt],[DemoInt_Nullable],[DemoLong],[DemoLong_Nullable])
            //VALUES(...),(),()...

            // 指定ID，无自增列批量增加
            maxId = context.GetTable<Model.Client>().Max(x => x.ClientId);
            nextId = maxId + 1;
            List<Model.Client> clients = new List<Model.Client>();
            for (int index = 0; index < 1002; index++)
            {
                nextId = nextId + 1;
                client = new Model.Client
                {
                    ClientId = nextId,
                    ClientCode = "ABC2",
                    ClientName = "啊啵呲2",
                    Remark = "在批处理、名称作用域和数据库上下文方面，sp_executesql 与 EXECUTE 的行为相同。",
                    CloudServerId = 11,
                    State = 1
                };
                clients.Add(client);
            }
            context.Insert<Model.Client>(clients);
            context.SubmitChanges();
        }

        protected virtual void API()
        {
            var context = _newContext();

            var any = context.GetTable<Model.Client>().Any();
            any = context.GetTable<Model.Client>().Any(x => x.ClientCode.Contains("XF"));

            var count = context.GetTable<Model.Client>().Count();
            count = context.GetTable<Model.Client>().Count(x => x.ClientCode.Contains("XF"));
#if !net40
            count = context.GetTable<Model.Client>().CountAsync().Result;
            count = context.GetTable<Model.Client>().CountAsync(x => x.ClientCode.Contains("XF")).Result;
#endif

            var firstOrDefault = context.GetTable<Model.Client>().FirstOrDefault();
            firstOrDefault = context.GetTable<Model.Client>().FirstOrDefault(x => x.ClientCode.Contains("XF"));
#if !net40
            firstOrDefault = context.GetTable<Model.Client>().FirstOrDefaultAsync(x => x.ClientCode.Contains("XF")).Result;
#endif

            // 适用于需要产生NULL的场景
            var max = context.GetTable<Model.Client>().Where(a => a.ClientId == -1).Max(a => (Nullable<int>)a.ClientId);
            // 适用于不需要产生NULL的场景
            max = context.GetTable<Model.Client>().Where(a => a.ClientCode.Contains("XF")).Max(a => a.ClientId);
            // 不需要忽略空值
            max = context.GetTable<Model.Client>().Where(a => a.ClientCode.Contains("XF")).Max(a => (Nullable<int>)a.ClientId ?? 0);

            var min = context.GetTable<Model.Client>().Min(a => a.ClientId);
            min = context.GetTable<Model.Client>().Where(a => a.ClientCode.Contains("XF")).Min(a => a.ClientId);

            var avg = context.GetTable<Model.Client>().Average(a => (double)a.Qty);
            avg = context.GetTable<Model.Client>().Where(a => a.ClientCode.Contains("XF")).Average(a => a.ClientId);

            var sum = context.GetTable<Model.Client>().Sum(a => (long)a.Qty);
            sum = context.GetTable<Model.Client>().Where(a => a.ClientCode.Contains("XF")).Sum(a => a.ClientId);

            var toArray = context.GetTable<Model.Client>().ToArray();
            toArray = context.GetTable<Model.Client>().OrderBy(a => a.ClientId).ToArray(2, 10);

            var dataTalbe = context.GetTable<Model.Client>().ToDataTable();
            var dataSet = context.GetTable<Model.Client>().ToDataSet();

            var cQuery = context.GetTable<Model.Client>().Where(x => x.ClientId <= 200);
            int rowCount = context.Database.ExecuteNonQuery(cQuery);

            cQuery = context.GetTable<Model.Client>().Where(x => x.ClientId <= 200);
            object obj = context.Database.ExecuteScalar(cQuery);

            context.Update<Model.Client>(x => new Model.Client
            {
                ClientName = "蒙3"
            }, x => x.ClientId == 3);
            var query =
                from a in context.GetTable<Model.Client>()
                where a.ClientId == 1
                select 5;
            context.AddQuery(query);
            List<int> result1 = null;
            context.SubmitChanges(out result1);

            context.Update<Model.Client>(x => new Model.Client
            {
                ClientName = "蒙4"
            }, x => x.ClientId == 4);
            query =
                from a in context.GetTable<Model.Client>()
                where a.ClientId == 1
                select 5;
            context.AddQuery(query);
            var query2 =
                from a in context.GetTable<Model.Client>()
                where a.ClientId == 1
                select 6;
            context.AddQuery(query2);
            result1 = null;
            List<int> result2 = null;
            context.SubmitChanges(out result1, out result2);


            // 一性加载多个列表 ****
            var query3 =
               from a in context.GetTable<Model.Client>()
               where a.ClientId >= 1 && a.ClientId <= 10
               select 5;
            var query4 =
                from a in context.GetTable<Model.Client>()
                where a.ClientId >= 1 && a.ClientId <= 10
                select 6;
            var tuple = context.Database.ExecuteMultiple<int, int>(query3, query4);

            query3 =
               from a in context.GetTable<Model.Client>()
               where a.ClientId >= 1 && a.ClientId <= 10
               select 5;
            query4 =
                from a in context.GetTable<Model.Client>()
                where a.ClientId >= 1 && a.ClientId <= 10
                select 6;
            var query5 =
                 from a in context.GetTable<Model.Client>()
                 where a.ClientId >= 1 && a.ClientId <= 10
                 select 7;
            var tuple2 = context.Database.ExecuteMultiple<int, int, int>(query3, query4, query5);
#if !net40
            query3 =
               from a in context.GetTable<Model.Client>()
               where a.ClientId >= 1 && a.ClientId <= 10
               select 5;
            query4 =
                from a in context.GetTable<Model.Client>()
                where a.ClientId >= 1 && a.ClientId <= 10
                select 6;
            tuple = context.Database.ExecuteMultipleAsync<int, int>(query3, query4).Result;

            query3 =
               from a in context.GetTable<Model.Client>()
               where a.ClientId >= 1 && a.ClientId <= 10
               select 5;
            query4 =
                from a in context.GetTable<Model.Client>()
                where a.ClientId >= 1 && a.ClientId <= 10
                select 6;
            query5 =
                 from a in context.GetTable<Model.Client>()
                 where a.ClientId >= 1 && a.ClientId <= 10
                 select 6;
            tuple2 = context.Database.ExecuteMultipleAsync<int, int, int>(query3, query4, query4).Result;
#endif

            // 事务1. 上下文独立事务
            try
            {
                using (var transaction = context.Database.BeginTransaction())
                {
                    var result = context.GetTable<Model.Client>().FirstOrDefault(x => x.ClientId <= 10);
                    context.Update<Model.Client>(x => new Model.Client
                    {
                        ClientName = "事务1"
                    }, x => x.ClientId == result.ClientId);
                    context.SubmitChanges();
                    result = context.GetTable<Model.Client>().FirstOrDefault(x => x.ClientId == result.ClientId);

                    context.Update<Model.Client>(x => new Model.Client
                    {
                        ClientName = "事务2"
                    }, x => x.ClientId == result.ClientId);
                    context.SubmitChanges();
                    result = context.GetTable<Model.Client>().FirstOrDefault(x => x.ClientId == result.ClientId);

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
                connection = context.Database.DbProviderFactory.CreateConnection();
                connection.ConnectionString = context.Database.ConnectionString;
                if (connection.State != ConnectionState.Open) connection.Open();
                transaction2 = connection.BeginTransaction();

                // 指定事务
                context.Database.Transaction = transaction2;

                var result = context.GetTable<Model.Client>().FirstOrDefault(x => x.ClientId <= 10);
                context.Update<Model.Client>(x => new Model.Client
                {
                    ClientName = "事务3"
                }, x => x.ClientId == result.ClientId);
                context.SubmitChanges();
                result = context.GetTable<Model.Client>().FirstOrDefault(x => x.ClientId == result.ClientId);

                context.Update<Model.Client>(x => new Model.Client
                {
                    ClientName = "事务4"
                }, x => x.ClientId == result.ClientId);
                result = context.GetTable<Model.Client>().FirstOrDefault(x => x.ClientId == result.ClientId);

                string sql = @"UPDATE Bas_Client SET ClientName = N'事务5' WHERE ClientID=2;UPDATE Bas_Client SET ClientName = N'事务6' WHERE ClientID=3;";
                context.AddQuery(sql);
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
        void Rabbit()
        {
            Stopwatch stop = new Stopwatch();
            var context = _newContext();

            stop = new Stopwatch();
            stop.Start();
            for (int i = 0; i < 10; i++)
            {
                DateTime sDate = DateTime.Now;
                var result = context
                    .GetTable<Model.Rabbit>()
                    .ToList();
                Console.WriteLine(string.Format("第 {0} 次，用时：{1}", (i + 1), (DateTime.Now - sDate).TotalMilliseconds / 1000.0));

                // 100w 数据量明显，清掉后内存会及时释放
                result.Clear();
                result = null;

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
                    .GetTable<Model.Client>()
                    .Include(a => a.Accounts)
                    .ToList();
                // Console.WriteLine(string.Format("第 {0} 次，用时：{1}", 1, (DateTime.Now - sDate).TotalMilliseconds / 1000.0));
            }
            stop.Stop();
            Console.WriteLine(string.Format("运行 100 次 2000 行主从数据，用时：{0}", stop.Elapsed));

            stop = new Stopwatch();
            stop.Start();
            for (int i = 0; i < 100; i++)
            {
                DateTime sDate = DateTime.Now;
                var result = context
                    .GetTable<Model.Client>()
                    .Include(a => a.Accounts)
                    .Include(a => a.Accounts[0].Markets)
                    .ToList();
                //Console.WriteLine(string.Format("第 {0} 次，用时：{1}", 1, (DateTime.Now - sDate).TotalMilliseconds / 1000.0));
            }
            stop.Stop();
            Console.WriteLine(string.Format("运行 100 次 2000 行主从孙数据，用时：{0}", stop.Elapsed));
            //Console.ReadLine();
        }

        /// <summary>
        /// 有参构造函数查询
        /// </summary>
        protected virtual void QueryWithParameterizedConstructor()
        {

        }
    }

    public interface ITest
    {
        void Run(DatabaseType dbType);
    }
}
