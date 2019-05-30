﻿using System;
using System.Text;

namespace ICS.XFramework
{
    /// <summary>
    /// 类型转换帮助类
    /// </summary>
    public class ConvertHelper
    {
        #region 进制转换

        /// <summary>
        /// 实现各进制数间的转换。ConvertBase("15",10,16)表示将十进制数15转换为16进制的数。
        /// </summary>
        /// <param name="value">要转换的值,即原值</param>
        /// <param name="from">原值的进制,只能是2,8,10,16四个值。</param>
        /// <param name="to">要转换到的目标进制，只能是2,8,10,16四个值。</param>
        /// <returns>返回值</returns>
        public static string ConvertBase(string value, int from, int to)
        {
            if (!IsBaseNumber(from))
                throw new ArgumentException("参数from只能是2,8,10,16四个值。");

            if (!IsBaseNumber(to))
                throw new ArgumentException("参数to只能是2,8,10,16四个值。");

            int intValue = Convert.ToInt32(value, from); //先转成10进制
            string result = Convert.ToString(intValue, to); //再转成目标进制
            if (to == 2)
            {
                int resultLength = result.Length; //获取二进制的长度
                switch (resultLength)
                {
                    case 7:
                        result = "0" + result;
                        break;
                    case 6:
                        result = "00" + result;
                        break;
                    case 5:
                        result = "000" + result;
                        break;
                    case 4:
                        result = "0000" + result;
                        break;
                    case 3:
                        result = "00000" + result;
                        break;
                }
            }
            return result;
        }

        /// <summary>
        /// 判断是否是  2 8 10 16
        /// </summary>
        /// <param name="baseNumber"></param>
        /// <returns>返回值</returns>
        private static bool IsBaseNumber(int baseNumber)
        {
            if (baseNumber == 2 || baseNumber == 8 || baseNumber == 10 || baseNumber == 16)
                return true;
            return false;
        }

        #endregion

        #region 使用指定字符集将string转换成byte[]

        /// <summary>
        /// 将string转换成byte[]
        /// </summary>
        /// <param name="text">要转换的字符串</param>
        /// <returns>返回值</returns>
        public static byte[] StringToBytes(string text)
        {
            return Encoding.Default.GetBytes(text);
        }

        /// <summary>
        /// 使用指定字符集将string转换成byte[]
        /// </summary>
        /// <param name="text">要转换的字符串</param>
        /// <param name="encoding">字符编码</param>
        /// <returns>返回值</returns>
        public static byte[] StringToBytes(string text, Encoding encoding)
        {
            return encoding.GetBytes(text);
        }

        #endregion

        #region 使用指定字符集将byte[]转换成string

        /// <summary>
        /// 将byte[]转换成string
        /// </summary>
        /// <param name="bytes">要转换的字节数组</param>
        /// <returns>返回值</returns>
        public static string BytesToString(byte[] bytes)
        {
            return Encoding.Default.GetString(bytes);
        }

        /// <summary>
        /// 使用指定字符集将byte[]转换成string
        /// </summary>
        /// <param name="bytes">要转换的字节数组</param>
        /// <param name="encoding">字符编码</param>
        /// <returns>返回值</returns>
        public static string BytesToString(byte[] bytes, Encoding encoding)
        {
            return encoding.GetString(bytes);
        }

        #endregion

        #region 将byte[]转换成int

        /// <summary>
        /// 将byte[]转换成int
        /// </summary>
        /// <param name="data">需要转换成整数的byte数组</param>
        /// <returns>返回值</returns>
        public static int BytesToInt32(byte[] data)
        {
            //如果传入的字节数组长度小于4,则返回0
            if (data.Length < 4)
            {
                return 0;
            }

            //定义要返回的整数
            int num = 0;

            //如果传入的字节数组长度大于4,需要进行处理
            if (data.Length >= 4)
            {
                //创建一个临时缓冲区
                var tempBuffer = new byte[4];

                //将传入的字节数组的前4个字节复制到临时缓冲区
                Buffer.BlockCopy(data, 0, tempBuffer, 0, 4);

                //将临时缓冲区的值转换成整数，并赋给num
                num = BitConverter.ToInt32(tempBuffer, 0);
            }

            //返回整数
            return num;
        }

        #endregion

        #region 将数据转换为整型

        /// <summary>
        /// 将数据转换为整型   转换失败返回默认值
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="data">数据</param>
        /// <param name="defValue">默认值</param>
        /// <returns>返回值</returns>
        public static int ToInt32<T>(T data, int defValue) where T : class
        {
            //如果为空则返回默认值
            if (data == null || Convert.IsDBNull(data))
            {
                return defValue;
            }

            try
            {
                return Convert.ToInt32(data);
            }
            catch
            {
                return defValue;
            }
        }

        /// <summary>
        /// 将数据转换为整型   转换失败返回默认值
        /// </summary>
        /// <param name="data">数据</param>
        /// <param name="defValue">默认值</param>
        /// <returns>返回值</returns>
        public static int ToInt32(string data, int defValue)
        {
            //如果为空则返回默认值
            if (string.IsNullOrEmpty(data))
            {
                return defValue;
            }

            int temp;
            if (Int32.TryParse(data, out temp))
            {
                return temp;
            }
            return defValue;
        }

        /// <summary>
        /// 将数据转换为整型  转换失败返回默认值
        /// </summary>
        /// <param name="data">数据</param>
        /// <param name="defValue">默认值</param>
        /// <returns>返回值</returns>
        public static int ToInt32(object data, int defValue)
        {
            //如果为空则返回默认值
            if (data == null || Convert.IsDBNull(data))
            {
                return defValue;
            }

            try
            {
                return Convert.ToInt32(data);
            }
            catch
            {
                return defValue;
            }
        }

        #endregion

        #region 将数据转换为布尔型

        /// <summary>
        /// 将数据转换为布尔类型  转换失败返回默认值
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="data">数据</param>
        /// <param name="defValue">默认值</param>
        /// <returns>返回值</returns>
        public static bool ToBoolean<T>(T data, bool defValue) where T : class
        {
            //如果为空则返回默认值
            if (data == null || Convert.IsDBNull(data))
            {
                return defValue;
            }

            try
            {
                return Convert.ToBoolean(data);
            }
            catch
            {
                return defValue;
            }
        }

        /// <summary>
        /// 将数据转换为布尔类型  转换失败返回 默认值
        /// </summary>
        /// <param name="data">数据</param>
        /// <param name="defValue">默认值</param>
        /// <returns>返回值</returns>
        public static bool ToBoolean(string data, bool defValue)
        {
            //如果为空则返回默认值
            if (string.IsNullOrEmpty(data))
            {
                return defValue;
            }

            bool temp;
            if (bool.TryParse(data, out temp))
            {
                return temp;
            }
            return defValue;
        }

        /// <summary>
        /// 将数据转换为布尔类型  转换失败返回 默认值
        /// </summary>
        /// <param name="data">数据</param>
        /// <param name="defValue">默认值</param>
        /// <returns>返回值</returns>
        public static bool ToBoolean(object data, bool defValue)
        {
            //如果为空则返回默认值
            if (data == null || Convert.IsDBNull(data))
            {
                return defValue;
            }

            try
            {
                return Convert.ToBoolean(data);
            }
            catch
            {
                return defValue;
            }
        }

        #endregion

        #region 将数据转换为单精度浮点型

        /// <summary>
        /// 将数据转换为单精度浮点型  转换失败 返回默认值
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="data">数据</param>
        /// <param name="defValue">默认值</param>
        /// <returns>返回值</returns>
        public static float ToFloat<T>(T data, float defValue) where T : class
        {
            //如果为空则返回默认值
            if (data == null || Convert.IsDBNull(data))
            {
                return defValue;
            }

            try
            {
                return Convert.ToSingle(data);
            }
            catch
            {
                return defValue;
            }
        }

        /// <summary>
        /// 将数据转换为单精度浮点型   转换失败返回默认值
        /// </summary>
        /// <param name="data">数据</param>
        /// <param name="defValue">默认值</param>
        /// <returns>返回值</returns>
        public static float ToFloat(object data, float defValue)
        {
            //如果为空则返回默认值
            if (data == null || Convert.IsDBNull(data))
            {
                return defValue;
            }

            try
            {
                return Convert.ToSingle(data);
            }
            catch
            {
                return defValue;
            }
        }

        /// <summary>
        /// 将数据转换为单精度浮点型   转换失败返回默认值
        /// </summary>
        /// <param name="data">数据</param>
        /// <param name="defValue">默认值</param>
        /// <returns>返回值</returns>
        public static float ToFloat(string data, float defValue)
        {
            //如果为空则返回默认值
            if (string.IsNullOrEmpty(data))
            {
                return defValue;
            }

            float temp;

            if (float.TryParse(data, out temp))
            {
                return temp;
            }
            return defValue;
        }

        #endregion

        #region 将数据转换为双精度浮点型

        /// <summary>
        /// 将数据转换为双精度浮点型   转换失败返回默认值
        /// </summary>
        /// <typeparam name="T">数据的类型</typeparam>
        /// <param name="data">要转换的数据</param>
        /// <param name="defValue">默认值</param>
        /// <returns>返回值</returns>
        public static double ToDouble<T>(T data, double defValue) where T : class
        {
            //如果为空则返回默认值
            if (data == null || Convert.IsDBNull(data))
            {
                return defValue;
            }

            try
            {
                return Convert.ToDouble(data);
            }
            catch
            {
                return defValue;
            }
        }

        /// <summary>
        /// 将数据转换为双精度浮点型,并设置小数位   转换失败返回默认值
        /// </summary>
        /// <typeparam name="T">数据的类型</typeparam>
        /// <param name="data">要转换的数据</param>
        /// <param name="decimals">小数的位数</param>
        /// <param name="defValue">默认值</param>
        /// <returns>返回值</returns>
        public static double ToDouble<T>(T data, int decimals, double defValue) where T : class
        {
            //如果为空则返回默认值
            if (data == null || Convert.IsDBNull(data))
            {
                return defValue;
            }

            try
            {
                return Math.Round(Convert.ToDouble(data), decimals);
            }
            catch
            {
                return defValue;
            }
        }


        /// <summary>
        /// 将数据转换为双精度浮点型  转换失败返回默认值
        /// </summary>
        /// <param name="data">要转换的数据</param>
        /// <param name="defValue">默认值</param>
        /// <returns>返回值</returns>
        public static double ToDouble(object data, double defValue)
        {
            //如果为空则返回默认值
            if (data == null || Convert.IsDBNull(data))
            {
                return defValue;
            }

            try
            {
                return Convert.ToDouble(data);
            }
            catch
            {
                return defValue;
            }
        }

        /// <summary>
        /// 将数据转换为双精度浮点型  转换失败返回默认值
        /// </summary>
        /// <param name="data">要转换的数据</param>
        /// <param name="defValue">默认值</param>
        /// <returns>返回值</returns>
        public static double ToDouble(string data, double defValue)
        {
            //如果为空则返回默认值
            if (string.IsNullOrEmpty(data))
            {
                return defValue;
            }

            double temp;

            if (double.TryParse(data, out temp))
            {
                return temp;
            }
            return defValue;
        }


        /// <summary>
        /// 将数据转换为双精度浮点型,并设置小数位  转换失败返回默认值
        /// </summary>
        /// <param name="data">要转换的数据</param>
        /// <param name="decimals">小数的位数</param>
        /// <param name="defValue">默认值</param>
        /// <returns>返回值</returns>
        public static double ToDouble(object data, int decimals, double defValue)
        {
            //如果为空则返回默认值
            if (data == null || Convert.IsDBNull(data))
            {
                return defValue;
            }

            try
            {
                return Math.Round(Convert.ToDouble(data), decimals);
            }
            catch
            {
                return defValue;
            }
        }

        /// <summary>
        /// 将数据转换为双精度浮点型,并设置小数位  转换失败返回默认值
        /// </summary>
        /// <param name="data">要转换的数据</param>
        /// <param name="decimals">小数的位数</param>
        /// <param name="defValue">默认值</param>
        /// <returns>返回值</returns>
        public static double ToDouble(string data, int decimals, double defValue)
        {
            //如果为空则返回默认值
            if (string.IsNullOrEmpty(data))
            {
                return defValue;
            }

            double temp;

            if (double.TryParse(data, out temp))
            {
                return Math.Round(temp, decimals);
            }
            return defValue;
        }

        #endregion

        #region 将数据转换为指定类型

        /// <summary>
        /// 将数据转换为指定类型
        /// </summary>
        /// <param name="data">转换的数据</param>
        /// <param name="targetType">转换的目标类型</param>
        /// <returns>返回值</returns>
        public static object ConvertTo(object data, Type targetType)
        {
            if (data == null || Convert.IsDBNull(data))
            {
                return null;
            }

            Type type2 = data.GetType();
            if (targetType == type2)
            {
                return data;
            }
            if (((targetType == typeof(Guid)) || (targetType == typeof(Guid?))) && (type2 == typeof(string)))
            {
                if (string.IsNullOrEmpty(data.ToString()))
                {
                    return null;
                }
                return new Guid(data.ToString());
            }

            if (targetType.IsEnum)
            {
                try
                {
                    return Enum.Parse(targetType, data.ToString(), true);
                }
                catch
                {
                    return Enum.ToObject(targetType, data);
                }
            }

            if (targetType.IsGenericType)
            {
                targetType = targetType.GetGenericArguments()[0];
            }

            return Convert.ChangeType(data, targetType);
        }

        /// <summary>
        /// 将数据转换为指定类型
        /// </summary>
        /// <typeparam name="T">转换的目标类型</typeparam>
        /// <param name="data">转换的数据</param>
        /// <returns>返回值</returns>
        public static T ConvertTo<T>(object data)
        {
            if (data == null || Convert.IsDBNull(data))
                return default(T);

            object obj = ConvertTo(data, typeof(T));
            if (obj == null)
            {
                return default(T);
            }
            return (T)obj;
        }

        #endregion

        #region 将数据转换Decimal

        /// <summary>
        /// 将数据转换为Decimal  转换失败返回默认值
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="data">数据</param>
        /// <param name="defValue">默认值</param>
        /// <returns>返回值</returns>
        public static Decimal ToDecimal<T>(T data, Decimal defValue) where T : class
        {
            //如果为空则返回默认值
            if (data == null || Convert.IsDBNull(data))
            {
                return defValue;
            }

            try
            {
                return Convert.ToDecimal(data);
            }
            catch
            {
                return defValue;
            }
        }

        /// <summary>
        /// 将数据转换为Decimal  转换失败返回 默认值
        /// </summary>
        /// <param name="data">数据</param>
        /// <param name="defValue">默认值</param>
        /// <returns>返回值</returns>
        public static Decimal ToDecimal(object data, Decimal defValue)
        {
            //如果为空则返回默认值
            if (data == null || Convert.IsDBNull(data))
            {
                return defValue;
            }

            try
            {
                return Convert.ToDecimal(data);
            }
            catch
            {
                return defValue;
            }
        }

        /// <summary>
        /// 将数据转换为Decimal  转换失败返回 默认值
        /// </summary>
        /// <param name="data">数据</param>
        /// <param name="defValue">默认值</param>
        /// <returns>返回值</returns>
        public static Decimal ToDecimal(string data, Decimal defValue)
        {
            //如果为空则返回默认值
            if (string.IsNullOrEmpty(data))
            {
                return defValue;
            }

            decimal temp;

            if (decimal.TryParse(data, out temp))
            {
                return temp;
            }
            return defValue;
        }

        #endregion

        #region 将数据转换为DateTime

        /// <summary>
        /// 将数据转换为DateTime  转换失败返回默认值
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="data">数据</param>
        /// <param name="defValue">默认值</param>
        /// <returns>返回值</returns>
        public static DateTime ToDateTime<T>(T data, DateTime defValue) where T : class
        {
            //如果为空则返回默认值
            if (data == null || Convert.IsDBNull(data))
            {
                return defValue;
            }

            try
            {
                return Convert.ToDateTime(data);
            }
            catch
            {
                return defValue;
            }
        }


        /// <summary>
        /// 将数据转换为DateTime  转换失败返回 默认值
        /// </summary>
        /// <param name="data">数据</param>
        /// <param name="defValue">默认值</param>
        /// <returns>返回值</returns>
        public static DateTime ToDateTime(object data, DateTime defValue)
        {
            //如果为空则返回默认值
            if (data == null || Convert.IsDBNull(data))
            {
                return defValue;
            }

            try
            {
                return Convert.ToDateTime(data);
            }
            catch
            {
                return defValue;
            }
        }

        /// <summary>
        /// 将数据转换为DateTime  转换失败返回 默认值
        /// </summary>
        /// <param name="data">数据</param>
        /// <param name="defValue">默认值</param>
        /// <returns>返回值</returns>
        public static DateTime ToDateTime(string data, DateTime defValue)
        {
            //如果为空则返回默认值
            if (string.IsNullOrEmpty(data))
            {
                return defValue;
            }

            DateTime temp;

            if (DateTime.TryParse(data, out temp))
            {
                return temp;
            }
            return defValue;
        }

        #endregion

        #region 半角全角转换

        /// <summary>
        /// 转全角的函数(SBC case)
        /// </summary>
        /// <param name="input">任意字符串</param>
        /// <returns>全角字符串</returns>
        /// <remarks>
        ///全角空格为12288，半角空格为32
        ///其他字符半角(33-126)与全角(65281-65374)的对应关系是：均相差65248
        /// </remarks>
        public static string ConvertToSBC(string input)
        {
            //半角转全角：
            char[] c = input.ToCharArray();
            for (int i = 0; i < c.Length; i++)
            {
                if (c[i] == 32)
                {
                    c[i] = (char)12288;
                    continue;
                }
                if (c[i] < 127)
                {
                    c[i] = (char)(c[i] + 65248);
                }
            }
            return new string(c);
        }


        /// <summary> 转半角的函数(DBC case) </summary>
        /// <param name="input">任意字符串</param>
        /// <returns>半角字符串</returns>
        /// <remarks>
        ///全角空格为12288，半角空格为32
        ///其他字符半角(33-126)与全角(65281-65374)的对应关系是：均相差65248
        /// </remarks>
        public static string ConvertToDBC(string input)
        {
            char[] c = input.ToCharArray();
            for (int i = 0; i < c.Length; i++)
            {
                if (c[i] == 12288)
                {
                    c[i] = (char)32;
                    continue;
                }
                if (c[i] > 65280 && c[i] < 65375)
                    c[i] = (char)(c[i] - 65248);
            }
            return new string(c);
        }

        #endregion
    }
}