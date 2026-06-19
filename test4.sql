CREATE TABLE FixedAssetMaster
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    BranchCode NVARCHAR(50),
    BranchName NVARCHAR(100),
    AssetCode NVARCHAR(20),
    AssetType NVARCHAR(50),
    Description NVARCHAR(500),
    PurchaseDate DATETIME,
    InvoiceNumber NVARCHAR(20),
    Amount INT,
    AssetOwner VARCHAR(20),
    AssetStatus NVARCHAR(20),
    Remarks NVARCHAR(500),

    CreatedBy NVARCHAR(100) NULL,
    CreatedDate DATETIME DEFAULT GETDATE(),
    UpdatedBy NVARCHAR(100) NULL,
    UpdatedDate DATETIME NULL
);
GO

CREATE TABLE FixedAssetTypeMaster
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    AssetType NVARCHAR(50),
    IsActive BIT,

    CreatedBy NVARCHAR(100) NULL,
    CreatedDate DATETIME DEFAULT GETDATE(),
    UpdatedBy NVARCHAR(100) NULL,
    UpdatedDate DATETIME NULL
);
GO

CREATE TABLE FixedAssetLogs
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    BranchCode NVARCHAR(50),
    BranchName NVARCHAR(100),
    AssetCode NVARCHAR(20),
    AssetType NVARCHAR(50),
    Description NVARCHAR(100),
    PurchaseDate DATETIME,
    InvoiceNumber NVARCHAR(20),
    Amount INT,
    AssetOwner VARCHAR(20),
    AssetStatus NVARCHAR(20),

    CreatedBy NVARCHAR(100) NULL,
    CreatedDate DATETIME DEFAULT GETDATE()
);
GO

CREATE TABLE FixedAssetRequestTransactions
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    AssetCode NVARCHAR(20),
    AssetType NVARCHAR(50),
    Description NVARCHAR(100),
    PurchaseDate DATETIME,
    SupportingPhoto NVARCHAR(500),
    Remarks NVARCHAR(500),
    AdditionStatus NVARCHAR(20),

    CreatedBy NVARCHAR(100) NULL,
    CreatedDate DATETIME DEFAULT GETDATE()
);
GO

CREATE TABLE FixedAssetTransferTransactions
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    AssetId INT,
    SourceBranch NVARCHAR(50),
    DestinationBranch NVARCHAR(50),
    Remarks NVARCHAR(500),

    CreatedBy NVARCHAR(100) NULL,
    CreatedDate DATETIME DEFAULT GETDATE(),

    CONSTRAINT FK_FixedAssetTransfer_Master
        FOREIGN KEY (AssetId)
        REFERENCES FixedAssetMaster(Id)
);
GO

CREATE TABLE FixedAssetSaleTransactions
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    SaleId NVARCHAR(50),
    AssetId INT,
    SaleAmount DECIMAL(18,2),
    InvoiceDate DATETIME,
    Vendor NVARCHAR(50),
    AmtTransferRef NVARCHAR(50),
    Remarks NVARCHAR(500),

    CreatedBy NVARCHAR(100) NULL,
    CreatedDate DATETIME DEFAULT GETDATE(),

    CONSTRAINT FK_FixedAssetSale_Master
        FOREIGN KEY (AssetId)
        REFERENCES FixedAssetMaster(Id)
);
GO

CREATE TABLE FixedAssetAuditTransactions
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    BranchCode NVARCHAR(50),
    AssetCode NVARCHAR(20),
    AssetType NVARCHAR(50),
    AssetList NVARCHAR(MAX),
    QtyVerified INT,
    QtyExpected INT,
    Difference INT,
    Remarks NVARCHAR(500),
    VerificationStatus NVARCHAR(50),

    CreatedBy NVARCHAR(100) NULL,
    CreatedDate DATETIME DEFAULT GETDATE()
);
GO

CREATE TABLE FixedAssetReportTransactions
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    AssetCode NVARCHAR(20),
    AssetType NVARCHAR(50),
    AssetCondition NVARCHAR(100),
    Remarks NVARCHAR(500),

    CreatedBy NVARCHAR(100) NULL,
    CreatedDate DATETIME DEFAULT GETDATE()
);
GO