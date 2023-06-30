
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
        /// <para>
        /// 注：4.0 版本的 WebRequest 默认限制连接并发数。
        /// 如果要提高并发数，需自行设置 ServicePointManager.DefaultConnectionLimit 属性
        /// </para>
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
                if (configuration != null)
                {
                    if (configuration.Method == HttpMethod.Get) request.Method = "GET";
                    else if (configuration.Method == HttpMethod.Post) request.Method = "POST";
                    else if (configuration.Method == HttpMethod.Put) request.Method = "PUT";
                    else if (configuration.Method == HttpMethod.Delete) request.Method = "DELETE";
                    else if (configuration.Method == HttpMethod.Head) request.Method = "HEAD";
                    else if (configuration.Method == HttpMethod.Trace) request.Method = "TRACE";
                    else if (configuration.Method == HttpMethod.Options) request.Method = "OPTIONS";
                }
                //// 默认连接最大数=2，如果没有全局设置，则需要设置并发连接数
                //if (ServicePointManager.DefaultConnectionLimit == 2) request.ServicePoint.ConnectionLimit = 65532;
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
                    //if (!string.IsNullOrEmpty(configuration.Cookies))
                    //{
                    //    string[] cookies = configuration.Cookies.Split(';');
                    //    foreach (var c in cookies)
                    //    {
                    //        if (configuration.CookieContainer == null)
                    //            configuration.CookieContainer = new CookieContainer();
                    //        configuration.CookieContainer.Add
                    //    }
                    //}
                    if (configuration.CookieContainer != null) request.CookieContainer = configuration.CookieContainer;

                    if (configuration.Headers != null)
                    {
                        // Authorization TODO
                        string scheme = null;
                        string token = null;
                        foreach (var kv in configuration.Headers)
                        {
                            if (string.Equals(kv.Key, "scheme", StringComparison.CurrentCultureIgnoreCase)) scheme = kv.Key;
                            else if (string.Equals(kv.Key, "token", StringComparison.CurrentCultureIgnoreCase)) token = kv.Key;
                            else request.Headers.Add(kv.Key, kv.Value);
                        }

                        if (token != null)
                        {
                            if (scheme == null) scheme = "Basic";
                            request.Headers.Add("authorization", string.Format("{0} {1}", scheme, token));
                        }
                    }

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
            /// <code>
            /// 4.0 版本用法
            /// </code>
            /// <code>
            /// 4.5 版本用法
            /// var content = new System.Net.Http.MultipartFormDataContent();
            /// content.Headers.Add("ContentType", "multipart/form-data"); --声明头部
            /// content.Add(new System.Net.Http.StringContent("223.104.64.213"), "log_ip");--参数, 内容在前,参数名称在后
            /// var string2 = WebHelper.PostAsync&lt;string&gt;(uri, new WebHelper.HttpConfiguration { Content = content  }).Result;
            /// </code>
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
            /// 字符串形式的cokkie，自动转成 CookieContainer
            /// </summary>
            public string Cookies { get; set; }

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

        #region 他山之石

        ///// <summary>
        ///// HTTP请求(包含多分部数据,multipart/form-data)。
        ///// 将多个文件以及多个参数以多分部数据表单方式上传到指定url的服务器
        ///// </summary>
        ///// <param name="url">请求目标URL</param>
        ///// <param name="fileFullNames">待上传的文件列表(包含全路径的完全限定名)。如果某个文件不存在，则忽略不上传</param>
        ///// <param name="kVDatas">请求时表单键值对数据。</param>
        ///// <param name="method">请求的方法。请使用 HttpMethod 的枚举值</param>
        ///// <param name="timeOut">获取或设置 <see cref="M:System.Net.HttpWebRequest.GetResponse" /> 和
        /////                       <see cref="M:System.Net.HttpWebRequest.GetRequestStream" /> 方法的超时值（以毫秒为单位）。
        /////                       -1 表示永不超时
        ///// </param>
        ///// <returns></returns>
        //public HttpResult UploadFormByMultipart(string url, string[] fileFullNames, NameValueCollection kVDatas = null, string method = HttpMethod.POST, int timeOut = -1)
        //{
        //    #region 说明
        //    /* 阿里云文档：https://www.alibabacloud.com/help/zh/doc-detail/42976.htm
        //       C# 示例：  https://github.com/aliyun/aliyun-oss-csharp-sdk/blob/master/samples/Samples/PostPolicySample.cs?spm=a2c63.p38356.879954.18.7f3f7c34W3bR9U&file=PostPolicySample.cs
        //                 (C#示例中仅仅是把文件中的文本内容当做 FormData 中的项，与文件流是不一样的。本方法展示的是文件流，更通用)
        //      */

        //    /* 说明：multipart/form-data 方式提交文件
        //     *     (1) Header 一定要有 Content-Type: multipart/form-data; boundary={boundary}。
        //     *     (2) Header 和bod y之间由 \r\n--{boundary} 分割。
        //     *     (3) 表单域格式 ：Content-Disposition: form-data; name="{key}"\r\n\r\n
        //     *                   {value}\r\n
        //     *                   --{boundary}
        //     *     (4)表单域名称大小写敏感，如policy、key、file、OSSAccessKeyId、OSSAccessKeyId、Content-Disposition。
        //     *     (5)注意:表单域 file 必须为最后一个表单域。即必须放在最后写。
        //     */
        //    #endregion

        //    #region ContentType 说明
        //    /* 该ContentType的属性包含请求的媒体类型。分配给ContentType属性的值在请求发送Content-typeHTTP标头时替换任何现有内容。
               
        //       要清除Content-typeHTTP标头，请将ContentType属性设置为null。
               
        //     * 注意：此属性的值存储在WebHeaderCollection中。如果设置了WebHeaderCollection，则属性值将丢失。
        //     *      所以放置在Headers 属性之后设置
        //     */
        //    #endregion

        //    #region Method 说明
        //    /* 如果 ContentLength 属性设置为-1以外的任何值，则必须将 Method 属性设置为上载数据的协议属性。 */
        //    #endregion

        //    #region HttpWebRequest.CookieContainer 在 .NET3.5 与 .NET4.0 中的不同
        //    /* 请参考：https://www.crifan.com/baidu_emulate_login_for_dotnet_4_0_error_the_fisrt_two_args_should_be_string_type_0_1/ */
        //    #endregion

        //    HttpResult httpResult = new HttpResult();

        //    #region 校验

        //    if (fileFullNames == null || fileFullNames.Length == 0)
        //    {
        //        httpResult.Status = HttpResult.STATUS_FAIL;

        //        httpResult.RefCode = (int)HttpStatusCode2.USER_FILE_NOT_EXISTS;
        //        httpResult.RefText = HttpStatusCode2.USER_FILE_NOT_EXISTS.GetCustomAttributeDescription();

        //        return httpResult;
        //    }

        //    List<string> lstFiles = new List<string>();
        //    foreach (string fileFullName in fileFullNames)
        //    {
        //        if (File.Exists(fileFullName))
        //        {
        //            lstFiles.Add(fileFullName);
        //        }
        //    }

        //    if (lstFiles.Count == 0)
        //    {
        //        httpResult.Status = HttpResult.STATUS_FAIL;

        //        httpResult.RefCode = (int)HttpStatusCode2.USER_FILE_NOT_EXISTS;
        //        httpResult.RefText = HttpStatusCode2.USER_FILE_NOT_EXISTS.GetCustomAttributeDescription();

        //        return httpResult;
        //    }

        //    #endregion

        //    string boundary = CreateFormDataBoundary();                                         // 边界符
        //    byte[] beginBoundaryBytes = Encoding.UTF8.GetBytes("--" + boundary + "\r\n");     // 边界符开始。【☆】右侧必须要有 \r\n 。
        //    byte[] endBoundaryBytes = Encoding.UTF8.GetBytes("\r\n--" + boundary + "--\r\n"); // 边界符结束。【☆】两侧必须要有 --\r\n 。
        //    byte[] newLineBytes = Encoding.UTF8.GetBytes("\r\n"); //换一行
        //    MemoryStream memoryStream = new MemoryStream();

        //    HttpWebRequest httpWebRequest = null;
        //    try
        //    {
        //        httpWebRequest = WebRequest.Create(url) as HttpWebRequest; // 创建请求
        //        httpWebRequest.ContentType = string.Format(HttpContentType.MULTIPART_FORM_DATA + "; boundary={0}", boundary);
        //        //httpWebRequest.Referer = "http://bimface.com/user-console";
        //        httpWebRequest.Method = method;
        //        httpWebRequest.KeepAlive = true;
        //        httpWebRequest.Timeout = timeOut;
        //        httpWebRequest.UserAgent = GetUserAgent();

        //        #region 步骤1：写入键值对
        //        if (kVDatas != null)
        //        {
        //            string formDataTemplate = "Content-Disposition: form-data; name=\"{0}\"\r\n\r\n" +
        //                                      "{1}\r\n";

        //            foreach (string key in kVDatas.Keys)
        //            {
        //                string formItem = string.Format(formDataTemplate, key.Replace(StringUtils.Symbol.KEY_SUFFIX, String.Empty), kVDatas[key]);
        //                byte[] formItemBytes = Encoding.UTF8.GetBytes(formItem);

        //                memoryStream.Write(beginBoundaryBytes, 0, beginBoundaryBytes.Length); // 1.1 写入FormData项的开始边界符
        //                memoryStream.Write(formItemBytes, 0, formItemBytes.Length);           // 1.2 将键值对写入FormData项中
        //            }
        //        }
        //        #endregion

        //        #region 步骤2：写入文件(表单域 file 必须为最后一个表单域)

        //        const string filePartHeaderTemplate = "Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\n" +
        //                                              "Content-Type: application/octet-stream\r\n\r\n";

        //        int i = 0;
        //        foreach (var fileFullName in lstFiles)
        //        {
        //            FileInfo fileInfo = new FileInfo(fileFullName);
        //            string fileName = fileInfo.Name;

        //            string fileHeaderItem = string.Format(filePartHeaderTemplate, "file", fileName);
        //            byte[] fileHeaderItemBytes = Encoding.UTF8.GetBytes(fileHeaderItem);

        //            if (i > 0)
        //            {
        //                // 第一笔及第一笔之后的数据项之间要增加一个换行 
        //                memoryStream.Write(newLineBytes, 0, newLineBytes.Length);
        //            }
        //            memoryStream.Write(beginBoundaryBytes, 0, beginBoundaryBytes.Length);      // 2.1 写入FormData项的开始边界符
        //            memoryStream.Write(fileHeaderItemBytes, 0, fileHeaderItemBytes.Length);    // 2.2 将文件头写入FormData项中

        //            int bytesRead;
        //            byte[] buffer = new byte[1024];

        //            FileStream fileStream = new FileStream(fileFullName, FileMode.Open, FileAccess.Read);
        //            while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0)
        //            {
        //                memoryStream.Write(buffer, 0, bytesRead);                              // 2.3 将文件流写入FormData项中
        //            }

        //            i++;
        //        }

        //        memoryStream.Write(endBoundaryBytes, 0, endBoundaryBytes.Length);             // 2.4 写入FormData的结束边界符

        //        #endregion

        //        #region 步骤3：将表单域(内存流)写入 httpWebRequest 的请求流中，并发起请求

        //        /*  在 http1.1 以上中，如果使用 post，并且 body 中非空时，必须要有 content-length 的标头。
        //         *  并且，如果字符中存在汉字，那么在 utf-8 编码模式下，其长度应该采用编码后的字符长度，也就是 byte 数组的长度，而不是原始字符串的长度。
        //         */
        //        httpWebRequest.ContentLength = memoryStream.Length;

        //        Stream requestStream = httpWebRequest.GetRequestStream();

        //        memoryStream.Position = 0;
        //        byte[] tempBuffer = new byte[memoryStream.Length];
        //        memoryStream.Read(tempBuffer, 0, tempBuffer.Length);
        //        memoryStream.Close();

        //        requestStream.Write(tempBuffer, 0, tempBuffer.Length);        // 将内存流中的字节写入 httpWebRequest 的请求流中
        //        requestStream.Close();
        //        #endregion

        //        HttpWebResponse httpWebResponse = httpWebRequest.GetResponse() as HttpWebResponse; // 获取响应
        //        if (httpWebResponse != null)
        //        {
        //            //GetHeaders(ref httpResult, httpWebResponse);
        //            GetResponse(ref httpResult, httpWebResponse);
        //            httpWebResponse.Close();
        //        }
        //    }
        //    catch (WebException webException)
        //    {
        //        GetWebExceptionResponse(ref httpResult, webException);
        //    }
        //    catch (Exception ex)
        //    {
        //        GetExceptionResponse(ref httpResult, ex, method, HttpContentType.MULTIPART_FORM_DATA);
        //    }
        //    finally
        //    {
        //        if (httpWebRequest != null)
        //        {
        //            httpWebRequest.Abort();
        //        }
        //    }

        //    return httpResult;
        //}

        #endregion
    }
}