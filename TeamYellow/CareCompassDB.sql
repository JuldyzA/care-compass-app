IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [AspNetRoles] (
    [Id] nvarchar(450) NOT NULL,
    [Name] nvarchar(256) NULL,
    [NormalizedName] nvarchar(256) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [AspNetUsers] (
    [Id] nvarchar(450) NOT NULL,
    [UserName] nvarchar(256) NULL,
    [NormalizedUserName] nvarchar(256) NULL,
    [Email] nvarchar(256) NULL,
    [NormalizedEmail] nvarchar(256) NULL,
    [EmailConfirmed] bit NOT NULL,
    [PasswordHash] nvarchar(max) NULL,
    [SecurityStamp] nvarchar(max) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    [PhoneNumber] nvarchar(max) NULL,
    [PhoneNumberConfirmed] bit NOT NULL,
    [TwoFactorEnabled] bit NOT NULL,
    [LockoutEnd] datetimeoffset NULL,
    [LockoutEnabled] bit NOT NULL,
    [AccessFailedCount] int NOT NULL,
    CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Discount] (
    [pkDiscountId] int NOT NULL IDENTITY,
    [discountCode] nvarchar(40) NOT NULL,
    [discountType] nvarchar(max) NOT NULL DEFAULT N'%',
    [value] decimal(10,2) NOT NULL,
    [startDateTime] datetime2 NOT NULL,
    [endDateTime] datetime2 NOT NULL,
    [createdAt] datetime2 NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    CONSTRAINT [PK_Discount] PRIMARY KEY ([pkDiscountId])
);
GO

CREATE TABLE [Plan] (
    [pkPlanId] int NOT NULL IDENTITY,
    [planName] nvarchar(80) NOT NULL,
    [planDescription] nvarchar(500) NOT NULL,
    [price] decimal(10,2) NOT NULL,
    [billingType] nvarchar(max) NOT NULL,
    [isActive] bit NOT NULL,
    [createdAt] datetime2 NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    CONSTRAINT [PK_Plan] PRIMARY KEY ([pkPlanId])
);
GO

CREATE TABLE [AspNetRoleClaims] (
    [Id] int NOT NULL IDENTITY,
    [RoleId] nvarchar(450) NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [AspNetUserClaims] (
    [Id] int NOT NULL IDENTITY,
    [UserId] nvarchar(450) NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [AspNetUserLogins] (
    [LoginProvider] nvarchar(450) NOT NULL,
    [ProviderKey] nvarchar(450) NOT NULL,
    [ProviderDisplayName] nvarchar(max) NULL,
    [UserId] nvarchar(450) NOT NULL,
    CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
    CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [AspNetUserRoles] (
    [UserId] nvarchar(450) NOT NULL,
    [RoleId] nvarchar(450) NOT NULL,
    CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
    CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [AspNetUserTokens] (
    [UserId] nvarchar(450) NOT NULL,
    [LoginProvider] nvarchar(450) NOT NULL,
    [Name] nvarchar(450) NOT NULL,
    [Value] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
    CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [Counsellor] (
    [pkCounsellorId] int NOT NULL IDENTITY,
    [practitionerLicenceId] nvarchar(50) NOT NULL,
    [displayName] nvarchar(100) NOT NULL,
    [isActive] bit NOT NULL,
    [createdAt] datetime2 NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    [fkUserId] nvarchar(450) NULL,
    [archivedNormalizedEmail] nvarchar(256) NULL,
    [archivedEmailDisplay] nvarchar(256) NULL,
    CONSTRAINT [PK_Counsellor] PRIMARY KEY ([pkCounsellorId]),
    CONSTRAINT [FK_Counsellor_AspNetUsers_fkUserId] FOREIGN KEY ([fkUserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE SET NULL
);
GO

CREATE TABLE [UserLog] (
    [pkLogId] int NOT NULL IDENTITY,
    [logInTime] datetime2 NOT NULL,
    [logOutTime] datetime2 NULL,
    [abandoned] bit NOT NULL,
    [fkUserId] nvarchar(450) NULL,
    [userEmailSnapshot] nvarchar(256) NULL,
    CONSTRAINT [PK_UserLog] PRIMARY KEY ([pkLogId]),
    CONSTRAINT [FK_UserLog_AspNetUsers_fkUserId] FOREIGN KEY ([fkUserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE SET NULL
);
GO

CREATE TABLE [UserProfile] (
    [pkUserProfileId] int NOT NULL IDENTITY,
    [firstName] nvarchar(50) NOT NULL,
    [lastName] nvarchar(50) NOT NULL,
    [phone] nvarchar(20) NULL,
    [createdAt] datetime2 NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    [updatedAt] datetime2 NULL,
    [profilePhotoUrl] nvarchar(max) NULL,
    [unitNumber] int NULL,
    [street] nvarchar(120) NULL,
    [city] nvarchar(80) NULL,
    [province] nvarchar(2) NULL,
    [postalCode] nvarchar(7) NULL,
    [fkUserId] nvarchar(450) NOT NULL,
    CONSTRAINT [PK_UserProfile] PRIMARY KEY ([pkUserProfileId]),
    CONSTRAINT [FK_UserProfile_AspNetUsers_fkUserId] FOREIGN KEY ([fkUserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [PlanDiscount] (
    [fkPlanId] int NOT NULL,
    [fkDiscountId] int NOT NULL,
    CONSTRAINT [PK_PlanDiscount] PRIMARY KEY ([fkPlanId], [fkDiscountId]),
    CONSTRAINT [FK_PlanDiscount_Discount_fkDiscountId] FOREIGN KEY ([fkDiscountId]) REFERENCES [Discount] ([pkDiscountId]) ON DELETE CASCADE,
    CONSTRAINT [FK_PlanDiscount_Plan_fkPlanId] FOREIGN KEY ([fkPlanId]) REFERENCES [Plan] ([pkPlanId]) ON DELETE CASCADE
);
GO

CREATE TABLE [PlanFeature] (
    [pkPlanFeatureId] int NOT NULL IDENTITY,
    [featureName] nvarchar(120) NOT NULL,
    [featureDescription] nvarchar(300) NOT NULL,
    [sortOrder] int NOT NULL,
    [fkPlanId] int NOT NULL,
    CONSTRAINT [PK_PlanFeature] PRIMARY KEY ([pkPlanFeatureId]),
    CONSTRAINT [FK_PlanFeature_Plan_fkPlanId] FOREIGN KEY ([fkPlanId]) REFERENCES [Plan] ([pkPlanId]) ON DELETE CASCADE
);
GO

CREATE TABLE [Client] (
    [pkClientId] int NOT NULL IDENTITY,
    [firstName] nvarchar(50) NOT NULL,
    [lastName] nvarchar(50) NOT NULL,
    [email] nvarchar(255) NOT NULL,
    [phone] nvarchar(20) NOT NULL,
    [status] nvarchar(20) NOT NULL,
    [createdAt] datetime2 NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    [fkCounsellorId] int NOT NULL,
    CONSTRAINT [PK_Client] PRIMARY KEY ([pkClientId]),
    CONSTRAINT [FK_Client_Counsellor_fkCounsellorId] FOREIGN KEY ([fkCounsellorId]) REFERENCES [Counsellor] ([pkCounsellorId]) ON DELETE CASCADE
);
GO

CREATE TABLE [Subscription] (
    [pkSubscriptionId] int NOT NULL IDENTITY,
    [status] nvarchar(20) NOT NULL,
    [cycleStart] datetime2 NOT NULL,
    [cycleEnd] datetime2 NOT NULL,
    [updatedAt] datetime2 NOT NULL,
    [fkPlanId] int NOT NULL,
    [fkCounsellorId] int NOT NULL,
    CONSTRAINT [PK_Subscription] PRIMARY KEY ([pkSubscriptionId]),
    CONSTRAINT [FK_Subscription_Counsellor_fkCounsellorId] FOREIGN KEY ([fkCounsellorId]) REFERENCES [Counsellor] ([pkCounsellorId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Subscription_Plan_fkPlanId] FOREIGN KEY ([fkPlanId]) REFERENCES [Plan] ([pkPlanId]) ON DELETE NO ACTION
);
GO

CREATE TABLE [PaymentTransaction] (
    [pkPaymentTransactionId] int NOT NULL IDENTITY,
    [payerName] nvarchar(100) NOT NULL,
    [amount] decimal(10,2) NOT NULL,
    [currency] char(3) NOT NULL,
    [provider] nvarchar(20) NOT NULL,
    [providerOrderId] nvarchar(100) NOT NULL,
    [status] nvarchar(20) NOT NULL,
    [paidAt] datetime2 NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    [fkSubscriptionId] int NOT NULL,
    [fkDiscountId] int NULL,
    CONSTRAINT [PK_PaymentTransaction] PRIMARY KEY ([pkPaymentTransactionId]),
    CONSTRAINT [FK_PaymentTransaction_Discount_fkDiscountId] FOREIGN KEY ([fkDiscountId]) REFERENCES [Discount] ([pkDiscountId]) ON DELETE SET NULL,
    CONSTRAINT [FK_PaymentTransaction_Subscription_fkSubscriptionId] FOREIGN KEY ([fkSubscriptionId]) REFERENCES [Subscription] ([pkSubscriptionId]) ON DELETE CASCADE
);
GO

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'pkDiscountId', N'createdAt', N'discountCode', N'endDateTime', N'startDateTime', N'value') AND [object_id] = OBJECT_ID(N'[Discount]'))
    SET IDENTITY_INSERT [Discount] ON;
INSERT INTO [Discount] ([pkDiscountId], [createdAt], [discountCode], [endDateTime], [startDateTime], [value])
VALUES (1, '2025-01-01T00:00:00.0000000', N'WELCOME10', '9999-12-31T00:00:00.0000000', '2025-01-01T00:00:00.0000000', 10.0);
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'pkDiscountId', N'createdAt', N'discountCode', N'endDateTime', N'startDateTime', N'value') AND [object_id] = OBJECT_ID(N'[Discount]'))
    SET IDENTITY_INSERT [Discount] OFF;
GO

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'pkDiscountId', N'createdAt', N'discountCode', N'discountType', N'endDateTime', N'startDateTime', N'value') AND [object_id] = OBJECT_ID(N'[Discount]'))
    SET IDENTITY_INSERT [Discount] ON;
INSERT INTO [Discount] ([pkDiscountId], [createdAt], [discountCode], [discountType], [endDateTime], [startDateTime], [value])
VALUES (2, '2025-01-01T00:00:00.0000000', N'YEARLY50', N'$', '9999-12-31T00:00:00.0000000', '2025-01-01T00:00:00.0000000', 50.0);
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'pkDiscountId', N'createdAt', N'discountCode', N'discountType', N'endDateTime', N'startDateTime', N'value') AND [object_id] = OBJECT_ID(N'[Discount]'))
    SET IDENTITY_INSERT [Discount] OFF;
GO

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'pkPlanId', N'billingType', N'createdAt', N'isActive', N'planDescription', N'planName', N'price') AND [object_id] = OBJECT_ID(N'[Plan]'))
    SET IDENTITY_INSERT [Plan] ON;
INSERT INTO [Plan] ([pkPlanId], [billingType], [createdAt], [isActive], [planDescription], [planName], [price])
VALUES (1, N'Free', '2025-01-01T00:00:00.0000000', CAST(1 AS bit), N'Basic access for new counsellors exploring the platform', N'Free', 0.0),
(2, N'Monthly', '2025-01-01T00:00:00.0000000', CAST(1 AS bit), N'Full platform access with flexible month-to-month billing', N'Monthly', 49.99),
(3, N'Yearly', '2025-01-01T00:00:00.0000000', CAST(1 AS bit), N'Full platform access with annual billing and better long-term value', N'Yearly', 499.99);
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'pkPlanId', N'billingType', N'createdAt', N'isActive', N'planDescription', N'planName', N'price') AND [object_id] = OBJECT_ID(N'[Plan]'))
    SET IDENTITY_INSERT [Plan] OFF;
GO

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'fkDiscountId', N'fkPlanId') AND [object_id] = OBJECT_ID(N'[PlanDiscount]'))
    SET IDENTITY_INSERT [PlanDiscount] ON;
INSERT INTO [PlanDiscount] ([fkDiscountId], [fkPlanId])
VALUES (1, 2),
(1, 3),
(2, 3);
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'fkDiscountId', N'fkPlanId') AND [object_id] = OBJECT_ID(N'[PlanDiscount]'))
    SET IDENTITY_INSERT [PlanDiscount] OFF;
GO

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'pkPlanFeatureId', N'featureDescription', N'featureName', N'fkPlanId', N'sortOrder') AND [object_id] = OBJECT_ID(N'[PlanFeature]'))
    SET IDENTITY_INSERT [PlanFeature] ON;
INSERT INTO [PlanFeature] ([pkPlanFeatureId], [featureDescription], [featureName], [fkPlanId], [sortOrder])
VALUES (1, N'Access to limited resources and tools.', N'Basic Access', 1, 1),
(2, N'Access to community forum support.', N'Community Support', 1, 2),
(3, N'Includes all Free plan features.', N'All Free Features', 2, 1),
(4, N'Get help faster with priority support.', N'Priority Support', 2, 2),
(5, N'Access to detailed reports and analytics.', N'Advanced Analytics', 2, 3),
(6, N'Includes all Monthly plan features.', N'All Monthly Features', 3, 1),
(7, N'Assigned a dedicated account manager for support.', N'Dedicated Account Manager', 3, 2),
(8, N'Store unlimited data and files.', N'Unlimited Storage', 3, 3);
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'pkPlanFeatureId', N'featureDescription', N'featureName', N'fkPlanId', N'sortOrder') AND [object_id] = OBJECT_ID(N'[PlanFeature]'))
    SET IDENTITY_INSERT [PlanFeature] OFF;
GO

CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
GO

CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL;
GO

CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
GO

CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
GO

CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
GO

CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
GO

CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL;
GO

CREATE UNIQUE INDEX [IX_Client_email] ON [Client] ([email]);
GO

CREATE INDEX [IX_Client_fkCounsellorId] ON [Client] ([fkCounsellorId]);
GO

CREATE INDEX [IX_Counsellor_archivedNormalizedEmail] ON [Counsellor] ([archivedNormalizedEmail]);
GO

CREATE UNIQUE INDEX [IX_Counsellor_fkUserId] ON [Counsellor] ([fkUserId]) WHERE [fkUserId] IS NOT NULL;
GO

CREATE UNIQUE INDEX [IX_Counsellor_practitionerLicenceId] ON [Counsellor] ([practitionerLicenceId]);
GO

CREATE UNIQUE INDEX [IX_Discount_discountCode] ON [Discount] ([discountCode]);
GO

CREATE INDEX [IX_PaymentTransaction_fkDiscountId] ON [PaymentTransaction] ([fkDiscountId]);
GO

CREATE UNIQUE INDEX [IX_PaymentTransaction_fkSubscriptionId] ON [PaymentTransaction] ([fkSubscriptionId]);
GO

CREATE INDEX [IX_PlanDiscount_fkDiscountId] ON [PlanDiscount] ([fkDiscountId]);
GO

CREATE INDEX [IX_PlanFeature_fkPlanId] ON [PlanFeature] ([fkPlanId]);
GO

CREATE INDEX [IX_Subscription_fkCounsellorId] ON [Subscription] ([fkCounsellorId]);
GO

CREATE INDEX [IX_Subscription_fkPlanId] ON [Subscription] ([fkPlanId]);
GO

CREATE INDEX [IX_UserLog_fkUserId] ON [UserLog] ([fkUserId]);
GO

CREATE UNIQUE INDEX [IX_UserProfile_fkUserId] ON [UserProfile] ([fkUserId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260820234119_InitialSqlServer', N'8.0.22');
GO

COMMIT;
GO

