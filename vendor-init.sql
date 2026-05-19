SELECT * FROM GoldLoanVendorMaster;

SELECT * FROM sys.procedures;

SELECT TABLE_NAME 
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_TYPE = 'BASE TABLE';


-- /****** Object:  StoredProcedure [dbo].[sp_getVendorsList] ******/
-- SET ANSI_NULLS ON
-- GO
-- SET QUOTED_IDENTIFIER ON
-- GO

-- ALTER PROCEDURE [dbo].[sp_getVendorsList]
-- AS
-- BEGIN
--     SET NOCOUNT ON;

--     SELECT *
--     FROM GoldLoanVendorMaster
--     WHERE Status = 1
-- END
-- GO


-- /****** Object:  StoredProcedure [dbo].[sp_getVendor] ******/
-- SET ANSI_NULLS ON
-- GO
-- SET QUOTED_IDENTIFIER ON
-- GO

-- ALTER PROCEDURE [dbo].[sp_getVendor]
--     @VendorID INT
-- AS
-- BEGIN
--     SET NOCOUNT ON;

--     SELECT 
--         VendorID,
--         VendorName,
--         ContactNumber,
--         BusinessShop,
--         BusinessType,
--         BranchID,
--         BranchName,
--         Region,
--         Status,
--         CreatedBy,
--         CreatedDate,
--         UpdatedBy,
--         UpdatedDate
--     FROM GoldLoanVendorMaster
--     WHERE VendorID = @VendorID
--       AND Status = 1
-- END
-- GO


-- /****** Object:  StoredProcedure [dbo].[sp_insertVendor] ******/
-- SET ANSI_NULLS ON
-- GO
-- SET QUOTED_IDENTIFIER ON
-- GO

-- ALTER PROCEDURE [dbo].[sp_insertVendor]
--     @VendorName NVARCHAR(100),
--     @ContactNumber NVARCHAR(20),
--     @BusinessShop NVARCHAR(100) = NULL,
--     @BusinessType NVARCHAR(100) = NULL,
--     @BranchID NVARCHAR(50),
--     @BranchName NVARCHAR(100) = NULL,
--     @Region NVARCHAR(100) = NULL,
--     @CreatedBy NVARCHAR(50),
--     @Output INT OUTPUT
-- AS
-- BEGIN
--     SET NOCOUNT ON;

--     BEGIN TRY

--         -- Check if vendor already exists
--         IF EXISTS (
--             SELECT 1
--             FROM GoldLoanVendorMaster
--             WHERE ContactNumber = @ContactNumber
--               AND Status = 1
--         )
--         BEGIN
--             SET @Output = -1;
--             RETURN;
--         END

--         INSERT INTO GoldLoanVendorMaster
--         (
--             VendorName,
--             ContactNumber,
--             BusinessShop,
--             BusinessType,
--             BranchID,
--             BranchName,
--             Region,
--             Status,
--             CreatedBy,
--             CreatedDate
--         )
--         VALUES
--         (
--             @VendorName,
--             @ContactNumber,
--             @BusinessShop,
--             @BusinessType,
--             @BranchID,
--             @BranchName,
--             @Region,
--             1,
--             @CreatedBy,
--             GETDATE()
--         )

--         -- Return newly generated VendorID
--         SET @Output = SCOPE_IDENTITY();

--     END TRY
--     BEGIN CATCH
--         SET @Output = 0;
--     END CATCH
-- END
-- GO


-- /****** Object:  StoredProcedure [dbo].[sp_updateVendor] ******/
-- SET ANSI_NULLS ON
-- GO
-- SET QUOTED_IDENTIFIER ON
-- GO

-- ALTER PROCEDURE [dbo].[sp_updateVendor]
--     @VendorID INT,
--     @VendorName NVARCHAR(100) = NULL,
--     @ContactNumber NVARCHAR(20) = NULL,
--     @BusinessShop NVARCHAR(100) = NULL,
--     @BusinessType NVARCHAR(100) = NULL,
--     @BranchID NVARCHAR(50) = NULL,
--     @BranchName NVARCHAR(100) = NULL,
--     @Region NVARCHAR(100) = NULL,
--     @UpdatedBy NVARCHAR(50),
--     @Output INT OUTPUT
-- AS
-- BEGIN
--     SET NOCOUNT ON;

--     BEGIN TRY

--         -- Check if vendor exists
--         IF NOT EXISTS (
--             SELECT 1
--             FROM GoldLoanVendorMaster
--             WHERE VendorID = @VendorID
--         )
--         BEGIN
--             SET @Output = -1;
--             RETURN;
--         END

--         UPDATE GoldLoanVendorMaster
--         SET
--             VendorName = ISNULL(@VendorName, VendorName),
--             ContactNumber = ISNULL(@ContactNumber, ContactNumber),
--             BusinessShop = ISNULL(@BusinessShop, BusinessShop),
--             BusinessType = ISNULL(@BusinessType, BusinessType),
--             BranchID = ISNULL(@BranchID, BranchID),
--             BranchName = ISNULL(@BranchName, BranchName),
--             Region = ISNULL(@Region, Region),
--             UpdatedBy = @UpdatedBy,
--             UpdatedDate = GETDATE()
--         WHERE VendorID = @VendorID;

--         IF @@ROWCOUNT > 0
--             SET @Output = 1;

--     END TRY
--     BEGIN CATCH
--         SET @Output = 0;
--     END CATCH
-- END
-- GO


-- /****** Object:  StoredProcedure [dbo].[sp_disableVendor] ******/
-- SET ANSI_NULLS ON
-- GO
-- SET QUOTED_IDENTIFIER ON
-- GO

-- ALTER PROCEDURE [dbo].[sp_disableVendor]
--     @VendorID INT,
--     @UpdatedBy NVARCHAR(50),
--     @Output INT OUTPUT
-- AS
-- BEGIN
--     SET NOCOUNT ON;

--     BEGIN TRY

--         -- Check if vendor exists
--         IF NOT EXISTS (
--             SELECT 1
--             FROM GoldLoanVendorMaster
--             WHERE VendorID = @VendorID
--         )
--         BEGIN
--             SET @Output = -1;
--             RETURN;
--         END

--         UPDATE GoldLoanVendorMaster
--         SET
--             Status = 0,
--             UpdatedBy = @UpdatedBy,
--             UpdatedDate = GETDATE()
--         WHERE VendorID = @VendorID;

--         SET @Output = 1;

--     END TRY
--     BEGIN CATCH
--         SET @Output = 0;
--     END CATCH
-- END
-- GO


-- /****** Object:  StoredProcedure [dbo].[sp_activateVendor] ******/
-- SET ANSI_NULLS ON
-- GO
-- SET QUOTED_IDENTIFIER ON
-- GO

-- ALTER PROCEDURE [dbo].[sp_activateVendor]
--     @VendorID INT,
--     @UpdatedBy NVARCHAR(50),
--     @Output INT OUTPUT
-- AS
-- BEGIN
--     SET NOCOUNT ON;

--     BEGIN TRY

--         -- Check if vendor exists
--         IF NOT EXISTS (
--             SELECT 1
--             FROM GoldLoanVendorMaster
--             WHERE VendorID = @VendorID
--         )
--         BEGIN
--             SET @Output = -1;
--             RETURN;
--         END

--         UPDATE GoldLoanVendorMaster
--         SET
--             Status = 1,
--             UpdatedBy = @UpdatedBy,
--             UpdatedDate = GETDATE()
--         WHERE VendorID = @VendorID;

--         SET @Output = 1;

--     END TRY
--     BEGIN CATCH
--         SET @Output = 0;
--     END CATCH
-- END
-- GO