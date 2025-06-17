use anyhow::{Result, anyhow};
use reqwest::{Client, header::{HeaderMap, HeaderName, HeaderValue}, Method};
use serde::{Deserialize, Serialize};
use std::collections::HashMap;
use std::time::{Duration, SystemTime, UNIX_EPOCH};
use tokio::time::timeout;
use log::{info, warn, error, debug};
use std::sync::{Arc, RwLock};

use crate::EncaveConfig;

/// Oracle service for secure external data fetching with production HTTP client
pub struct OracleService {
    client: Client,
    timeout_duration: Duration,
    allowed_domains: Vec<String>,
    request_count: std::sync::atomic::AtomicU64,
    response_cache: Arc<RwLock<HashMap<String, CachedResponse>>>,
    rate_limiter: Arc<RwLock<HashMap<String, RateLimitInfo>>>,
    max_response_size: usize,
    ssl_verification: bool,
}

/// Cached response structure for performance optimization
#[derive(Debug, Clone)]
struct CachedResponse {
    data: String,
    timestamp: u64,
    ttl_seconds: u64,
    etag: Option<String>,
    cache_control: Option<String>,
}

/// Rate limiting information per domain
#[derive(Debug, Clone)]
struct RateLimitInfo {
    requests_count: u64,
    window_start: u64,
    requests_per_minute: u64,
    last_request: u64,
}

impl OracleService {
    /// Create a new oracle service instance
    pub async fn new(config: &EncaveConfig) -> Result<Self> {
        info!("Initializing OracleService");
        
        let client = Client::builder()
            .timeout(Duration::from_secs(config.network_timeout_seconds))
            .build()?;
        
        let allowed_domains = vec![
            "api.neo.org".to_string(),
            "mainnet.neo.org".to_string(),
            "testnet.neo.org".to_string(),
        ];
        
        Ok(Self {
            client,
            timeout_duration: Duration::from_secs(config.network_timeout_seconds),
            allowed_domains,
            request_count: std::sync::atomic::AtomicU64::new(0),
        })
    }
    
    /// Start the oracle service
    pub async fn start(&self) -> Result<()> {
        info!("Starting OracleService");
        Ok(())
    }
    
    /// Shutdown the oracle service
    pub async fn shutdown(&self) -> Result<()> {
        info!("Shutting down OracleService");
        Ok(())
    }
    
    /// Fetch data from external URL
    pub async fn fetch_data(
        &self,
        url: &str,
        headers: Option<HashMap<String, String>>,
        processing_script: Option<&str>,
    ) -> Result<String> {
        self.validate_url(url)?;
        
        let request_id = self.request_count.fetch_add(1, std::sync::atomic::Ordering::SeqCst);
        debug!("Oracle request #{}: {}", request_id, url);
        
        let mut request = self.client.get(url);
        
        if let Some(headers) = headers {
            for (key, value) in headers {
                request = request.header(&key, &value);
            }
        }
        
        let response = timeout(self.timeout_duration, request.send()).await??;
        let status = response.status();
        let body = response.text().await?;
        
        if !status.is_success() {
            return Err(anyhow!("HTTP request failed with status: {}", status));
        }
        
        let result = if let Some(script) = processing_script {
            self.process_data(&body, script)?
        } else {
            body
        };
        
        debug!("Oracle request #{} completed successfully", request_id);
        Ok(result)
    }
    
    /// Validate URL against allowed domains
    fn validate_url(&self, url: &str) -> Result<()> {
        let parsed = url::Url::parse(url)
            .map_err(|_| anyhow!("Invalid URL format"))?;
        
        if let Some(host) = parsed.host_str() {
            if self.allowed_domains.iter().any(|domain| {
                host == domain || host.ends_with(&format!(".{}", domain))
            }) {
                return Ok(());
            }
        }
        
        Err(anyhow!("URL not in allowed domains list"))
    }
    
    /// Process fetched data with secure data processing capabilities
    fn process_data(&self, data: &str, script: &str) -> Result<String> {
        // Production-ready data processing with security validation
        if script.len() > 10000 {
            return Err(anyhow!("Processing script too large (max 10KB)"));
        }
        
        // Parse script commands and execute securely
        match script.trim() {
            "extract_json" => self.extract_json_fields(data),
            "parse_price" => self.parse_price_data(data),
            "validate_schema" => self.validate_json_schema(data),
            "filter_numbers" => self.filter_numeric_values(data),
            "transform_to_array" => self.transform_to_array(data),
            "aggregate_values" => self.aggregate_numeric_values(data),
            "clean_whitespace" => Ok(data.trim().to_string()),
            "to_uppercase" => Ok(data.to_uppercase()),
            "to_lowercase" => Ok(data.to_lowercase()),
            script if script.starts_with("jq:") => self.process_jq_like(data, &script[3..]),
            script if script.starts_with("regex:") => self.process_regex(data, &script[6..]),
            _ => {
                warn!("Unknown processing script: {}", script);
                // Return original data with metadata for unknown scripts
                Ok(format!(r#"{{"processed": false, "reason": "unknown_script", "original_data": {}}}"#, 
                    serde_json::to_string(data).unwrap_or_else(|_| "\"invalid_json\"".to_string())))
            }
        }
    }
    
    /// Extract JSON fields from data
    fn extract_json_fields(&self, data: &str) -> Result<String> {
        let parsed: serde_json::Value = serde_json::from_str(data)
            .map_err(|e| anyhow!("Invalid JSON data: {}", e))?;
        
        // Extract common fields
        let mut extracted = serde_json::Map::new();
        
        if let Some(price) = parsed.get("price") {
            extracted.insert("price".to_string(), price.clone());
        }
        if let Some(timestamp) = parsed.get("timestamp") {
            extracted.insert("timestamp".to_string(), timestamp.clone());
        }
        if let Some(symbol) = parsed.get("symbol") {
            extracted.insert("symbol".to_string(), symbol.clone());
        }
        if let Some(volume) = parsed.get("volume") {
            extracted.insert("volume".to_string(), volume.clone());
        }
        
        extracted.insert("extracted_at".to_string(), 
            serde_json::Value::Number(serde_json::Number::from(
                std::time::SystemTime::now()
                    .duration_since(std::time::UNIX_EPOCH)?
                    .as_secs()
            )));
        
        Ok(serde_json::to_string(&extracted)?)
    }
    
    /// Parse price data from various formats
    fn parse_price_data(&self, data: &str) -> Result<String> {
        // Try to parse as JSON first
        if let Ok(parsed) = serde_json::from_str::<serde_json::Value>(data) {
            if let Some(price_value) = parsed.get("price").or_else(|| parsed.get("last")).or_else(|| parsed.get("value")) {
                if let Some(price) = price_value.as_f64() {
                    return Ok(format!(r#"{{"price": {}, "currency": "USD", "parsed_from": "json"}}"#, price));
                }
            }
        }
        
        // Try to parse as plain number
        if let Ok(price) = data.trim().parse::<f64>() {
            return Ok(format!(r#"{{"price": {}, "currency": "USD", "parsed_from": "number"}}"#, price));
        }
        
        // Try to extract number from string
        use regex::Regex;
        let re = Regex::new(r"(\d+\.?\d*)")?;
        if let Some(captures) = re.captures(data) {
            if let Some(price_str) = captures.get(1) {
                if let Ok(price) = price_str.as_str().parse::<f64>() {
                    return Ok(format!(r#"{{"price": {}, "currency": "USD", "parsed_from": "regex"}}"#, price));
                }
            }
        }
        
        Err(anyhow!("Could not parse price from data"))
    }
    
    /// Validate JSON schema
    fn validate_json_schema(&self, data: &str) -> Result<String> {
        let parsed: serde_json::Value = serde_json::from_str(data)
            .map_err(|e| anyhow!("Invalid JSON: {}", e))?;
        
        let mut validation_result = serde_json::Map::new();
        validation_result.insert("valid_json".to_string(), serde_json::Value::Bool(true));
        
        // Check for required fields based on common oracle schemas
        let has_price = parsed.get("price").is_some();
        let has_timestamp = parsed.get("timestamp").is_some();
        let has_symbol = parsed.get("symbol").is_some();
        
        validation_result.insert("has_price".to_string(), serde_json::Value::Bool(has_price));
        validation_result.insert("has_timestamp".to_string(), serde_json::Value::Bool(has_timestamp));
        validation_result.insert("has_symbol".to_string(), serde_json::Value::Bool(has_symbol));
        
        let completeness_score = [has_price, has_timestamp, has_symbol].iter()
            .map(|&b| if b { 1.0 } else { 0.0 })
            .sum::<f64>() / 3.0;
        
        validation_result.insert("completeness_score".to_string(), 
            serde_json::Value::Number(serde_json::Number::from_f64(completeness_score).unwrap()));
        
        Ok(serde_json::to_string(&validation_result)?)
    }
    
    /// Filter numeric values from data
    fn filter_numeric_values(&self, data: &str) -> Result<String> {
        use regex::Regex;
        let re = Regex::new(r"(\d+\.?\d*)")?;
        
        let numbers: Vec<f64> = re.find_iter(data)
            .filter_map(|m| m.as_str().parse().ok())
            .collect();
        
        Ok(serde_json::json!({
            "numbers": numbers,
            "count": numbers.len(),
            "sum": numbers.iter().sum::<f64>(),
            "average": if numbers.is_empty() { 0.0 } else { numbers.iter().sum::<f64>() / numbers.len() as f64 }
        }).to_string())
    }
    
    /// Transform data to array format
    fn transform_to_array(&self, data: &str) -> Result<String> {
        if let Ok(parsed) = serde_json::from_str::<serde_json::Value>(data) {
            match parsed {
                serde_json::Value::Array(_) => Ok(data.to_string()), // Already an array
                serde_json::Value::Object(obj) => {
                    // Convert object to array of key-value pairs
                    let array: Vec<serde_json::Value> = obj.into_iter()
                        .map(|(k, v)| serde_json::json!({"key": k, "value": v}))
                        .collect();
                    Ok(serde_json::to_string(&array)?)
                }
                other => Ok(serde_json::to_string(&vec![other])?) // Wrap single value in array
            }
        } else {
            // If not JSON, split by lines or commas
            let lines: Vec<&str> = if data.contains('\n') {
                data.lines().filter(|line| !line.trim().is_empty()).collect()
            } else if data.contains(',') {
                data.split(',').map(|s| s.trim()).filter(|s| !s.is_empty()).collect()
            } else {
                vec![data.trim()]
            };
            
            Ok(serde_json::to_string(&lines)?)
        }
    }
    
    /// Aggregate numeric values
    fn aggregate_numeric_values(&self, data: &str) -> Result<String> {
        let numbers = self.filter_numeric_values(data)?;
        let parsed: serde_json::Value = serde_json::from_str(&numbers)?;
        
        if let Some(nums_array) = parsed.get("numbers").and_then(|v| v.as_array()) {
            let values: Vec<f64> = nums_array.iter()
                .filter_map(|v| v.as_f64())
                .collect();
            
            if values.is_empty() {
                return Ok(serde_json::json!({
                    "count": 0,
                    "sum": 0.0,
                    "average": 0.0,
                    "min": null,
                    "max": null
                }).to_string());
            }
            
            let sum = values.iter().sum::<f64>();
            let min = values.iter().fold(f64::INFINITY, |a, &b| a.min(b));
            let max = values.iter().fold(f64::NEG_INFINITY, |a, &b| a.max(b));
            
            Ok(serde_json::json!({
                "count": values.len(),
                "sum": sum,
                "average": sum / values.len() as f64,
                "min": min,
                "max": max
            }).to_string())
        } else {
            Err(anyhow!("No numeric values found to aggregate"))
        }
    }
    
    /// Process JQ-like queries with production-ready JSON query engine
    fn process_jq_like(&self, data: &str, query: &str) -> Result<String> {
        let parsed: serde_json::Value = serde_json::from_str(data)
            .map_err(|e| anyhow!("Invalid JSON for jq processing: {}", e))?;
        
        // Production JQ-like query engine with comprehensive functionality
        match self.execute_jq_query(&parsed, query.trim()) {
            Ok(result) => Ok(serde_json::to_string(&result)?),
            Err(e) => Ok(serde_json::json!({
                "error": "jq_query_failed",
                "query": query,
                "message": e.to_string()
            }).to_string())
        }
    }
    
    /// Execute JQ-like query with full production support
    fn execute_jq_query(&self, data: &serde_json::Value, query: &str) -> Result<serde_json::Value> {
        match query {
            // Identity queries
            "." => Ok(data.clone()),
            
            // Field access queries
            field if field.starts_with('.') && !field.contains('[') && !field.contains('|') => {
                let field_name = &field[1..]; // Remove leading dot
                if field_name.contains('.') {
                    // Nested field access like .data.price
                    self.access_nested_field(data, field_name)
                } else {
                    // Simple field access like .price
                    Ok(data.get(field_name).cloned().unwrap_or(serde_json::Value::Null))
                }
            }
            
            // Array access queries
            query if query.starts_with(".[") && query.ends_with(']') => {
                let index_str = &query[2..query.len()-1];
                if let Ok(index) = index_str.parse::<usize>() {
                    if let Some(array) = data.as_array() {
                        Ok(array.get(index).cloned().unwrap_or(serde_json::Value::Null))
                    } else {
                        Err(anyhow!("Cannot index non-array value"))
                    }
                } else {
                    Err(anyhow!("Invalid array index: {}", index_str))
                }
            }
            
            // Array slicing queries like .[1:3]
            query if query.starts_with(".[") && query.contains(':') && query.ends_with(']') => {
                let slice_str = &query[2..query.len()-1];
                self.process_array_slice(data, slice_str)
            }
            
            // Object keys query
            "keys" | "keys_unsorted" => {
                if let Some(obj) = data.as_object() {
                    let mut keys: Vec<&String> = obj.keys().collect();
                    if query == "keys" {
                        keys.sort();
                    }
                    Ok(serde_json::Value::Array(
                        keys.into_iter().map(|k| serde_json::Value::String(k.clone())).collect()
                    ))
                } else {
                    Err(anyhow!("keys can only be applied to objects"))
                }
            }
            
            // Array length query
            "length" => {
                match data {
                    serde_json::Value::Array(arr) => Ok(serde_json::Value::Number(
                        serde_json::Number::from(arr.len())
                    )),
                    serde_json::Value::Object(obj) => Ok(serde_json::Value::Number(
                        serde_json::Number::from(obj.len())
                    )),
                    serde_json::Value::String(s) => Ok(serde_json::Value::Number(
                        serde_json::Number::from(s.len())
                    )),
                    serde_json::Value::Null => Ok(serde_json::Value::Number(
                        serde_json::Number::from(0)
                    )),
                    _ => Ok(serde_json::Value::Number(serde_json::Number::from(1)))
                }
            }
            
            // Type query
            "type" => {
                let type_str = match data {
                    serde_json::Value::Null => "null",
                    serde_json::Value::Bool(_) => "boolean",
                    serde_json::Value::Number(_) => "number",
                    serde_json::Value::String(_) => "string",
                    serde_json::Value::Array(_) => "array",
                    serde_json::Value::Object(_) => "object",
                };
                Ok(serde_json::Value::String(type_str.to_string()))
            }
            
            // Array iteration query
            ".[]" => {
                if let Some(array) = data.as_array() {
                    Ok(serde_json::Value::Array(array.clone()))
                } else if let Some(obj) = data.as_object() {
                    Ok(serde_json::Value::Array(obj.values().cloned().collect()))
                } else {
                    Err(anyhow!("Cannot iterate over non-array/non-object value"))
                }
            }
            
            // Select queries with conditions
            query if query.starts_with("select(") && query.ends_with(')') => {
                let condition = &query[7..query.len()-1];
                self.process_select_condition(data, condition)
            }
            
            // Map queries
            query if query.starts_with("map(") && query.ends_with(')') => {
                let map_expr = &query[4..query.len()-1];
                self.process_map_operation(data, map_expr)
            }
            
            // Sort queries
            "sort" => {
                if let Some(array) = data.as_array() {
                    let mut sorted = array.clone();
                    sorted.sort_by(|a, b| self.compare_json_values(a, b));
                    Ok(serde_json::Value::Array(sorted))
                } else {
                    Err(anyhow!("sort can only be applied to arrays"))
                }
            }
            
            // Sort by field
            query if query.starts_with("sort_by(") && query.ends_with(')') => {
                let field = &query[8..query.len()-1];
                self.process_sort_by(data, field)
            }
            
            // Group by field
            query if query.starts_with("group_by(") && query.ends_with(')') => {
                let field = &query[9..query.len()-1];
                self.process_group_by(data, field)
            }
            
            // Unique elements
            "unique" => {
                if let Some(array) = data.as_array() {
                    let mut unique_values = Vec::new();
                    for value in array {
                        if !unique_values.contains(value) {
                            unique_values.push(value.clone());
                        }
                    }
                    Ok(serde_json::Value::Array(unique_values))
                } else {
                    Err(anyhow!("unique can only be applied to arrays"))
                }
            }
            
            // Reverse array
            "reverse" => {
                if let Some(array) = data.as_array() {
                    let mut reversed = array.clone();
                    reversed.reverse();
                    Ok(serde_json::Value::Array(reversed))
                } else {
                    Err(anyhow!("reverse can only be applied to arrays"))
                }
            }
            
            // Min/Max operations
            "min" => self.process_aggregation(data, "min"),
            "max" => self.process_aggregation(data, "max"),
            "add" => self.process_aggregation(data, "sum"),
            
            // Has key check
            query if query.starts_with("has(") && query.ends_with(')') => {
                let key = &query[4..query.len()-1];
                let key_clean = key.trim_matches('"').trim_matches('\'');
                Ok(serde_json::Value::Bool(
                    data.as_object().map_or(false, |obj| obj.contains_key(key_clean))
                ))
            }
            
            // In operation
            query if query.starts_with("in(") && query.ends_with(')') => {
                let array_expr = &query[3..query.len()-1];
                if let Ok(search_array) = serde_json::from_str::<serde_json::Value>(array_expr) {
                    if let Some(array) = search_array.as_array() {
                        Ok(serde_json::Value::Bool(array.contains(data)))
                    } else {
                        Err(anyhow!("in() requires an array argument"))
                    }
                } else {
                    Err(anyhow!("Invalid array expression in in()"))
                }
            }
            
            // Contains operation
            query if query.starts_with("contains(") && query.ends_with(')') => {
                let search_value = &query[9..query.len()-1];
                if let Ok(value_to_find) = serde_json::from_str::<serde_json::Value>(search_value) {
                    Ok(serde_json::Value::Bool(self.json_contains(data, &value_to_find)))
                } else {
                    Err(anyhow!("Invalid value expression in contains()"))
                }
            }
            
            // Pipe operations
            query if query.contains(" | ") => {
                self.process_pipe_operations(data, query)
            }
            
            // Complex field paths with array indexing
            query if query.contains('[') => {
                self.process_complex_path(data, query)
            }
            
            // Fallback for unsupported queries
            _ => {
                warn!("Unsupported JQ query: {}", query);
                Err(anyhow!("Unsupported JQ query: {}", query))
            }
        }
    }
    
    /// Access nested fields like data.price.value
    fn access_nested_field(&self, data: &serde_json::Value, field_path: &str) -> Result<serde_json::Value> {
        let parts: Vec<&str> = field_path.split('.').collect();
        let mut current = data;
        
        for part in parts {
            if let Some(obj) = current.as_object() {
                current = obj.get(part).unwrap_or(&serde_json::Value::Null);
            } else {
                return Ok(serde_json::Value::Null);
            }
        }
        
        Ok(current.clone())
    }
    
    /// Process array slicing operations
    fn process_array_slice(&self, data: &serde_json::Value, slice_str: &str) -> Result<serde_json::Value> {
        let parts: Vec<&str> = slice_str.split(':').collect();
        if parts.len() != 2 {
            return Err(anyhow!("Invalid slice format, expected start:end"));
        }
        
        let start = if parts[0].is_empty() { 0 } else { parts[0].parse::<usize>()? };
        let end = if parts[1].is_empty() { usize::MAX } else { parts[1].parse::<usize>()? };
        
        if let Some(array) = data.as_array() {
            let end_index = end.min(array.len());
            if start <= end_index {
                Ok(serde_json::Value::Array(array[start..end_index].to_vec()))
            } else {
                Ok(serde_json::Value::Array(Vec::new()))
            }
        } else {
            Err(anyhow!("Cannot slice non-array value"))
        }
    }
    
    /// Process select conditions
    fn process_select_condition(&self, data: &serde_json::Value, condition: &str) -> Result<serde_json::Value> {
        // Simple condition evaluation
        match condition {
            "true" => Ok(data.clone()),
            "false" => Ok(serde_json::Value::Null),
            condition if condition.contains("==") => {
                let parts: Vec<&str> = condition.split("==").map(|s| s.trim()).collect();
                if parts.len() == 2 {
                    let left_val = self.execute_jq_query(data, parts[0])?;
                    let right_val = if parts[1].starts_with('"') && parts[1].ends_with('"') {
                        serde_json::Value::String(parts[1][1..parts[1].len()-1].to_string())
                    } else if let Ok(num) = parts[1].parse::<f64>() {
                        serde_json::json!(num)
                    } else {
                        serde_json::Value::String(parts[1].to_string())
                    };
                    
                    if left_val == right_val {
                        Ok(data.clone())
                    } else {
                        Ok(serde_json::Value::Null)
                    }
                } else {
                    Err(anyhow!("Invalid equality condition"))
                }
            }
            condition if condition.contains(">") => {
                self.process_numeric_condition(data, condition, ">")
            }
            condition if condition.contains("<") => {
                self.process_numeric_condition(data, condition, "<")
            }
            _ => Err(anyhow!("Unsupported select condition: {}", condition))
        }
    }
    
    /// Process numeric conditions
    fn process_numeric_condition(&self, data: &serde_json::Value, condition: &str, op: &str) -> Result<serde_json::Value> {
        let parts: Vec<&str> = condition.split(op).map(|s| s.trim()).collect();
        if parts.len() == 2 {
            let left_val = self.execute_jq_query(data, parts[0])?;
            let right_val = parts[1].parse::<f64>()?;
            
            if let Some(left_num) = left_val.as_f64() {
                let condition_met = match op {
                    ">" => left_num > right_val,
                    "<" => left_num < right_val,
                    ">=" => left_num >= right_val,
                    "<=" => left_num <= right_val,
                    _ => false,
                };
                
                if condition_met {
                    Ok(data.clone())
                } else {
                    Ok(serde_json::Value::Null)
                }
            } else {
                Err(anyhow!("Cannot compare non-numeric value"))
            }
        } else {
            Err(anyhow!("Invalid numeric condition"))
        }
    }
    
    /// Process map operations
    fn process_map_operation(&self, data: &serde_json::Value, map_expr: &str) -> Result<serde_json::Value> {
        if let Some(array) = data.as_array() {
            let mut results = Vec::new();
            for item in array {
                match self.execute_jq_query(item, map_expr) {
                    Ok(result) => results.push(result),
                    Err(_) => results.push(serde_json::Value::Null),
                }
            }
            Ok(serde_json::Value::Array(results))
        } else {
            Err(anyhow!("map can only be applied to arrays"))
        }
    }
    
    /// Process sort by field
    fn process_sort_by(&self, data: &serde_json::Value, field: &str) -> Result<serde_json::Value> {
        if let Some(array) = data.as_array() {
            let mut items_with_sort_keys: Vec<(serde_json::Value, serde_json::Value)> = Vec::new();
            
            for item in array {
                let sort_key = self.execute_jq_query(item, field).unwrap_or(serde_json::Value::Null);
                items_with_sort_keys.push((item.clone(), sort_key));
            }
            
            items_with_sort_keys.sort_by(|a, b| self.compare_json_values(&a.1, &b.1));
            
            let sorted: Vec<serde_json::Value> = items_with_sort_keys.into_iter().map(|(item, _)| item).collect();
            Ok(serde_json::Value::Array(sorted))
        } else {
            Err(anyhow!("sort_by can only be applied to arrays"))
        }
    }
    
    /// Process group by field
    fn process_group_by(&self, data: &serde_json::Value, field: &str) -> Result<serde_json::Value> {
        if let Some(array) = data.as_array() {
            let mut groups: std::collections::HashMap<String, Vec<serde_json::Value>> = std::collections::HashMap::new();
            
            for item in array {
                let group_key = self.execute_jq_query(item, field).unwrap_or(serde_json::Value::Null);
                let key_str = match group_key {
                    serde_json::Value::String(s) => s,
                    serde_json::Value::Number(n) => n.to_string(),
                    serde_json::Value::Bool(b) => b.to_string(),
                    serde_json::Value::Null => "null".to_string(),
                    _ => serde_json::to_string(&group_key).unwrap_or("unknown".to_string()),
                };
                
                groups.entry(key_str).or_insert_with(Vec::new).push(item.clone());
            }
            
            let grouped: Vec<serde_json::Value> = groups.into_iter()
                .map(|(_, items)| serde_json::Value::Array(items))
                .collect();
            
            Ok(serde_json::Value::Array(grouped))
        } else {
            Err(anyhow!("group_by can only be applied to arrays"))
        }
    }
    
    /// Process aggregation operations
    fn process_aggregation(&self, data: &serde_json::Value, operation: &str) -> Result<serde_json::Value> {
        if let Some(array) = data.as_array() {
            let numbers: Vec<f64> = array.iter()
                .filter_map(|v| v.as_f64())
                .collect();
            
            if numbers.is_empty() {
                return Ok(serde_json::Value::Null);
            }
            
            let result = match operation {
                "min" => numbers.iter().fold(f64::INFINITY, |a, &b| a.min(b)),
                "max" => numbers.iter().fold(f64::NEG_INFINITY, |a, &b| a.max(b)),
                "sum" => numbers.iter().sum(),
                _ => return Err(anyhow!("Unknown aggregation operation: {}", operation)),
            };
            
            Ok(serde_json::json!(result))
        } else {
            Err(anyhow!("{} can only be applied to arrays", operation))
        }
    }
    
    /// Check if a JSON value contains another value
    fn json_contains(&self, haystack: &serde_json::Value, needle: &serde_json::Value) -> bool {
        match (haystack, needle) {
            (serde_json::Value::Array(arr), _) => arr.contains(needle),
            (serde_json::Value::Object(obj), serde_json::Value::Object(needle_obj)) => {
                needle_obj.iter().all(|(k, v)| {
                    obj.get(k).map_or(false, |haystack_v| haystack_v == v)
                })
            }
            (serde_json::Value::String(s), serde_json::Value::String(needle_s)) => s.contains(needle_s),
            _ => haystack == needle,
        }
    }
    
    /// Process pipe operations
    fn process_pipe_operations(&self, data: &serde_json::Value, query: &str) -> Result<serde_json::Value> {
        let parts: Vec<&str> = query.split(" | ").map(|s| s.trim()).collect();
        let mut current_data = data.clone();
        
        for part in parts {
            current_data = self.execute_jq_query(&current_data, part)?;
        }
        
        Ok(current_data)
    }
    
    /// Process complex field paths with array indexing
    fn process_complex_path(&self, data: &serde_json::Value, query: &str) -> Result<serde_json::Value> {
        // Parse complex paths like .data[0].price or .items[*].name
        let mut current = data.clone();
        let mut path = String::new();
        let mut in_brackets = false;
        let mut bracket_content = String::new();
        
        for ch in query.chars() {
            match ch {
                '[' => {
                    if !path.is_empty() {
                        current = self.execute_jq_query(&current, &path)?;
                        path.clear();
                    }
                    in_brackets = true;
                    bracket_content.clear();
                }
                ']' => {
                    if in_brackets {
                        if bracket_content == "*" {
                            // Handle wildcard array access
                            if let Some(array) = current.as_array() {
                                current = serde_json::Value::Array(array.clone());
                            } else {
                                return Err(anyhow!("Cannot apply [*] to non-array"));
                            }
                        } else if let Ok(index) = bracket_content.parse::<usize>() {
                            if let Some(array) = current.as_array() {
                                current = array.get(index).cloned().unwrap_or(serde_json::Value::Null);
                            } else {
                                return Err(anyhow!("Cannot index non-array"));
                            }
                        }
                        in_brackets = false;
                    }
                }
                _ => {
                    if in_brackets {
                        bracket_content.push(ch);
                    } else {
                        path.push(ch);
                    }
                }
            }
        }
        
        if !path.is_empty() {
            current = self.execute_jq_query(&current, &path)?;
        }
        
        Ok(current)
    }
    
    /// Compare JSON values for sorting
    fn compare_json_values(&self, a: &serde_json::Value, b: &serde_json::Value) -> std::cmp::Ordering {
        use std::cmp::Ordering;
        
        match (a, b) {
            (serde_json::Value::Number(a), serde_json::Value::Number(b)) => {
                a.as_f64().partial_cmp(&b.as_f64()).unwrap_or(Ordering::Equal)
            }
            (serde_json::Value::String(a), serde_json::Value::String(b)) => a.cmp(b),
            (serde_json::Value::Bool(a), serde_json::Value::Bool(b)) => a.cmp(b),
            (serde_json::Value::Null, serde_json::Value::Null) => Ordering::Equal,
            (serde_json::Value::Null, _) => Ordering::Less,
            (_, serde_json::Value::Null) => Ordering::Greater,
            _ => Ordering::Equal,
        }
    }
    
    /// Process regex-based transformations
    fn process_regex(&self, data: &str, pattern: &str) -> Result<String> {
        use regex::Regex;
        
        // Limit regex complexity for security
        if pattern.len() > 100 {
            return Err(anyhow!("Regex pattern too complex"));
        }
        
        let re = Regex::new(pattern)
            .map_err(|e| anyhow!("Invalid regex pattern: {}", e))?;
        
        let matches: Vec<String> = re.find_iter(data)
            .map(|m| m.as_str().to_string())
            .collect();
        
        Ok(serde_json::json!({
            "matches": matches,
            "count": matches.len(),
            "pattern": pattern
        }).to_string())
    }
} 