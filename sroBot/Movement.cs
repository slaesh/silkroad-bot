using SilkroadSecurityApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sroBot.SROBot;
using System.Drawing;

namespace sroBot
{
    class Movement
    {
        public static void Move(Packet packet, Bot bot)
        {
            uint id = packet.ReadUInt32();
            if (id == bot.Char.CharId)
            {
                if (packet.ReadUInt8() == 1)
                {
                    byte xsec = packet.ReadUInt8();
                    byte ysec = packet.ReadUInt8();
                    float xcoord = 0;
                    float zcoord = 0;
                    float ycoord = 0;
                    if (ysec == 0x80)
                    {
                        xcoord = packet.ReadUInt16() - packet.ReadUInt16();
                        zcoord = packet.ReadUInt16() - packet.ReadUInt16();
                        ycoord = packet.ReadUInt16() - packet.ReadUInt16();
                    }
                    else
                    {
                        xcoord = packet.ReadUInt16();
                        zcoord = packet.ReadUInt16();
                        ycoord = packet.ReadUInt16();
                    }
                    int real_xcoord = 0;
                    int real_ycoord = 0;
                    if (xcoord > 32768)
                    {
                        real_xcoord = (int)(65536 - xcoord);
                    }
                    else
                    {
                        real_xcoord = (int)xcoord;
                    }
                    if (ycoord > 32768)
                    {
                        real_ycoord = (int)(65536 - ycoord);
                    }
                    else
                    {
                        real_ycoord = (int)ycoord;
                    }
                    
                    int x = CalculatePositionX(xsec, real_xcoord);
                    int y = CalculatePositionY(ysec, real_ycoord);

#if false
                    if (BotData.loop && BotData.loopaction == "go")
                    {
                        int dist = Math.Abs((x - Character.X)) + Math.Abs((y - Character.Y));
                        timer.Stop();
                        timer.Dispose();
                        timer = new Timer();
                        int time = Convert.ToInt32(dist * 10000 / Convert.ToInt64(Character.speed)) + 500;
                        timer.Interval = time;
                        timer.Start();
                        timer.Enabled = true;
                        timer.Elapsed += new ElapsedEventHandler(OnTick);
                    }
                    if (BotData.bot && BotData.loopaction == "randwalk")
                    {
                        int dist = Math.Abs((x - Character.X)) + Math.Abs((y - Character.Y));
                        timer.Stop();
                        timer.Dispose();
                        timer = new Timer();
                        int time = Convert.ToInt32(dist * 10000 / Convert.ToInt64(Character.speed)) + 1;
                        timer.Interval = time;
                        timer.Start();
                        timer.Enabled = true;
                        timer.Elapsed += new ElapsedEventHandler(OnTick);
                    }
                    if (BotData.loopaction == "record")
                    {
                        string text = "go," + x + "," + y;
                        Globals.MainWindow.script_record_box.Items.Add(text);
                    }
#endif

                    if (bot.Char.CurPosition.X != x || bot.Char.CurPosition.Y != y)
                    {
                        bot.Char.LastPositions.Push(new Point(bot.Char.CurPosition.X, bot.Char.CurPosition.Y));
                    }

                    bot.Char.CurPosition.X = x;
                    bot.Char.CurPosition.Y = y;
                    
                    bot.Spawns.RecalculateDistances(bot.Char.CurPosition);
                }
                else
                {
                    bot.Debug("moving with sky?");
                    bot.Debug(String.Join(", ", packet.GetBytes().Select(b => b.ToString("X2"))));
                }
            }
            else if (bot.Char.Ridepet != null && bot.Char.Ridepet.UID == id)
            {
                if (packet.ReadUInt8() == 1)
                {
                    byte xsec = packet.ReadUInt8();
                    byte ysec = packet.ReadUInt8();
                    float xcoord = 0;
                    float zcoord = 0;
                    float ycoord = 0;
                    if (ysec == 0x80)
                    {
                        xcoord = packet.ReadUInt16() - packet.ReadUInt16();
                        zcoord = packet.ReadUInt16() - packet.ReadUInt16();
                        ycoord = packet.ReadUInt16() - packet.ReadUInt16();
                    }
                    else
                    {
                        xcoord = packet.ReadUInt16();
                        zcoord = packet.ReadUInt16();
                        ycoord = packet.ReadUInt16();
                    }
                    int real_xcoord = 0;
                    int real_ycoord = 0;
                    if (xcoord > 33000)
                    {
                        real_xcoord = (int)(65352 - xcoord);
                    }
                    else
                    {
                        real_xcoord = (int)xcoord;
                    }
                    if (ycoord > 33000)
                    {
                        real_ycoord = (int)(65352 - ycoord);
                    }
                    else
                    {
                        real_ycoord = (int)ycoord;
                    }

                    int x = CalculatePositionX(xsec, real_xcoord);
                    int y = CalculatePositionY(ysec, real_ycoord);

                    bot.Char.CurPosition.X = x;
                    bot.Char.CurPosition.Y = y;

                    bot.Spawns.RecalculateDistances(bot.Char.CurPosition);
                }
            }
            else
            {
                if (packet.ReadUInt8() == 0x01)
                {
                    //var mob = bot.Spawns.Mobs.Get(id);
                    //if (mob == null) return;

                    var xsec = packet.ReadUInt8();
                    var ysec = packet.ReadUInt8();
                    
                    float xcoord = 0;
                    float ycoord = 0;
                    if (ysec == 0x80)
                    {
                        xcoord = packet.ReadUInt16() - packet.ReadUInt16();
                        packet.ReadUInt16();
                        packet.ReadUInt16();
                        ycoord = packet.ReadUInt16() - packet.ReadUInt16();
                    }
                    else
                    {
                        xcoord = packet.ReadUInt16();
                        packet.ReadUInt16();
                        ycoord = packet.ReadUInt16();
                    }

                    int x = CalculatePositionX(xsec, xcoord);
                    int y = CalculatePositionY(ysec, ycoord);
                    //int dist = Movement.GetDistance(x, bot.Char.CurPosition.X, y, bot.Char.CurPosition.Y);
                    //mob.X = x;
                    //mob.Y = y;
                    //mob.Distance = dist;
                    
                    bot.Spawns.UpdatePosition(id, x, y);
                    bot.Spawns.UpdatePosition(id, x, y);bot.Spawns.RecalculateDistances(bot.Char.CurPosition);
                }
            }
        }
  
        public static void WalkTo(Bot bot, int X, int Y)
        {
            if (bot == null || bot.Proxy == null) return;

            int xPos = 0;
            int yPos = 0;
            
            //if (X > 0 && Y > 0)
            //{
            //    xPos = (uint)((X % 192) * 10);
            //    yPos = (uint)((Y % 192) * 10);
            //}
            //else
            //{
            //    if (X < 0 && Y > 0)
            //    {
            //        xPos = (uint)((192 + (X % 192)) * 10);
            //        yPos = (uint)((Y % 192) * 10);
            //    }
            //    else
            //    {
            //        if (X > 0 && Y < 0)
            //        {
            //            xPos = (uint)((X % 192) * 10);
            //            yPos = (uint)((192 + (Y % 192)) * 10);
            //        }
            //    }
            //}
            
            byte xSector = DlgMiniMap.getSectorX(X);
            byte ySector = DlgMiniMap.getSectorY(Y);

            Packet packet;

            if (bot.Char.Ridepet == null)
            {
                packet = new Packet((ushort)SROData.Opcodes.CLIENT.MOVEMENT, true);
                packet.WriteUInt8(0x01);
            }
            else
            {
                packet = new Packet(0x70C5, true);
                packet.WriteUInt32(bot.Char.Ridepet.UID);
                packet.WriteUInt8(1);
                packet.WriteUInt8(1);
            }

            packet.WriteUInt8(xSector);
            packet.WriteUInt8(ySector);

            if (false) // if (bot.Char.IsInCave)
            {
                xSector = 0; // bot.Char.CaveFloor;
                ySector = 0x80;
                
                xPos = ((X - ((xSector - 135) * 192)) * 10);
                yPos = ((Y - ((ySector - 92) * 192)) * 10);
            }
            else
            {
                xPos = (ushort)((X - (xSector - 135) * 192) * 10);
                yPos = (ushort)((Y - (ySector - 92) * 192) * 10);
            }

            packet.WriteUInt16(xPos);
            packet.WriteUInt16(0);
            packet.WriteUInt16(yPos);

            bot.Proxy.SendToSilkroadServer(packet);
        }

        public static void WalkTo(Bot bot, Point p)
        {
            WalkTo(bot, p.X, p.Y);
        }

        public static int CalculatePositionX(ushort xSector, float X)
        {
            return (int)((xSector - 135) * 192 + X / 10);
        }

        public static int CalculatePositionY(ushort ySector, float Y)
        {
            return (int)((ySector - 92) * 192 + Y / 10);
        }

        public static int GetDistance(int x1, int x2, int y1, int y2)
        {
            //return Math.Abs(x1 - x2) + Math.Abs(y1 - y2); // faster and the same?? -- no !!
            return (int)Math.Sqrt(Math.Pow(x1 - x2, 2.0) + Math.Pow(y1 - y2, 2.0));
        }

        public static int GetDistance(Point p1, Point p2)
        {
            return GetDistance(p1.X, p2.X, p1.Y, p2.Y);
        }

        public static uint CalculateTime(int distance, float speed)
        {
            return Convert.ToUInt32(distance * 10000 / Convert.ToInt64(speed)) + 1;
        }

        public static uint CalculateTime(Point p1, Point p2, float speed)
        {
            return CalculateTime(GetDistance(p1, p2), speed);
        }
    }
}
