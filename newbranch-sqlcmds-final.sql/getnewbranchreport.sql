SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE OR ALTER PROCEDURE [dbo].[SP_GetNewBranchReport]
AS
BEGIN
    SET NOCOUNT ON;

    -- Master Data
    SELECT 
        Id,
        Name,
        State,
        Zone,
        Region,
        Megazone,
        Status,
        AdminStatus,
        AdminDate,
        HRStatus,
        HRDate,
        ITStatus,
        ITDate,
        RCHRCBStatus,
        RCHRCBDate,
        FinanceStatus,
        FinanceDate,
        MarketingStatus,
        MarketingDate,
        COPSStatus,
        COPSDate,
        CreatedBy,
        CreatedDate,
        UpdatedBy,
        UpdatedDate
    FROM NewBranchMaster;

    -- Transaction Data
    SELECT 
        Id,
        Tid,
        ChecklistId,
        CurrentStatus,
        OverallStatus,
        Remarks,
        CreatedBy,
        CreatedDate,
        UpdatedBy,
        UpdatedDate
    FROM NewBranchTransaction;
END;
GO
