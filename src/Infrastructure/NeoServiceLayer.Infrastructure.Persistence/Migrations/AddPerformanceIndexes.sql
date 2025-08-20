-- Performance optimization indexes for Neo Service Layer
-- These indexes address the N+1 query problems and missing indexes identified in analysis

-- Transactions table indexes
CREATE INDEX IF NOT EXISTS IX_Transactions_Status_CreatedAt 
ON Transactions(Status, CreatedAt DESC) 
INCLUDE (Id, Amount, FromAddress, ToAddress, TokenAddress);

CREATE INDEX IF NOT EXISTS IX_Transactions_FromAddress_Status 
ON Transactions(FromAddress, Status) 
INCLUDE (Id, ToAddress, Amount, CreatedAt);

CREATE INDEX IF NOT EXISTS IX_Transactions_ToAddress_Status 
ON Transactions(ToAddress, Status) 
INCLUDE (Id, FromAddress, Amount, CreatedAt);

-- Oracle data feeds indexes
CREATE INDEX IF NOT EXISTS IX_OracleDataFeeds_Symbol_Source 
ON OracleDataFeeds(Symbol, Source, UpdatedAt DESC) 
INCLUDE (Price, Volume, MarketCap);

CREATE INDEX IF NOT EXISTS IX_OracleDataFeeds_UpdatedAt 
ON OracleDataFeeds(UpdatedAt DESC) 
INCLUDE (Symbol, Source, Price);

-- Cross-chain messages indexes
CREATE INDEX IF NOT EXISTS IX_CrossChainMessages_Status_CreatedAt 
ON CrossChainMessages(Status, CreatedAt DESC) 
INCLUDE (Id, SourceChain, TargetChain, MessageType);

CREATE INDEX IF NOT EXISTS IX_CrossChainMessages_SourceChain_TargetChain 
ON CrossChainMessages(SourceChain, TargetChain, Status) 
INCLUDE (Id, CreatedAt, ProcessedAt);

-- User activity indexes
CREATE INDEX IF NOT EXISTS IX_Users_Email_IsActive 
ON Users(Email, IsActive) 
INCLUDE (Id, Username, CreatedAt, LastLoginAt);

CREATE INDEX IF NOT EXISTS IX_UserSessions_UserId_ExpiresAt 
ON UserSessions(UserId, ExpiresAt) 
WHERE IsActive = 1;

-- Audit log indexes
CREATE INDEX IF NOT EXISTS IX_AuditLogs_EntityType_EntityId_CreatedAt 
ON AuditLogs(EntityType, EntityId, CreatedAt DESC) 
INCLUDE (Action, UserId, Details);

CREATE INDEX IF NOT EXISTS IX_AuditLogs_UserId_CreatedAt 
ON AuditLogs(UserId, CreatedAt DESC) 
INCLUDE (EntityType, EntityId, Action);

-- Key management indexes
CREATE INDEX IF NOT EXISTS IX_Keys_KeyId_IsActive 
ON Keys(KeyId, IsActive) 
INCLUDE (KeyType, CreatedAt, ExpiresAt);

CREATE INDEX IF NOT EXISTS IX_Keys_ExpiresAt 
ON Keys(ExpiresAt) 
WHERE IsActive = 1;

-- Event subscriptions indexes
CREATE INDEX IF NOT EXISTS IX_EventSubscriptions_EventType_IsActive 
ON EventSubscriptions(EventType, IsActive) 
INCLUDE (SubscriberId, CallbackUrl, CreatedAt);

-- Compliance records indexes
CREATE INDEX IF NOT EXISTS IX_ComplianceRecords_TransactionId_RuleType 
ON ComplianceRecords(TransactionId, RuleType) 
INCLUDE (Status, CheckedAt, Details);

-- Performance monitoring indexes
CREATE INDEX IF NOT EXISTS IX_PerformanceMetrics_ServiceName_Timestamp 
ON PerformanceMetrics(ServiceName, Timestamp DESC) 
INCLUDE (ResponseTime, CpuUsage, MemoryUsage);

-- Cache entries indexes (if using database cache)
CREATE INDEX IF NOT EXISTS IX_CacheEntries_Key_ExpiresAt 
ON CacheEntries(Key, ExpiresAt) 
WHERE ExpiresAt > CURRENT_TIMESTAMP;

-- Update statistics for query optimizer
ANALYZE Transactions;
ANALYZE OracleDataFeeds;
ANALYZE CrossChainMessages;
ANALYZE Users;
ANALYZE AuditLogs;

-- Create composite indexes for common join patterns
CREATE INDEX IF NOT EXISTS IX_Transactions_Users_Composite 
ON Transactions(UserId) 
INCLUDE (Status, Amount, CreatedAt);

CREATE INDEX IF NOT EXISTS IX_CrossChainMessages_Transactions_Composite 
ON CrossChainMessages(TransactionId) 
INCLUDE (Status, SourceChain, TargetChain);

-- Partial indexes for specific query patterns
CREATE INDEX IF NOT EXISTS IX_Transactions_Pending 
ON Transactions(CreatedAt DESC) 
WHERE Status = 'Pending';

CREATE INDEX IF NOT EXISTS IX_CrossChainMessages_Failed 
ON CrossChainMessages(CreatedAt DESC, RetryCount) 
WHERE Status = 'Failed' AND RetryCount < 3;

-- Function-based indexes for computed columns
CREATE INDEX IF NOT EXISTS IX_Transactions_DateOnly 
ON Transactions(DATE(CreatedAt), Status);

-- Text search indexes for audit logs
CREATE INDEX IF NOT EXISTS IX_AuditLogs_Details_FullText 
ON AuditLogs USING GIN(to_tsvector('english', Details));

-- Performance optimization settings
-- Increase work memory for complex queries
-- SET work_mem = '256MB';

-- Increase maintenance work memory for index creation
-- SET maintenance_work_mem = '512MB';

-- Enable parallel query execution
-- SET max_parallel_workers_per_gather = 4;

-- Vacuum and analyze all tables for optimal performance
-- VACUUM ANALYZE;