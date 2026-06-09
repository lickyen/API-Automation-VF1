SELECT * FROM NOC_Transactions;
GO

SELECT * FROM NOC_Transactions
WHERE Id = 3;
GO

SELECT * FROM NOC_RoleStatusMap;
GO

SELECT * FROM NOC_TransactionLogs;
GO

SELECT * FROM NOC_SignMaster;
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
    KYC_PDF = NULL
    ;