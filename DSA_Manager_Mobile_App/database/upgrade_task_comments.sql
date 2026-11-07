-- Add task discussion/comments to an existing DSA Manager database.
IF OBJECT_ID(N'dsa.TaskComments', N'U') IS NULL
BEGIN
    CREATE TABLE dsa.TaskComments
    (
        CommentId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_TaskComments PRIMARY KEY,
        TaskId bigint NOT NULL,
        UserId int NOT NULL,
        Body nvarchar(2000) NOT NULL,
        CreatedAt datetime2(0) NOT NULL CONSTRAINT DF_TaskComments_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_TaskComments_Tasks FOREIGN KEY (TaskId) REFERENCES dsa.Tasks(TaskId) ON DELETE CASCADE,
        CONSTRAINT FK_TaskComments_Users FOREIGN KEY (UserId) REFERENCES dsa.Users(UserId)
    );
    CREATE INDEX IX_TaskComments_Task_CreatedAt ON dsa.TaskComments(TaskId, CreatedAt);
END;
GO
