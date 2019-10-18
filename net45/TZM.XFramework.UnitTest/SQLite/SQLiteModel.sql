

--INSERT INTO [Sys_Demo]([DemoText_Nullable],[DemoNText_Nullable],[DemoTime_Nullable],[DemoDatetimeOffset_Nullable],[DemoBinary_Nullable],[DemVarBinary_Nullable],[DemoTimestamp_Nullable],[DemoCode],[DemoName],[DemoBoolean],[DemoBoolean_Nullable],[DemoChar],[DemoNChar],[DemoChar_Nullable],[DemoByte],[DemoByte_Nullable],[DemoDate],[DemoDate_Nullable],[DemoDateTime],[DemoDateTime_Nullable],[DemoDateTime2],[DemoDateTime2_Nullable],[DemoDecimal],[DemoDecimal_Nullable],[DemoDouble],[DemoDouble_Nullable],[DemoFloat],[DemoFloat_Nullable],[DemoGuid],[DemoGuid_Nullable],[DemoShort],[DemoShort_Nullable],[DemoInt],[DemoInt_Nullable],[DemoLong],[DemoLong_Nullable])
--    VALUES('TEXT 类型','NTEXT 类型','10:10:10.4567890','2007-06-10 00:00:00.0000000 -07:00',0xE8A1A8E7A4BAE697B6E58CBAE5818FE7A7BBE9878FEFBC88E58886E9929FEFBC89EFBC88E5A682E69E9CE4B8BAE695B4E695B0EFBC89E79A84E8A1A8E8BEBEE5BC8F,0xE8A1A8E7A4BAE697B6E58CBAE5818FE7A7BBE9878FEFBC88E58886E9929FEFBC89EFBC88E5A682E69E9CE4B8BAE695B4E695B0EFBC89E79A84E8A1A8E8BEBEE5BC8F,'2019-10-18 17:34:54.671','D0000001','N0000001',1,NULL,'A','B',NULL,64,NULL,'2019-10-18',NULL,'2019-10-18 17:34:54.671',NULL,'2019-10-18 17:34:54.6710128',NULL,64,NULL,64,NULL,64,NULL,'fd52afc4-733c-42b4-aa9e-cf5ce48a40b7',NULL,64,NULL,64,NULL,64,NULL)
    

update sys_demo set 
DemoBinary_Nullable = 'E8A1A8E7A4BAE697B6E58CBAE5818FE7A7BBE9878FEFBC88E58886E9929FEFBC89EFBC88E5A682E69E9CE4B8BAE695B4E695B0EFBC89E79A84E8A1A8E8BEBEE5BC8F'
where demoid=4