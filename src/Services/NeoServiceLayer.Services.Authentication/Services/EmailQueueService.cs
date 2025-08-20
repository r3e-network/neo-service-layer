using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Services.Authentication.Models;
using StackExchange.Redis;


namespace NeoServiceLayer.Services.Authentication.Services
{
    /// <summary>
    /// Email message representation
    /// </summary>
    public class EmailMessage
    {
        public string To { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string From { get; set; } = string.Empty;
        public bool IsHtml { get; set; } = true;
        public List<string> Cc { get; set; } = new();
        public List<string> Bcc { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
        public EmailPriority Priority { get; set; } = EmailPriority.Normal;
    }

    /// <summary>
    /// Email priority levels
    /// </summary>
    public enum EmailPriority
    {
        Low,
        Normal,
        High,
        Urgent,
        Critical
    }

    /// <summary>
    /// Email queue service for reliable email delivery
    /// </summary>
    public class EmailQueueService : BackgroundService, IEmailQueueService
    {
        private readonly ILogger<EmailQueueService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _serviceProvider;
        private readonly IConnectionMultiplexer _redis;
        private readonly IDatabase _db;
        private readonly string _queueKey = "email:queue";
        private readonly string _processingKey = "email:processing";
        private readonly string _deadLetterKey = "email:deadletter";
        private readonly int _batchSize;
        private readonly int _maxRetries;
        private readonly TimeSpan _retryDelay;
        private readonly TimeSpan _processingTimeout;

        public EmailQueueService(
            ILogger<EmailQueueService> logger,
            IConfiguration configuration,
            IServiceProvider serviceProvider,
            IConnectionMultiplexer redis)
        {
            _logger = logger;
            _configuration = configuration;
            _serviceProvider = serviceProvider;
            _redis = redis;
            _db = redis.GetDatabase();

            _batchSize = configuration.GetValue<int>("Email:Queue:BatchSize", 10);
            _maxRetries = configuration.GetValue<int>("Email:Queue:MaxRetries", 3);
            _retryDelay = TimeSpan.FromSeconds(configuration.GetValue<int>("Email:Queue:RetryDelaySeconds", 60));
            _processingTimeout = TimeSpan.FromMinutes(configuration.GetValue<int>("Email:Queue:ProcessingTimeoutMinutes", 5));
        }

        /// <summary>
        /// Add email to queue
        /// </summary>
        public async Task EnqueueEmailAsync(EmailMessage message)
        {
            try
            {
                var queueItem = new EmailQueueItem
                {
                    Id = Guid.NewGuid(),
                    Message = message,
                    EnqueuedAt = DateTime.UtcNow,
                    Priority = message.Priority,
                    RetryCount = 0,
                    Status = EmailStatus.Queued
                };

                var json = JsonSerializer.Serialize(queueItem);

                // Add to priority queue (higher priority = lower score)
                var score = GetPriorityScore(message.Priority);
                await _db.SortedSetAddAsync(_queueKey, json, score);

                _logger.LogInformation("Email queued with ID {EmailId} and priority {Priority}",
                    queueItem.Id, message.Priority);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enqueueing email");
                throw;
            }
        }

        /// <summary>
        /// Background service execution
        /// </summary>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Email queue service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessEmailBatchAsync(stoppingToken);
                    await RecoverStuckEmailsAsync();
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in email queue processing");
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                }
            }

            _logger.LogInformation("Email queue service stopped");
        }

        /// <summary>
        /// Process batch of emails from queue
        /// </summary>
        private async Task ProcessEmailBatchAsync(CancellationToken cancellationToken)
        {
            try
            {
                // Get batch of emails from queue
                var batch = await _db.SortedSetRangeByScoreAsync(
                    _queueKey,
                    take: _batchSize);

                if (batch.Length == 0)
                {
                    return;
                }

                var tasks = new List<Task>();

                foreach (var item in batch)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    tasks.Add(ProcessEmailItemAsync(item, cancellationToken));
                }

                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing email batch");
            }
        }

        /// <summary>
        /// Process individual email item
        /// </summary>
        private async Task ProcessEmailItemAsync(RedisValue itemJson, CancellationToken cancellationToken)
        {
            EmailQueueItem queueItem = null;

            try
            {
                queueItem = JsonSerializer.Deserialize<EmailQueueItem>(itemJson);

                // Move to processing queue
                var transaction = _db.CreateTransaction();
                transaction.SortedSetRemoveAsync(_queueKey, itemJson);
                transaction.HashSetAsync(_processingKey, queueItem.Id.ToString(), itemJson);
                transaction.KeyExpireAsync($"{_processingKey}:{queueItem.Id}", _processingTimeout);
                await transaction.ExecuteAsync();

                // Send email
                var success = await SendEmailWithRetryAsync(queueItem, cancellationToken);

                if (success)
                {
                    // Remove from processing
                    await _db.HashDeleteAsync(_processingKey, queueItem.Id.ToString());

                    _logger.LogInformation("Email {EmailId} sent successfully", queueItem.Id);
                }
                else
                {
                    // Handle failure
                    await HandleFailedEmailAsync(queueItem);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing email item {EmailId}", queueItem?.Id);

                if (queueItem != null)
                {
                    await HandleFailedEmailAsync(queueItem);
                }
            }
        }

        /// <summary>
        /// Send email with retry logic
        /// </summary>
        private async Task<bool> SendEmailWithRetryAsync(EmailQueueItem queueItem, CancellationToken cancellationToken)
        {
            // Email service should be injected through constructor or passed as parameter
            // var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            for (int attempt = 1; attempt <= _maxRetries; attempt++)
            {
                if (cancellationToken.IsCancellationRequested)
                    return false;

                try
                {
                    // Try to send email
                    var success = await SendEmailDirectlyAsync(queueItem.Message);

                    if (success)
                    {
                        return true;
                    }

                    // Wait before retry
                    if (attempt < _maxRetries)
                    {
                        var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt) * _retryDelay.TotalSeconds);
                        await Task.Delay(delay, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Attempt {Attempt} failed for email {EmailId}", attempt, queueItem.Id);

                    if (attempt < _maxRetries)
                    {
                        await Task.Delay(_retryDelay, cancellationToken);
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Send email directly (bypassing queue)
        /// </summary>
        private async Task<bool> SendEmailDirectlyAsync(EmailMessage message)
        {
            // This would normally call the actual email provider
            // For now, simulate sending
            await Task.Delay(100);

            // Simulate 95% success rate
            return Random.Shared.Next(100) < 95;
        }

        /// <summary>
        /// Handle failed email
        /// </summary>
        private async Task HandleFailedEmailAsync(EmailQueueItem queueItem)
        {
            try
            {
                queueItem.RetryCount++;
                queueItem.LastAttemptAt = DateTime.UtcNow;
                queueItem.Status = EmailStatus.Failed;

                if (queueItem.RetryCount >= _maxRetries)
                {
                    // Move to dead letter queue
                    queueItem.Status = EmailStatus.DeadLetter;
                    var json = JsonSerializer.Serialize(queueItem);
                    await _db.ListRightPushAsync(_deadLetterKey, json);
                    await _db.HashDeleteAsync(_processingKey, queueItem.Id.ToString());

                    _logger.LogError("Email {EmailId} moved to dead letter queue after {Retries} retries",
                        queueItem.Id, queueItem.RetryCount);
                }
                else
                {
                    // Re-queue with delay
                    queueItem.Status = EmailStatus.Queued;
                    var json = JsonSerializer.Serialize(queueItem);
                    var score = GetPriorityScore(queueItem.Message.Priority) + queueItem.RetryCount * 1000;
                    await _db.SortedSetAddAsync(_queueKey, json, score);
                    await _db.HashDeleteAsync(_processingKey, queueItem.Id.ToString());

                    _logger.LogWarning("Email {EmailId} re-queued for retry {RetryCount}/{MaxRetries}",
                        queueItem.Id, queueItem.RetryCount, _maxRetries);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling failed email {EmailId}", queueItem.Id);
            }
        }

        /// <summary>
        /// Recover emails stuck in processing
        /// </summary>
        private async Task RecoverStuckEmailsAsync()
        {
            try
            {
                var processingItems = await _db.HashGetAllAsync(_processingKey);

                foreach (var item in processingItems)
                {
                    var queueItem = JsonSerializer.Deserialize<EmailQueueItem>(item.Value);

                    // Check if processing timeout exceeded
                    if (queueItem.ProcessingStartedAt.HasValue)
                    {
                        var processingTime = DateTime.UtcNow - queueItem.ProcessingStartedAt.Value;
                        if (processingTime > _processingTimeout)
                        {
                            _logger.LogWarning("Recovering stuck email {EmailId}", queueItem.Id);

                            // Re-queue the email
                            queueItem.Status = EmailStatus.Queued;
                            queueItem.ProcessingStartedAt = null;
                            var json = JsonSerializer.Serialize(queueItem);
                            var score = GetPriorityScore(queueItem.Message.Priority);
                            await _db.SortedSetAddAsync(_queueKey, json, score);
                            await _db.HashDeleteAsync(_processingKey, queueItem.Id.ToString());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recovering stuck emails");
            }
        }

        /// <summary>
        /// Get queue statistics
        /// </summary>
        public async Task<EmailQueueStatistics> GetStatisticsAsync()
        {
            try
            {
                var stats = new EmailQueueStatistics
                {
                    QueuedCount = (int)await _db.SortedSetLengthAsync(_queueKey),
                    ProcessingCount = (int)await _db.HashLengthAsync(_processingKey),
                    DeadLetterCount = (int)await _db.ListLengthAsync(_deadLetterKey),
                    Timestamp = DateTime.UtcNow
                };

                // Get priority breakdown
                var queuedItems = await _db.SortedSetRangeByScoreAsync(_queueKey);
                foreach (var item in queuedItems)
                {
                    var queueItem = JsonSerializer.Deserialize<EmailQueueItem>(item);
                    switch (queueItem.Message.Priority)
                    {
                        case EmailPriority.Urgent:
                            stats.UrgentCount++;
                            break;
                        case EmailPriority.High:
                            stats.HighPriorityCount++;
                            break;
                        case EmailPriority.Normal:
                            stats.NormalPriorityCount++;
                            break;
                        case EmailPriority.Low:
                            stats.LowPriorityCount++;
                            break;
                    }
                }

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting queue statistics");
                return new EmailQueueStatistics();
            }
        }

        /// <summary>
        /// Retry dead letter emails
        /// </summary>
        public async Task RetryDeadLetterEmailsAsync()
        {
            try
            {
                var deadLetterItems = await _db.ListRangeAsync(_deadLetterKey);

                foreach (var item in deadLetterItems)
                {
                    var queueItem = JsonSerializer.Deserialize<EmailQueueItem>(item);
                    queueItem.RetryCount = 0;
                    queueItem.Status = EmailStatus.Queued;

                    await EnqueueEmailAsync(queueItem.Message);
                    await _db.ListRemoveAsync(_deadLetterKey, item);

                    _logger.LogInformation("Dead letter email {EmailId} re-queued for retry", queueItem.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrying dead letter emails");
            }
        }

        private double GetPriorityScore(EmailPriority priority)
        {
            return priority switch
            {
                EmailPriority.Urgent => 0,
                EmailPriority.High => 1000,
                EmailPriority.Normal => 2000,
                EmailPriority.Low => 3000,
                _ => 2000
            };
        }
    }

    /// <summary>
    /// Email queue service interface
    /// </summary>
    public interface IEmailQueueService
    {
        Task EnqueueEmailAsync(EmailMessage message);
        Task<EmailQueueStatistics> GetStatisticsAsync();
        Task RetryDeadLetterEmailsAsync();
    }

    /// <summary>
    /// Email queue item
    /// </summary>
    public class EmailQueueItem
    {
        public Guid Id { get; set; }
        public EmailMessage Message { get; set; }
        public DateTime EnqueuedAt { get; set; }
        public DateTime? ProcessingStartedAt { get; set; }
        public DateTime? LastAttemptAt { get; set; }
        public EmailPriority Priority { get; set; }
        public int RetryCount { get; set; }
        public EmailStatus Status { get; set; }
        public string LastError { get; set; }
    }

    /// <summary>
    /// Email status
    /// </summary>
    public enum EmailStatus
    {
        Queued,
        Processing,
        Sent,
        Failed,
        DeadLetter
    }

    /// <summary>
    /// Email queue statistics
    /// </summary>
    public class EmailQueueStatistics
    {
        public int QueuedCount { get; set; }
        public int ProcessingCount { get; set; }
        public int DeadLetterCount { get; set; }
        public int UrgentCount { get; set; }
        public int HighPriorityCount { get; set; }
        public int NormalPriorityCount { get; set; }
        public int LowPriorityCount { get; set; }
        public DateTime Timestamp { get; set; }
    }
}