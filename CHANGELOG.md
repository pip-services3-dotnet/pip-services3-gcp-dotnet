# <img src="https://uploads-ssl.webflow.com/5ea5d3315186cf5ec60c3ee4/5edf1c94ce4c859f2b188094_logo.svg" alt="Pip.Services Logo" width="200"> <br/> Remote Procedure Calls for Pip.Services in .NET Changelog

## <a name="3.2.0"></a> 3.2.0 (2025-12-24)

### Breaking Changes
* Migrate to .NET 10.0

## <a name="3.1.0"></a> 3.1.0 (2023-03-01)

### Breaking changes
* Renamed descriptors for services:
    - "\*:service:gcp-function\*:1.0" -> "\*:service:cloudfunc\*:1.0"
    - "\*:service:commandable-gcp-function\*:1.0" -> "\*:service:commandable-cloudfunc\*:1.0"

## <a name="3.0.1"></a> 3.0.1 (2022-11-10)

### Bug fixes
- Fixed error responses from container

## <a name="3.0.0"></a> 3.0.0 (2022-08-31)

### Features
- **clients** - client components for working with Google Cloud Platform
- **connect** - components of installation and connection settings
- **container** - components for creating containers for Google server-side functions
- **services** - contains interfaces and classes used to create Google services

