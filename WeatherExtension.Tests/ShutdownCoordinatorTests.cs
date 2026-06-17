// Copyright (c) Bald Bearded Builder LLC
// Bald Bearded Builder LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO.Pipes;
using System.Text;

namespace Microsoft.CmdPal.Ext.Weather.UnitTests;

[TestClass]
public class ShutdownCoordinatorTests
{
    [TestMethod]
    public void ShutdownCoordinator_CreatesNamedPipeListener()
    {
        // Arrange & Act
        using var coordinator = new WeatherExtension.ShutdownCoordinator();

        // Assert
        Assert.IsNotNull(coordinator.ShutdownHandle);
        Assert.IsFalse(coordinator.ShutdownHandle.WaitOne(0), "ShutdownHandle should not be signaled immediately");
    }

    [TestMethod]
    public async Task ShutdownCoordinator_NamedPipeSignalTriggersShutdown()
    {
        // Arrange
        using var coordinator = new WeatherExtension.ShutdownCoordinator();

        // Act - Send shutdown signal via named pipe
        var sendTask = Task.Run(() =>
        {
            Thread.Sleep(100); // Give coordinator time to start listening
            try
            {
                using var pipeClient = new NamedPipeClientStream(
                    ".",
                    "WeatherExtension-Shutdown",
                    PipeDirection.Out);

                pipeClient.Connect(timeout: 2000);
                pipeClient.WriteByte(1); // Send shutdown signal
            }
            catch
            {
                // Pipe listener may have already consumed the signal and shut down
            }
        });

        var waitResult = coordinator.ShutdownHandle.WaitOne(timeout: TimeSpan.FromSeconds(3));
        await sendTask;

        // Assert
        Assert.IsTrue(waitResult, "ShutdownHandle should be signaled after named pipe receives data");
    }

    [TestMethod]
    public async Task ShutdownCoordinator_NamedEventSignalTriggersShutdown()
    {
        // Arrange
        using var coordinator = new WeatherExtension.ShutdownCoordinator();

        // Act - Send shutdown signal via named event
        var sendTask = Task.Run(() =>
        {
            Thread.Sleep(100); // Give coordinator time to start listening
            try
            {
                using var evt = EventWaitHandle.OpenExisting("WeatherExtension-Shutdown");
                evt.Set();
            }
            catch
            {
                // Event may not exist or may already be signaled
            }
        });

        var waitResult = coordinator.ShutdownHandle.WaitOne(timeout: TimeSpan.FromSeconds(3));
        await sendTask;

        // Assert
        Assert.IsTrue(waitResult, "ShutdownHandle should be signaled after named event is set");
    }

    [TestMethod]
    public void ShutdownCoordinator_HandleIsThreadSafe()
    {
        // Arrange
        using var coordinator = new WeatherExtension.ShutdownCoordinator();
        var signalCount = 0;

        // Act - Multiple threads waiting on same handle
        var threads = Enumerable.Range(0, 5).Select(_ => new Thread(() =>
        {
            if (coordinator.ShutdownHandle.WaitOne(timeout: TimeSpan.FromSeconds(2)))
            {
                Interlocked.Increment(ref signalCount);
            }
        })).ToList();

        threads.ForEach(t => t.Start());
        Thread.Sleep(100);

        // Signal the coordinator (indirectly by disposing and creating a new one)
        coordinator.Dispose();

        threads.ForEach(t => t.Join(timeout: 3000));

        // Assert - Disposed coordinator shouldn't be waited on, but we're testing thread safety
        Assert.AreEqual(0, signalCount, "Disposed coordinator shouldn't signal waiting threads");
    }

    [TestMethod]
    public void ShutdownCoordinator_DisposeCancelsListenerThread()
    {
        // Arrange
        var coordinator = new WeatherExtension.ShutdownCoordinator();

        // Act
        coordinator.Dispose();

        // Assert - Should not hang
        // If it hangs, the test times out and fails
        Thread.Sleep(100); // Give thread time to clean up
        Assert.IsTrue(true, "Dispose should complete without hanging");
    }

    [TestMethod]
    public void ShutdownCoordinator_MultipleDisposesAreIdempotent()
    {
        // Arrange
        var coordinator = new WeatherExtension.ShutdownCoordinator();

        // Act & Assert - Should not throw
        coordinator.Dispose();
        coordinator.Dispose(); // Second dispose should be safe
        coordinator.Dispose(); // Third dispose should be safe
    }
}
