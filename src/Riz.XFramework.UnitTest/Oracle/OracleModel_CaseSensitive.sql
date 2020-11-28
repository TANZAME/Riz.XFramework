
--CREATE SEQUENCE SYS_DEMO_DEMOID_SEQ --创建序列名称
--INCREMENT BY 1                      --增长幅度
--START WITH 1                        --初始值
--MAXVALUE 9999999999999999;          --最大值

CREATE TABLE "bas_client_case"(
  "clientid" int PRIMARY KEY NOT NULL,
  "clientcode" nvarchar2(200) NOT NULL,
  "clientname" nvarchar2(200) NULL,
  "cloudserverid" int DEFAULT 0 NOT NULL ,
  "activedate" date NULL,
  "qty" int default 0 not null ,
  "state" number(3,0) NOT NULL,
  "remark" nvarchar2(250) DEFAULT N'默认值' NULL 
);
--alter table bas_Client modify cloudserverid default 0;
--alter table bas_Client modify state default 0;

CREATE TABLE "bas_clientaccount_case"(
  "clientid" int NOT NULL,
  "accountid" nvarchar2(100) NOT NULL,
  "accountcode" nvarchar2(200) NOT NULL,
  "accountname" nvarchar2(200) NOT NULL,
  "qty" int default 0 not null ,
  primary key("clientid","accountid")
);


CREATE TABLE "bas_clientaccountmarket_case"(
  "clientid" int NOT NULL,
  "accountid" nvarchar2(100) NOT NULL,
  "marketid" int NOT NULL,
  "marketcode" nvarchar2(50) NOT NULL,
  "marketname" nvarchar2(50) NOT NULL,
  PRIMARY KEY("clientid","accountid","marketid")
);

CREATE TABLE "sys_cloudserver_case"(
	"cloudserverid" int PRIMARY KEY NOT NULL,
	"cloudservercode" nvarchar2(50) NOT NULL,
	"cloudservername" nvarchar2(50) NOT NULL
);


CREATE TABLE "sys_demo_case"(
  "demoid" int PRIMARY KEY NOT NULL,
  "democode" varchar2(32) NULL,
  "demoname" nvarchar2(32) NOT NULL,
  "demoboolean" number(1,0) NOT NULL,             -- Boolean
  "demoboolean_nullable" number(1,0) NULL,
  "demochar" char(1) NOT NULL,
  "demonchar" nchar(1) NOT NULL,
  "demochar_nullable" char(1) NULL,
  "demobyte" number(3,0) NOT NULL,
  "demobyte_nullable" number(3,0) NULL,
  "demodate" date NOT NULL,
  "demodate_nullable" date NULL,
  "demodatetime" timestamp NOT NULL,
  "demodatetime_nullable" timestamp NULL,
  "demodatetime2" timestamp(7) NOT NULL,
  "demodatetime2_nullable" timestamp(7) NULL,
  "demodecimal" decimal(18, 2) NOT NULL,
  "demodecimal_nullable" decimal(18, 2) NULL,
  "demodouble" binary_double NOT NULL,     -- 双精度
  "demodouble_nullable" binary_double NULL,-- 双精度
  "demofloat" binary_float NOT NULL,       -- 单精度
  "demofloat_nullable" binary_float NULL,  -- 单精度
  "demoguid" raw(16) NOT NULL,
  "demoguid_nullable" raw(16) NULL,
  "demoshort" smallint NOT NULL,
  "demoshort_nullable" smallint NULL,      --number(5,0)
  "demoint" int NOT NULL,
  "demoint_nullable" int NULL,             --number(10,0)
  "demolong" number(19,0) NOT NULL,        --number(19,0)
  "demolong_nullable" number(19,0) NULL,
  
  "demotext_nullable" NCLOB NULL,
  "demontext_nullable" CLOB NULL,
	--DemoTime_Nullable timestamp NULL,      --INTERVAL DAY(2) TO SECOND(6) NULL,
  "demotime_nullable" INTERVAL DAY(2) TO SECOND(7) NULL,
  "demodatetimeoffset_nullable" timestamp(7) WITH TIME ZONE NULL,
  "demobinary_nullable" BLOB NULL,
  "demovarbinary_nullable" BLOB NULL,
  "demotimestamp_nullable"  timestamp(4) WITH LOCAL TIME ZONE NULL
);

DROP SEQUENCE "sys_demo_demoid_seq_case";
CREATE SEQUENCE "sys_demo_demoid_seq_case" INCREMENT BY 1;
COMMIT;

TRUNCATE TABLE "bas_client_case";
TRUNCATE TABLE "bas_clientaccount_case";
TRUNCATE TABLE "bas_clientaccountmarket_case";
TRUNCATE TABLE "sys_cloudserver_case";
COMMIT;

DECLARE rowIndex INT;
        rowCount_ INT;
        nextVal_ INT;

BEGIN
    rowIndex := 1;
    rowCount_ := 1000000;
    
    Delete FROM "sys_cloudserver_case";
    Insert Into "sys_cloudserver_case" ("cloudserverid","cloudservercode","cloudservername") Values (1,'0181',N'181服务器');
    Insert Into "sys_cloudserver_case" ("cloudserverid","cloudservercode","cloudservername") Values (2,'0182',N'182服务器');
    Insert Into "sys_cloudserver_case" ("cloudserverid","cloudservercode","cloudservername") Values (3,'0183',N'183服务器');      
   
  WHILE (rowIndex <= 100) LOOP
      
  
      nextVal_ := "sys_demo_demoid_seq_case".nextval;
      insert into "sys_demo_case"
          ("demoid"
          ,"democode"
          ,"demoname"
          ,"demoboolean"
          ,"demoboolean_nullable"
          ,"demochar"
          ,"demonchar"
          ,"demochar_nullable"
          ,"demobyte"
          ,"demobyte_nullable"
          ,"demodate"
          ,"demodate_nullable"
          ,"demodatetime"
          ,"demodatetime_nullable"
          ,"demodatetime2"
          ,"demodatetime2_nullable"
          ,"demodecimal"
          ,"demodecimal_nullable"
          ,"demodouble"
          ,"demodouble_nullable"
          ,"demofloat"
          ,"demofloat_nullable"
          ,"demoguid"
          ,"demoguid_nullable"
          ,"demoshort"
          ,"demoshort_nullable"
          ,"demoint"
          ,"demoint_nullable"
          ,"demolong"
          ,"demolong_nullable"
          ,"demotime_nullable"
          ,"demodatetimeoffset_nullable")
          values(nextval_
              ,rpad('c',length(to_char(rowcount_)) - length(to_char(rowindex)) + 1,'0') || to_char(rowindex)
              ,rpad('n',length(to_char(rowcount_)) - length(to_char(rowindex)) + 1,'0') || to_char(rowindex)
              ,case when mod(rowindex,2) = 0 then 1 else 0 end
              ,case when mod(rowindex,2) = 0 then 1 else null end
              ,case when mod(rowindex,2) = 0 then 'a' else 'b' end
              ,case when mod(rowindex,2) = 0 then 'c' else 'd' end
              ,case when mod(rowindex,2) = 0 then 'e' else null end
              ,case when rowindex <=200 then 64 when mod(rowindex,2) = 0 then 127 else 127 end
              ,case when mod(rowindex,2) = 0 then 127 else null end
              ,case when mod(rowindex,2) = 0 then sysdate else sysdate + 10 end
              ,case when mod(rowindex,2) = 0 then sysdate else null end
              ,case when mod(rowindex,2) = 0 then sysdate else sysdate + 10 end
              ,case when mod(rowindex,2) = 0 then sysdate else null end
              ,case when mod(rowindex,2) = 0 then sysdate else sysdate + 10 end
              ,case when mod(rowindex,2) = 0 then sysdate else null end
              ,case when mod(rowindex,2) = 0 then 1024.99 else 512.01 end
              ,case when mod(rowindex,2) = 0 then 1024.99 else null end
              ,case when mod(rowindex,2) = 0 then 2048.123456789 else 1024.987654321 end
              ,case when mod(rowindex,2) = 0 then 2048.123456789 else null end
              ,case when mod(rowindex,2) = 0 then 4096.123456789 else 4096.987654321 end
              ,case when mod(rowindex,2) = 0 then 4096.123456789 else null end
              ,sys_guid
              ,case when mod(rowindex,2) = 0 then sys_guid else null end
              ,case when mod(rowindex,2) = 0 then 8192 else 4096 end
              ,case when mod(rowindex,2) = 0 then 8192 else null end
              ,case when mod(rowindex,2) = 0 then 819200000 else 409600000 end
              ,case when mod(rowindex,2) = 0 then 819200000 else null end
              ,case when mod(rowindex,2) = 0 then 8192000000000 else 4096000000000 end
              ,case when mod(rowindex,2) = 0 then 8192000000000 else null end
              ,to_dsinterval('2 10:10:10.4567890')
              ,to_timestamp_tz('2007-06-10 00:00:00.0000000 -07:00','yyyy-mm-dd hh24:mi:ss.ff tzh:tzm')
      );

    INSERT INTO "bas_client_case"
        ("clientid"
        ,"clientcode"
        ,"clientname"
        ,"cloudserverid"
        ,"activedate"
        ,"state"
        ,"remark")
      VALUES
        (rowIndex
        ,'XFramework' || TO_CHAR(rowIndex)
        ,'XFramework' || TO_CHAR(rowIndex)
        ,CASE WHEN rowIndex > 100 THEN 1 ELSE 3 END
        ,SYSDATE
        ,1
        ,'XFramework' || TO_CHAR(rowIndex));
      
    INSERT INTO "bas_clientaccount_case"
        ("clientid"
        ,"accountid"
        ,"accountcode"
        ,"accountname"
        ,"qty")
      VALUES
        (rowIndex
        ,1
        ,'XFramework' || TO_CHAR(rowIndex)
        ,'XFramework' || TO_CHAR(rowIndex)
        ,CASE WHEN mod(rowIndex,2) = 0 THEN 1 ELSE 2 END);

    INSERT INTO "bas_clientaccount_case"
        ("clientid"
        ,"accountid"
        ,"accountcode"
        ,"accountname"
        ,"qty")
      VALUES
        (rowIndex
        ,2
        ,'XFramework' || TO_CHAR(rowIndex)
        ,'XFramework' || TO_CHAR(rowIndex)
        ,CASE WHEN mod(rowIndex,2) = 0 THEN 1 ELSE 2 END);
               
    INSERT INTO "bas_clientaccountmarket_case"
        ("clientid"
        ,"accountid"
        ,"marketid"
        ,"marketcode"
        ,"marketname")
      VALUES
        (rowIndex
        ,1
        ,1
        ,'XFramework' || TO_CHAR(rowIndex)
        ,'XFramework' || TO_CHAR(rowIndex));
               
    INSERT INTO "bas_clientaccountmarket_case"
        ("clientid"
        ,"accountid"
        ,"marketid"
        ,"marketcode"
        ,"marketname")
      VALUES
        (rowIndex
        ,1
        ,2
        ,'XFramework' || TO_CHAR(rowIndex)
        ,'XFramework' || TO_CHAR(rowIndex));

    INSERT INTO "bas_clientaccountmarket_case"
        ("clientid"
        ,"accountid"
        ,"marketid"
        ,"marketcode"
        ,"marketname")
      VALUES
        (rowIndex
        ,2
        ,1
        ,'XFramework' || TO_CHAR(rowIndex)
        ,'XFramework' || TO_CHAR(rowIndex));
               
    INSERT INTO "bas_clientaccountmarket_case"
        ("clientid"
        ,"accountid"
        ,"marketid"
        ,"marketcode"
        ,"marketname")
      VALUES
        (rowIndex
        ,2
        ,2
        ,'XFramework' || TO_CHAR(rowIndex)
        ,'XFramework' || TO_CHAR(rowIndex)); 
    rowIndex := rowIndex+1;
  END Loop;
  
  COMMIT;
End;
