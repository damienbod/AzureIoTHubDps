# Device provisioning service

[![.NET](https://github.com/damienbod/AzureIoTHubDps/actions/workflows/dotnet.yml/badge.svg)](https://github.com/damienbod/AzureIoTHubDps/actions/workflows/dotnet.yml)

[Provisioning X.509 Devices for Azure IoT Hub using .NET Core](https://damienbod.com/2020/02/20/provisioning-x-509-devices-for-azure-iot-hub-using-net-core/)

## User secrets:

You can find the DPS/IoT Hub Connection Strings in the portal:
- Azure IoT Hub Device Provisioning Service (DPS) | Shared access policies
- IoT Hub | Shared access policies

```json
{
  "ConnectionStrings": {
    "DpsConnection": "--your-connectionstring--",
    "IoTHubConnection": "--your-connectionstring--"
  }
}
```

## Migrations DpsWebManagement

```
Add-Migration "init"

Update-Database
```

## History

2023-03-27 Updated to .NET 7, fix group and individual enrollments

## Links

https://github.com/Azure/azure-iot-sdk-csharp

https://github.com/damienbod/AspNetCoreCertificates

https://damienbod.com/2020/01/29/creating-certificates-for-x-509-security-in-azure-iot-hub-using-net-core/

https://learn.microsoft.com/en-us/azure/iot-hub/troubleshoot-error-codes

https://stackoverflow.com/questions/52750160/what-is-the-rationale-for-all-the-different-x509keystorageflags/52840537#52840537

https://github.com/dotnet/runtime/issues/19581
