SELECT *
FROM dbo.NewBranchChecklistMaster
ORDER BY Team, Id;

-- INSERT INTO dbo.NewBranchChecklistMaster
-- (
--     Team,
--     ChecklistName,
--     Status,
--     CreatedBy,
--     ActionStatus
-- )
-- VALUES

-- -- =========================
-- -- ADMIN
-- -- =========================
-- ('Admin', 'Premises status', 'Ready/Pending', 1, 'Active'),
-- ('Admin', 'Interior status', 'Done/Pending', 1, 'Active'),
-- ('Admin', 'Safe status', 'Received/Pending', 1, 'Active'),
-- ('Admin1', 'KEY', 'Handedover/Pending', 1, 'Active'),
-- ('Admin', 'Registers', 'Received/Pending', 1, 'Active'),
-- ('Admin', 'S&E Registration', 'Received/Pending', 1, 'Active'),
-- ('Admin', 'Rental agreement status', 'Received/Pending', 1, 'Active'),
-- ('Admin', 'Premises ready , branch status to go to IT', 'Completed/Pending', 1, 'Active'),

-- -- =========================
-- -- IT
-- -- =========================
-- ('IT', 'Branch code creation', 'Pending/Created', 1, 'Active'),
-- ('IT', 'Asset dispatch', 'Pending/Received', 1, 'Active'),
-- ('IT', 'IT asset dispatch go ahead', 'Go ahead/Wait', 1, 'Active'),
-- ('IT', 'Key dispatch go ahead', 'Go ahead/Wait', 1, 'Active'),

-- -- =========================
-- -- FINANCE & COMPLIANCE
-- -- =========================
-- ('Finance & Compliance', 'Navision code creation', 'Pending/Created', 1, 'Active'),
-- ('Finance & Compliance', 'GST Compliance', 'Pending/Created', 1, 'Active'),

-- -- =========================
-- -- OPS
-- -- =========================
-- ('Ops', 'Training', 'Pending/Completed', 1, 'Active'),
-- ('Ops', 'Collateral dispatch', 'Pending/Completed', 1, 'Active'),

-- -- =========================
-- -- HR
-- -- =========================
-- ('HR', 'BM', 'EMP CODE', 1, 'Active'),
-- ('HR', 'CM', 'EMP CODE', 1, 'Active'),
-- ('HR', 'CRO', 'EMP CODE', 1, 'Active'),
-- ('HR', 'DSO', 'EMP CODE', 1, 'Active');