using Bytewizer.TinyCLR.Assertions;
using Bytewizer.TinyCLR.Drivers.Blues.Notecard;

using GHIElectronics.TinyCLR.Pins;
using GHIElectronics.TinyCLR.Devices.I2c;

namespace Bytewizer.TinyCLR.Tests.Notecard
{
    public abstract class NotecardFixture : TestFixture
    {
        private static bool _initialized;
        protected static NotecardController _notecard;

        protected NotecardFixture()
        {
            if (_initialized)
                return;

            // setup I2c bus for Fez Feather
            var controller = I2cController.FromName(SC20100.I2cBus.I2c1);
            _notecard = new NotecardController(controller);
            
            _initialized = true;
        }
    }
}