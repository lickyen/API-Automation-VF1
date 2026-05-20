SET NOCOUNT ON;

INSERT INTO dbo.GL_TrainingMaster
(
    TrainingName,
    TrainingLanguageID,
    Status,
    ApplicableDepts,
    TrainingType,
    CreatedDate,
    CreatedBy,
    Mandatory,
    No_Questions,
    CutOff_Marks,
    GlobalTraining,
    AudienceType,
    ExpiryDate,
    UpdatedBy,
    UpdatedDate,
    GeoLocation,
    SwipeTimer,
    ReAttempt
)
VALUES
(N'SQL Basics', 1, 'Active', 'IT,HR', 'Technical', GETDATE(), 'admin', 'Yes', 20, 12, 'Yes', 'Employee', DATEADD(DAY,90,GETDATE()), 'admin', GETDATE(), 'IN', '30', 'Allowed'),

(N'Advanced Excel', 1, 'Active', 'Finance', 'Skill', GETDATE(), 'admin', 'Yes', 25, 15, 'Yes', 'Employee', DATEADD(DAY,120,GETDATE()), 'admin', GETDATE(), 'IN', '40', 'Allowed'),

(N'Workplace Safety', 1, 'Active', 'All', 'Compliance', GETDATE(), 'hr', 'Yes', 15, 10, 'Yes', 'Employee', DATEADD(DAY,180,GETDATE()), 'hr', GETDATE(), 'IN', '20', 'NotAllowed'),

(N'Communication Skills', 1, 'Active', 'All', 'SoftSkill', GETDATE(), 'admin', 'Yes', 10, 7, 'Yes', 'Employee', DATEADD(DAY,60,GETDATE()), 'admin', GETDATE(), 'IN', '25', 'Allowed'),

(N'Cyber Security', 1, 'Active', 'IT', 'Security', GETDATE(), 'sec_team', 'Yes', 30, 20, 'Yes', 'Employee', DATEADD(DAY,365,GETDATE()), 'sec_team', GETDATE(), 'IN', '45', 'NotAllowed'),

(N'Java Basics', 1, 'Inactive', 'IT', 'Technical', GETDATE(), 'trainer1', 'No', 20, 12, 'No', 'Employee', DATEADD(DAY,100,GETDATE()), 'trainer1', GETDATE(), 'IN', '30', 'Allowed'),

(N'Leadership Training', 1, 'Active', 'Management', 'SoftSkill', GETDATE(), 'admin', 'Yes', 18, 14, 'Yes', 'Manager', DATEADD(DAY,200,GETDATE()), 'admin', GETDATE(), 'IN', '35', 'Allowed'),

(N'Data Analytics', 1, 'Active', 'IT,Finance', 'Technical', GETDATE(), 'data_team', 'Yes', 28, 18, 'Yes', 'Employee', DATEADD(DAY,150,GETDATE()), 'data_team', GETDATE(), 'IN', '50', 'NotAllowed'),

(N'HR Policies', 1, 'Active', 'HR', 'Compliance', GETDATE(), 'hr', 'Yes', 12, 9, 'Yes', 'Employee', DATEADD(DAY,90,GETDATE()), 'hr', GETDATE(), 'IN', '20', 'Allowed'),

(N'Cloud Computing', 1, 'Active', 'IT', 'Technical', GETDATE(), 'cloud_admin', 'Yes', 22, 16, 'Yes', 'Employee', DATEADD(DAY,300,GETDATE()), 'cloud_admin', GETDATE(), 'IN', '60', 'NotAllowed');