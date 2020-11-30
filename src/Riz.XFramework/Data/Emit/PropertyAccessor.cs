using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Riz.XFramework.Data
{
    /// <summary>
    /// 属性反射器
    /// <para>
    /// 底层使用 Emit IL 实现
    /// </para>
    /// </summary>
    public partial class PropertyAccessor : FieldAccessorBase
    {
        private Func<object, object> _getter = null;
        private Action<object, object> _setter = null;

        /// <summary>
        /// 字段元数据
        /// </summary>
        public new PropertyInfo Member => (PropertyInfo)base.Member;

        /// <summary>
        /// 初始化 <see cref="PropertyAccessor"/> 类的新实例
        /// </summary>
        /// <param name="property">属性元数据</param>
        public PropertyAccessor(PropertyInfo property)
            :base(property)
        {
        }

        /// <summary>
        /// 动态访问成员
        /// </summary>
        /// <param name="target">拥有该成员的类实例</param>
        /// <param name="parameters">字段/属性值</param>
        public override object Invoke(object target, params object[] parameters)
        {
            if (parameters != null && parameters.Length == 0)
            {
                // get
                object obj = Get(target);
                return obj;
            }
            else
            {
                // set
                Set(target, parameters != null ? parameters[0] : TypeUtils.GetNullValue(base.CLRType));
                return null;
            }
        }

        /// <summary>
        /// 获取属性值
        /// </summary>
        /// <param name="target">拥有该成员的类实例</param>
        /// <returns></returns>
        private object Get(object target)
        {
            if (!this.Member.CanRead) throw new XFrameworkException("{0} is unreadable", base.FullName);

            if (_getter == null) _getter = PropertyAccessor.InitializeGetter(this.Member);
            return _getter(target);
        }

        /// <summary>
        /// 设置属性值
        /// </summary>
        /// <param name="target">拥有该成员的类实例</param>
        /// <param name="value">字段/属性值</param>
        private void Set(object target, object value)
        {
            if (!this.Member.CanWrite) throw new XFrameworkException("{0} is unwritable", base.FullName);
            _setter = _setter ?? PropertyAccessor.InitializeSetter(this.Member);
            _setter(target, value ?? TypeUtils.GetNullValue(this.Member.PropertyType));
        }

        // 初始化 get 动态方法
        static Func<object, object> InitializeGetter(PropertyInfo property)
        {
            MethodInfo getMethod = property.GetGetMethod(true);
            var dynamicMethod = new DynamicMethod(getMethod.Name, typeof(object), new Type[] { typeof(object) }, getMethod.Module);
            ILGenerator il = dynamicMethod.GetILGenerator();

            if (!getMethod.IsStatic)
            {
                il.Emit(OpCodes.Ldarg_0);                            //Load the first argument,(target object)
                il.Emit(OpCodes.Castclass, property.DeclaringType);  //Cast to the source type
            }
            il.EmitCall(getMethod.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, getMethod, null);   //Get the property value
            if (getMethod.ReturnType.IsValueType) il.Emit(OpCodes.Box, getMethod.ReturnType);      //Box if necessary
            il.Emit(OpCodes.Ret);

            return dynamicMethod.CreateDelegate(typeof(Func<object, object>)) as Func<object, object>;
        }

        // 初始化 set 动态方法
        static Action<object, object> InitializeSetter(PropertyInfo property)
        {
            MethodInfo setMethod = property.GetSetMethod(true);
            var dynamicMethod = new DynamicMethod(setMethod.Name, null, new Type[] { typeof(object), typeof(object) }, setMethod.Module);
            ILGenerator il = dynamicMethod.GetILGenerator();
            Type paramType = setMethod.GetParameters()[0].ParameterType;

            if (!setMethod.IsStatic)
            {
                il.Emit(OpCodes.Ldarg_0);                            //Load the first argument (target object)
                il.Emit(OpCodes.Castclass, setMethod.DeclaringType);    //Cast to the source type
            }

            il.Emit(OpCodes.Ldarg_1); //Load the second argument (value object)
            il.EmitCast(paramType);
            il.EmitCall(setMethod.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, setMethod, null); //Set the property value
            il.Emit(OpCodes.Ret);

            return dynamicMethod.CreateDelegate(typeof(Action<object, object>)) as Action<object, object>;
        }


    }
}
