using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Net;
using System.Deployment.Application;
using System.Reflection;


namespace UnisokuRemote
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        bool _IsUdpConnected;
        IPAddressEnum _selectedIPAddressEnum;

        UDPSocket socket = new UDPSocket();

        void OnUdpReceive(string msg)
        {

            WriteInfo("UDP received msg: " + msg);
            if (msg == "1")
            {
                UnisokuAutomation.Instance.MoveZLeft();
                WriteInfo("Remote:MoveLeft");
            }
            else if (msg == "2")
            {
                UnisokuAutomation.Instance.MoveZRight();
                WriteInfo("Remote:MoveRight");
            }
            else if (msg == "0")
            {
                UnisokuAutomation.Instance.MoveZZero();
                WriteInfo("Remote:Zero");
            }
        }

        public bool IsUdpConnected
        {
            get
            {
                return _IsUdpConnected;
            }
            set
            {
                _IsUdpConnected = value;
                connectButton.Content = _IsUdpConnected ? "OFF" : "Connect";
                connectButton.Background = new SolidColorBrush(_IsUdpConnected ? Colors.Red : Colors.Green);
            }
        }



        public MainWindow()
        {
            InitializeComponent();
            mainWindow.Title = "UnisokuRemote " + getRunningVersion();

            
            if (UnisokuAutomation.Instance.Init())
            {
                IPComboBox.ItemsSource = Enum.GetValues(typeof(IPAddressEnum));
                _selectedIPAddressEnum = IPAddressEnum.Local;
                _IsUdpConnected = false;

                socket.OnUdpReceive += OnUdpReceive;

                WriteInfo("Find target exe at " + UnisokuAutomation.Instance.hWnd.ToString());
            }
            else
            {
                string messageBoxText = "can not find unisoku exe. Please open it first.";
                string caption = "Error";
                MessageBoxButton button = MessageBoxButton.OK;
                MessageBoxImage icon = MessageBoxImage.Error;
                MessageBox.Show(messageBoxText, caption, button, icon, MessageBoxResult.Yes);
                Application.Current.Shutdown();
            }



        }

       
        private void IPAddress_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedIPAddressEnum = (IPAddressEnum)IPComboBox.SelectedItem;
            IPEndPoint endPoint = ServerUtility.GetIPEndPoint(_selectedIPAddressEnum, 1453);
            IPText.Text = endPoint.Address.ToString();
        }

        private static readonly System.Text.RegularExpressions.Regex _regex = new System.Text.RegularExpressions.Regex("[^0-9]");
        private void PortTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = _regex.IsMatch(e.Text);
        }



        
        private void OnConnectButton_Click(object sender, RoutedEventArgs e)
        {
            
            IsUdpConnected = !IsUdpConnected;
            if (IsUdpConnected)
            {
                IPEndPoint endPoint = ServerUtility.GetIPEndPoint(_selectedIPAddressEnum, int.Parse(PortText.Text));
                socket.Server(endPoint);

                WriteInfo("Server In " + endPoint.Address.ToString() + ":" + endPoint.Port.ToString());
            }
            else
            {
                socket.Stop();

                WriteInfo("Disconnected");
            }
            
        }


        private void LeftButton_Click(object sender, RoutedEventArgs e)
        {
            UnisokuAutomation.Instance.MoveZLeft();
            WriteInfo("Remote:MoveLeft");
            StageLabel.Content = "Stage: " + UnisokuAutomation.Instance.ZSliderValue.ToString("f0") + "V";
        }

        private void RightButton_Click(object sender, RoutedEventArgs e)
        {
            UnisokuAutomation.Instance.MoveZRight();
            WriteInfo("Remote:MoveRight");
            StageLabel.Content = "Stage: " + UnisokuAutomation.Instance.ZSliderValue.ToString("f0") + "V";
        }

        private void ZeroButton_Click(object sender, RoutedEventArgs e)
        {
            UnisokuAutomation.Instance.MoveZZero();
            WriteInfo("Remote:Zero");
            StageLabel.Content = "Stage: " + UnisokuAutomation.Instance.ZSliderValue.ToString("f0") + "V";
        }

        public void WriteInfo(string info)
        {
            var now = DateTime.Now.ToString("HH:mm:ss");
            InfoText.AppendText(now + " " + info + "\r\n");
            InfoText.ScrollToEnd();
        }




        private Version getRunningVersion()
        {
            try
            {
                return ApplicationDeployment.CurrentDeployment.CurrentVersion;
            }
            catch (Exception)
            {
                return Assembly.GetExecutingAssembly().GetName().Version;
            }
        }
    }
}
