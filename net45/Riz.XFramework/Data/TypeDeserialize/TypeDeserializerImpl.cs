
using System;
using System.Data;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;

namespace Riz.XFramework.Data
{
    /// <summary>
    /// <see cref="IDataReader"/> 转实体映射器
    /// </summary>
    public class TypeDeserializerImpl
    {
        // 这个类的代码在 Dapper.NET 的基础上修改的，实体映射确实强悍得一匹
        // https://github.com/StackExchange/Dapper

        const string _linqBinaryName = "System.Data.Linq.Binary";
        static readonly MethodInfo _enumParse = typeof(Enum).GetMethod("Parse", new Type[] { typeof(Type), typeof(string), typeof(bool) });
        static readonly MethodInfo _readChar = typeof(TypeDeserializerImpl).GetMethod("ReadChar", BindingFlags.Static | BindingFlags.NonPublic);
        static readonly MethodInfo _readNullChar = typeof(TypeDeserializerImpl).GetMethod("ReadNullableChar", BindingFlags.Static | BindingFlags.NonPublic);
        static readonly MethodInfo _throwException = typeof(TypeDeserializerImpl).GetMethod("ThrowDataException", BindingFlags.Static | BindingFlags.NonPublic);
        static readonly MethodInfo _typeFromHandle = typeof(Type).GetMethod("GetTypeFromHandle");
        static readonly MethodInfo _changeType = typeof(Convert).GetMethod("ChangeType", new Type[] { typeof(object), typeof(Type) });
        static readonly MethodInfo _toString = typeof(object).GetMethod("ToString", Type.EmptyTypes);

        static readonly MethodInfo _isDBNull = typeof(IDataRecord).GetMethod("IsDBNull", new Type[] { typeof(int) });
        static readonly MethodInfo _getBoolean = typeof(IDataRecord).GetMethod("GetBoolean", new Type[] { typeof(int) });
        static readonly MethodInfo _getByte = typeof(IDataRecord).GetMethod("GetByte", new Type[] { typeof(int) });
        static readonly MethodInfo _getChar = typeof(IDataRecord).GetMethod("GetChar", new Type[] { typeof(int) });
        static readonly MethodInfo _getDateTime = typeof(IDataRecord).GetMethod("GetDateTime", new Type[] { typeof(int) });
        static readonly MethodInfo _getDecimal = typeof(IDataRecord).GetMethod("GetDecimal", new Type[] { typeof(int) });
        static readonly MethodInfo _getDouble = typeof(IDataRecord).GetMethod("GetDouble", new Type[] { typeof(int) });
        static readonly MethodInfo _getFloat = typeof(IDataRecord).GetMethod("GetFloat", new Type[] { typeof(int) });
        static readonly MethodInfo _getGuid = typeof(IDataRecord).GetMethod("GetGuid", new Type[] { typeof(int) });
        static readonly MethodInfo _getInt16 = typeof(IDataRecord).GetMethod("GetInt16", new Type[] { typeof(int) });
        static readonly MethodInfo _getInt32 = typeof(IDataRecord).GetMethod("GetInt32", new Type[] { typeof(int) });
        static readonly MethodInfo _getInt64 = typeof(IDataRecord).GetMethod("GetInt64", new Type[] { typeof(int) });
        static readonly MethodInfo _getString = typeof(IDataRecord).GetMethod("GetString", new Type[] { typeof(int) });
        static readonly MethodInfo _getValue = typeof(IDataRecord).GetMethod("GetValue", new Type[] { typeof(int) });

        static readonly ConstructorInfo _ctorGuid_string = typeof(Guid).GetConstructor(new[] { typeof(string) });
        static readonly ConstructorInfo _ctorGuid_bytes = typeof(Guid).GetConstructor(new[] { typeof(byte[]) });

        /// <summary>
        /// 单例实现
        /// </summary>
        public static TypeDeserializerImpl Instance = new TypeDeserializerImpl();

        /// <summary>
        /// 私有构造函数
        /// </summary>
        protected TypeDeserializerImpl() 
        {
        }

        /// <summary>
        /// 生成实体映射委托
        /// </summary>
        /// <param name="type">实体类型</param>
        /// <param name="reader">数据读取器</param>
        /// <param name="columns">字段列描述</param>
        /// <param name="start">开始索引</param>
        /// <param name="end">结束索引</param>
        /// <returns></returns>
        public Func<IDataRecord, object> GetTypeDeserializer(Type type, IDataRecord reader, ColumnDescriptorCollection columns = null, int start = 0, int? end = null)
        {
            //// specify a new assembly name
            //var assemblyName = new AssemblyName("Riz.Deserialize");

            //// create assembly builder
            //var assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndSave);

            //// create module builder
            //var moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name, assemblyName.Name + ".dll", true);

            //// create type builder for a class
            //var typeBuilder = moduleBuilder.DefineType("Riz.Deserialize.Deserializer", TypeAttributes.Public);

            //// create method builder
            //var methodBuilder = typeBuilder.DefineMethod("GetModel",
            //              MethodAttributes.Public | MethodAttributes.Static,
            //              typeof(object),
            //              new Type[] { typeof(IDataRecord)
            //    });

            TypeRuntimeInfo typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo(type);
            DynamicMethod dynamicMethod = new DynamicMethod(string.Format("Deserialize{0}", Guid.NewGuid()), typeof(object), new Type[] { typeof(IDataRecord) }, true);
            //ILGenerator il = methodBuilder.GetILGenerator();
            ILGenerator il = dynamicMethod.GetILGenerator();

            il.DeclareLocal(typeof(int));       // [0] int index
            il.DeclareLocal(type);              // [1] {type}

            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Stloc_0);

            // 有参构造函数
            ConstructorInfo specializedConstructor = null;
            if (type.IsValueType)
            {
                // 如果是值类型，则将值类型设置为空或者0
                il.Emit(OpCodes.Ldloca_S, (byte)1);
                il.Emit(OpCodes.Initobj, type);
            }
            else
            {
                var ctor = typeRuntime.Constructor.Member;
                if (ctor.GetParameters().Length > 0) specializedConstructor = ctor;
                else
                {
                    // 如果不是匿名类或者只有无参构造函数，则new一个对象
                    il.Emit(OpCodes.Newobj, ctor);
                    il.Emit(OpCodes.Stloc_1);   // [1] {type}=new {type}
                }

            }

            // try #####
            il.BeginExceptionBlock();
            if (specializedConstructor == null) il.Emit(OpCodes.Ldloc_1);// [target]

            // stack is now [target]
            Label finishLabel = il.DefineLabel();
            Label loadNullLabel = il.DefineLabel();
            int enumDeclareLocal = -1;
            if (end == null) end = reader.FieldCount;
            for (int index = start; index < end; index++)
            {
                // 找出对应DataReader中的字段名
                string memberName = reader.GetName(index);
                if (columns != null)
                {
                    ColumnDescriptor column = null;
                    columns.TryGetValue(memberName, out column);
                    memberName = column != null ? column.Name : string.Empty;
                }

                // 本地变量赋值
                il.Emit(OpCodes.Ldc_I4, index); // [target][index]
                il.Emit(OpCodes.Stloc_0);       // [target]

                // 如果导航属性分割列=DbNull，那么此导航属性赋空值
                if (memberName == AppConst.NAVIGATION_SPLITON_NAME)
                {
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldc_I4, index);
                    il.Emit(OpCodes.Callvirt, _isDBNull);
                    il.Emit(OpCodes.Brtrue_S, loadNullLabel);
                }

                var m = typeRuntime.GetMember(memberName);
                if (m == null) continue;

                if (specializedConstructor == null) il.Emit(OpCodes.Dup);// stack is now [target][target]

                // 数据字段类型
                Type myFieldType = reader.GetFieldType(index);
                Type realFieldType = myFieldType;
                // 实体属性类型
                Type memberType = m.DataType;
                MethodInfo getFieldValue = this.GetReaderMethod(myFieldType, memberType, ref realFieldType);
                if (myFieldType != realFieldType) myFieldType = realFieldType;

                Label isDbNullLabel = il.DefineLabel();
                Label nextLoopLabel = il.DefineLabel();
                // 判断字段是否是 DbNull
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldc_I4, index);
                il.Emit(OpCodes.Callvirt, _isDBNull);
                il.Emit(OpCodes.Brtrue, isDbNullLabel);

                // =>DataReader.Getxx(index)
                il.Emit(OpCodes.Ldarg_0);                       // stack is now [target][target][reader]
                if (getFieldValue.DeclaringType != typeof(IDataRecord)) il.Emit(OpCodes.Castclass, getFieldValue.DeclaringType); // (SqlDataReader)IDataReader
                il.Emit(OpCodes.Ldc_I4, index);                 // stack is now [target][target][reader][index]
                il.Emit(OpCodes.Callvirt, getFieldValue);       // stack is now [target][target][value-or-object]

                if (memberType == typeof(char) || memberType == typeof(char?))
                {
                    il.EmitCall(OpCodes.Call, memberType == typeof(char) ? _readChar : _readNullChar, null);    // stack is now [target][target][typed-value]
                }
                else
                {
                    // unbox nullable enums as the primitive, i.e. byte etc
                    var nullUnderlyingType = Nullable.GetUnderlyingType(memberType);
                    var unboxType = nullUnderlyingType != null && nullUnderlyingType.IsEnum ? nullUnderlyingType : memberType;

                    if (unboxType.IsEnum)
                    {
                        Type numericType = Enum.GetUnderlyingType(unboxType);
                        if (myFieldType != typeof(string)) ConvertBoxedStack(il, myFieldType, unboxType, numericType);
                        else
                        {
                            if (enumDeclareLocal == -1)
                            {
                                enumDeclareLocal = il.DeclareLocal(typeof(string)).LocalIndex;
                            }
                            il.Emit(OpCodes.Castclass, typeof(string));       // stack is now [target][target][string]
                            il.StoreLocal(enumDeclareLocal);                  // stack is now [target][target]
                            il.Emit(OpCodes.Ldtoken, unboxType);              // stack is now [target][target][enum-type-token]
                            il.EmitCall(OpCodes.Call, _typeFromHandle, null); // stack is now [target][target][enum-type]
                            il.LoadLocal(enumDeclareLocal);                   // stack is now [target][target][enum-type][string]
                            il.Emit(OpCodes.Ldc_I4_1);                        // stack is now [target][target][enum-type][string][true]
                            il.EmitCall(OpCodes.Call, _enumParse, null);      // stack is now [target][target][enum-as-object]
                            il.Emit(OpCodes.Unbox_Any, unboxType);            // stack is now [target][target][typed-value]
                        }

                        // new Nullable<TValue>(TValue)
                        if (nullUnderlyingType != null) EmitNewNullable(il, memberType); // stack is now [target][target][typed-value]
                    }
                    else if (memberType.FullName == _linqBinaryName)
                    {
                        var ctor = memberType.GetConstructor(new Type[] { typeof(byte[]) });
                        il.Emit(OpCodes.Unbox_Any, typeof(byte[]));           // stack is now [target][target][byte-array]
                        il.Emit(OpCodes.Newobj, ctor);                        // stack is now [target][target][binary]
                    }
                    else
                    {
                        bool noBoxed = myFieldType == unboxType || myFieldType == nullUnderlyingType;

                        // myFieldType和实体属性类型一致， 如果用 DataReader.GetValue，则要强制转换{object}为实体属性定义的类型
                        bool useCast = noBoxed && getFieldValue == _getValue && unboxType != typeof(object);
                        if (useCast) il.EmitCast(nullUnderlyingType ?? unboxType);// stack is now [target][target][typed-value]

                        // myFieldType和实体属性类型不一致，需要做类型转换
                        if (!noBoxed)
                        {
                            if (getFieldValue == _getValue && myFieldType.IsValueType) il.Emit(OpCodes.Unbox_Any, myFieldType);// stack is now [target][target][value]
                            // not a direct match; need to tweak the unbox
                            ConvertBoxedStack(il, myFieldType, nullUnderlyingType ?? unboxType, null);
                        }

                        // new Nullable<TValue>(TValue)
                        if (nullUnderlyingType != null) EmitNewNullable(il, memberType);// stack is now [target][target][typed-value]
                    }
                }

                if (specializedConstructor == null)
                {
                    // Store the value in the property/field
                    if (m.MemberType == MemberTypes.Field) il.Emit(OpCodes.Stfld, m.Member as FieldInfo);// stack is now [target]
                    else
                    {
                        MethodInfo setMethod = (m as PropertyAccessor).Member.GetSetMethod(true);
                        il.Emit(type.IsValueType ? OpCodes.Call : OpCodes.Callvirt, setMethod);// stack is now [target]
                    }
                }

                il.Emit(OpCodes.Br_S, nextLoopLabel);   // stack is now [target]


                il.MarkLabel(isDbNullLabel);    // incoming stack: [target][target]
                if (specializedConstructor == null) il.Emit(OpCodes.Pop);   // stack is now [target]
                else
                {
                    // DbNull，将NULL或者0推到栈顶
                    if (!m.DataType.IsValueType) il.Emit(OpCodes.Ldnull);
                    else
                    {
                        int localIndex = il.DeclareLocal(m.DataType).LocalIndex;
                        il.LoadLocalAddress(localIndex);
                        il.Emit(OpCodes.Initobj, m.DataType);
                        il.LoadLocal(localIndex);
                    }
                }


                il.MarkLabel(nextLoopLabel);

            }

            if (specializedConstructor != null)
            {
                il.Emit(OpCodes.Newobj, specializedConstructor);
            }
            il.Emit(OpCodes.Stloc_1);               // stack is empty

            // 直接跳到结束标签返回实体
            il.Emit(OpCodes.Br, finishLabel);

            // 将 null 赋值给实体
            il.MarkLabel(loadNullLabel);
            il.Emit(OpCodes.Pop);
            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Stloc_1);


            il.MarkLabel(finishLabel);
            il.BeginCatchBlock(typeof(Exception));  // stack is Exception
            il.Emit(OpCodes.Ldloc_0);   // stack is Exception, index
            il.Emit(OpCodes.Ldarg_0);   // stack is Exception, index, reader
            il.EmitCall(OpCodes.Call, _throwException, null);
            il.EndExceptionBlock();

            il.Emit(OpCodes.Ldloc_1);   // stack is [rval]
            il.Emit(OpCodes.Ret);

            //// then create the whole class type
            //typeBuilder.CreateType();
            //// save assembly
            //assemblyBuilder.Save(assemblyName.Name + ".dll");

            return (Func<IDataRecord, object>)dynamicMethod.CreateDelegate(typeof(Func<IDataRecord, object>));
        }

        /// <summary>
        /// 获取对应每列的读取方法
        /// </summary>
        /// <param name="myFieldType">字段类型</param>
        /// <param name="memberType">实体类型</param>
        /// <param name="realFieldType">字段类型（引用传递）</param>
        /// <returns></returns>
        protected virtual MethodInfo GetReaderMethod(Type myFieldType, Type memberType, ref Type realFieldType)
        {
            MethodInfo result = null;
            if (myFieldType == typeof(char)) result = _getChar;
            else if (myFieldType == typeof(string)) result = _getString;
            else if (myFieldType == typeof(bool) || myFieldType == typeof(bool?)) result = _getBoolean;
            else if (myFieldType == typeof(byte) || myFieldType == typeof(byte?)) result = _getByte;
            else if (myFieldType == typeof(DateTime) || myFieldType == typeof(DateTime?)) result = _getDateTime;
            else if (myFieldType == typeof(decimal) || myFieldType == typeof(decimal?)) result = _getDecimal;
            else if (myFieldType == typeof(double) || myFieldType == typeof(double?)) result = _getDouble;
            else if (myFieldType == typeof(float) || myFieldType == typeof(float?)) result = _getFloat;
            else if (myFieldType == typeof(Guid) || myFieldType == typeof(Guid?)) result = _getGuid;
            else if (myFieldType == typeof(short) || myFieldType == typeof(short?)) result = _getInt16;
            else if (myFieldType == typeof(int) || myFieldType == typeof(int?)) result = _getInt32;
            else if (myFieldType == typeof(long) || myFieldType == typeof(long?)) result = _getInt64;
            else result = _getValue;

            return result;

            //bit	Boolean
            //tinyint	Byte
            //smallint	Int16
            //int	Int32
            //bigint	Int64
            //smallmoney	Decimal
            //money	Decimal
            //numeric	Decimal
            //decimal	Decimal
            //float	Double
            //real	Single
            //smalldatetime	DateTime
            //datetime	DateTime
            //timestamp	DateTime
            //char	String
            //text	String
            //varchar	String
            //nchar	String
            //ntext	String
            //nvarchar	String
            //binary	Byte[]
            //varbinary	Byte[]
            //image	Byte[]
            //uniqueidentifier	Guid
            //Variant	Object
        }

        // 类型不匹配，稍微做一下转换
        private void ConvertBoxedStack(ILGenerator il, Type from, Type to, Type via)
        {
            MethodInfo op;
            if (from == (via ?? to))
            {
                //
            }
            else if ((op = GetOperator(from, via ?? to)) != null)
            {
                il.Emit(OpCodes.Call, op);// stack is now [target][target][typed-value]
            }
            else if (from.IsValueType && (via ?? to) == typeof(string))
            {
                // this is handy for things like value <===> string
                il.Emit(OpCodes.Box, from);
                il.Emit(OpCodes.Callvirt, _toString);
            }
            else if (this.ConvertBoxedStackExtension(il, from, to, via))
            {
            }
            else
            {
                bool handled = false;
                OpCode opCode = default(OpCode);
                switch (Type.GetTypeCode(from))
                {
                    case TypeCode.Boolean:
                    case TypeCode.Byte:
                    case TypeCode.SByte:
                    case TypeCode.Int16:
                    case TypeCode.UInt16:
                    case TypeCode.Int32:
                    case TypeCode.UInt32:
                    case TypeCode.Int64:
                    case TypeCode.UInt64:
                    case TypeCode.Single:
                    case TypeCode.Double:
                        handled = true;
                        switch (Type.GetTypeCode(via ?? to))
                        {
                            case TypeCode.Byte:
                                opCode = OpCodes.Conv_Ovf_I1_Un; break;
                            case TypeCode.SByte:
                                opCode = OpCodes.Conv_Ovf_I1; break;
                            case TypeCode.UInt16:
                                opCode = OpCodes.Conv_Ovf_I2_Un; break;
                            case TypeCode.Int16:
                                opCode = OpCodes.Conv_Ovf_I2; break;
                            case TypeCode.UInt32:
                                opCode = OpCodes.Conv_Ovf_I4_Un; break;
                            case TypeCode.Boolean:                      // boolean is basically an int, at least at this level
                            case TypeCode.Int32:
                                opCode = OpCodes.Conv_Ovf_I4; break;
                            case TypeCode.UInt64:
                                opCode = OpCodes.Conv_Ovf_I8_Un; break;
                            case TypeCode.Int64:
                                opCode = OpCodes.Conv_Ovf_I8; break;
                            case TypeCode.Single:
                                opCode = OpCodes.Conv_R4; break;
                            case TypeCode.Double:
                                opCode = OpCodes.Conv_R8; break;
                            default:
                                handled = false;
                                break;
                        }
                        break;
                }
                if (handled)
                {
                    //il.Emit(OpCodes.Unbox_Any, from);                 // stack is now [target][target][col-typed-value]
                    il.Emit(opCode);                                    // stack is now [target][target][typed-value]
                    if (to == typeof(bool))
                    {
                        // compare to zero; I checked "csc" - this is the trick it uses; nice
                        il.Emit(OpCodes.Ldc_I4_0);
                        il.Emit(OpCodes.Ceq);
                        il.Emit(OpCodes.Ldc_I4_0);
                        il.Emit(OpCodes.Ceq);
                    }
                }
                else
                {
                    il.Emit(OpCodes.Ldtoken, via ?? to);                // stack is now [target][target][value][member-type-token]
                    il.EmitCall(OpCodes.Call, _typeFromHandle, null);   // stack is now [target][target][value][member-type]
                    il.EmitCall(OpCodes.Call, _changeType, null);       // stack is now [target][target][boxed-member-type-value]
                    il.Emit(OpCodes.Unbox_Any, to);                     // stack is now [target][target][typed-value]
                }
            }
        }

        /// <summary>
        /// 自定义类型转换
        /// </summary>
        /// <param name="il">IL 指令</param>
        /// <param name="from">源类型</param>
        /// <param name="to">目标类型</param>
        /// <param name="via">拆箱类型</param>
        /// <returns></returns>
        protected virtual bool ConvertBoxedStackExtension(ILGenerator il, Type from, Type to, Type via)
        {
            // 内置 Guid 扩展 #######

            if (from == typeof(byte[]) && to == typeof(Guid))
            {
                // byte[] => guid
                il.Emit(OpCodes.Castclass, typeof(byte[]));
                il.Emit(OpCodes.Newobj, _ctorGuid_bytes);
                return true;
            }
            else if (from == typeof(string) && to == typeof(Guid))
            {
                // string => guid
                il.Emit(OpCodes.Castclass, typeof(string));
                il.Emit(OpCodes.Newobj, _ctorGuid_string);
                return true;
            }
            else
            {
                return false;
            }
        }

        static MethodInfo GetOperator(Type from, Type to)
        {
            if (to == null) return null;
            MethodInfo[] fromMethods, toMethods;
            return ResolveOperator(fromMethods = from.GetMethods(BindingFlags.Static | BindingFlags.Public), from, to, "op_Implicit")
                ?? ResolveOperator(toMethods = to.GetMethods(BindingFlags.Static | BindingFlags.Public), from, to, "op_Implicit")
                ?? ResolveOperator(fromMethods, from, to, "op_Explicit")
                ?? ResolveOperator(toMethods, from, to, "op_Explicit");

        }

        static MethodInfo ResolveOperator(MethodInfo[] methods, Type from, Type to, string name)
        {
            for (int i = 0; i < methods.Length; i++)
            {
                if (methods[i].Name != name || methods[i].ReturnType != to) continue;
                var args = methods[i].GetParameters();
                if (args.Length != 1 || args[0].ParameterType != from) continue;
                return methods[i];
            }
            return null;
        }

        static char ReadChar(object value)
        {
            if (value == null || value is DBNull) throw new ArgumentNullException("value");
            string s = value as string;
            if (s == null || s.Length != 1) throw new ArgumentException("A single-character was expected", "value");
            return s[0];
        }

        static char? ReadNullableChar(object value)
        {
            if (value == null || value is DBNull) return null;
            string s = value as string;
            if (s == null || s.Length != 1) throw new ArgumentException("A single-character was expected", "value");
            return s[0];
        }

        static void ThrowDataException(Exception ex, int index, IDataRecord reader)
        {
            Exception newException;

            string name = "(n/a)", value = "(n/a)";
            if (reader != null && index >= 0 && index < reader.FieldCount) name = reader.GetName(index);

            newException = new DataException(string.Format("Error parsing column {0} ({1}={2})", index, name, value), ex);
            throw newException;
        }

        static void EmitNewNullable(ILGenerator il, Type nullableType)
        {
            var ctor = nullableType.GetConstructor(new[] { Nullable.GetUnderlyingType(nullableType) });
            il.Emit(OpCodes.Newobj, ctor);
        }
    }
}
