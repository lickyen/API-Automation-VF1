/****** Object:  Table [dbo].[GL_TrainingMaster]    Script Date: 20-05-2026 17:01:08 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[GL_TrainingMaster](
	[TrainingId] [int] IDENTITY(1,1) NOT NULL,
	[TrainingName] [nvarchar](200) NULL,
	[TrainingLanguageID] [int] NOT NULL,
	[Status] [varchar](50) NOT NULL,
	[ApplicableDepts] [varchar](8000) NULL,
	[TrainingType] [varchar](1000) NULL,
	[CreatedDate] [datetime] NULL,
	[CreatedBy] [varchar](50) NULL,
	[Mandatory] [varchar](50) NULL,
	[No_Questions] [int] NULL,
	[CutOff_Marks] [int] NULL,
	[GlobalTraining] [varchar](20) NULL,
	[AudienceType] [varchar](50) NULL,
	[ExpiryDate] [date] NULL,
	[UpdatedBy] [varchar](50) NULL,
	[UpdatedDate] [datetime] NULL,
	[GeoLocation] [varchar](10) NULL,
	[SwipeTimer] [varchar](10) NULL,
	[ReAttempt] [varchar](10) NULL
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[TrainingCenterMaster]    Script Date: 20-05-2026 17:01:08 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[TrainingCenterMaster](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[BranchID] [varchar](20) NULL,
	[Location] [varchar](100) NULL,
	[Latitude] [varchar](100) NULL,
	[Longitude] [varchar](100) NULL,
	[Status] [varchar](100) NULL,
	[CreatedBy] [varchar](100) NULL,
	[CreatedDt] [datetime] NULL,
	[UpdatedBy] [varchar](100) NULL,
	[UpdatedDt] [datetime] NULL
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[TrainingEmployeeMaster]    Script Date: 20-05-2026 17:01:08 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[TrainingEmployeeMaster](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[EmpID] [varchar](50) NULL,
	[TrainingID] [varchar](50) NULL,
	[Location] [varchar](50) NULL,
	[Start_Date] [date] NULL,
	[Expiry_Date] [date] NULL,
	[Status] [varchar](50) NULL,
	[CreatedBy] [varchar](50) NULL,
	[CreatedDt] [datetime] NULL
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[TrainingCenterMaster] ADD  CONSTRAINT [DF_TrainingCenterMaster_CreatedDt]  DEFAULT (getdate()) FOR [CreatedDt]
GO
ALTER TABLE [dbo].[TrainingEmployeeMaster] ADD  CONSTRAINT [DF_TrainingEmployeeMaster_Status]  DEFAULT ('Active') FOR [Status]
GO
ALTER TABLE [dbo].[TrainingEmployeeMaster] ADD  CONSTRAINT [DF_TrainingEmployeeMaster_CreatedDt]  DEFAULT (getdate()) FOR [CreatedDt]
GO
/****** Object:  StoredProcedure [dbo].[SP_AssignTrainingEmployee]    Script Date: 20-05-2026 17:01:08 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[SP_AssignTrainingEmployee]
    @EmpID INT,
    @TrainingID INT,
    @Location NVARCHAR(255),
    @Start_Date DATETIME,
    @Expiry_Date DATETIME,
    @CreatedBy NVARCHAR(255),
    @Output INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        INSERT INTO TrainingEmployeeMaster(EmpID, TrainingID, Location, Start_Date, Expiry_Date, CreatedBy)
        VALUES (@EmpID, @TrainingID, @Location, @Start_Date, @Expiry_Date, @CreatedBy)

        SET @Output = 1 
    END TRY
    BEGIN CATCH
        SET @Output = 0 

    END CATCH
END
GO
/****** Object:  StoredProcedure [dbo].[SP_DisableTrainingEmployee]    Script Date: 20-05-2026 17:01:08 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE procedure [dbo].[SP_DisableTrainingEmployee]
@ID varchar(20)
as
begin
update TrainingEmployeeMaster set Status = 'Disabled' where id=@ID
end
GO
/****** Object:  StoredProcedure [dbo].[SP_GetEmployeeTrainings]    Script Date: 20-05-2026 17:01:08 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[SP_GetEmployeeTrainings]
AS
BEGIN
    SELECT TrainingId, TrainingName
    FROM GL_TrainingMaster
    WHERE AudienceType = 'Employee'
    and Status = 'Active'
    and ExpiryDate >= GETDATE()
END
GO
/****** Object:  StoredProcedure [dbo].[SP_GetTrainingCenters]    Script Date: 20-05-2026 17:01:08 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


  CREATE PROCEDURE [dbo].[SP_GetTrainingCenters]
  as
  begin
	select * from TrainingCenterMaster where status = 'Active'
  end


GO
/****** Object:  StoredProcedure [dbo].[SP_GetTrainingEmployeeList]    Script Date: 20-05-2026 17:01:08 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE procedure [dbo].[SP_GetTrainingEmployeeList]
@UserId varchar(20)
as
begin
SELECT *
FROM TrainingEmployeeMaster TE
join GL_TrainingMaster TM on TE.TrainingID = TM.TrainingId
WHERE TE.Status = 'Active'
and TM.Status = 'Active'
AND Expiry_Date >= CAST(GETDATE() AS date);

end
GO
/****** Object:  StoredProcedure [dbo].[SP_GetTrainingReport]    Script Date: 20-05-2026 17:01:08 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
create procedure [dbo].[SP_GetTrainingReport]
as
begin

select 
m.TrainingName as  [Training Name],
t.UserId as [User ID],
u.UserName as [User Name],
m.No_Questions as [Total Questions],
m.CutOff_Marks as [Cut-off],
t.TotalScore as [Score],
t.TestDecision as [Result],
t.TestStartDatetime as [Start Time],
t.TestCompletedDatetime as [Completed Time]
from Gurukul_Trascript t
left join Byte_UserMaster u on 	u.UserId = t.UserId
left join GL_TrainingMaster m on m.TrainingId = t.TrainingID
where t.TestStatus = 'Completed' 
and m.TrainingName is not null

union all

select 
m.TrainingName as  [Training Name],
t.UserId as [User ID],
u.UserName as [User Name],
m.No_Questions as [Total Questions],
m.CutOff_Marks as [Cut-off],
t.TotalScore as [Score],
t.TestDecision as [Result],
t.TestStartDatetime as [Start Time],
t.TestCompletedDatetime as [Completed Time]
from Gurukul_Trascript_History t
left join Byte_UserMaster u on 	u.UserId = t.UserId
left join GL_TrainingMaster m on m.TrainingId = t.TrainingID
where t.TestStatus = 'Completed' 
and m.TrainingName is not null

end
GO
