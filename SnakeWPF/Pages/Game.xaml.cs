using Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
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
    /// Логика взаимодействия для Game.xaml
    /// </summary>
    public partial class Game : Page
    {
        public int StepCadr = 0;
        public Game()
        {
            InitializeComponent();
        }

        public void CreateUI()
        {
            Dispatcher.Invoke(() =>
            {
                if (StepCadr == 0) StepCadr = 1;
                else StepCadr = 0;
                canvas.Children.Clear();
                for(int iPoint = MainWindow.mainWindow.viewModelGames.SnakesPlayers.Points.Count - 1; iPoint >= 0; iPoint--)
                {
                    Snakes.Point SnakePoint = MainWindow.mainWindow.viewModelGames.SnakesPlayers.Points[iPoint];

                    if(iPoint != 0)
                    {
                        Snakes.Point NextSnakePoint = MainWindow.mainWindow.viewModelGames.SnakesPlayers.Points[iPoint - 1];

                        if(SnakePoint.X > NextSnakePoint.X || SnakePoint.X < NextSnakePoint.X)
                        {
                            if (iPoint % 2 == 0)
                            {
                                if (StepCadr % 2 == 0)
                                    SnakePoint.Y -= 1;
                                else
                                    SnakePoint.Y += 1;
                            } else
                            {
                                if (StepCadr % 2 == 0)
                                    SnakePoint.Y += 1;
                                else
                                    SnakePoint.Y -= 1;
                            }
                        }
                        else if(SnakePoint.Y > NextSnakePoint.Y || SnakePoint.Y < NextSnakePoint.Y)
                        {
                            if (iPoint % 2 == 0)
                            {
                                if (StepCadr % 2 == 0)
                                    SnakePoint.X -= 1;
                                else
                                    SnakePoint.X += 1;
                            }
                            else
                            {
                                if (StepCadr % 2 == 0)
                                    SnakePoint.X += 1;
                                else
                                    SnakePoint.X -= 1;
                            }
                        }
                    }
                    Brush Color;
                    if(iPoint == 0)
                        Color = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 0, 127, 14));
                    else
                        Color = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 0, 198, 19));

                    Ellipse ellipse = new Ellipse()
                    {
                        Width = 20,
                        Height = 20,
                        Margin = new Thickness(SnakePoint.X - 10, SnakePoint.Y - 10, 0, 0),
                        Fill = Color,
                        Stroke = Brushes.Black
                    };
                    canvas.Children.Add(ellipse);
                }
                ImageBrush myBrush = new ImageBrush();
                myBrush.ImageSource = new BitmapImage(new Uri(Directory.GetCurrentDirectory() + @"\Image\apple.png"));

                Ellipse points = new Ellipse()
                {
                    Width = 40,
                    Height = 40,
                    Margin = new Thickness(
                        MainWindow.mainWindow.viewModelGames.Points.X - 20, 
                        MainWindow.mainWindow.viewModelGames.Points.Y - 20, 0, 0),
                    Fill = myBrush,
                };
                canvas.Children.Add(points);
            });
        }
    }
}
