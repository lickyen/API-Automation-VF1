SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE OR ALTER PROCEDURE [dbo].[SP_GetAllNewBranches]
AS
BEGIN
    SET NOCOUNT ON;

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

    FROM NewBranchMaster

    ORDER BY Id ASC
END
GO
