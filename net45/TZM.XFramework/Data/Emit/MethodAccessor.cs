
using System;
using System.Reflection;
using System.Reflection.Emit;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// 方法成员访问器
    /// <para>
    /// 底层使用 Emit IL 实现
    /// </para>
    /// </summary>
    public sealed class MethodAccessor : MemberAccessorBase
    {
        private Func<object, object[], object> _func;
        private MethodInfo _method = null;

        /// <summary>
        /// 初始化 <see cref="MethodAccessor"/> 类的新实例
        /// </summary>
        /// <param name="method">方法元数据</param>
        public MethodAccessor(MethodInfo method)
            : base(method)
        {
            _method = method;
        }

        /// <summary>
        /// 动态调用方法
        /// </summary>
        /// <param name="target">拥有该成员的类实例</param>
        /// <param name="parameters">方法参数</param>
        /// <returns></returns>
        public override object Invoke(object target, params object[] parameters)
        {
            if (_func == null) _func = MethodAccessor.Initialize(_method);
            return _func(target, parameters);
        }

        // 初始化方法调用器
        private static Func<object, object[], object> Initialize(MethodInfo method)
        {
            var m = new DynamicMethod(method.Name, typeof(object), new Type[2] { typeof(object), typeof(object[]) }, typeof(TypeRuntimeInfoCache), true);
            ILGenerator g = m.GetILGenerator();
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
                local[index] = g.DeclareLocal(parameterTypes[index], true);
            }
            for (int index = 0; index < parameters.Length; ++index)
            {
                g.Emit(OpCodes.Ldarg_1);
                g.EmitInt(index);
                g.Emit(OpCodes.Ldelem_Ref);
                g.EmitCast(parameterTypes[index]);
                g.Emit(OpCodes.Stloc, local[index]);
            }

            if (!method.IsStatic)
            {
                g.Emit(OpCodes.Ldarg_0);
                if (method.DeclaringType.IsValueType)
                {
                    g.Emit(OpCodes.Unbox_Any, method.DeclaringType);
                    g.Emit(OpCodes.Stloc, local[local.Length - 1]);
                    g.Emit(OpCodes.Ldloca_S, local.Length - 1);
                }
                else g.Emit(OpCodes.Castclass, method.DeclaringType);
                // fix #方法参数是接口类，传参是继承类。此时需要进行类型转换 <TypeDeserializer用>
            }

            for (int index = 0; index < parameters.Length; ++index)
            {
                if (parameters[index].ParameterType.IsByRef)
                    g.Emit(OpCodes.Ldloca_S, local[index]);
                else
                    g.Emit(OpCodes.Ldloc, local[index]);
            }

            if (method.IsVirtual)
                g.EmitCall(OpCodes.Callvirt, method, null);
            else
                g.EmitCall(OpCodes.Call, method, null);
            if (method.ReturnType == typeof(void))
                g.Emit(OpCodes.Ldnull);
            else
                g.EmitBoxIfNeeded(method.ReturnType);

            for (int index = 0; index < parameters.Length; ++index)
            {
                if (parameters[index].ParameterType.IsByRef)
                {
                    g.Emit(OpCodes.Ldarg_1);
                    g.EmitInt(index);
                    g.Emit(OpCodes.Ldloc, local[index]);
                    g.EmitBoxIfNeeded(local[index].LocalType);
                    g.Emit(OpCodes.Stelem_Ref);
                }
            }
            g.Emit(OpCodes.Ret);

            return m.CreateDelegate(typeof(Func<object, object[], object>)) as Func<object, object[], object>;
        }
    }
}
