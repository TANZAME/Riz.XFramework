
using System;
using System.Linq;
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
            bool isDebug = false;
            bool caseSensitive = true;
            ITest test = null;
            string fileName = string.Empty;
            DatabaseType databaseType = DatabaseType.None;
            string s = args != null && args.Length > 0 ? args[0] : null;
            if (!string.IsNullOrEmpty(s)) databaseType = (DatabaseType)Convert.ToByte(s);

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
                            writer.Write(p.Value == null ? string.Empty : (p.Value is byte[] ? Common.BytesToHex((byte[])p.Value, true, true) : p.Value));
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

            foreach (DatabaseType item in Enum.GetValues(typeof(DatabaseType)))
            {
                if (item == DatabaseType.None) continue;

                DatabaseType myDatabaseType = item;
                if (!string.IsNullOrEmpty(s)) myDatabaseType = databaseType;

                var obj = Activator.CreateInstance(null, string.Format("TZM.XFramework.UnitTest.{0}.{0}Test", myDatabaseType));
                test = (ITest)(obj.Unwrap());
                test.IsDebug = isDebug;
                test.CaseSensitive = caseSensitive;

                string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                fileName = baseDirectory + @"\Log_" + myDatabaseType + ".sql";
                if (System.IO.File.Exists(fileName)) System.IO.File.Delete(fileName);

                if (test != null)
                {
                    Console.WriteLine(myDatabaseType + " BEGIN");
                    test.Run(myDatabaseType);
                    Console.WriteLine(myDatabaseType + " END");
                }

                if (!string.IsNullOrEmpty(s)) break;
            }

            Console.WriteLine("回车退出~");
            Console.ReadLine();
        }
    }
}