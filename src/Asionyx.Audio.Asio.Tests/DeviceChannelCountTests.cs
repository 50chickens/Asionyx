using System;
using System.Threading;
using NAudio.Wave;
using NUnit.Framework;

namespace Asionyx.Audio.Asio.Tests
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    [Category("Integration")]
    public class DeviceChannelCountTests
    {
        [Test]
        public void AsioDevices_HaveAtLeastTwoInputAndOutputChannels()
        {
            var drivers = AsioOut.GetDriverNames();
            if (drivers == null || drivers.Length == 0)
                Assert.Inconclusive("No ASIO drivers available on this machine.");

            foreach (var d in drivers)
            {
                try
                {
                    using var dev = new AsioOut(d);
                    Assert.That(dev.DriverInputChannelCount, Is.GreaterThanOrEqualTo(2), $"Driver '{d}' has fewer than 2 input channels");
                    Assert.That(dev.DriverOutputChannelCount, Is.GreaterThanOrEqualTo(2), $"Driver '{d}' has fewer than 2 output channels");
                    TestContext.Progress.WriteLine($"{dev.DriverName} has {dev.DriverInputChannelCount} input channels.");
                    TestContext.Progress.WriteLine($"{dev.DriverName} has {dev.DriverOutputChannelCount} output channels.");
                }
                catch (Exception ex)
                {
                    Assert.Fail($"Failed to open ASIO driver '{d}': {ex.Message}");
                }
            }
        }
    }
}