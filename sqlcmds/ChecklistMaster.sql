SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[NewBranchChecklistMaster](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Team] [nvarchar](100) NOT NULL,
	[ChecklistName] [nvarchar](250) NOT NULL,
	[Status] [nvarchar](100) NOT NULL,
	[CreatedBy] [varchar](50) NOT NULL,
	[CreatedDate] [datetime] NOT NULL,
	[UpdatedBy] [varchar](50) NULL,
	[UpdatedDate] [datetime] NULL,
	[ActionStatus] [nvarchar](100) NOT NULL
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[NewBranchChecklistMaster] ADD PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
ALTER TABLE [dbo].[NewBranchChecklistMaster] ADD  DEFAULT (getdate()) FOR [CreatedDate]
GO
ALTER TABLE [dbo].[NewBranchChecklistMaster] ADD  DEFAULT ('Pending') FOR [ActionStatus]
GO
