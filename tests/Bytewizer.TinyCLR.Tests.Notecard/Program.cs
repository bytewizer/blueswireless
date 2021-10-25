using System.Diagnostics;

using Bytewizer.TinyCLR.Assertions;

namespace Bytewizer.TinyCLR.Tests.Notecard
{
    class Program
    {
        static void Main()
        {
            var testRunner = new TestRunner();
            testRunner.Run();
            Debug.WriteLine(testRunner.Results());     
        }
    }
}
