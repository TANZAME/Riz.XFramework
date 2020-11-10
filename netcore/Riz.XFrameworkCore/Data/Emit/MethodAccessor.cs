
using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;

namespace Riz.XFramework.Data
{
    /// <summary>
    /// 方法成员访问器
    /// </summary>
    public sealed class MethodAccessor : MemberAccessorBase
    {
        private Func<object, object[], object> _function;

        /// <summary>
        /// 方法元数据
        /// </summary>
        public new MethodInfo Member { get { return (MethodInfo)base.Member; } }

        /// <summary>
        /// 重载的方法列表
        /// 类似于哈希冲突的链址法
        /// </summary>
        public List<MethodAccessor> Overrides { get; set; }

        /// <summary>
        /// 初始化 <see cref="MethodAccessor"/> 类的新实例
        /// </summary>
        /// <param name="method">方法元数据</param>
        public MethodAccessor(MethodInfo method)
            : base(method)
        {
        }

        /// <summary>
        /// 动态调用方法
        /// </summary>
        /// <param name="target">拥有该成员的类实例</param>
        /// <param name="parameters">方法参数</param>
        /// <returns></returns>
        public override object Invoke(object target, params object[] parameters)
        {
            if (_function == null) _function = MethodAccessor.Initialize(this.Member);
            return _function(target, parameters);
        }

        // 初始化访问器
        private static Func<object, object[], object> Initialize(MethodInfo method)
        {
            var dynamicMethod = new DynamicMethod(method.Name, typeof(object), new Type[2] { typeof(object), typeof(object[]) }, typeof(TypeRuntimeInfoCache), true);
            ILGenerator il = dynamicMethod.GetILGenerator();
            ParameterInfo[] parameters = method.GetParameters();
            Type[] parameterTypes = new Type[parameters.Length + (!method.IsStatic ? 1 : 0)];

            for (int index = 0; index < parameterTypes.Length; ++index)
            {
                parameterTypes[index] = index == parameters.Length
                    ? method.DeclaringType
                    : (parameters[index].ParameterType.IsByRef ? parameters[index].ParameterType.GetElementType() : parameters[index].ParameterType);
            }
            LocalBuilder[] local = new LocalBuilder[parameterTypes.Length];
            for (int index = 0; index < parameterTypes.Length; ++index)
            {
                local[index] = il.DeclareLocal(parameterTypes[index], true);
            }
            for (int index = 0; index < parameters.Length; ++index)
            {
                il.Emit(OpCodes.Ldarg_1);
                il.EmitInt(index);
                il.Emit(OpCodes.Ldelem_Ref);
                il.EmitCast(parameterTypes[index]);
                il.Emit(OpCodes.Stloc, local[index]);
            }

            if (!method.IsStatic)
            {
                il.Emit(OpCodes.Ldarg_0);
                if (method.DeclaringType.IsValueType)
                {
                    il.Emit(OpCodes.Unbox_Any, method.DeclaringType);
                    il.Emit(OpCodes.Stloc, local[local.Length - 1]);
                    il.Emit(OpCodes.Ldloca_S, local.Length - 1);
                }
                else il.Emit(OpCodes.Castclass, method.DeclaringType);
                // fix #方法参数是接口类，传参是继承类。此时需要进行类型转换 <TypeDeserializer用>
            }

            for (int index = 0; index < parameters.Length; ++index)
            {
                if (parameters[index].ParameterType.IsByRef)
                    il.Emit(OpCodes.Ldloca_S, local[index]);
                else
                    il.Emit(OpCodes.Ldloc, local[index]);
            }

            if (method.IsVirtual)
                il.EmitCall(OpCodes.Callvirt, method, null);
            else
                il.EmitCall(OpCodes.Call, method, null);
            if (method.ReturnType == typeof(void))
                il.Emit(OpCodes.Ldnull);
            else
                il.EmitBoxIfNeeded(method.ReturnType);

            for (int index = 0; index < parameters.Length; ++index)
            {
                if (parameters[index].ParameterType.IsByRef)
                {
                    il.Emit(OpCodes.Ldarg_1);
                    il.EmitInt(index);
                    il.Emit(OpCodes.Ldloc, local[index]);
                    il.EmitBoxIfNeeded(local[index].LocalType);
                    il.Emit(OpCodes.Stelem_Ref);
                }
            }
            il.Emit(OpCodes.Ret);

            return dynamicMethod.CreateDelegate(typeof(Func<object, object[], object>)) as Func<object, object[], object>;
        }
    }
}
