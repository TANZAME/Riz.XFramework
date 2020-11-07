
CREATE TABLE IF NOT EXISTS Bas_Client(
	ClientId int PRIMARY KEY NOT NULL,
	ClientCode varchar(200) NOT NULL,
	ClientName varchar(200) NULL,
	CloudServerId int NOT NULL DEFAULT 0,
	ActiveDate datetime NULL,
	Qty int not null default 0,
	State tinyint NOT NULL,
	Remark varchar(250) NULL DEFAULT '默认值'
);
#ALTER TABLE bas_Client ALTER COLUMN CloudServerId SET DEFAULT 0;
#ALTER TABLE bas_Client ALTER COLUMN `State` SET DEFAULT 0;

CREATE TABLE IF NOT EXISTS Bas_ClientAccount(
	ClientId int NOT NULL,
	AccountId varchar(100) NOT NULL,
	AccountCode varchar(200) NOT NULL,
	AccountName varchar(200) NOT NULL,
	Qty int not null default 0,
	PRIMARY KEY(ClientId,AccountId)
);

CREATE TABLE IF NOT EXISTS Bas_ClientAccountMarket(
	ClientId int NOT NULL,
	AccountId varchar(100) NOT NULL,
	MarketId int NOT NULL,
	MarketCode varchar(50) NOT NULL,
	MarketName varchar(50) NOT NULL,
	PRIMARY KEY(ClientId,AccountId,MarketId)
);

CREATE TABLE IF NOT EXISTS Sys_CloudServer(
	CloudServerId int PRIMARY KEY NOT NULL,
	CloudServerCode varchar(50) NOT NULL,
	CloudServerName varchar(50) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

CREATE TABLE IF NOT EXISTS Sys_Demo(
	DemoId int auto_increment PRIMARY KEY NOT NULL,
	DemoCode varchar(32) NULL,
	DemoName varchar(32) NOT NULL,
	DemoBoolean BOOL NOT NULL,			  # BIT(1)sys_cloudserver
	DemoBoolean_Nullable BOOL NULL,		  # BIT(1)
	DemoChar char(1) NOT NULL,
	DemoNChar char(1) NOT NULL,
	DemoChar_Nullable char(1) NULL,
	DemoByte tinyint unsigned NOT NULL,
	DemoByte_Nullable tinyint unsigned NULL,
	DemoDate date NOT NULL,
	DemoDate_Nullable date NULL,
	DemoDateTime datetime NOT NULL,
	DemoDateTime_Nullable datetime NULL,		# 不指定精度默认0
	DemoDateTime2 datetime(6) NOT NULL,			# mysql 没有datetime2 类型
	DemoDateTime2_Nullable datetime(6) NULL,
	DemoDecimal decimal(18, 2) NOT NULL,
	DemoDecimal_Nullable decimal(18, 2) NULL,
	DemoDouble float NOT NULL,
	DemoDouble_Nullable float NULL,
	DemoFloat real NOT NULL,
	DemoFloat_Nullable real NULL,
	DemoGuid CHAR(36) NOT NULL,
	DemoGuid_Nullable CHAR(36) NULL,
	DemoShort smallint NOT NULL,			# mysql guid 类型
	DemoShort_Nullable smallint NULL,
	DemoInt int NOT NULL,
	DemoInt_Nullable int NULL,
	DemoLong bigint NOT NULL,
	DemoLong_Nullable bigint NULL,

	DemoText_Nullable mediumtext NULL,
	DemoNText_Nullable longtext NULL,
	DemoTime_Nullable time(5) NULL,
	DemoDatetimeOffset_Nullable datetime NULL,
	DemoBinary_Nullable blob NULL,			# 64K?
	DemoVarBinary_Nullable longblob NULL,	# 4G?
	DemoTimestamp_Nullable  timestamp(6) NULL
);

CREATE TABLE IF NOT EXISTS Sys_Rabbit(
	DemoId int auto_increment PRIMARY KEY NOT NULL,
	DemoCode varchar(32) NULL,
	DemoName varchar(32) NOT NULL,
	DemoBoolean BOOL NOT NULL,
	DemoBoolean_Nullable BOOL NULL,
	DemoChar char(2) NOT NULL,
	DemoNChar char(2) NOT NULL,
	DemoChar_Nullable char(2) NULL,
	DemoByte tinyint unsigned NOT NULL,
	DemoByte_Nullable tinyint unsigned NULL,
	DemoDate date NOT NULL,
	DemoDate_Nullable date NULL,
	DemoDateTime datetime NOT NULL,
	DemoDateTime_Nullable datetime NULL,
	DemoDateTime2 datetime(6) NOT NULL,		# mysql 没有datetime2 类型
	DemoDateTime2_Nullable datetime(6) NULL,
	DemoDecimal decimal(18, 2) NOT NULL,
	DemoDecimal_Nullable decimal(18, 2) NULL,
	DemoDouble float NOT NULL,
	DemoDouble_Nullable float NULL,
	DemoFloat real NOT NULL,
	DemoFloat_Nullable real NULL,
	DemoGuid CHAR(36) NOT NULL,
	DemoGuid_Nullable CHAR(36) NULL,	# mysql guid 类型
	DemoShort smallint NOT NULL,			
	DemoShort_Nullable smallint NULL,
	DemoInt int NOT NULL,
	DemoInt_Nullable int NULL,
	DemoLong bigint NOT NULL,
	DemoLong_Nullable bigint NULL
);

set AUTOCOMMIT =0;
CALL `sySys_Initialize`();