using GHIElectronics.TinyCLR.Pins;
using GHIElectronics.TinyCLR.Devices.I2c;

using Bytewizer.TinyCLR.Drivers.Blues.Notecard;

namespace Bytewizer.TinyCLR.Notecard.Terminal
{
    public class NotecardProvider
    {
        private static bool _initialized;
        private static readonly object _lock = new object();

        public static NotecardController Controller { get; private set; }

        public static void Initialize()
        {
            if (_initialized)
                return;

            lock (_lock)
            {
                if (_initialized)
                    return;

                var i2cController = I2cController.FromName(SC20100.I2cBus.I2c1);
                Controller = new NotecardController(i2cController);

                _initialized = true;
            }
        }
    }
}