USE [VisProductivity]
GO
/****** Object:  StoredProcedure [dbo].[sp_activateUser]    Script Date: 19-05-2026 14:50:52 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[sp_activateUser]
    @UserID NVARCHAR(50),
    @UpdatedBy NVARCHAR(50),
    @Output INT OUTPUT  -- Added output parameter
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        -- Check if user exists
        IF NOT EXISTS (SELECT 1 FROM UserMaster WHERE EmpId = @UserID)
        BEGIN
            SET @Output = -1; -- User not found
            RETURN;
        END

        -- Activate the user
        UPDATE UserMaster
        SET 
            Status = 'Active',
            UpdatedBy = @UpdatedBy,
            UpdatedDate = GETDATE()
        WHERE EmpId = @UserID;

        SET @Output = 1; -- Activated successfully

    END TRY
    BEGIN CATCH
        SET @Output = 0; -- Unknown error
    END CATCH
END;
GO
/****** Object:  StoredProcedure [dbo].[sp_disableUser]    Script Date: 19-05-2026 14:50:52 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[sp_disableUser]
    @UserID NVARCHAR(50),
    @UpdatedBy NVARCHAR(50),
    @Output INT OUTPUT  -- Added output parameter
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        -- Check if user exists
        IF NOT EXISTS (SELECT 1 FROM UserMaster WHERE EmpId = @UserID)
        BEGIN
            SET @Output = -1; -- User not found
            RETURN;
        END

        -- Disable the user
        UPDATE UserMaster
        SET 
            Status = 'Disabled',
            UpdatedBy = @UpdatedBy,
            UpdatedDate = GETDATE()
        WHERE EmpId = @UserID;

        SET @Output = 1; -- Disabled successfully

    END TRY
    BEGIN CATCH
        SET @Output = 0; -- Unknown error
    END CATCH
END;
GO
/****** Object:  StoredProcedure [dbo].[sp_getUser]    Script Date: 19-05-2026 14:50:52 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[sp_getUser]
    @UserID VARCHAR(20)
AS
BEGIN
    SELECT 
        U.EmpId,
        U.Name,
        U.Password,
        U.BranchID,
        U.BranchName,
        U.ClusterName,
        U.Zone,
        U.Megazone,
        U.Role,
        U.Status,
        U.MobileNo,
        U.CreatedBy,
        U.CreatedDate,
        U.UpdatedBy,
        U.UpdatedDate,
        U.AdSyncDate,

        -- Get role-based email
        CASE 
            WHEN U.Role = 'OPS' THEN B.OPS_Email
            WHEN U.Role = 'BM'  THEN B.BM_Email
            WHEN U.Role = 'CM'  THEN B.CM_Email
            ELSE U.EmailId
        END AS EmailId

    FROM UserMaster U
    LEFT JOIN BranchEmail_Master B 
        ON U.BranchID = B.BranchCode
        AND B.IsActive = 1

    WHERE U.EmpId = @UserID
      AND U.Status = 'Active'
END
GO
/****** Object:  StoredProcedure [dbo].[sp_getUsersList]    Script Date: 19-05-2026 14:50:52 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[sp_getUsersList]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT * FROM UserMaster
	where Status <> 'Disabled'
END
GO
/****** Object:  StoredProcedure [dbo].[sp_updateUser]    Script Date: 19-05-2026 14:50:52 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[sp_updateUser]
    @UserID NVARCHAR(50),
    @Name NVARCHAR(100) = NULL,
    @Role NVARCHAR(50) = NULL,
    @BranchID NVARCHAR(50) = NULL,
    @BranchName NVARCHAR(100) = NULL,
    @Region NVARCHAR(100) = NULL,
    @Zone NVARCHAR(100) = NULL,
    @Megazone NVARCHAR(100) = NULL,
    @EmailID NVARCHAR(100) = NULL,
    @Mobile NVARCHAR(20) = NULL,
    @UpdatedBy NVARCHAR(50),
    @Output INT OUTPUT  -- Added output parameter
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        -- Check if user exists
        IF NOT EXISTS (SELECT 1 FROM UserMaster WHERE EmpId = @UserID)
        BEGIN
            SET @Output = -1; -- User not found
            RETURN;
        END

        -- Update user
        UPDATE UserMaster
        SET
            Name = ISNULL(@Name, Name),
            Role = ISNULL(@Role, Role),
            BranchID = ISNULL(@BranchID, BranchID),
            BranchName = ISNULL(@BranchName, BranchName),
            ClusterName = ISNULL(@Region, ClusterName),
            Zone = ISNULL(@Zone, Zone),
            Megazone = ISNULL(@Megazone, Megazone),
            EmailID = ISNULL(@EmailID, EmailID),
            MobileNo = ISNULL(@Mobile, MobileNo),
            UpdatedBy = @UpdatedBy,
            UpdatedDate = GETDATE()
        WHERE EmpId = @UserID;

        -- Check if any row was updated
        IF @@ROWCOUNT > 0
            SET @Output = 1; -- Updated successfully

    END TRY
    BEGIN CATCH
        SET @Output = 0; -- Unknown error
    END CATCH
END;
GO
