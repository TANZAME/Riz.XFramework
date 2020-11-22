
using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Riz.XFramework.Data
{
    /// <summary>
    /// 构造函数访问器
    /// </summary>
    public class ConstructorAccessor : MemberAccessorBase
    {
        private Func<object[], object> _construction = null;

        /// <summary>
        /// 构造函数元数据
        /// </summary>
        public new ConstructorInfo Member => (ConstructorInfo)base.Member;

        /// <summary>
        /// 初始化 <see cref="ConstructorAccessor"/> 类的新实例
        /// </summary>
        /// <param name="ctor">构造函数</param>
        public ConstructorAccessor(ConstructorInfo ctor)
            : base(ctor)
        {
        }

        /// <summary>
        /// 动态调用构造函数
        /// </summary>
        /// <param name="parameters">构造函数参数</param>
        /// <returns></returns>
        public object Invoke(params object[] parameters)
        {
            _construction = _construction ?? ConstructorAccessor.Initialize(this.Member);
            return _construction(parameters);
        }

        /// <summary>
        /// 动态调用构造函数
        /// </summary>
        /// <param name="target">拥有此成员的实例，固定传 null</param>
        /// <param name="parameters">构造函数参数</param>
        /// <returns></returns>
        public override object Invoke(object target, params object[] parameters) => this.Invoke(parameters);

        // 初始化访问器
        private static Func<object[], object> Initialize(ConstructorInfo ctor)
        {
            var dynamicMethod = new DynamicMethod(string.Empty, typeof(object), new[] { typeof(object[]) }, true);
            ILGenerator il = dynamicMethod.GetILGenerator();
            ParameterInfo[] parameters = ctor.GetParameters();

            for (int index = 0; index < parameters.Length; ++index)
            {
                il.Emit(OpCodes.Ldarg_0);
                il.EmitInt(index);
                il.Emit(OpCodes.Ldelem_Ref);
                il.EmitCast(parameters[index].ParameterType);
            }
            il.Emit(OpCodes.Newobj, ctor);
            il.Emit(OpCodes.Ret);

            return dynamicMethod.CreateDelegate(typeof(Func<object[], object>)) as Func<object[], object>;
        }
    }
}
