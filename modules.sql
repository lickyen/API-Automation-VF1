SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ModuleMaster](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ModuleCode] [varchar](100) NOT NULL,
	[ModuleName] [varchar](100) NOT NULL,
	[IsActive] [bit] NOT NULL,
	[CreatedBy] [varchar](10) NULL,
	[CreatedDt] [datetime] NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[ModuleCode] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UQ_ModuleMaster_ModuleCode] UNIQUE NONCLUSTERED 
(
	[ModuleCode] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[RoleMaster]    Script Date: 25-06-2026 10:32:07 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[RoleMaster](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Role] [varchar](20) NULL,
	[IsActive] [bit] NULL,
	[CreatedBy] [varchar](20) NULL,
	[CreatedDt] [datetime] NULL,
 CONSTRAINT [UQ_RoleMaster_Role] UNIQUE NONCLUSTERED 
(
	[Role] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[RoleModuleMaster]    Script Date: 25-06-2026 10:32:07 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[RoleModuleMaster](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Role] [varchar](20) NULL,
	[Module] [varchar](100) NULL,
	[Access] [varchar](20) NULL,
	[IsActive] [bit] NULL,
	[CreatedBy] [varchar](20) NULL,
	[CreatedDt] [datetime] NULL,
 CONSTRAINT [UQ_RoleModuleMaster_Role_Module] UNIQUE NONCLUSTERED 
(
	[Role] ASC,
	[Module] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[ModuleMaster] ADD  DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [dbo].[ModuleMaster] ADD  DEFAULT (getdate()) FOR [CreatedDt]
GO
ALTER TABLE [dbo].[RoleMaster] ADD  CONSTRAINT [DF_RoleMaster_IsActive]  DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [dbo].[RoleMaster] ADD  CONSTRAINT [DF_RoleMaster_CreatedDt]  DEFAULT (getdate()) FOR [CreatedDt]
GO
ALTER TABLE [dbo].[RoleModuleMaster] ADD  CONSTRAINT [DF_RoleModuleMaster_IsActive]  DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [dbo].[RoleModuleMaster] ADD  CONSTRAINT [DF__RoleToMod__Creat__681E60A5]  DEFAULT (getdate()) FOR [CreatedDt]
GO
ALTER TABLE [dbo].[RoleModuleMaster]  WITH CHECK ADD  CONSTRAINT [FK_RoleModuleMaster_Module] FOREIGN KEY([Module])
REFERENCES [dbo].[ModuleMaster] ([ModuleCode])
GO
ALTER TABLE [dbo].[RoleModuleMaster] CHECK CONSTRAINT [FK_RoleModuleMaster_Module]
GO
ALTER TABLE [dbo].[RoleModuleMaster]  WITH CHECK ADD  CONSTRAINT [FK_RoleModuleMaster_Role] FOREIGN KEY([Role])
REFERENCES [dbo].[RoleMaster] ([Role])
GO
ALTER TABLE [dbo].[RoleModuleMaster] CHECK CONSTRAINT [FK_RoleModuleMaster_Role]
GO
