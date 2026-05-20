SELECT * FROM GoldLoanLeadMaster;

SELECT * FROM sys.procedures;

-- /* =========================================================
--    TABLE : GoldLoanLeadMaster
--    ========================================================= */

-- SET ANSI_NULLS ON
-- GO

-- SET QUOTED_IDENTIFIER ON
-- GO

-- IF NOT EXISTS (
--     SELECT 1
--     FROM sys.tables
--     WHERE name = 'GoldLoanLeadMaster'
-- )
-- BEGIN

--     CREATE TABLE GoldLoanLeadMaster
--     (
--         Id INT IDENTITY(1,1) PRIMARY KEY,

--         VendorID INT NOT NULL,

--         Name NVARCHAR(100) NOT NULL,

--         MobileNumber NVARCHAR(20) NOT NULL,

--         Caratage NVARCHAR(10) NOT NULL,

--         ApproximateWeight DECIMAL(10,2) NOT NULL,

--         GoldLoanRequiredFor NVARCHAR(250) NOT NULL,

--         CreatedBy NVARCHAR(50) NOT NULL,

--         CreatedDate DATETIME NOT NULL
--             DEFAULT DATEADD(MINUTE, 330, GETUTCDATE()),

--         UpdatedBy NVARCHAR(50) NULL,

--         UpdatedDate DATETIME NULL,

--         Status BIT NOT NULL DEFAULT 1
--     )

-- END
-- GO


-- /* =========================================================
--    FOREIGN KEY
--    ========================================================= */

-- IF NOT EXISTS (
--     SELECT 1
--     FROM sys.foreign_keys
--     WHERE name = 'FK_GoldLoanLeadMaster_GoldLoanVendorMaster'
-- )
-- BEGIN

--     ALTER TABLE GoldLoanLeadMaster
--     ADD CONSTRAINT FK_GoldLoanLeadMaster_GoldLoanVendorMaster
--     FOREIGN KEY (VendorID)
--     REFERENCES GoldLoanVendorMaster(VendorID)

-- END
-- GO


-- /* =========================================================
--    DROP OLD PROCEDURES
--    ========================================================= */

-- IF EXISTS (
--     SELECT * FROM sys.objects
--     WHERE type = 'P'
--     AND name = 'sp_getGoldLoanLeadsList'
-- )
-- DROP PROCEDURE sp_getGoldLoanLeadsList
-- GO

-- IF EXISTS (
--     SELECT * FROM sys.objects
--     WHERE type = 'P'
--     AND name = 'sp_getGoldLoanLead'
-- )
-- DROP PROCEDURE sp_getGoldLoanLead
-- GO

-- IF EXISTS (
--     SELECT * FROM sys.objects
--     WHERE type = 'P'
--     AND name = 'sp_insertGoldLoanLead'
-- )
-- DROP PROCEDURE sp_insertGoldLoanLead
-- GO


-- /* =========================================================
--    PROCEDURE : sp_getGoldLoanLeadsList
--    ========================================================= */

-- CREATE PROCEDURE sp_getGoldLoanLeadsList
-- AS
-- BEGIN
--     SET NOCOUNT ON;

--     SELECT

--         GL.Id,
--         GL.VendorID,

--         /* Vendor Object */

--         V.VendorID AS VendorId,
--         V.VendorName,
--         V.ContactNumber,
--         V.BusinessShop,
--         V.BusinessType,
--         V.BranchID,
--         V.BranchName,
--         V.Region,

--         /* Lead Details */

--         GL.Name,
--         GL.MobileNumber,
--         GL.Caratage,
--         GL.ApproximateWeight,
--         GL.GoldLoanRequiredFor,

--         GL.CreatedBy,
--         GL.CreatedDate,
--         GL.UpdatedBy,
--         GL.UpdatedDate,
--         GL.Status

--     FROM GoldLoanLeadMaster GL

--     INNER JOIN GoldLoanVendorMaster V
--         ON GL.VendorID = V.VendorID

--     WHERE GL.Status = 1
-- END
-- GO


-- /* =========================================================
--    PROCEDURE : sp_getGoldLoanLead
--    ========================================================= */

-- CREATE PROCEDURE sp_getGoldLoanLead
--     @Id INT
-- AS
-- BEGIN
--     SET NOCOUNT ON;

--     SELECT

--         GL.Id,
--         GL.VendorID,

--         /* Vendor Object */

--         V.VendorID AS VendorId,
--         V.VendorName,
--         V.ContactNumber,
--         V.BusinessShop,
--         V.BusinessType,
--         V.BranchID,
--         V.BranchName,
--         V.Region,

--         /* Lead Details */

--         GL.Name,
--         GL.MobileNumber,
--         GL.Caratage,
--         GL.ApproximateWeight,
--         GL.GoldLoanRequiredFor,

--         GL.CreatedBy,
--         GL.CreatedDate,
--         GL.UpdatedBy,
--         GL.UpdatedDate,
--         GL.Status

--     FROM GoldLoanLeadMaster GL

--     INNER JOIN GoldLoanVendorMaster V
--         ON GL.VendorID = V.VendorID

--     WHERE GL.Id = @Id
--       AND GL.Status = 1
-- END
-- GO


-- /* =========================================================
--    PROCEDURE : sp_insertGoldLoanLead
--    ========================================================= */

-- CREATE PROCEDURE sp_insertGoldLoanLead
-- (
--     @VendorID INT,
--     @Name NVARCHAR(100),
--     @MobileNumber NVARCHAR(20),
--     @Caratage NVARCHAR(10),
--     @ApproximateWeight DECIMAL(10,2),
--     @GoldLoanRequiredFor NVARCHAR(250),
--     @CreatedBy NVARCHAR(50),
--     @Output INT OUTPUT
-- )
-- AS
-- BEGIN

--     SET NOCOUNT ON;

--     BEGIN TRY

--         /* =========================================
--            VALIDATE VENDOR EXISTS
--            ========================================= */

--         IF NOT EXISTS
--         (
--             SELECT 1
--             FROM GoldLoanVendorMaster
--             WHERE VendorID = @VendorID
--               AND Status = 1
--         )
--         BEGIN
--             SET @Output = -1;
--             RETURN;
--         END

--         /* =========================================
--            VALIDATE MOBILE NUMBER
--            ========================================= */

--         IF LEN(@MobileNumber) < 10
--         BEGIN
--             SET @Output = -2;
--             RETURN;
--         END

--         /* =========================================
--            INSERT LEAD
--            ========================================= */

--         INSERT INTO GoldLoanLeadMaster
--         (
--             VendorID,
--             Name,
--             MobileNumber,
--             Caratage,
--             ApproximateWeight,
--             GoldLoanRequiredFor,
--             CreatedBy,
--             CreatedDate,
--             Status
--         )
--         VALUES
--         (
--             @VendorID,
--             @Name,
--             @MobileNumber,
--             @Caratage,
--             @ApproximateWeight,
--             @GoldLoanRequiredFor,
--             @CreatedBy,
--             DATEADD(MINUTE, 330, GETUTCDATE()),
--             1
--         )

--         /* =========================================
--            RETURN GENERATED ID
--            ========================================= */

--         SET @Output = SCOPE_IDENTITY();

--     END TRY

--     BEGIN CATCH

--         SET @Output = 0;

--     END CATCH

-- END
-- GO