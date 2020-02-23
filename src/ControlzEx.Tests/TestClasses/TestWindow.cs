namespace ControlzEx.Tests.TestClasses
{
    using System;
    using System.Diagnostics;
    using System.Windows;

    public sealed class TestWindow : Window, IDisposable
    {
        public TestWindow()
            : this(null)
        {
        }

        public TestWindow(object content)
        {
            this.Content = content;

            this.Width = 800;
            this.Height = 600;

            this.ShowActivated = false;
            this.ShowInTaskbar = false;

            if (Debugger.IsAttached == false)
            {
                this.Left = int.MinValue;
                this.Top = int.MinValue;
            }

            this.Show();
        }

        public void Dispose()
        {
            this.Close();
        }
    }
}