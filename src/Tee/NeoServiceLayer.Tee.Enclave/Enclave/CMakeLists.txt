cmake_minimum_required(VERSION 3.10)
project(NeoServiceLayerEnclave)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# Add include directories
include_directories(include)

# Add source files
set(SOURCES
    src/enclave_interface.cpp
)

# Create shared library
add_library(neo_service_layer_enclave SHARED ${SOURCES})

# Set output directory
set_target_properties(neo_service_layer_enclave PROPERTIES
    LIBRARY_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/lib"
)

# Install targets
install(TARGETS neo_service_layer_enclave
    LIBRARY DESTINATION lib
)

# Install headers
install(FILES include/enclave_interface.h
    DESTINATION include
)
