# CMakeLists.txt for NeoServiceLayer.Tee.Enclave
cmake_minimum_required(VERSION 3.10)
project(NeoServiceLayerEnclave)

# Set C++ standard
set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# Find required packages
find_package(nlohmann_json REQUIRED)
find_package(mbedtls REQUIRED)

# Set include directories
include_directories(
    ${CMAKE_CURRENT_SOURCE_DIR}/Enclave
    ${CMAKE_CURRENT_SOURCE_DIR}/Enclave/QuickJs
)

# Set source files
set(ENCLAVE_SOURCES
    # Core files
    Enclave/NeoServiceLayerEnclave.cpp
    Enclave/NeoServiceLayerEnclave2.cpp
    # Integration files
    Enclave/OcclumIntegration.Core.cpp
    Enclave/OcclumIntegration.Crypto.cpp
    Enclave/OcclumIntegration.JavaScriptEngine.cpp
    Enclave/OcclumIntegration.Sealing.cpp
    Enclave/OcclumIntegration.Utils.cpp
    Enclave/OcclumIntegration.Attestation.cpp
    # Manager files
    Enclave/StorageManager.cpp
    Enclave/KeyManager.Core.cpp
    Enclave/KeyManager.Crypto.cpp
    Enclave/KeyManager.Storage.cpp
    Enclave/SecretManager.Core.cpp
    Enclave/SecretManager.Storage.cpp
    Enclave/SecretManager.Crypto.cpp
    Enclave/SecretManager.Access.cpp
    Enclave/SecretManager.Access2.cpp
    Enclave/GasAccounting.cpp
    Enclave/GasAccountingManager.cpp
    # JavaScript engine
    Enclave/JavaScriptEngine.cpp
    Enclave/JavaScriptManager.cpp
    Enclave/QuickJs/QuickJsEngineAdapter.cpp
    # Event trigger
    Enclave/EventTrigger.Core.cpp
    Enclave/EventTrigger.Registration.cpp
    Enclave/EventTrigger.Processing.cpp
    Enclave/EventTrigger.Processing2.cpp
    Enclave/EventTrigger.Storage.cpp
    # Backup manager
    Enclave/BackupManager.Core.cpp
    Enclave/BackupManager.Backup.cpp
    Enclave/BackupManager.Restore.cpp
    Enclave/BackupManager.Scheduling.cpp
    Enclave/BackupManager.Utils.cpp
    # Utility files
    Enclave/EnclaveUtils.cpp
)

# Set compile definitions
add_compile_definitions(OCCLUM)
if(CMAKE_BUILD_TYPE STREQUAL "Debug")
    add_compile_definitions(_DEBUG)
endif()

# Create shared library
add_library(enclave SHARED ${ENCLAVE_SOURCES})

# Link libraries
target_link_libraries(enclave
    mbedtls
    mbedcrypto
    mbedx509
)

# Set output directory
set_target_properties(enclave PROPERTIES
    LIBRARY_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/lib
)

# Install targets
install(TARGETS enclave
    LIBRARY DESTINATION lib
)

# Custom target to create Occlum instance
add_custom_target(occlum_instance
    COMMAND ${CMAKE_COMMAND} -E make_directory ${CMAKE_BINARY_DIR}/occlum_instance
    COMMAND cd ${CMAKE_BINARY_DIR} && occlum new occlum_instance
    COMMAND ${CMAKE_COMMAND} -E copy ${CMAKE_BINARY_DIR}/lib/libenclave.so ${CMAKE_BINARY_DIR}/occlum_instance/image/lib/
    COMMAND ${CMAKE_COMMAND} -E copy /usr/bin/node ${CMAKE_BINARY_DIR}/occlum_instance/image/bin/
    COMMAND ${CMAKE_COMMAND} -E make_directory ${CMAKE_BINARY_DIR}/occlum_instance/image/app
    COMMAND echo "const { NeoServiceEnclave } = require('./libenclave'); NeoServiceEnclave.initialize(); NeoServiceEnclave.startServer();" > ${CMAKE_BINARY_DIR}/occlum_instance/image/app/enclave_main.js
    COMMAND cd ${CMAKE_BINARY_DIR}/occlum_instance && occlum build
    DEPENDS enclave
)

# Custom target to run Occlum instance
add_custom_target(run_occlum
    COMMAND cd ${CMAKE_BINARY_DIR}/occlum_instance && occlum run /bin/node /app/enclave_main.js
    DEPENDS occlum_instance
)

# Add all targets to the build-tree export set
export(TARGETS enclave FILE "${PROJECT_BINARY_DIR}/NeoServiceLayerEnclaveTargets.cmake")

# Export the package for use from the build-tree
export(PACKAGE NeoServiceLayerEnclave)
