-- Drop default constraint first
ALTER TABLE GoldLoanLeadMaster
DROP CONSTRAINT DF__GoldLoanL__Statu__07C12930;
GO

-- Now drop old Status column
ALTER TABLE GoldLoanLeadMaster
DROP COLUMN Status;
GO

-- Add new columns
ALTER TABLE GoldLoanLeadMaster
ADD
    CallingStatus VARCHAR(50) NULL,
    Status VARCHAR(20) NULL,
    Remarks VARCHAR(MAX) NULL;
GO