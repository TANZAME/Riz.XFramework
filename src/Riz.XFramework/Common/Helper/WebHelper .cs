
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Drawing;
using System.Collections.Generic;
using System.IO.Compression;

namespace Riz.XFramework
{
    /// <summary>
    /// WEB助手类
    /// </summary>
    public partial class WebHelper
    {
        #region 图片

        /// <summary>
        /// 生成缩略图，并把缩略图缩放到指定的大小，缩放要求不变形、不裁剪
        /// </summary>
        /// <param name="fileName">图片文件完全路径</param>
        /// <param name="destWidth">目标宽度</param>
        /// <param name="destHeight">目标高度</param>
        public static Image CreateThumbnail(string fileName, int destWidth, int destHeight)
        {
            if (!File.Exists(fileName) || destWidth == 0 || destHeight == 0) return null;
            Image srcImage = Image.FromFile(fileName);
            return CreateThumbnail(srcImage, destWidth, destHeight);
        }

        /// <summary>
        /// 生成缩略图，并把缩略图缩放到指定的大小，缩放要求不变形、不裁剪
        /// </summary>
        /// <param name="srcImage">来源图片</param>
        /// <param name="destWidth">目标宽度</param>
        /// <param name="destHeight">目标高度</param>
        public static Image CreateThumbnail(Image srcImage, int destWidth, int destHeight)
        {
            if (destWidth == 0 || destHeight == 0 || srcImage == null) return null;

            bool wasDispose = true;
            Bitmap bmp = null;
            System.Drawing.Graphics g = null;

            try
            {
                //原始图片的宽和高
                int srcWidth = srcImage.Width;
                int srcHeight = srcImage.Height;
                //如果不需要缩放，直接返回图片
                if (srcWidth <= destWidth && srcHeight <= destHeight)
                {
                    wasDispose = false;
                    return srcImage;
                }

                //由于原始图片的长宽比例跟指定缩略图的长宽比例可能不一样，所以最后缩放出来的图片的大小跟指定的长宽不一定
                //这个要根据原始图片和要求缩略图的比例来决定
                if ((float)srcWidth / (float)srcHeight >= (float)destWidth / (float)destHeight)
                {
                    destHeight = srcHeight * destWidth / srcWidth; //先乘后整除
                }
                else
                {
                    destWidth = srcWidth * destHeight / srcHeight;
                }


                //接着创建一个System.Drawing.Bitmap对象，并设置你希望的缩略图的宽度和高度。
                bmp = new Bitmap(destWidth, destHeight);

                //从Bitmap创建一个System.Drawing.Graphics对象，用来绘制高质量的缩小图。
                g = System.Drawing.Graphics.FromImage(bmp);
                //设置 System.Drawing.Graphics对象的SmoothingMode属性为HighQuality
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.High;
                //把原始图像绘制成上面所设置宽高的缩小图
                System.Drawing.Rectangle desRec = new System.Drawing.Rectangle(0, 0, destWidth, destHeight);
                g.DrawImage(srcImage, desRec, 0, 0, srcWidth, srcHeight, GraphicsUnit.Pixel);
            }
            catch
            {
                throw;
            }
            finally
            {
                if (g != null) g.Dispose();
                if (srcImage != null && wasDispose) srcImage.Dispose();
            }

            return bmp;
        }

        /// <summary>
        /// 生成图形验证码
        /// </summary>
        /// <param name="code">输出验证码内容</param>
        /// <param name="length">验证码长度</param>
        /// <param name="width">图片宽度</param>
        /// <param name="height">图片长度</param>
        /// <param name="fontSize">字体大小</param>
        /// <returns></returns>
        public static byte[] CreateValidateGraphic(out string code, int length, int width, int height, int fontSize)
        {
            string validCode = string.Empty;
            //顏色列表，用於驗證碼、噪線、噪點
            Color[] colors ={
                 System.Drawing.Color.Black,
                 System.Drawing.Color.Red,
                 System.Drawing.Color.Blue,
                 System.Drawing.Color.Green,
                 //System.Drawing.Color.Orange,
                 System.Drawing.Color.Brown,
                 System.Drawing.Color.Brown,
                 System.Drawing.Color.DarkBlue
            };
            //字體列表，用於驗證碼
            string[] fonts = { "Times New Roman", "MS Mincho", "Book Antiqua", "Gungsuh", "PMingLiU", "Impact" };
            //驗證碼的字元集，去掉了一些容易混淆的字元
            char[] chars = {
               '2','3','4','5','6','8','9',
               'A','B','C','D','E','F','G','H','J','K', 'L','M','N','P','R','S','T','W','X','Y'
              };
            Random random = new Random();
            Bitmap bmp = null;
            Graphics g = null;
            int index = 0;
            System.Drawing.Point p1 = default(System.Drawing.Point);
            System.Drawing.Point p2 = default(System.Drawing.Point);
            string fontName = null;
            Font font = null;
            Color color = default(Color);

            //生成驗證碼字串
            for (index = 0; index <= length - 1; index++)
            {
                validCode += chars[random.Next(chars.Length)];
            }

            bmp = new Bitmap(width, height);
            g = Graphics.FromImage(bmp);
            g.Clear(System.Drawing.Color.White);
            try
            {
                for (index = 0; index <= 4; index++)
                {
                    //畫噪線
                    p1.X = random.Next(width);
                    p1.Y = random.Next(height);
                    p2.X = random.Next(width);
                    p2.Y = random.Next(height);
                    color = colors[random.Next(colors.Length)];
                    g.DrawLine(new Pen(color), p1, p2);
                }

                float spaceWith = 0, dotX = 0, dotY = 0;
                if (length != 0)
                {
                    spaceWith = (width - fontSize * length - 10) / length;
                }

                for (index = 0; index <= validCode.Length - 1; index++)
                {
                    //畫驗證碼字串
                    fontName = fonts[random.Next(fonts.Length)];
                    font = new Font(fontName, fontSize, FontStyle.Italic);
                    color = colors[random.Next(colors.Length)];

                    dotY = (height - font.Height) / 2 + 2;//中心下移2像素
                    dotX = Convert.ToSingle(index) * fontSize + (index + 1) * spaceWith;

                    g.DrawString(validCode[index].ToString(), font, new SolidBrush(color), dotX, dotY);
                }

                for (int i = 0; i <= 30; i++)
                {
                    //畫噪點
                    int x = random.Next(bmp.Width);
                    int y = random.Next(bmp.Height);
                    Color clr = colors[random.Next(colors.Length)];
                    bmp.SetPixel(x, y, clr);
                }

                code = validCode;
                using (MemoryStream ms = new MemoryStream())
                {
                    bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                    //输出图片流
                    return ms.ToArray();
                }
                ////保存图片数据
                //MemoryStream ms = new MemoryStream();
                //bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                ////输出图片流
                //return ms.ToArray();
            }
            finally
            {
                g.Dispose();
            }
        }

        #endregion

        #region 网络

        /// <summary>
        /// 使用 GET 方式提交请求
        /// </summary>
        /// <typeparam name="T">返回类型，如果是 string 类型则直接返回原生 JSON </typeparam>
        /// <param name="uri"></param>
        /// <param name="headers">HTTP 标头的键值对</param>
        /// <returns>T</returns>
        public static T Get<T>(string uri, IDictionary<string, string> headers = null)
        {
            var configuration = new HttpConfiguration<T> { Headers = headers };
            return WebHelper.Get<T>(uri, configuration);
        }

        /// <summary>
        /// 使用 GET 方式提交请求
        /// </summary>
        /// <param name="uri">请求路径</param>
        /// <param name="configuration">HTTP 配置</param>
        public static T Get<T>(string uri, HttpConfiguration configuration)
        {
            if (configuration == null) configuration = new HttpConfiguration<T>();
            configuration.Method = HttpMethod.Get;
            var response = WebHelper.Send(uri, configuration);
            return WebHelper.ReadAsResult<T>(response, configuration);
        }

        /// <summary>
        /// 使用 POST 方式提交请求
        /// </summary>
        /// <typeparam name="T">返回类型，如果是 string 类型则直接返回原生 JSON </typeparam>
        /// <param name="uri">请求路径</param>
        /// <param name="content">参数内容，如果不是字符类型则序列化成字符串</param>
        /// <param name="headers">HTTP 标头的键值对</param>
        /// <param name="contentType">内容类型</param>
        public static T Post<T>(string uri, object content, string contentType = "application/json", IDictionary<string, string> headers = null)
        {
            var configuration = new HttpConfiguration<T>
            {
                Content = content,
                ContentType = contentType,
                Headers = headers,
            };
            return WebHelper.Post<T>(uri, configuration);
        }

        /// <summary>
        /// 使用 POST 方式提交请求，需要调用方自行释放响应对象
        /// </summary>
        /// <param name="uri">请求路径</param>
        /// <param name="configuration">HTTP 配置</param>
        public static T Post<T>(string uri, HttpConfiguration configuration)
        {
            if (configuration == null) configuration = new HttpConfiguration<T>();
            configuration.Method = HttpMethod.Post;
            var response = WebHelper.Send(uri, configuration);
            return WebHelper.ReadAsResult<T>(response, configuration);
        }

        /// <summary>
        /// 使用 DELETE 方式提交请求
        /// </summary>
        /// <typeparam name="T">返回类型，如果是 string 类型则直接返回原生 JSON </typeparam>
        /// <param name="uri">请求路径</param>
        /// <param name="content">参数内容，如果不是字符类型则序列化成字符串</param>
        /// <param name="headers">HTTP 标头的键值对</param>
        /// <param name="contentType">内容类型</param>
        public static T Delete<T>(string uri, object content, string contentType = "application/json", IDictionary<string, string> headers = null)
        {
            var configuration = new HttpConfiguration<T>
            {
                Content = content,
                ContentType = contentType,
                Headers = headers,
            };
            return WebHelper.Delete<T>(uri, configuration);
        }

        /// <summary>
        /// 使用 POST 方式提交请求，需要调用方自行释放响应对象
        /// </summary>
        /// <param name="uri">请求路径</param>
        /// <param name="configuration">HTTP 配置</param>
        public static T Delete<T>(string uri, HttpConfiguration configuration)
        {
            if (configuration == null) configuration = new HttpConfiguration<T>();
            configuration.Method = HttpMethod.Delete;
            var response = WebHelper.Send(uri, configuration);
            return WebHelper.ReadAsResult<T>(response, configuration);
        }

        /// <summary>
        /// 发起 HTTP，需要调用方自行释放响应对象
        /// </summary>
        /// <param name="uri">请求路径</param>
        /// <param name="configuration">HTTP 配置</param>
        /// <returns></returns>
        public static HttpWebResponse Send(string uri, HttpConfiguration configuration)
        {
            int tryTimes = configuration != null && configuration.TryTimes != null ? configuration.TryTimes.Value : 0;
            int sleep = configuration != null && configuration.Sleep != null ? configuration.Sleep.Value : 500;

            return WebHelper.Send(uri, configuration, tryTimes, sleep);
        }

        /// <summary>
        /// 抛出 webException 里面的信息
        /// </summary>
        public static void ThrowWebException(WebException we)
        {
            if (we == null) return;
            if (we.Response == null) return;

            Stream stream = null;
            StreamReader reader = null;
            try
            {
                stream = we.Response.GetResponseStream();
                reader = new StreamReader(stream);
                string line = reader.ReadToEnd();
                if (!string.IsNullOrEmpty(line)) throw new WebException(line, we);
            }
            finally
            {
                if (reader != null) reader.Close();
                if (stream != null) stream.Dispose();
            }
        }

        /// <summary>
        /// 读取 WebException 里的详细信息
        /// </summary>
        public static string ReadWebException(WebException we)
        {
            if (we == null) return string.Empty;
            if (we.Response == null) return we.Message;

            Stream stream = null;
            StreamReader reader = null;
            try
            {
                stream = we.Response.GetResponseStream();
                reader = new StreamReader(stream);
                string line = reader.ReadToEnd();
                return (!string.IsNullOrEmpty(line)) ? line : we.Message;
            }
            catch
            {
                return we.Message;
            }
            finally
            {
                if (reader != null) reader.Close();
                if (stream != null) stream.Dispose();
            }
        }

        // 发起 HTTP请求
        static HttpWebResponse Send(string uri, HttpConfiguration configuration, int tryTimes, int sleep)
        {
            if (uri != null)
            {
#if netcore
                if (uri.StartsWith("https", StringComparison.OrdinalIgnoreCase)) ServicePointManager.SecurityProtocol = SecurityProtocolType.SystemDefault | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
#endif
#if net45
                if (uri.StartsWith("https", StringComparison.OrdinalIgnoreCase)) ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
#endif
#if net40
                if (uri.StartsWith("https", StringComparison.OrdinalIgnoreCase)) ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | (SecurityProtocolType)768 | (SecurityProtocolType)3072;
#endif
            }

            try
            {
                // 创建请求
                var request = WebRequest.Create(uri) as HttpWebRequest;
                request.Method = "GET";
                // 默认连接最大数=2，如果没有全局设置，则需要设置并发连接数
                if (ServicePointManager.DefaultConnectionLimit == 2) request.ServicePoint.ConnectionLimit = 65532;
                if (configuration != null)
                {
                    request.ContentLength = 0;
                    request.Method = configuration.Method.ToString().ToUpper();
                    if (configuration.Timeout != null) request.Timeout = configuration.Timeout.Value;
                    if (configuration.ContentType != null) request.ContentType = configuration.ContentType;
                    if (configuration.Accept != null) request.Accept = configuration.Accept;
                    if (configuration.UserAgent != null) request.UserAgent = configuration.UserAgent;
                    if (configuration.KeepAlive != null) request.KeepAlive = configuration.KeepAlive.Value;
                    if (configuration.Proxy != null) request.Proxy = configuration.Proxy;
                    if (configuration.Headers != null) foreach (var kv in configuration.Headers) request.Headers.Add(kv.Key, kv.Value);
                    if (configuration.CookieContainer != null) request.CookieContainer = configuration.CookieContainer;

                    // 写入参数
                    string content = null;
                    if (configuration.Content != null && configuration.Content is string) content = (string)configuration.Content;
                    else if (configuration.Content != null) content = SerializeHelper.SerializeToJson(configuration.Content);
                    if (!string.IsNullOrEmpty(content))
                    {
                        var encoding = configuration.Encoding ?? Encoding.UTF8;
                        byte[] bytes = encoding.GetBytes(content);
                        request.ContentLength = bytes.Length;

                        using (var stream = request.GetRequestStream())
                        {
                            stream.Write(bytes, 0, bytes.Length);
                            stream.Close();
                        }
                    }
                }

                var response = request.GetResponse() as HttpWebResponse;
                return response;
            }
            catch (WebException we)
            {
                tryTimes--;
                if (tryTimes > 0)
                {
                    System.Threading.Thread.Sleep(sleep);
                    return WebHelper.Send(uri, configuration, tryTimes, sleep);
                }
                else
                {
                    WebHelper.ThrowWebException(we);
                    throw;
                }
            }
        }

        // 从响应流中读取响应为实体
        static T ReadAsResult<T>(HttpWebResponse response, HttpConfiguration configuration)
        {
            Stream stream = null;
            try
            {
                var conf = configuration as HttpConfiguration<T>;
                var deserializer = conf != null ? conf.Deserializer : null;
                Encoding encoding = configuration.Encoding;
                if (encoding == null)
                {
                    encoding = !string.IsNullOrEmpty(response.CharacterSet) 
                        ? Encoding.GetEncoding(response.CharacterSet) 
                        : null;
                }

                stream = response.GetResponseStream();
                if (response.ContentEncoding == "gzip") 
                    stream = new GZipStream(stream, CompressionMode.Decompress);
                else if (response.ContentEncoding == "deflate")
                    stream = new DeflateStream(stream, CompressionMode.Decompress);
                return WebHelper.ReadAsResult<T>(stream, encoding, deserializer);
            }
            finally
            {
                if (stream != null) stream.Close();
                if (response != null) response.Close();
            }
        }

        // 从响应流中读取响应为实体
        static T ReadAsResult<T>(Stream stream, Encoding encoding = null, Func<string, T> deserializer = null)
        {
            StreamReader reader = null;
            string json = string.Empty;
            try
            {
                // TODO 压缩类型流
                reader = encoding != null ? new StreamReader(stream, encoding) : new StreamReader(stream);
                json = reader.ReadToEnd();
                if (typeof(T) == typeof(string)) return (T)(json as object);
                else
                {
                    T value = deserializer != null ? deserializer(json) : SerializeHelper.DeserializeFromJson<T>(json);
                    return value;
                }
            }
            catch (Exception e)
            {
                if (string.IsNullOrEmpty(json)) throw;
                else
                {
                    // 抛出返回的原始 JSON
                    WebException we = e as WebException;
                    string line = e.Message;
                    if (we != null) line = WebHelper.ReadWebException(we);

                    string message = string.Format("{0}{1}{2}", line, Environment.NewLine, json);
                    throw new XFrameworkException(message, e);
                }
            }
            finally
            {
                if (reader != null) reader.Close();
                if (stream != null) stream.Close();
            }
        }

        /// <summary>
        /// HTTP 方法
        /// </summary>
        public enum HttpMethod
        {
            /// <summary>
            /// GET 方法
            /// </summary>
            Get = 1,

            /// <summary>
            /// POST 方法
            /// </summary>
            Post = 2,

            /// <summary>
            /// PUT 方法
            /// </summary>
            Put = 3,

            /// <summary>
            /// DELETE 方法
            /// </summary>
            Delete = 4,

            /// <summary>
            /// HEAD 方法
            /// </summary>
            Head = 5,

            /// <summary>
            /// OPTIONS 方法
            /// </summary>
            Options = 6,

            /// <summary>
            /// TRACE 方法
            /// </summary>
            Trace = 7,
        }

        /// <summary>
        /// HTTP 配置
        /// </summary>
        public class HttpConfiguration
        {
            /// <summary>
            /// HTTP 请求方式，默认使用 GET
            /// </summary>
            public HttpMethod Method { get; set; }

            /// <summary>
            /// 提交内容
            /// </summary>
            public object Content { get; set; }

            /// <summary>
            /// HTTP 内容类型标头
            /// </summary>
            public string ContentType { get; set; }

            /// <summary>
            /// HTTP 接受类型标头
            /// </summary>
            public string Accept { get; set; }

            /// <summary>
            /// HTTP 用户代理标头
            /// </summary>
            public string UserAgent { get; set; }

            /// <summary>
            /// 获取或设置一个值，该值指示是否与资源建立持久性连接
            /// </summary>
            public bool? KeepAlive { get; set; }

            /// <summary>
            /// 提交内容的编码方式
            /// </summary>
            public Encoding Encoding { get; set; }

            /// <summary>
            /// HTTP 标头集合
            /// <para>
            /// 身份验证方案（scheme）固定key=scheme
            /// 身份验证信息的凭据（token）固定用key=token
            /// </para>
            /// </summary>
            public IDictionary<string, string> Headers { get; set; }

            /// <summary>
            /// 请求超时时间，毫秒为单位
            /// </summary>
            public int? Timeout { get; set; }

            /// <summary>
            /// WEB 代理
            /// </summary>
            public WebProxy Proxy { get; set; }
            //WebProxy webProxy = new WebProxy(proxy.Address);
            //webProxy.Credentials = new NetworkCredential(proxy.AccountName, proxy.Password);

            /// <summary>
            /// COOKIE 容器
            /// </summary>
            public CookieContainer CookieContainer { get; set; }
            //string path = "/";
            //string domain = "www.merchantwords.com";
            //CookieContainer container = new CookieContainer();
            //container.Add(new Cookie("__zlcmid", "qShxFslerMOiOr", path, domain));
            //container.Add(new Cookie("_ga", "GA1.2.1649633403.1547951981", path, domain));

            /// <summary>
            /// 请求出错时重试次数
            /// </summary>
            public int? TryTimes { get; set; }

            /// <summary>
            /// 重试时线程等待时间，毫秒为单位
            /// </summary>
            public int? Sleep { get; set; }
        }

        /// <summary>
        /// HTTP 配置
        /// </summary>
        public class HttpConfiguration<T> : HttpConfiguration
        {
            /// <summary>
            /// 反序列化器
            /// </summary>
            public Func<string, T> Deserializer { get; set; }
        }

        #endregion
    }
}