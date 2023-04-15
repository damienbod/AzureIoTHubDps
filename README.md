# Device provisioning service

[![.NET](https://github.com/damienbod/AzureIoTHubDps/actions/workflows/dotnet.yml/badge.svg)](https://github.com/damienbod/AzureIoTHubDps/actions/workflows/dotnet.yml)

[Provisioning X.509 Devices for Azure IoT Hub using .NET Core](https://damienbod.com/2020/02/20/provisioning-x-509-devices-for-azure-iot-hub-using-net-core/)

[Provision Azure IoT Hub devices using DPS and X.509 certificates in ASP.NET Core](https://damienbod.com)

## User secrets:

You can find the DPS/IoT Hub Connection Strings in the portal:
- Azure IoT Hub Device Provisioning Service (DPS) | Shared access policies
- IoT Hub | Shared access policies

Note: multiple Azure IoT Hubs can be linked to any DPS. Due to this, the devices using the specific Hubs use the AssignedHub property of the DPS enrollment to connect. This needs to be set in the secrets or key vault.

```json
{
  "ConnectionStrings": {
    "DpsConnection": "--your-connectionstring--",
    "--AssignedHub-1--": "--your-connectionstring--",
    "--AssignedHub-2--": "--your-connectionstring--"
  }
}
```

## Migrations DpsWebManagement

```
Add-Migration "init"

Update-Database
```

## History

2023-04-15 Added support for Azure IoT Hub enable, disable, certificate rotation and multiple linked Azure IoT Hubs

2023-04-10 Add PKI web application for Azure IoT Hub DPS using DPS enrollment groups and certificates

2023-03-27 Updated to .NET 7, fix group and individual enrollments

## Links

https://github.com/Azure/azure-iot-sdk-csharp

https://github.com/damienbod/AspNetCoreCertificates

https://damienbod.com/2020/01/29/creating-certificates-for-x-509-security-in-azure-iot-hub-using-net-core/

https://learn.microsoft.com/en-us/azure/iot-hub/troubleshoot-error-codes

https://stackoverflow.com/questions/52750160/what-is-the-rationale-for-all-the-different-x509keystorageflags/52840537#52840537

https://github.com/dotnet/runtime/issues/19581
