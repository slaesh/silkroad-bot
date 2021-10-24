using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace sroBot
{
    /// <summary>
    /// Interaktionslogik für DlgMiniMap.xaml
    /// </summary>
    public partial class DlgMiniMap : Window
    {
        public const int SECTOR_IMG_SIZE = 256 - 10;
        public const int SECTOR_SRO_SIZE = 192;

        private SROBot.Bot bot;
        private Timer timer;

        public DlgMiniMap(SROBot.Bot bot)
        {
            InitializeComponent();

            //redrawMap(-10702, 3257);
            //redrawMap(-8969 + 0 /* +137 */, -622);
            //redrawMap(1337, 1337);
            //redrawMap(-9010, -557);
            //redrawMap(-10667, 3315); // huegel neben kreuzung
            //redrawMap(-10787, 3344); // kreuzung
            //redrawMap(-4417, 810); // fehler
            //redrawMap(bot.Char.CurPosition.X, bot.Char.CurPosition.Y);

            this.bot = bot;

            Title = bot.CharName;

            timer = new Timer();
            timer.Interval = 700;
            timer.Elapsed += (s, e) => Dispatcher.Invoke(() => { if (showCurrentCoords) { redrawMap(bot.Char.CurPosition.X, bot.Char.CurPosition.Y); } });
            timer.Start();

            Closed += (s, e) => { timer.Stop(); timer.Close(); timer.Dispose(); };
        }

        private BitmapImage CreateBitmapSource(Color color)
        {
            int width = SECTOR_IMG_SIZE;
            int height = SECTOR_IMG_SIZE;
            var pf = System.Windows.Media.PixelFormats.Indexed1;
            int stride = width / pf.BitsPerPixel;
            byte[] pixels = new byte[height * stride];

            List<System.Windows.Media.Color> colors = new List<System.Windows.Media.Color>();
            colors.Add(color);
            BitmapPalette myPalette = new BitmapPalette(colors);

            BitmapSource image = BitmapSource.Create(
                width,
                height,
                96,
                96,
                pf,
                myPalette,
                pixels,
                stride);


            BitmapSource bitmapSource = image;

            JpegBitmapEncoder encoder = new JpegBitmapEncoder();
            using (var memoryStream = new MemoryStream())
            {
                BitmapImage bImg = new BitmapImage();

                encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
                encoder.Save(memoryStream);

                bImg.BeginInit();
                bImg.StreamSource = new MemoryStream(memoryStream.ToArray());
                bImg.EndInit();

                return bImg;
            }
        }

        private BitmapImage loadSector(int xsec, int ysec)
        {
            var execDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

            var path = System.IO.Path.Combine(execDir, "minimap", String.Format("{0}x{1}.jpg", xsec, ysec));
            if (File.Exists(path))
            {
                try
                {
                    var image = new BitmapImage();
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;

                    var buf = File.ReadAllBytes(path);
                    image.StreamSource = new MemoryStream(buf);
                    image.EndInit();

                    return image;
                }
                catch { throw; }
            }

            return CreateBitmapSource(Colors.GreenYellow);
        }

        private System.Drawing.Point lastPosition = new System.Drawing.Point();
        private int lastXSec = 0;
        private int lastYSec = 0;

        private void redrawMap(int x, int y)
        {
            var xsec = getSectorX(x);
            var ysec = getSectorY(y);

            if (xsec != lastXSec || ysec != lastYSec)
            {
                //Console.WriteLine("xsec: {0}, ysec: {1} -- last: xsec: {2}, ysec: {3}", xsec, ysec, lastXSec, lastYSec);

                lastXSec = xsec;
                lastYSec = ysec;
                
                guiCanvas_map.Children.Clear();

                // left row
                var imgLeftTop = new Image() { Source = loadSector(xsec - 1, ysec + 1) };
                Canvas.SetLeft(imgLeftTop, 0);
                Canvas.SetTop(imgLeftTop, 0);

                var imgLeftMiddle = new Image() { Source = loadSector(xsec - 1, ysec) };
                Canvas.SetLeft(imgLeftMiddle, 0);
                Canvas.SetTop(imgLeftMiddle, SECTOR_IMG_SIZE);

                var imgLeftBottom = new Image() { Source = loadSector(xsec - 1, ysec - 1) };
                Canvas.SetLeft(imgLeftBottom, 0);
                Canvas.SetTop(imgLeftBottom, 2 * SECTOR_IMG_SIZE);

                // middle row
                var imgMiddleTop = new Image() { Source = loadSector(xsec, ysec + 1) };
                Canvas.SetLeft(imgMiddleTop, SECTOR_IMG_SIZE);
                Canvas.SetTop(imgMiddleTop, 0);

                var imgMiddleMiddle = new Image() { Source = loadSector(xsec, ysec) };
                Canvas.SetLeft(imgMiddleMiddle, SECTOR_IMG_SIZE);
                Canvas.SetTop(imgMiddleMiddle, SECTOR_IMG_SIZE);

                var imgMiddleBottom = new Image() { Source = loadSector(xsec, ysec - 1) };
                Canvas.SetLeft(imgMiddleBottom, SECTOR_IMG_SIZE);
                Canvas.SetTop(imgMiddleBottom, 2 * SECTOR_IMG_SIZE);

                // right row
                var imgRightTop = new Image() { Source = loadSector(xsec + 1, ysec + 1) };
                Canvas.SetLeft(imgRightTop, 2 * SECTOR_IMG_SIZE);
                Canvas.SetTop(imgRightTop, 0);

                var imgRightMiddle = new Image() { Source = loadSector(xsec + 1, ysec) };
                Canvas.SetLeft(imgRightMiddle, 2 * SECTOR_IMG_SIZE);
                Canvas.SetTop(imgRightMiddle, SECTOR_IMG_SIZE);

                var imgRightBottom = new Image() { Source = loadSector(xsec + 1, ysec - 1) };
                Canvas.SetLeft(imgRightBottom, 2 * SECTOR_IMG_SIZE);
                Canvas.SetTop(imgRightBottom, 2 * SECTOR_IMG_SIZE);

                guiCanvas_map.Children.Add(imgLeftTop);
                guiCanvas_map.Children.Add(imgLeftMiddle);
                guiCanvas_map.Children.Add(imgLeftBottom);
                guiCanvas_map.Children.Add(imgMiddleTop);
                guiCanvas_map.Children.Add(imgMiddleMiddle);
                guiCanvas_map.Children.Add(imgMiddleBottom);
                guiCanvas_map.Children.Add(imgRightTop);
                guiCanvas_map.Children.Add(imgRightMiddle);
                guiCanvas_map.Children.Add(imgRightBottom);
            }
            
            foreach (var circle in guiCanvas_map.Children.OfType<Ellipse>().ToArray())
            {
                guiCanvas_map.Children.Remove(circle);
            }
            foreach (var poly in guiCanvas_map.Children.OfType<Polygon>().ToArray())
            {
                guiCanvas_map.Children.Remove(poly);
            }

            if (lastPosition.X != x || lastPosition.Y != y)
            {
                //Console.WriteLine("x: {0}, y: {1} -- last: x: {2}, y: {3}", x, y, lastPosition.X, lastPosition.Y);
            }

            lastPosition.X = x;
            lastPosition.Y = y;

            if (bot.Config.TrainPlace.IsUsingCircle())
            {
                if (bot.Config.TrainPlace.Radius != 0)
                {
                    var trainCircle = createCircle((int)bot.Config.TrainPlace.Radius * 2, (int)bot.Config.TrainPlace.Radius * 2, Brushes.Blue, Brushes.LightBlue);
                    trainCircle.Opacity = 0.3;
                    var trainposinsector = getPositionInSector(bot.Config.TrainPlace.Middle.X, bot.Config.TrainPlace.Middle.Y);
                    trainposinsector.X += SECTOR_IMG_SIZE; // get the middle image
                    trainposinsector.Y += SECTOR_IMG_SIZE; // get the middle image
                    Canvas.SetLeft(trainCircle, trainposinsector.X - trainCircle.Width / 2);
                    Canvas.SetTop(trainCircle, trainposinsector.Y - trainCircle.Height / 2);
                    guiCanvas_map.Children.Add(trainCircle);
                }
            }
            else
            {
                var trainPoly = new Polygon();
                var firstPoint = new Point();
                Array.ForEach(bot.Config.TrainPlace.Polygon, p =>
                {
                    var mapPoint = getPositionInSector(p.X, p.Y);
                    mapPoint.X += SECTOR_IMG_SIZE; // get the middle image
                    mapPoint.Y += SECTOR_IMG_SIZE; // get the middle image
                    trainPoly.Points.Add(new System.Windows.Point(mapPoint.X, mapPoint.Y));

                    if (firstPoint.X == 0 && firstPoint.Y == 0)
                    {
                        firstPoint = mapPoint;
                    }
                });
                
                trainPoly.Stroke = Brushes.Blue;
                trainPoly.StrokeThickness = 4;
                trainPoly.Fill = Brushes.LightBlue;
                trainPoly.Opacity = 0.3;
                
                guiCanvas_map.Children.Add(trainPoly);
            }

            // draw ITEMS
            foreach (var item in bot.Spawns.Items.GetAll())
            {
                var m = createCircle(4, 4, Brushes.Yellow, Brushes.LightYellow);
                var pos = getPositionInSector(item.X, item.Y);
                pos.X += SECTOR_IMG_SIZE; // get the middle image
                pos.Y += SECTOR_IMG_SIZE; // get the middle image
                Canvas.SetLeft(m, pos.X - m.Width / 2);
                Canvas.SetTop(m, pos.Y - m.Height / 2);

                m.Opacity = 0.7;
                guiCanvas_map.Children.Add(m);
            }

            // draw MOBS
            foreach (var mob in bot.Spawns.Mobs.GetAll())
            {
                var m = createCircle(6, 6, Brushes.Red, Brushes.Red);
                var pos = getPositionInSector(mob.X, mob.Y);
                pos.X += SECTOR_IMG_SIZE; // get the middle image
                pos.Y += SECTOR_IMG_SIZE; // get the middle image
                Canvas.SetLeft(m, pos.X - m.Width / 2);
                Canvas.SetTop(m, pos.Y - m.Height / 2);

                m.Opacity = 0.7;
                guiCanvas_map.Children.Add(m);
            }

            // draw PETS
            foreach (var pet in bot.Spawns.Pets.GetAll())
            {
                var m = createCircle(5, 5, Brushes.DarkOrange, Brushes.Orange);
                var pos = getPositionInSector(pet.X, pet.Y);
                pos.X += SECTOR_IMG_SIZE; // get the middle image
                pos.Y += SECTOR_IMG_SIZE; // get the middle image
                Canvas.SetLeft(m, pos.X - m.Width / 2);
                Canvas.SetTop(m, pos.Y - m.Height / 2);

                m.Opacity = 0.7;
                guiCanvas_map.Children.Add(m);
            }

            // draw PLAYER
            foreach (var player in bot.Spawns.Player.GetAll())
            {
                var m = createCircle(8, 8, Brushes.DarkBlue, Brushes.Blue);
                var pos = getPositionInSector(player.X, player.Y);
                pos.X += SECTOR_IMG_SIZE; // get the middle image
                pos.Y += SECTOR_IMG_SIZE; // get the middle image
                Canvas.SetLeft(m, pos.X - m.Width / 2);
                Canvas.SetTop(m, pos.Y - m.Height / 2);

                m.Opacity = 0.7;
                guiCanvas_map.Children.Add(m);
            }

            // draw "ME"
            var me = createCircle(8, 8, Brushes.DarkGreen, Brushes.ForestGreen);
            var posinsector = getPositionInSector(x, y);
            posinsector.X += SECTOR_IMG_SIZE; // get the middle image
            posinsector.Y += SECTOR_IMG_SIZE; // get the middle image
            Canvas.SetLeft(me, posinsector.X - me.Width / 2);
            Canvas.SetTop(me, posinsector.Y - me.Height / 2);
            guiCanvas_map.Children.Add(me);


            if (recordScript)
            {
                foreach (var walkp in walkScriptPoints)
                {
                    var m = createCircle(4, 4, Brushes.Orange, Brushes.Orange);
                    var pos = getPositionInSector(walkp.X, walkp.Y);
                    pos.X += SECTOR_IMG_SIZE; // get the middle image
                    pos.Y += SECTOR_IMG_SIZE; // get the middle image
                    Canvas.SetLeft(m, pos.X - m.Width / 2);
                    Canvas.SetTop(m, pos.Y - m.Height / 2);

                    m.Opacity = 1;
                    guiCanvas_map.Children.Add(m);
                }
            }

        }

        private Ellipse createCircle(int width, int height, Brush outer, Brush inner)
        {
            var e = new Ellipse();
            e.Width = width;
            e.Height = height;
            e.StrokeThickness = 4;
            e.Stroke = outer;
            e.Fill = inner;

            return e;
        }

        private Point getPositionInSector(int x, int y)
        {
            var p = new Point();
            //p.X = (int)((256m / 100) * ((((decimal)x / 192 + 135) * 100) % 100) * (SECTORSIZE / 256m));
            //Console.WriteLine(p.X);
            
            p.X = Math.Abs(x - (((int)((float)x / 192)) * 192)) / 1;
            if (x < 0 && p.X != 0)
                p.X = 192 - p.X;

            var curXSec = getSectorX(x);
            if (lastXSec != curXSec)
            {
                if (curXSec > lastXSec) p.X += (curXSec - lastXSec) * 192;
                else p.X -= (lastXSec - curXSec) * 192;
            }

            p.X *= (SECTOR_IMG_SIZE / 192f);
            //Console.WriteLine(p.X);


            //p.Y = (int)((256m / 100) * ((((decimal)y / 192 + 92) * 100) % 100) * (SECTORSIZE / 256m));
            //Console.WriteLine(p.Y);
            //p.Y = SECTORSIZE - p.Y;
            //Console.WriteLine(p.Y);

            p.Y = (Math.Abs(y - (((int)((float)y / 192)) * 192)) / 1) * /*(192f / SECTORSIZE)*/1;
            //Console.WriteLine(p.Y);
            if (y > 0 && p.Y != 0)
                p.Y = 192 - p.Y;

            var curYSec = getSectorY(y);
            if (lastYSec != curYSec)
            {
                if (curYSec > lastYSec) p.Y -= (curYSec - lastYSec) * 192;
                else p.Y += (lastYSec - curYSec) * 192;
            }

            p.Y *= (SECTOR_IMG_SIZE / 192f);
            //Console.WriteLine(p.Y);
            return p;
        }
        
        public static byte getSectorY(int y)
        {
            return (byte)Math.Floor((double)y / 192 + 92);
        }

        public static byte getSectorX(int x)
        {
            return (byte)Math.Floor((double)x / 192 + 135);
        }

        private int getSectorXOffset(int x, int xsec)
        {
            return ((x / 192) - xsec + 135) * 192 * 10;
        }

        private int getSectorXOffset(int x)
        {
            return getSectorXOffset(x, getSectorX(x));
        }

        private int getSectorYOffset(int y, int ysec)
        {
            return ((y / 192) - ysec + 92) * 192 * 10;
        }

        private int getSectorYOffset(int y)
        {
            return getSectorYOffset(y, getSectorY(y));
        }

        private System.Drawing.Point mapToIngame(int x, int y)
        {
            var p = new System.Drawing.Point();

            //Console.WriteLine("char pos: {0}/{1}", lastPosition.X, lastPosition.Y);

            var nullPosXIngame = ((lastXSec - 135) * 192);
            var nullPosYIngame = ((lastYSec - 92) * 192);

            //Console.WriteLine("nullpos: {0}/{1}", nullPosXIngame, nullPosYIngame);

            var xOffset = (int)((x - SECTOR_IMG_SIZE) * (192f / SECTOR_IMG_SIZE));
            var yOffset = (int)(((y - SECTOR_IMG_SIZE) * 1) * (192f / SECTOR_IMG_SIZE));
            yOffset = 192 - yOffset;

            var distX = nullPosXIngame + xOffset;
            var distY = nullPosYIngame + yOffset;

            p.X = distX;
            p.Y = distY;

            return p;
        }

        private void guiCanvas_map_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var clickPos = e.GetPosition(this);
            var ingamePos = mapToIngame((int)clickPos.X, (int)clickPos.Y);
            if (setTRainArea)
            {
                uint radius = 50;
                uint.TryParse(guiTextbox_trainradius.Text, out radius);

                bot.Config.TrainPlace.SetTrainArea(ingamePos, radius);
                bot.Config.Save();

                //Console.WriteLine("set trainarea: {0} -> {1}", bot.Config.TrainPlace.Middle, bot.Config.TrainPlace.Radius);

                redrawMap(lastPosition.X, lastPosition.Y);

                setTRainArea = false;
            }
            else if (recordScript)
            {
                walkScriptPoints.Add(ingamePos);
                redrawMap(lastPosition.X, lastPosition.Y);
            }
            else
            {
                bot.Debug("walk via map: {0}", ingamePos);
                Movement.WalkTo(bot, ingamePos.X, ingamePos.Y);
                Movement.WalkTo(bot, ingamePos.X, ingamePos.Y);
                Movement.WalkTo(bot, ingamePos.X, ingamePos.Y);
            }
        }

        private bool setTRainArea = false;
        private void guiBtn_setTrainarea_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;

            if (setTRainArea)
            {
                setTRainArea = false;
                btn.Content = "set trainarea";
            }
            else
            {
                setTRainArea = true;
                btn.Content = "cancel";
            }
        }

        private void guiCanvas_map_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            
        }

        private void guiBtn_up_Click(object sender, RoutedEventArgs e)
        {
            showCurrentCoords = false;
            redrawMap(lastPosition.X, lastPosition.Y + 192);
        }

        private void guiBtn_left_Click(object sender, RoutedEventArgs e)
        {
            showCurrentCoords = false;
            redrawMap(lastPosition.X - 192, lastPosition.Y);
        }

        private void button_down_Click(object sender, RoutedEventArgs e)
        {
            showCurrentCoords = false;
            redrawMap(lastPosition.X, lastPosition.Y - 192);
        }

        private void button_right_Click(object sender, RoutedEventArgs e)
        {
            showCurrentCoords = false;
            redrawMap(lastPosition.X + 192, lastPosition.Y);
        }

        private bool showCurrentCoords = true;

        private void guiBtn_current_Click(object sender, RoutedEventArgs e)
        {
            showCurrentCoords = true;
            redrawMap(bot.Char.CurPosition.X, bot.Char.CurPosition.Y);
        }

        private bool recordScript = false;
        private List<System.Drawing.Point> walkScriptPoints = new List<System.Drawing.Point>();

        private void button_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;

            if (recordScript)
            {
                recordScript = false;
                btn.Content = "record walkscript";
                foreach (var p in walkScriptPoints)
                {
                    Console.WriteLine("{0}", p);
                }
            }
            else
            {
                walkScriptPoints.Clear();
                recordScript = true;
                btn.Content = "stop recording";
            }
        }

        private void guiBtn_hotan_Click(object sender, RoutedEventArgs e)
        {
            showCurrentCoords = false;
            redrawMap(111, 14);
        }

        private void guiBtn_alex_south_Click(object sender, RoutedEventArgs e)
        {
            showCurrentCoords = false;
            redrawMap(-16641, -332);
        }

        private void guiBtn_goToXY_Click(object sender, RoutedEventArgs e)
        {
            var x = 0;
            var y = 0;
            if (!int.TryParse(guiTextbox_x.Text, out x) || !int.TryParse(guiTextbox_y.Text, out y)) return;

            showCurrentCoords = false;
            redrawMap(x, y);
        }

#if false

        
    Function mappointTOigCoord(ByVal point As Point) As sPos
        Dim tempPos As sPos
        tempPos.x = centerPos.x + (PictureBox1.Width / 2 - point.X) / sectorsize * 192 * -1
        tempPos.y = centerPos.y + (PictureBox1.Width / 2 - point.Y) / sectorsize * 192
        Return tempPos
    End Function

    Function igCoordTOmappoint(ByVal pos As sPos) As PointF
        Dim _point As PointF
        'Console.WriteLine(pos.x - centerPos.x & "  " & pos.y - centerPos.y & "  " & ((pos.x - centerPos.x) * sectorsize / 192 & "  " & (((pos.y - centerPos.y) * sectorsize / 192)) * -1))
        _point.X = (PictureBox1.Width / 2) + ((pos.x - centerPos.x) * sectorsize / 192)
        _point.Y = (PictureBox1.Width / 2) + (((pos.y - centerPos.y) * sectorsize / 192) * -1)
        Return _point
    End Function

    Dim test As sPos
    Private Sub bfree_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles bfree.Click
        Dim pos As sPos
        pos.x = -11273
        pos.y = 2651
        test = pos
        Me.reDrawmap(pos)
    End Sub




    Sub moveto(ByVal pos As sPos)
        'dest:1 opc:30520 len:9 data:01-4F-68-CB-04-50-00-2B-01
        Dim mySectorX, mySectorY As SByte
        Dim mySectorXoffset, mySectorYoffset As Int16
        mySectorX = Math.Floor(pos.x / 192 + 135)
        mySectorY = Math.Floor(pos.y / 192 + 92)
        Console.WriteLine((pos.x / 192) - mySectorX + 135)
        mySectorXoffset = ((pos.x / 192) - mySectorX + 135) * 192 * 10
        mySectorYoffset = ((pos.y / 192) - mySectorY + 92) * 192 * 10




        Dim packet As Byte() = {&H9, 0, &H38, &H77, 2, 0, _
                                1, _
                                mySectorX, _
                                mySectorY, _
                                0, 0, _
                                0, 0, _
                                0, 0}
        BitConverter.GetBytes(mySectorXoffset).CopyTo(packet, 9)
        BitConverter.GetBytes(CShort(pos.z)).CopyTo(packet, 11)
        BitConverter.GetBytes(mySectorYoffset).CopyTo(packet, 13)
        connection.sendpacket(packet)
        Console.WriteLine(" " & BitConverter.ToString(packet))
    End Sub

#endif
    }
}
