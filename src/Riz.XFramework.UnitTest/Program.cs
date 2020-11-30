
using System;
using System.Linq;
using Riz.XFramework.Data;
using System.Data;
using System.Collections.Generic;

namespace Riz.XFramework.UnitTest
{
    public class Program
    {
        // 如果在尝试进行 COM 上下文转换期间检测到一个死锁，将激活 contextSwitchDeadlock 托管调试助手 (MDA)。
        // https://docs.microsoft.com/zh-cn/dotnet/framework/debug-trace-profile/contextswitchdeadlock-mda

        [MTAThread]
        //[STAThread]
        public static void Main(string[] args)
        {
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
                            writer.Write(p.Value == null ? string.Empty : (p.Value is byte[]? Common.BytesToHex((byte[])p.Value, true, true) : p.Value));
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


#if net40
            Console.WriteLine("- net 40 -");
#endif
#if net45
            Console.WriteLine("- net 45 -");
#endif
#if netcore
            Console.WriteLine("- net core -");
#endif

            foreach (DatabaseType item in Enum.GetValues(typeof(DatabaseType)))
            {
                if (item == DatabaseType.None) continue;

                DatabaseType myDatabaseType = item;
                if (!string.IsNullOrEmpty(s)) myDatabaseType = databaseType;

                string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                fileName = baseDirectory + @"\Log_" + myDatabaseType + ".sql";
                if (System.IO.File.Exists(fileName)) System.IO.File.Delete(fileName);

                List<Option> options = new List<Option>
                {
                    new Option { WithNameAttribute = false, IsDebug = true, CaseSensitive = false },
                    new Option { WithNameAttribute = true, IsDebug = false, CaseSensitive = false },
                };
                if (myDatabaseType == DatabaseType.Oracle || myDatabaseType == DatabaseType.Postgre)
                {
                    options.Add(new Option { WithNameAttribute = false, IsDebug = true, CaseSensitive = true });
                    options.Add(new Option { WithNameAttribute = true, IsDebug = true, CaseSensitive = true });
                }

                foreach (var opt in options)
                {
                    var obj = Activator.CreateInstance(null, string.Format("Riz.XFramework.UnitTest.{0}.{0}Test{1}", myDatabaseType, opt.WithNameAttribute ? "_NA" : string.Empty));
                    test = (ITest)(obj.Unwrap());
                    test.IsDebug = opt.IsDebug;
                    test.CaseSensitive = opt.CaseSensitive;

                    Console.WriteLine("================ " + myDatabaseType + " Begin ================");
                    Console.WriteLine(string.Format("WithNameAttribute => {0}，IsDebug => {1}，CaseSensitive => {2} ", opt.WithNameAttribute, opt.IsDebug, opt.CaseSensitive));
                    test.Run(myDatabaseType);
                    Console.WriteLine("================ " + myDatabaseType + " END   ================");
                    Console.WriteLine(string.Empty);

                    if (!string.IsNullOrEmpty(s)) break;
                }
            }

            Console.WriteLine("回车退出~");
            Console.ReadLine();
        }

        class Option
        {
            /// <summary>
            /// 成员属性使用 Name 特性
            /// </summary>
            public bool WithNameAttribute { get; set; }

            /// <summary>
            /// 调试模式
            /// </summary>
            public bool IsDebug { get; set; }

            /// <summary>
            /// 是否区分大小写，适用 ORCACLE 和 POSTGRESQL
            /// </summary>
            public bool CaseSensitive { get; set; }
        }
    }
}