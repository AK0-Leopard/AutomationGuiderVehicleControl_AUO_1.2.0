

GO
/*
資料表 [dbo].[ACEID] 中資料行 CEID 的型別目前是  CHAR (4) NOT NULL，但正要變更成  CHAR (3) NOT NULL。這可能導致資料遺失。

資料表 [dbo].[ACEID] 中資料行 RPTID 的型別目前是  CHAR (4) NOT NULL，但正要變更成  CHAR (3) NOT NULL。這可能導致資料遺失。
*/

IF EXISTS (select top 1 1 from [dbo].[ACEID])
    RAISERROR (N'偵測到資料列。結構描述更新即將終止，因為可能造成資料遺失。', 16, 127) WITH NOWAIT

GO
/*
資料表 [dbo].[ACMD_OHTC] 中資料行 VH_ID 的型別目前是  CHAR (8) NOT NULL，但正要變更成  CHAR (5) NOT NULL。這可能導致資料遺失。
*/

IF EXISTS (select top 1 1 from [dbo].[ACMD_OHTC])
    RAISERROR (N'偵測到資料列。結構描述更新即將終止，因為可能造成資料遺失。', 16, 127) WITH NOWAIT

GO
/*
資料表 [dbo].[AMCSREPORTQUEUE] 中資料行 STREAMFUNCTION_NAME 的型別目前是  VARCHAR (60) NOT NULL，但正要變更成  VARCHAR (50) NOT NULL。這可能導致資料遺失。

資料表 [dbo].[AMCSREPORTQUEUE] 中資料行 VEHICLE_ID 的型別目前是  CHAR (8) NULL，但正要變更成  CHAR (5) NULL。這可能導致資料遺失。
*/

IF EXISTS (select top 1 1 from [dbo].[AMCSREPORTQUEUE])
    RAISERROR (N'偵測到資料列。結構描述更新即將終止，因為可能造成資料遺失。', 16, 127) WITH NOWAIT

GO
/*
資料表 [dbo].[ARPTID] 中資料行 RPTID 的型別目前是  CHAR (4) NOT NULL，但正要變更成  CHAR (3) NOT NULL。這可能導致資料遺失。

資料表 [dbo].[ARPTID] 中資料行 VID 的型別目前是  CHAR (4) NOT NULL，但正要變更成  CHAR (3) NOT NULL。這可能導致資料遺失。
*/

IF EXISTS (select top 1 1 from [dbo].[ARPTID])
    RAISERROR (N'偵測到資料列。結構描述更新即將終止，因為可能造成資料遺失。', 16, 127) WITH NOWAIT

GO
/*
資料表 [dbo].[ASYSEXCUTEQUALITY] 中資料行 VH_ID 的型別目前是  CHAR (8) NULL，但正要變更成  CHAR (5) NULL。這可能導致資料遺失。
*/

IF EXISTS (select top 1 1 from [dbo].[ASYSEXCUTEQUALITY])
    RAISERROR (N'偵測到資料列。結構描述更新即將終止，因為可能造成資料遺失。', 16, 127) WITH NOWAIT

GO
/*
資料表 [dbo].[AVEHICLE] 中資料行 VEHICLE_ID 的型別目前是  CHAR (8) NOT NULL，但正要變更成  CHAR (5) NOT NULL。這可能導致資料遺失。
*/

IF EXISTS (select top 1 1 from [dbo].[AVEHICLE])
    RAISERROR (N'偵測到資料列。結構描述更新即將終止，因為可能造成資料遺失。', 16, 127) WITH NOWAIT

GO
/*
資料表 [dbo].[AVIDINFO] 中資料行 EQ_ID 的型別目前是  CHAR (8) NOT NULL，但正要變更成  CHAR (5) NOT NULL。這可能導致資料遺失。
*/

IF EXISTS (select top 1 1 from [dbo].[AVIDINFO])
    RAISERROR (N'偵測到資料列。結構描述更新即將終止，因為可能造成資料遺失。', 16, 127) WITH NOWAIT

GO
PRINT N'正在卸除 [dbo].[DF_AVEHICLE_BATTERYCAPACITY]...';


GO
ALTER TABLE [dbo].[AVEHICLE] DROP CONSTRAINT [DF_AVEHICLE_BATTERYCAPACITY];


GO
PRINT N'正在卸除 [dbo].[DF_AVEHICLE_STEERINGWHEELANGLE]...';


GO
ALTER TABLE [dbo].[AVEHICLE] DROP CONSTRAINT [DF_AVEHICLE_STEERINGWHEELANGLE];


GO
PRINT N'正在卸除 [dbo].[DF_AVEHICLE_IS_INSTALLED_1]...';


GO
ALTER TABLE [dbo].[AVEHICLE] DROP CONSTRAINT [DF_AVEHICLE_IS_INSTALLED_1];


GO
PRINT N'正在卸除 [dbo].[DF_AVIDINFO_REPLACE]...';


GO
ALTER TABLE [dbo].[AVIDINFO] DROP CONSTRAINT [DF_AVIDINFO_REPLACE];


GO
PRINT N'開始重建資料表 [dbo].[ACEID]...';


GO
BEGIN TRANSACTION;

SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;

SET XACT_ABORT ON;

CREATE TABLE [dbo].[tmp_ms_xx_ACEID] (
    [CEID]      CHAR (3)      NOT NULL,
    [RPTID]     CHAR (3)      NOT NULL,
    [ORDER_NUM] INT           NOT NULL,
    [NAME]      CHAR (20)     NULL,
    [UPD_TIME]  DATETIME2 (7) NULL,
    CONSTRAINT [tmp_ms_xx_constraint_PK_ACEID1] PRIMARY KEY CLUSTERED ([CEID] ASC, [RPTID] ASC)
);

IF EXISTS (SELECT TOP 1 1 
           FROM   [dbo].[ACEID])
    BEGIN
        INSERT INTO [dbo].[tmp_ms_xx_ACEID] ([CEID], [RPTID], [ORDER_NUM], [NAME], [UPD_TIME])
        SELECT   [CEID],
                 [RPTID],
                 [ORDER_NUM],
                 [NAME],
                 [UPD_TIME]
        FROM     [dbo].[ACEID]
        ORDER BY [CEID] ASC, [RPTID] ASC;
    END

DROP TABLE [dbo].[ACEID];

EXECUTE sp_rename N'[dbo].[tmp_ms_xx_ACEID]', N'ACEID';

EXECUTE sp_rename N'[dbo].[tmp_ms_xx_constraint_PK_ACEID1]', N'PK_ACEID', N'OBJECT';

COMMIT TRANSACTION;

SET TRANSACTION ISOLATION LEVEL READ COMMITTED;


GO
PRINT N'正在更改 [dbo].[ACMD_OHTC]...';


GO
ALTER TABLE [dbo].[ACMD_OHTC] ALTER COLUMN [VH_ID] CHAR (5) NOT NULL;


GO
PRINT N'正在更改 [dbo].[AMCSREPORTQUEUE]...';


GO
ALTER TABLE [dbo].[AMCSREPORTQUEUE] ALTER COLUMN [STREAMFUNCTION_NAME] VARCHAR (50) NOT NULL;

ALTER TABLE [dbo].[AMCSREPORTQUEUE] ALTER COLUMN [VEHICLE_ID] CHAR (5) NULL;


GO
PRINT N'開始重建資料表 [dbo].[ARPTID]...';


GO
BEGIN TRANSACTION;

SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;

SET XACT_ABORT ON;

CREATE TABLE [dbo].[tmp_ms_xx_ARPTID] (
    [RPTID]     CHAR (3)      NOT NULL,
    [VID]       CHAR (3)      NOT NULL,
    [ORDER_NUM] INT           NOT NULL,
    [NAME]      CHAR (20)     NULL,
    [UPD_TIME]  SMALLDATETIME NULL,
    CONSTRAINT [tmp_ms_xx_constraint_PK_ARPTID1] PRIMARY KEY CLUSTERED ([RPTID] ASC, [VID] ASC)
);

IF EXISTS (SELECT TOP 1 1 
           FROM   [dbo].[ARPTID])
    BEGIN
        INSERT INTO [dbo].[tmp_ms_xx_ARPTID] ([RPTID], [VID], [ORDER_NUM], [NAME], [UPD_TIME])
        SELECT   [RPTID],
                 [VID],
                 [ORDER_NUM],
                 [NAME],
                 [UPD_TIME]
        FROM     [dbo].[ARPTID]
        ORDER BY [RPTID] ASC, [VID] ASC;
    END

DROP TABLE [dbo].[ARPTID];

EXECUTE sp_rename N'[dbo].[tmp_ms_xx_ARPTID]', N'ARPTID';

EXECUTE sp_rename N'[dbo].[tmp_ms_xx_constraint_PK_ARPTID1]', N'PK_ARPTID', N'OBJECT';

COMMIT TRANSACTION;

SET TRANSACTION ISOLATION LEVEL READ COMMITTED;


GO
PRINT N'正在更改 [dbo].[ASYSEXCUTEQUALITY]...';


GO
ALTER TABLE [dbo].[ASYSEXCUTEQUALITY] ALTER COLUMN [VH_ID] CHAR (5) NULL;


GO
PRINT N'開始重建資料表 [dbo].[AVEHICLE]...';


GO
BEGIN TRANSACTION;

SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;

SET XACT_ABORT ON;

CREATE TABLE [dbo].[tmp_ms_xx_AVEHICLE] (
    [VEHICLE_ID]              CHAR (5)      NOT NULL,
    [VEHICLE_TYPE]            INT           NOT NULL,
    [CUR_SEG_ID]              CHAR (5)      NULL,
    [CUR_SEC_ID]              CHAR (5)      NULL,
    [CUR_ADR_ID]              CHAR (5)      NULL,
    [SEC_ENTRY_TIME]          DATETIME2 (7) NULL,
    [ACC_SEC_DIST]            FLOAT (53)    NOT NULL,
    [MODE_STATUS]             INT           NOT NULL,
    [ACT_STATUS]              INT           NOT NULL,
    [MCS_CMD]                 CHAR (64)     NULL,
    [OHTC_CMD]                CHAR (64)     NULL,
    [BLOCK_PAUSE]             INT           NOT NULL,
    [CMD_PAUSE]               INT           NOT NULL,
    [OBS_PAUSE]               INT           NOT NULL,
    [HID_PAUSE]               INT           NOT NULL,
    [ERROR]                   INT           NOT NULL,
    [EARTHQUAKE_PAUSE]        INT           NOT NULL,
    [SAFETY_DOOR_PAUSE]       INT           NOT NULL,
    [OHXC_OBS_PAUSE]          INT           NOT NULL,
    [OHXC_BLOCK_PAUSE]        INT           NOT NULL,
    [OBS_DIST]                INT           NOT NULL,
    [HAS_CST]                 INT           NOT NULL,
    [CST_ID]                  CHAR (64)     NULL,
    [UPD_TIME]                DATETIME2 (7) NULL,
    [VEHICLE_ACC_DIST]        INT           NOT NULL,
    [MANT_ACC_DIST]           INT           NOT NULL,
    [MANT_DATE]               DATETIME2 (7) NULL,
    [GRIP_COUNT]              INT           NOT NULL,
    [GRIP_MANT_COUNT]         INT           NOT NULL,
    [GRIP_MANT_DATE]          DATETIME2 (7) NULL,
    [NODE_ADR]                CHAR (5)      NULL,
    [IS_PARKING]              BIT           NOT NULL,
    [PARK_TIME]               DATETIME2 (7) NULL,
    [PARK_ADR_ID]             CHAR (5)      NULL,
    [IS_CYCLING]              BIT           NOT NULL,
    [CYCLERUN_TIME]           DATETIME2 (7) NULL,
    [CYCLERUN_ID]             CHAR (10)     NULL,
    [BATTERYCAPACITY]         INT           CONSTRAINT [DF_AVEHICLE_BATTERYCAPACITY] DEFAULT ((0)) NOT NULL,
    [LAST_FULLY_CHARGED_TIME] DATETIME2 (7) NULL,
    [STEERINGWHEELANGLE]      INT           CONSTRAINT [DF_AVEHICLE_STEERINGWHEELANGLE] DEFAULT ((0)) NOT NULL,
    [IS_INSTALLED]            BIT           CONSTRAINT [DF_AVEHICLE_IS_INSTALLED_1] DEFAULT ((0)) NOT NULL,
    [INSTALLED_TIME]          DATETIME2 (7) NULL,
    [REMOVED_TIME]            DATETIME2 (7) NULL,
    CONSTRAINT [tmp_ms_xx_constraint_PK_AVEHICLE1] PRIMARY KEY CLUSTERED ([VEHICLE_ID] ASC)
);

IF EXISTS (SELECT TOP 1 1 
           FROM   [dbo].[AVEHICLE])
    BEGIN
        INSERT INTO [dbo].[tmp_ms_xx_AVEHICLE] ([VEHICLE_ID], [VEHICLE_TYPE], [CUR_SEG_ID], [CUR_SEC_ID], [CUR_ADR_ID], [SEC_ENTRY_TIME], [ACC_SEC_DIST], [MODE_STATUS], [ACT_STATUS], [MCS_CMD], [OHTC_CMD], [BLOCK_PAUSE], [CMD_PAUSE], [OBS_PAUSE], [HID_PAUSE], [ERROR], [EARTHQUAKE_PAUSE], [SAFETY_DOOR_PAUSE], [OHXC_OBS_PAUSE], [OHXC_BLOCK_PAUSE], [OBS_DIST], [HAS_CST], [CST_ID], [UPD_TIME], [VEHICLE_ACC_DIST], [MANT_ACC_DIST], [MANT_DATE], [GRIP_COUNT], [GRIP_MANT_COUNT], [GRIP_MANT_DATE], [NODE_ADR], [IS_PARKING], [PARK_TIME], [PARK_ADR_ID], [IS_CYCLING], [CYCLERUN_TIME], [CYCLERUN_ID], [BATTERYCAPACITY], [LAST_FULLY_CHARGED_TIME], [STEERINGWHEELANGLE], [IS_INSTALLED], [INSTALLED_TIME], [REMOVED_TIME])
        SELECT   [VEHICLE_ID],
                 [VEHICLE_TYPE],
                 [CUR_SEG_ID],
                 [CUR_SEC_ID],
                 [CUR_ADR_ID],
                 [SEC_ENTRY_TIME],
                 [ACC_SEC_DIST],
                 [MODE_STATUS],
                 [ACT_STATUS],
                 [MCS_CMD],
                 [OHTC_CMD],
                 [BLOCK_PAUSE],
                 [CMD_PAUSE],
                 [OBS_PAUSE],
                 [HID_PAUSE],
                 [ERROR],
                 [EARTHQUAKE_PAUSE],
                 [SAFETY_DOOR_PAUSE],
                 [OHXC_OBS_PAUSE],
                 [OHXC_BLOCK_PAUSE],
                 [OBS_DIST],
                 [HAS_CST],
                 [CST_ID],
                 [UPD_TIME],
                 [VEHICLE_ACC_DIST],
                 [MANT_ACC_DIST],
                 [MANT_DATE],
                 [GRIP_COUNT],
                 [GRIP_MANT_COUNT],
                 [GRIP_MANT_DATE],
                 [NODE_ADR],
                 [IS_PARKING],
                 [PARK_TIME],
                 [PARK_ADR_ID],
                 [IS_CYCLING],
                 [CYCLERUN_TIME],
                 [CYCLERUN_ID],
                 [BATTERYCAPACITY],
                 [LAST_FULLY_CHARGED_TIME],
                 [STEERINGWHEELANGLE],
                 [IS_INSTALLED],
                 [INSTALLED_TIME],
                 [REMOVED_TIME]
        FROM     [dbo].[AVEHICLE]
        ORDER BY [VEHICLE_ID] ASC;
    END

DROP TABLE [dbo].[AVEHICLE];

EXECUTE sp_rename N'[dbo].[tmp_ms_xx_AVEHICLE]', N'AVEHICLE';

EXECUTE sp_rename N'[dbo].[tmp_ms_xx_constraint_PK_AVEHICLE1]', N'PK_AVEHICLE', N'OBJECT';

COMMIT TRANSACTION;

SET TRANSACTION ISOLATION LEVEL READ COMMITTED;


GO
PRINT N'開始重建資料表 [dbo].[AVIDINFO]...';


GO
BEGIN TRANSACTION;

SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;

SET XACT_ABORT ON;

CREATE TABLE [dbo].[tmp_ms_xx_AVIDINFO] (
    [EQ_ID]                  CHAR (5)      NOT NULL,
    [MCS_CARRIER_ID]         CHAR (64)     NULL,
    [CARRIER_ID]             CHAR (64)     NULL,
    [CARRIER_LOC]            CHAR (64)     NULL,
    [CARRIER_INSTALLED_TIME] DATETIME2 (7) NULL,
    [COMMAND_ID]             CHAR (64)     NULL,
    [SOURCEPORT]             CHAR (64)     NULL,
    [DESTPORT]               CHAR (64)     NULL,
    [PRIORITY]               INT           NOT NULL,
    [RESULT_CODE]            INT           NOT NULL,
    [VEHICLE_STATE]          INT           NOT NULL,
    [COMMAND_TYPE]           CHAR (64)     NULL,
    [ALARM_ID]               CHAR (64)     NULL,
    [ALARM_TEXT]             CHAR (64)     NULL,
    [UNIT_ID]                CHAR (64)     NULL,
    [PORT_ID]                CHAR (64)     NULL,
    [REPLACE]                INT           CONSTRAINT [DF_AVIDINFO_REPLACE] DEFAULT ((0)) NOT NULL,
    CONSTRAINT [tmp_ms_xx_constraint_PK_AVIDINFO1] PRIMARY KEY CLUSTERED ([EQ_ID] ASC)
);

IF EXISTS (SELECT TOP 1 1 
           FROM   [dbo].[AVIDINFO])
    BEGIN
        INSERT INTO [dbo].[tmp_ms_xx_AVIDINFO] ([EQ_ID], [MCS_CARRIER_ID], [CARRIER_ID], [CARRIER_LOC], [CARRIER_INSTALLED_TIME], [COMMAND_ID], [SOURCEPORT], [DESTPORT], [PRIORITY], [RESULT_CODE], [VEHICLE_STATE], [COMMAND_TYPE], [ALARM_ID], [ALARM_TEXT], [UNIT_ID], [PORT_ID], [REPLACE])
        SELECT   [EQ_ID],
                 [MCS_CARRIER_ID],
                 [CARRIER_ID],
                 [CARRIER_LOC],
                 [CARRIER_INSTALLED_TIME],
                 [COMMAND_ID],
                 [SOURCEPORT],
                 [DESTPORT],
                 [PRIORITY],
                 [RESULT_CODE],
                 [VEHICLE_STATE],
                 [COMMAND_TYPE],
                 [ALARM_ID],
                 [ALARM_TEXT],
                 [UNIT_ID],
                 [PORT_ID],
                 [REPLACE]
        FROM     [dbo].[AVIDINFO]
        ORDER BY [EQ_ID] ASC;
    END

DROP TABLE [dbo].[AVIDINFO];

EXECUTE sp_rename N'[dbo].[tmp_ms_xx_AVIDINFO]', N'AVIDINFO';

EXECUTE sp_rename N'[dbo].[tmp_ms_xx_constraint_PK_AVIDINFO1]', N'PK_AVIDINFO', N'OBJECT';

COMMIT TRANSACTION;

SET TRANSACTION ISOLATION LEVEL READ COMMITTED;


GO
PRINT N'正在重新整理 [dbo].[VACMD_MCS]...';


GO
EXECUTE sp_refreshsqlmodule N'[dbo].[VACMD_MCS]';


GO
PRINT N'更新完成。';


GO
