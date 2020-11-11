
using System;
using System.Text;
using System.IO;
#if !netcore
using System.Web;
using System.Web.Security;
#endif

namespace Riz.XFramework
{
    /// <summary>
    /// WEB助手类
    /// </summary>
    public partial class WebHelper
    {
#if !netcore

        // 此文件适用于 fx（net40,net45）

        #region Cookie

        /// <summary>
        /// 设置Cookie
        /// </summary>
        /// <param name="key">cookie键</param>
        /// <param name="value">cookie值</param>
        /// <param name="expires">过期时间，分钟为单位，默认一天</param>
        /// <param name="encrypt">是否加密cookie</param>
        public static void SetCookie(string key, string value, int expires = 1440, bool encrypt = true)
        {
            WebHelper.SetCookie(key, value, DateTime.Now.AddMinutes(expires));
        }

        /// <summary>
        /// 设置Cookie
        /// </summary>
        /// <param name="key">cookie键</param>
        /// <param name="value">cookie值</param>
        /// <param name="expires">过期时间，分钟为单位，默认一天</param>
        /// <param name="encrypt">是否加密cookie</param>
        public static void SetCookie(string key, string value, DateTime expires, bool encrypt = true)
        {
            HttpCookie cookie = HttpContext.Current.Request.Cookies[key];
            if (encrypt) value = SecurityHelper.DESEncrypt(value);

            if (cookie != null)
            {
                cookie.Value = value;
                cookie.Expires = expires;
                HttpContext.Current.Response.Cookies.Set(cookie);
            }
            else
            {
                cookie = new HttpCookie(key);
                cookie.Value = value;
                cookie.Expires = expires;
                HttpContext.Current.Response.Cookies.Add(cookie);
            }
        }

        /// <summary>
        /// 设置COOKIE
        /// </summary>
        /// <param name="key">cookie键</param>
        /// <param name="TCookie">cookie值</param>
        /// <param name="expires">过期时间，分钟为单位，默认一天</param>
        /// <param name="encrypt">是否加密cookie</param>
        public static void SetCookie<T>(string key, T TCookie, int expires = 1440, bool encrypt = true)
            where T : class, new()
        {
            string json = SerializeHelper.SerializeToJson(TCookie);
            WebHelper.SetCookie(key, json, expires, encrypt);
        }


        /// <summary>
        /// 设置COOKIE
        /// </summary>
        /// <param name="key">cookie键</param>
        /// <param name="cookie">cookie值</param>
        /// <param name="expires">过期时间，分钟为单位，默认一天</param>
        /// <param name="encrypt">是否加密cookie</param>
        public static void SetCookie<T>(string key, T cookie, DateTime expires, bool encrypt = true)
            where T : class, new()
        {
            string json = SerializeHelper.SerializeToJson(cookie);
            WebHelper.SetCookie(key, json, expires, encrypt);
        }

        /// <summary>
        /// 取指定KEY值的Cookie
        /// </summary>
        public static T GetCookie<T>(string key, bool decrypt = true)
            where T : class, new()
        {
            string cookieValue = WebHelper.GetCookie(key, decrypt);
            if (string.IsNullOrEmpty(cookieValue)) return default(T);

            if (decrypt) cookieValue = SecurityHelper.DESDecrypt(cookieValue);
            T TJson = SerializeHelper.DeserializeFromJson<T>(cookieValue);
            return TJson;
        }

        /// <summary>
        /// 取指定KEY值的Cookie
        /// </summary>
        public static string GetCookie(string key, bool decrypt = true)
        {
            HttpCookie cookie = HttpContext.Current.Request.Cookies[key];
            if (cookie == null || string.IsNullOrEmpty(cookie.Value)) return null;

            string cookieValue = cookie.Value;
            if (decrypt) cookieValue = SecurityHelper.DESDecrypt(cookieValue);
            return cookieValue;
        }

        /// <summary>
        /// 删除COOKIE
        /// </summary>
        /// <param name="key"></param>
        public static void RemoveCookie(string key)
        {
            HttpCookie cookie = HttpContext.Current.Request.Cookies[key];
            if (cookie != null)
            {
                cookie.Expires = DateTime.Now.AddDays(-1);
                HttpContext.Current.Response.Cookies.Set(cookie);
            }
        }

        /// <summary>
        /// 写登录Cookie
        /// </summary>
        /// <param name="ticketName">ticket名称</param>
        /// <param name="userData">存储在票证中的用户特定的数据。</param>
        /// <param name="expiration">过期时间（单位：分钟）</param>
        public static void SetAuthentication(string ticketName, string userData, int? expiration = 60)
        {
            DateTime now = DateTime.Now;
            double expire = expiration != null ? expiration.Value : FormsAuthentication.Timeout.TotalMinutes;
            //创建新的窗体身份验证票证，它包含用户名、到期时间和该用户所属的角色列表。
            FormsAuthenticationTicket ticket = new FormsAuthenticationTicket(1, ticketName, now, now.AddMinutes(expire), true, userData, "/");
            //加密序列化验证票为字符串
            string encryptedTicket = FormsAuthentication.Encrypt(ticket);
            //创建一个 cookie 并将加密的票证添加到该 cookie 作为数据。
            //FormsAuthentication.FormsCookieName是根目录web.config的authentication下的name
            HttpCookie cookie = new HttpCookie(FormsAuthentication.FormsCookieName, encryptedTicket);
            cookie.Expires = ticket.Expiration;
            //输出Cookie,将cookie添加到返回给用户浏览器的cookie集合中
            HttpContext.Current.Response.Cookies.Add(cookie);

        }

        /// <summary>
        /// 取登录
        /// </summary>
        /// <param name="cookieName">cookie名称</param>
        /// <param name="userData">存储在票证中的用户特定的数据。</param>
        /// <param name="expiration">过期时间（单位：分钟）</param>
        public static HttpCookie GetAuthenCookie(string cookieName, string userData, int? expiration = 60)
        {
            DateTime now = DateTime.Now;
            double expire = expiration != null ? expiration.Value : FormsAuthentication.Timeout.TotalMinutes;
            //创建新的窗体身份验证票证，它包含用户名、到期时间和该用户所属的角色列表。
            FormsAuthenticationTicket ticket = new FormsAuthenticationTicket(1, cookieName, now, now.AddMinutes(expire), true, userData, "/");
            //加密序列化验证票为字符串
            string encryptedTicket = FormsAuthentication.Encrypt(ticket);
            //创建一个 cookie 并将加密的票证添加到该 cookie 作为数据。
            //FormsAuthentication.FormsCookieName是根目录web.config的authentication下的name
            HttpCookie cookie = new HttpCookie(FormsAuthentication.FormsCookieName, encryptedTicket);
            cookie.Expires = ticket.Expiration;
            return cookie;
        }

        /// <summary>
        /// 清除验证COOKIE
        /// </summary>
        /// <param name="redirect">清除验证cookie后是否应该重定向</param>
        /// <param name="url">重定向地址</param>
        public static void ClearAuthentication(bool redirect = true, string url = "/")
        {
            FormsAuthentication.SignOut();
            if (redirect) HttpContext.Current.Response.Redirect(url, true);
        }

        #endregion

        #region 下载

        /// <summary>
        /// 下载文件
        /// </summary>
        /// <param name="webPath">相对路径</param>
        /// <param name="limitSpeed">是否限制客户端的下载速度</param>
        /// <param name="speed">客户端下载速度（B/MS）</param>
        /// <param name="delete">下载完成后是否删除文件</param>
        public static void DownloadFile(string webPath, bool limitSpeed = true, long speed = 102400, bool delete = false)
        {
            /*
             * IE的自带下载功能中没有断点续传功能，要实现断点续传功能，需要用到HTTP协议中鲜为人知的几个响应头和请求头。

                一. 两个必要响应头Accept-Ranges、ETag
                    客户端每次提交下载请求时，服务端都要添加这两个响应头，以保证客户端和服务端将此下载识别为可以断点续传的下载：
                    Accept-Ranges：告知下载客户端这是一个可以恢复续传的下载，存放本次下载的开始字节位置、文件的字节大小；
                    ETag：保存文件的唯一标识（我在用的文件名+文件最后修改时间，以便续传请求时对文件进行验证）；
                    Last-Modified：可选响应头，存放服务端文件的最后修改时间，用于验证

                二. 一个重要请求头Range
                    Range：首次下载时，Range头为null，此时服务端的响应头中必须添加响应头Accept-Ranges、ETag；
                    续传请求时，其值表示客户端已经收到的字节数，即本次下载的开始字节位置，服务端依据这个 值从相应位置读取数据发送到客户端。

                三. 用于验证的请求头If-Range、
                    当响应头中包含有Accept-Ranges、ETag时，续传请求时，将包含这些请求头：
                    If-Range：对应响应头ETag的值；
                    Unless-Modified-Since：对应响应头Last-Modified的值。
                    续传请求时，为了保证客户端与服务端的文件的一致性和正确性，有必要对文件进行验证，验证需要自己写验证代码，就根据解析这两个请求头的值，
                    将客户端已下载的部分与服务端的文件进行对比，如果不吻合，则从头开始下载，如果吻合，则断点续传。

                四.  速度限制
                     程序中加入了速度限制，用于对客户端进行权限控制的流量限制；计算公式：速度(byte/second) = 输出到客户端字节数(sizepack)/运行时间(sleep)
                五.  另外：UrlEncode编码后会把文件名中的空格转换中+（+转换为%2b），但是浏览器是不能理解加号为空格的，所以在浏览器下载得到的文件，空格就变成了加号；
                     解决办法：UrlEncode 之后, 将 "+" 替换成 "%20"，因为浏览器将%20转换为空格
             */

            string physicalPath = HttpContext.Current.Server.MapPath(webPath);
            if (!File.Exists(physicalPath)) throw new FileNotFoundException(physicalPath);

            var fs = new FileStream(physicalPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite); //文件流
            var reader = new BinaryReader(fs);  //reader

            var httpResponse = HttpContext.Current.Response; //当前WEB响应
            var httpRequest = HttpContext.Current.Request;   //当前WEB请求
            long length = fs.Length;    //文件长度
            long startPos = 0;          //开始位置
            int sizePack = 10240;      //分块下载，每块10K（单位：B）
            int sleep = 0;
            if (limitSpeed && speed < sizePack * 1000) sleep = (int)Math.Ceiling((sizePack * 1000.0/*转换时间为秒*// speed));

            string modified = File.GetLastWriteTimeUtc(physicalPath).ToString("r");  //最后修改时间
            string etag = HttpUtility.UrlEncode(physicalPath, Encoding.UTF8) + modified;//便于恢复下载时提取请求头;
            string ifRange = httpRequest.Headers["If-Range"];//IE  If-Match、If-Unmodified-Since或者Unless-Modified-Since
            string ifMatch = httpRequest.Headers["If-Match"];//FireFox
            if (string.IsNullOrEmpty(ifRange)) ifRange = ifMatch;

            //确定当次下载的起始位置
            if (httpRequest.Headers["Range"] != null)
            {
                //如果是续传请求，则获取续传的起始位置，即已经下载到客户端的字节数------
                httpResponse.StatusCode = 206;//重要：续传必须，表示局部范围响应。初始下载时默认为200
                string[] range = httpRequest.Headers["Range"].Split(new char[] { '=', '-' });//"bytes=1474560-"
                startPos = Convert.ToInt64(range[1]);//已经下载的字节数，即本次下载的开始位置
                if (!string.IsNullOrEmpty(ifRange) && ifRange.Replace("\"", "") != etag) startPos = 0; //上次被请求的日期之后被修改过，从头开始下载
                if (startPos < 0 || startPos >= length) throw new Exception(string.Format("下载的位置在{0}处无效！", startPos));
            }
            //如果是续传请求，告诉客户端本次的开始字节数，总长度，以便客户端将续传数据追加到startBytes位置后
            if (startPos > 0) httpResponse.AddHeader("Content-Range", string.Format(" bytes {0}-{1}/{2}", startPos, length - 1, length));

            httpResponse.Clear();
            httpResponse.Buffer = false;
            //httpResponse.AddHeader("Content-MD5", StringHepler.MD5Hash(fs));//用于验证文件
            httpResponse.AddHeader("Accept-Ranges", "bytes");//重要：续传必须
            httpResponse.AppendHeader("ETag", "\"" + etag + "\"");//重要：续传必须（实体标签，在IE浏览器续传的时候会把该值和Range值发送回服务器，对应Request.Headers["If-Range"]，确保下载从准确相同的文件恢复的一种途径）
            httpResponse.AppendHeader("Last-Modified", modified);//把最后修改日期写入响应                
            httpResponse.ContentType = "application/octet-stream";//MIME类型：匹配任意文件类型
            //httpResponse.AddHeader("Content-Disposition", "attachment;filename=" + HttpUtility.UrlEncode(Path.GetFileName(absolutePath), Encoding.UTF8).Replace("+", "%20"));
            //处理文件名
            string agent = httpRequest.UserAgent;
            string fileName = Path.GetFileName(physicalPath);
            if (!string.IsNullOrEmpty(agent))
            {
                agent = agent.ToUpper();
                if (agent.IndexOf("MSIE") > -1) fileName = ToHex(fileName);
            }
            if (!string.IsNullOrEmpty(agent) && agent.ToUpper().IndexOf("FIREFOX") > -1)
            {
                //当是FireFox时需要做特别处理
                httpResponse.AddHeader("Content-Disposition", "attachment;filename=\"" + fileName + "\"");
            }
            else
            {
                httpResponse.AddHeader("Content-Disposition", "attachment;filename=" + fileName);
            }

            httpResponse.AddHeader("Content-Length", (length - startPos).ToString());
            httpResponse.AddHeader("Connection", "Keep-Alive");
            httpResponse.ContentEncoding = Encoding.UTF8;

            reader.BaseStream.Seek(startPos, SeekOrigin.Begin);
            int maxCount = (int)Math.Ceiling((length - startPos + 0.0) / sizePack);//分块下载，剩余部分可分成的块数
            for (int i = 0; i < maxCount && httpResponse.IsClientConnected; i++)
            {
                //客户端中断连接，则暂停
                httpResponse.BinaryWrite(reader.ReadBytes(sizePack));
                httpResponse.Flush();
                if (sleep > 1) System.Threading.Thread.Sleep(sleep);
            }

            //释放流资源
            reader.Dispose();
            fs.Dispose();

            //检查下载完成后是否需要删除文件
            if (delete && File.Exists(physicalPath)) File.Delete(physicalPath);
        }

        //根据文件后缀来获取MIME类型字符串
        private static string GetMimeType(string extension)
        {
            string mime = string.Empty;
            extension = extension.ToLower();
            switch (extension)
            {
                case ".avi": mime = "video/x-msvideo"; break;
                case ".bin":
                case ".exe":
                case ".msi":
                case ".dll":
                case ".class": mime = "application/octet-stream"; break;
                case ".csv": mime = "text/comma-separated-values"; break;
                case ".html":
                case ".htm":
                case ".shtml": mime = "text/html"; break;
                case ".css": mime = "text/css"; break;
                case ".js": mime = "text/javascript"; break;
                case ".doc":
                case ".dot":
                case ".docx": mime = "application/msword"; break;
                case ".xla":
                case ".xls":
                case ".xlsx": mime = "application/msexcel"; break;
                case ".ppt":
                case ".pptx": mime = "application/mspowerpoint"; break;
                case ".gz": mime = "application/gzip"; break;
                case ".gif": mime = "image/gif"; break;
                case ".bmp": mime = "image/bmp"; break;
                case ".jpeg":
                case ".jpg":
                case ".jpe":
                case ".png": mime = "image/jpeg"; break;
                case ".mpeg":
                case ".mpg":
                case ".mpe":
                case ".wmv": mime = "video/mpeg"; break;
                case ".mp3":
                case ".wma": mime = "audio/mpeg"; break;
                case ".pdf": mime = "application/pdf"; break;
                case ".rar": mime = "application/octet-stream"; break;
                case ".txt": mime = "text/plain"; break;
                case ".7z":
                case ".z": mime = "application/x-compress"; break;
                case ".zip": mime = "application/x-zip-compressed"; break;
                default:
                    mime = "application/octet-stream";
                    break;
            }
            return mime;
        }

        /// <summary>
        /// 将非ASCII字符进行编码
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private static string ToHex(string str)
        {
            char[] chars = str.ToCharArray();
            var builder = new StringBuilder();
            for (int index = 0; index < chars.Length; index++)
            {
                if (NeedEncode(chars[index]))
                {
                    builder.Append(ToHex(chars[index]));
                }
                else
                {
                    builder.Append(chars[index]);
                }
            }

            return builder.ToString();
        }

        /// <summary>
        /// 将一个非ASCII字符进行编码
        /// </summary>
        /// <param name="chr"></param>
        /// <returns></returns>
        private static string ToHex(char chr)
        {
            var utf8 = new UTF8Encoding();
            byte[] data = utf8.GetBytes(chr.ToString());
            var builder = new StringBuilder();
            for (int index = 0; index < data.Length; index++) builder.AppendFormat("%{0}", Convert.ToString(data[index], 16));

            return builder.ToString();
        }

        /// <summary>
        /// 检测字符是否需要编码
        /// </summary>
        /// <param name="chr"></param>
        /// <returns></returns>
        private static bool NeedEncode(char chr)
        {
            string reservedChars = "$-_.+!*'(),@=&";

            if (chr > 127)
                return true;
            if (char.IsLetterOrDigit(chr) || reservedChars.IndexOf(chr) >= 0)
                return false;

            return true;
        }

        #endregion

#endif
    }
}