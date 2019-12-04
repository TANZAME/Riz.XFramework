
using System;
using System.Net;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.Collections.Generic;
using System.IO;

namespace TZM.XFramework
{
    /// <summary>
    /// WEB助手类
    /// </summary>
    public partial class WebHelper
    {
        #region 网络

        /// <summary>
        /// HttpClient 用GET方法访问指定URI
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="uri">请求发送到的 URI。</param>
        /// <param name="headers">请求的头部信息</param>
        /// <returns></returns>
        public static async Task<T> GetAsync<T>(string uri, IDictionary<string, string> headers = null)
        {
            var configuration = new HttpConfiguration<T> { Headers = headers };
            return await WebHelper.GetAsync<T>(uri, configuration);
        }

        /// <summary>
        /// 使用 GET 方式提交请求
        /// </summary>
        /// <param name="uri">请求路径</param>
        /// <param name="configuration">HTTP 配置</param>
        public static async Task<T> GetAsync<T>(string uri, HttpConfiguration configuration)
        {
            if (configuration == null) configuration = new HttpConfiguration<T>();
            configuration.Method = HttpMethod.Get;

            var conf = configuration as HttpConfiguration<T>;
            var response = await WebHelper.SendAsync(uri, configuration);
            return await WebHelper.ReadAsResultAsync<T>(response, true, conf != null ? conf.Deserializer : null);
        }

        /// <summary>
        /// 使用 POST 方式提交请求
        /// </summary>
        /// <typeparam name="T">返回类型，如果是 string 类型则直接返回原生 JSON </typeparam>
        /// <param name="uri">请求路径</param>
        /// <param name="content">参数内容，如果不是字符类型则序列化成字符串</param>
        /// <param name="headers">HTTP 标头的键值对</param>
        /// <param name="contentType">内容类型</param>
        public static async Task<T> PostAsync<T>(string uri, object content, string contentType = "application/json", IDictionary<string, string> headers = null)
        {
            var configuration = new HttpConfiguration<T>
            {
                Content = content,
                ContentType = contentType,
                Headers = headers,
            };
            return await WebHelper.PostAsync<T>(uri, configuration);
        }

        /// <summary>
        /// 使用 POST 方式提交请求，需要调用方自行释放响应对象
        /// </summary>
        /// <param name="uri">请求路径</param>
        /// <param name="configuration">HTTP 配置</param>
        public static async Task<T> PostAsync<T>(string uri, HttpConfiguration configuration)
        {
            if (configuration == null) configuration = new HttpConfiguration<T>();
            configuration.Method = HttpMethod.Post;

            var conf = configuration as HttpConfiguration<T>;
            var response = await WebHelper.SendAsync(uri, configuration);
            return await WebHelper.ReadAsResultAsync<T>(response, true, conf != null ? conf.Deserializer : null);
        }

        /// <summary>
        /// 使用 DELETE 方式提交请求
        /// </summary>
        /// <typeparam name="T">返回类型，如果是 string 类型则直接返回原生 JSON </typeparam>
        /// <param name="uri">请求路径</param>
        /// <param name="content">参数内容，如果不是字符类型则序列化成字符串</param>
        /// <param name="headers">HTTP 标头的键值对</param>
        /// <param name="contentType">内容类型</param>
        public static async Task<T> DeleteAsync<T>(string uri, object content, string contentType = "application/json", IDictionary<string, string> headers = null)
        {
            var configuration = new HttpConfiguration<T>
            {
                Content = content,
                ContentType = contentType,
                Headers = headers,
            };
            return await WebHelper.DeleteAsync<T>(uri, configuration);
        }

        /// <summary>
        /// 使用 POST 方式提交请求，需要调用方自行释放响应对象
        /// </summary>
        /// <param name="uri">请求路径</param>
        /// <param name="configuration">HTTP 配置</param>
        public static async Task<T> DeleteAsync<T>(string uri, HttpConfiguration configuration)
        {
            if (configuration == null) configuration = new HttpConfiguration<T>();
            configuration.Method = HttpMethod.Delete;

            var conf = configuration as HttpConfiguration<T>;
            var response = await WebHelper.SendAsync(uri, configuration);
            return await WebHelper.ReadAsResultAsync<T>(response, true, conf != null ? conf.Deserializer : null);
        }

        /// <summary>
        /// 发起 HTTP，需要调用方自行释放响应对象
        /// </summary>
        /// <param name="uri">请求路径</param>
        /// <param name="configuration">HTTP 配置</param>
        /// <returns></returns>
        public static async Task<HttpResponseMessage> SendAsync(string uri, HttpConfiguration configuration)
        {
            int tryTimes = configuration != null && configuration.TryTimes != null ? configuration.TryTimes.Value : 0;
            int sleep = configuration != null && configuration.Sleep != null ? configuration.Sleep.Value : 500;

            return await WebHelper.SendAsync(uri, configuration, tryTimes, sleep);
        }

        // 发起 HTTP请求
        static async Task<HttpResponseMessage> SendAsync(string uri, HttpConfiguration configuration, int tryTimes, int sleep)
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

            // 初始化 HTTP 客户端
            HttpClientHandler handler = null;
            if (configuration != null && configuration.Proxy != null) handler = new HttpClientHandler
            {
                Proxy = configuration.Proxy,
                UseProxy = true,
            };
            var client = handler != null ? new HttpClient(handler) : new HttpClient();
            if (configuration != null)
            {
                if (configuration.Timeout != null) client.Timeout = new System.TimeSpan(0, 0, 0, 0, configuration.Timeout.Value);
                if (configuration.Headers != null)
                {
                    // Authorization TODO
                    string scheme = null;
                    string token = null;
                    foreach (var kv in configuration.Headers)
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
            }

            // 初始化 HTTP 请求
            var method = System.Net.Http.HttpMethod.Get;
            if (configuration != null)
            {
                if (configuration.Method == HttpMethod.Get) method = System.Net.Http.HttpMethod.Get;
                else if (configuration.Method == HttpMethod.Post) method = System.Net.Http.HttpMethod.Post;
                else if (configuration.Method == HttpMethod.Put) method = System.Net.Http.HttpMethod.Put;
                else if (configuration.Method == HttpMethod.Delete) method = System.Net.Http.HttpMethod.Delete;
                else if (configuration.Method == HttpMethod.Head) method = System.Net.Http.HttpMethod.Head;
                else if (configuration.Method == HttpMethod.Head) method = System.Net.Http.HttpMethod.Head;
                else if (configuration.Method == HttpMethod.Trace) method = System.Net.Http.HttpMethod.Trace;
                else if (configuration.Method == HttpMethod.Options) method = System.Net.Http.HttpMethod.Options;
            }
            var request = new HttpRequestMessage(method, uri);
            string content = null;
            if (configuration.Content != null && configuration.Content is string) content = (string)configuration.Content;
            else if (configuration.Content != null) content = SerializeHelper.SerializeToJson(configuration.Content);
            if (content != null)
            {
                var encoding = configuration.Encoding ?? Encoding.UTF8;
                var contentType = "application/json";
                var httpContent = new StringContent(content, encoding ?? Encoding.UTF8, contentType);
                request.Content = httpContent;
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
                    return await WebHelper.SendAsync(uri, configuration, tryTimes, sleep);
                }
                else
                {
                    WebHelper.ThrowWebException(we);
                    throw;
                }
            }

            //MultipartFormDataContent => multipart/form-data
            //FormUrlEncodedContent => application/x-www-form-urlencoded
            //StringContent => application/json
            //StreamContent => binary
        }

        // 从响应流中读取响应为实体
        static async Task<T> ReadAsResultAsync<T>(HttpResponseMessage response, bool disposing = true, Func<string, T> deserializer = null)
        {
            Stream stream = null;
            try
            {
                stream = await response.Content.ReadAsStreamAsync();
                return WebHelper.ReadAsResult<T>(stream, disposing, deserializer);
            }
            finally
            {
                if (disposing)
                {
                    if (stream != null) stream.Close();
                    if (response != null) response.Dispose();
                }
            }
        }

        #endregion
    }
}
