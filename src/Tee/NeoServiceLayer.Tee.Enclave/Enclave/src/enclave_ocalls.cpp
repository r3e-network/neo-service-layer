/*
 * enclave_ocalls.cpp - Untrusted Application OCALLs Implementation
 * 
 * This file implements the untrusted functions (OCALLs) that the enclave can call
 * from within the trusted environment.
 */

#include "NeoServiceEnclave_u.h"  // Generated from EDL file
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <time.h>
#include <sys/time.h>
#include <curl/curl.h>
#include <fstream>
#include <iostream>
#include <memory>

/*
 * Logging and debugging OCALLs
 */
void ocall_print(const char* message) {
    if (message != nullptr) {
        printf("[ENCLAVE] %s\n", message);
        fflush(stdout);
    }
}

void ocall_print_hex(const unsigned char* data, size_t len) {
    if (data != nullptr && len > 0) {
        printf("[ENCLAVE HEX] ");
        for (size_t i = 0; i < len; i++) {
            printf("%02x", data[i]);
            if (i < len - 1 && (i + 1) % 16 == 0) {
                printf("\n              ");
            }
        }
        printf("\n");
        fflush(stdout);
    }
}

/*
 * HTTP request structure for curl callback
 */
struct HttpResponse {
    char* data;
    size_t size;
};

static size_t WriteCallback(void* contents, size_t size, size_t nmemb, void* userp) {
    size_t realsize = size * nmemb;
    struct HttpResponse* response = (struct HttpResponse*)userp;

    char* ptr = (char*)realloc(response->data, response->size + realsize + 1);
    if (ptr == nullptr) {
        return 0; // Out of memory
    }

    response->data = ptr;
    memcpy(&(response->data[response->size]), contents, realsize);
    response->size += realsize;
    response->data[response->size] = '\0';

    return realsize;
}

/*
 * Network operations OCALLs
 */
sgx_status_t ocall_http_request(const char* url, const char* headers,
                               char* response, size_t response_size,
                               size_t* actual_response_size) {
    if (url == nullptr || response == nullptr || actual_response_size == nullptr) {
        return SGX_ERROR_INVALID_PARAMETER;
    }

    CURL* curl;
    CURLcode res;
    struct HttpResponse http_response = {0};

    curl = curl_easy_init();
    if (!curl) {
        return SGX_ERROR_UNEXPECTED;
    }

    try {
        // Set URL
        curl_easy_setopt(curl, CURLOPT_URL, url);
        
        // Set callback function to write response
        curl_easy_setopt(curl, CURLOPT_WRITEFUNCTION, WriteCallback);
        curl_easy_setopt(curl, CURLOPT_WRITEDATA, &http_response);
        
        // Set timeout
        curl_easy_setopt(curl, CURLOPT_TIMEOUT, 30L);
        
        // Set headers if provided
        struct curl_slist* header_list = nullptr;
        if (headers != nullptr && strlen(headers) > 0) {
            header_list = curl_slist_append(header_list, headers);
            curl_easy_setopt(curl, CURLOPT_HTTPHEADER, header_list);
        }
        
        // Perform the request
        res = curl_easy_perform(curl);
        
        if (res == CURLE_OK && http_response.data != nullptr) {
            *actual_response_size = http_response.size;
            
            if (response_size >= http_response.size + 1) {
                memcpy(response, http_response.data, http_response.size);
                response[http_response.size] = '\0';
            } else {
                // Buffer too small
                curl_easy_cleanup(curl);
                if (header_list) curl_slist_free_all(header_list);
                if (http_response.data) free(http_response.data);
                return SGX_ERROR_INVALID_PARAMETER;
            }
        } else {
            *actual_response_size = 0;
        }
        
        // Cleanup
        curl_easy_cleanup(curl);
        if (header_list) curl_slist_free_all(header_list);
        if (http_response.data) free(http_response.data);
        
        return (res == CURLE_OK) ? SGX_SUCCESS : SGX_ERROR_UNEXPECTED;
        
    } catch (...) {
        curl_easy_cleanup(curl);
        if (http_response.data) free(http_response.data);
        return SGX_ERROR_UNEXPECTED;
    }
}

/*
 * File I/O OCALLs
 */
sgx_status_t ocall_write_file(const char* filename, const unsigned char* data, size_t data_size) {
    if (filename == nullptr || data == nullptr || data_size == 0) {
        return SGX_ERROR_INVALID_PARAMETER;
    }

    try {
        std::ofstream file(filename, std::ios::binary);
        if (!file.is_open()) {
            return SGX_ERROR_UNEXPECTED;
        }

        file.write(reinterpret_cast<const char*>(data), data_size);
        file.close();

        return file.good() ? SGX_SUCCESS : SGX_ERROR_UNEXPECTED;
    } catch (...) {
        return SGX_ERROR_UNEXPECTED;
    }
}

sgx_status_t ocall_read_file(const char* filename, unsigned char* buffer, 
                            size_t buffer_size, size_t* actual_size) {
    if (filename == nullptr || buffer == nullptr || actual_size == nullptr) {
        return SGX_ERROR_INVALID_PARAMETER;
    }

    try {
        std::ifstream file(filename, std::ios::binary | std::ios::ate);
        if (!file.is_open()) {
            *actual_size = 0;
            return SGX_ERROR_UNEXPECTED;
        }

        std::streamsize file_size = file.tellg();
        file.seekg(0, std::ios::beg);

        *actual_size = static_cast<size_t>(file_size);

        if (buffer_size < static_cast<size_t>(file_size)) {
            file.close();
            return SGX_ERROR_INVALID_PARAMETER; // Buffer too small
        }

        file.read(reinterpret_cast<char*>(buffer), file_size);
        file.close();

        return file.good() ? SGX_SUCCESS : SGX_ERROR_UNEXPECTED;
    } catch (...) {
        *actual_size = 0;
        return SGX_ERROR_UNEXPECTED;
    }
}

sgx_status_t ocall_delete_file(const char* filename) {
    if (filename == nullptr) {
        return SGX_ERROR_INVALID_PARAMETER;
    }

    try {
        if (remove(filename) == 0) {
            return SGX_SUCCESS;
        } else {
            return SGX_ERROR_UNEXPECTED;
        }
    } catch (...) {
        return SGX_ERROR_UNEXPECTED;
    }
}

/*
 * Time operations OCALLs
 */
uint64_t ocall_get_system_time(void) {
    struct timeval tv;
    if (gettimeofday(&tv, nullptr) == 0) {
        return static_cast<uint64_t>(tv.tv_sec) * 1000 + static_cast<uint64_t>(tv.tv_usec) / 1000;
    }
    return 0;
} 