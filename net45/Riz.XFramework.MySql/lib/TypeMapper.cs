

// MySqlConnector.Core.TypeMapper
using MySql.Data.MySqlClient;
using MySqlConnector.Core;
using MySqlConnector.Protocol;
using MySqlConnector.Protocol.Payloads;
using MySqlConnector.Protocol.Serialization;
using MySqlConnector.Utilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

internal sealed class TypeMapper
{
	public static TypeMapper Instance = new TypeMapper();

	private readonly List<ColumnTypeMetadata> m_columnTypeMetadata;

	private readonly Dictionary<Type, DbTypeMapping> m_dbTypeMappingsByClrType;

	private readonly Dictionary<DbType, DbTypeMapping> m_dbTypeMappingsByDbType;

	private readonly Dictionary<string, ColumnTypeMetadata> m_columnTypeMetadataLookup;

	private readonly Dictionary<MySqlDbType, ColumnTypeMetadata> m_mySqlDbTypeToColumnTypeMetadata;

	private TypeMapper()
	{
		m_columnTypeMetadata = new List<ColumnTypeMetadata>();
		m_dbTypeMappingsByClrType = new Dictionary<Type, DbTypeMapping>();
		m_dbTypeMappingsByDbType = new Dictionary<DbType, DbTypeMapping>();
		m_columnTypeMetadataLookup = new Dictionary<string, ColumnTypeMetadata>(StringComparer.OrdinalIgnoreCase);
		m_mySqlDbTypeToColumnTypeMetadata = new Dictionary<MySqlDbType, ColumnTypeMetadata>();
		DbTypeMapping typeBoolean = AddDbTypeMapping(new DbTypeMapping(typeof(bool), new DbType[1]
		{
			DbType.Boolean
		}, (object o) => Convert.ToBoolean(o)));
		AddColumnTypeMetadata(new ColumnTypeMetadata("TINYINT", typeBoolean, MySqlDbType.Bool, false, false, 1, "BOOL", "BOOL", 1L));
		AddColumnTypeMetadata(new ColumnTypeMetadata("TINYINT", typeBoolean, MySqlDbType.Bool, true, false, 1, null, null, 0L));
		DbTypeMapping typeSbyte = AddDbTypeMapping(new DbTypeMapping(typeof(sbyte), new DbType[1]
		{
			DbType.SByte
		}, (object o) => Convert.ToSByte(o)));
		DbTypeMapping typeByte = AddDbTypeMapping(new DbTypeMapping(typeof(byte), new DbType[1]
		{
			DbType.Byte
		}, (object o) => Convert.ToByte(o)));
		DbTypeMapping typeShort = AddDbTypeMapping(new DbTypeMapping(typeof(short), new DbType[1]
		{
			DbType.Int16
		}, (object o) => Convert.ToInt16(o)));
		DbTypeMapping typeUshort = AddDbTypeMapping(new DbTypeMapping(typeof(ushort), new DbType[1]
		{
			DbType.UInt16
		}, (object o) => Convert.ToUInt16(o)));
		DbTypeMapping typeInt = AddDbTypeMapping(new DbTypeMapping(typeof(int), new DbType[1]
		{
			DbType.Int32
		}, (object o) => Convert.ToInt32(o)));
		DbTypeMapping typeUint = AddDbTypeMapping(new DbTypeMapping(typeof(uint), new DbType[1]
		{
			DbType.UInt32
		}, (object o) => Convert.ToUInt32(o)));
		DbTypeMapping typeLong = AddDbTypeMapping(new DbTypeMapping(typeof(long), new DbType[1]
		{
			DbType.Int64
		}, (object o) => Convert.ToInt64(o)));
		DbTypeMapping typeUlong = AddDbTypeMapping(new DbTypeMapping(typeof(ulong), new DbType[1]
		{
			DbType.UInt64
		}, (object o) => Convert.ToUInt64(o)));
		AddColumnTypeMetadata(new ColumnTypeMetadata("TINYINT", typeSbyte, MySqlDbType.Byte, false, false, 0, null, null, 0L));
		AddColumnTypeMetadata(new ColumnTypeMetadata("TINYINT", typeByte, MySqlDbType.UByte, true, false, 0, null, null, 0L));
		AddColumnTypeMetadata(new ColumnTypeMetadata("SMALLINT", typeShort, MySqlDbType.Int16, false, false, 0, null, null, 0L));
		AddColumnTypeMetadata(new ColumnTypeMetadata("SMALLINT", typeUshort, MySqlDbType.UInt16, true, false, 0, null, null, 0L));
		AddColumnTypeMetadata(new ColumnTypeMetadata("INT", typeInt, MySqlDbType.Int32, false, false, 0, null, null, 0L));
		AddColumnTypeMetadata(new ColumnTypeMetadata("INT", typeUint, MySqlDbType.UInt32, true, false, 0, null, null, 0L));
		AddColumnTypeMetadata(new ColumnTypeMetadata("MEDIUMINT", typeInt, MySqlDbType.Int24, false, false, 0, null, null, 0L));
		AddColumnTypeMetadata(new ColumnTypeMetadata("MEDIUMINT", typeUint, MySqlDbType.UInt24, true, false, 0, null, null, 0L));
		AddColumnTypeMetadata(new ColumnTypeMetadata("BIGINT", typeLong, MySqlDbType.Int64, false, false, 0, null, null, 0L));
		AddColumnTypeMetadata(new ColumnTypeMetadata("BIGINT", typeUlong, MySqlDbType.UInt64, true, false, 0, null, null, 0L));
		AddColumnTypeMetadata(new ColumnTypeMetadata("BIT", typeUlong, MySqlDbType.Bit, false, false, 0, null, null, 0L));
		DbTypeMapping typeDecimal = AddDbTypeMapping(new DbTypeMapping(typeof(decimal), new DbType[3]
		{
			DbType.Decimal,
			DbType.Currency,
			DbType.VarNumeric
		}, (object o) => Convert.ToDecimal(o)));
		DbTypeMapping typeDouble = AddDbTypeMapping(new DbTypeMapping(typeof(double), new DbType[1]
		{
			DbType.Double
		}, (object o) => Convert.ToDouble(o)));
		DbTypeMapping typeFloat = AddDbTypeMapping(new DbTypeMapping(typeof(float), new DbType[1]
		{
			DbType.Single
		}, (object o) => Convert.ToSingle(o)));
		AddColumnTypeMetadata(new ColumnTypeMetadata("DECIMAL", typeDecimal, MySqlDbType.NewDecimal, false, false, 0, null, "DECIMAL({0},{1});precision,scale", 0L));
		AddColumnTypeMetadata(new ColumnTypeMetadata("DECIMAL", typeDecimal, MySqlDbType.Decimal, false, false, 0, null, null, 0L));
		AddColumnTypeMetadata(new ColumnTypeMetadata("DOUBLE", typeDouble, MySqlDbType.Double, false, false, 0, null, null, 0L));
		AddColumnTypeMetadata(new ColumnTypeMetadata("FLOAT", typeFloat, MySqlDbType.Float, false, false, 0, null, null, 0L));
		DbTypeMapping typeFixedString = AddDbTypeMapping(new DbTypeMapping(typeof(string), new DbType[2]
		{
			DbType.StringFixedLength,
			DbType.AnsiStringFixedLength
		}, Convert.ToString));
		DbTypeMapping typeString = AddDbTypeMapping(new DbTypeMapping(typeof(string), new DbType[3]
		{
			DbType.String,
			DbType.AnsiString,
			DbType.Xml
		}, Convert.ToString));
		AddColumnTypeMetadata(new ColumnTypeMetadata("VARCHAR", typeString, MySqlDbType.VarChar, false, false, 0, null, "VARCHAR({0});size", 0L));
		AddColumnTypeMetadata(new ColumnTypeMetadata("VARCHAR", typeString, MySqlDbType.VarString, false, false, 0, null, null, 0L));
		AddColumnTypeMetadata(new ColumnTypeMetadata("CHAR", typeFixedString, MySqlDbType.String, false, false, 0, null, "CHAR({0});size", 0L));
		AddColumnTypeMetadata(new ColumnTypeMetadata("TINYTEXT", typeString, MySqlDbType.TinyText, false, false, 0, "VARCHAR", null, 255L));
		AddColumnTypeMetadata(new ColumnTypeMetadata("TEXT", typeString, MySqlDbType.Text, false, false, 0, "VARCHAR", null, 65535L));
		AddColumnTypeMetadata(new ColumnTypeMetadata("MEDIUMTEXT", typeString, MySqlDbType.MediumText, false, false, 0, "VARCHAR", null, 16777215L));
		AddColumnTypeMetadata(new ColumnTypeMetadata("LONGTEXT", typeString, MySqlDbType.LongText, false, false, 0, "VARCHAR", null, 4294967295L));
		AddColumnTypeMetadata(new ColumnTypeMetadata("ENUM", typeString, MySqlDbType.Enum, false, false, 0, null, null, 0L));
		AddColumnTypeMetadata(new ColumnTypeMetadata("SET", typeString, MySqlDbType.Set, false, false, 0, null, null, 0L));
		AddColumnTypeMetadata(new ColumnTypeMetadata("JSON", typeString, MySqlDbType.JSON, false, false, 0, null, null, 0L));
		DbTypeMapping typeBinary = AddDbTypeMapping(new DbTypeMapping(typeof(byte[]), new DbType[1]
		{
			DbType.Binary
		}));
		AddColumnTypeMetadata(new ColumnTypeMetadata("BLOB", typeBinary, MySqlDbType.Blob, false, true, 0, "BLOB", null, 65535L));
		AddColumnTypeMetadata(new ColumnTypeMetadata("BINARY", typeBinary, MySqlDbType.Binary, false, true, 0, "BLOB", "BINARY({0});length", 0L));
		AddColumnTypeMetadata(new ColumnTypeMetadata("VARBINARY", typeBinary, MySqlDbType.VarBinary, false, true, 0, "BLOB", "VARBINARY({0});length", 0L));
		AddColumnTypeMetadata(new ColumnTypeMetadata("TINYBLOB", typeBinary, MySqlDbType.TinyBlob, false, true, 0, "BLOB", null, 255L));
		AddColumnTypeMetadata(new ColumnTypeMetadata("MEDIUMBLOB", typeBinary, MySqlDbType.MediumBlob, false, true, 0, "BLOB", null, 16777215L));
		AddColumnTypeMetadata(new ColumnTypeMetadata("LONGBLOB", typeBinary, MySqlDbType.LongBlob, false, true, 0, "BLOB", null, 4294967295L));
		AddColumnTypeMetadata(new ColumnTypeMetadata("GEOMETRY", typeBinary, MySqlDbType.Geometry, false, true, 0, null, null, 0L));
		AddColumnTypeMetadata(new ColumnTypeMetadata("POINT", typeBinary, MySqlDbType.Geometry, false, true, 0, null, null, 0L));
		AddColumnTypeMetadata(new ColumnTypeMetadata("LINESTRING", typeBinary, MySqlDbType.Geometry, false, true, 0, null, null, 0L));
		AddColumnTypeMetadata(new ColumnTypeMetadata("POLYGON", typeBinary, MySqlDbType.Geometry, false, true, 0, null, null, 0L));
		AddColumnTypeMetadata(new ColumnTypeMetadata("MULTIPOINT", typeBinary, MySqlDbType.Geometry, false, true, 0, null, null, 0L));
		AddColumnTypeMetadata(new ColumnTypeMetadata("MULTILINESTRING", typeBinary, MySqlDbType.Geometry, false, true, 0, null, null, 0L));
		AddColumnTypeMetadata(new ColumnTypeMetadata("MULTIPOLYGON", typeBinary, MySqlDbType.Geometry, false, true, 0, null, null, 0L));
		AddColumnTypeMetadata(new ColumnTypeMetadata("GEOMETRYCOLLECTION", typeBinary, MySqlDbType.Geometry, false, true, 0, null, null, 0L));
		DbTypeMapping typeDate = AddDbTypeMapping(new DbTypeMapping(typeof(DateTime), new DbType[1]
		{
			DbType.Date
		}));
		DbTypeMapping typeDateTime = AddDbTypeMapping(new DbTypeMapping(typeof(DateTime), new DbType[3]
		{
			DbType.DateTime,
			DbType.DateTime2,
			DbType.DateTimeOffset
		}));
		AddDbTypeMapping(new DbTypeMapping(typeof(DateTimeOffset), new DbType[1]
		{
			DbType.DateTimeOffset
		}));
		DbTypeMapping typeTime = AddDbTypeMapping(new DbTypeMapping(typeof(TimeSpan), new DbType[1]
		{
			DbType.Time
		}));
		AddColumnTypeMetadata(new ColumnTypeMetadata("DATETIME", typeDateTime, MySqlDbType.DateTime, false, false, 0, null, null, 0L));
		AddColumnTypeMetadata(new ColumnTypeMetadata("DATE", typeDate, MySqlDbType.Date, false, false, 0, null, null, 0L));
		AddColumnTypeMetadata(new ColumnTypeMetadata("DATE", typeDate, MySqlDbType.Newdate, false, false, 0, null, null, 0L));
		AddColumnTypeMetadata(new ColumnTypeMetadata("TIME", typeTime, MySqlDbType.Time, false, false, 0, null, null, 0L));
		AddColumnTypeMetadata(new ColumnTypeMetadata("TIMESTAMP", typeDateTime, MySqlDbType.Timestamp, false, false, 0, null, null, 0L));
		AddColumnTypeMetadata(new ColumnTypeMetadata("YEAR", typeInt, MySqlDbType.Year, false, false, 0, null, null, 0L));
		DbTypeMapping typeGuid = AddDbTypeMapping(new DbTypeMapping(typeof(Guid), new DbType[1]
		{
			DbType.Guid
		}, (object o) => Guid.Parse(Convert.ToString(o))));
		AddColumnTypeMetadata(new ColumnTypeMetadata("CHAR", typeGuid, MySqlDbType.Guid, false, false, 36, "CHAR(36)", "CHAR(36)", 0L));
		DbTypeMapping typeNull = AddDbTypeMapping(new DbTypeMapping(typeof(object), new DbType[1]
		{
			DbType.Object
		}, (object o) => null));
		AddColumnTypeMetadata(new ColumnTypeMetadata("NULL", typeNull, MySqlDbType.Null, false, false, 0, null, null, 0L));
	}

	public IReadOnlyList<ColumnTypeMetadata> GetColumnTypeMetadata()
	{
		return m_columnTypeMetadata.AsReadOnly();
	}

	public ColumnTypeMetadata GetColumnTypeMetadata(MySqlDbType mySqlDbType)
	{
		return m_mySqlDbTypeToColumnTypeMetadata[mySqlDbType];
	}

	public DbType GetDbTypeForMySqlDbType(MySqlDbType mySqlDbType)
	{
		return m_mySqlDbTypeToColumnTypeMetadata[mySqlDbType].DbTypeMapping.DbTypes[0];
	}

	public MySqlDbType GetMySqlDbTypeForDbType(DbType dbType)
	{
		foreach (KeyValuePair<MySqlDbType, ColumnTypeMetadata> pair in m_mySqlDbTypeToColumnTypeMetadata)
		{
			if (pair.Value.DbTypeMapping.DbTypes.Contains(dbType))
			{
				return pair.Key;
			}
		}
		return MySqlDbType.VarChar;
	}

	private DbTypeMapping AddDbTypeMapping(DbTypeMapping dbTypeMapping)
	{
		m_dbTypeMappingsByClrType[dbTypeMapping.ClrType] = dbTypeMapping;
		if (dbTypeMapping.DbTypes != null)
		{
			DbType[] dbTypes = dbTypeMapping.DbTypes;
			foreach (DbType dbType in dbTypes)
			{
				m_dbTypeMappingsByDbType[dbType] = dbTypeMapping;
			}
		}
		return dbTypeMapping;
	}

	private void AddColumnTypeMetadata(ColumnTypeMetadata columnTypeMetadata)
	{
		m_columnTypeMetadata.Add(columnTypeMetadata);
		string lookupKey = columnTypeMetadata.CreateLookupKey();
		if (!m_columnTypeMetadataLookup.ContainsKey(lookupKey))
		{
			m_columnTypeMetadataLookup.Add(lookupKey, columnTypeMetadata);
		}
		if (!m_mySqlDbTypeToColumnTypeMetadata.ContainsKey(columnTypeMetadata.MySqlDbType))
		{
			m_mySqlDbTypeToColumnTypeMetadata.Add(columnTypeMetadata.MySqlDbType, columnTypeMetadata);
		}
	}

	internal DbTypeMapping GetDbTypeMapping(Type clrType)
	{
		DbTypeMapping dbTypeMapping;
		m_dbTypeMappingsByClrType.TryGetValue(clrType, out dbTypeMapping);
		return dbTypeMapping;
	}

	internal DbTypeMapping GetDbTypeMapping(DbType dbType)
	{
		DbTypeMapping dbTypeMapping;
		m_dbTypeMappingsByDbType.TryGetValue(dbType, out dbTypeMapping);
		return dbTypeMapping;
	}

	public DbTypeMapping GetDbTypeMapping(string columnTypeName, bool unsigned = false, int length = 0)
	{
		ColumnTypeMetadata columnTypeMetadata = GetColumnTypeMetadata(columnTypeName, unsigned, length);
		if (columnTypeMetadata == null)
		{
			return null;
		}
		return columnTypeMetadata.DbTypeMapping;
	}

	public MySqlDbType GetMySqlDbType(string typeName, bool unsigned)
	{
		return GetColumnTypeMetadata(typeName, unsigned, 0).MySqlDbType;
	}

	private ColumnTypeMetadata GetColumnTypeMetadata(string columnTypeName, bool unsigned, int length)
	{
		ColumnTypeMetadata columnTypeMetadata;
		if (!m_columnTypeMetadataLookup.TryGetValue(ColumnTypeMetadata.CreateLookupKey(columnTypeName, unsigned, length), out columnTypeMetadata) && length != 0)
		{
			m_columnTypeMetadataLookup.TryGetValue(ColumnTypeMetadata.CreateLookupKey(columnTypeName, unsigned, 0), out columnTypeMetadata);
		}
		return columnTypeMetadata;
	}

	public static MySqlDbType ConvertToMySqlDbType(ColumnDefinitionPayload columnDefinition, bool treatTinyAsBoolean, MySqlGuidFormat guidFormat)
	{
		bool isUnsigned = (columnDefinition.ColumnFlags & ColumnFlags.Unsigned) != (ColumnFlags)0;
		switch (columnDefinition.ColumnType)
		{
		case ColumnType.Tiny:
			if (!treatTinyAsBoolean || columnDefinition.ColumnLength != 1 || isUnsigned)
			{
				if (!isUnsigned)
				{
					return MySqlDbType.Byte;
				}
				return MySqlDbType.UByte;
			}
			return MySqlDbType.Bool;
		case ColumnType.Int24:
			if (!isUnsigned)
			{
				return MySqlDbType.Int24;
			}
			return MySqlDbType.UInt24;
		case ColumnType.Long:
			if (!isUnsigned)
			{
				return MySqlDbType.Int32;
			}
			return MySqlDbType.UInt32;
		case ColumnType.Longlong:
			if (!isUnsigned)
			{
				return MySqlDbType.Int64;
			}
			return MySqlDbType.UInt64;
		case ColumnType.Bit:
			return MySqlDbType.Bit;
		case ColumnType.String:
			if (guidFormat == MySqlGuidFormat.Char36 && (long)columnDefinition.ColumnLength / (long)ProtocolUtility.GetBytesPerCharacter(columnDefinition.CharacterSet) == 36)
			{
				return MySqlDbType.Guid;
			}
			if (guidFormat == MySqlGuidFormat.Char32 && (long)columnDefinition.ColumnLength / (long)ProtocolUtility.GetBytesPerCharacter(columnDefinition.CharacterSet) == 32)
			{
				return MySqlDbType.Guid;
			}
			if ((columnDefinition.ColumnFlags & ColumnFlags.Enum) != 0)
			{
				return MySqlDbType.Enum;
			}
			if ((columnDefinition.ColumnFlags & ColumnFlags.Set) != 0)
			{
				return MySqlDbType.Set;
			}
			goto case ColumnType.TinyBlob;
		case ColumnType.TinyBlob:
		case ColumnType.MediumBlob:
		case ColumnType.LongBlob:
		case ColumnType.Blob:
		case ColumnType.VarString:
		{
			ColumnType type = columnDefinition.ColumnType;
			if (columnDefinition.CharacterSet == CharacterSet.Binary)
			{
				if ((guidFormat == MySqlGuidFormat.Binary16 || guidFormat == MySqlGuidFormat.TimeSwapBinary16 || guidFormat == MySqlGuidFormat.LittleEndianBinary16) && columnDefinition.ColumnLength == 16)
				{
					return MySqlDbType.Guid;
				}
				switch (type)
				{
				default:
					return MySqlDbType.LongBlob;
				case ColumnType.MediumBlob:
					return MySqlDbType.MediumBlob;
				case ColumnType.Blob:
					return MySqlDbType.Blob;
				case ColumnType.TinyBlob:
					return MySqlDbType.TinyBlob;
				case ColumnType.VarString:
					return MySqlDbType.VarBinary;
				case ColumnType.String:
					return MySqlDbType.Binary;
				}
			}
			switch (type)
			{
			default:
				return MySqlDbType.LongText;
			case ColumnType.MediumBlob:
				return MySqlDbType.MediumText;
			case ColumnType.Blob:
				return MySqlDbType.Text;
			case ColumnType.TinyBlob:
				return MySqlDbType.TinyText;
			case ColumnType.VarString:
				return MySqlDbType.VarChar;
			case ColumnType.String:
				return MySqlDbType.String;
			}
		}
		case ColumnType.Json:
			return MySqlDbType.JSON;
		case ColumnType.Short:
			if (!isUnsigned)
			{
				return MySqlDbType.Int16;
			}
			return MySqlDbType.UInt16;
		case ColumnType.Date:
			return MySqlDbType.Date;
		case ColumnType.DateTime:
			return MySqlDbType.DateTime;
		case ColumnType.Timestamp:
			return MySqlDbType.Timestamp;
		case ColumnType.Time:
			return MySqlDbType.Time;
		case ColumnType.Year:
			return MySqlDbType.Year;
		case ColumnType.Float:
			return MySqlDbType.Float;
		case ColumnType.Double:
			return MySqlDbType.Double;
		case ColumnType.Decimal:
			return MySqlDbType.Decimal;
		case ColumnType.NewDecimal:
			return MySqlDbType.NewDecimal;
		case ColumnType.Geometry:
			return MySqlDbType.Geometry;
		case ColumnType.Null:
			return MySqlDbType.Null;
		default:
			throw new NotImplementedException("ConvertToMySqlDbType for {0} is not implemented".FormatInvariant(columnDefinition.ColumnType));
		}
	}

	public static ushort ConvertToColumnTypeAndFlags(MySqlDbType dbType, MySqlGuidFormat guidFormat)
	{
		bool isUnsigned = false;
		ColumnType columnType;
		switch (dbType)
		{
		case MySqlDbType.Bool:
		case MySqlDbType.Byte:
			columnType = ColumnType.Tiny;
			break;
		case MySqlDbType.UByte:
			columnType = ColumnType.Tiny;
			isUnsigned = true;
			break;
		case MySqlDbType.Int16:
			columnType = ColumnType.Short;
			break;
		case MySqlDbType.UInt16:
			columnType = ColumnType.Short;
			isUnsigned = true;
			break;
		case MySqlDbType.Int24:
			columnType = ColumnType.Int24;
			break;
		case MySqlDbType.UInt24:
			columnType = ColumnType.Int24;
			isUnsigned = true;
			break;
		case MySqlDbType.Int32:
			columnType = ColumnType.Long;
			break;
		case MySqlDbType.UInt32:
			columnType = ColumnType.Long;
			isUnsigned = true;
			break;
		case MySqlDbType.Int64:
			columnType = ColumnType.Longlong;
			break;
		case MySqlDbType.UInt64:
			columnType = ColumnType.Longlong;
			isUnsigned = true;
			break;
		case MySqlDbType.Bit:
			columnType = ColumnType.Bit;
			break;
		case MySqlDbType.Guid:
			columnType = ((guidFormat != MySqlGuidFormat.Char36 && guidFormat != MySqlGuidFormat.Char32) ? ColumnType.Blob : ColumnType.String);
			break;
		case MySqlDbType.Enum:
		case MySqlDbType.Set:
			columnType = ColumnType.String;
			break;
		case MySqlDbType.String:
		case MySqlDbType.Binary:
			columnType = ColumnType.String;
			break;
		case MySqlDbType.VarString:
		case MySqlDbType.VarChar:
		case MySqlDbType.VarBinary:
			columnType = ColumnType.VarString;
			break;
		case MySqlDbType.TinyBlob:
		case MySqlDbType.TinyText:
			columnType = ColumnType.TinyBlob;
			break;
		case MySqlDbType.Blob:
		case MySqlDbType.Text:
			columnType = ColumnType.Blob;
			break;
		case MySqlDbType.MediumBlob:
		case MySqlDbType.MediumText:
			columnType = ColumnType.MediumBlob;
			break;
		case MySqlDbType.LongBlob:
		case MySqlDbType.LongText:
			columnType = ColumnType.LongBlob;
			break;
		case MySqlDbType.JSON:
			columnType = ColumnType.Json;
			break;
		case MySqlDbType.Date:
		case MySqlDbType.Newdate:
			columnType = ColumnType.Date;
			break;
		case MySqlDbType.DateTime:
			columnType = ColumnType.DateTime;
			break;
		case MySqlDbType.Timestamp:
			columnType = ColumnType.Timestamp;
			break;
		case MySqlDbType.Time:
			columnType = ColumnType.Time;
			break;
		case MySqlDbType.Year:
			columnType = ColumnType.Year;
			break;
		case MySqlDbType.Float:
			columnType = ColumnType.Float;
			break;
		case MySqlDbType.Double:
			columnType = ColumnType.Double;
			break;
		case MySqlDbType.Decimal:
			columnType = ColumnType.Decimal;
			break;
		case MySqlDbType.NewDecimal:
			columnType = ColumnType.NewDecimal;
			break;
		case MySqlDbType.Geometry:
			columnType = ColumnType.Geometry;
			break;
		default:
			throw new NotImplementedException("ConvertToColumnTypeAndFlags for {0} is not implemented".FormatInvariant(dbType));
		}
		return (ushort)((byte)columnType | (isUnsigned ? 32768 : 0));
	}

	internal IEnumerable<ColumnTypeMetadata> GetColumnMappings()
	{
		return m_columnTypeMetadataLookup.Values.AsEnumerable();
	}
}
