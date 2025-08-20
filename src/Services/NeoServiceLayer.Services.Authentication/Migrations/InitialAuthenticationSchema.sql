-- Initial Authentication Schema Migration
-- Version: 1.0.0
-- Date: 2024

-- Create Users table
CREATE TABLE Users (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    Username NVARCHAR(50) NOT NULL,
    Email NVARCHAR(100) NOT NULL,
    PasswordHash NVARCHAR(MAX) NOT NULL,
    EmailVerified BIT NOT NULL DEFAULT 0,
    EmailVerificationToken NVARCHAR(MAX) NULL,
    EmailVerificationTokenExpiry DATETIME2 NULL,
    TwoFactorEnabled BIT NOT NULL DEFAULT 0,
    TwoFactorSecret NVARCHAR(MAX) NULL,
    PasswordResetToken NVARCHAR(MAX) NULL,
    PasswordResetTokenExpiry DATETIME2 NULL,
    FailedLoginAttempts INT NOT NULL DEFAULT 0,
    LockoutEnd DATETIME2 NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL,
    LastLoginAt DATETIME2 NULL,
    LastLoginIp NVARCHAR(45) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    IsDeleted BIT NOT NULL DEFAULT 0,
    CONSTRAINT UQ_Users_Username UNIQUE (Username),
    CONSTRAINT UQ_Users_Email UNIQUE (Email)
);

CREATE INDEX IX_Users_EmailVerificationToken ON Users(EmailVerificationToken) WHERE EmailVerificationToken IS NOT NULL;
CREATE INDEX IX_Users_PasswordResetToken ON Users(PasswordResetToken) WHERE PasswordResetToken IS NOT NULL;

-- Create Roles table
CREATE TABLE Roles (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    Name NVARCHAR(50) NOT NULL,
    Description NVARCHAR(200) NULL,
    IsSystemRole BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT UQ_Roles_Name UNIQUE (Name)
);

-- Create Permissions table
CREATE TABLE Permissions (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    Name NVARCHAR(100) NOT NULL,
    Description NVARCHAR(200) NULL,
    Resource NVARCHAR(50) NOT NULL,
    Action NVARCHAR(50) NOT NULL,
    CONSTRAINT UQ_Permissions_Resource_Action UNIQUE (Resource, Action)
);

-- Create UserRoles junction table
CREATE TABLE UserRoles (
    UserId UNIQUEIDENTIFIER NOT NULL,
    RoleId UNIQUEIDENTIFIER NOT NULL,
    AssignedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ExpiresAt DATETIME2 NULL,
    AssignedBy NVARCHAR(MAX) NULL,
    CONSTRAINT PK_UserRoles PRIMARY KEY (UserId, RoleId),
    CONSTRAINT FK_UserRoles_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    CONSTRAINT FK_UserRoles_Roles FOREIGN KEY (RoleId) REFERENCES Roles(Id) ON DELETE CASCADE
);

CREATE INDEX IX_UserRoles_UserId ON UserRoles(UserId);
CREATE INDEX IX_UserRoles_RoleId ON UserRoles(RoleId);

-- Create RolePermissions junction table
CREATE TABLE RolePermissions (
    RoleId UNIQUEIDENTIFIER NOT NULL,
    PermissionId UNIQUEIDENTIFIER NOT NULL,
    GrantedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT PK_RolePermissions PRIMARY KEY (RoleId, PermissionId),
    CONSTRAINT FK_RolePermissions_Roles FOREIGN KEY (RoleId) REFERENCES Roles(Id) ON DELETE CASCADE,
    CONSTRAINT FK_RolePermissions_Permissions FOREIGN KEY (PermissionId) REFERENCES Permissions(Id) ON DELETE CASCADE
);

CREATE INDEX IX_RolePermissions_RoleId ON RolePermissions(RoleId);
CREATE INDEX IX_RolePermissions_PermissionId ON RolePermissions(PermissionId);

-- Create UserPermissions table (for direct user permissions)
CREATE TABLE UserPermissions (
    UserId UNIQUEIDENTIFIER NOT NULL,
    PermissionId UNIQUEIDENTIFIER NOT NULL,
    IsGranted BIT NOT NULL DEFAULT 1,
    AssignedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ExpiresAt DATETIME2 NULL,
    CONSTRAINT PK_UserPermissions PRIMARY KEY (UserId, PermissionId),
    CONSTRAINT FK_UserPermissions_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    CONSTRAINT FK_UserPermissions_Permissions FOREIGN KEY (PermissionId) REFERENCES Permissions(Id) ON DELETE CASCADE
);

CREATE INDEX IX_UserPermissions_UserId ON UserPermissions(UserId);
CREATE INDEX IX_UserPermissions_PermissionId ON UserPermissions(PermissionId);

-- Create RefreshTokens table
CREATE TABLE RefreshTokens (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    Token NVARCHAR(MAX) NOT NULL,
    UserId UNIQUEIDENTIFIER NOT NULL,
    JwtId NVARCHAR(MAX) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ExpiresAt DATETIME2 NOT NULL,
    RevokedAt DATETIME2 NULL,
    RevokedBy NVARCHAR(MAX) NULL,
    RevokedReason NVARCHAR(MAX) NULL,
    ReplacedByToken NVARCHAR(MAX) NULL,
    CreatedByIp NVARCHAR(45) NULL,
    RevokedByIp NVARCHAR(45) NULL,
    CONSTRAINT FK_RefreshTokens_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);

CREATE INDEX IX_RefreshTokens_Token ON RefreshTokens(Token(255));
CREATE INDEX IX_RefreshTokens_JwtId ON RefreshTokens(JwtId(255)) WHERE JwtId IS NOT NULL;
CREATE INDEX IX_RefreshTokens_UserId ON RefreshTokens(UserId);
CREATE INDEX IX_RefreshTokens_ExpiresAt ON RefreshTokens(ExpiresAt);

-- Create UserSessions table
CREATE TABLE UserSessions (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    SessionId NVARCHAR(MAX) NOT NULL,
    UserId UNIQUEIDENTIFIER NOT NULL,
    IpAddress NVARCHAR(45) NOT NULL,
    UserAgent NVARCHAR(MAX) NULL,
    DeviceInfo NVARCHAR(MAX) NULL,
    Location NVARCHAR(MAX) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    LastActivityAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ExpiredAt DATETIME2 NULL,
    TerminatedAt DATETIME2 NULL,
    TerminationReason NVARCHAR(MAX) NULL,
    CONSTRAINT FK_UserSessions_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);

CREATE INDEX IX_UserSessions_SessionId ON UserSessions(SessionId(255));
CREATE INDEX IX_UserSessions_UserId ON UserSessions(UserId);
CREATE INDEX IX_UserSessions_LastActivityAt ON UserSessions(LastActivityAt);

-- Create BackupCodes table
CREATE TABLE BackupCodes (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    Code NVARCHAR(MAX) NOT NULL,
    IsUsed BIT NOT NULL DEFAULT 0,
    UsedAt DATETIME2 NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_BackupCodes_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    CONSTRAINT UQ_BackupCodes_UserId_Code UNIQUE (UserId, Code(50))
);

-- Create BlacklistedTokens table
CREATE TABLE BlacklistedTokens (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    TokenHash NVARCHAR(MAX) NOT NULL,
    JwtId NVARCHAR(MAX) NULL,
    UserId UNIQUEIDENTIFIER NULL,
    BlacklistedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ExpiresAt DATETIME2 NOT NULL,
    Reason NVARCHAR(MAX) NULL,
    BlacklistedBy NVARCHAR(MAX) NULL
);

CREATE UNIQUE INDEX IX_BlacklistedTokens_TokenHash ON BlacklistedTokens(TokenHash(255));
CREATE INDEX IX_BlacklistedTokens_JwtId ON BlacklistedTokens(JwtId(255)) WHERE JwtId IS NOT NULL;
CREATE INDEX IX_BlacklistedTokens_UserId ON BlacklistedTokens(UserId) WHERE UserId IS NOT NULL;
CREATE INDEX IX_BlacklistedTokens_ExpiresAt ON BlacklistedTokens(ExpiresAt);

-- Create AuditLogs table
CREATE TABLE AuditLogs (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NULL,
    EventType NVARCHAR(50) NOT NULL,
    EventCategory NVARCHAR(100) NOT NULL,
    Description NVARCHAR(MAX) NULL,
    IpAddress NVARCHAR(45) NULL,
    UserAgent NVARCHAR(MAX) NULL,
    Success BIT NOT NULL DEFAULT 1,
    FailureReason NVARCHAR(MAX) NULL,
    OldValues NVARCHAR(MAX) NULL,
    NewValues NVARCHAR(MAX) NULL,
    Metadata NVARCHAR(MAX) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_AuditLogs_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE SET NULL
);

CREATE INDEX IX_AuditLogs_UserId ON AuditLogs(UserId) WHERE UserId IS NOT NULL;
CREATE INDEX IX_AuditLogs_EventType ON AuditLogs(EventType);
CREATE INDEX IX_AuditLogs_EventCategory ON AuditLogs(EventCategory);
CREATE INDEX IX_AuditLogs_CreatedAt ON AuditLogs(CreatedAt);

-- Create LoginAttempts table
CREATE TABLE LoginAttempts (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    Username NVARCHAR(MAX) NULL,
    Email NVARCHAR(MAX) NULL,
    UserId UNIQUEIDENTIFIER NULL,
    IpAddress NVARCHAR(45) NOT NULL,
    UserAgent NVARCHAR(MAX) NULL,
    Success BIT NOT NULL DEFAULT 0,
    FailureReason NVARCHAR(MAX) NULL,
    AttemptedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

CREATE INDEX IX_LoginAttempts_IpAddress ON LoginAttempts(IpAddress);
CREATE INDEX IX_LoginAttempts_UserId ON LoginAttempts(UserId) WHERE UserId IS NOT NULL;
CREATE INDEX IX_LoginAttempts_AttemptedAt ON LoginAttempts(AttemptedAt);

-- Create PasswordHistories table
CREATE TABLE PasswordHistories (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    PasswordHash NVARCHAR(MAX) NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_PasswordHistories_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);

CREATE INDEX IX_PasswordHistories_UserId ON PasswordHistories(UserId);
CREATE INDEX IX_PasswordHistories_CreatedAt ON PasswordHistories(CreatedAt);

-- Create RateLimitEntries table
CREATE TABLE RateLimitEntries (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    [Key] NVARCHAR(255) NOT NULL,
    Action NVARCHAR(255) NOT NULL,
    Count INT NOT NULL DEFAULT 0,
    WindowStart DATETIME2 NOT NULL,
    WindowEnd DATETIME2 NOT NULL,
    IsBlocked BIT NOT NULL DEFAULT 0,
    BlockedUntil DATETIME2 NULL,
    CONSTRAINT UQ_RateLimitEntries_Key_Action UNIQUE ([Key], Action)
);

CREATE INDEX IX_RateLimitEntries_WindowEnd ON RateLimitEntries(WindowEnd);

-- Create EmailTemplates table
CREATE TABLE EmailTemplates (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    Name NVARCHAR(50) NOT NULL,
    Subject NVARCHAR(200) NOT NULL,
    HtmlBody NVARCHAR(MAX) NOT NULL,
    TextBody NVARCHAR(MAX) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL,
    CONSTRAINT UQ_EmailTemplates_Name UNIQUE (Name)
);

-- Insert default roles
INSERT INTO Roles (Id, Name, Description, IsSystemRole, CreatedAt)
VALUES 
    ('11111111-1111-1111-1111-111111111111', 'Admin', 'System administrator with full access', 1, GETUTCDATE()),
    ('22222222-2222-2222-2222-222222222222', 'User', 'Standard user with basic access', 1, GETUTCDATE());

-- Insert default permissions
INSERT INTO Permissions (Id, Name, Description, Resource, Action)
VALUES
    ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'users.read', 'Read user information', 'users', 'read'),
    ('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 'users.write', 'Create and update users', 'users', 'write'),
    ('cccccccc-cccc-cccc-cccc-cccccccccccc', 'users.delete', 'Delete users', 'users', 'delete'),
    ('dddddddd-dddd-dddd-dddd-dddddddddddd', 'roles.manage', 'Manage roles and permissions', 'roles', 'manage'),
    ('eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee', 'system.admin', 'Full system administration', 'system', 'admin');

-- Assign permissions to roles
-- Admin gets all permissions
INSERT INTO RolePermissions (RoleId, PermissionId, GrantedAt)
VALUES
    ('11111111-1111-1111-1111-111111111111', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', GETUTCDATE()),
    ('11111111-1111-1111-1111-111111111111', 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', GETUTCDATE()),
    ('11111111-1111-1111-1111-111111111111', 'cccccccc-cccc-cccc-cccc-cccccccccccc', GETUTCDATE()),
    ('11111111-1111-1111-1111-111111111111', 'dddddddd-dddd-dddd-dddd-dddddddddddd', GETUTCDATE()),
    ('11111111-1111-1111-1111-111111111111', 'eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee', GETUTCDATE());

-- User role gets read permission
INSERT INTO RolePermissions (RoleId, PermissionId, GrantedAt)
VALUES
    ('22222222-2222-2222-2222-222222222222', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', GETUTCDATE());

-- Insert email templates
INSERT INTO EmailTemplates (Id, Name, Subject, HtmlBody, TextBody, IsActive, CreatedAt)
VALUES
    ('11111111-2222-3333-4444-555555555555', 'EmailVerification', 'Verify your email address',
     '<html><body><h2>Email Verification</h2><p>Hello {{Username}},</p><p>Please click the link below to verify your email address:</p><p><a href="{{VerificationLink}}">Verify Email</a></p><p>This link will expire in 24 hours.</p><p>Best regards,<br>The Team</p></body></html>',
     'Email Verification\n\nHello {{Username}},\n\nPlease click the link below to verify your email address:\n{{VerificationLink}}\n\nThis link will expire in 24 hours.\n\nBest regards,\nThe Team',
     1, GETUTCDATE()),
    
    ('22222222-3333-4444-5555-666666666666', 'PasswordReset', 'Password Reset Request',
     '<html><body><h2>Password Reset</h2><p>Hello {{Username}},</p><p>We received a request to reset your password. Click the link below to reset it:</p><p><a href="{{ResetLink}}">Reset Password</a></p><p>This link will expire in 1 hour.</p><p>If you didn''t request this, please ignore this email.</p><p>Best regards,<br>The Team</p></body></html>',
     'Password Reset\n\nHello {{Username}},\n\nWe received a request to reset your password. Click the link below to reset it:\n{{ResetLink}}\n\nThis link will expire in 1 hour.\n\nIf you didn''t request this, please ignore this email.\n\nBest regards,\nThe Team',
     1, GETUTCDATE()),
    
    ('33333333-4444-5555-6666-777777777777', 'TwoFactorCode', 'Your Two-Factor Authentication Code',
     '<html><body><h2>Two-Factor Authentication</h2><p>Hello {{Username}},</p><p>Your authentication code is: <strong>{{Code}}</strong></p><p>This code will expire in 5 minutes.</p><p>If you didn''t request this, please contact support immediately.</p><p>Best regards,<br>The Team</p></body></html>',
     'Two-Factor Authentication\n\nHello {{Username}},\n\nYour authentication code is: {{Code}}\n\nThis code will expire in 5 minutes.\n\nIf you didn''t request this, please contact support immediately.\n\nBest regards,\nThe Team',
     1, GETUTCDATE());

-- Create stored procedures for common operations

-- Cleanup expired tokens
CREATE PROCEDURE CleanupExpiredTokens
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Delete expired refresh tokens
    DELETE FROM RefreshTokens 
    WHERE ExpiresAt < GETUTCDATE() AND RevokedAt IS NULL;
    
    -- Delete expired blacklisted tokens
    DELETE FROM BlacklistedTokens 
    WHERE ExpiresAt < GETUTCDATE();
    
    -- Delete expired sessions
    UPDATE UserSessions 
    SET TerminatedAt = GETUTCDATE(), 
        TerminationReason = 'Session expired'
    WHERE ExpiredAt < GETUTCDATE() 
      AND TerminatedAt IS NULL;
    
    -- Delete old login attempts (older than 30 days)
    DELETE FROM LoginAttempts 
    WHERE AttemptedAt < DATEADD(DAY, -30, GETUTCDATE());
    
    -- Delete old audit logs (older than 90 days, keep security events for 1 year)
    DELETE FROM AuditLogs 
    WHERE CreatedAt < DATEADD(DAY, -90, GETUTCDATE())
      AND EventCategory != 'Security';
    
    DELETE FROM AuditLogs 
    WHERE CreatedAt < DATEADD(YEAR, -1, GETUTCDATE());
END;

-- Get user permissions (including role-based)
CREATE PROCEDURE GetUserEffectivePermissions
    @UserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Get permissions from roles
    SELECT DISTINCT p.*
    FROM Permissions p
    INNER JOIN RolePermissions rp ON p.Id = rp.PermissionId
    INNER JOIN UserRoles ur ON rp.RoleId = ur.RoleId
    WHERE ur.UserId = @UserId 
      AND (ur.ExpiresAt IS NULL OR ur.ExpiresAt > GETUTCDATE())
    
    UNION
    
    -- Get direct user permissions (granted)
    SELECT DISTINCT p.*
    FROM Permissions p
    INNER JOIN UserPermissions up ON p.Id = up.PermissionId
    WHERE up.UserId = @UserId 
      AND up.IsGranted = 1
      AND (up.ExpiresAt IS NULL OR up.ExpiresAt > GETUTCDATE());
END;