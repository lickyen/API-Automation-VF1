CREATE PROCEDURE SP_GetNewBranchById
(
    @Id INT
)
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

        OPSStatus,
        OPSDate,

        CreatedBy,
        CreatedDate,

        UpdatedBy,
        UpdatedDate

    FROM NewBranchMaster
    WHERE Id = @Id
END
GO