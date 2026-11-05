using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Common;
using System.Xml;
using Newtonsoft.Json;

namespace Snake
{
    public class Program
    {
        public static List<Leaders> Leaders = new List<Leaders>();
        public static List<ViewModelUserSettings> remoteIPAddress = new List<ViewModelUserSettings>();
        public static List<ViewModelGames> viewModelGames = new List<ViewModelGames>();
        private static int localPort = 5001;
        public static int MaxSpeed = 15;

        public static Snakes.Point Apple = new Snakes.Point();

        private static readonly object Sync = new object();
        private static readonly Random rnd = new Random();
        private static int nextId = 1;
        private const string LeadersFile = "leaders.json";

        public static void Send()
        {
            foreach (ViewModelUserSettings User in remoteIPAddress)
            {
                UdpClient sender = new UdpClient();
                IPEndPoint endPoint = new IPEndPoint(
                    IPAddress.Parse(User.IPAddress),
                    int.Parse(User.Port));

                try
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(viewModelGames.Find(x => x.IdSnake == User.IdSnake)));

                    sender.Send(bytes, bytes.Length, endPoint);
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Отправил данные пользователю: {User.IPAddress}:{User.Port}");
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Возникло исключение: " + ex.ToString() + "\n " + ex.Message);
                }
                finally
                {
                    sender.Close();
                }
            }
        }

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
                        ref RemoteIpEndPoint);

                    string returnData = Encoding.UTF8.GetString(receiveBytes);

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Получили команду: " + returnData.ToString());

                    if (returnData.ToString().Contains("/start"))
                    {
                        string[] dataMessage = returnData.ToString().Split('|');
                        ViewModelUserSettings viewModelUserSettings = JsonConvert.DeserializeObject<ViewModelUserSettings>(dataMessage[1]);
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"Подключился пользователь: {viewModelUserSettings.IPAddress}:{viewModelUserSettings.Port}");
                        remoteIPAddress.Add(viewModelUserSettings);
                        viewModelUserSettings.IdSnake = AddSnake();
                        viewModelGames[viewModelUserSettings.IdSnake].IdSnake = viewModelUserSettings.IdSnake;
                    }
                    else
                    {
                        string[] dataMessage = returnData.ToString().Split('|');
                        ViewModelUserSettings viewModelUserSettings = JsonConvert.DeserializeObject<ViewModelUserSettings>(dataMessage[1]);
                        int IdPlayer = -1;
                        IdPlayer = remoteIPAddress.FindIndex(x => x.IPAddress == viewModelUserSettings.IPAddress
                            && x.Port == viewModelUserSettings.Port);
                        if (IdPlayer != -1)
                        {
                            if (dataMessage[0] == "Up" &&
                                viewModelGames[IdPlayer].SnakesPlayers.direction != Snakes.Direction.Down)
                                viewModelGames[IdPlayer].SnakesPlayers.direction = Snakes.Direction.Up;
                            else if (dataMessage[0] == "Down" &&
                                viewModelGames[IdPlayer].SnakesPlayers.direction != Snakes.Direction.Up)
                                viewModelGames[IdPlayer].SnakesPlayers.direction = Snakes.Direction.Down;
                            else if (dataMessage[0] == "Left" &&
                                viewModelGames[IdPlayer].SnakesPlayers.direction != Snakes.Direction.Right)
                                viewModelGames[IdPlayer].SnakesPlayers.direction = Snakes.Direction.Left;
                            else if (dataMessage[0] == "Right" &&
                                viewModelGames[IdPlayer].SnakesPlayers.direction != Snakes.Direction.Left)
                                viewModelGames[IdPlayer].SnakesPlayers.direction = Snakes.Direction.Right;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Возникло исключение: " + ex.ToString() + "\n " + ex.Message);
            }
        }

        // ---------- Добавление змеи ----------
        public static int AddSnake()
        {
            ViewModelGames viewModelGamesPlayer = new ViewModelGames();
            viewModelGamesPlayer.SnakesPlayers = new Snakes()
            {
                Points = new List<Snakes.Point>() {
            new Snakes.Point() { X = 30, Y = 10 },
            new Snakes.Point() { X = 20, Y = 10 },
            new Snakes.Point() { X = 10, Y = 10 },
        },
                direction = Snakes.Direction.Start
            };
            viewModelGamesPlayer.Points = new Snakes.Point(new Random().Next(10, 783), new Random().Next(10, 410));
            viewModelGames.Add(viewModelGamesPlayer);
            return viewModelGames.FindIndex(x => x == viewModelGamesPlayer);
        }

        private static void NewApple()
        {
            Apple = new Snakes.Point
            {
                X = rnd.Next(1, GameSettings.Width - 1),
                Y = rnd.Next(1, GameSettings.Height - 1)
            };
        }

        // ---------- Игровой цикл ----------
        public static void Timer()
        {
            while (true)
            {
                Thread.Sleep(GameSettings.TickMs);
                lock (Sync)
                {
                    if (viewModelGames.Count == 0) continue;
                    Tick();
                }
            }
        }

        private static void Tick()
        {
            // 1. Движение
            foreach (var g in viewModelGames)
            {
                var s = g.SnakesPlayers;
                var head = s.Points[0];
                var nh = new Snakes.Point { X = head.X, Y = head.Y };

                switch (s.direction)
                {
                    case Snakes.Direction.Right: nh.X++; break;
                    case Snakes.Direction.Left: nh.X--; break;
                    case Snakes.Direction.Up: nh.Y--; break;
                    case Snakes.Direction.Down: nh.Y++; break;
                }

                s.Points.Insert(0, nh);

                if (nh.X == Apple.X && nh.Y == Apple.Y)
                    NewApple();                          // выросла
                else
                    s.Points.RemoveAt(s.Points.Count - 1);
            }

            // 2. Столкновения
            var dead = new List<ViewModelGames>();
            foreach (var g in viewModelGames)
            {
                var s = g.SnakesPlayers;
                var h = s.Points[0];

                bool crash =
                    h.X < 0 || h.Y < 0 || h.X >= GameSettings.Width || h.Y >= GameSettings.Height ||
                    s.Points.Skip(1).Any(p => p.X == h.X && p.Y == h.Y) ||
                    viewModelGames.Where(o => o != g)
                        .Any(o => o.SnakesPlayers.Points.Any(p => p.X == h.X && p.Y == h.Y));

                if (crash) dead.Add(g);
            }

            // 3. Обработка проигравших
            foreach (var g in dead)
            {
                int idx = viewModelGames.IndexOf(g);
                var user = remoteIPAddress[idx];

                g.Score = g.SnakesPlayers.Points.Count - 3;
                Leaders.Add(new Leaders { Name = user.Name, Points = g.Score });
                SaveLeaders();

                var sorted = Leaders.OrderByDescending(l => l.Points).ToList();
                g.Top =
页面访问验证
g.Top
    

sorted.FindIndex(l => l.Name == user.Name && l.Points == g.Score) + 1;
                g.SnakesPlayers.GameOver = true;
                g.Add = Apple;

                Send(user, g);
                Console.WriteLine($"Проиграл: {user.Name}, очки {g.Score}, место {g.Top}");
            }

            foreach (var g in dead)
            {
                int idx = viewModelGames.IndexOf(g);
                viewModelGames.RemoveAt(idx);
                remoteIPAddress.RemoveAt(idx);
            }

            // 4. Рассылка состояния живым
            var all = viewModelGames.Select(v => v.SnakesPlayers).ToList();
            for (int i = 0; i < viewModelGames.Count; i++)
            {
                viewModelGames[i].Add = Apple;
                viewModelGames[i].AllSnakes = all;
                viewModelGames[i].Score = viewModelGames[i].SnakesPlayers.Points.Count - 3;
                Send(remoteIPAddress[i], viewModelGames[i]);
            }
        }

        // ---------- Рекорды ----------
        public static void SaveLeaders()
        {
            try
            {
                File.WriteAllText(LeadersFile, JsonConvert.SerializeObject(Leaders, Formatting.Indented));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка сохранения рекордов: " + ex.Message);
            }
        }

        public static void LoadLeaders()
        {
            try
            {
                if (File.Exists(LeadersFile))
                    Leaders = JsonConvert.DeserializeObject < List < Leaders»(File.ReadAllText(LeadersFile))
                                ?? new List<Leaders>();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка загрузки рекордов: " + ex.Message);
                Leaders = new List<Leaders>();
            }
        }

        static void Main(string[] args)
        {
            LoadLeaders();

            new Thread(Receiver) { IsBackground = true }.Start();
            new Thread(Timer) { IsBackground = true }.Start();

            Console.WriteLine("Сервер запущен. Нажмите Enter для выхода.");
            Console.ReadLine();
        }
    }
}
