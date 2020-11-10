
using System;
using System.Data;
namespace Riz.XFramework.Data
{
    /// <summary>
    /// 数据列特性
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public class ColumnAttribute : Attribute
    {
        private object _dbType;
        private int _size;
        private int _precision;
        private int _scale;
        private ParameterDirection _direction;
        private bool _hasSetDbType = false;
        private bool _hasSetSize = false;
        private bool _hasSetPrecision = false;
        private bool _hasSetScale = false;
        private bool _hasSetDirection = false;

        /// <summary>
        /// 对应到数据库中的列名
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 是否为自增列
        /// </summary>
        public virtual bool IsIdentity { get; set; }

        /// <summary>
        /// 是否是主键列
        /// </summary>
        public bool IsKey { get; set; }

        /// <summary>
        /// 标志该属性不是主表字段
        /// <para>
        /// 用途：
        /// 1. 生成 INSERT/UPDATE 语句时忽略此字段
        /// 2. 生成不指定具体字段的 SELECT 语句时忽略此字段
        /// </para>
        /// </summary>
        public bool NoMapped { get; set; }

        /// <summary>
        /// 数据库字段的数据类型
        /// <para>字符串不设置此属性则默认为unicode</para>
        /// <para>为兼容不同数据库而使用 <see cref="object"/> 类型</para>
        /// </summary>
        public object DbType
        {
            get { return _dbType; }
            set
            {
                _dbType = value;
                _hasSetDbType = true;
            }
        }

        /// <summary>
        /// 指示 DbType 字段是否已设置
        /// </summary>
        public bool HasSetDbType
        {
            get { return _hasSetDbType; }
        }

        /// <summary>
        /// 精度（字段长度）
        /// <para>如果是字符串，size=-1 表示 max </para>
        /// </summary>
        public int Size
        {
            get { return _size; }
            set
            {
                _size = value;
                _hasSetSize = true;
            }
        }

        /// <summary>
        /// 指示 Size 字段是否已设置
        /// </summary>
        public bool HasSetSize
        {
            get { return _hasSetSize; }
        }

        /// <summary>
        /// 精度（字段长度）
        /// </summary>
        public int Precision
        {
            get { return _precision; }
            set
            {
                _precision = value;
                _hasSetPrecision = true;
            }
        }

        /// <summary>
        /// 指示 Precision 字段是否已设置
        /// </summary>
        public bool HasSetPrecision
        {
            get { return _hasSetPrecision; }
        }

        /// <summary>
        /// 范围（小数位数）
        /// </summary>
        public int Scale
        {
            get { return _scale; }
            set
            {
                _scale = value;
                _hasSetScale = true;
            }
        }

        /// <summary>
        /// 指示 Scale 字段是否已设置
        /// </summary>
        public bool HasSetScale
        {
            get { return _hasSetScale; }
        }

        /// <summary>
        /// 参数方向
        /// </summary>
        public ParameterDirection Direction
        {
            get { return _direction; }
            set
            {
                _direction = value;
                _hasSetDirection = true;
            }
        }

        /// <summary>
        /// 指示 Direction 字段是否已设置
        /// </summary>
        public bool HasSetDirection
        {
            get { return _hasSetDirection; }
        }


        /// <summary>
        /// 默认值表达式
        /// </summary>
        public object Default { get; set; }
    }
}
