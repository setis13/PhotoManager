use master
ALTER DATABASE Photo SET SINGLE_USER WITH ROLLBACK IMMEDIATE 
ALTER DATABASE Photo SET MULTI_USER
drop database Photo
RESTORE DATABASE Photo 
FROM DISK = '{0}' WITH MOVE 'Photo' 
TO 'C:\Program Files\Microsoft SQL Server\MSSQL12.SQLEXPRESS\MSSQL\DATA\Photo.mdf', 
MOVE 'Photo_log' 
TO 'C:\Program Files\Microsoft SQL Server\MSSQL12.SQLEXPRESS\MSSQL\DATA\Photo_log.mdf'