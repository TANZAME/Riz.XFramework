
 
 /*****************************************************************
说明：写入初始数据
作者：
日期：
公司：

*****************************************************************/

drop procedure if exists `sySys_Initialize`;
delimiter $$
Create procedure `sySys_Initialize`(
)
begin
    declare rowIndex int;
    declare rowCount int;
    set rowIndex := 0,rowCount := 1000000;
	
    Delete FROM Sys_CloudServer;
	Insert Into Sys_CloudServer (CloudServerId,CloudServerCode,CloudServerName) Values (1,'0181','181服务器');
	Insert Into Sys_CloudServer (CloudServerId,CloudServerCode,CloudServerName) Values (2,'0182','182服务器');
	Insert Into Sys_CloudServer (CloudServerId,CloudServerCode,CloudServerName) Values (3,'0183','183服务器');
    
    TRUNCATE TABLE Sys_Demo;
	TRUNCATE TABLE Bas_Client;
	TRUNCATE TABLE Bas_ClientAccount;
	TRUNCATE TABLE Bas_ClientAccountMarket;
	TRUNCATE TABLE Sys_Rabbit;
    
	WHILE(rowIndex <= rowCount) do
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
			   ,DemoLong_Nullable)
			 VALUES
				   (CONCAT('Code',REPEAT('0',CHAR_LENGTH(CAST(rowCount AS CHAR)) - CHAR_LENGTH(CAST(rowIndex AS CHAR))),CAST(rowIndex AS CHAR))
				   ,CONCAT('名称',REPEAT('0',CHAR_LENGTH(CAST(rowCount AS CHAR)) - CHAR_LENGTH(CAST(rowIndex AS CHAR))),CAST(rowIndex AS CHAR))
				   ,CASE WHEN rowIndex % 2 = 0 THEN 1 ELSE 0 END
				   ,CASE WHEN rowIndex % 2 = 0 THEN 1 ELSE NULL END
				   ,CASE WHEN rowIndex % 2 = 0 THEN 'A' ELSE 'B' END
				   ,CASE WHEN rowIndex % 2 = 0 THEN 'C' ELSE 'D' END
				   ,CASE WHEN rowIndex % 2 = 0 THEN 'E' ELSE NULL END
				   ,CASE WHEN rowIndex <=200 THEN 64 WHEN rowIndex % 2 = 0 THEN 127 ELSE 127 END
				   ,CASE WHEN rowIndex % 2 = 0 THEN 127 ELSE NULL END
				   ,CASE WHEN rowIndex % 2 = 0 THEN now() ELSE DATE_ADD(now(),INTERVAL 10 DAY) END
				   ,CASE WHEN rowIndex % 2 = 0 THEN now() ELSE NULL END
				   ,CASE WHEN rowIndex % 2 = 0 THEN now() ELSE DATE_ADD(now(),INTERVAL 10 DAY) END
				   ,CASE WHEN rowIndex % 2 = 0 THEN now() ELSE NULL END
				   ,CASE WHEN rowIndex % 2 = 0 THEN now() ELSE DATE_ADD(now(),INTERVAL 10 DAY) END
				   ,CASE WHEN rowIndex % 2 = 0 THEN now() ELSE NULL END
				   ,CASE WHEN rowIndex % 2 = 0 THEN 1024.99 ELSE 512.01 END
				   ,CASE WHEN rowIndex % 2 = 0 THEN 1024.99 ELSE NULL END
				   ,CASE WHEN rowIndex % 2 = 0 THEN 2048.123456789 ELSE 1024.987654321 END
				   ,CASE WHEN rowIndex % 2 = 0 THEN 2048.123456789 ELSE NULL END
				   ,CASE WHEN rowIndex % 2 = 0 THEN 4096.123456789 ELSE 4096.987654321 END
				   ,CASE WHEN rowIndex % 2 = 0 THEN 4096.123456789 ELSE NULL END
				   ,UUID()
				   ,CASE WHEN rowIndex % 2 = 0 THEN UUID() ELSE NULL END
				   ,CASE WHEN rowIndex % 2 = 0 THEN 8192 ELSE 4096 END
				   ,CASE WHEN rowIndex % 2 = 0 THEN 8192 ELSE NULL END
				   ,CASE WHEN rowIndex % 2 = 0 THEN 819200000 ELSE 409600000 END
				   ,CASE WHEN rowIndex % 2 = 0 THEN 819200000 ELSE NULL END
				   ,CASE WHEN rowIndex % 2 = 0 THEN 8192000000000 ELSE 4096000000000 END
				   ,CASE WHEN rowIndex % 2 = 0 THEN 8192000000000 ELSE NULL END
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
				   (CONCAT('Code',REPEAT('0',CHAR_LENGTH(CAST(rowCount AS CHAR)) - CHAR_LENGTH(CAST(rowIndex AS CHAR))),CAST(rowIndex AS CHAR))
				   ,CONCAT('名称',REPEAT('0',CHAR_LENGTH(CAST(rowCount AS CHAR)) - CHAR_LENGTH(CAST(rowIndex AS CHAR))),CAST(rowIndex AS CHAR))
				   ,CASE WHEN rowIndex % 2 = 0 THEN 1 ELSE 0 END
				   ,CASE WHEN rowIndex % 2 = 0 THEN 1 ELSE NULL END
				   ,CASE WHEN rowIndex % 2 = 0 THEN 'CN' ELSE 'TW' END
				   ,CASE WHEN rowIndex % 2 = 0 THEN 'US' ELSE 'MX' END
				   ,CASE WHEN rowIndex % 2 = 0 THEN 'CN' ELSE NULL END
				   ,CASE WHEN rowIndex <=200 THEN 64 WHEN rowIndex % 2 = 0 THEN 127 ELSE 127 END
				   ,CASE WHEN rowIndex % 2 = 0 THEN 127 ELSE NULL END
				   ,CASE WHEN rowIndex % 2 = 0 THEN now() ELSE DATE_ADD(now(),INTERVAL 10 DAY) END
				   ,CASE WHEN rowIndex % 2 = 0 THEN now() ELSE NULL END
				   ,CASE WHEN rowIndex % 2 = 0 THEN now() ELSE DATE_ADD(now(),INTERVAL 10 DAY) END
				   ,CASE WHEN rowIndex % 2 = 0 THEN now() ELSE NULL END
				   ,CASE WHEN rowIndex % 2 = 0 THEN now() ELSE DATE_ADD(now(),INTERVAL 10 DAY) END
				   ,CASE WHEN rowIndex % 2 = 0 THEN now() ELSE NULL END
				   ,CASE WHEN rowIndex % 2 = 0 THEN 1024.99 ELSE 512.01 END
				   ,CASE WHEN rowIndex % 2 = 0 THEN 1024.99 ELSE NULL END
				   ,CASE WHEN rowIndex % 2 = 0 THEN 2048.123456789 ELSE 1024.987654321 END
				   ,CASE WHEN rowIndex % 2 = 0 THEN 2048.123456789 ELSE NULL END
				   ,CASE WHEN rowIndex % 2 = 0 THEN 4096.123456789 ELSE 4096.987654321 END
				   ,CASE WHEN rowIndex % 2 = 0 THEN 4096.123456789 ELSE NULL END
				   ,UUID()
				   ,CASE WHEN rowIndex % 2 = 0 THEN UUID() ELSE NULL END
				   ,CASE WHEN rowIndex % 2 = 0 THEN 8192 ELSE 4096 END
				   ,CASE WHEN rowIndex % 2 = 0 THEN 8192 ELSE NULL END
				   ,CASE WHEN rowIndex % 2 = 0 THEN 819200000 ELSE 409600000 END
				   ,CASE WHEN rowIndex % 2 = 0 THEN 819200000 ELSE NULL END
				   ,CASE WHEN rowIndex % 2 = 0 THEN 8192000000000 ELSE 4096000000000 END
				   ,CASE WHEN rowIndex % 2 = 0 THEN 8192000000000 ELSE NULL END
			);        
		set rowIndex:=rowIndex+1;
	end while; 
    
    set rowIndex := 1;
	WHILE (rowIndex <= 2000) do
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
			   ,CONCAT('XFramework' , cast(rowIndex as char))
			   ,CONCAT('XFramework' , cast(rowIndex as char))
			   ,CASE WHEN rowIndex % 2 = 0 THEN 1 ELSE 3 END
			   ,now()
			   ,1
			   ,CONCAT('XFramework' , cast(rowIndex as char)));
		
		INSERT INTO Bas_ClientAccount
			   (ClientId
			   ,AccountId
			   ,AccountCode
			   ,AccountName
			   ,Qty)
		 VALUES
			   (rowIndex
			   ,1
			   ,CONCAT('XFramework' , cast(rowIndex as char))
			   ,CONCAT('XFramework' , cast(rowIndex as char))
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
			   ,CONCAT('XFramework' , cast(rowIndex as char))
			   ,CONCAT('XFramework' , cast(rowIndex as char))
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
			   ,CONCAT('XFramework' , cast(rowIndex as char))
			   ,CONCAT('XFramework' , cast(rowIndex as char)));
					   
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
			   ,CONCAT('XFramework' , cast(rowIndex as char))
			   ,CONCAT('XFramework' , cast(rowIndex as char)));

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
			   ,CONCAT('XFramework' , cast(rowIndex as char))
			   ,CONCAT('XFramework' , cast(rowIndex as char)));
					   
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
			   ,CONCAT('XFramework' , cast(rowIndex as char))
			   ,CONCAT('XFramework' , cast(rowIndex as char)));
		SET rowIndex=rowIndex+1;
	END WHILE;
    
    commit;
end $$