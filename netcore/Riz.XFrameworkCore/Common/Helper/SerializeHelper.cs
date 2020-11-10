
using System.IO;
using System.Xml;
using System.Text;
using System.Xml.Serialization;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace Riz.XFramework
{
    /// <summary>
    /// 序列化助手类
    /// </summary>
    public class SerializeHelper
    {
        /// <summary>
        /// 对象序列化成Json字符串
        /// </summary>
        /// <returns></returns>
        public static string SerializeToJson(object obj, string format = null)
        {
#if net40
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(obj.GetType());
#else
            DataContractJsonSerializer serializer = string.IsNullOrEmpty(format)
                ? new DataContractJsonSerializer(obj.GetType())
                : new DataContractJsonSerializer(obj.GetType(), new DataContractJsonSerializerSettings { DateTimeFormat = new DateTimeFormat(format) });
#endif
            using (var ms = new MemoryStream())
            {
                serializer.WriteObject(ms, obj);
                string strJson = Encoding.UTF8.GetString(ms.ToArray());
                return strJson;
            }
        }

        /// <summary>
        /// Json字符串反序列化成对象
        /// </summary>
        public static T DeserializeFromJson<T>(string json, string format = null)
        {
#if net40
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(T));
#else
            DataContractJsonSerializer serializer = string.IsNullOrEmpty(format)
                ? new DataContractJsonSerializer(typeof(T))
                : new DataContractJsonSerializer(typeof(T), new DataContractJsonSerializerSettings { DateTimeFormat = new DateTimeFormat(format) });
#endif
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                T obj = (T)serializer.ReadObject(ms);
                return obj;
            }
        }

        /// <summary>
        /// 对象序列化成 XML 字符串
        /// </summary>
        /// <typeparam name="T">T</typeparam>
        /// <param name="obj">要序列化的对象</param>
        /// <returns></returns>
        public static string SerializeToXml<T>(T obj) where T : class
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            return SerializeToXml(serializer, obj, null);
        }

        /// <summary>
        /// 对象序列化成 XML 字符串
        /// </summary>
        /// <typeparam name="T">T</typeparam>
        /// <param name="obj">要序列化的对象</param>
        /// <param name="ns">命名空间</param>
        /// <returns></returns>
        public static string SerializeToXml<T>(T obj, XmlSerializerNamespaces ns) where T : class
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            return SerializeToXml(serializer, obj, ns);
        }

        /// <summary>
        /// 对象序列化成 XML 字符串
        /// </summary>
        /// <typeparam name="T">T</typeparam>
        /// <param name="obj">要序列化的对象</param>
        /// <param name="root">指定根对象的名称</param>
        /// <returns></returns>
        public static string SerializeToXml<T>(T obj, XmlRootAttribute root) where T : class
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T), root);
            return SerializeToXml(serializer, obj, null);
        }

        /// <summary>
        /// 对象序列化成 XML 字符串
        /// </summary>
        /// <typeparam name="T">T</typeparam>
        /// <param name="obj">要序列化的对象</param>
        /// <param name="root">指定根对象的名称</param>
        /// <param name="defaultNamespace">xml命名空间</param>
        /// <returns></returns>
        public static string SerializeToXml<T>(T obj, XmlRootAttribute root, string defaultNamespace) where T : class
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T), null, new System.Type[] { }, root, defaultNamespace);
            return SerializeToXml(serializer, obj, null);
        }

        /// <summary>
        /// 对象序列化成 XML 字符串
        /// </summary>
        /// <typeparam name="T">T</typeparam>
        /// <param name="serializer">序列化器</param>
        /// <param name="obj">要序列化的对象</param>
        /// <param name="ns">命名空间</param>
        /// <returns></returns>
        public static string SerializeToXml<T>(XmlSerializer serializer, T obj, XmlSerializerNamespaces ns) where T : class
        {
            using (MemoryStream ms = new MemoryStream())
            {
                serializer.Serialize(ms, obj, ns);
                string xml = Encoding.UTF8.GetString(ms.ToArray());
                return xml;
            }
        }

        ///// <summary>
        ///// 对象序列化成 XML 字符串
        ///// </summary>
        ///// <typeparam name="T">T</typeparam>
        ///// <param name="serializer">序列化器</param>
        ///// <param name="obj">要序列化的对象</param>
        ///// <returns></returns>
        //public static string SerializeToXml<T>(XmlSerializer serializer, T obj, XmlSerializerNamespaces ns, string encodingName = "utf-8") where T : class
        //{
        //    using (MemoryStream ms = new MemoryStream())
        //    {
        //        //using (StreamWriter writer = new StreamWriter(ms, Encoding.GetEncoding(encodingName)))
        //        //{
        //        //    serializer.Serialize(writer, obj, ns);
        //        //    string xml = Encoding.UTF8.GetString(ms.ToArray());
        //        //    return xml;
        //        //}

        //        XmlWriterSettings settings = new XmlWriterSettings();
        //        settings.Encoding = Encoding.GetEncoding(encodingName);
        //        settings.OmitXmlDeclaration = false; // 是否生成<?xml version="1.0" encoding="utf-8"?>                    
        //        settings.Indent = true; // 自动格式化，缩进对齐

        //        //XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
        //        //ns.Add(string.Empty, string.Empty); // 去除默认生成的命名空间：xmlns:xsd和xmlns:xsi

        //        using (XmlWriter writer = XmlWriter.Create(ms, settings))
        //        {
        //            serializer.Serialize(writer, obj, ns);
        //            string xml = Encoding.UTF8.GetString(ms.ToArray());
        //            return xml;
        //        }
        //    }
        //}

        /// <summary>
        /// XML 字符串 反序列化成对象
        /// </summary>
        /// <typeparam name="T">T</typeparam>
        /// <param name="xml">xml内容</param>
        /// <returns></returns>
        public static T DeserializeFromXml<T>(string xml) where T : class
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            return DeserializeFromXml<T>(serializer, xml);
        }

        /// <summary>
        /// XML 字符串 反序列化成对象
        /// </summary>
        /// <typeparam name="T">T</typeparam>
        /// <param name="xml">xml内容</param>
        /// <param name="root">指定根对象的名称</param>
        /// <returns></returns>
        public static T DeserializeFromXml<T>(string xml, XmlRootAttribute root) where T : class
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T), root);
            return DeserializeFromXml<T>(serializer, xml);
        }

        /// <summary>
        /// XML 字符串 反序列化成对象
        /// </summary>
        /// <typeparam name="T">T</typeparam>
        /// <param name="xml">xml内容</param>
        /// <param name="root">指定根对象的名称</param>
        /// <param name="defaultNamespace">xml命名空间</param>
        /// <returns></returns>
        public static T DeserializeFromXml<T>(string xml, XmlRootAttribute root, string defaultNamespace) where T : class
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T), null, new System.Type[] { }, root, defaultNamespace);
            return DeserializeFromXml<T>(serializer, xml);
        }

        /// <summary>
        /// XML 字符串 反序列化成对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="serializer">序列化器</param>
        /// <param name="xml">xml内容</param>
        /// <returns></returns>
        public static T DeserializeFromXml<T>(XmlSerializer serializer, string xml) where T : class
        {
            using (Stream xmlStream = new MemoryStream(Encoding.UTF8.GetBytes(xml)))
            {
                using (XmlReader xmlReader = XmlReader.Create(xmlStream))
                {
                    T obj = serializer.Deserialize(xmlReader) as T;
                    return obj;
                }
            }
        }
    }
}
