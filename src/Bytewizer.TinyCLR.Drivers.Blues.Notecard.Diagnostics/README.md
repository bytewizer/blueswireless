# Bytewizer.TinyCLR.Drivers.Blues.Notecard.Diagnostics

  A TinyCLR library for debug tracing with the [Blues Wireless Notecard](https://blues.io/) over serial. This library works on any [GHI Electronics SITCore Microcontroller](https://www.ghielectronics.com), and can be installed from the Nuget Package Manager.  This driver is not intented to be used in production applications.

## Example Usage

```CSharp
static void Main()
{
    // Setup Uart and enable pin based on the chipset used
    var uartController = UartController.FromName(SC13048.UartPort.Uart4);
    _notecardLogger = new NotecardLogger(uartController, SC13048.GpioPin.PA4);
    _notecardLogger.MessageAvailable += NotecardLogger_DataReceived;

    _notecardLogger.TraceOn();
    _notecard.Request(new JsonRequest("hub.sync"));
    _notecardLogger.TraceOff();
}

private void NotecardLogger_DataReceived(string message)
{
    Debug.WriteLine(message);
}
```

## TinyCLR Packages
Install release package from [NuGet](https://www.nuget.org/packages?q=bytewizer.tinyclr.drivers.blues) or using the Package Manager Console :
```powershell
PM> Install-Package Bytewizer.TinyCLR.Drivers.Blues.Notecard.Diagnostics
```