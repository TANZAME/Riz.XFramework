using System;
using System.Text;
using System.Configuration;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace TZM.XFramework
{
    /// <summary>
    /// 公共帮助类
    /// </summary>
    public class XfwCommon
    {
        // Mono.Cecil
        // 

        #region 静态属性

        /// <summary>
        /// 验证方式
        /// </summary>
        public static string AuthScheme = "Basic";

        /// <summary>
        /// 系统最小时间
        /// </summary>
        public static DateTime MinDateTime = Convert.ToDateTime("2010-01-01");

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
        /// 获取当前应用程序默认配置的数据。
        /// </summary>
        public static NameValueCollection AppSettings { get { return ConfigurationManager.AppSettings; } }

        #endregion

        #region 其它方法

        /// <summary>
        /// 获取指定键值的连接字符串
        /// </summary>
        /// <param name="name">连接键</param>
        /// <returns></returns>
        public static string GetConnString(string name)
        {
            return ConfigurationManager.ConnectionStrings[name].ConnectionString;
        }

        /// <summary>
        /// 时间戳转为本地时间（毫秒）
        /// </summary>
        /// <returns></returns>
        public static DateTime ConvertToDateTime(long ticks)
        {
            DateTime sDate = XfwCommon.ToLocalTime(new DateTime(1970, 1, 1));
            ticks = long.Parse(ticks + "0000");
            TimeSpan ts = new TimeSpan(ticks);
            DateTime nDate = sDate.Add(ts);
            return nDate;
        }

        /// <summary>
        /// 本地时间转时间戳（毫秒）
        /// </summary>
        /// <returns></returns>
        public static long ConvertToLong(DateTime time)
        {
            DateTime sDate = XfwCommon.ToLocalTime(new DateTime(1970, 1, 1));
            TimeSpan ts = time.Subtract(sDate);
            long ticks = ts.Ticks;
            ticks = long.Parse(ticks.ToString().Substring(0, ticks.ToString().Length - 4));
            return ticks;
        }

        /// <summary>
        /// 时间戳转为本地时间（秒）
        /// </summary>
        /// <param name="sec">是已秒做单位的时间戳</param>
        /// <returns></returns>
        public static DateTime ConvertToDateTimeSec(string sec)
        {
            DateTime sDate = XfwCommon.ToLocalTime(new DateTime(1970, 1, 1));
            long ticks = long.Parse(sec + "0000000");
            TimeSpan toNow = new TimeSpan(ticks);
            DateTime nDate = sDate.Add(toNow);
            return nDate;
        }

        /// <summary>
        /// 本地时间转时间戳（秒）
        /// </summary>
        public static long ConvertToLongSec(string date)
        {
            System.DateTime eDate = DateTime.Parse(date);
            System.DateTime sDate = XfwCommon.ToLocalTime(new DateTime(1970, 1, 1));
            return (long)(eDate - sDate).TotalSeconds;
        }

        /// <summary>
        /// 本地时间转时间戳（秒）
        /// </summary>
        public static long ConvertToLongSec(DateTime date)
        {
            System.DateTime sDate = XfwCommon.ToLocalTime(new DateTime(1970, 1, 1));
            return (long)(date - sDate).TotalSeconds;
        }

        // 将客户端时间转换为服务端本地时间
        static DateTime ToLocalTime(DateTime clientTime)
        {
#if netcore
            DateTime serverTime1 = TimeZoneInfo.ConvertTime(clientTime, TimeZoneInfo.Local);
#else
            DateTime serverTime1 = TimeZone.CurrentTimeZone.ToLocalTime(clientTime);
#endif
            return serverTime1;
        }

        // unix时间戳是从1970年1月1日（UTC/GMT的午夜）开始所经过的秒数

        /// <summary>
        /// long 型转日期 Unix
        /// </summary>
        public static DateTime ConvertToDateTimeUnix(long d)
        {
            DateTime date = new DateTime(d * 10000000 + 621355968000000000);
            return date;
        }

        /// <summary>
        /// long 型转日期 Unix
        /// </summary>
        /// <returns></returns>
        public static DateTime ConvertToDateTimeUnix(string s)
        {
            long d = long.Parse(s);
            return XfwCommon.ConvertToDateTimeUnix(d);
        }

        /// <summary>
        /// 日期转long型 Unix
        /// </summary>
        public static long ConvertToLongUnix(DateTime date)
        {
            return (date.Ticks - 621355968000000000) / 10000000;
        }

        /// <summary>
        /// 将数字的字符串表示形式转换为它的等效 32 位有符号整数,如果转换不成功,则使用传入的默认值.
        /// </summary>
        /// <param name="s">字符值</param>
        /// <param name="vDefault">传入的默认值</param>
        /// <returns></returns>
        public static int TryParse(string s, int vDefault)
        {
            int result;
            if (!int.TryParse(s, out result))
            {
                result = vDefault;
            }
            return result;
        }

        /// <summary>
        /// 尝试将逻辑值的指定字符串表示形式转换为它的等效 System.Boolean 值,则使用传入的默认值.
        /// </summary>
        /// <param name="s">字符值</param>
        /// <param name="vDefault">传入的默认值</param>
        /// <returns></returns>
        public static bool TryParse(string s, bool vDefault)
        {
            bool result;
            if (!bool.TryParse(s, out result))
            {
                result = vDefault;
            }
            return result;
        }

        /// <summary>
        /// 根据表达式取得属性名称
        /// </summary>
        public static string Name<T>(System.Linq.Expressions.Expression<Func<T, object>> lambda)
        {

            List<string> navs = new List<string>();
            Expression node = (lambda as LambdaExpression).Body;
            if (node.NodeType == ExpressionType.New) node = (node as NewExpression).Arguments[0];
            if (node.NodeType == System.Linq.Expressions.ExpressionType.Convert) node = (node as System.Linq.Expressions.UnaryExpression).Operand;
            string closure = "<>c__DisplayClass";
            while (node != null)
            {
                if (node.NodeType == ExpressionType.MemberAccess)
                {
                    MemberExpression m = node as MemberExpression;
                    if (m.Expression != null && m.Expression.Type.Name.StartsWith(closure))
                    {
                        node = null;
                    }
                    else
                    {
                        navs.Add(m.Member.Name);
                        node = m.Expression;
                    }
                }
                else
                {
                    node = null;
                }
            }
            navs.Reverse();
            string nav = string.Join(".", navs);
            return nav;
        }

        /// <summary>
        /// 将字符串转为 16 进制形式
        /// </summary>
        public static string StringToHex(string str, Encoding encoding = null, bool upper = false)
        {
            byte[] buffer = (encoding ?? Encoding.UTF8).GetBytes(str);

            StringBuilder builder = new StringBuilder();
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
        public static string HexToString(string hs, Encoding encoding = null, bool upper = false)
        {
            hs = hs ?? string.Empty;
            encoding = encoding ?? Encoding.UTF8;

            string strTemp = "";
            byte[] b = new byte[hs.Length / 2];
            for (int i = 0; i < hs.Length / 2; i++)
            {
                strTemp = hs.Substring(i * 2, 2);
                b[i] = Convert.ToByte(strTemp, 16);
            }
            //按照指定编码将字节数组变为字符串
            return encoding.GetString(b);
        }

        /// <summary>
        /// 将源字符串的双字节部分（如中文字符）转为 \u 的unicode形式
        /// </summary>
        public static string ToUnicode(string str)
        {
            if (string.IsNullOrEmpty(str)) return string.Empty;

            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            for (int index = 0; index < str.Length; index++)
            {
                char c = str[index];
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
        /// <param name="n">小数位</param>
        public static string FormatVolume(decimal volume, int n = 1)
        {
            string result = string.Empty;
            result = volume < 1024 ? volume.ToString() : (volume / 1024).ToString();

            string[] d = result.Split('.');
            if (d.Length == 2 && d[1].Length > n)
            {
                d[1] = d[1].Substring(0, n);
                result = string.Format("{0}.{1}", d[0], d[1]);
            }

            string u = volume < 1024 ? " M" : " G";
            return result + u;
        }

        /// <summary>
        /// 根据页长计算总页码
        /// </summary>
        /// <param name="count">数据总数</param>
        /// <param name="pageSize">页码</param>
        /// <returns></returns>
        public static int Page(int count, int pageSize)
        {
            int page = count % pageSize == 0 ? count / pageSize : (count / pageSize + 1);
            return page;
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
