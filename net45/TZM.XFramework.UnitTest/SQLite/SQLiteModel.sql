
CREATE TABLE IF NOT EXISTS Bas_Client(
	ClientId int PRIMARY KEY NOT NULL,
	ClientCode varchar(200) NOT NULL,
	ClientName varchar(200) NULL,
	CloudServerId int NOT NULL DEFAULT 0,
	ActiveDate datetime NULL,
	Qty int not null default 0,
	State SMALLINT NOT NULL,
	Remark varchar(250) NULL DEFAULT '默认值'
);
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
);

CREATE TABLE IF NOT EXISTS Sys_Demo(
	DemoId integer PRIMARY KEY autoincrement NOT NULL,
	DemoCode varchar(32) NULL,
	DemoName varchar(32) NOT NULL,
	DemoBoolean BOOL NOT NULL,
	DemoBoolean_Nullable BOOL NULL,
	DemoChar char(1) NOT NULL,
	DemoNChar char(1) NOT NULL,
	DemoChar_Nullable char(1) NULL,
	DemoByte smallint NOT NULL,
	DemoByte_Nullable smallint NULL,
	DemoDate date NOT NULL,
	DemoDate_Nullable date NULL,
	DemoDateTime datetime NOT NULL,
	DemoDateTime_Nullable datetime NULL,
	DemoDateTime2 datetime(6) NOT NULL,
	DemoDateTime2_Nullable datetime(6) NULL,
	DemoDecimal decimal(18, 2) NOT NULL,
	DemoDecimal_Nullable decimal(18, 2) NULL,
	DemoDouble float NOT NULL,
	DemoDouble_Nullable float NULL,
	DemoFloat real NOT NULL,
	DemoFloat_Nullable real NULL,
	DemoGuid uuid NOT NULL,
	DemoGuid_Nullable uuid NULL,
	DemoShort smallint NOT NULL,
	DemoShort_Nullable smallint NULL,
	DemoInt int NOT NULL,
	DemoInt_Nullable int NULL,
	DemoLong bigint NOT NULL,
	DemoLong_Nullable bigint NULL,
	
	DemoText_Nullable text NULL,
	DemoNText_Nullable text NULL,
	DemoTime_Nullable time(2) NULL,			-- 不指定默认6位
	DemoDatetimeOffset_Nullable datetime(6) NULL,
	DemoBinary_Nullable blob NULL,
	DemVarBinary_Nullable blob NULL,
	DemoTimestamp_Nullable  datetime(6) NULL
);

CREATE TABLE IF NOT EXISTS Sys_Rabbit(
	DemoId integer PRIMARY KEY autoincrement NOT NULL,
	DemoCode varchar(32) NULL,
	DemoName varchar(32) NOT NULL,
	DemoBoolean BOOL NOT NULL,
	DemoBoolean_Nullable BOOL NULL,
	DemoChar char(1) NOT NULL,
	DemoNChar char(1) NOT NULL,
	DemoChar_Nullable char(1) NULL,
	DemoByte smallint NOT NULL,
	DemoByte_Nullable smallint NULL,
	DemoDate date NOT NULL,
	DemoDate_Nullable date NULL,
	DemoDateTime datetime NOT NULL,
	DemoDateTime_Nullable datetime NULL,
	DemoDateTime2 datetime(6) NOT NULL,
	DemoDateTime2_Nullable datetime(6) NULL,
	DemoDecimal decimal(18, 2) NOT NULL,
	DemoDecimal_Nullable decimal(18, 2) NULL,
	DemoDouble float NOT NULL,
	DemoDouble_Nullable float NULL,
	DemoFloat real NOT NULL,
	DemoFloat_Nullable real NULL,
	DemoGuid uuid NOT NULL,
	DemoGuid_Nullable uuid NULL,
	DemoShort smallint NOT NULL,
	DemoShort_Nullable smallint NULL,
	DemoInt int NOT NULL,
	DemoInt_Nullable int NULL,
	DemoLong bigint NOT NULL,
	DemoLong_Nullable bigint NULL
);