SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[NewBranchMaster](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](200) NOT NULL,
	[State] [nvarchar](100) NULL,
	[Zone] [nvarchar](100) NULL,
	[Region] [nvarchar](100) NULL,
	[Megazone] [nvarchar](100) NULL,
	[Status] [nvarchar](50) NULL,
	[AdminStatus] [nvarchar](50) NULL,
	[AdminDate] [datetime] NULL,
	[HRStatus] [nvarchar](50) NULL,
	[HRDate] [datetime] NULL,
	[ITStatus] [nvarchar](50) NULL,
	[ITDate] [datetime] NULL,
	[RCHRCBStatus] [nvarchar](50) NULL,
	[RCHRCBDate] [datetime] NULL,
	[FinanceStatus] [nvarchar](50) NULL,
	[FinanceDate] [datetime] NULL,
	[MarketingStatus] [nvarchar](50) NULL,
	[MarketingDate] [datetime] NULL,
	[COPSStatus] [nvarchar](50) NULL,
	[COPSDate] [datetime] NULL,
	[CreatedBy] [varchar](50) NOT NULL,
	[CreatedDate] [datetime] NOT NULL,
	[UpdatedBy] [varchar](50) NULL,
	[UpdatedDate] [datetime] NULL
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[NewBranchMaster] ADD PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
ALTER TABLE [dbo].[NewBranchMaster] ADD  DEFAULT (getdate()) FOR [CreatedDate]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TRIGGER [dbo].[TRG_UpdateBranchMasterStatus]
ON [dbo].[NewBranchMaster]
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE NBM
    SET
        Status = 'Completed',
        UpdatedDate = GETDATE(),
        UpdatedBy = SUSER_SNAME()
    FROM NewBranchMaster NBM
    WHERE
        ISNULL(AdminStatus, 'Pending') <> 'Pending'
        AND ISNULL(HRStatus, 'Pending') <> 'Pending'
        AND ISNULL(ITStatus, 'Pending') <> 'Pending'
        AND ISNULL(RCHRCBStatus, 'Pending') <> 'Pending'
        AND ISNULL(FinanceStatus, 'Pending') <> 'Pending'
        AND ISNULL(MarketingStatus, 'Pending') <> 'Pending'
        AND ISNULL(COPSStatus, 'Pending') <> 'Pending'
        AND Status <> 'Completed';
END;
GO
ALTER TABLE [dbo].[NewBranchMaster] ENABLE TRIGGER [TRG_UpdateBranchMasterStatus]
GO
