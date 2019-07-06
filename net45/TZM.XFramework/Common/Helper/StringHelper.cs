using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic;
using System.Web;

using System.IO;

namespace TZM.XFramework
{
    /// <summary>
    /// 字符串操作类
    /// </summary>
    public class StringHelper
    {
        #region 截取左中右端字符串函数

        /// <summary>
        /// 截取左边字符串函数
        /// </summary>
        /// <param name="sourceStr">原字符串</param>
        /// <param name="length">截取字符串的长度(一个汉字计2个单位长度)</param>
        /// <returns>截取后的字符串</returns>
        public static string Left(string sourceStr, int length, string appendStr)
        {
            if (sourceStr == null || sourceStr == "") { return string.Empty; }
            if (length < 0) { length = 0; }
            string sdot = string.Empty;
            StringBuilder sb = new StringBuilder();
            int n = 0;
            foreach (char ch in sourceStr)
            {
                n += System.Text.Encoding.Default.GetByteCount(ch.ToString());
                if (n > length)
                {
                    sdot = appendStr;
                    break;
                }
                else
                {
                    sb.Append(ch);
                }
            }
            return sb.Append(sdot).ToString();
        }

        /// <summary>
        /// 截取右边字符串函数
        /// </summary>
        /// <param name="Str">原字符串</param>
        /// <param name="Num">截取字符串的长度</param>
        /// <param name="length">截取字符串后省略部分的字符串(一个汉字计2个单位长度)</param>
        /// <returns>截取后的字符串</returns>
        public static string Left(string Str, int length)
        {
            return Left(Str, length, "");
        }

        /// <summary>
        /// 截取右边字符串
        /// </summary>
        /// <param name="sourceStr">原字符串</param>
        /// <param name="length">截取字符串的长度(一个汉字计2个单位长度)</param>
        /// <returns>截取后的字符串</returns>
        public static string Right(string sourceStr, int length)
        {
            if (sourceStr == null || sourceStr == "") { return string.Empty; }
            if (length < 0) { length = 0; }
            StringBuilder sb = new StringBuilder();
            int n = 0;
            for (int i = sourceStr.Length - 1; i >= 0; i--)
            {
                n += System.Text.Encoding.Default.GetByteCount(sourceStr[i].ToString());
                if (n > length)
                {
                    break;
                }
                else
                {
                    sb.Insert(0, sourceStr[i]);
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// 截取中间字符串
        /// </summary>
        /// <param name="sourceStr">原字符串</param>
        /// <param name="startIndex">起始位置索引(一个汉字计2个单位长度)</param>
        /// <returns>截取后的字符串</returns>
        public static string Mid(string sourceStr, int startIndex)
        {
            if (sourceStr == null || sourceStr == "") { return string.Empty; }
            if (startIndex > System.Text.Encoding.Default.GetByteCount(sourceStr)) { return string.Empty; }
            StringBuilder sb = new StringBuilder();
            int index = 0;
            foreach (char ch in sourceStr)
            {
                index += System.Text.Encoding.Default.GetByteCount(ch.ToString());
                if (index >= startIndex)
                {
                    sb.Append(ch);
                }
            }
            return sb.ToString();
        }
        /// <summary>
        /// 截取中间字符串
        /// </summary>
        /// <param name="sourceStr">原字符串</param>
        /// <param name="startIndex">起始位置索引(一个汉字计2个单位长度)</param>
        /// <param name="length">长度(一个汉字计2个单位长度)</param>
        /// <returns>截取后的字符串</returns>
        public static string Mid(string sourceStr, int startIndex, int length)
        {
            if (sourceStr == null || sourceStr == "") { return string.Empty; }
            if (startIndex > System.Text.Encoding.Default.GetByteCount(sourceStr)) { return string.Empty; }
            StringBuilder sb = new StringBuilder();
            int index = 0;
            int currentLength = 0;
            foreach (char ch in sourceStr)
            {
                int chLength = System.Text.Encoding.Default.GetByteCount(ch.ToString());
                index += chLength;
                if (index < startIndex)
                {
                    continue;
                }
                currentLength += chLength;
                if (currentLength > length)
                {
                    break;
                }
                sb.Append(ch);

            }
            return sb.ToString();
        }

        #endregion

        #region Substring方式截取字符串
        /// <summary>
        /// 截取左边指位数字符
        /// </summary>
        /// <param name="sourceStr">原字符串</param>
        /// <param name="lenght">长度</param>
        /// <param name="ellipsis">省略号</param>
        /// <returns></returns>
        public static string SubstringLeft(string sourceStr, int lenght, bool ellipsis = false)
        {
            if (string.IsNullOrEmpty(sourceStr)) return string.Empty;
            if (lenght < 0) lenght = 0;
            if (sourceStr.Length < lenght) return sourceStr;
            if (ellipsis) return sourceStr.Substring(0, lenght) + "...";
            else return sourceStr.Substring(0, lenght);
        }

        /// <summary>
        /// 截取从指定位置开始的之后所有字符
        /// </summary>
        /// <param name="sourceStr">原字符串</param>
        /// <param name="startIndex">起始位置索引</param>
        /// <returns></returns>
        public static string SubstringMid(string sourceStr, int startIndex)
        {
            if (string.IsNullOrEmpty(sourceStr)) return string.Empty;
            if (startIndex >= sourceStr.Length) { return string.Empty; }
            if (startIndex < 0) startIndex = 0;
            return sourceStr.Substring(startIndex);
        }

        /// <summary>     
        ///指符
        /// </summary>
        /// <param name="sourceStr">原字符串</param>
        /// <param name="startIndex">起始位置索引</param>
        /// <param name="lenght">长度</param>
        /// <returns></returns>
        public static string SubstringMid(string sourceStr, int startIndex, int lenght)
        {
            if (string.IsNullOrEmpty(sourceStr)) { return string.Empty; }
            if (startIndex >= sourceStr.Length) { return string.Empty; }
            if (startIndex < 0) startIndex = 0;
            if (lenght < 0) lenght = 0;
            return sourceStr.Substring(startIndex, sourceStr.Length < startIndex + lenght ? sourceStr.Length - startIndex : lenght);
        }

        /// <summary>
        /// 截取右边指字位数字符
        /// </summary>
        /// <param name="sourceStr">原字符串</param>
        /// <param name="lenght">长度</param>
        /// <returns></returns>
        public static string SubstringRight(string sourceStr, int lenght)
        {
            if (string.IsNullOrEmpty(sourceStr)) return string.Empty;
            if (lenght < 0) lenght = 0;
            return sourceStr.Substring(sourceStr.Length < lenght ? 0 : sourceStr.Length - lenght, sourceStr.Length < lenght ? sourceStr.Length : lenght);
        }

        #endregion

        #region 去除前尾部字符串Trim(), TrimStart(), TrimEnd()
        /// <summary>
        /// 去除前部指定字符串
        /// </summary>
        /// <param name="source">原字符串</param>
        /// <param name="trimStrs">去除的字符串数组</param>
        /// <returns></returns>
        public static string TrimStart(string source, params string[] trimStrs)
        {
            return TrimStart(source, false, trimStrs);
        }

        /// <summary>
        /// 去除前部指定字符串
        /// </summary>
        /// <param name="source">原字符串</param>
        /// <param name="ignoreCase">是否忽略大小写，true忽略大小写</param>
        /// <param name="trimStrs">去除的字符串数组</param>
        /// <returns></returns>
        public static string TrimStart(string source, bool ignoreCase, params string[] trimStrs)
        {
            if (string.IsNullOrEmpty(source) || trimStrs.Length == 0)
            {
                return source;
            }
            string regExStr = @"^(" + string.Join("|", trimStrs) + ")";
            string newSource = string.Empty;
            while (true)
            {
                RegexOptions regOptions;
                if (ignoreCase) { regOptions = RegexOptions.IgnoreCase; } else { regOptions = RegexOptions.None; }
                newSource = System.Text.RegularExpressions.Regex.Replace(source, regExStr, "", regOptions);
                if (newSource == source)
                {
                    break;
                }
                source = newSource;
            }
            return source;

        }

        /// <summary>
        /// 去除尾部指定字符串
        /// </summary>
        /// <param name="source">原字符串</param>
        /// <param name="trimStr">去除的字符串数组</param>
        /// <returns></returns>
        public static string TrimEnd(string source, params string[] trimStrs)
        {
            return TrimEnd(source, false, trimStrs);
        }

        /// <summary>
        /// 去除尾部指定字符串
        /// </summary>
        /// <param name="source">原字符串</param>
        /// <param name="ignoreCase">是否忽略大小写，true忽略大小写</param>
        /// <param name="trimStr">去除的字符串数组</param>
        /// <returns></returns>
        public static string TrimEnd(string source, bool ignoreCase, params string[] trimStrs)
        {
            if (string.IsNullOrEmpty(source) || trimStrs.Length == 0)
            {
                return source;
            }
            string regExStr = @"(" + string.Join("|", trimStrs) + ")$";
            string newSource = string.Empty;
            while (true)
            {
                RegexOptions regOptions;
                if (ignoreCase) { regOptions = RegexOptions.IgnoreCase; } else { regOptions = RegexOptions.None; }
                newSource = System.Text.RegularExpressions.Regex.Replace(source, regExStr, "", regOptions);
                if (newSource == source)
                {
                    break;
                }
                source = newSource;
            }
            return source;
        }

        /// <summary>
        /// 去除前尾部指定字符串
        /// </summary>
        /// <param name="source">原字符串</param>
        /// <param name="trimStr">去除的字符串数组</param>
        /// <returns></returns>
        public static string Trim(string source, params string[] trimStrs)
        {
            return Trim(source, false, trimStrs);
        }
        /// <summary>
        /// 去除前尾部指定字符串
        /// </summary>
        /// <param name="source">原字符串</param>
        /// <param name="ignoreCase">是否忽略大小写，true忽略大小写</param>
        /// <param name="trimStr">去除的字符串数组</param>
        /// <returns></returns>
        public static string Trim(string source, bool ignoreCase, params string[] trimStrs)
        {
            source = TrimStart(source, ignoreCase, trimStrs);
            source = TrimEnd(source, ignoreCase, trimStrs);
            return source;
        }
        #endregion

        //#region 获取字符串的长度
        ///// <summary>
        ///// 获取字符串的长度(一个汉字占两个长度)
        ///// </summary>
        ///// <param name="obj"></param>
        ///// <returns>字符串长度</returns>
        //public static int GetStringLength(object obj)
        //{
        //    string str = Commons.ToString(obj);
        //    if (string.IsNullOrEmpty(str)) { return 0; }
        //    int n = 0;
        //    foreach (char ch in str)
        //    {
        //        n += System.Text.Encoding.Default.GetByteCount(ch.ToString());
        //    }
        //    return n;
        //}
        //#endregion

        #region 获取字符串出现次数
        /// <summary>
        /// 获取关键字在字符串中出现次数
        /// </summary>
        /// <param name="sourceStr">原字符串</param>
        /// <param name="key">关键字</param>
        /// <returns>次数</returns>
        public static int GetRepeatCount(string sourceStr, string key)
        {
            //StringBuilder sb = new StringBuilder(sourceStr);
            //int len = sb.Length - sb.Replace(key, "").Length;
            //int count = len / key.Length;
            //return count;
            return Regex.Matches(sourceStr, key).Count;
        }
        #endregion

        #region 获取数字出现次数
        /// <summary>
        /// 获取数字出现次数
        /// </summary>
        /// <param name="sourceStr">源字符串</param>
        /// <returns></returns>
        public static int GetNumberRepeatCount(string sourceStr)
        {
            int totalCount = Encoding.Default.GetByteCount(sourceStr);
            int count = totalCount - Encoding.Default.GetByteCount(Regex.Replace(sourceStr, "[0-9]+", "", RegexOptions.IgnoreCase));
            return count;
        }
        #endregion

        #region 获取字母出现次数
        /// <summary>
        /// 获取字母出现次数
        /// </summary>
        /// <param name="sourceStr">源字符串</param>
        /// <returns></returns>
        public static int GetAlphabetRepeatCount(string sourceStr)
        {
            int totalCount = Encoding.Default.GetByteCount(sourceStr);
            int count = totalCount - Encoding.Default.GetByteCount(Regex.Replace(sourceStr, "[a-zA-Z]+", "", RegexOptions.IgnoreCase));
            return count;
        }
        #endregion

        #region 获取中文出现次数
        /// <summary>
        /// 获取中文出现次数
        /// </summary>
        /// <param name="sourceStr">源字符串</param>
        /// <returns></returns>
        public static int GetChineseRepeatCount(string sourceStr)
        {
            int totalCount = Encoding.Default.GetByteCount(sourceStr);
            int count = totalCount - Encoding.Default.GetByteCount(Regex.Replace(sourceStr, "[\u4e00-\u9fa5]", "", RegexOptions.IgnoreCase));
            return count;
        }
        #endregion

        #region 移除iframe
        /// <summary>
        /// 移除iframe
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        public static string RemoveIframe(string html)
        {
            if (string.IsNullOrEmpty(html)) return string.Empty;
            System.Text.RegularExpressions.Regex regex1 = new System.Text.RegularExpressions.Regex(@"<iframe[\s\S]+</iframe *>", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            System.Text.RegularExpressions.Regex regex2 = new System.Text.RegularExpressions.Regex(@"<frameset[\s\S]+</frameset *>", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            html = regex1.Replace(html, ""); //过滤<iframe></iframe>标记
            html = regex2.Replace(html, ""); //过滤<frameset></frameset>标记
            return html;
        }
        #endregion

        #region 移除Script
        /// <summary>
        /// 移除Script
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        public static string RemoveScript(string html)
        {
            if (string.IsNullOrEmpty(html)) return string.Empty;
            System.Text.RegularExpressions.Regex regex1 = new System.Text.RegularExpressions.Regex(@"<script[\s\S]+</script *>", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            html = regex1.Replace(html, ""); //过滤<script></script>标记
            return html;
        }
        #endregion

        #region 过滤HTML中的不安全标签
        /// <summary>
        /// 过滤HTML中的不安全标签
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public static string RemoveUnsafeHtml(string html)
        {
            html = Regex.Replace(html, @"(\<|\s+)o([a-z]+\s?=)", "$1$2", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"(script|frame|form|meta|behavior|style)([\s|:|>])+", "$1.$2", RegexOptions.IgnoreCase);
            return html;
        }
        #endregion

        #region 移除Html
        /// <summary>
        /// 移除Html
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string RemoveHTML(object input)
        {
            if (input == null)
            {
                return string.Empty;
            }
            else
            {
                string html = input.ToString();
                if (string.IsNullOrEmpty(html)) return string.Empty;
                System.Text.RegularExpressions.Regex regex1 = new System.Text.RegularExpressions.Regex(@"<[^>]*>", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                html = regex1.Replace(html, string.Empty);
                return html.Replace("\"", "“");
            }
        }
        #endregion

        #region 过滤日记HTML语法
        /// <summary>
        /// 过滤日记HTML语法
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        public static string FilterLogHTML(string html)
        {
            if (string.IsNullOrEmpty(html)) return string.Empty;
            if (html.ToLower().IndexOf("<img") > -1 && html.ToLower().IndexOf(">") > -1)
            {
                html = "<span class=\"sp_reshow\">【图文日志】</span>" + html;
            }
            System.Text.RegularExpressions.Regex regex1 = new System.Text.RegularExpressions.Regex(@"<[^>]*>", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            html = regex1.Replace(html, string.Empty);
            return html;
        }
        #endregion

        #region 移除所有的HTML标签
        /// <summary>
        /// 移除所有的HTML标签
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string RemoveAllHtml(string str)
        {
            if (str == null || string.IsNullOrEmpty(str))
            {
                return string.Empty;
            }
            return Regex.Replace(str, @"<[\/\!]*?[^<>]*?>", "");
        }
        #endregion

        #region 去除除IMG以外的HTML
        /// <summary>
        /// 去除除IMG以外的HTML
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        public static string LoseExceptImg(string html)
        {
            if (string.IsNullOrEmpty(html)) return string.Empty;
            System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex(@"<(?!/?img)[\s\S]*?>", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            html = regex.Replace(html, "");
            return html;
        }
        #endregion

        #region 把图文分解成图，文
        /// <summary>
        /// 把图文分解成图，文
        /// </summary>
        /// <param name="html">输入HTML</param>
        /// <param name="img">图</param>
        /// <param name="text">文</param>
        public static void SplitImgAndText(string html, out string img, out string text)
        {
            if (html == null)
            {
                img = "";
                text = "";
                return;
            }
            Regex regex = new System.Text.RegularExpressions.Regex(@"<img[\s\S]+</img *>|<img[^>]+/? *>", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            Match m = regex.Match(html);
            string imgstr = string.Empty;
            while (m.Success)
            {
                imgstr += m.Value;
                m = m.NextMatch();
            }
            string textStr = RemoveHTML(html);
            img = imgstr;
            text = textStr;
        }
        #endregion

        #region 替换html中的特殊字符
        /// <summary>
        /// 替换html中的特殊字符
        /// </summary>
        /// <param name="Str">需要进行替换的文本。</param>
        /// <returns>替换完的文本。</returns>
        public static string HtmlEncode(string Str)
        {
            Str = Str.Replace(">", "&gt;");
            Str = Str.Replace("<", "&lt;");
            Str = Str.Replace("  ", " &nbsp;");
            Str = Str.Replace("  ", " &nbsp;");
            Str = Str.Replace("\"", "&quot;");
            Str = Str.Replace("\'", "&#39;");
            Str = Str.Replace("\r\n", "<br/>");
            return Str;
        }
        #endregion

        #region 恢复html中的特殊字符
        /// <summary>
        /// 恢复html中的特殊字符
        /// </summary>
        /// <param name="Str">需要恢复的文本。</param>
        /// <returns>恢复好的文本。</returns>
        public static string HtmlDiscode(string Str)
        {
            Str = Str.Replace("&gt;", ">");
            Str = Str.Replace("&lt;", "<");
            Str = Str.Replace("&nbsp;", " ");
            Str = Str.Replace(" &nbsp;", "  ");
            Str = Str.Replace("&quot;", "\"");
            Str = Str.Replace("&#39;", "\'");
            Str = Str.Replace("<br/>", "\r\n");
            return Str;
        }
        #endregion

        #region 半角转全角
        /// <summary>
        /// 半角转全角
        /// </summary>
        /// <param name="str">字符串</param>
        /// <returns></returns>
        public static string GetSBCcase(string str)
        {
            char[] c = str.ToCharArray();
            for (int i = 0; i < c.Length; i++)
            {
                byte[] b = System.Text.Encoding.Unicode.GetBytes(c, i, 1);
                if (b.Length == 2)
                {
                    if (b[1] == 0)
                    {
                        b[0] = (byte)(b[0] - 32);
                        b[1] = 255;
                        c[i] = System.Text.Encoding.Unicode.GetChars(b)[0];
                    }
                }
            }

            string strNew = new string(c);
            return strNew;
        }
        #endregion

        #region 全角转半角
        /// <summary>
        /// 全角转半角
        /// </summary>
        /// <param name="Str"></param>
        /// <returns></returns>
        public static string GetDBCcase(string Str)
        {
            char[] c = Str.ToCharArray();
            for (int i = 0; i < c.Length; i++)
            {
                byte[] b = System.Text.Encoding.Unicode.GetBytes(c, i, 1);
                if (b.Length == 2)
                {
                    if (b[1] == 255)
                    {
                        b[0] = (byte)(b[0] + 32);
                        b[1] = 0;
                        c[i] = System.Text.Encoding.Unicode.GetChars(b)[0];
                    }
                }
            }
            string strNew = new string(c);
            return strNew;
        }
        #endregion
        
        #region 获取两个指定字符串间的数据列表（开闭标记不需要完全匹配）
        /// <summary>
        /// 获取两个指定字符串间的数据列表（开闭标记不需要完全匹配）
        /// </summary>
        /// <param name="sourceStr">源字符串</param>
        /// <param name="startStr">开始字符串</param>
        /// <param name="endStr">结束字符串</param>
        /// <returns></returns>
        public static List<string> SearchString(string sourceStr, string startStr, string endStr)
        {
            return SearchString(sourceStr, startStr, endStr, false);
        }

        /// <summary>
        /// 获取两个指定字符串间的数据列表（开闭标记不需要完全匹配）
        /// </summary>
        /// <param name="sourceStr">源字符串</param>
        /// <param name="startStr">开始字符串</param>
        /// <param name="endStr">结束字符串</param>
        /// <param name="isAppendStartAndEnd">是否附加开始和结束字符串</param>
        /// <returns></returns>
        public static List<string> SearchString(string sourceStr, string startStr, string endStr, bool isAppendStartAndEnd)
        {
            string tempStr = sourceStr;
            List<string> myList = new List<string>();
            int startStrLength = startStr.Length;
            int endStrLength = endStr.Length;
            if (string.IsNullOrEmpty(tempStr) || startStrLength == 0 || endStrLength == 0)
            {
                return myList;
            }
            while (true)
            {
                int startStrIndex = tempStr.IndexOf(startStr);
                if (tempStr.Length < startStrLength || startStrIndex < 0)
                {
                    break;
                }
                tempStr = tempStr.Remove(0, startStrIndex + startStrLength);    //去除开始标记
                int endStrIndex = tempStr.IndexOf(endStr);
                if (tempStr.Length < endStrLength || endStrIndex < 0)
                {
                    break;
                }
                myList.Add((isAppendStartAndEnd ? startStr : "") + tempStr.Substring(0, endStrIndex) + (isAppendStartAndEnd ? endStr : ""));
                tempStr = tempStr.Remove(0, endStrIndex);   //去除中间的内容（结束标记被保留）
            }
            return myList;
        }
        #endregion

        #region 获得两个匹配字符串间的数据列表（开闭标记必须完全匹配）
        /// <summary>
        /// 获得两个匹配字符串间的数据列表（开闭标记必须完全匹配）
        /// </summary>
        /// <param name="sourceStr">源字符串</param>
        /// <param name="startStr">开始字符串</param>
        /// <param name="endStr">结束字符串</param>
        /// <returns></returns>
        public static List<string> MatchString(string sourceStr, string startStr, string endStr)
        {
            return MatchString(sourceStr, startStr, endStr, false);
        }

        /// <summary>
        /// 获得两个匹配字符串间的数据列表（开闭标记必须完全匹配）
        /// </summary>
        /// <param name="sourceStr">源字符串</param>
        /// <param name="startStr">开始字符串</param>
        /// <param name="endStr">结束字符串</param>
        /// <param name="isAppendStartAndEnd">是否附加开始和结束字符串</param>
        /// <returns></returns>
        public static List<string> MatchString(string sourceStr, string startStr, string endStr, bool isAppendStartAndEnd)
        {
            List<string> myList = new List<string>();
            if (string.IsNullOrEmpty(sourceStr))
            {
                return myList;
            }
            Regex reg;
            if (isAppendStartAndEnd)
            {
                reg = new Regex(Regex.Escape(startStr) + "(.*?)" + Regex.Escape(endStr));
                foreach (Match match in reg.Matches(sourceStr))
                {
                    myList.Add(match.Value);
                }
                return myList;
            }
            else
            {
                reg = new Regex(Regex.Escape(startStr) + "(?<group>.*?)" + Regex.Escape(endStr));
                foreach (Match match in reg.Matches(sourceStr))
                {
                    myList.Add(match.Groups["group"].Value);
                }
                return myList;
            }
        }
        #endregion

        #region 第一个字符大小
        public static void FirstCharUpper(ref string text)
        {
            string str = text.Substring(0, 1).ToUpper();
            text = str + text.Substring(1, text.Length - 1);
        }
        #endregion

        #region 传参编码
        /// <summary>
        /// 传参编码
        /// </summary>
        /// <param name="s">参数值</param>
        /// <returns></returns>
        public static string Escape(string s)
        {
            StringBuilder sb = new StringBuilder();
            byte[] ba = System.Text.Encoding.Unicode.GetBytes(s);
            for (int i = 0; i < ba.Length; i += 2)
            {
                if (ba[i + 1] == 0)
                {
                    //数字,大小写字母,以及"+-*/._"不变
                    if (
                          (ba[i] >= 48 && ba[i] <= 57)
                        || (ba[i] >= 64 && ba[i] <= 90)
                        || (ba[i] >= 97 && ba[i] <= 122)
                        || (ba[i] == 42 || ba[i] == 43 || ba[i] == 45 || ba[i] == 46 || ba[i] == 47 || ba[i] == 95)
                        )//保持不变
                    {
                        sb.Append(Encoding.Unicode.GetString(ba, i, 2));

                    }
                    else//%xx形式
                    {
                        sb.Append("%");
                        sb.Append(ba[i].ToString("X2"));
                    }
                }
                else
                {
                    sb.Append("%u");
                    sb.Append(ba[i + 1].ToString("X2"));
                    sb.Append(ba[i].ToString("X2"));
                }
            }
            return sb.ToString();
        }
        #endregion

        #region Url编码、解码
        /// <summary>
        /// URL编码
        /// </summary>
        /// <param name="str">原字符串</param>
        /// <returns>字符串</returns>
        public static string UrlEncode(string str)
        {
            return UrlEncode(str, System.Text.Encoding.UTF8);
        }

        /// <summary>
        /// URL编码
        /// </summary>
        /// <param name="str">原字符串</param>
        /// <param name="encode">编码类型</param>
        /// <returns>字符串</returns>
        public static string UrlEncode(string str, Encoding encode)
        {
            return System.Web.HttpUtility.UrlEncode(str, encode);
        }

        /// <summary>
        /// URL解码
        /// </summary>
        /// <param name="str">编码过的字符串</param>
        /// <returns>字符串</returns>
        public static string UrlDecode(string str)
        {
            return UrlDecode(str, System.Text.Encoding.UTF8);
        }

        /// <summary>
        /// URL解码
        /// </summary>
        /// <param name="str">编码过的字符串</param>
        /// <param name="encode">编码类型</param>
        /// <returns>字符串</returns>
        public static string UrlDecode(string str, Encoding encode)
        {
            return System.Web.HttpUtility.UrlDecode(str, encode);
        }

        #endregion

        #region Html编码、解码
        /// <summary>
        /// 将字符转化为HTML编码
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string HtmlEncode(object str)
        {
            return HtmlEncode(str, false);
        }

        /// <summary>
        /// 将字符转化为HTML编码
        /// </summary>
        /// <param name="str"></param>
        /// <param name="isReserveBR">是否保留&lt;br/&gt;</param>
        /// <returns></returns>
        public static string HtmlEncode(object str, bool isReserveBR)
        {
            string str2 = ToString(str);
            if (string.IsNullOrEmpty(str2))
            {
                return string.Empty;
            }
            if (isReserveBR)
            {
                return HttpContext.Current.Server.HtmlEncode(str2).Replace("\r\n", "<br />").Replace("\r", "<br />").Replace("\n", "<br />").Replace("&lt;br /&gt;", "<br />");
            }
            else
            {
                return HttpContext.Current.Server.HtmlEncode(str2);
            }

        }
        /// <summary>
        /// 将字符HTML解码
        /// <param name="str"></param>
        /// <param name="isReserveBR">是否保留&lt;br/&gt;</param>
        /// </summary>
        public static string HtmlDecode(object str)
        {
            return HtmlDecode(str, false);
        }
        /// <summary>
        /// 将字符HTML解码
        /// <param name="str"></param>
        /// <param name="isReserveBR">是否保留&lt;br/&gt;</param>
        /// </summary>
        public static string HtmlDecode(object str, bool isReserveBR)
        {
            string str2 = ToString(str);
            if (string.IsNullOrEmpty(str2))
            {
                return string.Empty;
            }
            if (isReserveBR)
            {
                return System.Web.HttpContext.Current.Server.HtmlDecode(str2).Replace("\r\n", "<br />").Replace("\r", "<br />").Replace("\n", "<br />");
            }
            else
            {
                return HttpContext.Current.Server.HtmlDecode(str2);
            }
        }


        #endregion

        #region Base64编码、解码

        /// <summary>
        /// Base64加密，采用utf8编码方式加密
        /// </summary>
        /// <param name="source">待加密的明文</param>
        /// <returns>加密后的字符串</returns>
        public static string Base64Encode(string source)
        {
            return Base64Encode(source, Encoding.UTF8);
        }

        /// <summary>
        /// Base64加密
        /// </summary>
        /// <param name="source">待加密的明文</param>
        /// <param name="codeName">加密采用的编码方式</param>
        /// <returns></returns>
        public static string Base64Encode(string source, Encoding encode)
        {
            byte[] bytes = encode.GetBytes(source);
            string encodeStr;
            try
            {
                encodeStr = Convert.ToBase64String(bytes);
            }
            catch
            {
                encodeStr = source;
            }
            return encodeStr;
        }

        /// <summary>
        /// Base64解密，采用utf8编码方式解密
        /// </summary>
        /// <param name="result">待解密的密文</param>
        /// <returns>解密后的字符串</returns>
        public static string Base64Decode(string result)
        {
            return Base64Decode(result, Encoding.UTF8);
        }

        /// <summary>
        /// Base64解密
        /// </summary>
        /// <param name="result">待解密的密文</param>
        /// <param name="codeName">解密采用的编码方式，注意和加密时采用的方式一致</param>
        /// <returns>解密后的字符串</returns>
        public static string Base64Decode(string result, Encoding encode)
        {
            string decodeStr = "";
            byte[] bytes = Convert.FromBase64String(result);
            try
            {
                decodeStr = encode.GetString(bytes);
            }
            catch
            {
                decodeStr = result;
            }
            return decodeStr;
        }

        /// <summary>
        /// 转换成字符串
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string ToString(object obj)
        {
            if (obj != null && obj != DBNull.Value)
            {
                return obj.ToString();
            }
            return string.Empty;
        }
        #endregion

        #region Json字符编码
        /// <summary>
        /// 将Json字符串转换成Unicode
        /// </summary>
        /// <param name="jsonString">原字符串</param>
        /// <returns></returns>
        public static string StringToJsonUnicode(string jsonString)
        {
            if (string.IsNullOrEmpty(jsonString)) return "";
            string str = jsonString;
            str = str.Replace("'", "\\u0027");
            str = str.Replace("\"", "\\u0022");
            str = str.Replace("\\", "\\u005C");
            str = str.Replace("/", "\\u002f");
            str = str.Replace("\b", "\\u0008");
            str = str.Replace("\t", "\\u0009");
            str = str.Replace("\f", "\\u000c");
            str = str.Replace("\r", "\\u000d");
            str = str.Replace("\n", "\\u000a");
            return str;
        }
        /// <summary>
        /// 转义Json特殊字符
        /// </summary>
        /// <param name="srcText">原字符串</param>
        /// <returns>转义Json特殊字符后的字符串</returns>
        public static string StringToJson(String srcText)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < srcText.Length; i++)
            {
                char c = srcText.ToCharArray()[i];
                switch (c)
                {
                    case '\"':
                        sb.Append("\\\""); break;
                    case '\\':
                        sb.Append("\\\\"); break;
                    case '/':
                        sb.Append("\\/"); break;
                    case '\b':
                        sb.Append("\\b"); break;
                    case '\f':
                        sb.Append("\\f"); break;
                    case '\n':
                        sb.Append("\\n"); break;
                    case '\r':
                        sb.Append("\\r"); break;
                    case '\t':
                        sb.Append("\\t"); break;
                    default:
                        sb.Append(c); break;
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// 格式化字符型、日期型、布尔型
        /// </summary>
        /// <param name="srcText">原字符串</param>
        /// <param name="type">类型[限字符串、日期、布尔三种]</param>
        /// <returns></returns>
        public static string StringFormat(string srcText, Type type)
        {
            if (type == typeof(string))
            {
                srcText = StringToJson(srcText);
                srcText = "\"" + srcText + "\"";
            }
            else if (type == typeof(DateTime))
            {
                srcText = "\"" + srcText + "\"";
            }
            else if (type == typeof(bool))
            {
                srcText = srcText.ToLower();
            }
            else if (type != typeof(string) && string.IsNullOrEmpty(srcText))
            {
                srcText = "\"" + srcText + "\"";
            }
            return srcText;
        }

        #endregion

        #region unicode与字符串相互转换
        /// <summary>
        /// 将原始字符串转换为unicode,格式为\u....\u....
        /// </summary>
        /// <param name="srcText">原始字符串</param>
        /// <returns>Uncode字符</returns>
        public static string StringToUnicode(string srcText)
        {
            string dst = "";
            char[] src = srcText.ToCharArray();
            for (int i = 0; i < src.Length; i++)
            {
                byte[] bytes = Encoding.Unicode.GetBytes(src[i].ToString());
                string str = @"\u" + bytes[1].ToString("X2") + bytes[0].ToString("X2");
                dst += str;
            }
            return dst;
        }

        /// <summary>
        /// 将Unicode字串\u....\u....格式字串转换为原始字符串
        /// </summary>
        /// <param name="srcText">unicode字符串</param>
        /// <returns>原始字符串</returns>
        public static string UnicodeToString(string srcText)
        {
            string dst = "";
            string src = srcText;
            int len = srcText.Length / 6;

            for (int i = 0; i <= len - 1; i++)
            {
                string str = "";
                str = src.Substring(0, 6).Substring(2);
                src = src.Substring(6);
                byte[] bytes = new byte[2];
                bytes[1] = byte.Parse(int.Parse(str.Substring(0, 2), System.Globalization.NumberStyles.HexNumber).ToString());
                bytes[0] = byte.Parse(int.Parse(str.Substring(2, 2), System.Globalization.NumberStyles.HexNumber).ToString());
                dst += Encoding.Unicode.GetString(bytes);
            }
            return dst;
        }
        #endregion

        /// <summary>
        /// 过滤掉字符串中会引起注入攻击的字符
        /// </summary>
        /// <param name="strchar">要过滤的字符串</param>
        /// <returns>已过滤的字符串</returns>
        public static string FilterBadChar(string strchar)
        {
            string tempstrChar;
            string newstrChar = string.Empty;
            if (string.IsNullOrEmpty(strchar))
            {
                newstrChar = string.Empty;
            }
            else
            {
                tempstrChar = strchar;
                string[] strBadChar = { "+", "'", "%", "^", "&", "?", "(", ")", "<", ">", "[", "]", "{", "}", "/", "\"", ";", ":", "Chr(34)", "Chr(0)", "--" };
                StringBuilder strBuilder = new StringBuilder(tempstrChar);
                for (int i = 0; i < strBadChar.Length; i++)
                {
                    newstrChar = strBuilder.Replace(strBadChar[i], string.Empty).ToString();
                }

                newstrChar = Regex.Replace(newstrChar, "@+", "@");
            }

            return newstrChar;
        }

        /// <summary>
        /// 过滤SQL关键字
        /// </summary>
        /// <param name="strchar">待过滤的字符串</param>
        /// <returns>过滤后的字符串</returns>
        public static string FilterSqlKeyword(string strchar)
        {
            bool contains = false;
            if (string.IsNullOrEmpty(strchar))
            {
                return string.Empty;
            }

            strchar = strchar.ToUpperInvariant();
            string[] keywords = { "SELECT", "UPDATE", "INSERT", "DELETE", "DECLARE", "@", "EXEC", "DBCC", "ALTER", "DROP", "CREATE", "BACKUP", "IF", "ELSE", "END", "AND", "OR", "ADD", "SET", "OPEN", "CLOSE", "USE", "BEGIN", "RETUN", "AS", "GO", "EXISTS", "KILL" };

            for (int i = 0; i < keywords.Length; i++)
            {
                if (strchar.Contains(keywords[i]))
                {
                    strchar = strchar.Replace(keywords[i], string.Empty);
                    contains = true;
                }
            }

            if (contains)
            {
                return FilterSqlKeyword(strchar);
            }

            return strchar;
        }

        /// <summary>
        /// String to int
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static int StringToInt(string input)
        {
            int val = 0;
            if (!string.IsNullOrEmpty(input))
                int.TryParse(input, out val);
            return val;
        }

        /// <summary>
        /// String to int
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static int StringToInt(string input, int defaultVal)
        {
            int val = defaultVal;
            if (!string.IsNullOrEmpty(input))
                int.TryParse(input, out val);
            return val;
        }

        /// <summary>
        /// String to int
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static long StringToLong(string input)
        {
            long val = 0;
            if (!string.IsNullOrEmpty(input))
                long.TryParse(input, out val);
            return val;
        }


        /// <summary>
        /// String to bool
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static bool StringToBool(string input)
        {
            bool val = false;
            if (!string.IsNullOrEmpty(input))
                bool.TryParse(input, out val);
            return val;
        }

        /// <summary>
        /// String to decimal
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static decimal StringToDecimal(string input)
        {
            decimal val = 0;
            if (!string.IsNullOrEmpty(input))
                decimal.TryParse(input, out val);
            return val;
        }

        /// <summary>
        /// String to decimal
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static double StringToDouble(string input)
        {
            double val = 0;
            if (!string.IsNullOrEmpty(input))
                double.TryParse(input, out val);
            return val;
        }

        /// <summary>　
        /// 字符过滤
        /// </summary>
        /// <param name="str">需要过滤的字符</param>
        /// <param name="options">
        /// 中文　          FilterOptions.HoldChinese
        /// 英文　          FilterOptions.HoldLetter
        /// 数字　          FilterOptions.HoldNumber
        /// 英文数字　      FilterOptions.HoldLetter|FilterOptions.HoldNumber
        /// 英文中文        FilterOptions.HoldChinese | FilterOptions.HoldLetter
        /// 英文中文数字    FilterOptions.HoldChinese | FilterOptions.HoldNumber | FilterOptions.HoldLetter
        /// 英文数字并自动转换全角为半角 FilterOptions.HoldNumber | FilterOptions.HoldLetter | FilterOptions.SBCToDBC
        /// </param>
        /// <returns>过滤后的字符</returns>
        public static string FilterInvalidChar(string str, FilterOptions options)
        {
            if (string.IsNullOrEmpty(str))
            {
                return string.Empty;
            }
            StringBuilder sb = new StringBuilder(str.Length);
            for (int i = 0; i < str.Length; i++)
            {
                int number = str[i];

                //数字
                if (number >= 48 && number <= 57
                    && (FilterOptions.HoldNumber & options) == FilterOptions.HoldNumber)
                {
                    sb.Append(str[i]);
                }
                else if (((number >= 65 && number <= 90) || (number >= 97 && number <= 122))
                    && (FilterOptions.HoldLetter & options) == FilterOptions.HoldLetter
                    ) //字母
                {
                    sb.Append(str[i]);
                }
                else if (number >= 19968 && number <= 40869
                    && (FilterOptions.HoldChinese & options) == FilterOptions.HoldChinese
                    ) //中文字符
                {
                    sb.Append(str[i]);
                }
                else if (((number >= 65296 && number <= 65305)
                    || (number >= 65313 && number <= 65338)
                    || (number >= 65345 && number <= 65370))
                    && (FilterOptions.SBCToDBC & options) == FilterOptions.SBCToDBC)//65248
                {
                    sb.Append((char)(number - 65248));
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// 对关键字进行格式化
        /// </summary>
        /// <param name="str"></param>
        /// <param name="keyword"></param>
        /// <returns></returns>
        public static string FormartKey(string str, string keyword)
        {
            if (!string.IsNullOrEmpty(keyword))
                str = Regex.Replace(str, keyword, "<font color='red'>" + keyword + "</font>", RegexOptions.IgnoreCase);

            return str;
        }

        /// <summary>
        /// 字符过滤选项
        /// </summary>
        [FlagsAttribute]
        public enum FilterOptions
        {
            /// <summary>
            /// 只保留数字
            /// </summary>
            HoldNumber = 1,
            /// <summary>
            /// 指保留字母
            /// </summary>
            HoldLetter = 2,
            /// <summary>
            /// 只保留汉字
            /// </summary>
            HoldChinese = 4,
            /// <summary>
            /// 全角转半角
            /// </summary>
            SBCToDBC = 8
        }

        public static string GetSqlStringArray(string[] args)
        {
            if (args == null)
                return null;
            string result = null;
            foreach (var v in args)
            {
                result += "'" + v + "',";
            }
            return result;
        }

        #region 创建随机字符串
        /// <summary>
        /// 创建随机字符串
        /// </summary>
        /// <returns></returns>
        public static string createNonceStr()
        {
            int length = 16;
            string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            string str = "";
            Random rad = new Random();
            for (int i = 0; i < length; i++)
            {
                str += chars.Substring(rad.Next(0, chars.Length - 1), 1);
            }
            return str;
        }
        #endregion

        public static string ToHexStr(byte[] bytes)
        {
            if (bytes == null)
            {
                return string.Empty;
            }
            StringBuilder stringBuilder = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                stringBuilder.Append(bytes[i].ToString("X2"));
            }
            return stringBuilder.ToString();
        }
    }
}
