namespace ControlzEx.Showcase
{
    using System.Windows.Input;
#pragma warning disable 618
    using SystemCommands = Microsoft.Windows.Shell.SystemCommands;
#pragma warning restore 618

    public partial class MainWindow
    {
        public MainWindow()
        {
            this.InitializeComponent();
        }

        private void TitleBarGrid_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void TitleBarGrid_OnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
#pragma warning disable 618
            SystemCommands.ShowSystemMenu(this, e);
#pragma warning restore 618
        }
    }
}