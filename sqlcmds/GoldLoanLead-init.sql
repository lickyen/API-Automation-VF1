SELECT * FROM GoldLoanLeadMaster;

SELECT * FROM sys.procedures;

-- /* =========================================================
--    TABLE : GoldLoanLeadMaster
--    ========================================================= */

-- SET ANSI_NULLS ON
-- GO

-- SET QUOTED_IDENTIFIER ON
-- GO

-- CREATE TABLE GoldLoanLeadMaster
-- (
--     Id INT IDENTITY(1,1) PRIMARY KEY,

--     VendorID INT NOT NULL,

--     Name NVARCHAR(100) NOT NULL,

--     MobileNumber NVARCHAR(20) NOT NULL,

--     Caratage NVARCHAR(10) NOT NULL,

--     ApproximateWeight DECIMAL(10,2) NOT NULL,

--     GoldLoanRequiredFor NVARCHAR(250) NOT NULL,

--     CreatedBy NVARCHAR(50) NOT NULL,

--     CreatedDate DATETIME NOT NULL DEFAULT DATEADD(MINUTE, 330, GETUTCDATE()),

--     UpdatedBy NVARCHAR(50) NULL,

--     UpdatedDate DATETIME NULL,

--     Status BIT NOT NULL DEFAULT 1
-- )
-- GO


-- /* =========================================================
--    FOREIGN KEY : Vendors(VendorID)
--    ========================================================= */

-- ALTER TABLE GoldLoanLeadMaster
-- ADD CONSTRAINT FK_GoldLoanLeadMaster_GoldLoanVendorMaster
-- FOREIGN KEY (VendorID)
-- REFERENCES GoldLoanVendorMaster(VendorID)
-- GO


-- /* =========================================================
--    PROCEDURE : sp_getGoldLoanLeadsList
--    ========================================================= */

-- SET ANSI_NULLS ON
-- GO

-- SET QUOTED_IDENTIFIER ON
-- GO

-- CREATE PROCEDURE sp_getGoldLoanLeadsList
-- AS
-- BEGIN
--     SET NOCOUNT ON;

--     SELECT
--         Id,
--         VendorID,
--         Name,
--         MobileNumber,
--         Caratage,
--         ApproximateWeight,
--         GoldLoanRequiredFor,
--         CreatedBy,
--         CreatedDate,
--         UpdatedBy,
--         UpdatedDate,
--         Status
--     FROM GoldLoanLeadMaster
--     WHERE Status = 1
-- END
-- GO


-- /* =========================================================
--    PROCEDURE : sp_getGoldLoanLead
--    ========================================================= */

-- SET ANSI_NULLS ON
-- GO

-- SET QUOTED_IDENTIFIER ON
-- GO

-- CREATE PROCEDURE sp_getGoldLoanLead
--     @Id INT
-- AS
-- BEGIN
--     SET NOCOUNT ON;

--     SELECT
--         Id,
--         VendorID,
--         Name,
--         MobileNumber,
--         Caratage,
--         ApproximateWeight,
--         GoldLoanRequiredFor,
--         CreatedBy,
--         CreatedDate,
--         UpdatedBy,
--         UpdatedDate,
--         Status
--     FROM GoldLoanLeadMaster
--     WHERE Id = @Id
--       AND Status = 1
-- END
-- GO


-- /* =========================================================
--    PROCEDURE : sp_insertGoldLoanLead
--    ========================================================= */

-- SET ANSI_NULLS ON
-- GO

-- SET QUOTED_IDENTIFIER ON
-- GO

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

--         /* Validate Vendor Exists */
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

--         /* Return newly generated Lead ID */
--         SET @Output = SCOPE_IDENTITY();

--     END TRY
--     BEGIN CATCH

--         SET @Output = 0;

--     END CATCH
-- END
-- GO