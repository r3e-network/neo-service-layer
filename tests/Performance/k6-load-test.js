import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate, Trend } from 'k6/metrics';

// Custom metrics
const errorRate = new Rate('errors');
const notificationDuration = new Trend('notification_duration');
const configurationDuration = new Trend('configuration_duration');
const smartContractDuration = new Trend('smart_contract_duration');

// Test configuration
export const options = {
  stages: [
    { duration: '30s', target: 10 },   // Ramp up to 10 users
    { duration: '1m', target: 50 },    // Ramp up to 50 users
    { duration: '3m', target: 100 },   // Stay at 100 users
    { duration: '1m', target: 200 },   // Spike to 200 users
    { duration: '30s', target: 0 },    // Ramp down to 0 users
  ],
  thresholds: {
    http_req_duration: ['p(95)<500'],  // 95% of requests must complete below 500ms
    http_req_failed: ['rate<0.1'],     // Error rate must be below 10%
    errors: ['rate<0.1'],              // Custom error rate below 10%
  },
};

const BASE_URL = 'http://localhost:5000';

export default function () {
  const userId = `user_${__VU}_${Date.now()}`;
  
  // Test 1: Send notification
  const notificationPayload = JSON.stringify({
    recipient: `${userId}@example.com`,
    subject: 'Load Test Notification',
    message: 'This is a load test notification',
    channel: 'Email',
  });

  const notificationRes = http.post(
    `${BASE_URL}/api/notification/send`,
    notificationPayload,
    {
      headers: { 'Content-Type': 'application/json' },
      tags: { name: 'send_notification' },
    }
  );

  check(notificationRes, {
    'notification sent successfully': (r) => r.status === 200,
    'notification has ID': (r) => r.json('notificationId') !== undefined,
  });

  errorRate.add(notificationRes.status !== 200);
  notificationDuration.add(notificationRes.timings.duration);

  sleep(1);

  // Test 2: Configuration read/write
  const configKey = `test_key_${userId}`;
  const configValue = `test_value_${Date.now()}`;
  
  const configWriteRes = http.post(
    `${BASE_URL}/api/configuration/loadtest/${configKey}`,
    JSON.stringify({ value: configValue }),
    {
      headers: { 'Content-Type': 'application/json' },
      tags: { name: 'write_config' },
    }
  );

  check(configWriteRes, {
    'config written successfully': (r) => r.status === 200,
  });

  const configReadRes = http.get(
    `${BASE_URL}/api/configuration/loadtest/${configKey}`,
    {
      tags: { name: 'read_config' },
    }
  );

  check(configReadRes, {
    'config read successfully': (r) => r.status === 200,
    'config value matches': (r) => r.json('value') === configValue,
  });

  configurationDuration.add(configReadRes.timings.duration);

  sleep(1);

  // Test 3: Smart contract query (read-only)
  const contractQueryRes = http.get(
    `${BASE_URL}/api/smart-contracts/metadata/0x1234567890abcdef`,
    {
      tags: { name: 'query_contract' },
    }
  );

  check(contractQueryRes, {
    'contract query successful': (r) => r.status === 200 || r.status === 404,
  });

  smartContractDuration.add(contractQueryRes.timings.duration);

  // Test 4: Health check
  const healthRes = http.get(`${BASE_URL}/health`, {
    tags: { name: 'health_check' },
  });

  check(healthRes, {
    'API gateway is healthy': (r) => r.status === 200,
  });

  // Test 5: Service discovery
  const servicesRes = http.get(`${BASE_URL}/api/gateway/services`, {
    tags: { name: 'service_discovery' },
  });

  check(servicesRes, {
    'services discovered': (r) => r.status === 200,
    'multiple services found': (r) => r.json().length > 5,
  });

  sleep(2);
}

// Custom end-of-test summary
export function handleSummary(data) {
  return {
    'stdout': textSummary(data, { indent: ' ', enableColors: true }),
    'summary.json': JSON.stringify(data),
    'summary.html': htmlReport(data),
  };
}

function textSummary(data, options) {
  const { metrics } = data;
  
  return `
Load Test Summary
=================
Total Requests: ${metrics.http_reqs.values.count}
Failed Requests: ${metrics.http_req_failed.values.rate * 100}%
Average Duration: ${metrics.http_req_duration.values.avg}ms
95th Percentile: ${metrics.http_req_duration.values['p(95)']}ms

Service Metrics:
- Notifications: ${metrics.notification_duration.values.avg}ms avg
- Configuration: ${metrics.configuration_duration.values.avg}ms avg
- Smart Contracts: ${metrics.smart_contract_duration.values.avg}ms avg

Error Rate: ${metrics.errors.values.rate * 100}%
`;
}

function htmlReport(data) {
  return `
<!DOCTYPE html>
<html>
<head>
    <title>Neo Service Layer Load Test Results</title>
    <style>
        body { font-family: Arial, sans-serif; margin: 20px; }
        .metric { margin: 10px 0; padding: 10px; background: #f0f0f0; }
        .success { color: green; }
        .failure { color: red; }
    </style>
</head>
<body>
    <h1>Load Test Results</h1>
    <div class="metric">
        <h3>Summary</h3>
        <p>Total Requests: ${data.metrics.http_reqs.values.count}</p>
        <p>Duration: ${data.state.testRunDurationMs}ms</p>
        <p>Error Rate: ${(data.metrics.http_req_failed.values.rate * 100).toFixed(2)}%</p>
    </div>
    <div class="metric">
        <h3>Response Times</h3>
        <p>Average: ${data.metrics.http_req_duration.values.avg.toFixed(2)}ms</p>
        <p>95th Percentile: ${data.metrics.http_req_duration.values['p(95)'].toFixed(2)}ms</p>
        <p>Max: ${data.metrics.http_req_duration.values.max.toFixed(2)}ms</p>
    </div>
</body>
</html>
`;
}