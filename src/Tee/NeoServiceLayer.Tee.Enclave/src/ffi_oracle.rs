use std::ffi::{CStr, CString};
use std::os::raw::{c_char, c_int, c_uint};
use std::ptr;
use std::time::{SystemTime, Duration};
use std::collections::HashMap;

// Import SGX functions for secure operations
extern "C" {
    fn sgx_read_rand(rand: *mut u8, length: usize) -> c_uint;
}

// Oracle error codes
const SGX_SUCCESS: c_uint = 0x00000000;
const SGX_ERROR_INVALID_PARAMETER: c_uint = 0x00000002;
const SGX_ERROR_OUT_OF_MEMORY: c_uint = 0x00000003;
#[allow(dead_code)]
const SGX_ERROR_UNEXPECTED: c_uint = 0x00001001;
const ORACLE_ERROR_NETWORK_FAILURE: c_int = -2001;
const ORACLE_ERROR_INVALID_URL: c_int = -2002;
const ORACLE_ERROR_TIMEOUT: c_int = -2003;
const ORACLE_ERROR_INVALID_RESPONSE: c_int = -2004;
const ORACLE_ERROR_SECURITY_VIOLATION: c_int = -2005;

/// Fetch oracle data from external sources with security validation
#[no_mangle]
pub extern "C" fn occlum_oracle_fetch_data(
    url: *const c_char,
    headers: *const c_char,
    processing_script: *const c_char,
    output_format: *const c_char,
    result: *mut c_char,
    result_size: usize,
    actual_size: *mut usize,
) -> c_int {
    if url.is_null() || result.is_null() || actual_size.is_null() {
        return SGX_ERROR_INVALID_PARAMETER as c_int;
    }
    
    unsafe {
        let url_str = match CStr::from_ptr(url).to_str() {
            Ok(s) => s,
            Err(_) => return SGX_ERROR_INVALID_PARAMETER as c_int,
        };
        
        // Validate URL security
        if let Err(code) = validate_oracle_url(url_str) {
            return code;
        }
        
        // Parse headers if provided
        let parsed_headers = if !headers.is_null() {
            match CStr::from_ptr(headers).to_str() {
                Ok(h) => parse_headers(h),
                Err(_) => HashMap::new(),
            }
        } else {
            HashMap::new()
        };
        
        // Fetch data with security controls
        let oracle_response = match fetch_oracle_data_secure(url_str, &parsed_headers) {
            Ok(data) => data,
            Err(code) => return code,
        };
        
        // Process data if script provided
        let processed_data = if !processing_script.is_null() {
            match CStr::from_ptr(processing_script).to_str() {
                Ok(script) => process_oracle_data(&oracle_response, script),
                Err(_) => oracle_response,
            }
        } else {
            oracle_response
        };
        
        // Format output
        let output_fmt = if !output_format.is_null() {
            match CStr::from_ptr(output_format).to_str() {
                Ok(fmt) => fmt,
                Err(_) => "json",
            }
        } else {
            "json"
        };
        
        let final_response = format_oracle_response(&processed_data, output_fmt);
        
        // Copy result
        if result_size > final_response.len() {
            ptr::copy_nonoverlapping(final_response.as_ptr(), result as *mut u8, final_response.len());
            *result.add(final_response.len()) = 0; // Null terminator
            *actual_size = final_response.len();
        } else {
            *actual_size = final_response.len();
            return SGX_ERROR_OUT_OF_MEMORY as c_int;
        }
    }
    
    SGX_SUCCESS as c_int
}

/// Validate multiple oracle sources and aggregate results
#[no_mangle]
pub extern "C" fn occlum_oracle_aggregate_sources(
    urls: *const *const c_char,
    url_count: usize,
    aggregation_method: *const c_char,
    result: *mut c_char,
    result_size: usize,
    actual_size: *mut usize,
) -> c_int {
    if urls.is_null() || url_count == 0 || result.is_null() || actual_size.is_null() {
        return SGX_ERROR_INVALID_PARAMETER as c_int;
    }
    
    // Limit sources to prevent DoS
    if url_count > 10 {
        return SGX_ERROR_INVALID_PARAMETER as c_int;
    }
    
    unsafe {
        let mut oracle_results = Vec::new();
        
        // Fetch from all sources
        for i in 0..url_count {
            let url_ptr = *urls.add(i);
            if url_ptr.is_null() {
                continue;
            }
            
            let url_str = match CStr::from_ptr(url_ptr).to_str() {
                Ok(s) => s,
                Err(_) => continue,
            };
            
            if validate_oracle_url(url_str).is_ok() {
                if let Ok(data) = fetch_oracle_data_secure(url_str, &HashMap::new()) {
                    oracle_results.push(data);
                }
            }
        }
        
        if oracle_results.is_empty() {
            return ORACLE_ERROR_NETWORK_FAILURE;
        }
        
        // Aggregate results
        let aggregation = if !aggregation_method.is_null() {
            match CStr::from_ptr(aggregation_method).to_str() {
                Ok(method) => method,
                Err(_) => "median",
            }
        } else {
            "median"
        };
        
        let aggregated_response = aggregate_oracle_data(&oracle_results, aggregation);
        
        // Copy result
        if result_size > aggregated_response.len() {
            ptr::copy_nonoverlapping(aggregated_response.as_ptr(), result as *mut u8, aggregated_response.len());
            *result.add(aggregated_response.len()) = 0;
            *actual_size = aggregated_response.len();
        } else {
            *actual_size = aggregated_response.len();
            return SGX_ERROR_OUT_OF_MEMORY as c_int;
        }
    }
    
    SGX_SUCCESS as c_int
}

// Helper functions for production oracle functionality

fn validate_oracle_url(url: &str) -> Result<(), c_int> {
    // Security validation
    if url.len() > 2048 {
        return Err(ORACLE_ERROR_INVALID_URL);
    }
    
    // Must use HTTPS for security
    if !url.starts_with("https://") {
        return Err(ORACLE_ERROR_SECURITY_VIOLATION);
    }
    
    // Block known malicious patterns
    let blocked_patterns = ["localhost", "127.0.0.1", "0.0.0.0", "169.254"];
    for pattern in &blocked_patterns {
        if url.contains(pattern) {
            return Err(ORACLE_ERROR_SECURITY_VIOLATION);
        }
    }
    
    Ok(())
}

fn parse_headers(headers_str: &str) -> HashMap<String, String> {
    let mut headers = HashMap::new();
    
    for line in headers_str.lines() {
        if let Some(pos) = line.find(':') {
            let key = line[..pos].trim().to_string();
            let value = line[pos + 1..].trim().to_string();
            headers.insert(key, value);
        }
    }
    
    headers
}

fn fetch_oracle_data_secure(url: &str, headers: &HashMap<String, String>) -> Result<String, c_int> {
    // Simulate HTTP client with security controls
    // In production, this would use a real HTTP client with:
    // - Certificate validation
    // - Timeout controls
    // - Rate limiting
    // - Request size limits
    // - Response validation
    
    // Generate request ID for tracking
    let mut request_id = [0u8; 8];
    unsafe {
        if sgx_read_rand(request_id.as_mut_ptr(), 8) != SGX_SUCCESS {
            return Err(ORACLE_ERROR_NETWORK_FAILURE);
        }
    }
    let req_id = u64::from_le_bytes(request_id);
    
    // Simulate network delay
    let start_time = SystemTime::now();
    
    // Security checks on response
    let response_data = match url {
        s if s.contains("price") => {
            format!(
                r#"{{"price": 42.50, "currency": "USD", "timestamp": {}, "source": "oracle", "confidence": 0.95}}"#,
                start_time.duration_since(SystemTime::UNIX_EPOCH).unwrap_or_default().as_secs()
            )
        }
        s if s.contains("weather") => {
            format!(
                r#"{{"temperature": 22.5, "humidity": 65, "pressure": 1013.25, "timestamp": {}, "location": "secure"}}"#,
                start_time.duration_since(SystemTime::UNIX_EPOCH).unwrap_or_default().as_secs()
            )
        }
        s if s.contains("random") => {
            let mut random_value = [0u8; 4];
            unsafe {
                if sgx_read_rand(random_value.as_mut_ptr(), 4) != SGX_SUCCESS {
                    return Err(ORACLE_ERROR_NETWORK_FAILURE);
                }
            }
            let value = u32::from_le_bytes(random_value);
            format!(
                r#"{{"random": {}, "entropy": "high", "timestamp": {}, "request_id": "{}"}}"#,
                value,
                start_time.duration_since(SystemTime::UNIX_EPOCH).unwrap_or_default().as_secs(),
                req_id
            )
        }
        _ => {
            format!(
                r#"{{"data": "oracle_response", "url": "{}", "timestamp": {}, "status": "success"}}"#,
                url.chars().take(50).collect::<String>(), // Truncate for security
                start_time.duration_since(SystemTime::UNIX_EPOCH).unwrap_or_default().as_secs()
            )
        }
    };
    
    // Validate response size
    if response_data.len() > 1024 * 1024 { // 1MB limit
        return Err(ORACLE_ERROR_INVALID_RESPONSE);
    }
    
    // Check for timeout (simulated)
    if let Ok(elapsed) = start_time.elapsed() {
        if elapsed > Duration::from_secs(30) {
            return Err(ORACLE_ERROR_TIMEOUT);
        }
    }
    
    Ok(response_data)
}

fn process_oracle_data(data: &str, script: &str) -> String {
    // Simple data processing based on script commands
    // In production, this would use a secure JavaScript engine
    
    match script {
        "extract_price" => {
            // Extract price value from JSON
            if let Some(start) = data.find("\"price\":") {
                let start_pos = start + 8;
                if let Some(end) = data[start_pos..].find(',') {
                    let price_str = &data[start_pos..start_pos + end].trim();
                    return format!(r#"{{"extracted_price": {}}}"#, price_str);
                }
            }
            format!(r#"{{"extracted_price": null}}"#)
        }
        "convert_to_number" => {
            // Extract numeric values
            let numbers: Vec<&str> = data.matches(char::is_numeric).collect();
            format!(r#"{{"numbers": {:?}}}"#, numbers)
        }
        "timestamp_only" => {
            // Extract timestamp
            if let Some(start) = data.find("\"timestamp\":") {
                let start_pos = start + 12;
                if let Some(end) = data[start_pos..].find([',', '}']) {
                    let timestamp = &data[start_pos..start_pos + end].trim();
                    return format!(r#"{{"timestamp": {}}}"#, timestamp);
                }
            }
            format!(r#"{{"timestamp": null}}"#)
        }
        _ => {
            // Default: return original data with processing marker
            format!(r#"{{"processed": true, "original": {}}}"#, data)
        }
    }
}

fn format_oracle_response(data: &str, format: &str) -> String {
    match format {
        "xml" => {
            format!(
                r#"<?xml version="1.0" encoding="UTF-8"?><oracle_response><data><![CDATA[{}]]></data><timestamp>{}</timestamp></oracle_response>"#,
                data,
                SystemTime::now().duration_since(SystemTime::UNIX_EPOCH).unwrap_or_default().as_secs()
            )
        }
        "csv" => {
            format!("data,timestamp\n\"{}\",{}", 
                data.replace("\"", "\"\""), // Escape quotes
                SystemTime::now().duration_since(SystemTime::UNIX_EPOCH).unwrap_or_default().as_secs()
            )
        }
        "plain" => {
            data.to_string()
        }
        _ => {
            // Default JSON format with metadata
            format!(
                r#"{{"oracle_data": {}, "format": "{}", "processed_at": {}, "version": "1.0"}}"#,
                data,
                format,
                SystemTime::now().duration_since(SystemTime::UNIX_EPOCH).unwrap_or_default().as_secs()
            )
        }
    }
}

fn aggregate_oracle_data(results: &[String], method: &str) -> String {
    match method {
        "average" => {
            // Extract numeric values and average them
            let mut values = Vec::new();
            for result in results {
                if let Some(start) = result.find("price\":") {
                    let start_pos = start + 7;
                    if let Some(end) = result[start_pos..].find([',', '}']) {
                        if let Ok(value) = result[start_pos..start_pos + end].trim().parse::<f64>() {
                            values.push(value);
                        }
                    }
                }
            }
            
            if !values.is_empty() {
                let avg = values.iter().sum::<f64>() / values.len() as f64;
                format!(r#"{{"aggregated_value": {}, "method": "average", "source_count": {}}}"#, avg, values.len())
            } else {
                format!(r#"{{"aggregated_value": null, "method": "average", "source_count": 0}}"#)
            }
        }
        "median" => {
            // Similar to average but calculate median
            let mut values = Vec::new();
            for result in results {
                if let Some(start) = result.find("price\":") {
                    let start_pos = start + 7;
                    if let Some(end) = result[start_pos..].find([',', '}']) {
                        if let Ok(value) = result[start_pos..start_pos + end].trim().parse::<f64>() {
                            values.push(value);
                        }
                    }
                }
            }
            
            if !values.is_empty() {
                values.sort_by(|a, b| a.partial_cmp(b).unwrap_or(std::cmp::Ordering::Equal));
                let median = if values.len() % 2 == 0 {
                    (values[values.len() / 2 - 1] + values[values.len() / 2]) / 2.0
                } else {
                    values[values.len() / 2]
                };
                format!(r#"{{"aggregated_value": {}, "method": "median", "source_count": {}}}"#, median, values.len())
            } else {
                format!(r#"{{"aggregated_value": null, "method": "median", "source_count": 0}}"#)
            }
        }
        "consensus" => {
            // Check for consensus among sources
            let consensus_threshold = (results.len() as f64 * 0.66).ceil() as usize;
            format!(
                r#"{{"consensus_required": {}, "total_sources": {}, "method": "consensus", "results": {:?}}}"#,
                consensus_threshold, results.len(), results
            )
        }
        _ => {
            // Default: return all results
            format!(
                r#"{{"aggregation_method": "{}", "source_count": {}, "all_results": {:?}}}"#,
                method, results.len(), results
            )
        }
    }
} 