
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace TZM.XFramework
{
    /// <summary>
    /// 类型元数据注册器
    /// </summary>
    public static class MetadataTypesRegister
    {
        /// <summary>
        /// 注册类型元数据描述类 
        /// 常用于使用 System.ComponentModel.DataAnnotations.Validator 进行数据验证时批量注入 MetadataTypes
        /// </summary>
        public static void Register(Assembly assembly)
        {
            foreach (Type type in assembly.GetTypes())
            {
                AttributeCollection collection = TypeDescriptor.GetAttributes(type);
                foreach (var attr in collection)
                {
                    MetadataTypeAttribute m = attr as MetadataTypeAttribute;
                    if (m != null)
                    {                        
                        TypeDescriptor.AddProviderTransparent(
                        new AssociatedMetadataTypeTypeDescriptionProvider(type, m.MetadataClassType), type);
                    }
                }
            }
        }
    }
}