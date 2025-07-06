# Notification Service Providers Implementation Guide

## Overview

The Notification Service currently has placeholder implementations for Email, SMS, and other notification channels. This guide explains how to implement real notification providers.

## Current State

The service has the following placeholder implementations:

1. **Email Provider** - Simulates email sending with delays
2. **SMS Provider** - Simulates SMS sending with delays  
3. **Webhook Provider** - Has basic HTTP POST implementation
4. **Other Channels** - Push, Slack, Discord, Telegram are not implemented

## Implementation Steps

### 1. Email Provider (SendGrid)

Replace the placeholder email implementation with SendGrid:

```csharp
private async Task<NotificationResult> SendEmailNotificationAsync(
    NotificationRequest request, 
    string notificationId)
{
    var client = new SendGridClient(_configuration["SendGrid:ApiKey"]);
    var from = new EmailAddress(_configuration["SendGrid:FromEmail"], 
                               _configuration["SendGrid:FromName"]);
    var to = new EmailAddress(request.Recipient);
    
    var msg = MailHelper.CreateSingleEmail(
        from, to, request.Subject, request.Message, request.Message);
    
    var response = await client.SendEmailAsync(msg);
    
    return new NotificationResult
    {
        NotificationId = notificationId,
        Success = response.IsSuccessStatusCode,
        Status = response.IsSuccessStatusCode 
            ? DeliveryStatus.Delivered 
            : DeliveryStatus.Failed,
        ErrorMessage = response.IsSuccessStatusCode 
            ? null 
            : await response.Body.ReadAsStringAsync(),
        SentAt = DateTime.UtcNow
    };
}
```

### 2. SMS Provider (Twilio)

Replace the placeholder SMS implementation with Twilio:

```csharp
private async Task<NotificationResult> SendSmsNotificationAsync(
    NotificationRequest request, 
    string notificationId)
{
    var accountSid = _configuration["Twilio:AccountSid"];
    var authToken = _configuration["Twilio:AuthToken"];
    
    TwilioClient.Init(accountSid, authToken);
    
    var message = await MessageResource.CreateAsync(
        body: request.Message,
        from: new Twilio.Types.PhoneNumber(_configuration["Twilio:FromNumber"]),
        to: new Twilio.Types.PhoneNumber(request.Recipient)
    );
    
    return new NotificationResult
    {
        NotificationId = notificationId,
        Success = message.Status != MessageResource.StatusEnum.Failed,
        Status = ConvertTwilioStatus(message.Status),
        ErrorMessage = message.ErrorMessage,
        SentAt = DateTime.UtcNow,
        DeliveredAt = message.DateSent?.DateTime
    };
}
```

### 3. Push Notification Provider (Firebase)

Implement push notifications using Firebase Cloud Messaging:

```csharp
private async Task<NotificationResult> SendPushNotificationAsync(
    NotificationRequest request, 
    string notificationId)
{
    var message = new Message()
    {
        Token = request.Recipient, // FCM token
        Notification = new Notification()
        {
            Title = request.Subject,
            Body = request.Message
        },
        Data = request.Metadata
    };
    
    string response = await FirebaseMessaging.DefaultInstance
        .SendAsync(message);
    
    return new NotificationResult
    {
        NotificationId = notificationId,
        Success = !string.IsNullOrEmpty(response),
        Status = DeliveryStatus.Delivered,
        SentAt = DateTime.UtcNow
    };
}
```

### 4. Slack Provider

Implement Slack notifications:

```csharp
private async Task<NotificationResult> SendSlackNotificationAsync(
    NotificationRequest request, 
    string notificationId)
{
    var webhookUrl = request.Metadata?.GetValueOrDefault("webhook_url") 
        ?? _configuration["Slack:DefaultWebhook"];
    
    var payload = new
    {
        text = request.Message,
        username = _configuration["Slack:BotName"],
        icon_emoji = ":robot_face:"
    };
    
    var response = await _httpClient.PostAsJsonAsync(webhookUrl, payload);
    
    return new NotificationResult
    {
        NotificationId = notificationId,
        Success = response.IsSuccessStatusCode,
        Status = response.IsSuccessStatusCode 
            ? DeliveryStatus.Delivered 
            : DeliveryStatus.Failed,
        SentAt = DateTime.UtcNow
    };
}
```

## Configuration

Add the following to `appsettings.json`:

```json
{
  "Notification": {
    "Providers": {
      "SendGrid": {
        "ApiKey": "${SENDGRID_API_KEY}",
        "FromEmail": "noreply@example.com",
        "FromName": "NEO Service Layer"
      },
      "Twilio": {
        "AccountSid": "${TWILIO_ACCOUNT_SID}",
        "AuthToken": "${TWILIO_AUTH_TOKEN}",
        "FromNumber": "+1234567890"
      },
      "Firebase": {
        "ProjectId": "your-project-id",
        "ServiceAccountKeyPath": "path/to/serviceAccountKey.json"
      },
      "Slack": {
        "DefaultWebhook": "${SLACK_WEBHOOK_URL}",
        "BotName": "NEO Notifier"
      }
    }
  }
}
```

## Security Considerations

1. **API Keys**: Store all API keys in secure configuration (Azure Key Vault, AWS Secrets Manager)
2. **Rate Limiting**: Implement rate limiting to prevent abuse
3. **Validation**: Validate all recipient addresses/numbers before sending
4. **Encryption**: Encrypt notification content for sensitive data
5. **Audit Trail**: Log all notification attempts for compliance

## Testing

Create integration tests for each provider:

```csharp
[Fact]
public async Task SendEmail_WithValidRecipient_ShouldSucceed()
{
    // Arrange
    var service = new NotificationService(/*dependencies*/);
    var request = new SendNotificationRequest
    {
        Channel = NotificationChannel.Email,
        Recipient = "test@example.com",
        Subject = "Test Email",
        Message = "This is a test"
    };
    
    // Act
    var result = await service.SendNotificationAsync(
        request, BlockchainType.NeoN3);
    
    // Assert
    Assert.True(result.Success);
    Assert.Equal(DeliveryStatus.Delivered, result.Status);
}
```

## Monitoring

Add metrics for each provider:

- Send success rate
- Average delivery time
- Error rates by type
- Provider availability

## NuGet Packages Required

```xml
<PackageReference Include="SendGrid" Version="9.28.1" />
<PackageReference Include="Twilio" Version="6.2.0" />
<PackageReference Include="FirebaseAdmin" Version="2.4.0" />
<PackageReference Include="Microsoft.Azure.NotificationHubs" Version="4.1.0" />
```