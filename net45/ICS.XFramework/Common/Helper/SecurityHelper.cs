using System;
using System.IO;
using System.Text;
using System.Security.Cryptography;

namespace ICS.XFramework
{
    public class SecurityHelper
    {
        private const string _rgbKey = "XFRMWORK";
        private const string _pattern = @"^[0-9a-zA-Z@#$%^*]{8,8}";

        /// <summary>
        /// DES解密
        /// </summary>
        /// <param name="str">要解密的字符串</param>
        /// <returns></returns>
        public static string DESDecrypt(string str)
        {
            return DESDecrypt(str, _rgbKey);
        }

        /// <summary>
        /// DES解密
        /// </summary>
        /// <param name="str">要加密的字符串，用 Base64 数字编码的等效字符串表示形式。</param>
        /// <param name="key">密钥，且必须为8位。</param>
        /// <returns></returns>
        public static string DESDecrypt(string str, string key)
        {
            //规定密钥只能是数字与字母的组合，并且长度不得小于８位
            if (!System.Text.RegularExpressions.Regex.IsMatch(key, _pattern)) throw new XFrameworkException("keys must be must be made up of 8 digits and letters");

            using (DESCryptoServiceProvider provider = new DESCryptoServiceProvider())
            {
                byte[] buffer = Convert.FromBase64String(str);
                byte[] rgbKey = Encoding.UTF8.GetBytes(key);
                byte[] rgbIv = Encoding.UTF8.GetBytes(key);

                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, provider.CreateDecryptor(rgbKey, rgbIv), CryptoStreamMode.Write))
                    {
                        cs.Write(buffer, 0, buffer.Length);
                        cs.FlushFinalBlock();
                        return Encoding.UTF8.GetString(ms.ToArray());
                    }
                }
            }

        }

        /// <summary>
        /// DES加密字符串，用 Base64 数字编码的等效字符串表示形式
        /// </summary>
        /// <param name="str">要加密的字符串</param>
        /// <returns></returns>
        public static string DESEncrypt(string str)
        {
            return DESEncrypt(str, _rgbKey);
        }

        /// <summary>
        /// DES加密字符串，用 Base64 数字编码的等效字符串表示形式
        /// </summary>
        /// <param name="str">要加密的字符串。</param>
        /// <param name="key">密钥，且必须为8位。</param>
        /// <returns></returns>
        public static string DESEncrypt(string str, string key)
        {
            //规定密钥只能是数字与字母的组合，并且长度不得小于８位
            if (!System.Text.RegularExpressions.Regex.IsMatch(key, _pattern)) throw new XFrameworkException("keys must be must be made up of 8 digits and letters");

            using (DESCryptoServiceProvider provider = new DESCryptoServiceProvider())
            {
                byte[] buffer = Encoding.UTF8.GetBytes(str);
                byte[] rgbKey = Encoding.UTF8.GetBytes(key);
                byte[] rgbIv = Encoding.UTF8.GetBytes(key);

                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, provider.CreateEncryptor(rgbKey, rgbIv), CryptoStreamMode.Write))
                    {
                        cs.Write(buffer, 0, buffer.Length);
                        cs.FlushFinalBlock();
                        return Convert.ToBase64String(ms.ToArray());
                    }
                }
            }
        }

        ///// <summary>
        ///// 表示md5加密的字符串
        ///// </summary>
        ///// <returns></returns>
        //public static string Md5(string str, bool upper = true, Encoding encoding = null)
        //{
        //    MD5 md5 = new MD5CryptoServiceProvider();
        //    byte[] buffer = md5.ComputeHash((encoding ?? Encoding.UTF8).GetBytes(str));

        //    StringBuilder builder = new StringBuilder();
        //    foreach (var c in buffer) builder.Append(upper ? c.ToString().ToUpper() : c.ToString());

        //    return builder.ToString();
        //}

        /// <summary>
        /// 转为 16 进制表示的 md5加密的字符串
        /// </summary>
        /// <returns></returns>
        public static string ConvertToHexMD5(string str, bool upper = true, Encoding encoding = null)
        {
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] buffer = md5.ComputeHash((encoding ?? Encoding.UTF8).GetBytes(str));

            StringBuilder builder = new StringBuilder();
            foreach (var c in buffer)
            {
                string hex = Convert.ToString(c, 16);
                if (hex.Length == 1) builder.Append("0");
                builder.Append(upper ? hex.ToUpper() : hex);
            }

            return builder.ToString();
        }
    }
}
