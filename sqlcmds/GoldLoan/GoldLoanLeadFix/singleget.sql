ALTER PROCEDURE sp_getGoldLoanLead
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        -- =========================
        -- LEAD MODEL
        -- =========================
        GL.Id,
        GL.VendorID,
        GL.Name,
        GL.MobileNumber,
        GL.Caratage,
        GL.ApproximateWeight,
        GL.GoldLoanRequiredFor,
        GL.CreatedBy,
        GL.CreatedDate,
        GL.UpdatedBy,
        GL.UpdatedDate,
        GL.Status,

        -- =========================
        -- SPLIT COLUMN
        -- =========================
        V.VendorID AS SplitVendorID,

        -- =========================
        -- VENDOR MODEL
        -- =========================
        V.VendorID,
        V.VendorName,
        V.ContactNumber,
        V.BusinessShop,
        V.BusinessType,
        V.BranchID,
        V.BranchName,
        V.Region,
        V.Status,
        V.CreatedBy,
        V.CreatedDate,
        V.UpdatedBy,
        V.UpdatedDate

    FROM GoldLoanLeadMaster GL
    INNER JOIN GoldLoanVendorMaster V
        ON GL.VendorID = V.VendorID
    WHERE GL.Id = @Id
      AND GL.Status = 1
END