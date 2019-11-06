-- 必须先创建数据库 TZM_XFramework

 USE [TZM_XFramework]
 GO

If Not Exists (Select Top 1 1  From sys.objects Where [object_id] = OBJECT_ID('Bas_Client','U'))
CREATE TABLE [dbo].[Bas_Client](
	[ClientId] [int] PRIMARY KEY NOT NULL,
	[ClientCode] [nvarchar](200) NOT NULL,
	[ClientName] [nvarchar](200) NULL,
	[CloudServerId] [int] NOT NULL DEFAULT(0),
	[ActiveDate] [datetime] NULL,
	[Qty] [int] not null default(0),
	[State] [tinyint] NOT NULL,
	[Remark] [nvarchar](250) NULL DEFAULT('默认值')
)
GO

If Not Exists (Select Top 1 1  From sys.objects Where [object_id] = OBJECT_ID('Bas_ClientAccount','U'))
CREATE TABLE [dbo].[Bas_ClientAccount](
	[ClientId] [int] NOT NULL,
	[AccountId] [nvarchar](100) NOT NULL,
	[AccountCode] [nvarchar](200) NOT NULL,
	[AccountName] [nvarchar](200) NOT NULL,
	[Qty] [int] not null default(0),
	PRIMARY KEY([ClientId],[AccountId])
)
GO

If Not Exists (Select Top 1 1  From sys.objects Where [object_id] = OBJECT_ID('Bas_ClientAccountMarket','U'))
CREATE TABLE [dbo].[Bas_ClientAccountMarket](
	[ClientId] [int] NOT NULL,
	[AccountId] [nvarchar](100) NOT NULL,
	[MarketId] [int] NOT NULL,
	[MarketCode] [nvarchar](50) NOT NULL,
	[MarketName] [nvarchar](50) NOT NULL,
	PRIMARY KEY([ClientId],[AccountId],[MarketId])
)
GO

If Not Exists (Select Top 1 1  From sys.objects Where [object_id] = OBJECT_ID('Sys_CloudServer','U'))
CREATE TABLE [dbo].[Sys_CloudServer](
	[CloudServerId] [int] PRIMARY KEY NOT NULL,
	[CloudServerCode] [nvarchar](50) NOT NULL,
	[CloudServerName] [nvarchar](50) NOT NULL
)
GO

If Not Exists (Select Top 1 1  From sys.objects Where [object_id] = OBJECT_ID('Sys_Demo','U'))
CREATE TABLE [dbo].[Sys_Demo](
	[DemoId] [int] IDENTITY(1,1) PRIMARY KEY NOT NULL,
	[DemoCode] [varchar](32) NULL,
	[DemoName] [nvarchar](32) NOT NULL,
	[DemoBoolean] BIT NOT NULL,
	[DemoBoolean_Nullable] BIT NULL,
	[DemoChar] [char](1) NOT NULL,
	[DemoNChar] [nchar](1) NOT NULL,
	[DemoChar_Nullable] [char](1) NULL,
	[DemoByte] [tinyint] NOT NULL,
	[DemoByte_Nullable] [tinyint] NULL,
	[DemoDate] [date] NOT NULL,
	[DemoDate_Nullable] [date] NULL,
	[DemoDateTime] [datetime] NOT NULL,
	[DemoDateTime_Nullable] [datetime] NULL,
	[DemoDateTime2] [datetime2](7) NOT NULL,				-- 不指定精度时默认为7
	[DemoDateTime2_Nullable] [datetime2](7) NULL,			-- 不指定精度时默认为7
	[DemoDecimal] [decimal](18, 2) NOT NULL,
	[DemoDecimal_Nullable] [decimal](18, 2) NULL,
	[DemoDouble] [float] NOT NULL,
	[DemoDouble_Nullable] [float] NULL,
	[DemoFloat] [real] NOT NULL,
	[DemoFloat_Nullable] [real] NULL,
	[DemoGuid] [uniqueidentifier] NOT NULL,
	[DemoGuid_Nullable] [uniqueidentifier] NULL,
	[DemoShort] [smallint] NOT NULL,
	[DemoShort_Nullable] [smallint] NULL,
	[DemoInt] [int] NOT NULL,
	[DemoInt_Nullable] [int] NULL,
	[DemoLong] [bigint] NOT NULL,
	[DemoLong_Nullable] [bigint] NULL,

	---- 
	[DemoText_Nullable] [text] NULL,
	[DemoNText_Nullable] [ntext] NULL,
	[DemoTime_Nullable] [time](6) NULL,						-- 不指定精度时默认为7
	[DemoDatetimeOffset_Nullable] [datetimeoffset](6) NULL,	-- 不指定精度时默认为7
	[DemoBinary_Nullable] binary(128) NULL,					-- 固定长
	[DemoVarBinary_Nullable] VarBinary(max) NULL,			-- 变长
	[DemoTimestamp_Nullable]  [timestamp] NULL,				-- 行版本号
	--[DemoXml_Nullable]  [xml] NULL,						-- XML
)
GO

If Not Exists (Select Top 1 1  From sys.objects Where [object_id] = OBJECT_ID('Sys_Rabbit','U'))
CREATE TABLE [dbo].[Sys_Rabbit](
	[DemoId] [int] IDENTITY(1,1) PRIMARY KEY NOT NULL,
	[DemoCode] [varchar](32) NULL,
	[DemoName] [nvarchar](32) NOT NULL,
	[DemoBoolean] BIT NOT NULL,
	[DemoBoolean_Nullable] BIT NULL,
	[DemoChar] [char](2) NOT NULL,
	[DemoNChar] [nchar](2) NOT NULL,
	[DemoChar_Nullable] [char](2) NULL,
	[DemoByte] [tinyint] NOT NULL,
	[DemoByte_Nullable] [tinyint] NULL,
	[DemoDate] [date] NOT NULL,
	[DemoDate_Nullable] [date] NULL,
	[DemoDateTime] [datetime] NOT NULL,
	[DemoDateTime_Nullable] [datetime] NULL,
	[DemoDateTime2] [datetime2](6) NOT NULL,
	[DemoDateTime2_Nullable] [datetime2](6) NULL,
	[DemoDecimal] [decimal](18, 2) NOT NULL,
	[DemoDecimal_Nullable] [decimal](18, 2) NULL,
	[DemoDouble] [float] NOT NULL,
	[DemoDouble_Nullable] [float] NULL,
	[DemoFloat] [real] NOT NULL,
	[DemoFloat_Nullable] [real] NULL,
	[DemoGuid] [uniqueidentifier] NOT NULL,
	[DemoGuid_Nullable] [uniqueidentifier] NULL,
	[DemoShort] [smallint] NOT NULL,
	[DemoShort_Nullable] [smallint] NULL,
	[DemoInt] [int] NOT NULL,
	[DemoInt_Nullable] [int] NULL,
	[DemoLong] [bigint] NOT NULL,
	[DemoLong_Nullable] [bigint] NULL
)
GO

IF NOT EXISTS (SELECT 1 FROM sys.types t join sys.schemas s on t.schema_id=s.schema_id  and t.name='JoinKey' and s.name='dbo')
   CREATE TYPE JoinKey AS TABLE 
	(
		Key1 INT NULL,
		Key2 NVARCHAR(200) NULL
	)
GO

TRUNCATE TABLE Sys_CloudServer 
Go
Insert Into [Sys_CloudServer] ([CloudServerId],[CloudServerCode],[CloudServerName]) Values (1,N'0181',N'181服务器')
Insert Into [Sys_CloudServer] ([CloudServerId],[CloudServerCode],[CloudServerName]) Values (2,N'0182',N'182服务器')
Insert Into [Sys_CloudServer] ([CloudServerId],[CloudServerCode],[CloudServerName]) Values (3,N'0183',N'183服务器')
Go


TRUNCATE TABLE [Sys_Demo]
TRUNCATE TABLE [Bas_Client]
TRUNCATE TABLE [Bas_ClientAccount]
TRUNCATE TABLE [Bas_ClientAccountMarket]
TRUNCATE TABLE [Sys_Rabbit]
GO

DECLARE @rowIndex INT = 1;
DECLARE @rowCount INT = 1000000;
BEGIN TRAN;

WHILE(@rowIndex<=@rowCount)
BEGIN
	IF @rowIndex <= 100
	BEGIN
		INSERT INTO [dbo].[Sys_Demo]
           ([DemoCode]
           ,[DemoName]
           ,[DemoBoolean]
           ,[DemoBoolean_Nullable]
           ,[DemoChar]
           ,[DemoNChar]
           ,[DemoChar_Nullable]
           ,[DemoByte]
           ,[DemoByte_Nullable]
           ,[DemoDate]
           ,[DemoDate_Nullable]
           ,[DemoDateTime]
           ,[DemoDateTime_Nullable]
           ,[DemoDateTime2]
           ,[DemoDateTime2_Nullable]
           ,[DemoDecimal]
           ,[DemoDecimal_Nullable]
           ,[DemoDouble]
           ,[DemoDouble_Nullable]
           ,[DemoFloat]
           ,[DemoFloat_Nullable]
           ,[DemoGuid]
           ,[DemoGuid_Nullable]
           ,[DemoShort]
           ,[DemoShort_Nullable]
           ,[DemoInt]
           ,[DemoInt_Nullable]
           ,[DemoLong]
           ,[DemoLong_Nullable]
           ,[DemoTime_Nullable]
           ,[DemoDatetimeOffset_Nullable]
		 )
		 VALUES
			   ('C' +  REPLICATE('0',LEN(CAST(@rowCount AS VARCHAR)) - LEN(CAST(@rowIndex AS VARCHAR))) + CAST(@rowIndex AS VARCHAR)
			   ,'N' +  REPLICATE('0',LEN(CAST(@rowCount AS VARCHAR)) - LEN(CAST(@rowIndex AS NVARCHAR))) + CAST(@rowIndex AS NVARCHAR)
			   ,CASE WHEN @rowIndex % 2 = 0 THEN 1 ELSE 0 END
			   ,CASE WHEN @rowIndex % 2 = 0 THEN 1 ELSE NULL END
			   ,CASE WHEN @rowIndex % 2 = 0 THEN 'A' ELSE 'B' END
			   ,CASE WHEN @rowIndex % 2 = 0 THEN N'C' ELSE N'D' END
			   ,CASE WHEN @rowIndex % 2 = 0 THEN 'E' ELSE NULL END
			   ,CASE WHEN @rowIndex <=200 THEN 64 WHEN @rowIndex % 2 = 0 THEN 127 ELSE 127 END
			   ,CASE WHEN @rowIndex % 2 = 0 THEN 127 ELSE NULL END
			   ,CASE WHEN @rowIndex % 2 = 0 THEN GETDATE() ELSE DATEADD(D,10,GETDATE()) END
			   ,CASE WHEN @rowIndex % 2 = 0 THEN GETDATE() ELSE NULL END
			   ,CASE WHEN @rowIndex % 2 = 0 THEN GETDATE() ELSE DATEADD(D,10,GETDATE()) END
			   ,CASE WHEN @rowIndex % 2 = 0 THEN GETDATE() ELSE NULL END
			   ,CASE WHEN @rowIndex % 2 = 0 THEN GETDATE() ELSE DATEADD(D,10,GETDATE()) END
			   ,CASE WHEN @rowIndex % 2 = 0 THEN GETDATE() ELSE NULL END
			   ,CASE WHEN @rowIndex % 2 = 0 THEN 1024.99 ELSE 512.01 END
			   ,CASE WHEN @rowIndex % 2 = 0 THEN 1024.99 ELSE NULL END
			   ,CASE WHEN @rowIndex % 2 = 0 THEN 2048.123456789 ELSE 1024.987654321 END
			   ,CASE WHEN @rowIndex % 2 = 0 THEN 2048.123456789 ELSE NULL END
			   ,CASE WHEN @rowIndex % 2 = 0 THEN 4096.123456789 ELSE 4096.987654321 END
			   ,CASE WHEN @rowIndex % 2 = 0 THEN 4096.123456789 ELSE NULL END
			   ,NEWID()
			   ,CASE WHEN @rowIndex % 2 = 0 THEN NEWID() ELSE NULL END
			   ,CASE WHEN @rowIndex % 2 = 0 THEN 8192 ELSE 4096 END
			   ,CASE WHEN @rowIndex % 2 = 0 THEN 8192 ELSE NULL END
			   ,CASE WHEN @rowIndex % 2 = 0 THEN 819200000 ELSE 409600000 END
			   ,CASE WHEN @rowIndex % 2 = 0 THEN 819200000 ELSE NULL END
			   ,CASE WHEN @rowIndex % 2 = 0 THEN 8192000000000 ELSE 4096000000000 END
			   ,CASE WHEN @rowIndex % 2 = 0 THEN 8192000000000 ELSE NULL END
			   ,'10:10:10.4567890'
			   ,TODATETIMEOFFSET('2007-06-10 00:00:00.0000000','-07:00')
		)
	END

	INSERT INTO [dbo].[Sys_Rabbit]
           ([DemoCode]
           ,[DemoName]
           ,[DemoBoolean]
           ,[DemoBoolean_Nullable]
           ,[DemoChar]
           ,[DemoNChar]
           ,[DemoChar_Nullable]
           ,[DemoByte]
           ,[DemoByte_Nullable]
           ,[DemoDate]
           ,[DemoDate_Nullable]
           ,[DemoDateTime]
           ,[DemoDateTime_Nullable]
           ,[DemoDateTime2]
           ,[DemoDateTime2_Nullable]
           ,[DemoDecimal]
           ,[DemoDecimal_Nullable]
           ,[DemoDouble]
           ,[DemoDouble_Nullable]
           ,[DemoFloat]
           ,[DemoFloat_Nullable]
           ,[DemoGuid]
           ,[DemoGuid_Nullable]
           ,[DemoShort]
           ,[DemoShort_Nullable]
           ,[DemoInt]
           ,[DemoInt_Nullable]
           ,[DemoLong]
           ,[DemoLong_Nullable])
     VALUES
           ('C' +  REPLICATE('0',LEN(CAST(@rowCount AS VARCHAR)) - LEN(CAST(@rowIndex AS VARCHAR))) + CAST(@rowIndex AS VARCHAR)
           ,'N' +  REPLICATE('0',LEN(CAST(@rowCount AS VARCHAR)) - LEN(CAST(@rowIndex AS VARCHAR))) + CAST(@rowIndex AS VARCHAR)
           ,CASE WHEN @rowIndex % 2 = 0 THEN 1 ELSE 0 END
           ,CASE WHEN @rowIndex % 2 = 0 THEN 1 ELSE NULL END
           ,CASE WHEN @rowIndex % 2 = 0 THEN 'US' ELSE 'US' END
		   ,CASE WHEN @rowIndex % 2 = 0 THEN N'US' ELSE N'MX' END
           ,CASE WHEN @rowIndex % 2 = 0 THEN 'CN' ELSE NULL END
           ,CASE WHEN @rowIndex % 2 = 0 THEN 127 ELSE 127 END
           ,CASE WHEN @rowIndex % 2 = 0 THEN 127 ELSE NULL END
           ,CASE WHEN @rowIndex % 2 = 0 THEN GETDATE() ELSE DATEADD(D,10,GETDATE()) END
           ,CASE WHEN @rowIndex % 2 = 0 THEN GETDATE() ELSE NULL END
           ,CASE WHEN @rowIndex % 2 = 0 THEN GETDATE() ELSE DATEADD(D,10,GETDATE()) END
           ,CASE WHEN @rowIndex % 2 = 0 THEN GETDATE() ELSE NULL END
           ,CASE WHEN @rowIndex % 2 = 0 THEN GETDATE() ELSE DATEADD(D,10,GETDATE()) END
           ,CASE WHEN @rowIndex % 2 = 0 THEN GETDATE() ELSE NULL END
           ,CASE WHEN @rowIndex % 2 = 0 THEN 1024.99 ELSE 512.01 END
           ,CASE WHEN @rowIndex % 2 = 0 THEN 1024.99 ELSE NULL END
           ,CASE WHEN @rowIndex % 2 = 0 THEN 2048.123456789 ELSE 1024.987654321 END
           ,CASE WHEN @rowIndex % 2 = 0 THEN 2048.123456789 ELSE NULL END
           ,CASE WHEN @rowIndex % 2 = 0 THEN 4096.123456789 ELSE 4096.987654321 END
           ,CASE WHEN @rowIndex % 2 = 0 THEN 4096.123456789 ELSE NULL END
           ,NEWID()
           ,CASE WHEN @rowIndex % 2 = 0 THEN NEWID() ELSE NULL END
           ,CASE WHEN @rowIndex % 2 = 0 THEN 8192 ELSE 4096 END
           ,CASE WHEN @rowIndex % 2 = 0 THEN 8192 ELSE NULL END
           ,CASE WHEN @rowIndex % 2 = 0 THEN 819200000 ELSE 409600000 END
           ,CASE WHEN @rowIndex % 2 = 0 THEN 819200000 ELSE NULL END
           ,CASE WHEN @rowIndex % 2 = 0 THEN 8192000000000 ELSE 4096000000000 END
           ,CASE WHEN @rowIndex % 2 = 0 THEN 8192000000000 ELSE NULL END
	)
	
	SET @rowIndex = @rowIndex + 1
END

-- 如果转换时没有指定数据类型的长度，则SQServer自动提供长度为30
SET @rowIndex = 1
WHILE @rowIndex<=100
BEGIN
	INSERT INTO [dbo].[Bas_Client]
           ([ClientId]
           ,[ClientCode]
           ,[ClientName]
           ,[CloudServerId]
           ,[ActiveDate]
           ,[State]
           ,[Remark])
     VALUES
           (@rowIndex
           ,'XFramework' + CAST(@rowIndex as nvarchar)
           ,'XFramework' + CAST(@rowIndex as nvarchar)
           ,CASE WHEN @rowIndex > 100 THEN 1 ELSE 3 END
           ,getdate()
           ,1
           ,'XFramework' + CAST(@rowIndex as nvarchar))
	
	INSERT INTO [dbo].[Bas_ClientAccount]
           ([ClientId]
           ,[AccountId]
           ,[AccountCode]
           ,[AccountName]
		   ,[Qty])
     VALUES
           (@rowIndex
           ,1
           ,'XFrameworkAccount' + CAST(@rowIndex as nvarchar)
           ,'XFrameworkAccount' + CAST(@rowIndex as nvarchar)
		   ,CASE WHEN @rowIndex % 2 = 0 THEN 1 ELSE 2 END)

	INSERT INTO [dbo].[Bas_ClientAccount]
           ([ClientId]
           ,[AccountId]
           ,[AccountCode]
           ,[AccountName]
		   ,[Qty])
     VALUES
           (@rowIndex
           ,2
           ,'XFrameworkAccount' + CAST(@rowIndex as nvarchar)
           ,'XFrameworkAccount' + CAST(@rowIndex as nvarchar)
		   ,CASE WHEN @rowIndex % 2 = 0 THEN 1 ELSE 2 END)
		   		   
	INSERT INTO [dbo].[Bas_ClientAccountMarket]
           ([ClientId]
           ,[AccountId]
           ,[MarketId]
           ,[MarketCode]
           ,[MarketName])
     VALUES
           (@rowIndex
           ,1
           ,1
           ,'XFrameworkAccountMarket' + CAST(@rowIndex as nvarchar)
           ,'XFrameworkAccountMarket' + CAST(@rowIndex as nvarchar))
		   		   
	INSERT INTO [dbo].[Bas_ClientAccountMarket]
           ([ClientId]
           ,[AccountId]
           ,[MarketId]
           ,[MarketCode]
           ,[MarketName])
     VALUES
           (@rowIndex
           ,1
           ,2
           ,'XFrameworkAccountMarket' + CAST(@rowIndex as nvarchar)
           ,'XFrameworkAccountMarket' + CAST(@rowIndex as nvarchar))

	INSERT INTO [dbo].[Bas_ClientAccountMarket]
           ([ClientId]
           ,[AccountId]
           ,[MarketId]
           ,[MarketCode]
           ,[MarketName])
     VALUES
           (@rowIndex
           ,2
           ,1
           ,'XFrameworkAccountMarket' + CAST(@rowIndex as nvarchar)
           ,'XFrameworkAccountMarket' + CAST(@rowIndex as nvarchar))
		   		   
	INSERT INTO [dbo].[Bas_ClientAccountMarket]
           ([ClientId]
           ,[AccountId]
           ,[MarketId]
           ,[MarketCode]
           ,[MarketName])
     VALUES
           (@rowIndex
           ,2
           ,2
           ,'XFrameworkAccountMarket' + CAST(@rowIndex as nvarchar)
           ,'XFrameworkAccountMarket' + CAST(@rowIndex as nvarchar))
		  
	SET @rowIndex=@rowIndex+1
END
COMMIT;