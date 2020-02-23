namespace ControlzEx.Tests
{
    using System.Threading;
    using System.Windows;
    using System.Windows.Threading;
    using NUnit.Framework;

    [SetUpFixture]
    public class AssemblySetup
    {
        [OneTimeSetUp]
        public void Setup()
        {
            SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext());

            new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            Dispatcher.CurrentDispatcher.InvokeShutdown();
        }
    }
}