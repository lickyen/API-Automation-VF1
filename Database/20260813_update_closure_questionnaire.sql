USE [UATProductivity]
GO

IF OBJECT_ID('dbo.Closure_Questionnaire_Master', 'U') IS NULL
BEGIN
    THROW 51000, 'dbo.Closure_Questionnaire_Master does not exist.', 1;
END
GO

IF OBJECT_ID('dbo.Closure_Questionnaire_Response', 'U') IS NULL
BEGIN
    THROW 51001, 'dbo.Closure_Questionnaire_Response does not exist.', 1;
END
GO

DECLARE @Sql nvarchar(max) = N'';

SELECT @Sql = @Sql + N'ALTER TABLE [dbo].[Closure_Questionnaire_Master] DROP CONSTRAINT ' + QUOTENAME(name) + N';'
FROM sys.check_constraints
WHERE parent_object_id = OBJECT_ID('dbo.Closure_Questionnaire_Master')
  AND definition LIKE '%Input_Type%';

IF @Sql <> N''
    EXEC sp_executesql @Sql;
GO

ALTER TABLE [dbo].[Closure_Questionnaire_Master] WITH CHECK ADD CONSTRAINT [CK_Closure_Questionnaire_Master_InputType]
CHECK ([Input_Type] IN ('Number', 'Dropdown', 'Textarea', 'Textbox', 'Upload', 'Date'));
GO

DECLARE @Sql nvarchar(max) = N'';

SELECT @Sql = @Sql + N'ALTER TABLE [dbo].[Closure_Questionnaire_Master] DROP CONSTRAINT ' + QUOTENAME(name) + N';'
FROM sys.check_constraints
WHERE parent_object_id = OBJECT_ID('dbo.Closure_Questionnaire_Master')
  AND definition LIKE '%Trigger_Operator%';

IF @Sql <> N''
    EXEC sp_executesql @Sql;
GO

ALTER TABLE [dbo].[Closure_Questionnaire_Master] WITH CHECK ADD CONSTRAINT [CK_Closure_Questionnaire_Master_TriggerOperator]
CHECK ([Trigger_Operator] IS NULL OR [Trigger_Operator] IN ('Equals', 'In'));
GO

DECLARE @Questions TABLE
(
    QuestionCode varchar(50) NOT NULL PRIMARY KEY,
    QuestionText nvarchar(max) NOT NULL,
    SectionName varchar(50) NOT NULL,
    InputType varchar(30) NOT NULL,
    OptionsJson nvarchar(max) NULL,
    ParentQuestionCode varchar(50) NULL,
    TriggerOperator varchar(20) NULL,
    TriggerValue nvarchar(500) NULL,
    DisplayOrder int NOT NULL,
    IsMandatory bit NOT NULL
);

INSERT INTO @Questions
(
    QuestionCode,
    QuestionText,
    SectionName,
    InputType,
    OptionsJson,
    ParentQuestionCode,
    TriggerOperator,
    TriggerValue,
    DisplayOrder,
    IsMandatory
)
VALUES
('Q_FC_PRIMARY_REASON', N'What is the primary reason for closure?', 'General', 'Dropdown', N'["Balance Transfer","Arranged Funds from somewhere","Selling Property"]', NULL, NULL, NULL, 1, 1),

('Q_FC_BT_SANCTION_AVAILABLE', N'Is Sanction Letter Available?', 'BalanceTransfer', 'Dropdown', N'["Yes","No"]', 'Q_FC_PRIMARY_REASON', 'Equals', N'Balance Transfer', 2, 1),
('Q_FC_BT_SANCTION_UPLOAD', N'Upload Sanction Letter', 'BalanceTransfer', 'Upload', NULL, 'Q_FC_BT_SANCTION_AVAILABLE', 'Equals', N'Yes', 3, 1),
('Q_FC_BT_OFFER_AMOUNT', N'What is the Offer Amount?', 'BalanceTransfer', 'Number', NULL, 'Q_FC_PRIMARY_REASON', 'Equals', N'Balance Transfer', 4, 1),
('Q_FC_BT_OFFER_STATUS', N'What is the Offer Status?', 'BalanceTransfer', 'Dropdown', N'["Released","WIP"]', 'Q_FC_PRIMARY_REASON', 'Equals', N'Balance Transfer', 5, 1),
('Q_FC_BT_COMPETITOR_NAME', N'What is the Competitor Name?', 'BalanceTransfer', 'Textbox', NULL, 'Q_FC_PRIMARY_REASON', 'Equals', N'Balance Transfer', 6, 1),
('Q_FC_BT_TENURE_OFFERED', N'What is the Tenure Offered?', 'BalanceTransfer', 'Number', NULL, 'Q_FC_PRIMARY_REASON', 'Equals', N'Balance Transfer', 7, 1),
('Q_FC_BT_ROI_OFFERED', N'ROI Offered (%)', 'BalanceTransfer', 'Number', NULL, 'Q_FC_PRIMARY_REASON', 'Equals', N'Balance Transfer', 8, 1),
('Q_FC_BT_OUT_LOAN_PURPOSE', N'What is the Purpose of BT Out Loan?', 'BalanceTransfer', 'Textarea', NULL, 'Q_FC_PRIMARY_REASON', 'Equals', N'Balance Transfer', 9, 1),
('Q_FC_BT_EMI_OFFERED', N'What is the EMI Offered?', 'BalanceTransfer', 'Number', NULL, 'Q_FC_PRIMARY_REASON', 'Equals', N'Balance Transfer', 10, 1),
('Q_FC_BT_CAN_MATCH_OFFER', N'Can we match the offer?', 'BalanceTransfer', 'Dropdown', N'["Yes","No"]', 'Q_FC_PRIMARY_REASON', 'Equals', N'Balance Transfer', 11, 1),

('Q_FC_BT_TENTATIVE_PF', N'Tentative PF Charged by Competitor', 'ExitCost', 'Number', N'{"calculation":"1.5% of Offer Amount","dependsOn":["Q_FC_BT_OFFER_AMOUNT"],"readOnly":true}', 'Q_FC_PRIMARY_REASON', 'Equals', N'Balance Transfer', 12, 1),
('Q_FC_BT_INSURANCE_AMOUNT', N'Insurance Amount', 'ExitCost', 'Number', N'{"calculation":"2% of Offer Amount","dependsOn":["Q_FC_BT_OFFER_AMOUNT"],"readOnly":true}', 'Q_FC_PRIMARY_REASON', 'Equals', N'Balance Transfer', 13, 1),
('Q_FC_BT_VALUATION_CHARGES', N'Valuation Charges', 'ExitCost', 'Number', N'{"defaultValue":4000,"readOnly":true}', 'Q_FC_PRIMARY_REASON', 'Equals', N'Balance Transfer', 14, 1),
('Q_FC_BT_FC_CHARGES', N'FC Charges', 'ExitCost', 'Number', N'{"calculation":"5% of Vistaar Disbursed Amount","dependsOn":["DisbursedAmt"],"readOnly":true}', 'Q_FC_PRIMARY_REASON', 'Equals', N'Balance Transfer', 15, 1),
('Q_FC_BT_STAMP_DUTY', N'Stamp Duty', 'ExitCost', 'Number', N'{"calculation":"3% of Offer Amount","dependsOn":["Q_FC_BT_OFFER_AMOUNT"],"readOnly":true}', 'Q_FC_PRIMARY_REASON', 'Equals', N'Balance Transfer', 16, 1),
('Q_FC_BT_EXIT_COST', N'Exit Cost', 'ExitCost', 'Number', N'{"calculation":"Sum of PF, Insurance, Valuation Charges, FC Charges and Stamp Duty","dependsOn":["Q_FC_BT_TENTATIVE_PF","Q_FC_BT_INSURANCE_AMOUNT","Q_FC_BT_VALUATION_CHARGES","Q_FC_BT_FC_CHARGES","Q_FC_BT_STAMP_DUTY"],"readOnly":true}', 'Q_FC_PRIMARY_REASON', 'Equals', N'Balance Transfer', 17, 1),
('Q_FC_BT_EXIT_COST_LOAN_PER', N'Exit Cost as % of Loan', 'ExitCost', 'Number', N'{"calculation":"Exit Cost / Disbursed Amount","dependsOn":["Q_FC_BT_EXIT_COST","DisbursedAmt"],"readOnly":true}', 'Q_FC_PRIMARY_REASON', 'Equals', N'Balance Transfer', 18, 1),
('Q_FC_BT_CUSTOMER_UNDERSTOOD_EXIT_COST', N'Has the customer understood exit cost?', 'ExitCost', 'Dropdown', N'["Yes","No"]', 'Q_FC_PRIMARY_REASON', 'Equals', N'Balance Transfer', 19, 1),
('Q_FC_BT_EXIT_REMARKS', N'If customer still wants to exit, put remarks', 'ExitCost', 'Textarea', NULL, 'Q_FC_PRIMARY_REASON', 'Equals', N'Balance Transfer', 20, 0),

('Q_FC_AF_SOURCE_OF_FUNDS', N'What is the Source of Funds?', 'ArrangedFunds', 'Dropdown', N'["Self","Relative","Friend","Money Lender"]', 'Q_FC_PRIMARY_REASON', 'Equals', N'Arranged Funds from somewhere', 21, 1),
('Q_FC_AF_TRANSFER_PERSON_NAME', N'Name of the person who has transferred', 'ArrangedFunds', 'Textbox', NULL, 'Q_FC_AF_SOURCE_OF_FUNDS', 'In', N'Relative,Friend,Money Lender', 22, 1),
('Q_FC_AF_SELF_SOURCE_TYPE', N'If Source is Self', 'ArrangedFunds', 'Dropdown', N'["Savings","Business Profit","Have taken Unsecured Loan"]', 'Q_FC_AF_SOURCE_OF_FUNDS', 'Equals', N'Self', 23, 1),
('Q_FC_AF_SOURCE_VALIDATED_FROM', N'Source of Fund Validated from', 'ArrangedFunds', 'Dropdown', N'["Bank Statement","SOA","Message on Phone","Other"]', 'Q_FC_PRIMARY_REASON', 'Equals', N'Arranged Funds from somewhere', 24, 1),
('Q_FC_AF_AMOUNT_AVAILABLE', N'Is the amount Reflecting/Available?', 'ArrangedFunds', 'Dropdown', N'["Yes","No"]', 'Q_FC_PRIMARY_REASON', 'Equals', N'Arranged Funds from somewhere', 25, 1),
('Q_FC_AF_OUTSIDE_LOAN_NEXT_2M', N'Is Customer going to take loan from outside in 0-2 M?', 'ArrangedFunds', 'Dropdown', N'["Yes","No"]', 'Q_FC_PRIMARY_REASON', 'Equals', N'Arranged Funds from somewhere', 26, 1),
('Q_FC_AF_MONEY_RECEIVED_PROOF', N'Upload Proof of money received', 'ArrangedFunds', 'Upload', NULL, 'Q_FC_PRIMARY_REASON', 'Equals', N'Arranged Funds from somewhere', 27, 1),

('Q_FC_SP_SELLER_NAME', N'Name of the Seller', 'SellingProperty', 'Textbox', NULL, 'Q_FC_PRIMARY_REASON', 'Equals', N'Selling Property', 28, 1),
('Q_FC_SP_AGREEMENT_TO_SALE', N'Agreement to Sale', 'SellingProperty', 'Upload', NULL, 'Q_FC_PRIMARY_REASON', 'Equals', N'Selling Property', 29, 1),
('Q_FC_SP_EXPECTED_SALE_VALUE', N'Expected sale value', 'SellingProperty', 'Number', NULL, 'Q_FC_PRIMARY_REASON', 'Equals', N'Selling Property', 30, 1),
('Q_FC_SP_ADVANCE_RECEIVED', N'Has Customer received any advance?', 'SellingProperty', 'Dropdown', N'["Yes","No"]', 'Q_FC_PRIMARY_REASON', 'Equals', N'Selling Property', 31, 1),
('Q_FC_SP_REGISTRATION_DATE', N'Registration date', 'SellingProperty', 'Date', NULL, 'Q_FC_PRIMARY_REASON', 'Equals', N'Selling Property', 32, 1),
('Q_FC_SP_SELLER_NEEDS_LOAN', N'Does the seller need a loan?', 'SellingProperty', 'Dropdown', N'["Yes","No"]', 'Q_FC_PRIMARY_REASON', 'Equals', N'Selling Property', 33, 1),
('Q_FC_SP_ADVANCE_PROOF', N'Proof of advance money received', 'SellingProperty', 'Upload', NULL, 'Q_FC_SP_ADVANCE_RECEIVED', 'Equals', N'Yes', 34, 1);

UPDATE target
SET target.Is_Active = 0
FROM [dbo].[Closure_Questionnaire_Master] target
WHERE NOT EXISTS
(
    SELECT 1
    FROM @Questions source
    WHERE source.QuestionCode = target.Question_Code
);

UPDATE target
SET
    target.Question_Text = source.QuestionText,
    target.Section_Name = source.SectionName,
    target.Input_Type = source.InputType,
    target.Options_Json = source.OptionsJson,
    target.Parent_Question_Code = source.ParentQuestionCode,
    target.Trigger_Operator = source.TriggerOperator,
    target.Trigger_Value = source.TriggerValue,
    target.Display_Order = source.DisplayOrder,
    target.Is_Mandatory = source.IsMandatory,
    target.Is_Active = 1
FROM [dbo].[Closure_Questionnaire_Master] target
INNER JOIN @Questions source
    ON source.QuestionCode = target.Question_Code;

INSERT INTO [dbo].[Closure_Questionnaire_Master]
(
    Question_Code,
    Question_Text,
    Section_Name,
    Input_Type,
    Options_Json,
    Parent_Question_Code,
    Trigger_Operator,
    Trigger_Value,
    Display_Order,
    Is_Mandatory,
    Is_Active,
    CreatedBy,
    CreatedDt
)
SELECT
    source.QuestionCode,
    source.QuestionText,
    source.SectionName,
    source.InputType,
    source.OptionsJson,
    source.ParentQuestionCode,
    source.TriggerOperator,
    source.TriggerValue,
    source.DisplayOrder,
    source.IsMandatory,
    1,
    'System',
    GETDATE()
FROM @Questions source
WHERE NOT EXISTS
(
    SELECT 1
    FROM [dbo].[Closure_Questionnaire_Master] target
    WHERE target.Question_Code = source.QuestionCode
);
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_newClosure_GetQuestionnaire]
(
    @ReqId      INT,
    @ReqSource  VARCHAR(10) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @LetterId INT;

    IF @ReqSource = 'FC'
        SELECT @LetterId = Letter_Id
        FROM [dbo].[Closure_Transactions]
        WHERE Id = @ReqId;

    SELECT
         r.Id
        ,r.Req_Id
        ,r.Req_Source
        ,r.Question_Id
        ,r.Question_Code
        ,r.Response_Value
        ,r.CreatedBy
        ,r.CreatedDt
        ,m.Question_Text
        ,m.Section_Name
        ,m.Input_Type
        ,m.Options_Json
        ,m.Parent_Question_Code
        ,m.Trigger_Operator
        ,m.Trigger_Value
        ,m.Display_Order
        ,m.Is_Mandatory
    FROM [dbo].[Closure_Questionnaire_Response] r
    INNER JOIN [dbo].[Closure_Questionnaire_Master] m
        ON r.Question_Id = m.Id
    WHERE
        (
            @ReqSource = 'FC'
            AND (
                (r.Req_Source = 'FC' AND r.Req_Id = @ReqId)
             OR (@LetterId IS NOT NULL AND r.Req_Source = 'SOA/LOD/FCL' AND r.Req_Id = @LetterId)
            )
        )
        OR
        (
            @ReqSource = 'SOA/LOD/FCL'
            AND r.Req_Source = 'SOA/LOD/FCL'
            AND r.Req_Id = @ReqId
        )
    ORDER BY m.Display_Order;
END
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_newClosure_GetQuestionnaireMaster]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        Id,
        Question_Code AS QuestionCode,
        Question_Text AS QuestionText,
        Section_Name AS SectionName,
        Input_Type AS InputType,
        Options_Json AS OptionsJson,
        Parent_Question_Code AS ParentQuestionCode,
        Trigger_Operator AS TriggerOperator,
        Trigger_Value AS TriggerValue,
        Display_Order AS DisplayOrder,
        Is_Mandatory AS IsMandatory,
        Is_Active AS IsActive
    FROM [dbo].[Closure_Questionnaire_Master]
    WHERE Is_Active = 1
    ORDER BY Display_Order;
END
GO
