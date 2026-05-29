SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[NewBranchChecklistMaster](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[DepartmentGroup] [nvarchar](100) NULL,
	[Team] [nvarchar](100) NULL,
	[ChecklistName] [nvarchar](255) NULL,
	[ActionStatus] [nvarchar](255) NULL,
	[Checker] [nvarchar](50) NULL,
	[ParentChecklistId] [int] NULL,
	[PrereqName] [nvarchar](255) NULL,
	[PrereqStatus] [bit] NULL,
	[Status] [bit] NULL,
	[CreatedBy] [nvarchar](100) NULL,
	[CreatedDate] [datetime] NULL,
	[UpdatedBy] [nvarchar](100) NULL,
	[UpdatedDate] [datetime] NULL
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[NewBranchChecklistMaster] ADD PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
