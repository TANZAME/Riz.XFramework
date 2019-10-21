using System;
using System.Linq;
using TZM.XFramework.Data;
using System.ComponentModel;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TZM.XFramework
{
    /// <summary>
    /// 实体验证器
    /// </summary>
    public class XFrameworkValidator
    {
        // 微软自带的 Validator 的不足：
        // 验证顺序按实体属性的书写顺序而不按元数据类型属性的书写顺序
        // 在前端要求只检查到一个错误就返回的情况下，极有可能产生返回的错误
        // 与前端UI的控制摆放顺序不一致的情况。故重写验证器



        // 验证特性缓存器
        private static ValidationAttributeStore _store = ValidationAttributeStore.Instance;

        // 获取实例的属性值
        private static IDictionary<ValidationContext, object> GetPropertyValues(object instance, ValidationContext context)
        {
            IDictionary<ValidationContext, object> result = new Dictionary<ValidationContext, object>();
            TypeRuntimeInfo typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo(instance.GetType());
            foreach (var invoker in typeRuntime.Invokers)
            {
                if (invoker.MemberType == System.Reflection.MemberTypes.Property)
                {
                    ValidationContext context2 = XFrameworkValidator.CreateValidationContext(instance, context);
                    context2.MemberName = invoker.Member.Name;

                    if (XFrameworkValidator._store.GetPropertyValidationAttributes(context2).Any<ValidationAttribute>())
                    {
                        result.Add(context2, invoker.Invoke(instance));
                    }
                }
            }

            return result;
        }

        // 创建新的上下文
        internal static ValidationContext CreateValidationContext(object instance, ValidationContext validationContext)
        {
            if (validationContext == null) throw new ArgumentNullException("validationContext");
            return new ValidationContext(instance, validationContext, validationContext.Items);
        }

        // 验证错误包装器
        private class ValidationError
        {
            internal object Value { get; set; }

            internal ValidationAttribute ValidationAttribute { get; set; }

            internal ValidationResult ValidationResult { get; set; }

            internal ValidationError(ValidationAttribute validationAttribute, object value, ValidationResult validationResult)
            {
                this.ValidationAttribute = validationAttribute;
                this.ValidationResult = validationResult;
                this.Value = value;
            }

            internal void ThrowValidationException()
            {
                throw new ValidationException(this.ValidationResult, this.ValidationAttribute, this.Value);
            }
        }

        // 验证给定值
        private static IEnumerable<XFrameworkValidator.ValidationError> GetValidationErrors(object value, ValidationContext validationContext, IEnumerable<ValidationAttribute> attributes, bool breakOnFirstError)
        {
            if (validationContext == null)
            {
                throw new ArgumentNullException("validationContext");
            }

            List<XFrameworkValidator.ValidationError> result = new List<XFrameworkValidator.ValidationError>();
            XFrameworkValidator.ValidationError item;
            foreach (ValidationAttribute attr in attributes)
            {
                if (!XFrameworkValidator.TryValidate(value, validationContext, attr, out item))
                {
                    result.Add(item);
                    if (breakOnFirstError) break;
                }
            }
            return result;
        }

        // 验证给定值
        private static bool TryValidate(object value, ValidationContext validationContext, ValidationAttribute attribute, out XFrameworkValidator.ValidationError validationError)
        {
            if (validationContext == null)
            {
                throw new ArgumentNullException("validationContext");
            }

            ValidationResult validationResult = attribute.GetValidationResult(value, validationContext);
            if (validationResult != ValidationResult.Success)
            {
                validationError = new XFrameworkValidator.ValidationError(attribute, value, validationResult);
                return false;
            }

            validationError = null;
            return true;
        }


        /// <summary>
        /// 通过使用验证上下文、验证结果集合和用于指定是否验证所有属性的值，确定指定的对象是否有效。
        /// </summary>
        /// <param name="instance">要验证的对象</param>
        /// <param name="validationContext">用于描述要验证的对象的上下文</param>
        /// <param name="validationResults">用于包含每个失败的验证的集合</param>
        /// <param name="breakOnFirstError">当第一个错误产生时，是否不再进行后续验证</param>
        /// <returns></returns>
        public static bool TryValidateObject(object instance, ValidationContext validationContext, ICollection<ValidationResult> validationResults, bool breakOnFirstError)//, Type metadataType = null)
        {
            if (instance == null) throw new ArgumentNullException("instance");
            if (validationContext == null) throw new ArgumentNullException("validationContext");

            // 取当前实例的所有属性值
            IDictionary<ValidationContext, object> propertyValues = XFrameworkValidator.GetPropertyValues(instance, validationContext);
            List<XFrameworkValidator.ValidationError> list = new List<XFrameworkValidator.ValidationError>();

            // 取元数据描述
            Type metadataType = null;
            //if (metadataType == null)
            //{
            Type type = instance.GetType();
            MetadataTypeAttribute attr =
                TypeDescriptor
                .GetAttributes(type)
                .OfType<MetadataTypeAttribute>()
                .FirstOrDefault();
            if (attr != null) metadataType = attr.MetadataClassType;
            //}

            if (metadataType == null) throw new NullReferenceException(string.Format("type {{{0}}} has no metadata descriptor.", type.Name));
            PropertyDescriptorCollection descriptors = TypeDescriptor.GetProperties(metadataType);

            // 按照元数据属性的书写顺序逐个验证实体属性
            foreach (PropertyDescriptor des in descriptors)
            {
                KeyValuePair<ValidationContext, object> kv = propertyValues.FirstOrDefault(x => x.Key.MemberName == des.Name);
                ValidationContext context = kv.Key;
                object value = kv.Value;
                if (context != null)
                {
                    IEnumerable<ValidationAttribute> validationAttributes = XFrameworkValidator._store.GetPropertyValidationAttributes(context);
                    list.AddRange(XFrameworkValidator.GetValidationErrors(value, context, validationAttributes, breakOnFirstError));
                }
                if (list.Count > 0 && breakOnFirstError) break;
            }

            bool result = true;
            foreach (var current in list)
            {
                result = false;
                if (validationResults != null)
                {
                    validationResults.Add(current.ValidationResult);
                }
            }

            return result;
        }

        /// <summary>
        /// 通过使用验证上下文、验证结果集合和用于指定是否验证所有属性的值，确定指定的属性是否有效。
        /// </summary>
        /// <param name="value">要验证的对象</param>
        /// <param name="context">用于描述要验证的对象的上下文</param>
        /// <param name="validationResults">用于包含每个失败的验证的集合</param>
        /// <param name="breakOnFirstError">当第一个错误产生时，是否不再进行后续验证</param>
        /// <returns></returns>
        public static bool TryValidateProperty(object value, ValidationContext context, ICollection<ValidationResult> validationResults, bool breakOnFirstError)
        {
            bool result = true;
            IEnumerable<ValidationAttribute> propertyValidationAttributes = XFrameworkValidator._store.GetPropertyValidationAttributes(context);

            foreach (XFrameworkValidator.ValidationError current in XFrameworkValidator.GetValidationErrors(value, context, propertyValidationAttributes, breakOnFirstError))
            {
                result = false;
                if (validationResults != null)
                {
                    validationResults.Add(current.ValidationResult);
                }
            }
            return result;
        }

        // 验证属性缓存器
        internal class ValidationAttributeStore
        {
            // 缓存项
            private abstract class StoreItem
            {
                private static IEnumerable<ValidationAttribute> _emptyValidationAttributeEnumerable = new ValidationAttribute[0];

                private IEnumerable<ValidationAttribute> _validationAttributes;

                internal IEnumerable<ValidationAttribute> ValidationAttributes
                {
                    get
                    {
                        return this._validationAttributes;
                    }
                }

                internal DisplayAttribute DisplayAttribute
                {
                    get;
                    set;
                }

                internal StoreItem(IEnumerable<Attribute> attributes)
                {
                    this._validationAttributes = attributes.OfType<ValidationAttribute>();
                    this.DisplayAttribute = attributes.OfType<DisplayAttribute>().SingleOrDefault<DisplayAttribute>();
                }
            }

            // 类型缓存项
            private class TypeStoreItem : ValidationAttributeStore.StoreItem
            {
                private object _syncRoot = new object();

                private Type _type;

                private Dictionary<string, ValidationAttributeStore.PropertyStoreItem> _propertyStoreItems;

                internal TypeStoreItem(Type type, IEnumerable<Attribute> attributes)
                    : base(attributes)
                {
                    this._type = type;
                }

                internal ValidationAttributeStore.PropertyStoreItem GetPropertyStoreItem(string propertyName)
                {
                    ValidationAttributeStore.PropertyStoreItem result = null;
                    if (!this.TryGetPropertyStoreItem(propertyName, out result))
                    {
                        throw new ArgumentException(string.Format(System.Globalization.CultureInfo.CurrentCulture, "Unkown Property {0}.{1}", new object[]
					{
						this._type.Name,
						propertyName
					}), "propertyName");
                    }
                    return result;
                }

                internal bool TryGetPropertyStoreItem(string propertyName, out ValidationAttributeStore.PropertyStoreItem item)
                {
                    if (string.IsNullOrEmpty(propertyName))
                    {
                        throw new ArgumentNullException("propertyName");
                    }
                    if (this._propertyStoreItems == null)
                    {
                        object syncRoot = this._syncRoot;
                        lock (syncRoot)
                        {
                            if (this._propertyStoreItems == null)
                            {
                                this._propertyStoreItems = this.CreatePropertyStoreItems();
                            }
                        }
                    }
                    return this._propertyStoreItems.TryGetValue(propertyName, out item);
                }

                private Dictionary<string, ValidationAttributeStore.PropertyStoreItem> CreatePropertyStoreItems()
                {
                    Dictionary<string, ValidationAttributeStore.PropertyStoreItem> dictionary = new Dictionary<string, ValidationAttributeStore.PropertyStoreItem>();
                    PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(this._type);
                    foreach (PropertyDescriptor propertyDescriptor in properties)
                    {
                        ValidationAttributeStore.PropertyStoreItem value = new ValidationAttributeStore.PropertyStoreItem(propertyDescriptor.PropertyType, ValidationAttributeStore.TypeStoreItem.GetExplicitAttributes(propertyDescriptor).Cast<Attribute>());
                        dictionary[propertyDescriptor.Name] = value;
                    }
                    return dictionary;
                }

                public static AttributeCollection GetExplicitAttributes(PropertyDescriptor propertyDescriptor)
                {
                    //if (propertyDescriptor.Name == "LowActualQty_N")
                    //{
                    //}
                    List<Attribute> list = new List<Attribute>(propertyDescriptor.Attributes.Cast<Attribute>());
                    IEnumerable<Attribute> enumerable = TypeDescriptor.GetAttributes(propertyDescriptor.PropertyType).Cast<Attribute>();
                    bool flag = false;
                    foreach (Attribute current in enumerable)
                    {
                        for (int i = list.Count - 1; i >= 0; i--)
                        {
                            if (current == list[i])
                            {
                                list.RemoveAt(i);
                                flag = true;
                            }
                        }
                    }
                    if (!flag)
                    {
                        return propertyDescriptor.Attributes;
                    }
                    return new AttributeCollection(list.ToArray());
                }
            }

            // 属性缓存项
            private class PropertyStoreItem : ValidationAttributeStore.StoreItem
            {
                private Type _propertyType;

                internal Type PropertyType
                {
                    get
                    {
                        return this._propertyType;
                    }
                }

                internal PropertyStoreItem(Type propertyType, IEnumerable<Attribute> attributes)
                    : base(attributes)
                {
                    this._propertyType = propertyType;
                }
            }

            private static ValidationAttributeStore _singleton = new ValidationAttributeStore();

            private Dictionary<Type, ValidationAttributeStore.TypeStoreItem> _typeStoreItems = new Dictionary<Type, ValidationAttributeStore.TypeStoreItem>();

            internal static ValidationAttributeStore Instance
            {
                get
                {
                    return ValidationAttributeStore._singleton;
                }
            }

            internal IEnumerable<ValidationAttribute> GetPropertyValidationAttributes(ValidationContext validationContext)
            {
                ValidationAttributeStore.EnsureValidationContext(validationContext);
                ValidationAttributeStore.TypeStoreItem typeStoreItem = this.GetTypeStoreItem(validationContext.ObjectType);
                ValidationAttributeStore.PropertyStoreItem propertyStoreItem = typeStoreItem.GetPropertyStoreItem(validationContext.MemberName);
                var attributes = propertyStoreItem.ValidationAttributes;

                List<IOrderValidationAttribute> list = new List<IOrderValidationAttribute>();
                List<ValidationAttribute> list2 = new List<ValidationAttribute>();
                foreach (var attribute in attributes)
                {
                    var item = attribute as IOrderValidationAttribute;
                    if (item != null) list.Add(item);
                    else list2.Add(attribute);
                }

                var result =
                    list
                    .OrderBy(x => x.Order)
                    .OfType<ValidationAttribute>()
                    .Union(list2);
                return result;
            }

            internal Type GetPropertyType(ValidationContext validationContext)
            {
                ValidationAttributeStore.EnsureValidationContext(validationContext);
                ValidationAttributeStore.TypeStoreItem typeStoreItem = this.GetTypeStoreItem(validationContext.ObjectType);
                ValidationAttributeStore.PropertyStoreItem propertyStoreItem = typeStoreItem.GetPropertyStoreItem(validationContext.MemberName);
                return propertyStoreItem.PropertyType;
            }


            private ValidationAttributeStore.TypeStoreItem GetTypeStoreItem(Type type)
            {
                if (type == null)
                {
                    throw new ArgumentNullException("type");
                }
                Dictionary<Type, ValidationAttributeStore.TypeStoreItem> typeStoreItems = this._typeStoreItems;
                ValidationAttributeStore.TypeStoreItem result;
                lock (typeStoreItems)
                {
                    ValidationAttributeStore.TypeStoreItem typeStoreItem = null;
                    if (!this._typeStoreItems.TryGetValue(type, out typeStoreItem))
                    {
                        IEnumerable<Attribute> attributes = TypeDescriptor.GetAttributes(type).Cast<Attribute>();
                        typeStoreItem = new ValidationAttributeStore.TypeStoreItem(type, attributes);
                        this._typeStoreItems[type] = typeStoreItem;
                    }
                    result = typeStoreItem;
                }
                return result;
            }

            private static void EnsureValidationContext(ValidationContext validationContext)
            {
                if (validationContext == null)
                {
                    throw new ArgumentNullException("validationContext");
                }
            }
        }
    }

    /// <summary>
    /// 顺序接口
    /// </summary>
    public interface IOrderValidationAttribute
    {
        // https://forums.asp.net/t/2105833.aspx?Is+it+posible+to+control+Validation+attribute+order+
        // I checked the docs here https://docs.asp.net/en/latest/mvc/models/validation.html
        // And there is no concept of Validation attribute order...
        // is there any bug of validation attribute order?

        /// <summary>
        /// 验证顺序
        /// </summary>
        int Order { get; set; }
    }
}
