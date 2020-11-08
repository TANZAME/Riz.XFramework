using System;

namespace Riz.XFramework
{
    /// <summary>
    /// XFramework 表示在应用程序执行期间发生的错误
    /// </summary>
    [Serializable]
    public class XFrameworkException : Exception
    {
        /// <summary>
        /// 初始化 <see cref="XFrameworkException"/> 类的新实例。
        /// </summary>
        public XFrameworkException()
        {
        }

        /// <summary>
        /// 使用指定的错误消息初始化 <see cref="XFrameworkException"/> 类的新实例。
        /// </summary>
        /// <param name="message">描述错误的消息</param>
        public XFrameworkException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// 使用指定错误消息和对作为此异常原因的内部异常的引用来初始化 <see cref="XFrameworkException"/> 类的新实例。
        /// </summary>
        /// <param name="message">解释异常原因的错误消息</param>
        /// <param name="innerException">导致当前异常的异常；如果未指定内部异常，则是一个 null 引用</param>
        public XFrameworkException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// 使用指定的错误消息初始化 <see cref="XFrameworkException"/> 类的新实例。
        /// </summary>
        /// <param name="message">描述错误的消息</param>
        /// <param name="args">一个对象数组，其中包含零个或多个要设置格式的对象</param>
        public XFrameworkException(string message, params object[] args)
            : base(string.Format(message, args))
        {
        }

        /// <summary>
        /// 使用指定错误消息和对作为此异常原因的内部异常的引用来初始化 <see cref="XFrameworkException"/> 类的新实例。
        /// </summary>
        /// <param name="message">解释异常原因的错误消息</param>
        /// <param name="innerException">导致当前异常的异常；如果未指定内部异常，则是一个 null 引用</param>
        /// <param name="args">一个对象数组，其中包含零个或多个要设置格式的对象</param>
        public XFrameworkException(string message, Exception innerException, params object[] args)
            : base(string.Format(message, args), innerException)
        {
        }

        /// <summary>
        /// 抛出一个异常
        /// </summary>
        /// <param name="message">描述错误的消息</param>
        public static void Throw(string message)
        {
            throw new XFrameworkException(message);
        }

        /// <summary>
        /// 抛出一个异常
        /// </summary>
        /// <param name="message">描述错误的消息</param>
        /// <param name="args">一个对象数组，其中包含零个或多个要设置格式的对象</param>
        public static void Throw(string message, params object[] args)
        {
            throw new XFrameworkException(message, args);
        }

        /// <summary>
        /// 参数检查类
        /// </summary>
        public class Check
        {
            /// <summary>
            /// 检查参数是否为空
            /// </summary>
            public static T NotNull<T>(T value, string parameterName) where T : class
            {
                if (value == null)
                {
                    throw new ArgumentNullException(parameterName);
                }

                return value;
            }

            /// <summary>
            /// 检查参数是否为空
            /// </summary>
            public static T? NotNull<T>(T? value, string parameterName) where T : struct
            {
                if (value == null)
                {
                    throw new ArgumentNullException(parameterName);
                }

                return value;
            }

            /// <summary>
            /// 检查参数是否为空
            /// </summary>
            public static string NotNull(string value, string parameterName)
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new ArgumentException(parameterName);
                }

                return value;
            }
        }
    }
}
