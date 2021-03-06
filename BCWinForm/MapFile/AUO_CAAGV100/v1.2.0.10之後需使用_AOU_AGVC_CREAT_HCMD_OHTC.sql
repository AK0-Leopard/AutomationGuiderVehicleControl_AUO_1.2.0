
CREATE TABLE [dbo].[HCMD_OHTC](
	[CMD_ID] [char](64) NOT NULL,
	[VH_ID] [char](5) NOT NULL,
	[CARRIER_ID] [char](64) NULL,
	[CMD_ID_MCS] [char](64) NULL,
	[CMD_TPYE] [int] NOT NULL,
	[SOURCE] [char](64) NULL,
	[DESTINATION] [char](64) NULL,
	[PRIORITY] [int] NOT NULL,
	[CMD_START_TIME] [datetime2](7) NULL,
	[CMD_END_TIME] [datetime2](7) NOT NULL,
	[CMD_STAUS] [int] NOT NULL,
	[CMD_PROGRESS] [int] NOT NULL,
	[INTERRUPTED_REASON] [int] NULL,
	[ESTIMATED_TIME] [int] NOT NULL,
	[ESTIMATED_EXCESS_TIME] [int] NOT NULL,
	[REAL_CMP_TIME] [int] NULL,
 CONSTRAINT [PK_HCMD_OHTC] PRIMARY KEY CLUSTERED 
(
	[CMD_ID] ASC,
	[CMD_END_TIME] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
