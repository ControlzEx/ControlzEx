namespace ControlzEx.Tests.TestClasses
{
    using System.Windows.Threading;

    public static class UITestHelper
    {
        public static void DoEvents()
        {
            var frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background,
                                                     new DispatcherOperationCallback(
                                                         delegate(object f)
                                                         {
                                                             ((DispatcherFrame)f).Continue = false;
                                                             return null;
                                                         }), frame);
            Dispatcher.PushFrame(frame);
        }
    }
}