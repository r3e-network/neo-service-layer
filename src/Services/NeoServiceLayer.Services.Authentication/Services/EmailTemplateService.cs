using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Services.Authentication.Models;
using NeoServiceLayer.Services.Authentication.Repositories;
using Scriban;
using Scriban.Runtime;
using System.Threading;


namespace NeoServiceLayer.Services.Authentication.Services
{
    /// <summary>
    /// Email template rendering service with caching
    /// </summary>
    public class EmailTemplateService : IEmailTemplateService
    {
        private readonly ILogger<EmailTemplateService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IEmailTemplateRepository _repository;
        private readonly IMemoryCache _cache;
        private readonly string _templatesPath;
        private readonly bool _useDatabase;
        private readonly TimeSpan _cacheExpiration = TimeSpan.FromHours(1);

        public EmailTemplateService(
            ILogger<EmailTemplateService> logger,
            IConfiguration configuration,
            IEmailTemplateRepository repository,
            IMemoryCache cache)
        {
            _logger = logger;
            _configuration = configuration;
            _repository = repository;
            _cache = cache;

            _templatesPath = configuration["Email:TemplatesPath"] ?? "EmailTemplates";
            _useDatabase = configuration.GetValue<bool>("Email:UseDatabase", true);
        }

        /// <summary>
        /// Render email template with data
        /// </summary>
        public async Task<EmailContent> RenderTemplateAsync(
            string templateName,
            Dictionary<string, string> data)
        {
            try
            {
                var template = await GetTemplateAsync(templateName);
                if (template == null)
                {
                    throw new InvalidOperationException($"Email template '{templateName}' not found");
                }

                // Render subject
                var subject = RenderString(template.Subject, data);

                // Render HTML body
                var htmlBody = RenderString(template.HtmlBody, data);

                // Render text body (or generate from HTML if not provided)
                var textBody = !string.IsNullOrEmpty(template.TextBody)
                    ? RenderString(template.TextBody, data)
                    : ConvertHtmlToText(htmlBody);

                return new EmailContent
                {
                    Subject = subject,
                    HtmlBody = htmlBody,
                    TextBody = textBody
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rendering template {TemplateName}", templateName);
                throw;
            }
        }

        /// <summary>
        /// Get template from cache, database, or file system
        /// </summary>
        private async Task<EmailTemplate> GetTemplateAsync(string templateName)
        {
            // Check cache first
            var cacheKey = $"email_template_{templateName}";
            if (_cache.TryGetValue<EmailTemplate>(cacheKey, out var cachedTemplate))
            {
                return cachedTemplate;
            }

            EmailTemplate template = null;

            // Try to load from database
            if (_useDatabase && _repository != null)
            {
                template = await _repository.GetByNameAsync(templateName);
            }

            // Fall back to file system
            if (template == null)
            {
                template = await LoadTemplateFromFileAsync(templateName);
            }

            // Cache the template
            if (template != null)
            {
                _cache.Set(cacheKey, template, _cacheExpiration);
            }

            return template;
        }

        /// <summary>
        /// Load template from file system
        /// </summary>
        private async Task<EmailTemplate> LoadTemplateFromFileAsync(string templateName)
        {
            try
            {
                var basePath = Path.Combine(Directory.GetCurrentDirectory(), _templatesPath, templateName);

                var subjectFile = $"{basePath}.subject.txt";
                var htmlFile = $"{basePath}.html";
                var textFile = $"{basePath}.txt";

                if (!File.Exists(htmlFile))
                {
                    // Try to load from embedded templates
                    return GetEmbeddedTemplate(templateName);
                }

                var subject = File.Exists(subjectFile)
                    ? await File.ReadAllTextAsync(subjectFile)
                    : templateName.Replace("_", " ");

                var htmlBody = await File.ReadAllTextAsync(htmlFile);

                var textBody = File.Exists(textFile)
                    ? await File.ReadAllTextAsync(textFile)
                    : null;

                return new EmailTemplate
                {
                    Name = templateName,
                    Subject = subject,
                    HtmlBody = htmlBody,
                    TextBody = textBody,
                    IsActive = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading template {TemplateName} from file", templateName);
                return GetEmbeddedTemplate(templateName);
            }
        }

        /// <summary>
        /// Get embedded default template
        /// </summary>
        private EmailTemplate GetEmbeddedTemplate(string templateName)
        {
            return templateName switch
            {
                "EmailVerification" => new EmailTemplate
                {
                    Name = "EmailVerification",
                    Subject = "Verify your email address",
                    HtmlBody = GetEmailVerificationHtml(),
                    TextBody = GetEmailVerificationText(),
                    IsActive = true
                },
                "PasswordReset" => new EmailTemplate
                {
                    Name = "PasswordReset",
                    Subject = "Password Reset Request",
                    HtmlBody = GetPasswordResetHtml(),
                    TextBody = GetPasswordResetText(),
                    IsActive = true
                },
                "TwoFactorCode" => new EmailTemplate
                {
                    Name = "TwoFactorCode",
                    Subject = "Your Two-Factor Authentication Code",
                    HtmlBody = GetTwoFactorCodeHtml(),
                    TextBody = GetTwoFactorCodeText(),
                    IsActive = true
                },
                "AccountLocked" => new EmailTemplate
                {
                    Name = "AccountLocked",
                    Subject = "Your account has been locked",
                    HtmlBody = GetAccountLockedHtml(),
                    TextBody = GetAccountLockedText(),
                    IsActive = true
                },
                "SuspiciousActivity" => new EmailTemplate
                {
                    Name = "SuspiciousActivity",
                    Subject = "Suspicious activity detected on your account",
                    HtmlBody = GetSuspiciousActivityHtml(),
                    TextBody = GetSuspiciousActivityText(),
                    IsActive = true
                },
                "Welcome" => new EmailTemplate
                {
                    Name = "Welcome",
                    Subject = "Welcome to Neo Service Layer!",
                    HtmlBody = GetWelcomeHtml(),
                    TextBody = GetWelcomeText(),
                    IsActive = true
                },
                _ => null
            };
        }

        /// <summary>
        /// Render template string with data using Scriban
        /// </summary>
        private string RenderString(string templateString, Dictionary<string, string> data)
        {
            try
            {
                // Use Scriban for advanced templating
                var template = Template.Parse(templateString);

                if (template.HasErrors)
                {
                    var errors = string.Join(", ", template.Messages.Select(m => m.Message));
                    _logger.LogError("Template parsing errors: {Errors}", errors);

                    // Fall back to simple replacement
                    return SimpleTemplateReplace(templateString, data);
                }

                var scriptObject = new ScriptObject();
                foreach (var kvp in data)
                {
                    scriptObject[kvp.Key] = kvp.Value;
                }

                var context = new TemplateContext();
                context.PushGlobal(scriptObject);

                return template.Render(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rendering template with Scriban, falling back to simple replacement");
                return SimpleTemplateReplace(templateString, data);
            }
        }

        /// <summary>
        /// Simple template replacement fallback
        /// </summary>
        private string SimpleTemplateReplace(string template, Dictionary<string, string> data)
        {
            var result = template;

            foreach (var kvp in data)
            {
                result = result.Replace($"{{{{{kvp.Key}}}}}", kvp.Value);
                result = result.Replace($"{{{kvp.Key}}}", kvp.Value);
            }

            return result;
        }

        /// <summary>
        /// Convert HTML to plain text
        /// </summary>
        private string ConvertHtmlToText(string html)
        {
            if (string.IsNullOrEmpty(html))
                return string.Empty;

            // Remove HTML tags
            var text = Regex.Replace(html, "<[^>]*>", "");

            // Decode HTML entities
            text = System.Net.WebUtility.HtmlDecode(text);

            // Replace multiple spaces with single space
            text = Regex.Replace(text, @"\s+", " ");

            // Replace multiple newlines with double newline
            text = Regex.Replace(text, @"\n{3,}", "\n\n");

            return text.Trim();
        }

        #region Embedded Templates

        private string GetEmailVerificationHtml() => @"
<!DOCTYPE html>
<html>
<head>
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { background: #007bff; color: white; padding: 20px; text-align: center; }
        .content { padding: 20px; background: #f4f4f4; }
        .button { display: inline-block; padding: 10px 20px; background: #007bff; color: white; text-decoration: none; border-radius: 5px; }
        .footer { text-align: center; padding: 20px; color: #666; font-size: 12px; }
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Email Verification</h1>
        </div>
        <div class='content'>
            <p>Hello {{Username}},</p>
            <p>Thank you for registering! Please verify your email address by clicking the button below:</p>
            <p style='text-align: center;'>
                <a href='{{VerificationLink}}' class='button'>Verify Email</a>
            </p>
            <p>Or copy and paste this link into your browser:</p>
            <p style='word-break: break-all;'>{{VerificationLink}}</p>
            <p>This link will expire in {{ExpirationHours}} hours.</p>
            <p>If you didn't create an account, you can safely ignore this email.</p>
        </div>
        <div class='footer'>
            <p>&copy; 2024 Neo Service Layer. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";

        private string GetEmailVerificationText() => @"
Email Verification

Hello {{Username}},

Thank you for registering! Please verify your email address by clicking the link below:

{{VerificationLink}}

This link will expire in {{ExpirationHours}} hours.

If you didn't create an account, you can safely ignore this email.

Best regards,
The Neo Service Layer Team";

        private string GetPasswordResetHtml() => @"
<!DOCTYPE html>
<html>
<head>
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { background: #dc3545; color: white; padding: 20px; text-align: center; }
        .content { padding: 20px; background: #f4f4f4; }
        .button { display: inline-block; padding: 10px 20px; background: #dc3545; color: white; text-decoration: none; border-radius: 5px; }
        .footer { text-align: center; padding: 20px; color: #666; font-size: 12px; }
        .warning { background: #fff3cd; border: 1px solid #ffc107; padding: 10px; margin: 10px 0; }
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Password Reset Request</h1>
        </div>
        <div class='content'>
            <p>Hello {{Username}},</p>
            <p>We received a request to reset your password. Click the button below to create a new password:</p>
            <p style='text-align: center;'>
                <a href='{{ResetLink}}' class='button'>Reset Password</a>
            </p>
            <p>Or copy and paste this link into your browser:</p>
            <p style='word-break: break-all;'>{{ResetLink}}</p>
            <p>This link will expire in {{ExpirationHours}} hour(s).</p>
            <div class='warning'>
                <strong>Security Notice:</strong> If you didn't request this password reset, please ignore this email and consider changing your password as a precaution.
            </div>
            <p>Need help? Contact us at {{SupportEmail}}</p>
        </div>
        <div class='footer'>
            <p>&copy; 2024 Neo Service Layer. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";

        private string GetPasswordResetText() => @"
Password Reset Request

Hello {{Username}},

We received a request to reset your password. Click the link below to create a new password:

{{ResetLink}}

This link will expire in {{ExpirationHours}} hour(s).

Security Notice: If you didn't request this password reset, please ignore this email and consider changing your password as a precaution.

Need help? Contact us at {{SupportEmail}}

Best regards,
The Neo Service Layer Team";

        private string GetTwoFactorCodeHtml() => @"
<!DOCTYPE html>
<html>
<head>
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { background: #28a745; color: white; padding: 20px; text-align: center; }
        .content { padding: 20px; background: #f4f4f4; }
        .code-box { background: white; border: 2px solid #28a745; padding: 20px; text-align: center; font-size: 32px; font-weight: bold; letter-spacing: 5px; margin: 20px 0; }
        .footer { text-align: center; padding: 20px; color: #666; font-size: 12px; }
        .warning { color: #dc3545; font-weight: bold; }
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Two-Factor Authentication</h1>
        </div>
        <div class='content'>
            <p>Hello {{Username}},</p>
            <p>Your authentication code is:</p>
            <div class='code-box'>{{Code}}</div>
            <p>Enter this code to complete your login. The code will expire in {{ExpirationMinutes}} minutes.</p>
            <p class='warning'>Never share this code with anyone. Our team will never ask for this code.</p>
        </div>
        <div class='footer'>
            <p>&copy; 2024 Neo Service Layer. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";

        private string GetTwoFactorCodeText() => @"
Two-Factor Authentication

Hello {{Username}},

Your authentication code is: {{Code}}

Enter this code to complete your login. The code will expire in {{ExpirationMinutes}} minutes.

WARNING: Never share this code with anyone. Our team will never ask for this code.

Best regards,
The Neo Service Layer Team";

        private string GetAccountLockedHtml() => @"
<!DOCTYPE html>
<html>
<head>
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { background: #ffc107; color: #333; padding: 20px; text-align: center; }
        .content { padding: 20px; background: #f4f4f4; }
        .alert { background: #fff3cd; border: 1px solid #ffc107; padding: 15px; margin: 20px 0; }
        .footer { text-align: center; padding: 20px; color: #666; font-size: 12px; }
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Account Security Alert</h1>
        </div>
        <div class='content'>
            <p>Hello {{Username}},</p>
            <div class='alert'>
                <strong>Your account has been temporarily locked</strong>
                <p>Reason: {{Reason}}</p>
                <p>Your account will be unlocked at: {{LockoutEnd}}</p>
            </div>
            <p>This is a security measure to protect your account. If you believe this is an error or need immediate assistance, please contact our support team at {{SupportEmail}}.</p>
            <h3>What you can do:</h3>
            <ul>
                <li>Wait until the lockout period expires</li>
                <li>Contact support if you need immediate access</li>
                <li>Review your account security settings once unlocked</li>
            </ul>
        </div>
        <div class='footer'>
            <p>&copy; 2024 Neo Service Layer. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";

        private string GetAccountLockedText() => @"
Account Security Alert

Hello {{Username}},

Your account has been temporarily locked.

Reason: {{Reason}}
Your account will be unlocked at: {{LockoutEnd}}

This is a security measure to protect your account. If you believe this is an error or need immediate assistance, please contact our support team at {{SupportEmail}}.

What you can do:
- Wait until the lockout period expires
- Contact support if you need immediate access
- Review your account security settings once unlocked

Best regards,
The Neo Service Layer Team";

        private string GetSuspiciousActivityHtml() => @"
<!DOCTYPE html>
<html>
<head>
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { background: #dc3545; color: white; padding: 20px; text-align: center; }
        .content { padding: 20px; background: #f4f4f4; }
        .alert { background: #f8d7da; border: 1px solid #dc3545; padding: 15px; margin: 20px 0; }
        .button { display: inline-block; padding: 10px 20px; background: #dc3545; color: white; text-decoration: none; border-radius: 5px; }
        .details { background: white; padding: 15px; margin: 15px 0; border-left: 4px solid #dc3545; }
        .footer { text-align: center; padding: 20px; color: #666; font-size: 12px; }
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Security Alert</h1>
        </div>
        <div class='content'>
            <p>Hello {{Username}},</p>
            <div class='alert'>
                <strong>Suspicious activity detected on your account</strong>
            </div>
            <div class='details'>
                <p><strong>Activity:</strong> {{Activity}}</p>
                <p><strong>IP Address:</strong> {{IpAddress}}</p>
                <p><strong>Time:</strong> {{Timestamp}}</p>
            </div>
            <p>If this was you, you can ignore this message. Otherwise, we recommend you:</p>
            <ol>
                <li>Change your password immediately</li>
                <li>Review your recent account activity</li>
                <li>Enable two-factor authentication if not already active</li>
            </ol>
            <p style='text-align: center;'>
                <a href='{{SecurityLink}}' class='button'>Review Account Security</a>
            </p>
        </div>
        <div class='footer'>
            <p>&copy; 2024 Neo Service Layer. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";

        private string GetSuspiciousActivityText() => @"
Security Alert

Hello {{Username}},

Suspicious activity detected on your account:

Activity: {{Activity}}
IP Address: {{IpAddress}}
Time: {{Timestamp}}

If this was you, you can ignore this message. Otherwise, we recommend you:

1. Change your password immediately
2. Review your recent account activity
3. Enable two-factor authentication if not already active

Review your account security: {{SecurityLink}}

Best regards,
The Neo Service Layer Team";

        private string GetWelcomeHtml() => @"
<!DOCTYPE html>
<html>
<head>
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 40px 20px; text-align: center; }
        .content { padding: 20px; background: #f4f4f4; }
        .button { display: inline-block; padding: 12px 30px; background: #667eea; color: white; text-decoration: none; border-radius: 5px; margin: 10px; }
        .features { background: white; padding: 20px; margin: 20px 0; border-radius: 5px; }
        .footer { text-align: center; padding: 20px; color: #666; font-size: 12px; }
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Welcome to Neo Service Layer!</h1>
        </div>
        <div class='content'>
            <p>Hello {{Username}},</p>
            <p>Welcome aboard! We're excited to have you as part of our community.</p>
            <div class='features'>
                <h3>Here's what you can do:</h3>
                <ul>
                    <li>Access secure authentication services</li>
                    <li>Manage your profile and preferences</li>
                    <li>Enable two-factor authentication for extra security</li>
                    <li>Connect with our API services</li>
                </ul>
            </div>
            <p style='text-align: center;'>
                <a href='{{GettingStartedLink}}' class='button'>Get Started</a>
                <a href='{{ProfileLink}}' class='button'>Complete Your Profile</a>
            </p>
            <p>If you have any questions, feel free to reach out to us at {{SupportEmail}}.</p>
        </div>
        <div class='footer'>
            <p>&copy; 2024 Neo Service Layer. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";

        private string GetWelcomeText() => @"
Welcome to Neo Service Layer!

Hello {{Username}},

Welcome aboard! We're excited to have you as part of our community.

Here's what you can do:
- Access secure authentication services
- Manage your profile and preferences
- Enable two-factor authentication for extra security
- Connect with our API services

Get Started: {{GettingStartedLink}}
Complete Your Profile: {{ProfileLink}}

If you have any questions, feel free to reach out to us at {{SupportEmail}}.

Best regards,
The Neo Service Layer Team";

        #endregion
    }

    /// <summary>
    /// Email template service interface
    /// </summary>
    public interface IEmailTemplateService
    {
        Task<EmailContent> RenderTemplateAsync(string templateName, Dictionary<string, string> data);
    }

    /// <summary>
    /// Rendered email content
    /// </summary>
    public class EmailContent
    {
        public string Subject { get; set; }
        public string HtmlBody { get; set; }
        public string TextBody { get; set; }
    }

    /// <summary>
    /// Email template repository interface
    /// </summary>
    public interface IEmailTemplateRepository
    {
        Task<EmailTemplate> GetByNameAsync(string name);
        Task<EmailTemplate> CreateAsync(EmailTemplate template);
        Task<EmailTemplate> UpdateAsync(EmailTemplate template);
        Task<bool> DeleteAsync(string name);
        Task<List<EmailTemplate>> GetAllActiveAsync();
    }
}