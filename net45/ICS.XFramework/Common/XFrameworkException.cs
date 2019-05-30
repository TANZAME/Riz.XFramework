using System;

namespace ICS.XFramework
{
    /// <summary>
    /// XFramework 类库异常
    /// </summary>
    [Serializable]
    public class XFrameworkException : Exception
    {
        public XFrameworkException()
        {
        }

        public XFrameworkException(string message)
            : base(message)
        {
        }

        public XFrameworkException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public XFrameworkException(string message, params object[] args)
            : base(string.Format(message, args))
        {
        }

        public XFrameworkException(string message, Exception innerException, params object[] args)
            : base(string.Format(message, args), innerException)
        {
        }

        public static void Throw(string message)
        {
            throw new XFrameworkException(message);
        }

        public static void Throw(string message, params object[] args)
        {
            throw new XFrameworkException(message, args);
        }

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
