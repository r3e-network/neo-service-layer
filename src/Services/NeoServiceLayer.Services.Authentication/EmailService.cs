using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Mail;
using System.Net;
using System.Text;

namespace NeoServiceLayer.Services.Authentication
{
    /// <summary>
    /// Email service for sending authentication-related emails
    /// </summary>
    public interface IEmailService
    {
        Task SendVerificationEmailAsync(string email, string verificationToken);
        Task SendPasswordResetEmailAsync(string email, string resetToken);
        Task SendMfaCodeEmailAsync(string email, string code);
        Task SendAccountLockedEmailAsync(string email, string reason);
        Task SendPasswordChangedNotificationAsync(string email);
        Task SendNewLoginAlertAsync(string email, string ipAddress, string userAgent, DateTime loginTime);
    }

    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;
        private readonly IConfiguration _configuration;
        private readonly ISecurityLogger _securityLogger;
        private readonly string _smtpHost;
        private readonly int _smtpPort;
        private readonly string _smtpUsername;
        private readonly string _smtpPassword;
        private readonly bool _enableSsl;
        private readonly string _fromEmail;
        private readonly string _fromName;
        private readonly string _baseUrl;

        public EmailService(
            ILogger<EmailService> logger,
            IConfiguration configuration,
            ISecurityLogger securityLogger)
        {
            _logger = logger;
            _configuration = configuration;
            _securityLogger = securityLogger;

            // Load SMTP configuration
            _smtpHost = configuration["Email:Smtp:Host"] ?? "localhost";
            _smtpPort = configuration.GetValue<int>("Email:Smtp:Port", 587);
            _smtpUsername = configuration["Email:Smtp:Username"];
            _smtpPassword = configuration["Email:Smtp:Password"];
            _enableSsl = configuration.GetValue<bool>("Email:Smtp:EnableSsl", true);
            _fromEmail = configuration["Email:From:Address"] ?? "noreply@neoservicelayer.com";
            _fromName = configuration["Email:From:Name"] ?? "Neo Service Layer";
            _baseUrl = configuration["Application:BaseUrl"] ?? "https://localhost:5001";
        }

        public async Task SendVerificationEmailAsync(string email, string verificationToken)
        {
            var subject = "Verify Your Email Address";
            var verificationUrl = $"{_baseUrl}/api/v1/authentication/verify-email?token={Uri.EscapeDataString(verificationToken)}";
            
            var body = $@"
                <html>
                <head>
                    <style>
                        body {{ font-family: 'Segoe UI', Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
                        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
                        .button {{ display: inline-block; padding: 12px 30px; background: #667eea; color: white; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
                        .footer {{ text-align: center; color: #666; font-size: 12px; margin-top: 20px; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>Email Verification</h1>
                        </div>
                        <div class='content'>
                            <p>Welcome to Neo Service Layer!</p>
                            <p>Please verify your email address by clicking the button below:</p>
                            <center>
                                <a href='{verificationUrl}' class='button'>Verify Email Address</a>
                            </center>
                            <p>Or copy and paste this link into your browser:</p>
                            <p style='word-break: break-all; color: #667eea;'>{verificationUrl}</p>
                            <p>This link will expire in 24 hours.</p>
                            <p>If you didn't create an account, you can safely ignore this email.</p>
                        </div>
                        <div class='footer'>
                            <p>© 2024 Neo Service Layer. All rights reserved.</p>
                        </div>
                    </div>
                </body>
                </html>";

            await SendEmailAsync(email, subject, body, true);
            
            _logger.LogInformation("Verification email sent to {Email}", email);
        }

        public async Task SendPasswordResetEmailAsync(string email, string resetToken)
        {
            var subject = "Reset Your Password";
            var resetUrl = $"{_baseUrl}/api/v1/authentication/reset-password?token={Uri.EscapeDataString(resetToken)}";
            
            var body = $@"
                <html>
                <head>
                    <style>
                        body {{ font-family: 'Segoe UI', Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
                        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
                        .button {{ display: inline-block; padding: 12px 30px; background: #f5576c; color: white; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
                        .warning {{ background: #fff3cd; border: 1px solid #ffc107; padding: 10px; border-radius: 5px; margin: 20px 0; }}
                        .footer {{ text-align: center; color: #666; font-size: 12px; margin-top: 20px; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>Password Reset Request</h1>
                        </div>
                        <div class='content'>
                            <p>We received a request to reset your password.</p>
                            <p>Click the button below to create a new password:</p>
                            <center>
                                <a href='{resetUrl}' class='button'>Reset Password</a>
                            </center>
                            <p>Or copy and paste this link into your browser:</p>
                            <p style='word-break: break-all; color: #f5576c;'>{resetUrl}</p>
                            <div class='warning'>
                                <strong>Security Notice:</strong> This link will expire in 1 hour. If you didn't request a password reset, please ignore this email and ensure your account is secure.
                            </div>
                        </div>
                        <div class='footer'>
                            <p>© 2024 Neo Service Layer. All rights reserved.</p>
                        </div>
                    </div>
                </body>
                </html>";

            await SendEmailAsync(email, subject, body, true);
            
            await _securityLogger.LogSecurityEventAsync("PasswordResetEmailSent", null, 
                new Dictionary<string, object> { ["Email"] = email });
            
            _logger.LogInformation("Password reset email sent to {Email}", email);
        }

        public async Task SendMfaCodeEmailAsync(string email, string code)
        {
            var subject = "Your Two-Factor Authentication Code";
            
            var body = $@"
                <html>
                <head>
                    <style>
                        body {{ font-family: 'Segoe UI', Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
                        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
                        .code {{ font-size: 32px; font-weight: bold; color: #667eea; text-align: center; padding: 20px; background: white; border-radius: 5px; margin: 20px 0; letter-spacing: 5px; }}
                        .footer {{ text-align: center; color: #666; font-size: 12px; margin-top: 20px; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>Two-Factor Authentication</h1>
                        </div>
                        <div class='content'>
                            <p>Your verification code is:</p>
                            <div class='code'>{code}</div>
                            <p>Enter this code to complete your login. The code will expire in 5 minutes.</p>
                            <p><strong>Never share this code with anyone.</strong> Neo Service Layer staff will never ask for this code.</p>
                        </div>
                        <div class='footer'>
                            <p>© 2024 Neo Service Layer. All rights reserved.</p>
                        </div>
                    </div>
                </body>
                </html>";

            await SendEmailAsync(email, subject, body, true);
            
            _logger.LogInformation("MFA code email sent to {Email}", email);
        }

        public async Task SendAccountLockedEmailAsync(string email, string reason)
        {
            var subject = "Account Security Alert - Account Locked";
            
            var body = $@"
                <html>
                <head>
                    <style>
                        body {{ font-family: 'Segoe UI', Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background: linear-gradient(135deg, #ee5a24 0%, #f5af19 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
                        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
                        .alert {{ background: #f8d7da; border: 1px solid #dc3545; padding: 15px; border-radius: 5px; margin: 20px 0; }}
                        .footer {{ text-align: center; color: #666; font-size: 12px; margin-top: 20px; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>Security Alert</h1>
                        </div>
                        <div class='content'>
                            <div class='alert'>
                                <strong>Your account has been locked</strong>
                            </div>
                            <p><strong>Reason:</strong> {reason}</p>
                            <p>Your account has been temporarily locked for security reasons. This may be due to:</p>
                            <ul>
                                <li>Multiple failed login attempts</li>
                                <li>Suspicious activity detected</li>
                                <li>Administrative action</li>
                            </ul>
                            <p>To unlock your account, please contact our support team or wait for the lockout period to expire.</p>
                            <p>If you believe this is an error or you didn't attempt to log in, please contact support immediately.</p>
                        </div>
                        <div class='footer'>
                            <p>© 2024 Neo Service Layer. All rights reserved.</p>
                        </div>
                    </div>
                </body>
                </html>";

            await SendEmailAsync(email, subject, body, true);
            
            await _securityLogger.LogSecurityEventAsync("AccountLockedEmailSent", null, 
                new Dictionary<string, object> { ["Email"] = email, ["Reason"] = reason });
            
            _logger.LogWarning("Account locked email sent to {Email} for reason: {Reason}", email, reason);
        }

        public async Task SendPasswordChangedNotificationAsync(string email)
        {
            var subject = "Your Password Has Been Changed";
            
            var body = $@"
                <html>
                <head>
                    <style>
                        body {{ font-family: 'Segoe UI', Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
                        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
                        .info {{ background: #d1ecf1; border: 1px solid #17a2b8; padding: 15px; border-radius: 5px; margin: 20px 0; }}
                        .footer {{ text-align: center; color: #666; font-size: 12px; margin-top: 20px; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>Password Changed</h1>
                        </div>
                        <div class='content'>
                            <div class='info'>
                                <strong>Your password has been successfully changed</strong>
                            </div>
                            <p>This email confirms that your password was changed at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC.</p>
                            <p>If you made this change, no further action is required.</p>
                            <p><strong>If you didn't change your password:</strong></p>
                            <ul>
                                <li>Your account may be compromised</li>
                                <li>Reset your password immediately</li>
                                <li>Contact our support team</li>
                                <li>Review your account activity</li>
                            </ul>
                        </div>
                        <div class='footer'>
                            <p>© 2024 Neo Service Layer. All rights reserved.</p>
                        </div>
                    </div>
                </body>
                </html>";

            await SendEmailAsync(email, subject, body, true);
            
            _logger.LogInformation("Password changed notification sent to {Email}", email);
        }

        public async Task SendNewLoginAlertAsync(string email, string ipAddress, string userAgent, DateTime loginTime)
        {
            var subject = "New Login to Your Account";
            
            var body = $@"
                <html>
                <head>
                    <style>
                        body {{ font-family: 'Segoe UI', Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
                        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
                        .details {{ background: white; padding: 15px; border-radius: 5px; margin: 20px 0; }}
                        .footer {{ text-align: center; color: #666; font-size: 12px; margin-top: 20px; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>New Login Detected</h1>
                        </div>
                        <div class='content'>
                            <p>We detected a new login to your account:</p>
                            <div class='details'>
                                <p><strong>Time:</strong> {loginTime:yyyy-MM-dd HH:mm:ss} UTC</p>
                                <p><strong>IP Address:</strong> {ipAddress}</p>
                                <p><strong>Device:</strong> {userAgent}</p>
                            </div>
                            <p>If this was you, no action is needed.</p>
                            <p>If you don't recognize this login, please:</p>
                            <ul>
                                <li>Change your password immediately</li>
                                <li>Review your account activity</li>
                                <li>Enable two-factor authentication</li>
                            </ul>
                        </div>
                        <div class='footer'>
                            <p>© 2024 Neo Service Layer. All rights reserved.</p>
                        </div>
                    </div>
                </body>
                </html>";

            await SendEmailAsync(email, subject, body, true);
            
            _logger.LogInformation("New login alert sent to {Email} from IP {IpAddress}", email, ipAddress);
        }

        private async Task SendEmailAsync(string to, string subject, string body, bool isHtml = false)
        {
            try
            {
                // In production, use a proper email service like SendGrid, AWS SES, etc.
                // This is a basic SMTP implementation for demonstration
                
                if (string.IsNullOrEmpty(_smtpHost) || _smtpHost == "localhost")
                {
                    // Log email content for development/testing
                    _logger.LogInformation("Email (Mock Send) - To: {To}, Subject: {Subject}", to, subject);
                    await Task.CompletedTask;
                    return;
                }

                using var client = new SmtpClient(_smtpHost, _smtpPort)
                {
                    EnableSsl = _enableSsl,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false
                };

                if (!string.IsNullOrEmpty(_smtpUsername) && !string.IsNullOrEmpty(_smtpPassword))
                {
                    client.Credentials = new NetworkCredential(_smtpUsername, _smtpPassword);
                }

                var message = new MailMessage
                {
                    From = new MailAddress(_fromEmail, _fromName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = isHtml
                };
                message.To.Add(new MailAddress(to));

                await client.SendMailAsync(message);
                
                _logger.LogInformation("Email sent successfully to {To}", to);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {To}", to);
                throw new InvalidOperationException($"Failed to send email to {to}", ex);
            }
        }
    }
}