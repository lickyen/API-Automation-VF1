SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ChecklistDependency](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ChecklistId] [int] NOT NULL,
	[DependsOnChecklistId] [int] NOT NULL,
	[CreatedDate] [datetime] NULL
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[ChecklistDependency] ADD PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
ALTER TABLE [dbo].[ChecklistDependency] ADD  DEFAULT (getdate()) FOR [CreatedDate]
GO
ALTER TABLE [dbo].[ChecklistDependency]  WITH CHECK ADD  CONSTRAINT [FK_CD_Checklist] FOREIGN KEY([ChecklistId])
REFERENCES [dbo].[NewBranchChecklistMaster] ([Id])
GO
ALTER TABLE [dbo].[ChecklistDependency] CHECK CONSTRAINT [FK_CD_Checklist]
GO
ALTER TABLE [dbo].[ChecklistDependency]  WITH CHECK ADD  CONSTRAINT [FK_CD_DependsOn] FOREIGN KEY([DependsOnChecklistId])
REFERENCES [dbo].[NewBranchChecklistMaster] ([Id])
GO
ALTER TABLE [dbo].[ChecklistDependency] CHECK CONSTRAINT [FK_CD_DependsOn]
GO
