-- CREATE TABLE NewBranchChecklistMaster
-- (
--     Id INT IDENTITY(1,1) PRIMARY KEY,

--     Team NVARCHAR(100) NOT NULL,

--     ChecklistName NVARCHAR(250) NOT NULL,

--     ActionStatus NVARCHAR(100) NOT NULL, -- Done / Pending

--     Status NVARCHAR(100) NOT NULL,

--     CreatedBy INT NOT NULL,

--     CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),

--     UpdatedBy INT NULL,

--     UpdatedDate DATETIME NULL
-- );

SELECT * FROM NewBranchChecklistMaster