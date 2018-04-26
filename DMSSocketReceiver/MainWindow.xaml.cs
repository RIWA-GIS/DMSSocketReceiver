using DMSSocketReceiver.dmshandler;
using DMSSocketReceiver.network.tcp;
using DMSSocketReceiver.Utils;
using System;
using System.Windows;

namespace DMSSocketReceiver
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private AsynchronousTCPListener listener;
        private LogWriter writer;

        public MainWindow()
        {
            InitializeComponent();
            btnStartServer.IsEnabled = true;
            btnStopServer.IsEnabled = false;
            edtPort.IsEnabled = true;
            edtPort.Text = ConfigurationUtils.readAppSetting("serverport", "11000");
            textBox.Text = "";

            writer = new LogWriter(textBox);
            writer.WriteMessage("Hi. Here's your Outputwindow! ;-) ");

            IDMSHandler handler = new DMSSocketReceiver.dmshandler.impl.TestDMSHandler(writer);

            listener = new AsynchronousTCPListener(writer, handler);
            btnStartServer_Click(null, null);
        }

        private void btnStartServer_Click(object sender, RoutedEventArgs e)
        {
            btnStartServer.IsEnabled = false;
            btnStopServer.IsEnabled = true;
            edtPort.IsEnabled = false;
            listener.StopListening();
            listener.StartListening(int.Parse(edtPort.Text));
            saveSettings();
        }

        private void saveSettings()
        {
            ConfigurationUtils.saveAppSetting("serverport", edtPort.Text);
        }

        private void btnStopServer_Click(object sender, RoutedEventArgs e)
        {
            btnStartServer.IsEnabled = true;
            btnStopServer.IsEnabled = false;
            edtPort.IsEnabled = true;
            listener.StopListening();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            btnStopServer_Click(null, null);
            saveSettings();
        }
    }
}
