
using System;
using System.Reflection;
using System.Reflection.Emit;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// 构造函数访问器
    /// </summary>
    public class ConstructorAccessor
    {
        private Func<object[], object> _func = null;
        private ConstructorInfo _ctor = null;

        /// <summary>
        /// 类的第一个构造函数
        /// </summary>
        public ConstructorInfo Constructor { get { return _ctor; } }

        /// <summary>
        /// 初始化 <see cref="MemberAccessorBase"/> 类的新实例
        /// </summary>
        /// <param name="ctor">构造函数</param>
        public ConstructorAccessor(ConstructorInfo ctor)
        {
            _ctor = ctor;
        }

        static Func<object[], object> Initialize(ConstructorInfo ctor)
        {
            var m = new DynamicMethod(string.Empty, typeof(object), new[] { typeof(object[]) }, true);//declaringType);
            ILGenerator g = m.GetILGenerator();
            ParameterInfo[] parameters = ctor.GetParameters();

            for (int index = 0; index < parameters.Length; ++index)
            {
                g.Emit(OpCodes.Ldarg_0);
                g.EmitInt(index);
                g.Emit(OpCodes.Ldelem_Ref);
                g.EmitCast(parameters[index].ParameterType);
            }
            g.Emit(OpCodes.Newobj, ctor);
            g.Emit(OpCodes.Ret);

            return m.CreateDelegate(typeof(Func<object[], object>)) as Func<object[], object>;
        }

        /// <summary>
        /// 动态调用构造函数
        /// </summary>
        /// <param name="parameters">构造函数参数</param>
        /// <returns></returns>
        public object Invoke(params object[] parameters)
        {
            _func = _func ?? ConstructorAccessor.Initialize(_ctor);
            return _func(parameters);
        }
    }
}
