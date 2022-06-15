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
        // ICSharpCode.SharpZipLib.dll
        ///// <summary>
        ///// 读取 zip 文件中的其中一个文件
        ///// </summary>
        ///// <param name="zipFileName">zip 文件完全路径</param>
        ///// <param name="entryName">zip 里的文件,a/b/c.json</param>
        //public static T ReadZipFile<T>(string zipFileName, string entryName, Func<Stream, T> method)
        //{
        //    using (MemoryStream ms = (MemoryStream)ReadZipFile(zipFileName, entryName))
        //    {
        //        return method(ms);
        //    }
        //}

        ///// <summary>
        ///// 读取 zip 文件中的其中一个文件
        ///// </summary>
        ///// <param name="zipFileName">zip 文件完全路径</param>
        ///// <param name="entryName">zip 里的文件,a/b/c.json</param>
        //public static Stream ReadZipFile(string zipFileName, string entryName)
        //{
        //    // https://github.com/icsharpcode/SharpZipLib/wiki/Unpack-a-zip-using-ZipInputStream
        //    using (var fs = File.OpenRead(zipFileName))
        //    using (var zip = new ZipInputStream(fs))
        //    {
        //        ZipEntry entry;
        //        byte[] data = new byte[2048];
        //        MemoryStream ms = new MemoryStream();

        //        while ((entry = zip.GetNextEntry()) != null)
        //        {
        //            if (!entry.IsFile) continue;
        //            if (string.Compare(entry.Name, entryName, true) == 0)
        //            {
        //                StreamUtils.Copy(zip, ms, data);
        //                break;
        //            }
        //        }

        //        return ms;
        //    }
        //}

        ///// <summary>
        ///// 将多个文件打包
        ///// </summary>
        ///// <param name="zipFullName">目标文件（.zip）路径</param>
        ///// <param name="sources">多个源文件路径</param>
        //public static void CreateZipFile(string zipFullName, params string[] sources)
        //{
        //    if (sources == null || sources.Length == 0) return;
        //    using (var fs = File.Create(zipFullName))
        //    using (ZipOutputStream zip = new ZipOutputStream(fs))
        //    {
        //        //var crc = new Crc32();
        //        zip.SetLevel(3);
        //        foreach (string s in sources)
        //        {
        //            CreateZipFileImpl(s, zip, sources.Length > 1 && Directory.Exists(s) ? s.Substring(s.LastIndexOf("\\") + 1) + '\\' : string.Empty);
        //        }
        //    }
        //}

        ///// <summary>
        ///// 压缩文件夹
        ///// </summary>
        ///// <param name="source">待压缩的文件或文件夹路径</param>
        ///// <param name="zipFileName">打包结果的zip文件路径（类似 D:\WorkSpace\a.zip）,全路径包括文件名和.zip扩展名</param>
        ///// <param name="segBytes">分卷大小（字节）</param>
        //public static int CreateZipFile(string source, string zipFileName, int segBytes)
        //{
        //    // https://bbs.csdn.net/topics/390791210
        //    // 分卷压缩

        //    var num = 0;
        //    long length = 0;
        //    using (var fs = File.Create(zipFileName))
        //    {
        //        using (ZipOutputStream zip = new ZipOutputStream(fs))
        //        {
        //            //var crc = new Crc32();
        //            //0-9, 9 being the highest level of compression
        //            zip.SetLevel(3);
        //            CreateZipFileImpl(source, zip, string.Empty);

        //            length = fs.Length;
        //        }
        //    }

        //    // 分段
        //    if (segBytes > 0 && length > segBytes)
        //    {
        //        using (FileStream sourceStream = File.OpenRead(zipFileName))
        //        {
        //            byte[] buffer = new byte[4096];
        //            int readSize;
        //            do
        //            {
        //                num++;
        //                using (var fs = File.Create(string.Format("{0}.{1}", zipFileName, num.ToString().PadLeft(3, '0'))))
        //                {
        //                    do
        //                    {
        //                        readSize = sourceStream.Read(buffer, 0, buffer.Length);
        //                        fs.Write(buffer, 0, readSize);
        //                    } while (fs.Length + buffer.Length <= segBytes && readSize > 0);
        //                }
        //            } while (readSize > 0);
        //        }
        //    }

        //    return num == 0 ? 1 : num;
        //}

        //// 递归压缩文件
        //static void CreateZipFileImpl(string source, ZipOutputStream zip, string folder)
        //{
        //    string[] entryFullNames = File.Exists(source)
        //        ? new[] { source }
        //        : Directory.GetFileSystemEntries(source);

        //    if (entryFullNames.Length == 0 && !string.IsNullOrEmpty(folder))
        //    {
        //        //  空文件夹
        //        ZipEntry newEntry = new ZipEntry(folder);
        //        newEntry.DateTime = DateTime.Now;
        //        byte[] buffer = new byte[0];

        //        //crc.Reset();
        //        //crc.Update(buffer);

        //        //entry.Crc = crc.Value;
        //        //zip.UseZip64 = UseZip64.Off;
        //        zip.PutNextEntry(newEntry);

        //        //zip.Write(buffer, 0, buffer.Length);
        //        zip.CloseEntry();
        //    }
        //    else
        //    {
        //        foreach (string fullName in entryFullNames)
        //        {
        //            if (Directory.Exists(fullName))
        //            {
        //                //如果当前是文件夹，递归
        //                string name = folder;
        //                name += fullName.Substring(fullName.LastIndexOf("\\") + 1);
        //                name += "\\";
        //                CreateZipFileImpl(fullName, zip, name);
        //            }
        //            else
        //            {
        //                var fi = new FileInfo(fullName);
        //                string entryName = folder + fullName.Substring(fullName.LastIndexOf("\\") + 1);
        //                // Remove drive from name and fix slash direction
        //                entryName = ZipEntry.CleanName(entryName);
        //                var newEntry = new ZipEntry(entryName);
        //                // Note the zip format stores 2 second granularity
        //                newEntry.DateTime = fi.LastWriteTime;
        //                // To permit the zip to be unpacked by built-in extractor in WinXP and Server2003,
        //                // WinZip 8, Java, and other older code, you need to do one of the following: 
        //                // Specify UseZip64.Off, or set the Size.
        //                // If the file may be bigger than 4GB, or you do not need WinXP built-in compatibility, 
        //                // you do not need either, but the zip will be in Zip64 format which
        //                // not all utilities can understand.
        //                //   zipStream.UseZip64 = UseZip64.Off;
        //                newEntry.Size = fi.Length;
        //                zip.PutNextEntry(newEntry);
        //                // Zip the file in buffered chunks
        //                // the "using" will close the stream even if an exception occurs
        //                var buffer = new byte[4096];
        //                using (FileStream fs = File.OpenRead(fullName))
        //                {
        //                    StreamUtils.Copy(fs, zip, buffer);
        //                }
        //                zip.CloseEntry();

        //                ////如果是文件，开始压缩
        //                //using (FileStream fs = File.OpenRead(fullName))
        //                //{
        //                //    var fi = new FileInfo(fullName);
        //                //    byte[] buffer = new byte[fs.Length];
        //                //    fs.Read(buffer, 0, buffer.Length);

        //                //    string entryName = folder + fullName.Substring(fullName.LastIndexOf("\\") + 1);
        //                //    // Remove drive from name and fix slash direction
        //                //    entryName = ZipEntry.CleanName(entryName); 
        //                //    ZipEntry newEntry = new ZipEntry(entryName);

        //                //    newEntry.DateTime = DateTime.Now;
        //                //    newEntry.Size = fs.Length;

        //                //    fs.Close();

        //                //    crc.Reset();
        //                //    crc.Update(buffer);

        //                //    newEntry.Crc = crc.Value;
        //                //    zip.PutNextEntry(newEntry);

        //                //    zip.Write(buffer, 0, buffer.Length);
        //                //}
        //            }
        //        }
        //    }
        //}
    }
}
