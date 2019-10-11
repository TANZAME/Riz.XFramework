
--CREATE SEQUENCE SYS_DEMO_DEMOID_SEQ --创建序列名称
--INCREMENT BY 1                      --增长幅度
--START WITH 1                        --初始值
--MAXVALUE 9999999999999999;          --最大值

call spSys_DropIfExists('Bas_Client');
CREATE TABLE Bas_Client(
  ClientId int PRIMARY KEY NOT NULL,
  ClientCode nvarchar2(200) NOT NULL,
  ClientName nvarchar2(200) NULL,
  CloudServerId int DEFAULT 0 NOT NULL ,
  ActiveDate date NULL,
  Qty int default 0 not null ,
  State number(3,0) NOT NULL,
	Remark nvarchar2(250) DEFAULT N'默认值' NULL 
);
--alter table bas_Client modify cloudserverid default 0;
--alter table bas_Client modify state default 0;

call spSys_DropIfExists('Bas_ClientAccount');
create table Bas_ClientAccount(
  ClientId int NOT NULL,
  AccountId nvarchar2(100) NOT NULL,
  AccountCode nvarchar2(200) NOT NULL,
  AccountName nvarchar2(200) NOT NULL,
	Qty int default 0 not null ,
  primary key(ClientId,AccountId)
);

call spSys_DropIfExists('Bas_ClientAccountMarket');
create table Bas_ClientAccountMarket(
  ClientId int NOT NULL,
  AccountId nvarchar2(100) NOT NULL,
  MarketId int NOT NULL,
  MarketCode nvarchar2(50) NOT NULL,
  MarketName nvarchar2(50) NOT NULL,
  primary key(ClientId,AccountId,MarketId)
);

call spSys_DropIfExists('Sys_CloudServer');
create table Sys_CloudServer(
	CloudServerId int PRIMARY KEY NOT NULL,
	CloudServerCode nvarchar2(50) NOT NULL,
	CloudServerName nvarchar2(50) NOT NULL
);

call spSys_DropIfExists('Sys_Demo');
CREATE TABLE Sys_Demo(
  DemoId int PRIMARY KEY NOT NULL,
  DemoCode varchar2(32) NULL,
  DemoName nvarchar2(32) NOT NULL,
  DemoBoolean number(1,0) NOT NULL,             -- Boolean
  DemoBoolean_Nullable number(1,0) NULL,
  DemoChar char(1) NOT NULL,
	DemoNChar nchar(1) NOT NULL,
  DemoChar_Nullable char(1) NULL,
  DemoByte number(3,0) NOT NULL,
  DemoByte_Nullable number(3,0) NULL,
  DemoDate date NOT NULL,
  DemoDate_Nullable date NULL,
  DemoDateTime timestamp NOT NULL,
  DemoDateTime_Nullable timestamp NULL,
  DemoDateTime2 timestamp(9) NOT NULL,
  DemoDateTime2_Nullable timestamp(9) NULL,
  DemoDecimal decimal(18, 2) NOT NULL,
  DemoDecimal_Nullable decimal(18, 2) NULL,
  DemoDouble binary_double NOT NULL,     -- 双精度
  DemoDouble_Nullable binary_double NULL,-- 双精度
  DemoFloat binary_float NOT NULL,       -- 单精度
  DemoFloat_Nullable binary_float NULL,  -- 单精度
  DemoGuid raw(16) NOT NULL,
  DemoGuid_Nullable raw(16) NULL,
  DemoShort smallint NOT NULL,
  DemoShort_Nullable smallint NULL,      --number(5,0)
  DemoInt int NOT NULL,
  DemoInt_Nullable int NULL,             --number(10,0)
  DemoLong number(19,0) NOT NULL,        --number(19,0)
  DemoLong_Nullable number(19,0) NULL,
  
  DemoText_Nullable NCLOB NULL,
	DemoNText_Nullable CLOB NULL,
	--DemoTime_Nullable timestamp NULL,      --INTERVAL DAY(2) TO SECOND(6) NULL,
	DemoTime_Nullable INTERVAL DAY(2) TO SECOND(6) NULL,
	DemoDatetimeOffset_Nullable timestamp(4) WITH TIME ZONE NULL,
	DemoBinary_Nullable BLOB NULL,
	DemVarBinary_Nullable BLOB NULL,
	DemoTimestamp_Nullable  timestamp(4) WITH LOCAL TIME ZONE NULL
);

call spSys_DropIfExists('Sys_Rabbit');
CREATE TABLE Sys_Rabbit(
  DemoId int PRIMARY KEY NOT NULL,
  DemoCode varchar(32) NULL,
  DemoName nvarchar2(32) NOT NULL,
  DemoBoolean number(1,0) NOT NULL,             -- Boolean
  DemoBoolean_Nullable number(1,0) NULL,
  DemoChar char(2) NOT NULL,
  DemoNChar nchar(2) NOT NULL,
  DemoChar_Nullable char(2) NULL,
  DemoByte number(3,0) NOT NULL,
  DemoByte_Nullable number(3,0) NULL,
  DemoDate date NOT NULL,
  DemoDate_Nullable date NULL,
  DemoDateTime timestamp NOT NULL,
  DemoDateTime_Nullable timestamp NULL,
  DemoDateTime2 timestamp(9) NOT NULL,
  DemoDateTime2_Nullable timestamp(9) NULL,
  DemoDecimal decimal(18, 2) NOT NULL,
  DemoDecimal_Nullable decimal(18, 2) NULL,
  DemoDouble binary_double NOT NULL,     -- 双精度
  DemoDouble_Nullable binary_double NULL,-- 双精度
  DemoFloat binary_float NOT NULL,       -- 单精度
  DemoFloat_Nullable binary_float NULL,  -- 单精度
  DemoGuid raw(16) NOT NULL,
  DemoGuid_Nullable raw(16) NULL,
  DemoShort smallint NOT NULL,
  DemoShort_Nullable smallint NULL,      --number(5,0)
  DemoInt int NOT NULL,
  DemoInt_Nullable int NULL,             --number(10,0)
  DemoLong number(19,0) NOT NULL,       --number(19,0)
  DemoLong_Nullable number(19,0) NULL
);

DROP SEQUENCE SYS_DEMO_DEMOID_SEQ;
CREATE SEQUENCE SYS_DEMO_DEMOID_SEQ INCREMENT BY 1;
COMMIT;

DECLARE rowIndex INT;
        rowCount2 INT;
BEGIN
    rowIndex := 1;
    rowCount2 := 1000000;
    
    Delete FROM Sys_CloudServer;
    Insert Into Sys_CloudServer (CloudServerId,CloudServerCode,CloudServerName) Values (1,'0181',N'181服务器');
    Insert Into Sys_CloudServer (CloudServerId,CloudServerCode,CloudServerName) Values (2,'0182',N'182服务器');
    Insert Into Sys_CloudServer (CloudServerId,CloudServerCode,CloudServerName) Values (3,'0183',N'183服务器');    
    
    DELETE FROM Sys_Demo;
    DELETE FROM Bas_Client;
    DELETE FROM Bas_ClientAccount;
    DELETE FROM Bas_ClientAccountMarket;
    DELETE FROM Sys_Rabbit;
  
    WHILE(rowIndex <= rowCount2) LOOP
      IF rowIndex <= 100 THEN
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
          VALUES(SYS_DEMO_DEMOID_SEQ.nextval
              , 'Code' || RPAD('',LENGTH(TO_CHAR(rowCount2)) - LENGTH(TO_CHAR(rowIndex)),'0') || TO_CHAR(rowIndex)
              ,N'名称' || RPAD('',LENGTH(TO_CHAR(rowCount2)) - LENGTH(TO_CHAR(rowIndex)),'0') || TO_CHAR(rowIndex)
              ,CASE WHEN mod(rowIndex,2) = 0 THEN 1 ELSE 0 END
              ,CASE WHEN mod(rowIndex,2) = 0 THEN 1 ELSE NULL END
              ,CASE WHEN mod(rowIndex,2) = 0 THEN 'A' ELSE 'B' END
              ,CASE WHEN mod(rowIndex,2) = 0 THEN 'C' ELSE 'D' END
              ,CASE WHEN mod(rowIndex,2) = 0 THEN 'E' ELSE NULL END
              ,CASE WHEN rowIndex <=200 THEN 64 WHEN mod(rowIndex,2) = 0 THEN 127 ELSE 127 END
              ,CASE WHEN mod(rowIndex,2) = 0 THEN 127 ELSE NULL END
              ,CASE WHEN mod(rowIndex,2) = 0 THEN SYSDATE ELSE SYSDATE + 10 END
              ,CASE WHEN mod(rowIndex,2) = 0 THEN SYSDATE ELSE NULL END
              ,CASE WHEN mod(rowIndex,2) = 0 THEN SYSDATE ELSE SYSDATE + 10 END
              ,CASE WHEN mod(rowIndex,2) = 0 THEN SYSDATE ELSE NULL END
              ,CASE WHEN mod(rowIndex,2) = 0 THEN SYSDATE ELSE SYSDATE + 10 END
              ,CASE WHEN mod(rowIndex,2) = 0 THEN SYSDATE ELSE NULL END
              ,CASE WHEN mod(rowIndex,2) = 0 THEN 1024.99 ELSE 512.01 END
              ,CASE WHEN mod(rowIndex,2) = 0 THEN 1024.99 ELSE NULL END
              ,CASE WHEN mod(rowIndex,2) = 0 THEN 2048.123456789 ELSE 1024.987654321 END
              ,CASE WHEN mod(rowIndex,2) = 0 THEN 2048.123456789 ELSE NULL END
              ,CASE WHEN mod(rowIndex,2) = 0 THEN 4096.123456789 ELSE 4096.987654321 END
              ,CASE WHEN mod(rowIndex,2) = 0 THEN 4096.123456789 ELSE NULL END
              ,SYS_GUID
              ,CASE WHEN mod(rowIndex,2) = 0 THEN SYS_GUID ELSE NULL END
              ,CASE WHEN mod(rowIndex,2) = 0 THEN 8192 ELSE 4096 END
              ,CASE WHEN mod(rowIndex,2) = 0 THEN 8192 ELSE NULL END
              ,CASE WHEN mod(rowIndex,2) = 0 THEN 819200000 ELSE 409600000 END
              ,CASE WHEN mod(rowIndex,2) = 0 THEN 819200000 ELSE NULL END
              ,CASE WHEN mod(rowIndex,2) = 0 THEN 8192000000000 ELSE 4096000000000 END
              ,CASE WHEN mod(rowIndex,2) = 0 THEN 8192000000000 ELSE NULL END
      );
      END IF;
        
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
      VALUES
          (SYS_DEMO_DEMOID_SEQ.nextval
          , 'Code' || RPAD('',LENGTH(TO_CHAR(rowCount2)) - LENGTH(TO_CHAR(rowIndex)),'0') || TO_CHAR(rowIndex)
          ,N'名称' || RPAD('',LENGTH(TO_CHAR(rowCount2)) - LENGTH(TO_CHAR(rowIndex)),'0') || TO_CHAR(rowIndex)
          ,CASE WHEN mod(rowIndex,2) = 0 THEN 1 ELSE 0 END
          ,CASE WHEN mod(rowIndex,2) = 0 THEN 1 ELSE NULL END
          ,CASE WHEN mod(rowIndex,2) = 0 THEN 'CN' ELSE 'TW' END
              ,CASE WHEN mod(rowIndex,2) = 0 THEN 'US' ELSE 'MX' END
          ,CASE WHEN mod(rowIndex,2) = 0 THEN 'CN' ELSE NULL END
          ,CASE WHEN rowIndex <=200 THEN 64 WHEN mod(rowIndex,2) = 0 THEN 127 ELSE 127 END
          ,CASE WHEN mod(rowIndex,2) = 0 THEN 127 ELSE NULL END
          ,CASE WHEN mod(rowIndex,2) = 0 THEN SYSDATE ELSE SYSDATE + 10 END
          ,CASE WHEN mod(rowIndex,2) = 0 THEN SYSDATE ELSE NULL END
          ,CASE WHEN mod(rowIndex,2) = 0 THEN SYSDATE ELSE SYSDATE + 10 END
          ,CASE WHEN mod(rowIndex,2) = 0 THEN SYSDATE ELSE NULL END
          ,CASE WHEN mod(rowIndex,2) = 0 THEN SYSDATE ELSE SYSDATE + 10 END
          ,CASE WHEN mod(rowIndex,2) = 0 THEN SYSDATE ELSE NULL END
          ,CASE WHEN mod(rowIndex,2) = 0 THEN 1024.99 ELSE 512.01 END
          ,CASE WHEN mod(rowIndex,2) = 0 THEN 1024.99 ELSE NULL END
          ,CASE WHEN mod(rowIndex,2) = 0 THEN 2048.123456789 ELSE 1024.987654321 END
          ,CASE WHEN mod(rowIndex,2) = 0 THEN 2048.123456789 ELSE NULL END
          ,CASE WHEN mod(rowIndex,2) = 0 THEN 4096.123456789 ELSE 4096.987654321 END
          ,CASE WHEN mod(rowIndex,2) = 0 THEN 4096.123456789 ELSE NULL END
          ,SYS_GUID
          ,CASE WHEN mod(rowIndex,2) = 0 THEN SYS_GUID ELSE NULL END
          ,CASE WHEN mod(rowIndex,2) = 0 THEN 8192 ELSE 4096 END
          ,CASE WHEN mod(rowIndex,2) = 0 THEN 8192 ELSE NULL END
          ,CASE WHEN mod(rowIndex,2) = 0 THEN 819200000 ELSE 409600000 END
          ,CASE WHEN mod(rowIndex,2) = 0 THEN 819200000 ELSE NULL END
          ,CASE WHEN mod(rowIndex,2) = 0 THEN 8192000000000 ELSE 4096000000000 END
          ,CASE WHEN mod(rowIndex,2) = 0 THEN 8192000000000 ELSE NULL END
      );        
      rowIndex:=rowIndex+1;
  End Loop;
  
   
  rowIndex := 1;
  WHILE (rowIndex <= 2000) LOOP
    INSERT INTO Bas_Client
        (ClientId
        ,ClientCode
        ,ClientName
        ,CloudServerId
        ,ActiveDate
        ,State
        ,Remark)
      VALUES
        (rowIndex
        ,'XFramework' || TO_CHAR(rowIndex)
        ,'XFramework' || TO_CHAR(rowIndex)
        ,CASE WHEN mod(rowIndex,2) = 0 THEN 1 ELSE 3 END
        ,SYSDATE
        ,1
        ,'XFramework' || TO_CHAR(rowIndex));
      
    INSERT INTO Bas_ClientAccount
        (ClientId
        ,AccountId
        ,AccountCode
        ,AccountName
        ,Qty)
      VALUES
        (rowIndex
        ,1
        ,'XFramework' || TO_CHAR(rowIndex)
        ,'XFramework' || TO_CHAR(rowIndex)
        ,CASE WHEN mod(rowIndex,2) = 0 THEN 1 ELSE 2 END);

    INSERT INTO Bas_ClientAccount
        (ClientId
        ,AccountId
        ,AccountCode
        ,AccountName
        ,Qty)
      VALUES
        (rowIndex
        ,2
        ,'XFramework' || TO_CHAR(rowIndex)
        ,'XFramework' || TO_CHAR(rowIndex)
        ,CASE WHEN mod(rowIndex,2) = 0 THEN 1 ELSE 2 END);
               
    INSERT INTO Bas_ClientAccountMarket
        (ClientId
        ,AccountId
        ,MarketId
        ,MarketCode
        ,MarketName)
      VALUES
        (rowIndex
        ,1
        ,1
        ,'XFramework' || TO_CHAR(rowIndex)
        ,'XFramework' || TO_CHAR(rowIndex));
               
    INSERT INTO Bas_ClientAccountMarket
        (ClientId
        ,AccountId
        ,MarketId
        ,MarketCode
        ,MarketName)
      VALUES
        (rowIndex
        ,1
        ,2
        ,'XFramework' || TO_CHAR(rowIndex)
        ,'XFramework' || TO_CHAR(rowIndex));

    INSERT INTO Bas_ClientAccountMarket
        (ClientId
        ,AccountId
        ,MarketId
        ,MarketCode
        ,MarketName)
      VALUES
        (rowIndex
        ,2
        ,1
        ,'XFramework' || TO_CHAR(rowIndex)
        ,'XFramework' || TO_CHAR(rowIndex));
               
    INSERT INTO Bas_ClientAccountMarket
        (ClientId
        ,AccountId
        ,MarketId
        ,MarketCode
        ,MarketName)
      VALUES
        (rowIndex
        ,2
        ,2
        ,'XFramework' || TO_CHAR(rowIndex)
        ,'XFramework' || TO_CHAR(rowIndex)); 
    rowIndex := rowIndex+1;
  END loop;
  
  COMMIT;
end;
