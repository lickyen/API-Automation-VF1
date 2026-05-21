INSERT INTO dbo.NewBranchChecklistMaster
(
    Team,
    ChecklistName,
    Status,
    CreatedBy,
    ActionStatus
)
VALUES

-- =========================================
-- CHECKER ITEMS FOR ADMIN TASKS
-- =========================================

-- Premises status checker (BH)
('BH', 'Premises status verification', 'Pending/Approved', 1, 'Active'),

-- Interior status checker (BH)
('BH', 'Interior status verification', 'Pending/Approved', 1, 'Active'),

-- S&E Registration checker (Finance & Compliance)
('Finance & Compliance', 'S&E Registration verification', 'Pending/Approved', 1, 'Active'),

-- Rental agreement checker (Finance & Compliance)
('Finance & Compliance', 'Rental agreement verification', 'Pending/Approved', 1, 'Active');