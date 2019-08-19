
using System;
using System.Data;
using System.Data.Common;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// 扩展帮助类
    /// </summary>
    public static class DataExtensions
    {
        /// <summary>
        /// 创建命令参数
        /// </summary>
        /// <param name="providerFactory">创建数据源类的提供程序</param>
        /// <param name="name">参数名称</param>
        /// <param name="value">参数值</param>
        /// <param name="dbType">数据类型</param>
        /// <param name="size">参数大小</param>
        /// <param name="precision">精度</param>
        /// <param name="scale">小数位</param>
        /// <param name="direction">方向</param>
        /// <returns></returns>
        public static IDbDataParameter CreateParameter(this DbProviderFactory providerFactory, string name, object value,
            DbType? dbType = null, int? size = null, int? precision = null, int? scale = null, ParameterDirection? direction = null)
        {
            IDbDataParameter parameter = providerFactory.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value;

            if (dbType != null) parameter.DbType = dbType.Value;
            if (size != null && (size.Value > 0 || size.Value == -1)) parameter.Size = size.Value;
            if (precision != null && precision.Value > 0) parameter.Precision = (byte)precision.Value;
            if (scale != null && scale.Value > 0) parameter.Scale = (byte)scale.Value;
            if (direction != null)
                parameter.Direction = direction.Value;
            else
                parameter.Direction = ParameterDirection.Input;

            // 补充字符串的长度
            if (value != null && value.GetType() == typeof(string) && size == null)
            {
                string s = value.ToString();
                if (dbType == null) parameter.DbType = DbType.String;
                if (parameter.DbType == DbType.String || parameter.DbType == DbType.StringFixedLength ||
                    parameter.DbType == DbType.AnsiString || parameter.DbType == DbType.AnsiStringFixedLength)
                {
                    if (s.Length <= 256) parameter.Size = 256;
                    else if (s.Length <= 512) parameter.Size = 512;
                    else if (s.Length <= 1024) parameter.Size = 1024;
                    else if (s.Length <= 4000) parameter.Size = 4000;
                    else if (s.Length <= 8000) parameter.Size = 8000;
                    else parameter.Size = -1;
                }
            }

            // 返回创建的参数
            return parameter;
        }
    }
}
