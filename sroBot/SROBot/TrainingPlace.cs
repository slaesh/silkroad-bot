using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sroBot.SROBot
{
    public class TrainingPlace : MVVM.ViewModelBase
    {
        private Point[] polygon = new Point[0];
        private double[] constant;
        private double[] multiple;
        private double[] polyX;
        private double[] polyY;
        private const uint POLYGON_MIN_COUT = 3;

        public Point[] Polygon
        {
            get { return polygon; }
            set
            {
                polygon = value;
                precalcPolygonCheckValues();
            }
        }
        public Point Middle { get; set; }
        public uint Radius { get; set; } = 0;
        public String WalkingScript
        {
            get { return GetValue(() => WalkingScript); }
            set { SetValue(() => WalkingScript, value); }
        }

        public TrainingPlace() { }

        public void SetTrainArea(Point middle, uint radius)
        {
            Middle = middle;
            Radius = radius;
            Polygon = new Point[0];
        }

        //  Globals which should be set before calling these functions:
        //
        //  int    polyCorners  =  how many corners the polygon has (no repeats)
        //  float  polyX[]      =  horizontal coordinates of corners
        //  float  polyY[]      =  vertical coordinates of corners
        //  float  x, y         =  point to be tested
        //
        //  The following global arrays should be allocated before calling these functions:
        //
        //  float  constant[] = storage for precalculated constants (same size as polyX)
        //  float  multiple[] = storage for precalculated multipliers (same size as polyX)
        //
        //  (Globals are used in this example for purposes of speed.  Change as
        //  desired.)
        //
        //  USAGE:
        //  Call precalc_values() to initialize the constant[] and multiple[] arrays,
        //  then call pointInPolygon(x, y) to determine if the point is in the polygon.
        //
        //  The function will return YES if the point x,y is inside the polygon, or
        //  NO if it is not.  If the point is exactly on the edge of the polygon,
        //  then the function may return YES or NO.
        //
        //  Note that division by zero is avoided because the division is protected
        //  by the "if" clause which surrounds it.

        private void precalcPolygonCheckValues()
        {
            polyX = polygon.Select(p => (double)p.X).ToArray();
            polyY = polygon.Select(p => (double)p.Y).ToArray();
            constant = new double[Polygon.Length];
            multiple = new double[Polygon.Length];

            var polyCorners = Polygon.Length;
            int i, j = polyCorners - 1;

            for (i = 0; i < polyCorners; i++)
            {
                if (polyY[j] == polyY[i])
                {
                    constant[i] = polyX[i];
                    multiple[i] = 0;
                }
                else
                {
                    constant[i] = polyX[i] - (polyY[i] * polyX[j]) / (polyY[j] - polyY[i]) + (polyY[i] * polyX[i]) / (polyY[j] - polyY[i]);
                    multiple[i] = (polyX[j] - polyX[i]) / (polyY[j] - polyY[i]);
                }
                j = i;
            }
        }

        public void SetTrainArea(IEnumerable<Point> polygon)
        {
            if (polygon == null || polygon.Count() < POLYGON_MIN_COUT) return;

            Radius = 0;
            Middle = new Point();
            Polygon = polygon.ToArray();
        }

        public bool IsInside(Mob mob, int tolerance = 0)
        {
            return IsInside(mob.X, mob.Y, tolerance);
        }

        public bool IsInside(Point p)
        {
            return IsInside(p.X, p.Y);
        }

        public bool IsInside(int x, int y, int tolerance = 0)
        {
            if (IsUsingCircle())
            {
                if (Radius == 0) return true;

                var R = Radius + tolerance;
                var dx = Math.Abs(x - Middle.X);
                var dy = Math.Abs(y - Middle.Y);
                if (dx + dy <= R) return true;
                if (dx > R) return false;
                if (dy > R) return false;
                if (Math.Pow(dx, 2) + Math.Pow(dy, 2) <= Math.Pow(R, 2)) return true;
                else return false;
            }
            else if (polygon.Length >= POLYGON_MIN_COUT)
            {
                var polyCorners = polygon.Length;
                int i, j = polyCorners - 1;
                bool oddNodes = false;

                for (i = 0; i < polyCorners; i++)
                {
                    if ((polyY[i] < y && polyY[j] >= y
                    || polyY[j] < y && polyY[i] >= y))
                    {
                        oddNodes ^= (y * multiple[i] + constant[i] < x);
                    }
                    j = i;
                }

                return oddNodes;
            }

            return true;
        }

        public bool IsUsingCircle()
        {
            return Polygon.Length < POLYGON_MIN_COUT;
        }

        public bool LevelUp(byte level)
        {
            var wlkScpt = WalkingScript;

            if (level < 10)
            {
                wlkScpt = "mangyang.txt";
            }
            else if (level < 18)
            {
                wlkScpt = "level_010.txt";
            }
            else if (level < 22)
            {
                wlkScpt = "level_020.txt";
            }
            else if (level < 30)
            {
                wlkScpt = "level_025.txt";
            }
            else if (level < 35)
            {
                wlkScpt = "level_035.txt";
            }
            else if (level < 45)
            {
                wlkScpt = "level_040.txt";
            }
            else if (level < 55)
            {
                wlkScpt = "level_050.txt";
            }
            else if (level < 65)
            {
                wlkScpt = "level_060.txt";
            }
            else if (level < 95)
            {
                wlkScpt = "level_070.txt";
            }
            else if (level < 110)
            {
                wlkScpt = "level_101.txt";
            }
            else if (level < 115)
            {
                wlkScpt = "level_106.txt";
            }
            else if (level < 120)
            {
                wlkScpt = "level_108.txt";
            }

            if (wlkScpt != WalkingScript)
            {
                WalkingScript = wlkScpt;
                return true;
            }

            return false;
        }
    }
}
