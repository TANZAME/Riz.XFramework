
CREATE TABLE IF NOT EXISTS Bas_Client(
	ClientId int PRIMARY KEY NOT NULL,
	ClientCode varchar(200) NOT NULL,
	ClientName varchar(200) NULL,
	CloudServerId int NOT NULL DEFAULT 0,
	ActiveDate DATETIME(6) NULL,
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
-- 原始类型
-- 使用原始类型和使用.net常用类型建表，都能被 System.Data.SQLite.dll 正确映射
CREATE TABLE IF NOT EXISTS Sys_Demo(
	DemoId integer PRIMARY KEY autoincrement NOT NULL,
	DemoCode text NULL,
	DemoName text NOT NULL,
	DemoBoolean int NOT NULL,
	DemoBoolean_Nullable int NULL,
	DemoChar text NOT NULL,
	DemoNChar text NOT NULL,
	DemoChar_Nullable text NULL,
	DemoByte int NOT NULL,
	DemoByte_Nullable int NULL,
	DemoDate TEXT NOT NULL,
	DemoDate_Nullable TEXT NULL,
	DemoDateTime TEXT NOT NULL,
	DemoDateTime_Nullable TEXT NULL,
	DemoDateTime2 TEXT NOT NULL,
	DemoDateTime2_Nullable TEXT NULL,
	DemoDecimal NUMERIC NOT NULL,
	DemoDecimal_Nullable NUMERIC NULL,
	DemoDouble REAL NOT NULL,
	DemoDouble_Nullable real NULL,
	DemoFloat real NOT NULL,
	DemoFloat_Nullable real NULL,
	DemoGuid TEXT NOT NULL,
	DemoGuid_Nullable TEXT NULL,
	DemoShort integer NOT NULL,
	DemoShort_Nullable integer NULL,
	DemoInt int NOT NULL,
	DemoInt_Nullable int NULL,
	DemoLong integer NOT NULL,
	DemoLong_Nullable integer NULL,
	
	DemoText_Nullable text NULL,
	DemoNText_Nullable text NULL,
	DemoTime_Nullable text NULL,			-- 不指定默认6位
	DemoDatetimeOffset_Nullable text NULL,
	DemoBinary_Nullable blob NULL,
	DemVarBinary_Nullable blob NULL,
	DemoTimestamp_Nullable  text NULL
);

---- .net 类型
--CREATE TABLE IF NOT EXISTS Sys_Demo(
--	DemoId integer PRIMARY KEY autoincrement NOT NULL,
--	DemoCode varchar(32) NULL,
--	DemoName varchar(32) NOT NULL,
--	DemoBoolean BOOL NOT NULL,
--	DemoBoolean_Nullable BOOL NULL,
--	DemoChar char(1) NOT NULL,
--	DemoNChar char(1) NOT NULL,
--	DemoChar_Nullable char(1) NULL,
--	DemoByte smallint NOT NULL,
--	DemoByte_Nullable smallint NULL,
--	DemoDate date NOT NULL,
--	DemoDate_Nullable date NULL,
--	DemoDateTime datetime NOT NULL,
--	DemoDateTime_Nullable datetime NULL,
--	DemoDateTime2 datetime(6) NOT NULL,
--	DemoDateTime2_Nullable datetime(6) NULL,
--	DemoDecimal decimal(18, 2) NOT NULL,
--	DemoDecimal_Nullable decimal(18, 2) NULL,
--	DemoDouble float NOT NULL,
--	DemoDouble_Nullable float NULL,
--	DemoFloat real NOT NULL,
--	DemoFloat_Nullable real NULL,
--	DemoGuid uuid NOT NULL,
--	DemoGuid_Nullable uuid NULL,
--	DemoShort smallint NOT NULL,
--	DemoShort_Nullable smallint NULL,
--	DemoInt int NOT NULL,
--	DemoInt_Nullable int NULL,
--	DemoLong bigint NOT NULL,
--	DemoLong_Nullable bigint NULL,
	
--	DemoText_Nullable text NULL,
--	DemoNText_Nullable text NULL,
--	DemoTime_Nullable time(2) NULL,			-- 不指定默认6位
--	DemoDatetimeOffset_Nullable datetimeoffset(6) NULL,
--	DemoBinary_Nullable blob NULL,
--	DemVarBinary_Nullable blob NULL,
--	DemoTimestamp_Nullable  datetime(6) NULL
--);

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

begin transaction;
    Delete FROM Sys_CloudServer;
    Insert Into Sys_CloudServer (CloudServerId,CloudServerCode,CloudServerName) Values (1,'0181','181服务器');
    Insert Into Sys_CloudServer (CloudServerId,CloudServerCode,CloudServerName) Values (2,'0182','182服务器');
    Insert Into Sys_CloudServer (CloudServerId,CloudServerCode,CloudServerName) Values (3,'0183','183服务器');    
    
    DELETE FROM Sys_Demo;
    update sqlite_sequence set seq=0 where name='Sys_Demo';  
    DELETE FROM Bas_Client;
    DELETE FROM Bas_ClientAccount;
    DELETE FROM Bas_ClientAccountMarket;
    DELETE FROM Sys_Rabbit;
    
    INSERT INTO Sys_Demo
          (DemoId
          ,DemoCode
          ,DemoName
          ,DemoBoolean
          ,DemoBoolean_Nullable
          ,DemoChar
          ,DemoNChar
          ,DemoChar_Nullable
          ,DemoByte
          ,DemoByte_Nullable
          ,DemoDate
          ,DemoDate_Nullable
          ,DemoDateTime
          ,DemoDateTime_Nullable
          ,DemoDateTime2
          ,DemoDateTime2_Nullable
          ,DemoDecimal
          ,DemoDecimal_Nullable
          ,DemoDouble
          ,DemoDouble_Nullable
          ,DemoFloat
          ,DemoFloat_Nullable
          ,DemoGuid
          ,DemoGuid_Nullable
          ,DemoShort
          ,DemoShort_Nullable
          ,DemoInt
          ,DemoInt_Nullable
          ,DemoLong
          ,DemoLong_Nullable)
          VALUES(NULL
              ,'C0000009'
              ,'N0000001'
              ,1
              ,NULL
              ,'A'
              ,'B'
              ,NULL
              ,127
              ,NULL
              ,DATE('now')
              ,NULL
              ,DATETIME('now')
              ,NULL
              ,DATETIME('now')
              ,NULL
              ,1024.123456789
              ,NULL
              ,2048.123456789
              ,NULL
              ,4096.123456789
              ,NULL
              ,'67329bff-518e-4fa8-8ab8-8872fc401dcf'
              ,'67329bff-518e-4fa8-8ab8-8872fc401dcf'
              ,8192
              ,NULL
              ,819200000
              ,NULL
              ,8192000000000
              ,NULL
      );
      
    INSERT INTO Sys_Rabbit
          (DemoId
          ,DemoCode
          ,DemoName
          ,DemoBoolean
          ,DemoBoolean_Nullable
          ,DemoChar
          ,DemoNChar
          ,DemoChar_Nullable
          ,DemoByte
          ,DemoByte_Nullable
          ,DemoDate
          ,DemoDate_Nullable
          ,DemoDateTime
          ,DemoDateTime_Nullable
          ,DemoDateTime2
          ,DemoDateTime2_Nullable
          ,DemoDecimal
          ,DemoDecimal_Nullable
          ,DemoDouble
          ,DemoDouble_Nullable
          ,DemoFloat
          ,DemoFloat_Nullable
          ,DemoGuid
          ,DemoGuid_Nullable
          ,DemoShort
          ,DemoShort_Nullable
          ,DemoInt
          ,DemoInt_Nullable
          ,DemoLong
          ,DemoLong_Nullable)
          VALUES(NULL
              ,'C0000009'
              ,'N0000001'
              ,1
              ,NULL
              ,'A'
              ,'B'
              ,NULL
              ,127
              ,NULL
              ,DATE('now')
              ,NULL
              ,DATETIME('now')
              ,NULL
              ,DATETIME('now')
              ,NULL
              ,1024.123456789
              ,NULL
              ,2048.123456789
              ,NULL
              ,4096.123456789
              ,NULL
              ,'67329bff-518e-4fa8-8ab8-8872fc401dcf'
              ,'67329bff-518e-4fa8-8ab8-8872fc401dcf'
              ,8192
              ,NULL
              ,819200000
              ,NULL
              ,8192000000000
              ,NULL
      );
      
    INSERT INTO Bas_Client
        (ClientId
        ,ClientCode
        ,ClientName
        ,CloudServerId
        ,ActiveDate
        ,State
        ,Remark)
      VALUES
        (1
        ,'XFramework1'
        ,'XFramework1'
        ,1
        ,DATETIME('now')
        ,1
        ,'XFramework1');
        
    INSERT INTO Bas_ClientAccount
        (ClientId
        ,AccountId
        ,AccountCode
        ,AccountName
        ,Qty)
      VALUES
        (1
        ,1
        ,'XFramework1'
        ,'XFramework1'
        
        ,1);
    INSERT INTO Bas_ClientAccountMarket
        (ClientId
        ,AccountId
        ,MarketId
        ,MarketCode
        ,MarketName)
      VALUES
        (1
        ,1
        ,1
        ,'XFramework'
        ,'XFramework');
        
--
INSERT INTO Bas_Client
        (ClientId
        ,ClientCode
        ,ClientName
        ,CloudServerId
        ,ActiveDate
        ,State
        ,Remark)
      VALUES
        (90
        ,'XFramework1'
        ,'XFramework1'
        ,1
        ,DATETIME('now')
        ,1
        ,'XFramework1');
        
    INSERT INTO Bas_ClientAccount
        (ClientId
        ,AccountId
        ,AccountCode
        ,AccountName
        ,Qty)
      VALUES
        (90
        ,1
        ,'XFramework1'
        ,'XFramework1'
        
        ,1);
    INSERT INTO Bas_ClientAccountMarket
        (ClientId
        ,AccountId
        ,MarketId
        ,MarketCode
        ,MarketName)
      VALUES
        (90
        ,1
        ,1
        ,'XFramework'
        ,'XFramework');
        --
INSERT INTO Bas_Client
        (ClientId
        ,ClientCode
        ,ClientName
        ,CloudServerId
        ,ActiveDate
        ,State
        ,Remark)
      VALUES
        (91
        ,'XFramework1'
        ,'XFramework1'
        ,1
        ,DATETIME('now')
        ,1
        ,'XFramework1');
        
    INSERT INTO Bas_ClientAccount
        (ClientId
        ,AccountId
        ,AccountCode
        ,AccountName
        ,Qty)
      VALUES
        (91
        ,1
        ,'XFramework1'
        ,'XFramework1'
        
        ,1);
    INSERT INTO Bas_ClientAccountMarket
        (ClientId
        ,AccountId
        ,MarketId
        ,MarketCode
        ,MarketName)
      VALUES
        (91
        ,1
        ,1
        ,'XFramework'
        ,'XFramework');
        
commit;