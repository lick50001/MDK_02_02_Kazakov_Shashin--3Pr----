using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Snake
{
    internal class Program
    {
        public static List<Leaders> Leaders = new List<Leaders> ();
        public static List<ViewModelUserSettings> remoteIPAdress = new List<ViewModelUserSettings> ();
        public static List<ViewModelGames> viewModelGames = new List<ViewModelGames> ();
        public static int localPort = 5001;
        public static int MaxSpeed = 15;
      
        static void Main(string[] args)
        {
        }
    }
}
