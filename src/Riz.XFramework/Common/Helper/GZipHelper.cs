using System;
using System.IO;
using System.Text;
using System.IO.Compression;
using System.Security.Cryptography;

namespace Riz.XFramework
{
    /// <summary>  
    /// 压缩文本、字节或者文件的压缩辅助类  
    /// </summary>  
    public class GZipHelper
    {
        /// <summary>
        /// 压缩字符串，压缩后的字节数组转为 base64
        /// </summary>
        /// <param name="source">源字符串</param>
        /// <returns></returns>
        public static string Compress(string source)
            => Convert.ToBase64String(GZipHelper.Compress(Encoding.UTF8.GetBytes(source)));

        /// <summary>
        /// 压缩字节  
        /// </summary>
        /// <param name="bytes">无符号字节数组</param>
        /// <returns></returns>
        public static byte[] Compress(byte[] bytes)
        {
            using (var ms = new MemoryStream())
            {
                using (var gz = new GZipStream(ms, CompressionMode.Compress, true))
                {
                    gz.Write(bytes, 0, bytes.Length);
                }

                // gz 流关闭之后，再能正常返回压缩后的字符串
                return ms.ToArray();
            }
        }

        /// <summary>
        /// 解压字符串，它是经过 gzip 压缩的字节数组转为 base64的字符串
        /// </summary>
        /// <param name="source">源字符串</param>
        /// <returns></returns>
        public static string Decompress(string source)
            => Encoding.UTF8.GetString(GZipHelper.Decompress(Convert.FromBase64String(source)));

        /// <summary>
        /// 解压字节 
        /// </summary>
        /// <param name="bytes">压缩过的字节数组</param>
        /// <returns></returns>
        public static byte[] Decompress(byte[] bytes)
        {
            using (var ms = new MemoryStream())
            {
                ms.Write(bytes, 0, bytes.Length);
                ms.Seek(0L, SeekOrigin.Begin);

                byte[] array2 = null;
                array2 = new byte[5];
                int num = (int)ms.Length - 4;
                ms.Position = num;
                ms.Read(array2, 0, 4);

                int num2 = BitConverter.ToInt32(array2, 0);
                ms.Seek(0L, SeekOrigin.Begin);

                using (var gz = new GZipStream(ms, CompressionMode.Decompress, false))
                {
                    byte[] array3 = new byte[num2];
                    gz.Read(array3, 0, num2);
                    return array3;
                }
            }            
        }

        /// <summary>  
        /// 压缩文件  
        /// </summary>  
        /// <param name="srcFillFullName">源文件</param>  
        /// <param name="destFileFullName">目标文件</param>  
        public static void Compress(string srcFillFullName, string destFileFullName)
        {
            if (!File.Exists(srcFillFullName))
                throw new FileNotFoundException(srcFillFullName);
            if (File.Exists(destFileFullName))
            {
                FileAttributes attributes = File.GetAttributes(destFileFullName);
                if ((attributes & FileAttributes.Normal) != FileAttributes.Normal)
                    File.SetAttributes(destFileFullName, File.GetAttributes(destFileFullName) | FileAttributes.Normal);
                File.Delete(destFileFullName);
            }

            using (var src = File.OpenRead(srcFillFullName))
            using (var dest = File.OpenWrite(destFileFullName))
            using (var gz = new GZipStream(dest, CompressionMode.Compress))
            {
                byte[] byts = new byte[1024 * 10];
                int len = 0;
                while ((len = src.Read(byts, 0, byts.Length)) > 0)
                {
                    gz.Write(byts, 0, len);
                }
            }
        }

        /// <summary>  
        /// 解压文件  
        /// </summary>  
        /// <param name="srcFillFullName">源文件</param>  
        /// <param name="destFileFullName">目标文件</param>  
        public static void Decompress(string srcFillFullName, string destFileFullName)
        {
            if (!File.Exists(srcFillFullName))
                throw new FileNotFoundException(srcFillFullName);
            if (File.Exists(destFileFullName))
            {
                FileAttributes attributes = File.GetAttributes(destFileFullName);
                if ((attributes & FileAttributes.Normal) != FileAttributes.Normal)
                    File.SetAttributes(destFileFullName, File.GetAttributes(destFileFullName) | FileAttributes.Normal);
                File.Delete(destFileFullName);
            }

            //读取压缩文件
            using (FileStream src = File.OpenRead(srcFillFullName))
            using (GZipStream gz = new GZipStream(src, CompressionMode.Decompress))
            using (FileStream dest = File.OpenWrite(destFileFullName))
            {
                byte[] byts = new byte[1024 * 10];
                int len = 0;
                while ((len = gz.Read(byts, 0, byts.Length)) > 0)
                {
                    dest.Write(byts, 0, len);
                }
            }
        }

        // 压缩/解压整个目录# https://blog.csdn.net/weixin_34167043/article/details/94505403
    }
}
