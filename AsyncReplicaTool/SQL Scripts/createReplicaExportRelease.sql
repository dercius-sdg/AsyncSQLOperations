SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- =============================================
-- Author:		Dmitry Suschevsky
-- Create date: 2018/04/19
-- Description:	јльтернативна€ процедура формировани€ реплики с возможной проверкой выполнени€ команды bcp и логирование результата в таблицу ReplicaSyncSessions
-- =============================================
--CREATE PROCEDURE [dbo].[ReplicaExportRelease] 
ALTER PROCEDURE [dbo].[ReplicaExportRelease] 
(
	@DestinationReplicaId INT,
	@exportPath NVARCHAR(4000) = null
)
AS
BEGIN

SET NOCOUNT ON;

DECLARE @starttime DATETIME = CURRENT_TIMESTAMP                                                                                                                       --start time
DECLARE @spid SMALLINT = @@SPID                                                                                                                                                                               --current process

DECLARE @this_dbname NVARCHAR(100) = DB_NAME() 
DECLARE @this_db_schema NVARCHAR(100) = '[' + @this_dbname + '].['+ SCHEMA_NAME() + '].'      --current [db name].[schema name].
DECLARE @current_version BIGINT       = CHANGE_TRACKING_CURRENT_VERSION();                                                --current version of replica A
DECLARE @min_valid_version BIGINT                                                                                                                                                                                              --min of min versions of changes of sync tables of replica A aware of
DECLARE @SourceReplicaId INT                                                                                                                                                                                                                          --I am replica A
DECLARE @last_sync_version BIGINT                                                                                                                                                                                               --last version of A after import from B
DECLARE @last_known_version BIGINT                                                                                                                                                                                                          --replica A knows about that version of replica B
DECLARE @last_aware_version BIGINT                                                                                                                                                                                                           --replica B aware of version of replica A
DECLARE @filepath NVARCHAR(4000)                                                                                                                                                                                              --save file path (limit for xp_cmdshell)
DECLARE @bcp_format_cmd NVARCHAR(1024)
DECLARE @bcp_queryout_cmd NVARCHAR(1024)
DECLARE @bcp_meta_query NVARCHAR(1024)
DECLARE @security_word NVARCHAR(1024)
DECLARE @err_msg NVARCHAR(1024) = N'Replica export fail because of change tracking data lost for replica ' + cast(@DestinationReplicaId as nvarchar(128)) + '. Snapshot export and reinitialization of destination replica required.'
DECLARE @divide_by_replicas_own BIT 
DECLARE @Schema_Holder INT
DECLARE @tables nvarchar(1024)
DECLARE @tables_query nvarchar(1024)
DECLARE @check_data_exists NVARCHAR(1024)
DECLARE @count int
DECLARE @dbVersion NVARCHAR(40)
DECLARE @instanceName NVARCHAR(128) = cast(serverproperty('InstanceName') AS NVARCHAR(128)), @serverName NVARCHAR(128)
DECLARE @cmdText as nvarchar(255)
DECLARE @cmdQuery as nvarchar(255)
DECLARE @cmdErrorText as nvarchar(max)
DECLARE @cmdResultCode as bit

If (OBJECT_ID('tempdb..#cmdResults','u') is not null) -- если существует временна€ таблица дл€ хранени€ результатов работы bcp то удал€ем чтобы избежать ошибок
BEGIN
drop table #cmdResults
END

SELECT @serverName = CASE WHEN @instanceName IS NULL THEN '' ELSE ' -S ' + @@SERVERNAME END

SELECT @SourceReplicaId = Id, @security_word = IsNull(Security_Word,''), @Schema_Holder = IsNull(Schema_Holder, 0)
FROM Replica

SELECT @last_sync_version = IsNull(LastSyncVersion,0),
                   @last_known_version = IsNull(KnownVersion,0),
                   @filepath = IsNull(FilePathSave,''),
                   @last_aware_version = IsNull(AwareVersion,0)             
FROM dbo.Replicas
WHERE ReplicaId = @DestinationReplicaId

SELECT @filepath = IsNull(@exportPath,@filepath) + '\'

IF (@last_aware_version > 0)
BEGIN
                SELECT @min_valid_version = MIN(ISNULL(CHANGE_TRACKING_MIN_VALID_VERSION(OBJECT_ID(tablename)),0)) FROM dbo.ReplicaSyncTables WHERE syncmode <> 1
                IF (@min_valid_version > @last_aware_version)                                                                                                                         --change data lost 
                BEGIN
                               RAISERROR (@err_msg, 11, 1) WITH NOWAIT
                               RETURN
                END
END


--check if folder empty of meta.xml-->
DECLARE @cmd NVARCHAR(1024) = 'DIR "'+@filepath+'meta.xml" /B'
DECLARE @output INT
EXEC @output = XP_CMDSHELL @cmd, no_output

IF (@output <> 1)--exists meta.xml
BEGIN
                RAISERROR ('Export folder is not empty. File "meta.xml" was found.', 11, 1) WITH NOWAIT
                RETURN
END
SELECT @cmd = 'DIR "'+@filepath+'gmmq.package.end" /B'
EXEC @output = XP_CMDSHELL @cmd, no_output

IF (@output <> 1)--exists gmmq.package.end
BEGIN
                RAISERROR ('Export folder is not empty. File "gmmq.package.end" was found.', 11, 1) WITH NOWAIT
                RETURN
END
/*
SELECT @bcp_meta_query = 'select '+
                cast(@SourceReplicaId as NVARCHAR(128)) +' as srcid, '+
                cast(@DestinationReplicaId as NVARCHAR(128)) + ' as dstid, '+
                cast(@last_known_version as NVARCHAR(128)) + ' as know, '+
                cast(@current_version as NVARCHAR(128)) + ' as srcversion, ' + 
                'DATABASEPROPERTYEX(''' + @this_dbname + ''', ''Collation'') as collation, ' +
                '''' + @security_word + ''' as securityword from '+
                @this_db_schema +'replica as replica for xml auto,type' 
SELECT @bcp_meta_query = 'select cast((select (' + @bcp_meta_query + ') as knowledge,'+
                '(select tablename as name, syncmode as syncmode from ' + @this_db_schema + 'replicasynctables as synctable for xml auto,type) as synctables from ' + @this_db_schema + 'Replica as root for xml AUTO) as nvarchar(max))'
SELECT @bcp_queryout_cmd = 'bcp "' + @bcp_meta_query + '" queryout ' + @filepath + 'meta.xml -T -w'

EXEC xp_cmdshell @bcp_queryout_cmd, no_output
*/

SELECT @tables = NewId()
SELECT @tables = '##tables_' + replace(@tables, '-', '')
SELECT @tables_query = 'CREATE TABLE ' + @tables + '(tablename nvarchar(100), data int)'
EXEC(@tables_query)

CREATE TABLE #ReplicasOwn(tableName NVARCHAR(100))
--INSERT #ReplicasOwn SELECT DISTINCT TableName FROM ReplicasOwn
INSERT #ReplicasOwn SELECT DISTINCT b.TableName FROM ReplicasOwn a INNER JOIN REPLICASYNCTABLES b ON a.TABLENAME=b.TABLENAME

--CREATE TABLE ##tables(tablename nvarchar(100), data int) --metadata tables
DECLARE @ord INT
DECLARE @tablename NVARCHAR(100)
DECLARE @exportmode INT
DECLARE synctables_cursor CURSOR FOR SELECT tablename, ord, exportmode
                FROM dbo.ReplicaSyncTables 
                WHERE syncmode <> 1 
                UNION
                SELECT
                'ReplicaSchemaChanges', -1 as ord, 0
                WHERE @Schema_Holder = 1
                ORDER BY ord
;

create table #cmdResults (id int identity(1,1), output nvarchar(255) null)

SET @cmdErrorText = ''

declare cmdCursor cursor
scroll static read_only
for
select output
from #cmdResults
where output is not null

OPEN synctables_cursor;
FETCH NEXT FROM synctables_cursor INTO @tablename, @ord, @exportmode;
WHILE @@FETCH_STATUS = 0
BEGIN
                SELECT @divide_by_replicas_own = count(1) FROM (select top 1 * from #ReplicasOwn where TableName = @tablename) b

                select @count = 1
                exec ReplicaQuery @tablename, @DestinationReplicaId, @last_aware_version, @divide_by_replicas_own, @exportMode, @count output

                if(@count <> 0 or @tablename = 'ReplicaSchemaChanges')
                BEGIN
                SELECT @bcp_format_cmd = 'bcp "select ' + @this_db_schema + 'GetBcpFormatXml(''' + @tablename + ''',0,0)" queryout ' + @filepath + @tablename + '.fe -T -w ' + @serverName

                INSERT #cmdResults EXEC @cmdResultCode = xp_cmdshell @bcp_format_cmd
				IF(@cmdResultCode = 1)
				BEGIN
					
					SET @cmdErrorText = 'Error occured on file "' + @tablename + '.fe".Exception message is "'
					OPEN cmdCursor
					FETCH FIRST FROM cmdCursor INTO @cmdText
					WHILE @@FETCH_STATUS = 0
					BEGIN

					SET @cmdErrorText = @cmdErrorText + @cmdText
					FETCH NEXT FROM cmdCursor INTO @cmdText

					END

					delete from #cmdResults
					close cmdCursor

					SET @cmdErrorText = @cmdErrorText +'".'

					SET @cmdQuery = 'del "' + @filepath + '*.*" /Q'
					INSERT #cmdResults EXEC @cmdResultCode = xp_cmdshell @cmdQuery
					IF(@cmdResultCode = 0)
					BEGIN
						SET @cmdErrorText = @cmdErrorText + 'Export folder is flushed.'
					END
					ELSE
					BEGIN
						SET @cmdErrorText = @cmdErrorText + 'Export folder cannot be flushed because of the next exception: "'
						OPEN cmdCursor
						FETCH FIRST FROM cmdCursor INTO @cmdText
						WHILE @@FETCH_STATUS = 0
						BEGIN

						SET @cmdErrorText = @cmdErrorText + @cmdText
						FETCH NEXT FROM cmdCursor into @cmdText

						END

						SET @cmdErrorText = @cmdErrorText + '".'

						DELETE FROM #cmdResults
						CLOSE cmdCursor
					END

					DELETE FROM #cmdResults

					INSERT INTO dbo.ReplicaSyncSessions(ReplicaId, spid, Start, STATUS, FINISH, ERRORTEXT)
					VALUES(@DestinationReplicaId, @spid, @starttime, 2, CURRENT_TIMESTAMP, @cmdErrorText)

					RAISERROR (@cmdErrorText, 11, 1) WITH NOWAIT
					RETURN

				END

                SELECT @bcp_format_cmd = 'bcp "select ' + @this_db_schema + 'GetBcpFormatXml(''' + @tablename + ''',1,0)" queryout ' + @filepath + @tablename + '.fi -T -w ' + @serverName

                INSERT #cmdResults EXEC @cmdResultCode = xp_cmdshell @bcp_format_cmd
				IF(@cmdResultCode = 1)
				BEGIN
					
					SET @cmdErrorText = 'Error occured on file "' + @tablename + '.fi".Exception message is "'
					OPEN cmdCursor
					FETCH FIRST FROM cmdCursor INTO @cmdText
					WHILE @@FETCH_STATUS = 0
					BEGIN

					SET @cmdErrorText = @cmdErrorText + @cmdText
					FETCH NEXT FROM cmdCursor into @cmdText

					END

					delete from #cmdResults
					close cmdCursor

					SET @cmdErrorText = @cmdErrorText +'".'

					SET @cmdQuery = 'del "' + @filepath + '*.*" /Q'
					INSERT #cmdResults EXEC @cmdResultCode = xp_cmdshell @cmdQuery
					IF(@cmdResultCode = 0)
					BEGIN
						SET @cmdErrorText = @cmdErrorText + 'Export folder is flushed.'
					END
					ELSE
					BEGIN
						SET @cmdErrorText = @cmdErrorText + 'Export folder cannot be flushed because of the next exception: "'
						OPEN cmdCursor
						FETCH FIRST FROM cmdCursor INTO @cmdText
						WHILE @@FETCH_STATUS = 0
						BEGIN

						SET @cmdErrorText = @cmdErrorText + @cmdText
						FETCH NEXT FROM cmdCursor into @cmdText

						END

						SET @cmdErrorText = @cmdErrorText + '".'

						DELETE FROM #cmdResults
						CLOSE cmdCursor
					END

					DELETE FROM #cmdResults

					INSERT INTO dbo.ReplicaSyncSessions(ReplicaId, spid, Start, STATUS, FINISH, ERRORTEXT)
					VALUES(@DestinationReplicaId, @spid, @starttime, 2, CURRENT_TIMESTAMP, @cmdErrorText)

					RAISERROR (@cmdErrorText, 11, 1) WITH NOWAIT
					RETURN

				END

                SELECT @bcp_queryout_cmd = 'bcp "exec ' + @this_db_schema + 'ReplicaQuery ''' + 
                               @tablename + ''',' + cast(@DestinationReplicaId as NVARCHAR(128)) + ',' + cast(@last_aware_version as  NVARCHAR(128)) + ',' + cast(@divide_by_replicas_own as NVARCHAR(1)) + ',' + cast(@exportmode as NVARCHAR(128)) +
                               '" queryout ' + @filepath + @tablename + '.dat -f ' + @filepath + @tablename + '.fe -T ' + @serverName
                INSERT #cmdResults EXEC @cmdResultCode = xp_cmdshell @bcp_queryout_cmd
				IF(@cmdResultCode = 1)
				BEGIN
					
					SET @cmdErrorText = 'Error occured on file "' + @tablename + '.dat".Exception message is "'
					OPEN cmdCursor
					FETCH FIRST FROM cmdCursor INTO @cmdText
					WHILE @@FETCH_STATUS = 0
					BEGIN

					SET @cmdErrorText = @cmdErrorText + @cmdText
					FETCH NEXT FROM cmdCursor into @cmdText

					END

					delete from #cmdResults
					close cmdCursor

					SET @cmdErrorText = @cmdErrorText +'".'

					SET @cmdQuery = 'del "' + @filepath + '*.*" /Q'
					INSERT #cmdResults EXEC @cmdResultCode = xp_cmdshell @cmdQuery
					IF(@cmdResultCode = 0)
					BEGIN
						SET @cmdErrorText = @cmdErrorText + 'Export folder is flushed.'
					END
					ELSE
					BEGIN
						SET @cmdErrorText = @cmdErrorText + 'Export folder cannot be flushed because of the next exception: "'
						OPEN cmdCursor
						FETCH FIRST FROM cmdCursor INTO @cmdText
						WHILE @@FETCH_STATUS = 0
						BEGIN

						SET @cmdErrorText = @cmdErrorText + @cmdText
						FETCH NEXT FROM cmdCursor into @cmdText

						END

						SET @cmdErrorText = @cmdErrorText + '".'

						DELETE FROM #cmdResults
						CLOSE cmdCursor
					END

					DELETE FROM #cmdResults

					INSERT INTO dbo.ReplicaSyncSessions(ReplicaId, spid, Start, STATUS, FINISH, ERRORTEXT)
					VALUES(@DestinationReplicaId, @spid, @starttime, 2, CURRENT_TIMESTAMP, @cmdErrorText)

					RAISERROR (@cmdErrorText, 11, 1) WITH NOWAIT
					RETURN

				END
                END

                SELECT @tables_query = 'INSERT ' + @tables + ' SELECT ''' + @tablename + ''', ' + cast(@count as nvarchar(128))
				EXEC(@tables_query)
				--INSERT ##tables SELECT @tablename, @count

    FETCH NEXT FROM synctables_cursor INTO @tablename, @ord, @exportmode;
END

CLOSE synctables_cursor;
DEALLOCATE synctables_cursor;

DROP TABLE #ReplicasOwn

SELECT @tables_query = 'INSERT ' + @tables + ' SELECT TABLENAME, 0 FROM ReplicaSyncTables x WHERE not exists (SELECT 1 FROM ' + @tables + ' y WHERE x.TABLENAME = y.tablename)'
EXEC(@tables_query)
--INSERT ##tables
--SELECT TABLENAME, 0
--FROM ReplicaSyncTables x 
--                WHERE not exists (SELECT 1 FROM ##tables y WHERE x.TABLENAME = y.tablename)

--force check anyway snapshot isolation is off or on->
IF (@last_aware_version > 0)
BEGIN
                SELECT @min_valid_version = MIN(ISNULL(CHANGE_TRACKING_MIN_VALID_VERSION(OBJECT_ID(tablename)),0)) FROM dbo.ReplicaSyncTables WHERE syncmode <> 1
                IF (@min_valid_version > @last_aware_version)                                                                                                                         --change data lost 
                BEGIN
					SELECT @tables_query = 'DROP TABLE ' + @tables
					EXEC(@tables_query)
					--DROP TABLE ##tables
                    RAISERROR (@err_msg, 11, 1) WITH NOWAIT
                    RETURN
                END 
END
--force check anyway snapshot isolation is off or on<--

SELECT TOP 1 @dbVersion = Version FROM GM_VersionHistory ORDER BY id DESC

SELECT @bcp_meta_query = 'select '+
                cast(@SourceReplicaId as NVARCHAR(128)) +' as srcid, '+
                cast(@DestinationReplicaId as NVARCHAR(128)) + ' as dstid, '+
                cast(@last_known_version as NVARCHAR(128)) + ' as know, '+
                cast(@current_version as NVARCHAR(128)) + ' as srcversion, ' + 
                'DATABASEPROPERTYEX(''' + @this_dbname + ''', ''Collation'') as collation, ' +
                '''' + @security_word + ''' as securityword, '+
                '''' + @dbVersion + ''' as dbVersion from '+
                @this_db_schema +'replica as replica for xml auto,type' 
SELECT @bcp_meta_query = 'select cast((select (' + @bcp_meta_query + ') as knowledge,'+
			'(select synctable.tablename as name, synctable.syncmode as syncmode, synctable.data as data from (select a.tableName, a.syncMode, b.data from ' + 
			@this_db_schema + 'replicasynctables a join ' + @tables + ' b on a.TableName = b.TableName) as synctable for xml auto,type) as synctables from ' +
			@this_db_schema + 'Replica as root for xml AUTO) as nvarchar(max))'
SELECT @bcp_queryout_cmd = 'bcp "' + @bcp_meta_query + '" queryout ' + @filepath + 'meta.xml -T -w ' + @serverName
INSERT #cmdResults EXEC @cmdResultCode = xp_cmdshell @bcp_queryout_cmd
				IF(@cmdResultCode = 1)
                BEGIN
					
					SET @cmdErrorText = 'Error occured on file "meta.xml".Exception message is "'
					OPEN cmdCursor
					FETCH FIRST FROM cmdCursor INTO @cmdText
					WHILE @@FETCH_STATUS = 0
					BEGIN

					SET @cmdErrorText = @cmdErrorText + @cmdText
					FETCH NEXT FROM cmdCursor into @cmdText

					END

					delete from #cmdResults
					close cmdCursor

					SET @cmdErrorText = @cmdErrorText +'".'

					SET @cmdQuery = 'del "' + @filepath + '*.*" /Q'
					INSERT #cmdResults EXEC @cmdResultCode = xp_cmdshell @cmdQuery
					IF(@cmdResultCode = 0)
					BEGIN
						SET @cmdErrorText = @cmdErrorText + 'Export folder is flushed.'
					END
					ELSE
					BEGIN
						SET @cmdErrorText = @cmdErrorText + 'Export folder cannot be flushed because of the next exception: "'
						OPEN cmdCursor
						FETCH FIRST FROM cmdCursor INTO @cmdText
						WHILE @@FETCH_STATUS = 0
						BEGIN

						SET @cmdErrorText = @cmdErrorText + @cmdText
						FETCH NEXT FROM cmdCursor into @cmdText

						END

						SET @cmdErrorText = @cmdErrorText + '".'

						DELETE FROM #cmdResults
						CLOSE cmdCursor
					END

					DELETE FROM #cmdResults

					INSERT INTO dbo.ReplicaSyncSessions(ReplicaId, spid, Start, STATUS, FINISH, ERRORTEXT)
					VALUES(@DestinationReplicaId, @spid, @starttime, 2, CURRENT_TIMESTAMP, @cmdErrorText)

					RAISERROR (@cmdErrorText, 11, 1) WITH NOWAIT
					RETURN

				END

SELECT @tables_query = 'DROP TABLE ' + @tables
EXEC(@tables_query)
--DROP TABLE ##tables

SELECT @bcp_queryout_cmd = 'bcp "select getdate()" queryout ' + @filepath + 'gmmq.package.end -T -w ' + @serverName
INSERT #cmdResults EXEC @cmdResultCode = xp_cmdshell @bcp_queryout_cmd

IF(@cmdResultCode = 1)
				BEGIN
					
					SET @cmdErrorText = 'Error occured on file "gmmq.package.end".Exception message is "'
					OPEN cmdCursor
					FETCH FIRST FROM cmdCursor INTO @cmdText
					WHILE @@FETCH_STATUS = 0
					BEGIN

					SET @cmdErrorText = @cmdErrorText + @cmdText
					FETCH NEXT FROM cmdCursor into @cmdText

					END

					delete from #cmdResults
					close cmdCursor

					SET @cmdErrorText = @cmdErrorText +'".'

					SET @cmdQuery = 'del "' + @filepath + '*.*" /Q'
					INSERT #cmdResults EXEC @cmdResultCode = xp_cmdshell @cmdQuery
					IF(@cmdResultCode = 0)
					BEGIN
						SET @cmdErrorText = @cmdErrorText + 'Export folder is flushed.'
					END
					ELSE
					BEGIN
						SET @cmdErrorText = @cmdErrorText + 'Export folder cannot be flushed because of the next exception: "'
						OPEN cmdCursor
						FETCH FIRST FROM cmdCursor INTO @cmdText
						WHILE @@FETCH_STATUS = 0
						BEGIN

						SET @cmdErrorText = @cmdErrorText + @cmdText
						FETCH NEXT FROM cmdCursor into @cmdText

						END

						SET @cmdErrorText = @cmdErrorText + '".'

						DELETE FROM #cmdResults
						CLOSE cmdCursor
					END

					DELETE FROM #cmdResults

					INSERT INTO dbo.ReplicaSyncSessions(ReplicaId, spid, Start, STATUS, FINISH, ERRORTEXT)
					VALUES(@DestinationReplicaId, @spid, @starttime, 2, CURRENT_TIMESTAMP, @cmdErrorText)

					RAISERROR (@cmdErrorText, 11, 1) WITH NOWAIT
					RETURN

				END

INSERT INTO dbo.ReplicaSyncSessions(ReplicaId, spid, Start, STATUS, FINISH, ERRORTEXT)
VALUES(@DestinationReplicaId, @spid, @starttime, 0, CURRENT_TIMESTAMP, 'Export completed successfully.')   

END