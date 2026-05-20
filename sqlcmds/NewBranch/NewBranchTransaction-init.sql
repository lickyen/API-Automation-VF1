-- CREATE TABLE NewBranchTransaction
-- (
--     Id INT IDENTITY(1,1) PRIMARY KEY,

--     Tid INT NOT NULL, -- Branch Id from NewBranchMaster

--     ChecklistId INT NOT NULL, -- FK from NewBranchChecklistMaster

--     CurrentStatus NVARCHAR(100) NOT NULL,

--     OverallStatus NVARCHAR(100) NOT NULL,

--     Remarks NVARCHAR(500) NULL,

--     CreatedBy INT NOT NULL,

--     CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),

--     UpdatedBy INT NULL,

--     UpdatedDate DATETIME NULL,

--     CONSTRAINT FK_NewBranchTransaction_Branch
--     FOREIGN KEY (Tid)
--     REFERENCES NewBranchMaster(Id),

--     CONSTRAINT FK_NewBranchTransaction_Checklist
--     FOREIGN KEY (ChecklistId)
--     REFERENCES NewBranchChecklistMaster(Id)
-- );

SELECT * FROM NewBranchTransaction