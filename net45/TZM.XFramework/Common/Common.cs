using System;
using System.Text;
using System.Linq;
using System.Configuration;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Security.Cryptography;

namespace TZM.XFramework
{
    /// <summary>
    /// 公共帮助类
    /// </summary>
    public class Common
    {
        // Mono.Cecil
        // 

        static string _autoClassName = "<>c__DisplayClass";
        static DateTime _utcMinDate = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        #region 静态属性

        /// <summary>
        /// 验证方式
        /// </summary>
        public static string AuthorizeScheme = "Basic";

        /// <summary>
        /// 日期格式
        /// </summary>
        public static string DateFormat = "yyyy-MM-dd";

        /// <summary>
        /// 时间格式
        /// </summary>
        public static string TimeFormat = "yyyy-MM-dd HH:mm";

        /// <summary>
        /// 长时间格式
        /// </summary>
        public static string LongTimeFormat = "yyyy-MM-dd HH:mm:ss";

        /// <summary>
        /// 金额格式
        /// </summary>
        public static string MoneyFormat = "0.00";

        /// <summary>
        /// 系统本地最小时间
        /// </summary>
        public static DateTime MinDate = new DateTime(1970, 1, 1, 0, 0, 0);

        /// <summary>
        /// 获取当前应用程序默认配置的数据。
        /// </summary>
        public static NameValueCollection AppSettings { get { return ConfigurationManager.AppSettings; } }

        #endregion

        #region 其它方法

        /// <summary>
        /// 获取指定键值的连接字符串
        /// </summary>
        /// <param name="key">连接键</param>
        /// <returns></returns>
        public static string GetConnectionString(string key)
        {
            return ConfigurationManager.ConnectionStrings[key].ConnectionString;
        }

        /// <summary>
        /// 时间戳转时间类型
        /// </summary>
        /// <typeparam name="T">时间戳值类型</typeparam>
        /// <param name="source">时间戳</param>
        /// <param name="datePart">时间戳单位（秒/毫秒/计时周期）</param>
        /// <param name="destinationTimeZoneId">目标时区，空或者 Local 表示本地时间（常用 UTC，Local，Pacific Standard Time）</param>
        /// <returns></returns>
        public static DateTime ConvertToDateTime<T>(T source, DatePart datePart = DatePart.Millisecond, string destinationTimeZoneId = null) where T : struct
        {
            // 时间戳是自 1970 年 1 月 1 日（00:00:00 GMT）以来的秒数。它也被称为 Unix 时间戳（Unix Timestamp）。
            // Unix时间戳(Unix timestamp)，或称Unix时间(Unix time)、POSIX时间(POSIX time)，是一种时间表示方式，定义为从格林威治时间1970年01月01日00时00分00秒起至现在的总秒数

            DateTime result;
            DateTime s = _utcMinDate;

            if (datePart == DatePart.Second) result = s.AddSeconds(Convert.ToDouble(source));
            else if (datePart == DatePart.Millisecond) result = s.AddMilliseconds(Convert.ToDouble(source));
            else if (datePart == DatePart.Tick) result = s.AddTicks(Convert.ToInt64(source));
            else throw new NotSupportedException(datePart + " is not a support type.");

            if (destinationTimeZoneId == null || destinationTimeZoneId == "Local")
                result = result.ToLocalTime();
            else if (destinationTimeZoneId != "UTC")
                result = Common.ConvertDateTime(result, "UTC", destinationTimeZoneId);

            return result;
        }

        /// <summary>
        /// 时间转时间戳
        /// </summary>
        /// <param name="source">源时间</param>
        /// <param name="sourceTimeZoneId">源时区，空或者 Local 表示本地时间（常用 UTC，Local，Pacific Standard Time）</param>
        /// <param name="dayPart">时间戳单位（秒/毫秒/计时周期）</param>
        /// <returns></returns>
        public static long ConvertToTimeStamp(DateTime source, string sourceTimeZoneId = null, DatePart dayPart = DatePart.Millisecond)
        {
            // 转至UTC时区
            if (sourceTimeZoneId != "UTC") source = Common.ConvertDateTime(source, sourceTimeZoneId, "UTC");
            switch (dayPart)
            {
                case DatePart.Second:
                    return (source.Ticks - 621355968000000000L) / 10000000;
                case DatePart.Millisecond:
                    return (source.Ticks - 621355968000000000L) / 10000;
                case DatePart.Tick:
                    return source.Ticks - 621355968000000000L;
                default:
                    throw new NotSupportedException(dayPart + " is not a support type.");
            }
        }

        /// <summary>
        /// 不同时区时间转换
        /// </summary>
        /// <param name="source">时间</param>
        /// <param name="sourceTimeZoneId">源时区，空或者 Local 表示本地时间（常用 UTC，Local，Pacific Standard Time）</param>
        /// <param name="destinationTimeZoneId">目标时区，空或者 Local 表示本地时间（常用 UTC，Local，Pacific Standard Time）</param>
        /// <returns></returns>
        public static DateTime ConvertDateTime(DateTime source, string sourceTimeZoneId = "UTC", string destinationTimeZoneId = "Pacific Standard Time")
        {
            DateTime newDateTime = new DateTime(source.Ticks);

            if (sourceTimeZoneId == null || sourceTimeZoneId == "Local")
                sourceTimeZoneId = TimeZoneInfo.Local.Id;

            if (destinationTimeZoneId == null || destinationTimeZoneId == "Local")
                destinationTimeZoneId = TimeZoneInfo.Local.Id;

            return TimeZoneInfo.ConvertTimeBySystemTimeZoneId(newDateTime, sourceTimeZoneId, destinationTimeZoneId);
        }

        /// <summary>
        /// 判断是否为空日期
        /// </summary>
        /// <param name="source">来源日期</param>
        /// <returns></returns>
        public static bool IsEmptyDate(DateTime source)
        {
            if (source == DateTime.MinValue) return true;

            string s = source.ToString("yyyy-MM-dd HH:mm:ss");
            if (s == "0001-01-01 00:00:00") return true;
            else if (s == "1900-01-01 00:00:00") return true;
            else if (s == "1970-01-01 00:00:00") return true;
            else return false;
        }

        /// <summary>
        /// 尝试将逻辑值的指定字符串表示形式转换为它的等效值，如果不能转换则使用传入的默认值.
        /// </summary>
        /// <param name="s">字符值</param>
        /// <param name="default">传入的默认值</param>
        /// <returns></returns>
        public static T TryParse<T>(string s, T @default)
            where T : struct
        {
            if (string.IsNullOrEmpty(s)) 
                return @default;

            T result = @default;
            if (typeof(T) == typeof(short))
            {
                short value;
                if (!short.TryParse(s, out value)) value = (short)((object)@default);
                result = (T)((object)value);
            }
            else if (typeof(T) == typeof(int))
            {
                int value;
                if (!int.TryParse(s, out value)) value = (int)((object)@default);
                result = (T)((object)value);
            }
            else if (typeof(T) == typeof(long))
            {
                long value;
                if (!long.TryParse(s, out value)) value = (long)((object)@default);
                result = (T)((object)value);
            }
            else if (typeof(T) == typeof(float))
            {
                float value;
                if (!float.TryParse(s, out value)) value = (float)((object)@default);
                result = (T)((object)value);
            }
            else if (typeof(T) == typeof(double))
            {
                double value;
                if (!double.TryParse(s, out value)) value = (double)((object)@default);
                result = (T)((object)value);
            }
            else if (typeof(T) == typeof(decimal))
            {
                decimal value;
                if (!decimal.TryParse(s, out value)) value = (decimal)((object)@default);
                result = (T)((object)value);
            }
            else if (typeof(T) == typeof(byte))
            {
                byte value;
                if (!byte.TryParse(s, out value)) value = (byte)((object)@default);
                result = (T)((object)value);
            }
            else if (typeof(T) == typeof(bool))
            {
                bool value;
                if (!bool.TryParse(s, out value)) value = (bool)((object)@default);
                result = (T)((object)value);
            }
            else if (typeof(T) == typeof(DateTime))
            {
                DateTime value;
                if (!DateTime.TryParse(s, out value)) value = (DateTime)((object)@default);
                result = (T)((object)value);
            }

            return result;
        }

        /// <summary>
        /// 根据表达式取得属性名称
        /// </summary>
        public static string Name<T>(Expression<Func<T, object>> lambda)
        {
            var navs = new List<string>();
            Expression node = (lambda as LambdaExpression).Body;
            if (node.NodeType == ExpressionType.New) node = (node as NewExpression).Arguments[0];
            if (node.NodeType == ExpressionType.Convert) node = (node as UnaryExpression).Operand;

            while (node != null)
            {
                if (node.NodeType != ExpressionType.MemberAccess) node = null;
                else
                {
                    var m = node as MemberExpression;
                    if (m.Expression != null && m.Expression.Type.Name.StartsWith(_autoClassName, StringComparison.Ordinal)) node = null;
                    else
                    {
                        navs.Add(m.Member.Name);
                        node = m.Expression;
                        if (node.NodeType == ExpressionType.Call) node = (node as MethodCallExpression).Object;
                    }
                }
            }

            navs.Reverse();
            string nav = string.Join(".", navs);
            return nav;
        }

        /// <summary>
        /// 将字符串转为 16 进制形式
        /// </summary>
        /// <param name="source">来源字符串</param>
        /// <param name="encoding">编码，如果不传则使用 UTF8</param>
        /// <param name="upper">转为大写形式</param>
        /// <returns></returns>
        public static string StringToHex(string source, Encoding encoding = null, bool upper = false)
        {
            byte[] buffer = (encoding ?? Encoding.UTF8).GetBytes(source);

            var builder = new StringBuilder();
            foreach (var c in buffer)
            {
                string hex = Convert.ToString(c, 16);
                if (hex.Length == 1) builder.Append("0");
                builder.Append(upper ? hex.ToUpper() : hex);
            }

            return builder.ToString();
        }

        /// <summary>
        /// 将字符串转为 16 进制形式
        /// </summary>
        /// <param name="buffer">字节序列</param>
        /// <param name="append0x">返回的字符串前面加上 0x</param>
        /// <param name="upper">转为大写形式</param>
        /// <returns></returns>
        public static string BytesToHex(byte[] buffer, bool append0x = true, bool upper = false)
        {
            if (buffer == null) return null;
            if (buffer.Length == 0) return append0x ? "0x" : string.Empty;

            var builder = new StringBuilder(append0x && buffer.Length > 0 ? "0x" : "");
            foreach (var c in buffer)
            {
                string hex = Convert.ToString(c, 16);
                if (hex.Length == 1) builder.Append("0");
                builder.Append(upper ? hex.ToUpper() : hex);
            }

            return builder.ToString();
        }

        /// <summary>
        /// 将byte[]转换成int
        /// </summary>
        /// <param name="data">需要转换成整数的byte数组</param>
        /// <returns>返回值</returns>
        public static int BytesToInt32(byte[] data)
        {
            //如果传入的字节数组长度小于4,则返回0
            if (data.Length < 4) return 0;

            //定义要返回的整数
            int num = 0;

            //如果传入的字节数组长度大于4,需要进行处理
            if (data.Length >= 4)
            {
                //创建一个临时缓冲区
                var tempBuffer = new byte[4];

                //将传入的字节数组的前4个字节复制到临时缓冲区
                Buffer.BlockCopy(data, 0, tempBuffer, 0, 4);

                //将临时缓冲区的值转换成整数，并赋给num
                num = BitConverter.ToInt32(tempBuffer, 0);
            }

            //返回整数
            return num;
        }

        /// <summary>
        /// 16进制字符串转为明文
        /// </summary>
        /// <param name="hex">16进制字符串</param>
        /// <param name="encoding">编码，如果不传则使用 UTF8</param>
        /// <returns></returns>
        public static string HexToString(string hex, Encoding encoding = null)
        {
            hex = hex ?? string.Empty;
            encoding = encoding ?? Encoding.UTF8;

            string s = "";
            byte[] bytes = new byte[hex.Length / 2];
            for (int i = 0; i < hex.Length / 2; i++)
            {
                s = hex.Substring(i * 2, 2);
                bytes[i] = Convert.ToByte(s, 16);
            }
            //按照指定编码将字节数组变为字符串
            return encoding.GetString(bytes);
        }

        /// <summary>
        /// 将源字符串的双字节部分（如中文字符）转为 \u 的unicode形式
        /// </summary>
        /// <param name="source">源字符串</param>
        /// <returns></returns>
        public static string ToUnicode(string source)
        {
            if (string.IsNullOrEmpty(source)) return string.Empty;

            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            for (int index = 0; index < source.Length; index++)
            {
                char c = source[index];
                bool isChinese = (int)c > 127;
                if (isChinese)
                {
                    var bytes = System.Text.Encoding.Unicode.GetBytes(new char[] { c });
                    for (var i = 0; i < bytes.Length; i += 2) builder.AppendFormat("\\u{0:x2}{1:x2}", bytes[i + 1], bytes[i]);
                }
                else
                {
                    builder.Append(c);
                }
            }

            return builder.ToString();
        }

        /// <summary>
        /// 格式化容量
        /// </summary>
        /// <param name="volume">容量</param>
        /// <param name="scale">小数位</param>
        public static string FormatVolume(decimal volume, int scale = 1)
        {
            if (volume > 1024 * 1024 * 1024)
                return Convert.ToString(Math.Round(volume / (1024 * 1024 * 1024), scale)) + " G";
            else if (volume > 1024 * 1024)
                return Convert.ToString(Math.Round(volume / (1024 * 1024), scale)) + " M";
            else return Convert.ToString(Math.Round(volume / 1024, scale)) + " KB";
        }

        /// <summary>
        /// 根据页长计算总页码
        /// </summary>
        /// <param name="rowCount">数据总数</param>
        /// <param name="pageSize">页码</param>
        /// <returns></returns>
        public static int Page(int rowCount, int pageSize)
        {
            return ~~((rowCount - 1) / pageSize) + 1;
        }

        /// <summary>
        /// 根据IP地址转换为long类型
        /// </summary>
        /// <param name="address">ip地址</param>
        /// <returns></returns>
        public static long ConvertIpToInt64(string address)
        {
            if (string.IsNullOrEmpty(address)) return 0;

            int index = address.LastIndexOf('/');
            if (index > 0) address = address.Substring(0, index);

            char[] separator = new char[] { '.' };
            string[] items = address.Split(separator);
            return long.Parse(items[0]) << 24
                    | long.Parse(items[1]) << 16
                    | long.Parse(items[2]) << 8
                    | long.Parse(items[3]);
        }

        /// <summary>
        /// 将 long 类型转为IP地址
        /// </summary>
        /// <param name="value">long 类型的地址</param>
        /// <returns></returns>
        public static string ConvertToIp(long value)
        {
            var builder = new StringBuilder();
            builder.Append((value >> 24) & 0xFF).Append(".");
            builder.Append((value >> 16) & 0xFF).Append(".");
            builder.Append((value >> 8) & 0xFF).Append(".");
            builder.Append(value & 0xFF);
            return builder.ToString();
        }

        /// <summary>
        /// 字符串转 64 位整数，默认使用 SHA 256
        /// </summary>
        /// <param name="text">来源字符串</param>
        /// <returns></returns>
        public static ulong ConvertHashToInt64(string text)
        {
            return Common.ConvertHashToInt64(SHA256.Create(), text);
        }

        /// <summary>
        /// 字符串转 64 位整数
        /// </summary>
        /// <param name="hasher">哈希算法</param>
        /// <param name="text">来源字符串</param>
        /// <returns></returns>
        public static ulong ConvertHashToInt64(HashAlgorithm hasher, string text)
        {
            using (hasher)
            {
                var bytes = hasher.ComputeHash(Encoding.Default.GetBytes(text));
                return Enumerable.Range(0, bytes.Length / 8) //8 bytes in an 64 bit interger
                    .Select(i => BitConverter.ToUInt64(bytes, i * 8))
                    .Aggregate((x, y) => x ^ y);
            }
        }

        #endregion

        ///// <summary>
        ///// 全屏截图
        ///// </summary>
        ///// <returns></returns>
        //public static Image FullScreenShot()
        //{
        //    Image image = new Bitmap(System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width, System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height);
        //    using (Graphics g = Graphics.FromImage(image))
        //    {
        //        g.CopyFromScreen(new Point(0, 0), new Point(0, 0), System.Windows.Forms.Screen.PrimaryScreen.Bounds.Size);
        //    }

        //    return image;
        //}

        ///// <summary>
        ///// 从视频文件截图,生成在视频文件所在文件夹
        ///// 在Web.Config 中需要两个前置配置项:
        ///// 1.ffmpeg.exe文件的路径
        ///// <add key="ffmpeg" value="E:\ffmpeg\ffmpeg.exe" />
        ///// 2.截图的尺寸大小
        ///// <add key="CatchFlvImgSize" value="240x180" />
        ///// 3.视频处理程序ffmpeg.exe
        ///// </summary>
        ///// <param name="vFileName">视频文件地址,如:/Web/FlvFile/User1/00001.Flv</param>
        ///// <returns>成功:返回图片虚拟地址; 失败:返回空字符串</returns>
        //public string CatchImg(string vFileName)
        // {
        //    //取得ffmpeg.exe的路径,路径配置在Web.Config中,如:<add key="ffmpeg" value="E:\ffmpeg\ffmpeg.exe" />
        //    string ffmpeg=System.Configuration.ConfigurationSettings.AppSettings["ffmpeg"];
        //    if ( (!System.IO.File.Exists(ffmpeg)) || (!System.IO.File.Exists(vFileName)) )
        //     {
        //    return "";
        //     }
        //    //获得图片相对路径/最后存储到数据库的路径,如:/Web/FlvFile/User1/00001.jpg
        //    string flv_img = System.IO.Path.ChangeExtension(vFileName,".jpg") ;
        //    //图片绝对路径,如:D:\Video\Web\FlvFile\User1\0001.jpg
        //    string flv_img_p = HttpContext.Current.Server.MapPath(flv_img);
        //    //截图的尺寸大小,配置在Web.Config中,如:<add key="CatchFlvImgSize" value="240x180" />
        //    string FlvImgSize=System.Configuration.ConfigurationSettings.AppSettings["CatchFlvImgSize"];
        //     System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo(ffmpeg);
        //     startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden; 
        //    //此处组合成ffmpeg.exe文件需要的参数即可,此处命令在ffmpeg 0.4.9调试通过
        //    startInfo.Arguments = " -i " vFileName " -y -f image2 -t 0.001 -s " FlvImgSize " " flv_img_p ;
        //    try 
        //     {
        //     System.Diagnostics.Process.Start(startInfo);
        //     }
        //    catch
        //     {
        //    return "";
        //     }
        //    ///注意:图片截取成功后,数据由内存缓存写到磁盘需要时间较长,大概在3,4秒甚至更长;
        //    ///这儿需要延时后再检测,我服务器延时8秒,即如果超过8秒图片仍不存在,认为截图失败;
        //    ///此处略去延时代码.如有那位知道如何捕捉ffmpeg.exe截图失败消息,请告知,先谢过!
        //    if ( System.IO.File.Exists(flv_img_p))
        //     {
        //    return flv_img; 
        //     }//51aspx
        //    return "";
        //}

    }
}
