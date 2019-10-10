
using System;
using TZM.XFramework.Data;
using System.Data;

namespace TZM.XFramework.UnitTest
{
    public class Program
    {
        // 包还原失败参考
        // https://docs.microsoft.com/zh-cn/nuget/consume-packages/package-restore-troubleshooting#missing

        [MTAThread]
        //[STAThread]
        public static void Main(string[] args)
        {
            var g1 = new { Name = 22 };
            Type t1 = typeof(Guid);
            Type t2 = typeof(Guid?);
            var c1 = Type.GetTypeCode(t1);
            var c2 = Type.GetTypeCode(t2);

            int m = 1;
            bool testAll = false;
            string fileName = string.Empty;
            string line = args.Length > 0 ? args[0] : string.Empty;

            if (!string.IsNullOrEmpty(line))
            {
                args = line.Split(';');
                if (args.Length > 0) m = Convert.ToInt32(args[0]);
                if (args.Length > 1) testAll = args[1] == "1" ? true : false;
            }

            ITest test = null;
            DatabaseType databaseType = DatabaseType.None;

            // 命令拦截
            var interceptor = new DbCommandInterceptor
            {
                OnExecuting = cmd =>
                {
                    var writer = System.IO.File.AppendText(fileName);
                    writer.WriteLine(cmd.CommandText);
                    if (cmd.Parameters != null)
                    {
                        for (int i = 0; i < cmd.Parameters.Count; i++)
                        {
                            IDbDataParameter p = (IDbDataParameter)cmd.Parameters[i];
                            writer.Write("-- ");
                            writer.Write(p.ParameterName);
                            writer.Write(" = ");
                            writer.Write((p.Value ?? string.Empty));
                            writer.Write(", DbType = {0}, ", p.DbType);
                            if (p.Size != default(int)) writer.Write("Size = {0}, ", p.Size);
                            if (p.Precision != default(byte)) writer.Write("Precision = {0}, ", p.Precision);
                            if (p.Scale != default(byte)) writer.Write("Scale = {0}, ", p.Scale);
                            if (p.Direction != ParameterDirection.Input) writer.Write("Direction = {0}, ", p.Direction);
                            writer.WriteLine();
                            if (i == cmd.Parameters.Count - 1) writer.WriteLine();
                        }
                    }

                    writer.Close();
                },
                OnExecuted = cmd => { }
            };
            DbInterception.Add(interceptor);

            for (int i = 1; i <= 4; i++)
            {
                if (testAll) m = i;

                if (m == 1)
                {
                    test = new SqlServer.SqlServerTest();
                    databaseType = DatabaseType.SqlServer;
                }
                else if (m == 2)
                {
                    test = new MySql.MySqlTest();
                    databaseType = DatabaseType.MySql;
                }
                else if (m == 3)
                {
                    test = new Oracle.OracleTest();
                    databaseType = DatabaseType.Oracle;
                }
                else if (m == 4)
                {
                    test = new Postgre.PostgreTest();
                    databaseType = DatabaseType.Postgre;
                }
                //else if (m == 5)
                //{
                //    test = new SQLite.SQLiteTest();
                //    databaseType = DatabaseType.SQLite;
                //}

                string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                fileName = baseDirectory + @"\Log_" + databaseType + ".sql";
                if (System.IO.File.Exists(fileName)) System.IO.File.Delete(fileName);

                if (test != null)
                {
                    Console.WriteLine(databaseType + " BEGIN");
                    test.Run(databaseType);
                    Console.WriteLine(databaseType + " END");
                }

                if (!testAll) break;
            }

            Console.WriteLine("回车退出~");
            Console.ReadLine();
        }

        public class A
        {
            public void Write()
            {
                Console.WriteLine("A");
            }
        }

        public class B : A 
        {
            public new void Write()
            {
                Console.WriteLine("B");
            }
        }
    }
}