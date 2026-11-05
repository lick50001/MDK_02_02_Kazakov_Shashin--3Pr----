using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Common;
using Newtonsoft.Json;

namespace SnakeWPF
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static MainWindow mainWindow;
        public ViewModelUserSettings viewModelUserSettings = new ViewModelUserSettings();
        public ViewModelGames viewModelGames = null;
        public static IPAddress remoteIPAddress = IPAddress.Parse("127.0.0.1");
        public static int remotePort = 5001;
        public Thread tRec;
        public UdpClient receivingUdpClient;
        public Pages.Home Home = new Pages.Home();
        public Pages.Game Game = new Pages.Game();
        public MainWindow()
        {
            InitializeComponent();
            mainWindow = this;
            OpenPage(Home);
        }
        
        public void StartReceiver()
        {
            tRec = new Thread(new ThreadStart(Receiver));
            tRec.Start();
        }

        public void OpenPage(Page PageOpen)
        {
            DoubleAnimation startAnimation = new DoubleAnimation();
            startAnimation.From = 1;
            startAnimation.To = 0;
            startAnimation.Duration = TimeSpan.FromSeconds(0.6);
            startAnimation.Completed += delegate
            {
                frame.Navigate(PageOpen);
                DoubleAnimation endAnimation = new DoubleAnimation();
                endAnimation.From = 0;
                endAnimation.To = 1;
                endAnimation.Duration = TimeSpan.FromSeconds(0.6);
                frame.BeginAnimation(OpacityProperty, endAnimation);
            };
            frame.BeginAnimation(OpacityProperty, startAnimation);
        }

        public void Receiver()
        {
            receivingUdpClient = new UdpClient(int.Parse(viewModelUserSettings.Port));
            IPEndPoint RemoteIpEndPoint = null;
            try
            {
                while (true)
                {
                    byte[] receiveBytes = receivingUdpClient.Receive(ref RemoteIpEndPoint);
                    string returnData = Encoding.UTF8.GetString(receiveBytes);
                    if(viewModelGames == null)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            OpenPage(Game);
                        });
                    }
                    viewModelGames = JsonConvert.DeserializeObject<ViewModelGames>(returnData.ToString());

                    if(viewModelGames.SnakesPlayers.GameOver)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            OpenPage(new Pages.EndGame());
                        });
                    }
                    else
                    {
                        Game.CreateUI();
                    }
                }
            } 
            catch(Exception ex)
            {
                Debug.WriteLine("Возникло исключение: " + ex.ToString() + "\n " + ex.Message);
            }

        }

        public static void Send(string datagram)
        {
            UdpClient sender = new UdpClient();
            IPEndPoint endPoint = new IPEndPoint(remoteIPAddress, remotePort);
            try
            {
                byte[] bytes = Encoding.UTF8.GetBytes(datagram);
                sender.Send(bytes, bytes.Length, endPoint);
            } 
            catch(Exception ex)
            {
                Debug.WriteLine("Возникло исключение: " + ex.ToString() + "\n " + ex.Message);
            }
            finally
            {
                sender.Close();
            }
        }

        public void EventKeyUp(object sender, KeyEventArgs e)
        {
            if(!string.IsNullOrEmpty(viewModelUserSettings.IPAddress) && 
                !string.IsNullOrEmpty(viewModelUserSettings.Port) && 
                (viewModelGames != null && !viewModelGames.SnakesPlayers.GameOver))
            {
                if (e.Key == Key.Up)
                {
                    Send($"Up|{JsonConvert.SerializeObject(viewModelUserSettings)}");
                }
                else if (e.Key == Key.Down)
                {
                    Send($"Down|{JsonConvert.SerializeObject(viewModelUserSettings)}");
                }
                else if (e.Key == Key.Left)
                {
                    Send($"Left|{JsonConvert.SerializeObject(viewModelUserSettings)}");
                }
                else if (e.Key == Key.Right)
                {
                    Send($"Right|{JsonConvert.SerializeObject(viewModelUserSettings)}");
                }
            }
        }

        public void QuitApplication(object sender, System.ComponentModel.CancelEventArgs e)
        {
            receivingUdpClient.Close();
            tRec.Abort();
        }
    }
}
