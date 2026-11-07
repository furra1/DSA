/*
    DSA Manager — SQL Server database, version 1.0
    Target: Microsoft SQL Server 2019+
    Run in SQL Server Management Studio (SSMS) or Azure Data Studio.

    IMPORTANT:
    - This script creates schema [dsa] in the CURRENT database.
    - It does not create/drop a database and does not delete existing data.
    - It is designed from the supplied project archive + project requirements.
    - The current Backend uses MySQL and maps only Users. The Backend must be
      migrated to Microsoft.EntityFrameworkCore.SqlServer and its DbContext
      mappings updated before this schema can be used by the application.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

IF SCHEMA_ID(N'dsa') IS NULL
    EXEC(N'CREATE SCHEMA [dsa]');

-------------------------------------------------------------------------------
-- 1. Reference tables
-------------------------------------------------------------------------------

IF OBJECT_ID(N'dsa.Roles', N'U') IS NULL
BEGIN
    CREATE TABLE dsa.Roles
    (
        RoleId          tinyint IDENTITY(1,1) NOT NULL CONSTRAINT PK_Roles PRIMARY KEY,
        Code            varchar(30) NOT NULL,
        Name            nvarchar(100) NOT NULL,
        Description     nvarchar(300) NULL,
        CONSTRAINT UQ_Roles_Code UNIQUE (Code)
    );

    INSERT INTO dsa.Roles (Code, Name, Description)
    VALUES
        ('Employee', N'Сотрудник', N'Работа с назначенными задачами и планами'),
        ('Manager',  N'Главный менеджер', N'Управление группой, задачами и планами'),
        ('Admin',    N'Администратор', N'Администрирование системы');
END;

IF OBJECT_ID(N'dsa.Products', N'U') IS NULL
BEGIN
    CREATE TABLE dsa.Products
    (
        ProductId       int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Products PRIMARY KEY,
        Name            nvarchar(150) NOT NULL,
        Description     nvarchar(1000) NULL,
        IsActive        bit NOT NULL CONSTRAINT DF_Products_IsActive DEFAULT (1),
        CreatedAt       datetime2(0) NOT NULL CONSTRAINT DF_Products_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt       datetime2(0) NOT NULL CONSTRAINT DF_Products_UpdatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT UQ_Products_Name UNIQUE (Name)
    );
END;

-------------------------------------------------------------------------------
-- 2. Users, groups, memberships
-------------------------------------------------------------------------------

IF OBJECT_ID(N'dsa.Users', N'U') IS NULL
BEGIN
    CREATE TABLE dsa.Users
    (
        UserId              int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
        FirstName           nvarchar(100) NOT NULL,
        LastName            nvarchar(100) NOT NULL,
        Email               nvarchar(255) NOT NULL,
        EmployeeNumber      nvarchar(50) NOT NULL,
        PasswordHash        nvarchar(500) NOT NULL,
        RoleCode            varchar(30) NOT NULL CONSTRAINT DF_Users_RoleCode DEFAULT ('Employee'),
        GroupId             int NULL,
        Phone               nvarchar(30) NULL,
        IsActive            bit NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT (1),
        EmailVerifiedAt     datetime2(0) NULL,
        LastLoginAt         datetime2(0) NULL,
        TwoFactorEnabled    bit NOT NULL CONSTRAINT DF_Users_TwoFactorEnabled DEFAULT (0),
        TwoFactorSecret     varbinary(512) NULL, -- store encrypted secret only
        CreatedAt           datetime2(0) NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt           datetime2(0) NOT NULL CONSTRAINT DF_Users_UpdatedAt DEFAULT (SYSUTCDATETIME()),
        RowVersion          rowversion NOT NULL,
        CONSTRAINT UQ_Users_Email UNIQUE (Email),
        CONSTRAINT UQ_Users_EmployeeNumber UNIQUE (EmployeeNumber),
        CONSTRAINT FK_Users_Roles FOREIGN KEY (RoleCode) REFERENCES dsa.Roles(Code)
    );
END;

IF OBJECT_ID(N'dsa.Groups', N'U') IS NULL
BEGIN
    CREATE TABLE dsa.Groups
    (
        GroupId         int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Groups PRIMARY KEY,
        Name            nvarchar(120) NOT NULL,
        Description     nvarchar(500) NULL,
        ManagerId       int NULL,
        IsActive        bit NOT NULL CONSTRAINT DF_Groups_IsActive DEFAULT (1),
        CreatedAt       datetime2(0) NOT NULL CONSTRAINT DF_Groups_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt       datetime2(0) NOT NULL CONSTRAINT DF_Groups_UpdatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT UQ_Groups_Name UNIQUE (Name),
        CONSTRAINT FK_Groups_Manager FOREIGN KEY (ManagerId) REFERENCES dsa.Users(UserId)
    );
END;

-- Add user's primary group FK after Groups exists (resolves the circular reference).
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Users_Groups')
    ALTER TABLE dsa.Users ADD CONSTRAINT FK_Users_Groups
        FOREIGN KEY (GroupId) REFERENCES dsa.Groups(GroupId);

IF OBJECT_ID(N'dsa.GroupMembers', N'U') IS NULL
BEGIN
    CREATE TABLE dsa.GroupMembers
    (
        GroupMemberId   bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_GroupMembers PRIMARY KEY,
        GroupId         int NOT NULL,
        UserId          int NOT NULL,
        JoinedAt        datetime2(0) NOT NULL CONSTRAINT DF_GroupMembers_JoinedAt DEFAULT (SYSUTCDATETIME()),
        LeftAt          datetime2(0) NULL,
        IsPrimary       bit NOT NULL CONSTRAINT DF_GroupMembers_IsPrimary DEFAULT (0),
        CONSTRAINT FK_GroupMembers_Groups FOREIGN KEY (GroupId) REFERENCES dsa.Groups(GroupId),
        CONSTRAINT FK_GroupMembers_Users FOREIGN KEY (UserId) REFERENCES dsa.Users(UserId),
        CONSTRAINT CK_GroupMembers_Dates CHECK (LeftAt IS NULL OR LeftAt >= JoinedAt)
    );
    CREATE UNIQUE INDEX UX_GroupMembers_ActiveMembership
        ON dsa.GroupMembers(GroupId, UserId) WHERE LeftAt IS NULL;
    CREATE INDEX IX_GroupMembers_User_Active ON dsa.GroupMembers(UserId, LeftAt);
END;

-------------------------------------------------------------------------------
-- 3. Tasks and task workflow
-------------------------------------------------------------------------------

IF OBJECT_ID(N'dsa.Tasks', N'U') IS NULL
BEGIN
    CREATE TABLE dsa.Tasks
    (
        TaskId              bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_Tasks PRIMARY KEY,
        Title               nvarchar(200) NOT NULL,
        Description         nvarchar(max) NULL,
        Deadline            datetime2(0) NOT NULL,
        Priority            varchar(20) NOT NULL CONSTRAINT DF_Tasks_Priority DEFAULT ('Medium'),
        Status              varchar(20) NOT NULL CONSTRAINT DF_Tasks_Status DEFAULT ('New'),
        ProductId           int NULL,
        GroupId             int NULL,
        AssignedUserId      int NULL, -- compatibility with the current Task entity
        CreatedById         int NOT NULL,
        StartedAt           datetime2(0) NULL,
        CompletedAt         datetime2(0) NULL,
        CreatedAt           datetime2(0) NOT NULL CONSTRAINT DF_Tasks_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt           datetime2(0) NOT NULL CONSTRAINT DF_Tasks_UpdatedAt DEFAULT (SYSUTCDATETIME()),
        RowVersion          rowversion NOT NULL,
        CONSTRAINT FK_Tasks_Products FOREIGN KEY (ProductId) REFERENCES dsa.Products(ProductId),
        CONSTRAINT FK_Tasks_Groups FOREIGN KEY (GroupId) REFERENCES dsa.Groups(GroupId),
        CONSTRAINT FK_Tasks_AssignedUser FOREIGN KEY (AssignedUserId) REFERENCES dsa.Users(UserId),
        CONSTRAINT FK_Tasks_CreatedBy FOREIGN KEY (CreatedById) REFERENCES dsa.Users(UserId),
        CONSTRAINT CK_Tasks_Priority CHECK (Priority IN ('Low','Medium','High')),
        CONSTRAINT CK_Tasks_Status CHECK (Status IN ('New','InProgress','Completed','Overdue','Cancelled'))
    );
    CREATE INDEX IX_Tasks_Status_Deadline ON dsa.Tasks(Status, Deadline);
    CREATE INDEX IX_Tasks_Group_Status_Deadline ON dsa.Tasks(GroupId, Status, Deadline);
    CREATE INDEX IX_Tasks_AssignedUser_Status ON dsa.Tasks(AssignedUserId, Status);
    CREATE INDEX IX_Tasks_Product_Deadline ON dsa.Tasks(ProductId, Deadline);
END;

IF OBJECT_ID(N'dsa.TaskAssignments', N'U') IS NULL
BEGIN
    CREATE TABLE dsa.TaskAssignments
    (
        TaskAssignmentId    bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_TaskAssignments PRIMARY KEY,
        TaskId              bigint NOT NULL,
        UserId              int NULL,
        GroupId             int NULL,
        AssignedById        int NOT NULL,
        AssignedAt          datetime2(0) NOT NULL CONSTRAINT DF_TaskAssignments_AssignedAt DEFAULT (SYSUTCDATETIME()),
        IsActive            bit NOT NULL CONSTRAINT DF_TaskAssignments_IsActive DEFAULT (1),
        CONSTRAINT FK_TaskAssignments_Tasks FOREIGN KEY (TaskId) REFERENCES dsa.Tasks(TaskId),
        CONSTRAINT FK_TaskAssignments_Users FOREIGN KEY (UserId) REFERENCES dsa.Users(UserId),
        CONSTRAINT FK_TaskAssignments_Groups FOREIGN KEY (GroupId) REFERENCES dsa.Groups(GroupId),
        CONSTRAINT FK_TaskAssignments_AssignedBy FOREIGN KEY (AssignedById) REFERENCES dsa.Users(UserId),
        CONSTRAINT CK_TaskAssignments_ExactlyOneTarget CHECK
            ((CASE WHEN UserId IS NULL THEN 0 ELSE 1 END) +
             (CASE WHEN GroupId IS NULL THEN 0 ELSE 1 END) = 1)
    );
    CREATE INDEX IX_TaskAssignments_Task_Active ON dsa.TaskAssignments(TaskId, IsActive);
    CREATE INDEX IX_TaskAssignments_User_Active ON dsa.TaskAssignments(UserId, IsActive);
    CREATE INDEX IX_TaskAssignments_Group_Active ON dsa.TaskAssignments(GroupId, IsActive);
END;

IF OBJECT_ID(N'dsa.TaskComments', N'U') IS NULL
BEGIN
    CREATE TABLE dsa.TaskComments
    (
        CommentId       bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_TaskComments PRIMARY KEY,
        TaskId          bigint NOT NULL,
        UserId          int NOT NULL,
        CommentText     nvarchar(4000) NOT NULL,
        CreatedAt       datetime2(0) NOT NULL CONSTRAINT DF_TaskComments_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt       datetime2(0) NULL,
        IsDeleted       bit NOT NULL CONSTRAINT DF_TaskComments_IsDeleted DEFAULT (0),
        CONSTRAINT FK_TaskComments_Tasks FOREIGN KEY (TaskId) REFERENCES dsa.Tasks(TaskId),
        CONSTRAINT FK_TaskComments_Users FOREIGN KEY (UserId) REFERENCES dsa.Users(UserId)
    );
    CREATE INDEX IX_TaskComments_Task_CreatedAt ON dsa.TaskComments(TaskId, CreatedAt);
END;

IF OBJECT_ID(N'dsa.TaskAttachments', N'U') IS NULL
BEGIN
    CREATE TABLE dsa.TaskAttachments
    (
        AttachmentId    bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_TaskAttachments PRIMARY KEY,
        TaskId          bigint NOT NULL,
        UploadedById    int NOT NULL,
        FileName        nvarchar(260) NOT NULL,
        ContentType     nvarchar(150) NULL,
        FileSizeBytes   bigint NOT NULL,
        StorageKey      nvarchar(500) NOT NULL,
        FileHash        varbinary(32) NULL,
        UploadedAt      datetime2(0) NOT NULL CONSTRAINT DF_TaskAttachments_UploadedAt DEFAULT (SYSUTCDATETIME()),
        IsDeleted       bit NOT NULL CONSTRAINT DF_TaskAttachments_IsDeleted DEFAULT (0),
        CONSTRAINT FK_TaskAttachments_Tasks FOREIGN KEY (TaskId) REFERENCES dsa.Tasks(TaskId),
        CONSTRAINT FK_TaskAttachments_Users FOREIGN KEY (UploadedById) REFERENCES dsa.Users(UserId),
        CONSTRAINT CK_TaskAttachments_FileSize CHECK (FileSizeBytes > 0)
    );
    CREATE INDEX IX_TaskAttachments_Task ON dsa.TaskAttachments(TaskId, IsDeleted);
END;

IF OBJECT_ID(N'dsa.TaskHistory', N'U') IS NULL
BEGIN
    CREATE TABLE dsa.TaskHistory
    (
        TaskHistoryId   bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_TaskHistory PRIMARY KEY,
        TaskId          bigint NOT NULL,
        ChangedById     int NOT NULL,
        FieldName       nvarchar(100) NOT NULL,
        OldValue        nvarchar(2000) NULL,
        NewValue        nvarchar(2000) NULL,
        ChangedAt       datetime2(0) NOT NULL CONSTRAINT DF_TaskHistory_ChangedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_TaskHistory_Tasks FOREIGN KEY (TaskId) REFERENCES dsa.Tasks(TaskId),
        CONSTRAINT FK_TaskHistory_Users FOREIGN KEY (ChangedById) REFERENCES dsa.Users(UserId)
    );
    CREATE INDEX IX_TaskHistory_Task_ChangedAt ON dsa.TaskHistory(TaskId, ChangedAt DESC);
END;

-------------------------------------------------------------------------------
-- 4. Plans and measurable plan items
-------------------------------------------------------------------------------

IF OBJECT_ID(N'dsa.Plans', N'U') IS NULL
BEGIN
    CREATE TABLE dsa.Plans
    (
        PlanId          int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Plans PRIMARY KEY,
        GroupId         int NOT NULL,
        Name            nvarchar(200) NOT NULL,
        PeriodType      varchar(20) NOT NULL, -- Day, Week, Month, Custom
        StartDate       date NOT NULL,
        EndDate         date NOT NULL,
        CreatedById     int NOT NULL,
        Status          varchar(20) NOT NULL CONSTRAINT DF_Plans_Status DEFAULT ('Draft'),
        CreatedAt       datetime2(0) NOT NULL CONSTRAINT DF_Plans_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt       datetime2(0) NOT NULL CONSTRAINT DF_Plans_UpdatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_Plans_Groups FOREIGN KEY (GroupId) REFERENCES dsa.Groups(GroupId),
        CONSTRAINT FK_Plans_CreatedBy FOREIGN KEY (CreatedById) REFERENCES dsa.Users(UserId),
        CONSTRAINT CK_Plans_Dates CHECK (EndDate >= StartDate),
        CONSTRAINT CK_Plans_PeriodType CHECK (PeriodType IN ('Day','Week','Month','Custom')),
        CONSTRAINT CK_Plans_Status CHECK (Status IN ('Draft','Active','Completed','Archived'))
    );
    CREATE INDEX IX_Plans_Group_Period ON dsa.Plans(GroupId, StartDate, EndDate);
END;

IF OBJECT_ID(N'dsa.PlanItems', N'U') IS NULL
BEGIN
    CREATE TABLE dsa.PlanItems
    (
        PlanItemId      bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_PlanItems PRIMARY KEY,
        PlanId          int NOT NULL,
        ProductId       int NULL,
        Title           nvarchar(200) NOT NULL,
        Description     nvarchar(1000) NULL,
        TargetValue     decimal(18,2) NOT NULL,
        ActualValue     decimal(18,2) NOT NULL CONSTRAINT DF_PlanItems_ActualValue DEFAULT (0),
        Unit            nvarchar(30) NULL,
        SortOrder       int NOT NULL CONSTRAINT DF_PlanItems_SortOrder DEFAULT (0),
        CreatedAt       datetime2(0) NOT NULL CONSTRAINT DF_PlanItems_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt       datetime2(0) NOT NULL CONSTRAINT DF_PlanItems_UpdatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_PlanItems_Plans FOREIGN KEY (PlanId) REFERENCES dsa.Plans(PlanId),
        CONSTRAINT FK_PlanItems_Products FOREIGN KEY (ProductId) REFERENCES dsa.Products(ProductId),
        CONSTRAINT CK_PlanItems_Values CHECK (TargetValue >= 0 AND ActualValue >= 0)
    );
    CREATE INDEX IX_PlanItems_Plan ON dsa.PlanItems(PlanId, SortOrder);
    CREATE INDEX IX_PlanItems_Product ON dsa.PlanItems(ProductId);
END;

-------------------------------------------------------------------------------
-- 5. Notifications and user preferences
-------------------------------------------------------------------------------

IF OBJECT_ID(N'dsa.UserNotificationSettings', N'U') IS NULL
BEGIN
    CREATE TABLE dsa.UserNotificationSettings
    (
        UserId                  int NOT NULL CONSTRAINT PK_UserNotificationSettings PRIMARY KEY,
        PushEnabled             bit NOT NULL CONSTRAINT DF_UNS_Push DEFAULT (1),
        TaskAssignedEnabled     bit NOT NULL CONSTRAINT DF_UNS_Assigned DEFAULT (1),
        TaskDeadlineEnabled     bit NOT NULL CONSTRAINT DF_UNS_Deadline DEFAULT (1),
        CommentsEnabled         bit NOT NULL CONSTRAINT DF_UNS_Comments DEFAULT (1),
        PlanUpdatesEnabled      bit NOT NULL CONSTRAINT DF_UNS_Plan DEFAULT (1),
        QuietHoursStart         time(0) NULL,
        QuietHoursEnd           time(0) NULL,
        UpdatedAt               datetime2(0) NOT NULL CONSTRAINT DF_UNS_UpdatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_UserNotificationSettings_Users FOREIGN KEY (UserId) REFERENCES dsa.Users(UserId)
    );
END;

IF OBJECT_ID(N'dsa.Notifications', N'U') IS NULL
BEGIN
    CREATE TABLE dsa.Notifications
    (
        NotificationId  bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_Notifications PRIMARY KEY,
        UserId          int NOT NULL,
        Type            varchar(40) NOT NULL,
        Title           nvarchar(200) NOT NULL,
        Message         nvarchar(1000) NOT NULL,
        RelatedTaskId   bigint NULL,
        RelatedPlanId   int NULL,
        CreatedAt       datetime2(0) NOT NULL CONSTRAINT DF_Notifications_CreatedAt DEFAULT (SYSUTCDATETIME()),
        ReadAt          datetime2(0) NULL,
        DeliveredAt     datetime2(0) NULL,
        CONSTRAINT FK_Notifications_Users FOREIGN KEY (UserId) REFERENCES dsa.Users(UserId),
        CONSTRAINT FK_Notifications_Tasks FOREIGN KEY (RelatedTaskId) REFERENCES dsa.Tasks(TaskId),
        CONSTRAINT FK_Notifications_Plans FOREIGN KEY (RelatedPlanId) REFERENCES dsa.Plans(PlanId)
    );
    CREATE INDEX IX_Notifications_User_Unread ON dsa.Notifications(UserId, CreatedAt DESC) WHERE ReadAt IS NULL;
END;

-------------------------------------------------------------------------------
-- 6. Authentication sessions and audit trail
-------------------------------------------------------------------------------

IF OBJECT_ID(N'dsa.RefreshTokens', N'U') IS NULL
BEGIN
    CREATE TABLE dsa.RefreshTokens
    (
        RefreshTokenId  bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_RefreshTokens PRIMARY KEY,
        UserId          int NOT NULL,
        TokenHash       varbinary(32) NOT NULL,
        CreatedAt       datetime2(0) NOT NULL CONSTRAINT DF_RefreshTokens_CreatedAt DEFAULT (SYSUTCDATETIME()),
        ExpiresAt       datetime2(0) NOT NULL,
        RevokedAt       datetime2(0) NULL,
        ReplacedByTokenId bigint NULL,
        DeviceName      nvarchar(200) NULL,
        IpAddress       varchar(45) NULL,
        CONSTRAINT FK_RefreshTokens_Users FOREIGN KEY (UserId) REFERENCES dsa.Users(UserId),
        CONSTRAINT FK_RefreshTokens_ReplacedBy FOREIGN KEY (ReplacedByTokenId) REFERENCES dsa.RefreshTokens(RefreshTokenId),
        CONSTRAINT UQ_RefreshTokens_TokenHash UNIQUE (TokenHash),
        CONSTRAINT CK_RefreshTokens_Expiry CHECK (ExpiresAt > CreatedAt)
    );
    CREATE INDEX IX_RefreshTokens_User_Active ON dsa.RefreshTokens(UserId, ExpiresAt) WHERE RevokedAt IS NULL;
END;

IF OBJECT_ID(N'dsa.AuditLogs', N'U') IS NULL
BEGIN
    CREATE TABLE dsa.AuditLogs
    (
        AuditLogId      bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_AuditLogs PRIMARY KEY,
        UserId          int NULL,
        Action          nvarchar(100) NOT NULL,
        EntityName      nvarchar(100) NOT NULL,
        EntityId        nvarchar(100) NULL,
        Details         nvarchar(max) NULL,
        IpAddress       varchar(45) NULL,
        UserAgent       nvarchar(500) NULL,
        CreatedAt       datetime2(0) NOT NULL CONSTRAINT DF_AuditLogs_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_AuditLogs_Users FOREIGN KEY (UserId) REFERENCES dsa.Users(UserId)
    );
    CREATE INDEX IX_AuditLogs_CreatedAt ON dsa.AuditLogs(CreatedAt DESC);
    CREATE INDEX IX_AuditLogs_User_CreatedAt ON dsa.AuditLogs(UserId, CreatedAt DESC);
END;

-------------------------------------------------------------------------------
-- 7. Helpful views for dashboards and overdue-task reporting
-------------------------------------------------------------------------------

IF OBJECT_ID(N'dsa.vw_TaskSummary', N'V') IS NOT NULL
    DROP VIEW dsa.vw_TaskSummary;
EXEC(N'
CREATE VIEW dsa.vw_TaskSummary
AS
SELECT
    t.TaskId,
    t.Title,
    t.Status,
    t.Priority,
    t.Deadline,
    t.GroupId,
    g.Name AS GroupName,
    t.AssignedUserId,
    u.FirstName,
    u.LastName,
    t.ProductId,
    p.Name AS ProductName,
    t.CreatedById,
    t.CreatedAt,
    CASE
        WHEN t.Status NOT IN (''Completed'', ''Cancelled'') AND t.Deadline < SYSUTCDATETIME()
        THEN CAST(1 AS bit) ELSE CAST(0 AS bit)
    END AS IsOverdue
FROM dsa.Tasks AS t
LEFT JOIN dsa.Groups AS g ON g.GroupId = t.GroupId
LEFT JOIN dsa.Users AS u ON u.UserId = t.AssignedUserId
LEFT JOIN dsa.Products AS p ON p.ProductId = t.ProductId;
');

IF OBJECT_ID(N'dsa.vw_PlanProgress', N'V') IS NOT NULL
    DROP VIEW dsa.vw_PlanProgress;
EXEC(N'
CREATE VIEW dsa.vw_PlanProgress
AS
SELECT
    p.PlanId,
    p.Name AS PlanName,
    p.GroupId,
    g.Name AS GroupName,
    p.PeriodType,
    p.StartDate,
    p.EndDate,
    p.Status,
    COUNT(pi.PlanItemId) AS ItemCount,
    SUM(COALESCE(pi.TargetValue, 0)) AS TotalTarget,
    SUM(COALESCE(pi.ActualValue, 0)) AS TotalActual,
    CAST(
        CASE WHEN SUM(COALESCE(pi.TargetValue, 0)) = 0 THEN 0
             ELSE 100.0 * SUM(COALESCE(pi.ActualValue, 0)) / SUM(pi.TargetValue)
        END AS decimal(6,2)
    ) AS ProgressPercent
FROM dsa.Plans AS p
JOIN dsa.Groups AS g ON g.GroupId = p.GroupId
LEFT JOIN dsa.PlanItems AS pi ON pi.PlanId = p.PlanId
GROUP BY p.PlanId, p.Name, p.GroupId, g.Name, p.PeriodType,
         p.StartDate, p.EndDate, p.Status;
');

COMMIT TRANSACTION;

PRINT N'DSA Manager schema created/verified successfully.';
PRINT N'All application objects are in schema [dsa].';
