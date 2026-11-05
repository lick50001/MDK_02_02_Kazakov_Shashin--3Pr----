
using Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Snake
{
    public class Program
    {
        public static List<Leaders> Leaders = new List<Leaders>();
        public static List<ViewModelUserSettings> remoteIPAddress =
            new List<ViewModelUserSettings>();

        public static List<ViewModelGames> viewModelGames =
            new List<ViewModelGames>();

        private static int localPort = 5001;
        public static int MaxSpeed = 15;

        // Блокировка общих данных между потоками
        private static readonly object stateLock = new object();

        // Уникальный ID каждой змейки
        private static int nextSnakeId = 0;

        // Общий генератор случайных чисел
        private static Random random = new Random();

        // ОТПРАВКА ДАННЫХ КЛИЕНТАМ
        public static void Send()
        {
            lock (stateLock)
            {
                foreach (ViewModelUserSettings User in remoteIPAddress)
                {
                    UdpClient sender = new UdpClient();

                    try
                    {
                        IPEndPoint endPoint = new IPEndPoint(
                            IPAddress.Parse(User.IPAddress),
                            int.Parse(User.Port)
                        );

                        ViewModelGames game = viewModelGames.Find(
                            x => x.IdSnake == User.IdSnake
                        );

                        if (game == null)
                            continue;

                        byte[] bytes = Encoding.UTF8.GetBytes(
                            JsonConvert.SerializeObject(game)
                        );

                        sender.Send(bytes, bytes.Length, endPoint);

                        Console.ForegroundColor = ConsoleColor.Yellow;

                        Console.WriteLine(
                            $"Отправил данные пользователю: " +
                            $"{User.IPAddress}:{User.Port}"
                        );
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;

                        Console.WriteLine(
                            "Возникло исключение: " + ex.ToString()
                        );
                    }
                    finally
                    {
                        sender.Close();
                    }
                }
            }
        }

        // ПОЛУЧЕНИЕ ДАННЫХ ОТ КЛИЕНТОВ
        public static void Receiver()
        {
            UdpClient receivingUdpClient = new UdpClient(localPort);
            IPEndPoint RemoteIpEndPoint = null;

            try
            {
                Console.WriteLine("Команды сервера:");

                while (true)
                {
                    byte[] receiveBytes = receivingUdpClient.Receive(
                        ref RemoteIpEndPoint
                    );

                    string returnData = Encoding.UTF8.GetString(receiveBytes);

                    Console.ForegroundColor = ConsoleColor.Green;

                    Console.WriteLine("Получили команду: " + returnData);

                    string[] dataMessage = returnData.Split('|');

                    if (dataMessage.Length < 2)
                        continue;

                    ViewModelUserSettings viewModelUserSettings =
                        JsonConvert.DeserializeObject<ViewModelUserSettings>(
                            dataMessage[1]
                        );

                    if (viewModelUserSettings == null)
                        continue;

                    lock (stateLock)
                    {
                        // ПОДКЛЮЧЕНИЕ НОВОГО ИГРОКА
                        if (dataMessage[0].Contains("/start"))
                        {
                            // Проверяем, не подключен ли уже пользователь
                            bool alreadyConnected = remoteIPAddress.Any(
                                x => x.IPAddress == viewModelUserSettings.IPAddress &&
                                     x.Port == viewModelUserSettings.Port
                            );

                            if (alreadyConnected)
                                continue;

                            Console.ForegroundColor = ConsoleColor.Green;

                            Console.WriteLine(
                                $"Подключился пользователь: " +
                                $"{viewModelUserSettings.IPAddress}:" +
                                $"{viewModelUserSettings.Port}"
                            );

                            // Создаем змейку
                            viewModelUserSettings.IdSnake = AddSnake();

                            // Добавляем пользователя
                            remoteIPAddress.Add(viewModelUserSettings);
                        }
                        else
                        {
                            // ИЩЕМ ИГРОКА
                            ViewModelUserSettings User = remoteIPAddress.Find(
                                x => x.IPAddress == viewModelUserSettings.IPAddress &&
                                     x.Port == viewModelUserSettings.Port
                            );

                            if (User == null)
                                continue;

                            // ИЩЕМ ЕГО ЗМЕЙКУ ПО ID
                            ViewModelGames game = viewModelGames.Find(
                                x => x.IdSnake == User.IdSnake
                            );

                            if (game == null)
                                continue;

                            // ИЗМЕНЕНИЕ НАПРАВЛЕНИЯ ДВИЖЕНИЯ
                            if (dataMessage[0] == "Up" &&
                                game.SnakesPlayers.direction != Snakes.Direction.Down)
                            {
                                game.SnakesPlayers.direction =
                                    Snakes.Direction.Up;
                            }
                            else if (dataMessage[0] == "Down" &&
                                game.SnakesPlayers.direction != Snakes.Direction.Up)
                            {
                                game.SnakesPlayers.direction =
                                    Snakes.Direction.Down;
                            }
                            else if (dataMessage[0] == "Left" &&
                                game.SnakesPlayers.direction != Snakes.Direction.Right)
                            {
                                game.SnakesPlayers.direction =
                                    Snakes.Direction.Left;
                            }
                            else if (dataMessage[0] == "Right" &&
                                game.SnakesPlayers.direction != Snakes.Direction.Left)
                            {
                                game.SnakesPlayers.direction =
                                    Snakes.Direction.Right;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;

                Console.WriteLine(
                    "Возникло исключение: " + ex.ToString() +
                    "\n" + ex.Message
                );

                receivingUdpClient.Close();
            }
        }

        // СОЗДАНИЕ НОВОЙ ЗМЕЙКИ
        public static int AddSnake()
        {
            ViewModelGames viewModelGamesPlayer = new ViewModelGames();

            // Присваиваем уникальный ID
            viewModelGamesPlayer.IdSnake = nextSnakeId++;

            viewModelGamesPlayer.SnakesPlayers = new Snakes()
            {
                Points = new List<Snakes.Point>()
                {
                    new Snakes.Point() { X = 30, Y = 10 },
                    new Snakes.Point() { X = 20, Y = 10 },
                    new Snakes.Point() { X = 10, Y = 10 }
                },

                direction = Snakes.Direction.Start
            };

            viewModelGamesPlayer.Points = new Snakes.Point(
                random.Next(10, 783),
                random.Next(10, 410)
            );

            viewModelGames.Add(viewModelGamesPlayer);

            return viewModelGamesPlayer.IdSnake;
        }

        // ОСНОВНОЙ ИГРОВОЙ ЦИКЛ
        public static void Timer()
        {
            while (true)
            {
                Thread.Sleep(100);

                lock (stateLock)
                {
                    // ПОИСК ПРОИГРАВШИХ ИГРОКОВ
                    List<ViewModelGames> RemoteSnakes =
                        viewModelGames.FindAll(
                            x => x.SnakesPlayers.GameOver
                        );

                    if (RemoteSnakes.Count > 0)
                    {
                        foreach (ViewModelGames DeadSnake in RemoteSnakes)
                        {
                            ViewModelUserSettings User = remoteIPAddress.Find(
                                x => x.IdSnake == DeadSnake.IdSnake
                            );

                            if (User != null)
                            {
                                Console.ForegroundColor = ConsoleColor.Green;

                                Console.WriteLine(
                                    $"Отключим пользователя: " +
                                    $"{User.IPAddress}:{User.Port}"
                                );

                                remoteIPAddress.Remove(User);
                            }
                        }

                        // Удаляем проигравшие змейки
                        viewModelGames.RemoveAll(
                            x => x.SnakesPlayers.GameOver
                        );
                    }

                    // ПЕРЕБОР ВСЕХ ИГРОКОВ
                    foreach (ViewModelUserSettings User in remoteIPAddress)
                    {
                        // Находим игру по уникальному ID
                        ViewModelGames game = viewModelGames.Find(
                            x => x.IdSnake == User.IdSnake
                        );

                        if (game == null)
                            continue;

                        Snakes Snake = game.SnakesPlayers;

                        // ДВИЖЕНИЕ ЗМЕЙКИ
                        for (int i = Snake.Points.Count - 1; i >= 0; i--)
                        {
                            if (i > 0)
                            {
                                Snake.Points[i] = Snake.Points[i - 1];
                            }
                            else
                            {
                                int Speed = 10 + (int)Math.Round(
                                    Snake.Points.Count / 20f
                                );

                                if (Speed > MaxSpeed)
                                    Speed = MaxSpeed;

                                if (Snake.direction == Snakes.Direction.Right)
                                {
                                    Snake.Points[i] = new Snakes.Point()
                                    {
                                        X = Snake.Points[i].X + Speed,
                                        Y = Snake.Points[i].Y
                                    };
                                }
                                else if (Snake.direction == Snakes.Direction.Down)
                                {
                                    Snake.Points[i] = new Snakes.Point()
                                    {
                                        X = Snake.Points[i].X,
                                        Y = Snake.Points[i].Y + Speed
                                    };
                                }
                                else if (Snake.direction == Snakes.Direction.Up)
                                {
                                    Snake.Points[i] = new Snakes.Point()
                                    {
                                        X = Snake.Points[i].X,
                                        Y = Snake.Points[i].Y - Speed
                                    };
                                }
                                else if (Snake.direction == Snakes.Direction.Left)
                                {
                                    Snake.Points[i] = new Snakes.Point()
                                    {
                                        X = Snake.Points[i].X - Speed,
                                        Y = Snake.Points[i].Y
                                    };
                                }
                            }
                        }

                        // ПРОВЕРКА СТОЛКНОВЕНИЯ СО СТЕНАМИ
                        if (Snake.Points[0].X < 0 ||
                            Snake.Points[0].X > 793)
                        {
                            Snake.GameOver = true;
                        }
                        else if (Snake.Points[0].Y < 0 ||
                                 Snake.Points[0].Y > 420)
                        {
                            Snake.GameOver = true;
                        }

                        // ПРОВЕРКА СТОЛКНОВЕНИЯ С СОБСТВЕННЫМ ТЕЛОМ
                        if (Snake.direction != Snakes.Direction.Start)
                        {
                            for (int i = 1; i < Snake.Points.Count; i++)
                            {
                                if (Snake.Points[0].X > Snake.Points[i].X - 1 &&
                                    Snake.Points[0].X < Snake.Points[i].X + 1)
                                {
                                    if (Snake.Points[0].Y > Snake.Points[i].Y - 1 &&
                                        Snake.Points[0].Y < Snake.Points[i].Y + 1)
                                    {
                                        Snake.GameOver = true;
                                        break;
                                    }
                                }
                            }
                        }

                        // ПРОВЕРКА СТОЛКНОВЕНИЯ С ЕДОЙ
                        if (!Snake.GameOver &&
                            Snake.Points[0].X > game.Points.X - 15 &&
                            Snake.Points[0].X < game.Points.X + 15)
                        {
                            if (Snake.Points[0].Y > game.Points.Y - 15 &&
                                Snake.Points[0].Y < game.Points.Y + 15)
                            {
                                // Перемещаем еду
                                game.Points = new Snakes.Point(
                                    random.Next(10, 783),
                                    random.Next(10, 410)
                                );

                                // Увеличиваем длину змейки
                                Snake.Points.Add(new Snakes.Point()
                                {
                                    X = Snake.Points[Snake.Points.Count - 1].X,
                                    Y = Snake.Points[Snake.Points.Count - 1].Y
                                });

                                // ОБНОВЛЯЕМ ТАБЛИЦУ ЛИДЕРОВ
                                LoadLeaders();

                                Leaders.Add(new Leaders()
                                {
                                    Name = User.Name,
                                    Points = Snake.Points.Count - 3
                                });

                                Leaders = Leaders
                                    .OrderByDescending(x => x.Points)
                                    .ThenBy(x => x.Name)
                                    .ToList();

                                game.Top = Leaders.FindIndex(
                                    x => x.Points == Snake.Points.Count - 3 &&
                                         x.Name == User.Name
                                ) + 1;
                            }
                        }

                        // ОБРАБОТКА ПРОИГРЫША
                        if (Snake.GameOver)
                        {
                            LoadLeaders();

                            Leaders.Add(new Leaders()
                            {
                                Name = User.Name,
                                Points = Snake.Points.Count - 3
                            });

                            SaveLeaders();
                        }
                    }

                    // ОТПРАВКА ОБНОВЛЕННЫХ ДАННЫХ
                    Send();
                }
            }
        }

        // СОХРАНЕНИЕ ТАБЛИЦЫ ЛИДЕРОВ
        public static void SaveLeaders()
        {
            string json = JsonConvert.SerializeObject(Leaders);

            StreamWriter SW = new StreamWriter("./leaders.txt");

            SW.WriteLine(json);

            SW.Close();
        }

        // ЗАГРУЗКА ТАБЛИЦЫ ЛИДЕРОВ
        public static void LoadLeaders()
        {
            if (File.Exists("./leaders.txt"))
            {
                StreamReader SR = new StreamReader("./leaders.txt");

                string json = SR.ReadLine();

                SR.Close();

                if (!string.IsNullOrEmpty(json))
                {
                    Leaders = JsonConvert.DeserializeObject<List<Leaders>>(json);

                    if (Leaders == null)
                        Leaders = new List<Leaders>();
                }
                else
                {
                    Leaders = new List<Leaders>();
                }
            }
            else
            {
                Leaders = new List<Leaders>();
            }
        }

        // ЗАПУСК СЕРВЕРА
        static void Main(string[] args)
        {
            try
            {
                LoadLeaders();

                Thread tRec = new Thread(new ThreadStart(Receiver));
                tRec.Start();

                Thread tTime = new Thread(new ThreadStart(Timer));
                tTime.Start();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;

                Console.WriteLine(
                    "Возникло исключение: " + ex.ToString() +
                    "\n" + ex.Message
                );
            }
        }
    }
}
