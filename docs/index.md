# Neo Service Layer Documentation

## Overview

The Neo Service Layer (NSL) is a secure computing environment that uses Trusted Execution Environments (TEEs) to run JavaScript functions in a confidential manner. The NSL provides features for User Secrets, Event Triggers, GAS Accounting, Provably Fair Randomness, and Compliance Verification.

## Documentation

### Guides

- [User Guide](NeoServiceLayer.UserGuide.md): Instructions on how to use the Neo Service Layer.
- [API Reference](NeoServiceLayer.ApiReference.md): Detailed reference for the Neo Service Layer API.
- [Enclave Documentation](NeoServiceLayer.Tee.Enclave.md): Technical documentation for the enclave component.
- [Troubleshooting Guide](Troubleshooting.md): Solutions for common issues.

### Tutorials

- [Using NSL with Neo Smart Contracts](tutorials/NSL_With_Neo_SmartContracts.md): Learn how to use the Neo Service Layer with Neo N3 smart contracts.
- [Implementing a Provably Fair Game](tutorials/Provably_Fair_Game.md): Create a provably fair game using the randomness service.

### Diagrams

- [NSL Architecture](diagrams/NSL_Architecture.md): Overview of the Neo Service Layer architecture.
- [JavaScript Execution Workflow](diagrams/JavaScript_Execution_Workflow.md): Workflow for executing JavaScript functions.
- [Event Trigger Workflow](diagrams/Event_Trigger_Workflow.md): Workflow for event triggers.
- [Randomness Service Workflow](diagrams/Randomness_Service_Workflow.md): Workflow for the randomness service.
- [Compliance Service Workflow](diagrams/Compliance_Service_Workflow.md): Workflow for the compliance service.

## Features

### JavaScript Execution

The NSL allows you to execute JavaScript functions securely within a TEE. This provides confidentiality and integrity for your code and data.

### User Secrets

The NSL provides a secure way to store and retrieve user secrets. These secrets are encrypted and can only be accessed by the enclave.

### Event Triggers

The NSL allows you to register event triggers that execute JavaScript functions in response to specific events, such as blockchain events or scheduled events.

### Provably Fair Randomness

The NSL provides a provably fair randomness service that allows you to generate random numbers that can be verified by external parties.

### Compliance Verification

The NSL provides a compliance verification service that allows you to verify JavaScript code for compliance with regulatory requirements.

### GAS Accounting

The NSL tracks the computational resources used by JavaScript functions using a gas accounting system.

## Getting Started

To get started with the Neo Service Layer, see the [User Guide](NeoServiceLayer.UserGuide.md).
