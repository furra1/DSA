/* Safe additive upgrade. Existing task rows are retained. */
IF COL_LENGTH('dsa.Tasks','ProgressTarget') IS NULL
    ALTER TABLE [dsa].[Tasks] ADD [ProgressTarget] int NULL;
GO
IF COL_LENGTH('dsa.Tasks','ProgressCompleted') IS NULL
    ALTER TABLE [dsa].[Tasks] ADD [ProgressCompleted] int NOT NULL
        CONSTRAINT [DF_Tasks_ProgressCompleted] DEFAULT (0) WITH VALUES;
GO
