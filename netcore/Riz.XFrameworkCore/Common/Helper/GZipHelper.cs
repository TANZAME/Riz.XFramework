using System;
using System.IO;
using System.Text;
using System.IO.Compression;

namespace Riz.XFramework
{
    /// <summary>  
    /// 压缩文本、字节或者文件的压缩辅助类  
    /// </summary>  
    public class GZipHelper
    {
        /// <summary>
        /// 压缩字符串  
        /// </summary>
        /// <param name="source">源字符串</param>
        /// <returns></returns>
        public static string Compress(string source)
        {
            // get a stream  
            MemoryStream ms = null;
            GZipStream zip = null;

            try
            {
                // convert text to bytes  
                byte[] buffer = Encoding.UTF8.GetBytes(source);
                ms = new MemoryStream();
                zip = new GZipStream(ms, CompressionMode.Compress, true);
                // compress the data into our buffer  
                zip.Write(buffer, 0, buffer.Length);
                // reset our position in compressed stream to the start  
                ms.Position = 0;
                // get the compressed data  
                byte[] compressed = ms.ToArray();
                ms.Read(compressed, 0, compressed.Length);
                // prepare final data with header that indicates length  
                byte[] gzBuffer = new byte[compressed.Length + 4];
                //copy compressed data 4 bytes from start of final header  
                System.Buffer.BlockCopy(compressed, 0, gzBuffer, 4, compressed.Length);
                // copy header to first 4 bytes  
                System.Buffer.BlockCopy(BitConverter.GetBytes(buffer.Length), 0, gzBuffer, 0, 4);
                // convert back to string and return  
                return Convert.ToBase64String(gzBuffer);
            }
            finally
            {
                if (ms != null) ms.Dispose();
                if (zip != null) zip.Dispose();
            }
        }

        /// <summary>
        /// 压缩字节  
        /// </summary>
        /// <param name="bytes">无符号字节数组</param>
        /// <returns></returns>
        public static byte[] Compress(byte[] bytes)
        {
            using (var stream = GZip(new MemoryStream(bytes), CompressionMode.Compress))
            {
                return stream.ToArray();
            }
        }

        /// <summary>  
        /// 压缩文件  
        /// </summary>  
        /// <param name="sourceFileName">源文件</param>  
        /// <param name="destFileName">目标文件</param>  
        public static void Compress(string sourceFileName, string destFileName)
        {
            if (!File.Exists(sourceFileName))
                throw new FileNotFoundException(sourceFileName);
            if (File.Exists(destFileName))
            {
                FileAttributes attributes = File.GetAttributes(destFileName);
                if ((attributes & FileAttributes.Normal) != FileAttributes.Normal)
                    File.SetAttributes(destFileName, File.GetAttributes(destFileName) | FileAttributes.Normal);
                File.Delete(destFileName);
            }

            using (var sourceStream = File.OpenRead(sourceFileName))
            {
                //创建写入文件的流
                using (var destStream = File.OpenWrite(destFileName))
                {
                    //创建压缩流
                    using (var zipStream = new GZipStream(destStream, CompressionMode.Compress))
                    {
                        byte[] byts = new byte[1024 * 10];
                        int len = 0;
                        while ((len = sourceStream.Read(byts, 0, byts.Length)) > 0)
                        {
                            zipStream.Write(byts, 0, len);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 解压字符串 
        /// </summary>
        /// <param name="source">源字符串</param>
        /// <returns></returns>
        public static string Decompress(string source)
        {
            // get a stream  
            MemoryStream ms = null;
            GZipStream zip = null;

            try
            {
                // get string as bytes  
                byte[] gzBuffer = Convert.FromBase64String(source);
                // prepare stream to do uncompression  
                ms = new MemoryStream();
                // get the length of compressed data  
                int msgLength = BitConverter.ToInt32(gzBuffer, 0);
                // uncompress everything besides the header  
                ms.Write(gzBuffer, 4, gzBuffer.Length - 4);
                // prepare final buffer for just uncompressed data  
                byte[] buffer = new byte[msgLength];
                // reset our position in stream since we're starting over  
                ms.Position = 0;
                // unzip the data through stream  
                zip = new GZipStream(ms, CompressionMode.Decompress);
                // do the unzip  
                zip.Read(buffer, 0, buffer.Length);
                // convert back to string and return  
                return Encoding.UTF8.GetString(buffer);
            }
            finally
            {
                if (ms != null) ms.Dispose();
                if (zip != null) zip.Dispose();
            }
        }

        /// <summary>
        /// 解压字节 
        /// </summary>
        /// <param name="bytes">无符号字节数组</param>
        /// <returns></returns>
        public static byte[] Decompress(byte[] bytes)
        {
            using (var stream = GZip(new MemoryStream(bytes), CompressionMode.Decompress))
            {
                return stream.ToArray();
            }
        }

        /// <summary>  
        /// 解压文件  
        /// </summary>  
        /// <param name="sourceFileName">源文件</param>  
        /// <param name="destFileName">目标文件</param>  
        public static void Decompress(string sourceFileName, string destFileName)
        {
            if (!File.Exists(sourceFileName))
                throw new FileNotFoundException(sourceFileName);
            if (File.Exists(destFileName))
            {
                FileAttributes attributes = File.GetAttributes(destFileName);
                if ((attributes & FileAttributes.Normal) != FileAttributes.Normal)
                    File.SetAttributes(destFileName, File.GetAttributes(destFileName) | FileAttributes.Normal);
                File.Delete(destFileName);
            }

            //读取压缩文件
            using (FileStream sourceStream = File.OpenRead(sourceFileName))
            {
                //创建压缩流
                using (GZipStream zipStream = new GZipStream(sourceStream, CompressionMode.Decompress))
                {
                    using (FileStream destStream = File.OpenWrite(destFileName))
                    {
                        byte[] byts = new byte[1024 * 10];
                        int len = 0;
                        while ((len = zipStream.Read(byts, 0, byts.Length)) > 0)
                        {
                            destStream.Write(byts, 0, len);
                        }
                    }

                }
            }
        }

        /// <summary>
        /// 压缩/解压流
        /// </summary>
        /// <param name="stream">要压缩或解压缩的流</param>
        /// <param name="mode">表示要采取的操作</param>
        /// <returns></returns>
        public static MemoryStream GZip(Stream stream, CompressionMode mode)
        {
            byte[] buffer = new byte[4096];
            var ms = new MemoryStream();
            using (Stream zip = new GZipStream(stream, mode))
            {
                while (true)
                {
                    Array.Clear(buffer, 0, buffer.Length);
                    int size = zip.Read(buffer, 0, buffer.Length);
                    if (size > 0)
                        ms.Write(buffer, 0, size);
                    else
                        break;
                }
                return ms;
            }
        }
    }
}
