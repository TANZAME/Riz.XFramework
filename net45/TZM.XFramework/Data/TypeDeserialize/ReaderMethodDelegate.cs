
using System;
using System.Data;
using System.Reflection;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// 返回 DataReader 的方法成员
    /// </summary>
    /// <param name="reader">提供数据列访问</param>
    /// <param name="memberType">实体属性类型</param>
    /// <param name="columnType">数据字段类型</param>
    /// <returns></returns>
    public delegate MethodInfo ReaderMethodDelegate(IDataRecord reader, Type memberType, ref Type columnType);
}
