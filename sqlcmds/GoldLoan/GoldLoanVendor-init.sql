/*==============================================================
  TABLE: GoldLoanVendorMaster
==============================================================*/

IF OBJECT_ID('dbo.GoldLoanVendorMaster', 'U') IS NOT NULL
    DROP TABLE dbo.GoldLoanVendorMaster;
GO

CREATE TABLE dbo.GoldLoanVendorMaster
(
    VendorID INT IDENTITY(1,1) PRIMARY KEY,

    VendorName NVARCHAR(100) NOT NULL,
    ContactNumber NVARCHAR(20) NOT NULL,

    BusinessShop NVARCHAR(100) NULL,
    BusinessType NVARCHAR(100) NULL,

    BranchID NVARCHAR(50) NOT NULL,
    BranchName NVARCHAR(100) NULL,

    Region NVARCHAR(100) NULL,

    Status BIT NOT NULL DEFAULT(1),

    CreatedBy NVARCHAR(50) NOT NULL,
    CreatedDate DATETIME NOT NULL DEFAULT(DATEADD(MINUTE, 330, GETUTCDATE())),

    UpdatedBy NVARCHAR(50) NULL,
    UpdatedDate DATETIME NULL
);
GO


/*==============================================================
  STORED PROCEDURE: sp_getVendorsList
==============================================================*/

IF OBJECT_ID('dbo.sp_getVendorsList', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_getVendorsList;
GO

CREATE PROCEDURE dbo.sp_getVendorsList
AS
BEGIN
    SET NOCOUNT ON;

    SELECT *
    FROM dbo.GoldLoanVendorMaster
    WHERE Status = 1
    ORDER BY VendorID DESC;
END
GO


/*==============================================================
  STORED PROCEDURE: sp_getVendor
==============================================================*/

IF OBJECT_ID('dbo.sp_getVendor', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_getVendor;
GO

CREATE PROCEDURE dbo.sp_getVendor
    @VendorID INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        VendorID,
        VendorName,
        ContactNumber,
        BusinessShop,
        BusinessType,
        BranchID,
        BranchName,
        Region,
        Status,
        CreatedBy,
        CreatedDate,
        UpdatedBy,
        UpdatedDate
    FROM dbo.GoldLoanVendorMaster
    WHERE VendorID = @VendorID
      AND Status = 1;
END
GO


/*==============================================================
  STORED PROCEDURE: sp_insertVendor
==============================================================*/

IF OBJECT_ID('dbo.sp_insertVendor', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_insertVendor;
GO

CREATE PROCEDURE dbo.sp_insertVendor
    @VendorName NVARCHAR(100),
    @ContactNumber NVARCHAR(20),
    @BusinessShop NVARCHAR(100) = NULL,
    @BusinessType NVARCHAR(100) = NULL,
    @BranchID NVARCHAR(50),
    @BranchName NVARCHAR(100) = NULL,
    @Region NVARCHAR(100) = NULL,
    @CreatedBy NVARCHAR(50),
    @Output INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY

        -- Check if vendor already exists
        IF EXISTS
        (
            SELECT 1
            FROM dbo.GoldLoanVendorMaster
            WHERE ContactNumber = @ContactNumber
              AND Status = 1
        )
        BEGIN
            SET @Output = -1;
            RETURN;
        END

        INSERT INTO dbo.GoldLoanVendorMaster
        (
            VendorName,
            ContactNumber,
            BusinessShop,
            BusinessType,
            BranchID,
            BranchName,
            Region,
            Status,
            CreatedBy,
            CreatedDate
        )
        VALUES
        (
            @VendorName,
            @ContactNumber,
            @BusinessShop,
            @BusinessType,
            @BranchID,
            @BranchName,
            @Region,
            1,
            @CreatedBy,
            DATEADD(MINUTE, 330, GETUTCDATE())
        );

        -- Return newly inserted VendorID
        SET @Output = SCOPE_IDENTITY();

    END TRY
    BEGIN CATCH
        SET @Output = 0;
    END CATCH
END
GO


/*==============================================================
  STORED PROCEDURE: sp_updateVendor
==============================================================*/

IF OBJECT_ID('dbo.sp_updateVendor', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_updateVendor;
GO

CREATE PROCEDURE dbo.sp_updateVendor
    @VendorID INT,
    @VendorName NVARCHAR(100) = NULL,
    @ContactNumber NVARCHAR(20) = NULL,
    @BusinessShop NVARCHAR(100) = NULL,
    @BusinessType NVARCHAR(100) = NULL,
    @BranchID NVARCHAR(50) = NULL,
    @BranchName NVARCHAR(100) = NULL,
    @Region NVARCHAR(100) = NULL,
    @UpdatedBy NVARCHAR(50),
    @Output INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY

        -- Check if vendor exists
        IF NOT EXISTS
        (
            SELECT 1
            FROM dbo.GoldLoanVendorMaster
            WHERE VendorID = @VendorID
        )
        BEGIN
            SET @Output = -1;
            RETURN;
        END

        UPDATE dbo.GoldLoanVendorMaster
        SET
            VendorName   = ISNULL(@VendorName, VendorName),
            ContactNumber = ISNULL(@ContactNumber, ContactNumber),
            BusinessShop = ISNULL(@BusinessShop, BusinessShop),
            BusinessType = ISNULL(@BusinessType, BusinessType),
            BranchID     = ISNULL(@BranchID, BranchID),
            BranchName   = ISNULL(@BranchName, BranchName),
            Region       = ISNULL(@Region, Region),
            UpdatedBy    = @UpdatedBy,
            UpdatedDate  = DATEADD(MINUTE, 330, GETUTCDATE())
        WHERE VendorID = @VendorID;

        IF @@ROWCOUNT > 0
            SET @Output = 1;
        ELSE
            SET @Output = 0;

    END TRY
    BEGIN CATCH
        SET @Output = 0;
    END CATCH
END
GO


/*==============================================================
  STORED PROCEDURE: sp_disableVendor
==============================================================*/

IF OBJECT_ID('dbo.sp_disableVendor', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_disableVendor;
GO

CREATE PROCEDURE dbo.sp_disableVendor
    @VendorID INT,
    @UpdatedBy NVARCHAR(50),
    @Output INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY

        -- Check if vendor exists
        IF NOT EXISTS
        (
            SELECT 1
            FROM dbo.GoldLoanVendorMaster
            WHERE VendorID = @VendorID
        )
        BEGIN
            SET @Output = -1;
            RETURN;
        END

        UPDATE dbo.GoldLoanVendorMaster
        SET
            Status = 0,
            UpdatedBy = @UpdatedBy,
            UpdatedDate = DATEADD(MINUTE, 330, GETUTCDATE())
        WHERE VendorID = @VendorID;

        SET @Output = 1;

    END TRY
    BEGIN CATCH
        SET @Output = 0;
    END CATCH
END
GO


/*==============================================================
  STORED PROCEDURE: sp_activateVendor
==============================================================*/

IF OBJECT_ID('dbo.sp_activateVendor', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_activateVendor;
GO

CREATE PROCEDURE dbo.sp_activateVendor
    @VendorID INT,
    @UpdatedBy NVARCHAR(50),
    @Output INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY

        -- Check if vendor exists
        IF NOT EXISTS
        (
            SELECT 1
            FROM dbo.GoldLoanVendorMaster
            WHERE VendorID = @VendorID
        )
        BEGIN
            SET @Output = -1;
            RETURN;
        END

        UPDATE dbo.GoldLoanVendorMaster
        SET
            Status = 1,
            UpdatedBy = @UpdatedBy,
            UpdatedDate = DATEADD(MINUTE, 330, GETUTCDATE())
        WHERE VendorID = @VendorID;

        SET @Output = 1;

    END TRY
    BEGIN CATCH
        SET @Output = 0;
    END CATCH
END
GO