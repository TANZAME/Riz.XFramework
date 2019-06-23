
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
                            writer.Write("N\"");
                            writer.Write(p.ParameterName);
                            writer.Write(" = ");
                            writer.Write((p.Value ?? string.Empty).ToString().Replace("\"", "\"\""));
                            writer.Write(", ");
                            writer.Write("DbType = {0}, ", p.DbType);
                            writer.Write("Size = {0}, ", p.Size);
                            writer.Write("Precision = {0}, ", p.Precision);
                            writer.Write("Scale = {0}, ", p.Scale);
                            writer.Write("Direction = {0}", p.Direction);
                            writer.Write("\"");
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
                fileName = baseDirectory + @"\" + databaseType + "Log.sql";
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
    }
}