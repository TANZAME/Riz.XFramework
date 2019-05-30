
create or replace procedure spSys_DropIfExists( 
    p_table in varchar2 
) is 
    v_count number(10); 
begin 
   select count(*) 
   into v_count 
   from user_tables 
   where table_name = upper(p_table);
 
   if v_count > 0 then 
      execute immediate 'drop table ' || p_table ||' purge'; 
   end if; 
end spSys_DropIfExists;
