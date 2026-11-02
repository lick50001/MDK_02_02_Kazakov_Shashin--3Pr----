using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class Snakes
    {
        public class Point
        {
            public int X { get; set; }
            public int Y { get; set; }

            public Point(int X, int Y)
            {
                this.X = X;
                this.Y = Y;
            }

            public Point() { }
        }

        public enum Direction { Left, Right, Up, Down, Start }

        public List<Point> Points { get; set; } = new List<Point>();
        public Direction direction { get; set; } = Direction.Start;
        public bool GameOver = false;
    }
}
