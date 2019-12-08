using System;
using System.Reflection.Emit;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// 中间语言 (MSIL) 指令扩展类
    /// </summary>
    public static class ILGeneratorExtensions
    {
        /// <summary>
        /// 对值类型进行装箱
        /// </summary>
        /// <param name="il">(MSIL)指令</param>
        /// <param name="type">指定类型</param>
        public static void EmitBoxIfNeeded(this ILGenerator il, Type type)
        {
            if (!type.IsValueType)
                return;
            il.Emit(OpCodes.Box, type);
        }

        /// <summary>
        /// 尝试将对象转换为指定的类。
        /// </summary>
        /// <param name="il">(MSIL)指令</param>
        /// <param name="type">指定类型</param>
        public static void EmitCast(this ILGenerator il, Type type)
        {
            if (type.IsValueType)
                il.Emit(OpCodes.Unbox_Any, type);
            else
                il.Emit(OpCodes.Castclass, type);
        }

        /// <summary>
        /// 将整形数值推送到计算堆栈上。
        /// </summary>
        /// <param name="il">(MSIL)指令</param>
        /// <param name="value">整形数值</param>
        public static void EmitInt(this ILGenerator il, int value)
        {
            switch (value)
            {
                case -1: il.Emit(OpCodes.Ldc_I4_M1); break;
                case 0: il.Emit(OpCodes.Ldc_I4_0); break;
                case 1: il.Emit(OpCodes.Ldc_I4_1); break;
                case 2: il.Emit(OpCodes.Ldc_I4_2); break;
                case 3: il.Emit(OpCodes.Ldc_I4_3); break;
                case 4: il.Emit(OpCodes.Ldc_I4_4); break;
                case 5: il.Emit(OpCodes.Ldc_I4_5); break;
                case 6: il.Emit(OpCodes.Ldc_I4_6); break;
                case 7: il.Emit(OpCodes.Ldc_I4_7); break;
                case 8: il.Emit(OpCodes.Ldc_I4_8); break;
                default:
                    if (value > -129 && value < 128)
                    {
                        il.Emit(OpCodes.Ldc_I4_S, (sbyte)value);
                    }
                    else
                    {
                        il.Emit(OpCodes.Ldc_I4, value);
                    }
                    break;
            }
        }

        /// <summary>
        /// 将整形数值推送到计算堆栈上。
        /// </summary>
        /// <param name="il">(MSIL)指令</param>
        /// <param name="value">整形数值</param>
        public static void EmitInt32(this ILGenerator il, int value)
        {
            switch (value)
            {
                case -1: il.Emit(OpCodes.Ldc_I4_M1); break;
                case 0: il.Emit(OpCodes.Ldc_I4_0); break;
                case 1: il.Emit(OpCodes.Ldc_I4_1); break;
                case 2: il.Emit(OpCodes.Ldc_I4_2); break;
                case 3: il.Emit(OpCodes.Ldc_I4_3); break;
                case 4: il.Emit(OpCodes.Ldc_I4_4); break;
                case 5: il.Emit(OpCodes.Ldc_I4_5); break;
                case 6: il.Emit(OpCodes.Ldc_I4_6); break;
                case 7: il.Emit(OpCodes.Ldc_I4_7); break;
                case 8: il.Emit(OpCodes.Ldc_I4_8); break;
                default:
                    if (value >= -128 && value <= 127)
                    {
                        il.Emit(OpCodes.Ldc_I4_S, (sbyte)value);
                    }
                    else
                    {
                        il.Emit(OpCodes.Ldc_I4, value);
                    }
                    break;
            }
        }

        /// <summary>
        /// 加载指定索引处的本地局部变量到计算堆栈上
        /// </summary>
        /// <param name="il">(MSIL)指令</param>
        /// <param name="index">指定索引</param>
        public static void LoadLocal(this ILGenerator il, int index)
        {
            if (index < 0 || index >= short.MaxValue) throw new ArgumentNullException("index");
            switch (index)
            {
                case 0: il.Emit(OpCodes.Ldloc_0); break;
                case 1: il.Emit(OpCodes.Ldloc_1); break;
                case 2: il.Emit(OpCodes.Ldloc_2); break;
                case 3: il.Emit(OpCodes.Ldloc_3); break;
                default:
                    if (index <= 255)
                    {
                        il.Emit(OpCodes.Ldloc_S, (byte)index);
                    }
                    else
                    {
                        il.Emit(OpCodes.Ldloc, (short)index);
                    }
                    break;
            }
        }

        /// <summary>
        /// 从计算堆栈的顶部弹出当前值并将其存储到指定索引 的局部变量列表中
        /// </summary>
        /// <param name="il">(MSIL)指令</param>
        /// <param name="index">指定索引</param>
        public static void StoreLocal(this ILGenerator il, int index)
        {
            if (index < 0 || index >= short.MaxValue) throw new ArgumentNullException("index");
            switch (index)
            {
                case 0: il.Emit(OpCodes.Stloc_0); break;
                case 1: il.Emit(OpCodes.Stloc_1); break;
                case 2: il.Emit(OpCodes.Stloc_2); break;
                case 3: il.Emit(OpCodes.Stloc_3); break;
                default:
                    if (index <= 255)
                    {
                        il.Emit(OpCodes.Stloc_S, (byte)index);
                    }
                    else
                    {
                        il.Emit(OpCodes.Stloc, (short)index);
                    }
                    break;
            }
        }

        /// <summary>
        /// 加载指定索引处的本地局部变量到计算堆栈上（短格式）
        /// </summary>
        /// <param name="il">(MSIL)指令</param>
        /// <param name="index">指定索引</param>
        public static void LoadLocalAddress(this ILGenerator il, int index)
        {
            if (index < 0 || index >= short.MaxValue) throw new ArgumentNullException("index");

            if (index <= 255)
            {
                il.Emit(OpCodes.Ldloca_S, (byte)index);
            }
            else
            {
                il.Emit(OpCodes.Ldloca, (short)index);
            }
        }
    }
}
