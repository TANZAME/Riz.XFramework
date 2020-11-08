using System;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

namespace Riz.XFramework.Data
{
    /// <summary>
    /// 运行时类型工具类
    /// </summary>
    public class TypeUtils
    {
        static HashSet<Type> _primitiveTypes = new HashSet<Type>();
        static HashSet<Type> _numberTypes = new HashSet<Type>();
        static HashSet<Type> _numericTypes = new HashSet<Type>();

        static TypeUtils()
        {
            _primitiveTypes.Add(typeof(char));
            _primitiveTypes.Add(typeof(Nullable<char>));
            _primitiveTypes.Add(typeof(string));
            _primitiveTypes.Add(typeof(byte[]));
            _primitiveTypes.Add(typeof(bool));
            _primitiveTypes.Add(typeof(Nullable<bool>));
            _primitiveTypes.Add(typeof(byte));
            _primitiveTypes.Add(typeof(Nullable<byte>));
            _primitiveTypes.Add(typeof(DateTime));
            _primitiveTypes.Add(typeof(Nullable<DateTime>));
            _primitiveTypes.Add(typeof(decimal));
            _primitiveTypes.Add(typeof(Nullable<decimal>));
            _primitiveTypes.Add(typeof(double));
            _primitiveTypes.Add(typeof(Nullable<double>));
            _primitiveTypes.Add(typeof(Guid));
            _primitiveTypes.Add(typeof(Nullable<Guid>));
            _primitiveTypes.Add(typeof(short));
            _primitiveTypes.Add(typeof(Nullable<short>));
            _primitiveTypes.Add(typeof(int));
            _primitiveTypes.Add(typeof(Nullable<int>));
            _primitiveTypes.Add(typeof(long));
            _primitiveTypes.Add(typeof(Nullable<long>));
            _primitiveTypes.Add(typeof(sbyte));
            _primitiveTypes.Add(typeof(Nullable<sbyte>));
            _primitiveTypes.Add(typeof(float));
            _primitiveTypes.Add(typeof(Nullable<float>));
            _primitiveTypes.Add(typeof(ushort));
            _primitiveTypes.Add(typeof(Nullable<ushort>));
            _primitiveTypes.Add(typeof(uint));
            _primitiveTypes.Add(typeof(Nullable<uint>));
            _primitiveTypes.Add(typeof(ulong));
            _primitiveTypes.Add(typeof(Nullable<ulong>));
            _primitiveTypes.Add(typeof(TimeSpan));
            _primitiveTypes.Add(typeof(Nullable<TimeSpan>));
            _primitiveTypes.Add(typeof(DateTimeOffset));
            _primitiveTypes.Add(typeof(Nullable<DateTimeOffset>));
            _primitiveTypes.Add(typeof(byte[]));
            // object 类型不能加进来，不然会与dynamic类型产生冲突

            _numberTypes.Add(typeof(byte));
            _numberTypes.Add(typeof(Nullable<byte>));
            _numberTypes.Add(typeof(decimal));
            _numberTypes.Add(typeof(Nullable<decimal>));
            _numberTypes.Add(typeof(double));
            _numberTypes.Add(typeof(Nullable<double>));
            _numberTypes.Add(typeof(short));
            _numberTypes.Add(typeof(Nullable<short>));
            _numberTypes.Add(typeof(int));
            _numberTypes.Add(typeof(Nullable<int>));
            _numberTypes.Add(typeof(long));
            _numberTypes.Add(typeof(Nullable<long>));
            _numberTypes.Add(typeof(sbyte));
            _numberTypes.Add(typeof(Nullable<sbyte>));
            _numberTypes.Add(typeof(float));
            _numberTypes.Add(typeof(Nullable<float>));
            _numberTypes.Add(typeof(ushort));
            _numberTypes.Add(typeof(Nullable<ushort>));
            _numberTypes.Add(typeof(uint));
            _numberTypes.Add(typeof(Nullable<uint>));
            _numberTypes.Add(typeof(ulong));
            _numberTypes.Add(typeof(Nullable<ulong>));

            _numericTypes.Add(typeof(decimal));
            _numericTypes.Add(typeof(Nullable<decimal>));
            _numericTypes.Add(typeof(double));
            _numericTypes.Add(typeof(Nullable<double>));
            _numericTypes.Add(typeof(float));
            _numericTypes.Add(typeof(Nullable<float>));
        }

        /// <summary>
        /// 判断给定类型是否是ORM支持的基元类型
        /// </summary>
        public static bool IsPrimitiveType(Type type)
        {
            return _primitiveTypes.Contains(type);
        }

        /// <summary>
        /// 判断给定类型是否是数字类型
        /// </summary>
        public static bool IsNumberType(Type type)
        {
            return _numberTypes.Contains(type);
        }

        /// <summary>
        /// 判断给定类型是否是数值类型，即有小数位的数值
        /// </summary>
        public static bool IsNumericType(Type type)
        {
            return _numericTypes.Contains(type);
        }

        /// <summary>
        /// 判断给定类型是否是匿名类
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsAnonymousType(Type type)
        {
            return type != null && type.Name.Length > 18 && type.Name.IndexOf("AnonymousType", 5, StringComparison.InvariantCulture) == 5;
        }

        /// <summary>
        /// 判断给定类型是否是ORM支持的泛型列表类型  IList 接口及其继承类
        /// </summary>
        public static bool IsCollectionType(Type type)
        {
            //typeof(IList<>).GetTypeInfo()
            //type.GetGenericTypeDefinition();
            if (!type.IsGenericType) return false;

            var type2 = type.GetGenericTypeDefinition();
            if (type2 == typeof(List<>)) return true;
            else if (type2 == typeof(IList<>)) return true;
            //if (type == typeof(List<>)) return true;
            //else if (type == typeof(IList<>)) return true;
            else return typeof(IList<>).IsAssignableFrom(type.GetGenericTypeDefinition()) || type.GetInterface(typeof(IList<>).FullName) != null;

        }

        /// <summary>
        /// 是否编译生成的类型
        /// </summary>
        public static bool IsCompilerGenerated(Type t)
        {
            if (t == null)
                return false;

            return t.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), false)
                || IsCompilerGenerated(t.DeclaringType);
        }

        /// <summary>
        /// 返回给定类型的 NULL 值
        /// </summary>
        /// <param name="type">给定类型</param>
        /// <returns></returns>
        public static object GetNullValue(Type type)
        {
            if (type.IsValueType)
            {
                if (type.IsEnum)
                {
                    return GetNullValue(Enum.GetUnderlyingType(type));
                }

                if (type.IsPrimitive)
                {
                    if (type == typeof(Int32)) { return 0; }
                    if (type == typeof(Double)) { return (Double)0; }
                    if (type == typeof(Int16)) { return (Int16)0; }
                    if (type == typeof(SByte)) { return (SByte)0; }
                    if (type == typeof(Int64)) { return (Int64)0; }
                    if (type == typeof(Byte)) { return (Byte)0; }
                    if (type == typeof(UInt16)) { return (UInt16)0; }
                    if (type == typeof(UInt32)) { return (UInt32)0; }
                    if (type == typeof(UInt64)) { return (UInt64)0; }
                    if (type == typeof(UInt64)) { return (UInt64)0; }
                    if (type == typeof(Single)) { return (Single)0; }
                    if (type == typeof(Boolean)) { return false; }
                    if (type == typeof(char)) { return '\0'; }
                }
                else
                {
                    //DateTime : 01/01/0001 00:00:00
                    //TimeSpan : 00:00:00
                    //Guid : 00000000-0000-0000-0000-000000000000
                    //Decimal : 0

                    if (type == typeof(DateTime)) { return DateTime.MinValue; }
                    if (type == typeof(Decimal)) { return 0m; }
                    if (type == typeof(Guid)) { return Guid.Empty; }
                    if (type == typeof(TimeSpan)) { return new TimeSpan(0, 0, 0); }
                }
            }

            return null;
        }

        /// <summary>
        /// 获取给定类型的ORM支持成员
        /// </summary>
        /// <param name="type"></param>
        /// <param name="includePrivate">包含私有成员</param>
        /// <returns></returns>
        public static IEnumerable<MemberInfo> GetMembers(Type type, bool includePrivate = false)
        {
            Func<MemberInfo, bool> predicate = x => x.MemberType == MemberTypes.Method || x.MemberType == MemberTypes.Field || x.MemberType == MemberTypes.Property;
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public;
            if (includePrivate) flags = flags | BindingFlags.NonPublic;

            var result = type.GetMembers(flags).Where(predicate);
            if (type.IsInterface)
            {
                var inheritsTypes = type.GetInterfaces();
                foreach (var ihType in inheritsTypes)
                {
                    var second = TypeUtils.GetMembers(ihType);
                    result = result.Union(second);
                }
            }

            return result;
        }

        /// <summary>
        /// 获取指定成员的数据类型
        /// <para>仅字段和属性有效</para>
        /// </summary>
        public static Type GetDataType(MemberInfo member)
        {
            Type result = null;
            if (member != null && member.MemberType == MemberTypes.Field)
                result = ((FieldInfo)member).FieldType;
            else if (member != null && member.MemberType == MemberTypes.Property)
                result = ((PropertyInfo)member).PropertyType;

            return result;
        }

        /// <summary>
        /// 判断字段或属性成员的数据类型是否为 ORM 支持的基元类型
        /// </summary>
        /// <param name="member">字段或属性成员</param>
        /// <returns></returns>
        public static bool IsPrimitive(MemberInfo member)
        {
            Type t = null;
            if (member != null && member.MemberType == MemberTypes.Field)
                t = ((FieldInfo)member).FieldType;
            else if (member != null && member.MemberType == MemberTypes.Property)
                t = ((PropertyInfo)member).PropertyType;

            return t == null ? false : TypeUtils.IsPrimitiveType(t);
        }

        /// <summary>
        /// 获取字段或属性成员的 <see cref="ColumnAttribute"/>
        /// </summary>
        /// <param name="member">字段或属性成员</param>
        /// <param name="reflectedType">调用字段或属性成员的实际类型</param>
        /// <returns></returns>
        public static ColumnAttribute GetColumnAttribute(MemberInfo member, Type reflectedType)
        {
            if (member == null) return null;

            if (reflectedType == null) reflectedType = member.ReflectedType ?? member.DeclaringType;
            ColumnAttribute column = null;
            if (!TypeUtils.IsAnonymousType(reflectedType) && !TypeUtils.IsPrimitiveType(reflectedType))
            {
                var typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo(reflectedType);
                var m = typeRuntime.GetMember(member.Name);
                if (m != null) column = m.Column;
            }

            return column;
        }

        /// <summary>
        /// 获取字段或属性成员对应的数据字段名称
        /// </summary>
        /// <param name="member">字段或属性成员</param>
        /// <param name="reflectedType">调用字段或属性成员的实际类型</param>
        /// <returns></returns>
        public static string GetFieldName(MemberInfo member, Type reflectedType)
        {
            if (member == null) return null;

            ColumnAttribute column = TypeUtils.GetColumnAttribute(member, reflectedType);
            return column != null && !string.IsNullOrEmpty(column.Name) ? column.Name : member.Name;
        }
    }
}
