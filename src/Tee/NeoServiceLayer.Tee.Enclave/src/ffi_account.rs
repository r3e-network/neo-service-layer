// Stub account FFI functions for future implementation
use std::ffi::{CStr, CString};
use std::os::raw::{c_char, c_int};

/// Create abstract account (stub)
#[no_mangle]
pub extern "C" fn occlum_account_create(
    _account_id: *const c_char,
    _account_data: *const c_char,
    _result: *mut c_char,
    _result_size: usize,
    _actual_result_size: *mut usize,
) -> c_int {
    0 // Success stub
}

/// Sign transaction (stub)
#[no_mangle]
pub extern "C" fn occlum_account_sign_transaction(
    _account_id: *const c_char,
    _transaction_data: *const c_char,
    _result: *mut c_char,
    _result_size: usize,
    _actual_result_size: *mut usize,
) -> c_int {
    0 // Success stub
}

/// Add guardian (stub)
#[no_mangle]
pub extern "C" fn occlum_account_add_guardian(
    _account_id: *const c_char,
    _guardian_data: *const c_char,
    _result: *mut c_char,
    _result_size: usize,
    _actual_result_size: *mut usize,
) -> c_int {
    0 // Success stub
} 