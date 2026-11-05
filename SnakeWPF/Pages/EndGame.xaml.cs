using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SnakeWPF.Pages
{
    /// <summary>
    /// Логика взаимодействия для EndGame.xaml
    /// </summary>
    public partial class EndGame : Page
    {
        public EndGame()
        {
            InitializeComponent();
            name.Content = MainWindow.mainWindow.viewModelUserSettings.Name;
            top.Content = MainWindow.mainWindow.viewModelGames.Top;
            glasses.Content = $"{MainWindow.mainWindow.viewModelGames.SnakesPlayers.Points.Count - 3} glasses";
            MainWindow.mainWindow.receivingUdpClient.Close();
            MainWindow.mainWindow.tRec.Abort();
            MainWindow.mainWindow.viewModelGames = null;
        }

        private void OpenHome(object sender, RoutedEventArgs e)
        {
            MainWindow.mainWindow.OpenPage(MainWindow.mainWindow.Home);
        }
    }
}
