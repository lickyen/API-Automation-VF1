ALTER PROCEDURE sp_getGoldLoanLead
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        -- GOLD LOAN LEAD TABLE
        gl.Id,
        gl.VendorID AS VendorId,
        gl.Name,
        gl.MobileNumber,
        gl.Caratage,
        gl.ApproximateWeight,
        gl.GoldLoanRequiredFor,
        gl.CreatedBy,
        gl.CreatedDate,
        gl.UpdatedBy,
        gl.UpdatedDate,
        gl.CallingStatus,
        gl.Status,
        gl.Remarks,

        -- SPLIT COLUMN
        vm.VendorID AS SplitVendorID,

        -- VENDOR TABLE
        vm.VendorID,
        vm.VendorName,
        vm.ContactNumber,
        vm.BusinessShop,
        vm.BusinessType,
        vm.BranchId,
        vm.BranchName,
        vm.Region,
        vm.Status,
        vm.CreatedBy,
        vm.CreatedDate,
        vm.UpdatedBy,
        vm.UpdatedDate

    FROM GoldLoanLeadMaster gl
    INNER JOIN GoldLoanVendorMaster vm
        ON gl.VendorID = vm.VendorID
    WHERE gl.Id = @Id;
END
GO