# Bytewizer.TinyCLR.Drivers.Blues.Notecard

  A TinyCLR library for communicating with the [Blues Wireless Notecard](https://blues.io/) over I²C. This library works on any [GHI Electronics SITCore Microcontroller](https://www.ghielectronics.com), and can be installed from the Nuget Package Manager.

## Example Usage

```CSharp
static void Main()
{
    // Setup I2c bus based on the chipset used
    var controller = I2cController.FromName(SC20100.I2cBus.I2c1);
    var notecard = new NotecardController(controller);

    // Create a json message body and include with note
    var body = new JsonObject();
    body.Add("manufacture", "GHI Electronics");
    body.Add("device", "SC20100");

    request = new JsonRequest("note.add");
    request.Add("body", body);
    request.Add("sync", true);

    var results = notecard.Request(request);
    if (!results.IsSuccess)
    {
        throw new Exception("Faild to add note");
    }
    else
    {
        Debug.WriteLine(results.Response);
    }
    
    Debug.WriteLine(notecard.Request(request).Response);
}
```

## TinyCLR Packages
Install release package from [NuGet](https://www.nuget.org/packages?q=bytewizer.tinyclr.drivers.blues) or using the Package Manager Console :
```powershell
PM> Install-Package Bytewizer.TinyCLR.Drivers.Blues.Notecard
```