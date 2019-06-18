using System;
using System.Reflection;
using System.Reflection.Emit;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// 属性反射器
    /// <para>
    /// 底层使用 Emit IL 实现
    /// </para>
    /// </summary>
    public partial class PropertyInvoker : MemberInvokerBase
    {
        private Func<object, object> _get = null;
        private Action<object, object> _set = null;
        private PropertyInfo _member = null;
        private MethodInfo _getMethod = null;
        private MethodInfo _setMethod = null;

        /// <summary>
        /// 初始化 <see cref="PropertyInvoker"/> 类的新实例
        /// </summary>
        /// <param name="property">字段元数据</param>
        public PropertyInvoker(PropertyInfo property)
            :base(property)
        {
            _member = property;
        }

        /// <summary>
        /// 属性的Get方法
        /// </summary>
        public MethodInfo GetMethod
        {
            get
            {
                if (_getMethod == null) _getMethod = _member.GetGetMethod(true);
                return _getMethod;
            }
        }

        /// <summary>
        /// 属性的Set方法
        /// </summary>
        public MethodInfo SetMethod
        {
            get
            {
                if (_setMethod == null) _setMethod = _member.GetSetMethod(true);
                return _setMethod;
            }
        }

        /// <summary>
        /// 可读
        /// </summary>
        public bool CanRead
        {
            get
            {
                return _member.CanRead;
            }
        }


        /// <summary>
        /// 可写
        /// </summary>
        public bool CanWrite
        {
            get
            {
                return _member.CanWrite;
            }
        }

        /// <summary>
        /// 动态访问成员
        /// </summary>
        /// <param name="target">拥有该成员的类实例</param>
        /// <param name="value">字段/属性值</param>
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
                Set(target, parameters != null ? parameters[0] : TypeUtils.GetNullValue(base.DataType));
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
            if (!_member.CanRead) throw new XFrameworkException("{0} is unreadable", base.FullName);

            if (_get == null) _get = PropertyInvoker.InitializeGetInvoke(this);
            return _get(target);
        }

        /// <summary>
        /// 设置属性值
        /// </summary>
        /// <param name="target">拥有该成员的类实例</param>
        /// <param name="value">字段/属性值</param>
        private void Set(object target, object value)
        {
            if (!_member.CanWrite) throw new XFrameworkException("{0} is unwritable", base.FullName);
            //value = value ?? TypeUtils.GetNullValue(_member.PropertyType);
            //if (value != null)
            //{
            //    if (value.GetType() != this.DataType) value = Convert.ChangeType(value, this.DataType);
            //}
            _set = _set ?? PropertyInvoker.InitializeSetInvoke(this);
            _set(target, value ?? TypeUtils.GetNullValue(_member.PropertyType));
        }

        // 初始化 Get 动态方法
        static Func<object, object> InitializeGetInvoke(PropertyInvoker invoke)
        {
            MethodInfo method = invoke.GetMethod;
            DynamicMethod dynamicMethod = new DynamicMethod(method.Name, typeof(object), new Type[] { typeof(object) }, method.Module);
            ILGenerator g = dynamicMethod.GetILGenerator();

            if (!method.IsStatic)
            {
                g.Emit(OpCodes.Ldarg_0);  //Load the first argument,(target object)
                g.Emit(OpCodes.Castclass, invoke.Member.DeclaringType);   //Cast to the source type
            }
            g.EmitCall(method.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, method, null); //Get the property value
            if (method.ReturnType.IsValueType) g.Emit(OpCodes.Box, method.ReturnType); //Box if necessary
            g.Emit(OpCodes.Ret);

            return dynamicMethod.CreateDelegate(typeof(Func<object, object>)) as Func<object, object>;
        }

        // 初始化 Set 动态方法
        static Action<object, object> InitializeSetInvoke(PropertyInvoker invoke)
        {
            MethodInfo method = invoke.SetMethod;
            DynamicMethod dynamicMethod = new DynamicMethod(method.Name, null, new Type[] { typeof(object), typeof(object) }, method.Module);
            ILGenerator g = dynamicMethod.GetILGenerator();
            Type paramType = method.GetParameters()[0].ParameterType;

            if (!method.IsStatic)
            {
                g.Emit(OpCodes.Ldarg_0); //Load the first argument (target object)
                g.Emit(OpCodes.Castclass, method.DeclaringType); //Cast to the source type
            }
            g.Emit(OpCodes.Ldarg_1); //Load the second argument (value object)
            g.EmitCast(paramType);
            g.EmitCall(method.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, method, null); //Set the property value
            g.Emit(OpCodes.Ret);

            return dynamicMethod.CreateDelegate(typeof(Action<object, object>)) as Action<object, object>;
        }


    }
}
