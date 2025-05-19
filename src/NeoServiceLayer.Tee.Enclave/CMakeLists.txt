cmake_minimum_required(VERSION 3.10)
project(NeoServiceLayerEnclave)

# Set C++ standard
set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# Set build type if not specified
if(NOT CMAKE_BUILD_TYPE)
    set(CMAKE_BUILD_TYPE Release)
endif()

# Set compiler flags
set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -Wall -Wextra")
set(CMAKE_CXX_FLAGS_DEBUG "${CMAKE_CXX_FLAGS_DEBUG} -g -O0")
set(CMAKE_CXX_FLAGS_RELEASE "${CMAKE_CXX_FLAGS_RELEASE} -O2")

# Check if we're in simulation mode
if(DEFINED ENV{OCCLUM_SIMULATION})
    if($ENV{OCCLUM_SIMULATION} STREQUAL "1")
        add_definitions(-DOCCLUM_SIMULATION_MODE)
        message(STATUS "Building in Occlum simulation mode")
    endif()
endif()

# Set include directories
include_directories(
    ${CMAKE_CURRENT_SOURCE_DIR}
    ${CMAKE_CURRENT_SOURCE_DIR}/Enclave
    ${CMAKE_CURRENT_SOURCE_DIR}/Enclave/Core
    ${CMAKE_CURRENT_SOURCE_DIR}/Enclave/Crypto
    ${CMAKE_CURRENT_SOURCE_DIR}/Enclave/JavaScript
    ${CMAKE_CURRENT_SOURCE_DIR}/Enclave/Storage
    ${CMAKE_CURRENT_SOURCE_DIR}/Enclave/Occlum
    ${CMAKE_CURRENT_SOURCE_DIR}/Enclave/QuickJs
)

# Find required packages
find_package(OpenSSL REQUIRED)

# Set library output directory
set(CMAKE_LIBRARY_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/lib)

# Source files
set(ENCLAVE_SOURCES
    # Core files
    Enclave/Core/NeoServiceLayerEnclave.cpp
    Enclave/Core/EnclaveCore.cpp
    Enclave/Core/EnclaveUtils.cpp
    Enclave/Core/EnclaveConfig.cpp
    
    # Occlum integration
    Enclave/Occlum/OcclumEnclave.cpp
    Enclave/Occlum/OcclumIntegration.cpp
    Enclave/Occlum/OcclumUtils.cpp
    
    # JavaScript engine
    Enclave/JavaScript/JavaScriptEngine.cpp
    Enclave/JavaScript/JavaScriptManager.cpp
    Enclave/JavaScript/QuickJs/QuickJsEngineAdapter.cpp
    
    # Crypto
    Enclave/Crypto/CryptoManager.cpp
    Enclave/Crypto/KeyManager.cpp
    Enclave/Crypto/RandomnessProvider.cpp
    
    # Storage
    Enclave/Storage/StorageManager.cpp
    Enclave/Storage/PersistentStorage.cpp
    Enclave/Storage/OcclumFileStorage.cpp
    
    # Secret management
    Enclave/Secrets/SecretManager.cpp
    Enclave/Secrets/SecretStorage.cpp
    
    # Event trigger
    Enclave/Events/EventTrigger.cpp
    Enclave/Events/EventProcessor.cpp
    
    # Gas accounting
    Enclave/Gas/GasAccounting.cpp
    Enclave/Gas/GasAccountingManager.cpp
    
    # Backup
    Enclave/Backup/BackupManager.cpp
)

# Create shared library
add_library(enclave SHARED ${ENCLAVE_SOURCES})

# Link libraries
target_link_libraries(enclave
    OpenSSL::Crypto
    OpenSSL::SSL
    pthread
    dl
)

# Install targets
install(TARGETS enclave
    LIBRARY DESTINATION lib
)

# Create Occlum instance directory
add_custom_target(occlum_instance
    COMMAND mkdir -p ${CMAKE_BINARY_DIR}/occlum_instance
    COMMAND cd ${CMAKE_BINARY_DIR}/occlum_instance && occlum init
    COMMAND mkdir -p ${CMAKE_BINARY_DIR}/occlum_instance/image/lib
    COMMAND cp ${CMAKE_LIBRARY_OUTPUT_DIRECTORY}/libenclave.so ${CMAKE_BINARY_DIR}/occlum_instance/image/lib/
    COMMAND cd ${CMAKE_BINARY_DIR}/occlum_instance && occlum build
    DEPENDS enclave
)

# Add custom target to run the enclave
add_custom_target(run_enclave
    COMMAND cd ${CMAKE_BINARY_DIR}/occlum_instance && occlum run /bin/node /app/enclave_main.js
    DEPENDS occlum_instance
)

# Print configuration
message(STATUS "CMAKE_BUILD_TYPE: ${CMAKE_BUILD_TYPE}")
message(STATUS "CMAKE_CXX_FLAGS: ${CMAKE_CXX_FLAGS}")
message(STATUS "CMAKE_CXX_FLAGS_DEBUG: ${CMAKE_CXX_FLAGS_DEBUG}")
message(STATUS "CMAKE_CXX_FLAGS_RELEASE: ${CMAKE_CXX_FLAGS_RELEASE}")
