-- Clear existing data
DELETE FROM NOC_RoleStatusMap;

-- Reset identity
DBCC CHECKIDENT ('NOC_RoleStatusMap', RESEED, 0);

----------------------------------------------------
-- EDIT PERMISSIONS
----------------------------------------------------

-- COPSRMU
INSERT INTO NOC_RoleStatusMap (Role, Status, Permissions)
VALUES ('COPSRMU', 'File Indented', 'EDIT');

-- RMUCOPS (EDIT)  <-- NEW STATUS ADDED HERE
INSERT INTO NOC_RoleStatusMap (Role, Status, Permissions)
VALUES 
('RMUCOPS', 'File at NOC', 'EDIT');

-- NOCCOPS (EDIT)
INSERT INTO NOC_RoleStatusMap (Role, Status, Permissions)
VALUES 
('NOCCOPS', 'Ready for dispatch', 'EDIT'),
('NOCCOPS', 'Dispatched', 'EDIT'),
('NOCCOPS', 'Returned to RMU', 'EDIT'),
('NOCCOPS', 'Received with discrepancy', 'EDIT'),
('NOCCOPS', 'Document Collected', 'EDIT');

-- Branch Roles (EDIT)
INSERT INTO NOC_RoleStatusMap (Role, Status, Permissions)
VALUES 
('OPS', 'Delivered in Branch', 'EDIT'),
('OPS', 'Document Collected', 'EDIT'),
('OPS', 'Returned to HO', 'EDIT'),

('BM', 'Delivered in Branch', 'EDIT'),
('BM', 'Document Collected', 'EDIT'),
('BM', 'Returned to HO', 'EDIT'),

('CM', 'Delivered in Branch', 'EDIT'),
('CM', 'Document Collected', 'EDIT'),
('CM', 'Returned to HO', 'EDIT');

----------------------------------------------------
-- VIEW PERMISSIONS
----------------------------------------------------

-- COPSRMU
INSERT INTO NOC_RoleStatusMap (Role, Status, Permissions)
VALUES ('COPSRMU', 'Generate NOC', 'VIEW');

-- RMUCOPS (VIEW)  <-- NEW STATUS ADDED HERE
INSERT INTO NOC_RoleStatusMap (Role, Status, Permissions)
VALUES 
('RMUCOPS', 'File at NOC', 'VIEW');

-- NOCCOPS (VIEW)
INSERT INTO NOC_RoleStatusMap (Role, Status, Permissions)
VALUES 
('NOCCOPS', 'File Indented', 'VIEW'),
('NOCCOPS', 'Ready for dispatch', 'VIEW'),
('NOCCOPS', 'Dispatched', 'VIEW'),
('NOCCOPS', 'Returned to RMU', 'VIEW'),
('NOCCOPS', 'Received with discrepancy', 'VIEW'),
('NOCCOPS', 'Document Collected', 'VIEW');

-- Branch Roles (VIEW)
INSERT INTO NOC_RoleStatusMap (Role, Status, Permissions)
VALUES 
('OPS', 'Dispatched', 'VIEW'),
('OPS', 'Delivered in Branch', 'VIEW'),
('OPS', 'Document Collected', 'VIEW'),
('OPS', 'Returned to HO', 'VIEW'),

('BM', 'Dispatched', 'VIEW'),
('BM', 'Delivered in Branch', 'VIEW'),
('BM', 'Document Collected', 'VIEW'),
('BM', 'Returned to HO', 'VIEW'),

('CM', 'Dispatched', 'VIEW'),
('CM', 'Delivered in Branch', 'VIEW'),
('CM', 'Document Collected', 'VIEW'),
('CM', 'Returned to HO', 'VIEW');

----------------------------------------------------
-- VERIFY
----------------------------------------------------
SELECT *
FROM NOC_RoleStatusMap
ORDER BY Role, Permissions, Status;