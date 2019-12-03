
using System.Net;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.Collections.Generic;

namespace TZM.XFramework
{
    /// <summary>
    /// WEB助手类
    /// </summary>
    public partial class WebHelper
    {
        #region 网络

        /// <summary>
        /// HttpClient 用POST方法访问指定URI
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="uri">请求发送到的 URI。</param>
        /// <param name="content">发送到服务器的 HTTP 请求内容。</param>
        /// <param name="headers">请求的头部信息</param>
        /// <param name="authentication">请求的验证信息</param>
        /// <returns></returns>
        public static async Task<T> PostAsync<T>(string uri, string content, IDictionary<string, string> headers = null, AuthenticationHeaderValue authentication = null)
        {
            HttpClient c = null;
            HttpContent httpContent = null;
            HttpResponseMessage msg = null;
            T TEntity = default(T);

            try
            {
                c = new HttpClient();
                c.
                httpContent = new StringContent(content);
                if (headers != null) foreach (var kv in headers) c.DefaultRequestHeaders.Add(kv.Key, kv.Value);
                if (authentication != null) c.DefaultRequestHeaders.Authorization = authentication;

                msg = await c.PostAsync(uri, httpContent);
                string json = await msg.Content.ReadAsStringAsync(); // ReadAsAsync
                TEntity = SerializeHelper.DeserializeFromJson<T>(json);
            }
            catch (WebException we)
            {
                WebHelper.ThrowWebException(we);
                throw;
            }
            finally
            {
                if (httpContent != null) httpContent.Dispose();
                if (c != null) c.Dispose();
                if (msg != null) msg.Dispose();

            }

            return TEntity;
        }

        /// <summary>
        /// HttpClient 用POST方法访问指定URI
        /// </summary>
        /// <param name="uri">请求发送到的 URI。</param>
        /// <param name="content">发送到服务器的 HTTP 请求内容。</param>
        /// <param name="token">请求的验证信息</param>
        /// <param name="headers">请求的头部信息</param>
        /// <returns></returns>
        public static async Task<HttpContent> PostAsync(string uri, string content, string token, IDictionary<string, string> headers)
        {
            HttpClient c = null;
            HttpContent httpContent = null;
            HttpResponseMessage msg = null;

            try
            {
                c = new HttpClient();
                httpContent = new StringContent(content, Encoding.UTF8, "application/json");
                //httpContent.Headers.Add("Content-Type", "application/json");
                if (headers != null) foreach (var kv in headers) c.DefaultRequestHeaders.Add(kv.Key, kv.Value);
                if (!string.IsNullOrEmpty(token)) c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", token);

                msg = await c.PostAsync(uri, httpContent);
                return msg.Content;
            }
            catch (WebException we)
            {
                WebHelper.ThrowWebException(we);
                throw;
            }
            finally
            {
                if (httpContent != null) httpContent.Dispose();
                if (c != null) c.Dispose();
            }
        }

        /// <summary>
        /// HttpClient 用GET方法访问指定URI
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="uri">请求发送到的 URI。</param>
        /// <param name="headers">请求的头部信息</param>
        /// <param name="authentication">请求的验证信息</param>
        /// <returns></returns>
        public static async Task<T> GetAsync<T>(string uri, IDictionary<string, string> headers = null, AuthenticationHeaderValue authentication = null)
        {
            HttpClient c = null;
            HttpResponseMessage msg = null;
            T TEntity = default(T);

            try
            {
                c = new HttpClient();
                if (headers != null) foreach (var kv in headers) c.DefaultRequestHeaders.Add(kv.Key, kv.Value);
                if (authentication != null) c.DefaultRequestHeaders.Authorization = authentication;

                msg = await c.GetAsync(uri);
                string json = await msg.Content.ReadAsStringAsync();
                TEntity = SerializeHelper.DeserializeFromJson<T>(json);
            }
            catch (WebException we)
            {
                WebHelper.ThrowWebException(we);
                throw;
            }
            finally
            {
                if (c != null) c.Dispose();
                if (msg != null) msg.Dispose();

            }

            return TEntity;
        }

        /// <summary>
        /// HttpClient 用GET方法访问指定URI
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="uri">请求发送到的 URI。</param>
        /// <param name="token">Basic 验证模式的令牌</param>
        /// <param name="headers">请求的头部信息</param>
        /// <returns></returns>
        public static async Task<T> GetAsync<T>(string uri, string token, IDictionary<string, string> headers = null)
        {
            return await WebHelper.GetAsync<T>(uri, headers, new AuthenticationHeaderValue("Basic", token));
        }

        /// <summary>
        /// HttpClient 用GET方法访问指定URI <c>用完注意调用HttpContent.Dispose方法</c>
        /// </summary>
        /// <param name="uri">请求发送到的 URI。</param>
        /// <param name="token">Basic 验证模式的令牌</param>
        /// <param name="headers">请求的头部信息</param>
        /// <returns></returns>
        public static async Task<HttpContent> GetAsync(string uri, string token, IDictionary<string, string> headers = null)
        {
            HttpClient c = null;

            try
            {
                c = new HttpClient();
                if (headers != null) foreach (var kv in headers) c.DefaultRequestHeaders.Add(kv.Key, kv.Value);
                if (!string.IsNullOrEmpty(token)) c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", token);


                var r = await c.GetAsync(uri);
                return r.Content;
            }
            catch (WebException we)
            {
                WebHelper.ThrowWebException(we);
                throw;
            }
            finally
            {
                if (c != null) c.Dispose();
            }
        }

        /// <summary>
        /// 发起 HTTP  请求并返回 HTTP 响应
        /// </summary>
        /// <param name="request">请求参数</param>
        /// <param name="tryTimes">出现错误时重试次数</param>
        /// <param name="sleep">重试等待毫秒数，默认500</param>
        /// <returns></returns>
        public static async Task<HttpResponseMessage> SendAsync(string uri, HttpMethod method, object obj = null, string contentType = "application/json",
            IDictionary<string, string> headers = null, int? timeout = null, Encoding encoding = null, WebProxy proxy = null, int tryTimes = 3, int sleep = 500)
        {
            var handler = proxy != null ? new HttpClientHandler
            {
                Proxy = proxy,
                UseProxy = true,
            } : null;
            var client = handler != null ? new HttpClient(handler) : new HttpClient();
            if (timeout != null) client.Timeout = new System.TimeSpan(0, 0, 0, 0, timeout.Value);
            //client.DefaultRequestHeaders.Clear();
            if (headers != null)
            {
                string scheme = null;
                string token = null;
                foreach (var kv in headers)
                {
                    if (string.Equals(kv.Key, "scheme", System.StringComparison.CurrentCultureIgnoreCase)) scheme = kv.Key;
                    else if (string.Equals(kv.Key, "token", System.StringComparison.CurrentCultureIgnoreCase)) token = kv.Key;
                    else client.DefaultRequestHeaders.Add(kv.Key, kv.Value);
                }

                if (token != null)
                {
                    if (scheme == null) scheme = "Basic";
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", token);
                }
            }

            var request = new HttpRequestMessage(method, uri);
            string content = null;
            if (obj != null && obj is string) content = System.Convert.ToString(obj);
            else if (obj != null) content = SerializeHelper.SerializeToJson(obj);
            if (content != null)
            {
                var httpContent = new StringContent(content, encoding ?? Encoding.UTF8, contentType);
                request.Content = httpContent;
            }

            var requestUri = request.RequestUri;
            if (requestUri != null)
            {
#if netcore
                if (requestUri.Scheme== "https") ServicePointManager.SecurityProtocol = SecurityProtocolType.SystemDefault | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
#endif
#if net45
                if (requestUri.Scheme == "https") ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
#endif
#if net40
                if (requestUri.Scheme== "https") ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | (SecurityProtocolType)768 | (SecurityProtocolType)3072;
#endif
            }

            try
            {
                var response = await client.SendAsync(request);
                return response;
            }
            catch (WebException we)
            {
                tryTimes--;
                if (tryTimes > 0)
                {
                    System.Threading.Thread.Sleep(sleep);
                    return WebHelper.Send(request, tryTimes);
                }
                else
                {
                    WebHelper.ThrowWebException(we);
                    throw;
                }
            }

            //MultipartFormDataContent => multipart / form - data
            //FormUrlEncodedContent => application / x - www - form - urlencoded
            //StringContent => application / json等
            //StreamContent => binary
        }

        #endregion
    }
}
