using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Riz.XFramework.Data
{
    /// <summary>
    /// 字段成员访问器
    /// </summary>
    public partial class FieldAccessor : FieldAccessorBase
    {
        private Func<object, object> _getter = null;
        private Action<object, object> _setter = null;

        /// <summary>
        /// 字段元数据
        /// </summary>
        public new FieldInfo Member { get { return (FieldInfo)base.Member; } }

        /// <summary>
        /// 初始化 <see cref="FieldAccessor"/> 类的新实例
        /// </summary>
        /// <param name="field">字段元数据</param>
        public FieldAccessor(FieldInfo field)
            : base(field)
        {
        }

        /// <summary>
        /// 获取字段值
        /// </summary>
        /// <param name="target">拥有该成员的类实例</param>
        /// <returns></returns>
        private object Get(object target)
        {
            if (_getter == null) _getter = FieldAccessor.InitializeGetter(this.Member);
            return _getter(target);
        }

        /// <summary>
        /// 设置字段值
        /// </summary>
        /// <param name="target">拥有该成员的类实例</param>
        /// <param name="value">字段/属性值</param>
        private void Set(object target, object value)
        {
            if (_setter == null) _setter = FieldAccessor.InitializeSetter(this.Member);
            value = value ?? TypeUtils.GetNullValue(this.Member.FieldType);
            _setter(target, value);
        }

        /// <summary>
        /// 动态访问成员
        /// </summary>
        /// <param name="target">拥有该成员的类实例</param>
        /// <param name="parameters">参数列表</param>
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
                Set(target, parameters[0]);
                return null;
            }
        }

        // 初始化 Get 动态方法
        private static Func<object, object> InitializeGetter(FieldInfo field)
        {
            Type returnType = typeof(object);
            Type declaringType = field.DeclaringType;
            var dynamicMethod = new DynamicMethod(string.Empty, returnType, new Type[] { returnType }, declaringType);
            ILGenerator il = dynamicMethod.GetILGenerator();

            // We need a reference to the current instance (stored in local argument index 1) 
            // so Ldfld can load from the correct instance (this one).
            if (!field.IsStatic) il.Emit(OpCodes.Ldarg_0);
            il.Emit(field.IsStatic ? OpCodes.Ldsfld : OpCodes.Ldfld, field);

            // Now, we execute the box opcode, which pops the value of field 'x',
            // returning a reference to the filed value boxed as an object.
            if (field.FieldType.IsValueType) il.Emit(OpCodes.Box, field.FieldType);
            il.Emit(OpCodes.Ret);

            return dynamicMethod.CreateDelegate(typeof(Func<object, object>)) as Func<object, object>;
        }

        // 初始化 Set 动态方法
        private static Action<object, object> InitializeSetter(FieldInfo field)
        {
            Type declaringType = field.DeclaringType;
            var dynamicMethod = new DynamicMethod(string.Empty, null, new Type[] { typeof(object), typeof(object) }, declaringType);
            ILGenerator il = dynamicMethod.GetILGenerator();
            Type fieldType = field.FieldType;

            if (!field.IsStatic)
            {
                il.Emit(OpCodes.Ldarg_0);//Load the first argument (target object)
                il.Emit(OpCodes.Castclass, declaringType); //Cast to the source type
            }
            il.Emit(OpCodes.Ldarg_1);//Load the second argument (value object)
            if (fieldType.IsValueType) il.Emit(OpCodes.Unbox_Any, fieldType); //Unbox it 	
            il.Emit(field.IsStatic ? OpCodes.Stsfld : OpCodes.Stfld, field);
            il.Emit(OpCodes.Ret);

            return dynamicMethod.CreateDelegate(typeof(Action<object, object>)) as Action<object, object>;
        }
    }
}
