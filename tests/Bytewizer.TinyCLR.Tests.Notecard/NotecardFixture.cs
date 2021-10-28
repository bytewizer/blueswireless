using Bytewizer.TinyCLR.Assertions;
using Bytewizer.TinyCLR.Drivers.Blues.Notecard;
using Bytewizer.TinyCLR.Drivers.Blues.Notecard.Diagnostics;

using GHIElectronics.TinyCLR.Pins;
using GHIElectronics.TinyCLR.Devices.I2c;
using GHIElectronics.TinyCLR.Devices.Uart;

namespace Bytewizer.TinyCLR.Tests.Notecard
{
    public abstract class NotecardFixture : TestFixture
    {
        private static bool _initialized;
        protected static NotecardController _notecard;
        protected static NotecardLogger _notecardLogger;

        protected NotecardFixture()
        {
            if (_initialized)
                return;

            //var i2cController = I2cController.FromName(SC20100.I2cBus.I2c1); // Feather
            var i2cController = I2cController.FromName(SC13048.I2cBus.I2c1); // Flea
            _notecard = new NotecardController(i2cController);

            var uartController = UartController.FromName(SC13048.UartPort.Uart4);
            _notecardLogger = new NotecardLogger(uartController, SC13048.GpioPin.PA4);

            _initialized = true;
        }
    }
}