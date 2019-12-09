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
    public partial class PropertyAccessor : MemberAccessorBase
    {
        private Func<object, object> _get = null;
        private Action<object, object> _set = null;
        private PropertyInfo _property = null;
        private MethodInfo _getMethod = null;
        private MethodInfo _setMethod = null;

        /// <summary>
        /// 初始化 <see cref="PropertyAccessor"/> 类的新实例
        /// </summary>
        /// <param name="property">字段元数据</param>
        public PropertyAccessor(PropertyInfo property)
            :base(property)
        {
            _property = property;
        }

        /// <summary>
        /// 属性的Get方法
        /// </summary>
        public MethodInfo GetMethod
        {
            get
            {
                if (_getMethod == null) _getMethod = _property.GetGetMethod(true);
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
                if (_setMethod == null) _setMethod = _property.GetSetMethod(true);
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
                return _property.CanRead;
            }
        }


        /// <summary>
        /// 可写
        /// </summary>
        public bool CanWrite
        {
            get
            {
                return _property.CanWrite;
            }
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
            if (!_property.CanRead) throw new XFrameworkException("{0} is unreadable", base.FullName);

            if (_get == null) _get = PropertyAccessor.InitializeGetter(_property);
            return _get(target);
        }

        /// <summary>
        /// 设置属性值
        /// </summary>
        /// <param name="target">拥有该成员的类实例</param>
        /// <param name="value">字段/属性值</param>
        private void Set(object target, object value)
        {
            if (!_property.CanWrite) throw new XFrameworkException("{0} is unwritable", base.FullName);
            _set = _set ?? PropertyAccessor.InitializeSetter(_property);
            _set(target, value ?? TypeUtils.GetNullValue(_property.PropertyType));
        }

        // 初始化 get 动态方法
        static Func<object, object> InitializeGetter(PropertyInfo property)
        {
            MethodInfo method = property.GetGetMethod(true);
            var m = new DynamicMethod(method.Name, typeof(object), new Type[] { typeof(object) }, method.Module);
            ILGenerator g = m.GetILGenerator();

            if (!method.IsStatic)
            {
                g.Emit(OpCodes.Ldarg_0);                            //Load the first argument,(target object)
                g.Emit(OpCodes.Castclass, property.DeclaringType);  //Cast to the source type
            }
            g.EmitCall(method.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, method, null);   //Get the property value
            if (method.ReturnType.IsValueType) g.Emit(OpCodes.Box, method.ReturnType);      //Box if necessary
            g.Emit(OpCodes.Ret);

            return m.CreateDelegate(typeof(Func<object, object>)) as Func<object, object>;
        }

        // 初始化 set 动态方法
        static Action<object, object> InitializeSetter(PropertyInfo property)
        {
            MethodInfo method = property.GetSetMethod(true);
            var m = new DynamicMethod(method.Name, null, new Type[] { typeof(object), typeof(object) }, method.Module);
            ILGenerator g = m.GetILGenerator();
            Type paramType = method.GetParameters()[0].ParameterType;

            if (!method.IsStatic)
            {
                g.Emit(OpCodes.Ldarg_0);                            //Load the first argument (target object)
                g.Emit(OpCodes.Castclass, method.DeclaringType);    //Cast to the source type
            }

            g.Emit(OpCodes.Ldarg_1); //Load the second argument (value object)
            g.EmitCast(paramType);
            g.EmitCall(method.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, method, null); //Set the property value
            g.Emit(OpCodes.Ret);

            return m.CreateDelegate(typeof(Action<object, object>)) as Action<object, object>;
        }


    }
}
