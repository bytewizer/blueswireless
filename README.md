# Blues Wireless for TinyCLR OS

[![NuGet Status](http://img.shields.io/nuget/v/Bytewizer.TinyCLR.Drivers.Blues.Notecard.svg?style=flat&logo=nuget)](https://www.nuget.org/packages?q=bytewizer.tinyclr)
[![Release](https://github.com/bytewizer/blueswireless/actions/workflows/release.yml/badge.svg)](https://github.com/bytewizer/blues/actions/workflows/release.yml)
[![Build](https://github.com/bytewizer/blueswireless/actions/workflows/actions.yml/badge.svg)](https://github.com/bytewizer/blues/actions/workflows/actions.yml)


## Notecarrier-AF with GHI Electronics FEZ Feather
![Notecard](/images/notecard.jpg)

## Requirements

<a href="https://visualstudio.microsoft.com/downloads/">Visual Studio 2019</a> and <a href="https://www.ghielectronics.com/">GHI Electronics TinyCLR OS 2.1</a> or higher.  

## Give a Star! :star:

If you like or are using this project to start your solution, please give it a star. Thanks!

## Getting Started

We encourage users to play with the samples and test programs. See the working [Playground](https://github.com/bytewizer/blueswireless/tree/master/playground/Bytewizer.TinyCLR.Notecard) for an example of how to use the packages. The [Tests](https://github.com/bytewizer/blueswireless/tree/master/tests/Bytewizer.TinyCLR.Tests.Notecard) also includes many working examples.

## Simple Example
```CSharp
class Program
{
    static void Main()
    {
        // Setup I2c bus for Fez Feather
        var controller = I2cController.FromName(SC20100.I2cBus.I2c1);
        var notecard = new NotecardController(controller);

        // Set product id with json request (this only needs to be done once)
        var request1 = new JsonRequest("hub.set");
        request1.Add("product", "[your-product-uid]"); // replace this with your product uid

        var results1 = notecard.Request(request1);
        if (results1.IsSuccess)
        {
            Debug.WriteLine(results1.Response);
        }

        // Create a json body object
        var body = new JsonObject();
        body.Add("temp", 35.5);
        body.Add("humid", 56.23);

        // Set note with json request and included the body message
        var request2 = new JsonRequest("note.add");
        request2.Add("body", body);
        request2.Add("sync", true);

        var results2 = notecard.Request(request2);

        if (results2.IsSuccess)
        {
            Debug.WriteLine(results2.Response);
        }
    }
}
```
## Continuous Integration

**main** :: This is the branch containing the latest release build. No contributions should be made directly to this branch. The development branch will periodically be merged to the main branch, and be released to [NuGet](https://www.nuget.org/packages?q=bytewizer.tinyclr).

**develop** :: This is the development branch to which contributions should be proposed by contributors as pull requests. Development build packages are available as attached artifacts on successful build [workflows](https://github.com/bytewizer/blueswireless/actions/workflows/actions.yml).

## Contributions

Contributions to this project are always welcome. Please consider forking this project on GitHub and sending a pull request to get your improvements added to the original project.

## Disclaimer

All source, documentation, instructions and products of this project are provided as-is without warranty. No liability is accepted for any damages, data loss or costs incurred by its use.