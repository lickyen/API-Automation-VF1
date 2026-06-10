SELECT * FROM NOC_Transactions;
GO

SELECT * FROM NOC_Transactions
WHERE Id IN (3, 4);
GO

SELECT * FROM NOC_RoleStatusMap;
GO

SELECT * FROM NOC_TransactionLogs;
GO

SELECT * FROM NOC_SignMaster;
GO

UPDATE NOC_Transactions
SET Status = 'KYC Verified by NOC'
GO

UPDATE NOC_Transactions
SET Status = 'Delivered in Branch'
GO

UPDATE NOC_Transactions
SET Status = 'Generate NOC'
GO

UPDATE NOC_Transactions
SET
    Otp_Status_NOC = NULL,
    Otp_Status_NOC_Date = NULL,
    Otp_Status_KYC_Date = NULL,
    Otp_Status_KYC = NULL,
    Ack_PDF = NULL,
    Ack_PDF_Signed = NULL,
    NOC_PDF = NULL,
    NOC_PDF_Signed = NULL,
    POD = NULL,
    DeliveryDate = NULL,
    KYC_PDF = NULL,
    UpdatedCustMobileNo = NULL,
    UpdatedOwnerMobileNo = NULL
    ;

UPDATE NOC_Transactions
SET
    Otp_Status_NOC = NULL,
    Otp_Status_NOC_Date = NULL,
    Otp_Status_KYC_Date = NULL,
    Otp_Status_KYC = NULL,
    UpdatedCustMobileNo = NULL,
    UpdatedOwnerMobileNo = NULL;

SELECT * FROM OTP_Master;
GO