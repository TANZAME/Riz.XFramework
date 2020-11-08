
CREATE TABLE IF NOT EXISTS Bas_Client(
	ClientId int PRIMARY KEY NOT NULL,
	ClientCode varchar(200) NOT NULL,
	ClientName varchar(200) NULL,
	CloudServerId int NOT NULL DEFAULT 0,
	ActiveDate timestamp(3) NULL,
	Qty int not null default 0,
	State SMALLINT NOT NULL,
	Remark national character varying(250) NULL DEFAULT '默认值'
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
	DemoId serial PRIMARY KEY NOT NULL,
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
	DemoDateTime timestamp NOT NULL,
	DemoDateTime_Nullable timestamp NULL,
	DemoDateTime2 timestamp(6) NOT NULL,
	DemoDateTime2_Nullable timestamp(6) NULL,
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
	DemoDatetimeOffset_Nullable timestamptz(6) NULL,
	DemoBinary_Nullable Bytea NULL,
	DemoVarBinary_Nullable Bytea NULL,
	DemoTimestamp_Nullable  timestamp NULL
);

CREATE TABLE IF NOT EXISTS Sys_Rabbit(
	DemoId serial PRIMARY KEY NOT NULL,
	DemoCode varchar(32) NULL,
	DemoName varchar(32) NOT NULL,
	DemoBoolean BOOL NOT NULL,
	DemoBoolean_Nullable BOOL NULL,
	DemoChar char(2) NOT NULL,
	DemoNChar char(2) NOT NULL,
	DemoChar_Nullable char(2) NULL,
	DemoByte smallint NOT NULL,
	DemoByte_Nullable smallint NULL,
	DemoDate date NOT NULL,
	DemoDate_Nullable date NULL,
	DemoDateTime timestamp(3) NOT NULL,
	DemoDateTime_Nullable timestamp(3) NULL,
	DemoDateTime2 timestamp(6) NOT NULL,
	DemoDateTime2_Nullable timestamp(6) NULL,
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


do $$
declare rowIndex int;
declare rowCount int;
BEGIN
    rowIndex := 1;
	rowCount := 1000000;
	
    Delete FROM Sys_CloudServer;
	Insert Into Sys_CloudServer (CloudServerId,CloudServerCode,CloudServerName) Values (1,'0181','181服务器');
	Insert Into Sys_CloudServer (CloudServerId,CloudServerCode,CloudServerName) Values (2,'0182','182服务器');
	Insert Into Sys_CloudServer (CloudServerId,CloudServerCode,CloudServerName) Values (3,'0183','183服务器');
    
    TRUNCATE TABLE Sys_Demo RESTART IDENTITY;
	TRUNCATE TABLE Bas_Client;
	TRUNCATE TABLE Bas_ClientAccount;
	TRUNCATE TABLE Bas_ClientAccountMarket;
	TRUNCATE TABLE Sys_Rabbit RESTART IDENTITY;
    
	WHILE(rowIndex <= rowCount) LOOP
		IF rowIndex <= 100 THEN
			INSERT INTO Sys_Demo
			   (DemoCode
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
			   ,DemoLong_Nullable 
				 ,demotime_nullable
			   ,demodatetimeoffset_nullable)
			 VALUES(
				    'C' || REPEAT('0',CHAR_LENGTH(CAST(rowCount AS varchar)) - CHAR_LENGTH(CAST(rowIndex AS varchar))) || CAST(rowIndex AS varchar)
				   ,'N'  || REPEAT('0',CHAR_LENGTH(CAST(rowCount AS varchar)) - CHAR_LENGTH(CAST(rowIndex AS varchar))) || CAST(rowIndex AS varchar)
				   ,CASE WHEN rowIndex % 2 = 0 THEN TRUE ELSE FALSE END
				   ,CASE WHEN rowIndex % 2 = 0 THEN TRUE ELSE NULL END
				   ,CASE WHEN rowIndex % 2 = 0 THEN 'A' ELSE 'B' END
				   ,CASE WHEN rowIndex % 2 = 0 THEN 'C' ELSE 'D' END
				   ,CASE WHEN rowIndex % 2 = 0 THEN 'E' ELSE NULL END
				   ,CASE WHEN rowIndex <=200 THEN 64 WHEN rowIndex % 2 = 0 THEN 127 ELSE 127 END
				   ,CASE WHEN rowIndex % 2 = 0 THEN 127 ELSE NULL END
				   ,CASE WHEN rowIndex % 2 = 0 THEN now() ELSE now() + INTERVAL '10 day' END
				   ,CASE WHEN rowIndex % 2 = 0 THEN now() ELSE NULL END
				   ,CASE WHEN rowIndex % 2 = 0 THEN now() ELSE now() + INTERVAL '10 day' END
				   ,CASE WHEN rowIndex % 2 = 0 THEN now() ELSE NULL END
				   ,CASE WHEN rowIndex % 2 = 0 THEN now() ELSE now() + INTERVAL '10 day' END
				   ,CASE WHEN rowIndex % 2 = 0 THEN now() ELSE NULL END
				   ,CASE WHEN rowIndex % 2 = 0 THEN 1024.99 ELSE 512.01 END
				   ,CASE WHEN rowIndex % 2 = 0 THEN 1024.99 ELSE NULL END
				   ,CASE WHEN rowIndex % 2 = 0 THEN 2048.123456789 ELSE 1024.987654321 END
				   ,CASE WHEN rowIndex % 2 = 0 THEN 2048.123456789 ELSE NULL END
				   ,CASE WHEN rowIndex % 2 = 0 THEN 4096.123456789 ELSE 4096.987654321 END
				   ,CASE WHEN rowIndex % 2 = 0 THEN 4096.123456789 ELSE NULL END
				   ,'0681757b-5f92-42c2-a4cd-90976f50225f'
				   ,'0681757b-5f92-42c2-a4cd-90976f50225f' --pg 的uuid列用不了case when表达式?? CASE WHEN rowIndex % 2 = 0 THEN '0681757b-5f92-42c2-a4cd-90976f50225f' ELSE NULL END
				   ,CASE WHEN rowIndex % 2 = 0 THEN 8192 ELSE 4096 END
				   ,CASE WHEN rowIndex % 2 = 0 THEN 8192 ELSE NULL END
				   ,CASE WHEN rowIndex % 2 = 0 THEN 819200000 ELSE 409600000 END
				   ,CASE WHEN rowIndex % 2 = 0 THEN 819200000 ELSE NULL END
				   ,CASE WHEN rowIndex % 2 = 0 THEN 8192000000000 ELSE 4096000000000 END
				   ,CASE WHEN rowIndex % 2 = 0 THEN 8192000000000 ELSE NULL END
					 ,(TIME '10:10:10.456789')
					 ,(TIMESTAMP WITH TIME ZONE '2019-10-28 16:16:32.444793+08')
			);
		END IF;
		
        INSERT INTO Sys_Rabbit
			   (DemoCode
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
				   ('C' || REPEAT('0',CHAR_LENGTH(CAST(rowCount AS varchar)) - CHAR_LENGTH(CAST(rowIndex AS varchar))) || CAST(rowIndex AS varchar)
				   ,'N'  || REPEAT('0',CHAR_LENGTH(CAST(rowCount AS varchar)) - CHAR_LENGTH(CAST(rowIndex AS varchar))) || CAST(rowIndex AS varchar)
				   ,CASE WHEN rowIndex % 2 = 0 THEN TRUE ELSE FALSE END
				   ,CASE WHEN rowIndex % 2 = 0 THEN TRUE ELSE NULL END
				   ,CASE WHEN rowIndex % 2 = 0 THEN 'CN' ELSE 'TW' END
				   ,CASE WHEN rowIndex % 2 = 0 THEN 'US' ELSE 'MX' END
				   ,CASE WHEN rowIndex % 2 = 0 THEN 'CN' ELSE NULL END
				   ,CASE WHEN rowIndex <=200 THEN 64 WHEN rowIndex % 2 = 0 THEN 127 ELSE 127 END
				   ,CASE WHEN rowIndex % 2 = 0 THEN 127 ELSE NULL END
				   ,CASE WHEN rowIndex % 2 = 0 THEN now() ELSE now() + INTERVAL '10 day' END
				   ,CASE WHEN rowIndex % 2 = 0 THEN now() ELSE NULL END
				   ,CASE WHEN rowIndex % 2 = 0 THEN now() ELSE now() + INTERVAL '10 day' END
				   ,CASE WHEN rowIndex % 2 = 0 THEN now() ELSE NULL END
				   ,CASE WHEN rowIndex % 2 = 0 THEN now() ELSE now() + INTERVAL '10 day' END
				   ,CASE WHEN rowIndex % 2 = 0 THEN now() ELSE NULL END
				   ,CASE WHEN rowIndex % 2 = 0 THEN 1024.99 ELSE 512.01 END
				   ,CASE WHEN rowIndex % 2 = 0 THEN 1024.99 ELSE NULL END
				   ,CASE WHEN rowIndex % 2 = 0 THEN 2048.123456789 ELSE 1024.987654321 END
				   ,CASE WHEN rowIndex % 2 = 0 THEN 2048.123456789 ELSE NULL END
				   ,CASE WHEN rowIndex % 2 = 0 THEN 4096.123456789 ELSE 4096.987654321 END
				   ,CASE WHEN rowIndex % 2 = 0 THEN 4096.123456789 ELSE NULL END
				   ,'0681757b-5f92-42c2-a4cd-90976f50225f'
				   ,'0681757b-5f92-42c2-a4cd-90976f50225f' --pg 的uuid列用不了case when表达式?? CASE WHEN rowIndex % 2 = 0 THEN '0681757b-5f92-42c2-a4cd-90976f50225f' ELSE NULL END
				   ,CASE WHEN rowIndex % 2 = 0 THEN 8192 ELSE 4096 END
				   ,CASE WHEN rowIndex % 2 = 0 THEN 8192 ELSE NULL END
				   ,CASE WHEN rowIndex % 2 = 0 THEN 819200000 ELSE 409600000 END
				   ,CASE WHEN rowIndex % 2 = 0 THEN 819200000 ELSE NULL END
				   ,CASE WHEN rowIndex % 2 = 0 THEN 8192000000000 ELSE 4096000000000 END
				   ,CASE WHEN rowIndex % 2 = 0 THEN 8192000000000 ELSE NULL END
			);        
		rowIndex:=rowIndex+1;
	End Loop; 
    
    rowIndex := 1;
	WHILE (rowIndex <= 100) LOOP
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
			   ,'XFramework' || cast(rowIndex as varchar)
			   ,'XFramework' || cast(rowIndex as varchar)
			   ,CASE WHEN rowIndex > 700 THEN 3 ELSE 1 END
			   ,now()
			   ,1
			   ,'XFramework' || cast(rowIndex as varchar));
		
		INSERT INTO Bas_ClientAccount
			   (ClientId
			   ,AccountId
			   ,AccountCode
			   ,AccountName
			   ,Qty)
		 VALUES
			   (rowIndex
			   ,1
			   ,'XFramework' || cast(rowIndex as varchar)
			   ,'XFramework' || cast(rowIndex as varchar)
			   ,CASE WHEN rowIndex % 2 = 0 THEN 1 ELSE 2 END);

		INSERT INTO Bas_ClientAccount
			   (ClientId
			   ,AccountId
			   ,AccountCode
			   ,AccountName
			   ,Qty)
		 VALUES
			   (rowIndex
			   ,2
			   ,'XFramework' || cast(rowIndex as varchar)
			   ,'XFramework' || cast(rowIndex as varchar)
			   ,CASE WHEN rowIndex % 2 = 0 THEN 1 ELSE 2 END);
					   
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
			   ,'XFramework' || cast(rowIndex as varchar)
			   ,'XFramework' || cast(rowIndex as varchar));
					   
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
			   ,'XFramework' || cast(rowIndex as varchar)
			   ,'XFramework' || cast(rowIndex as varchar));

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
			   ,'XFramework' || cast(rowIndex as varchar)
			   ,'XFramework' || cast(rowIndex as varchar));
					   
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
			   ,'XFramework' || cast(rowIndex as varchar)
			   ,'XFramework' || cast(rowIndex as varchar)); 
		rowIndex=rowIndex+1;
	END loop;
    
    --commit;
end $$;