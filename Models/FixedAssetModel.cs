using System.ComponentModel.DataAnnotations;

namespace ProductivityApi.Models
{
    #region Masters

    public class FixedAssetMaster
    {
        public int Id { get; set; }

        public string? BranchCode { get; set; }

        public string? BranchName { get; set; }

        public string? AssetCode { get; set; }

        public string? AssetType { get; set; }

        public string? Description { get; set; }

        public DateTime? PurchaseDate { get; set; }

        public string? InvoiceNumber { get; set; }

        public int? Amount { get; set; }

        public string? AssetOwner { get; set; }

        public string? AssetStatus { get; set; }

        public string? Remarks { get; set; }

        public string? CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; }

        public string? UpdatedBy { get; set; }

        public DateTime? UpdatedDate { get; set; }
    }

    public class FixedAssetTypeMaster
    {
        public int Id { get; set; }

        public string? AssetType { get; set; }

        public bool IsActive { get; set; }

        public string? CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; }

        public string? UpdatedBy { get; set; }

        public DateTime? UpdatedDate { get; set; }
    }

    public class FixedAssetLog
    {
        public int Id { get; set; }

        public string? BranchCode { get; set; }

        public string? BranchName { get; set; }

        public string? AssetCode { get; set; }

        public string? AssetType { get; set; }

        public string? Description { get; set; }

        public DateTime? PurchaseDate { get; set; }

        public string? InvoiceNumber { get; set; }

        public int? Amount { get; set; }

        public string? AssetOwner { get; set; }

        public string? AssetStatus { get; set; }

        public string? CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; }
    }

    #endregion

    #region Transactions

    public class FixedAssetRequestTransaction
    {
        public int Id { get; set; }
        public string? BranchCode { get; set; }

        public string? AssetCode { get; set; }

        public string? AssetType { get; set; }

        public string? Description { get; set; }

        public DateTime? PurchaseDate { get; set; }

        public string? SupportingPhoto { get; set; }

        public string? Remarks { get; set; }

        public string? AdditionStatus { get; set; }

        public string? CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; }
    }

    public class FixedAssetTransferTransaction
    {
        public int Id { get; set; }

        public int AssetId { get; set; }

        public string? SourceBranch { get; set; }

        public string? DestinationBranch { get; set; }

        public string? Remarks { get; set; }

        public string? CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; }
    }

    public class FixedAssetSaleTransaction
    {
        public int Id { get; set; }

        public string? SaleId { get; set; }

        public int AssetId { get; set; }

        public string? BranchCode { get; set; }

        public decimal? SaleAmount { get; set; }

        public DateTime? InvoiceDate { get; set; }

        public string? Vendor { get; set; }

        public string? AmtTransferRef { get; set; }

        public string? Remarks { get; set; }

        public string? CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; }
    }

    public class FixedAssetAuditTransaction
    {
        public int Id { get; set; }

        public string? BranchCode { get; set; }

        public string? AssetCode { get; set; }

        public string? AssetType { get; set; }

        public string? AssetList { get; set; }

        public int? QtyVerified { get; set; }

        public int? QtyExpected { get; set; }

        public int? Difference { get; set; }

        public string? Remarks { get; set; }

        public string? VerificationStatus { get; set; }

        public string? CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; }
    }

    public class FixedAssetReportTransaction
    {
        public int Id { get; set; }
        public string? BranchCode { get; set; }

        public string? AssetCode { get; set; }

        public string? AssetType { get; set; }

        public string? AssetCondition { get; set; }

        public string? Remarks { get; set; }

        public string? CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; }
    }

    #endregion

    #region DTOs

    public class FixedAssetInsertDto
    {
        public string BranchCode { get; set; } = string.Empty;

        public string BranchName { get; set; } = string.Empty;

        public string AssetCode { get; set; } = string.Empty;

        public string AssetType { get; set; } = string.Empty;

        public string? Description { get; set; }

        public DateTime? PurchaseDate { get; set; }

        public string? InvoiceNumber { get; set; }

        public int? Amount { get; set; }

        public string? AssetOwner { get; set; }

        public string? AssetStatus { get; set; }

        public string? Remarks { get; set; }

        public string CreatedBy { get; set; } = string.Empty;
    }

    public class UpdateAssetStatusDto
    {
        public int AssetId { get; set; }

        public string AssetStatus { get; set; } = string.Empty;

        public string? Remarks { get; set; }

        public string UpdatedBy { get; set; } = string.Empty;
    }

    public class AddFixedAssetTypeDto
    {
        [Required]
        public string AssetType { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public string CreatedBy { get; set; } = string.Empty;
    }

    public class AssetAdditionRequestDto
    {
        public string? BranchCode { get; set; }
        public string AssetCode { get; set; } = string.Empty;

        public string AssetType { get; set; } = string.Empty;

        public string? Description { get; set; }

        public DateTime? PurchaseDate { get; set; }

        public string? SupportingPhoto { get; set; }

        public string? Remarks { get; set; }

        public string CreatedBy { get; set; } = string.Empty;
    }

    public class UpdateAdditionStatusDto
    {
        public int Id { get; set; }

        public string AdditionStatus { get; set; } = string.Empty;

        public string? Remarks { get; set; }

        public string UpdatedBy { get; set; } = string.Empty;
    }

    public class AssetTransferRequestDto
    {
        public int AssetId { get; set; }

        public string SourceBranch { get; set; } = string.Empty;

        public string DestinationBranch { get; set; } = string.Empty;

        public string? Remarks { get; set; }

        public string CreatedBy { get; set; } = string.Empty;
    }

    public class BulkAssetTransferRequestDto
    {
        public List<int> AssetIds { get; set; } = new();

        public string SourceBranch { get; set; } = string.Empty;

        public string DestinationBranch { get; set; } = string.Empty;

        public string? Remarks { get; set; }

        public string CreatedBy { get; set; } = string.Empty;
    }

    public class UpdateAssetBranchDto
    {
        public int AssetId { get; set; }

        public string BranchCode { get; set; } = string.Empty;

        public string BranchName { get; set; } = string.Empty;

        public string UpdatedBy { get; set; } = string.Empty;
    }
    public class BulkAssetSaleRequestDto
    {
        public List<int> AssetIds { get; set; } = new();

        public string? BranchCode { get; set; }

        public decimal SaleId { get; set; }

        public string? Remarks { get; set; }

        public string CreatedBy { get; set; } = string.Empty;
    }

    public class UpdateSaleInvoiceDto
    {
        public int Id { get; set; }

        public string SaleAmount { get; set; } = string.Empty;

        public DateTime InvoiceDate { get; set; }

        public string Vendor { get; set; } = string.Empty;

        public string AmtTransferRef { get; set; } = string.Empty;

        public string? Remarks { get; set; }

        public string UpdatedBy { get; set; } = string.Empty;
    }

    public class UploadAuditReportDto
    {
        public string BranchCode { get; set; } = string.Empty;

        public string? AssetCode { get; set; }

        public string? AssetType { get; set; }

        public string AssetList { get; set; } = string.Empty;

        public int QtyVerified { get; set; }

        public int QtyExpected { get; set; }

        public int Difference { get; set; }

        public string? Remarks { get; set; }

        public string CreatedBy { get; set; } = string.Empty;
    }

    public class UpdateVerificationStatusDto
    {
        public int Id { get; set; }

        public string VerificationStatus { get; set; } = string.Empty;

        public string? Remarks { get; set; }

        public string UpdatedBy { get; set; } = string.Empty;
    }

    public class ConditionReportDto
    {
        public string? BranchCode { get; set; }
        public string AssetCode { get; set; } = string.Empty;

        public string AssetType { get; set; } = string.Empty;

        public string AssetCondition { get; set; } = string.Empty;

        public string? Remarks { get; set; }

        public string CreatedBy { get; set; } = string.Empty;
    }

    public class FixedAssetBulkInsertDto
    {
        public string BranchCode { get; set; } = string.Empty;

        public string BranchName { get; set; } = string.Empty;

        public string AssetCode { get; set; } = string.Empty;

        public string AssetType { get; set; } = string.Empty;

        public string? Description { get; set; }

        public DateTime? PurchaseDate { get; set; }

        public string? InvoiceNumber { get; set; }

        public int? Amount { get; set; }

        public string? AssetOwner { get; set; }

        public string? AssetStatus { get; set; }

        public string? Remarks { get; set; }

        public string UpdatedBy { get; set; } = string.Empty;
    }

    #endregion
}