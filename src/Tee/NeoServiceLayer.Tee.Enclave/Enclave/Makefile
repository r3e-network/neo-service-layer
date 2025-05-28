# Makefile for building the Neo Service Layer enclave with Occlum

# Directories
BUILD_DIR := build
INSTALL_DIR := $(BUILD_DIR)/install
OCCLUM_INSTANCE_DIR := occlum_instance

# Commands
CMAKE := cmake
MAKE := make
OCCLUM := occlum
MKDIR := mkdir -p
RM := rm -rf

# Targets
.PHONY: all clean build install occlum_instance occlum_build

all: build install occlum_instance occlum_build

clean:
	$(RM) $(BUILD_DIR)
	$(RM) $(OCCLUM_INSTANCE_DIR)

build:
	$(MKDIR) $(BUILD_DIR)
	cd $(BUILD_DIR) && $(CMAKE) ..
	cd $(BUILD_DIR) && $(MAKE)

install:
	cd $(BUILD_DIR) && $(MAKE) install DESTDIR=$(INSTALL_DIR)

occlum_instance:
	$(MKDIR) $(OCCLUM_INSTANCE_DIR)
	cd $(OCCLUM_INSTANCE_DIR) && $(OCCLUM) init
	$(MKDIR) $(OCCLUM_INSTANCE_DIR)/image/lib
	$(MKDIR) $(OCCLUM_INSTANCE_DIR)/image/bin
	cp $(INSTALL_DIR)/lib/libneo_service_layer_enclave.so $(OCCLUM_INSTANCE_DIR)/image/lib/
	# Add any additional files needed for the enclave

occlum_build:
	cd $(OCCLUM_INSTANCE_DIR) && $(OCCLUM) build
	cd $(OCCLUM_INSTANCE_DIR) && $(OCCLUM) sign
