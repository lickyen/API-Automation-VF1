EXEC sp_help Indus_Archived_Master;
GO

SELECT TOP 20 * FROM Indus_Archived_Master
GO

SELECT * FROM CIBIL_Correction_LoanCancellation;
GO

SELECT * FROM CIBIL_Correction_NonCustomer;
GO

SELECT * FROM CIBIL_Correction_WC_WS_SC;
GO

SELECT * FROM CIBIL_Correction_Logs;
GO

-- Delete all data
DELETE FROM CIBIL_Correction_Logs;
GO

-- Reseed identity back to 0 (next insert will start at 1)
DBCC CHECKIDENT ('CIBIL_Correction_Logs', RESEED, 0);
GO

-- Delete all data
DELETE FROM CIBIL_Correction_LoanCancellation;
GO

-- Reseed identity back to 0 (next insert will start at 1)
DBCC CHECKIDENT ('CIBIL_Correction_LoanCancellation', RESEED, 0);
GO

INSERT INTO CIBIL_Correction_LoanCancellation
(
    [Date],
    [LAN],
    [Customer_Name],
    [PENNANT_Status],
    [Remarks],
    [Created_By],
    [Created_Dt],
    [Updated_By],
    [Updated_Dt],
    [Status]
)
VALUES
('2024-08-12','0121SBML02100','Mr. JITENDRA SAINI','Cancelled',NULL,'SYSTEM',GETDATE(),NULL,NULL,'Pending'),
('2024-08-12','0156SBML02239','Mr. NANDHAKUMAR T','Cancelled',NULL,'SYSTEM',GETDATE(),NULL,NULL,'Pending'),
('2024-08-12','0276SBML00434','Mr. ROBERT SANTHIYAGU','Cancelled',NULL,'SYSTEM',GETDATE(),NULL,NULL,'Pending'),
('2024-08-12','0276SBML00436','Mr. NAGARAJ KALIYAPPAN','Cancelled',NULL,'SYSTEM',GETDATE(),NULL,NULL,'Pending'),
('2024-08-13','0126SBML03087','Mr. RATAN SINGH','Cancelled',NULL,'SYSTEM',GETDATE(),NULL,NULL,'Pending'),
('2024-08-13','0216SBML01771','Mr. krishna mahto','Cancelled',NULL,'SYSTEM',GETDATE(),NULL,NULL,'Pending'),
('2024-08-13','0047SBML03234','Mr. EGAMBARAM MUNISAMI','Cancelled',NULL,'SYSTEM',GETDATE(),NULL,NULL,'Pending'),
('2024-08-14','0153SBML02598','Mrs. MANIMEGALAI B','Cancelled',NULL,'SYSTEM',GETDATE(),NULL,NULL,'Pending'),
('2024-08-19','0033SBML01748','Mrs. SEETHALAKSHMI SENTHILKUMAR','Cancelled',NULL,'SYSTEM',GETDATE(),NULL,NULL,'Pending'),
('2024-08-19','0140SBML01155','Mr. KAMALBHAI JITUBHAI VYAS','Cancelled',NULL,'SYSTEM',GETDATE(),NULL,NULL,'Pending');
GO

SELECT *
FROM CIBIL_Correction_LoanCancellation
ORDER BY id;
GO