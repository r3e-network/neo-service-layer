using Microsoft.EntityFrameworkCore;
using Neo.SecretsManagement.Service.Data;
using Neo.SecretsManagement.Service.Models;
using System.Text.RegularExpressions;

namespace Neo.SecretsManagement.Service.Services;

public class SecretPolicyService : ISecretPolicyService
{
    private readonly SecretsDbContext _context;
    private readonly ILogger<SecretPolicyService> _logger;
    private readonly IAuditService _auditService;

    public SecretPolicyService(
        SecretsDbContext context,
        ILogger<SecretPolicyService> logger,
        IAuditService auditService)
    {
        _context = context;
        _logger = logger;
        _auditService = auditService;
    }

    public async Task<SecretPolicy> CreatePolicyAsync(SecretPolicy policy, string userId)
    {
        try
        {
            // Validate policy rules
            ValidatePolicyRules(policy);

            policy.Id = Guid.NewGuid();
            policy.CreatedAt = DateTime.UtcNow;
            policy.UpdatedAt = DateTime.UtcNow;

            _context.SecretPolicies.Add(policy);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                userId,
                "create",
                "policy",
                policy.Id.ToString(),
                null,
                true,
                null,
                new Dictionary<string, object>
                {
                    ["policy_name"] = policy.Name,
                    ["priority"] = policy.Priority,
                    ["enabled"] = policy.IsEnabled,
                    ["path_patterns"] = policy.PathPatterns,
                    ["operations"] = policy.AllowedOperations?.Select(o => o.ToString()).ToArray() ?? Array.Empty<string>()
                }
            );

            _logger.LogInformation("Policy {PolicyId} '{PolicyName}' created by user {UserId}",
                policy.Id, policy.Name, userId);

            return policy;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create policy '{PolicyName}'", policy.Name);
            await _auditService.LogAsync(
                userId,
                "create",
                "policy",
                policy.Id.ToString(),
                null,
                false,
                ex.Message
            );
            throw;
        }
    }

    public async Task<SecretPolicy?> GetPolicyAsync(Guid policyId)
    {
        try
        {
            return await _context.SecretPolicies
                .FirstOrDefaultAsync(p => p.Id == policyId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get policy {PolicyId}", policyId);
            return null;
        }
    }

    public async Task<List<SecretPolicy>> ListPoliciesAsync(string userId)
    {
        try
        {
            var policies = await _context.SecretPolicies
                .Where(p => p.IsEnabled)
                .OrderBy(p => p.Priority)
                .ThenBy(p => p.Name)
                .ToListAsync();

            await _auditService.LogAsync(
                userId,
                "list",
                "policies",
                "all",
                null,
                true,
                null,
                new Dictionary<string, object>
                {
                    ["policy_count"] = policies.Count
                }
            );

            return policies;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list policies for user {UserId}", userId);
            await _auditService.LogAsync(
                userId,
                "list",
                "policies",
                "all",
                null,
                false,
                ex.Message
            );
            return new List<SecretPolicy>();
        }
    }

    public async Task<bool> UpdatePolicyAsync(Guid policyId, SecretPolicy policy, string userId)
    {
        try
        {
            var existingPolicy = await _context.SecretPolicies
                .FirstOrDefaultAsync(p => p.Id == policyId);

            if (existingPolicy == null)
            {
                _logger.LogWarning("Policy {PolicyId} not found for update", policyId);
                return false;
            }

            // Validate policy rules
            ValidatePolicyRules(policy);

            // Store old values for audit
            var oldValues = new Dictionary<string, object>
            {
                ["old_name"] = existingPolicy.Name,
                ["old_priority"] = existingPolicy.Priority,
                ["old_enabled"] = existingPolicy.IsEnabled
            };

            // Update properties
            existingPolicy.Name = policy.Name;
            existingPolicy.Description = policy.Description;
            existingPolicy.PathPatterns = policy.PathPatterns;
            existingPolicy.AllowedOperations = policy.AllowedOperations;
            existingPolicy.DeniedOperations = policy.DeniedOperations;
            existingPolicy.RequiredRoles = policy.RequiredRoles;
            existingPolicy.TimeRestrictions = policy.TimeRestrictions;
            existingPolicy.IpWhitelist = policy.IpWhitelist;
            existingPolicy.MaxAccessCount = policy.MaxAccessCount;
            existingPolicy.Priority = policy.Priority;
            existingPolicy.IsEnabled = policy.IsEnabled;
            existingPolicy.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var newValues = new Dictionary<string, object>
            {
                ["new_name"] = policy.Name,
                ["new_priority"] = policy.Priority,
                ["new_enabled"] = policy.IsEnabled,
                ["path_patterns"] = policy.PathPatterns,
                ["operations"] = policy.AllowedOperations?.Select(o => o.ToString()).ToArray() ?? Array.Empty<string>()
            };

            await _auditService.LogAsync(
                userId,
                "update",
                "policy",
                policyId.ToString(),
                null,
                true,
                null,
                oldValues.Concat(newValues).ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
            );

            _logger.LogInformation("Policy {PolicyId} updated by user {UserId}", policyId, userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update policy {PolicyId}", policyId);
            await _auditService.LogAsync(
                userId,
                "update",
                "policy",
                policyId.ToString(),
                null,
                false,
                ex.Message
            );
            return false;
        }
    }

    public async Task<bool> DeletePolicyAsync(Guid policyId, string userId)
    {
        try
        {
            var policy = await _context.SecretPolicies
                .FirstOrDefaultAsync(p => p.Id == policyId);

            if (policy == null)
            {
                _logger.LogWarning("Policy {PolicyId} not found for deletion", policyId);
                return false;
            }

            _context.SecretPolicies.Remove(policy);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                userId,
                "delete",
                "policy",
                policyId.ToString(),
                null,
                true,
                null,
                new Dictionary<string, object>
                {
                    ["policy_name"] = policy.Name,
                    ["deleted_at"] = DateTime.UtcNow.ToString("O")
                }
            );

            _logger.LogInformation("Policy {PolicyId} '{PolicyName}' deleted by user {UserId}",
                policyId, policy.Name, userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete policy {PolicyId}", policyId);
            await _auditService.LogAsync(
                userId,
                "delete",
                "policy",
                policyId.ToString(),
                null,
                false,
                ex.Message
            );
            return false;
        }
    }

    public async Task<bool> EvaluatePolicyAsync(string path, string userId, SecretOperation operation, Dictionary<string, object> context)
    {
        try
        {
            var applicablePolicies = await GetApplicablePoliciesAsync(path);
            
            if (!applicablePolicies.Any())
            {
                // No policies means default allow for now (configurable)
                _logger.LogDebug("No applicable policies found for path {Path}, defaulting to allow", path);
                return true;
            }

            // Sort by priority (lower number = higher priority)
            var sortedPolicies = applicablePolicies
                .Where(p => p.IsEnabled)
                .OrderBy(p => p.Priority)
                .ToList();

            var userRoles = GetUserRoles(userId, context);
            var clientIp = GetClientIp(context);
            var currentTime = DateTime.UtcNow;

            foreach (var policy in sortedPolicies)
            {
                var evaluation = EvaluateSinglePolicy(policy, operation, userRoles, clientIp, currentTime, context);
                
                await _auditService.LogAsync(
                    userId,
                    "evaluate_policy",
                    "policy",
                    policy.Id.ToString(),
                    path,
                    evaluation.Allowed,
                    evaluation.Reason,
                    new Dictionary<string, object>
                    {
                        ["policy_name"] = policy.Name,
                        ["operation"] = operation.ToString(),
                        ["evaluation_result"] = evaluation.Allowed,
                        ["evaluation_reason"] = evaluation.Reason ?? "No reason provided"
                    }
                );

                // If policy explicitly denies, return false immediately
                if (!evaluation.Allowed && evaluation.Explicit)
                {
                    _logger.LogWarning("Policy {PolicyName} explicitly denied operation {Operation} for user {UserId} on path {Path}: {Reason}",
                        policy.Name, operation, userId, path, evaluation.Reason);
                    return false;
                }

                // If policy explicitly allows, return true immediately
                if (evaluation.Allowed && evaluation.Explicit)
                {
                    _logger.LogDebug("Policy {PolicyName} explicitly allowed operation {Operation} for user {UserId} on path {Path}",
                        policy.Name, operation, userId, path);
                    return true;
                }
            }

            // Default to deny if no explicit allow
            _logger.LogWarning("No policy explicitly allowed operation {Operation} for user {UserId} on path {Path}, defaulting to deny",
                operation, userId, path);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating policies for path {Path}, operation {Operation}, user {UserId}",
                path, operation, userId);
            
            await _auditService.LogAsync(
                userId,
                "evaluate_policy",
                "policy",
                "error",
                path,
                false,
                ex.Message,
                new Dictionary<string, object>
                {
                    ["operation"] = operation.ToString(),
                    ["error"] = ex.Message
                }
            );

            // Default to deny on error
            return false;
        }
    }

    public async Task<List<SecretPolicy>> GetApplicablePoliciesAsync(string path)
    {
        try
        {
            var allPolicies = await _context.SecretPolicies
                .Where(p => p.IsEnabled)
                .ToListAsync();

            var applicablePolicies = new List<SecretPolicy>();

            foreach (var policy in allPolicies)
            {
                if (PathMatchesPatterns(path, policy.PathPatterns))
                {
                    applicablePolicies.Add(policy);
                }
            }

            return applicablePolicies.OrderBy(p => p.Priority).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get applicable policies for path {Path}", path);
            return new List<SecretPolicy>();
        }
    }

    private void ValidatePolicyRules(SecretPolicy policy)
    {
        if (string.IsNullOrWhiteSpace(policy.Name))
        {
            throw new ArgumentException("Policy name cannot be empty");
        }

        if (policy.PathPatterns == null || policy.PathPatterns.Count == 0)
        {
            throw new ArgumentException("Policy must have at least one path pattern");
        }

        // Validate regex patterns
        foreach (var pattern in policy.PathPatterns)
        {
            try
            {
                _ = new Regex(pattern);
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Invalid regex pattern '{pattern}': {ex.Message}");
            }
        }

        if (policy.Priority < 0)
        {
            throw new ArgumentException("Policy priority must be non-negative");
        }
    }

    private bool PathMatchesPatterns(string path, List<string> patterns)
    {
        foreach (var pattern in patterns)
        {
            try
            {
                if (Regex.IsMatch(path, pattern, RegexOptions.IgnoreCase))
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Invalid regex pattern '{Pattern}': {Error}", pattern, ex.Message);
                continue;
            }
        }
        return false;
    }

    private List<string> GetUserRoles(string userId, Dictionary<string, object> context)
    {
        // Extract roles from context (e.g., JWT claims, user service)
        if (context.TryGetValue("roles", out var rolesValue) && rolesValue is string[] roles)
        {
            return roles.ToList();
        }

        if (context.TryGetValue("user_roles", out var userRolesValue) && userRolesValue is List<string> userRoles)
        {
            return userRoles;
        }

        // Default roles based on user pattern (simplified)
        var defaultRoles = new List<string> { "user" };
        if (userId.StartsWith("admin-"))
        {
            defaultRoles.Add("admin");
        }
        if (userId.StartsWith("service-"))
        {
            defaultRoles.Add("service");
        }

        return defaultRoles;
    }

    private string? GetClientIp(Dictionary<string, object> context)
    {
        if (context.TryGetValue("client_ip", out var ipValue) && ipValue is string ip)
        {
            return ip;
        }
        return null;
    }

    private (bool Allowed, bool Explicit, string? Reason) EvaluateSinglePolicy(
        SecretPolicy policy, 
        SecretOperation operation, 
        List<string> userRoles, 
        string? clientIp, 
        DateTime currentTime,
        Dictionary<string, object> context)
    {
        // Check denied operations first (explicit deny)
        if (policy.DeniedOperations?.Contains(operation) == true)
        {
            return (false, true, $"Operation {operation} is explicitly denied by policy");
        }

        // Check allowed operations
        if (policy.AllowedOperations?.Contains(operation) != true)
        {
            return (false, false, $"Operation {operation} is not in allowed operations list");
        }

        // Check required roles
        if (policy.RequiredRoles?.Count > 0)
        {
            var hasRequiredRole = policy.RequiredRoles.Any(role => userRoles.Contains(role, StringComparer.OrdinalIgnoreCase));
            if (!hasRequiredRole)
            {
                return (false, true, "User does not have required roles");
            }
        }

        // Check IP whitelist
        if (policy.IpWhitelist?.Count > 0 && !string.IsNullOrEmpty(clientIp))
        {
            var ipAllowed = policy.IpWhitelist.Any(allowedIp => 
                clientIp.StartsWith(allowedIp) || clientIp.Equals(allowedIp, StringComparison.OrdinalIgnoreCase));
            
            if (!ipAllowed)
            {
                return (false, true, $"Client IP {clientIp} is not in whitelist");
            }
        }

        // Check time restrictions
        if (policy.TimeRestrictions?.Count > 0)
        {
            var currentHour = currentTime.Hour;
            var isWithinAllowedTime = policy.TimeRestrictions.Any(timeRange =>
            {
                var parts = timeRange.Split('-');
                if (parts.Length == 2 && 
                    int.TryParse(parts[0], out var startHour) && 
                    int.TryParse(parts[1], out var endHour))
                {
                    if (startHour <= endHour)
                    {
                        return currentHour >= startHour && currentHour <= endHour;
                    }
                    else
                    {
                        // Handle overnight ranges like "22-6"
                        return currentHour >= startHour || currentHour <= endHour;
                    }
                }
                return false;
            });

            if (!isWithinAllowedTime)
            {
                return (false, true, $"Current time {currentTime:HH:mm} is outside allowed time restrictions");
            }
        }

        // Check max access count
        if (policy.MaxAccessCount.HasValue)
        {
            // This would require tracking access counts in the database
            // For now, we'll just check if it's configured
            var accessCount = GetAccessCount(policy.Id, context);
            if (accessCount >= policy.MaxAccessCount.Value)
            {
                return (false, true, $"Maximum access count ({policy.MaxAccessCount}) exceeded");
            }
        }

        return (true, true, "Policy evaluation passed all checks");
    }

    private int GetAccessCount(Guid policyId, Dictionary<string, object> context)
    {
        // This would typically query the audit log or a separate access tracking table
        // For now, return 0 as a placeholder
        // TODO: Implement actual access count tracking
        return 0;
    }
}