using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sroBot.SROBot
{
    public static class ConcurrentBagExtensions
    {
        public static int IndexOf<T>(this ConcurrentBag<T> cb, T obj)
        {
            var idx = 0;
            while (idx < cb.Count)
            {
                if (cb.ElementAt(idx).Equals(obj)) return idx;
                ++idx;
            }
            return -1;
        }
    }

    public partial class Loop
    {
        private long goToTrainplaceTimer = 0;
        private ConcurrentBag<String> trainplaceScript = new ConcurrentBag<String>();
        private ushort goToTrainplaceIdx = 0;

        public bool LoadPathToTrainplace(String script = "")
        {
            if (String.IsNullOrEmpty(script)) script = Path.Combine(App.ExecutingPath, "scripts", bot.Config.TrainPlace.WalkingScript);

            bot.Log("load walkscript: {0}", System.IO.Path.GetFileName(script));
            if (!File.Exists(script))
            {
                bot.Log("could not find walkscript: {0}", script);
                Stop(Statistic.STOP_REASON.INVALID_WALKSCRIPT);
                return false;
            }

            goToTrainplaceIdx = 0;
            trainplaceScript = new ConcurrentBag<String>();
            using (var sr = new StreamReader(script))
            {
                while (!sr.EndOfStream)
                {
                    var line = sr.ReadLine();
                    try
                    {
                        var splitted = line.Split(';');
                        trainplaceScript.Add(line);
                    }
                    catch { }
                }
                trainplaceScript = new ConcurrentBag<string>(trainplaceScript);
            }

            return true;
        }

        private bool usingReverseReturnScroll = false;
        public void StartGoToTrainplace(int startIdx = 0)
        {
            if (!LoadPathToTrainplace()) return;

            //if (bot.Config.HalloweenEventSpecial)
            //{
            //    if (usingReverseReturnScroll)
            //    {
            //        usingReverseReturnScroll = false;

            //        loopState = LOOP_AREAS.Trainplace;
            //        trainState = LOOP_TRAINING_STATES.Attacking;

            //        return;
            //    }

            if (bot.Config.Loop.UseReverseReturnToLastDead)
            {
                var revRetScroll = bot.Inventory.GetItem("Reverse Return");
                if (revRetScroll != null)
                {
                    Actions.UseReverseReturnScroll(revRetScroll.Slot, 3 /* last place where died */, bot);

                    bot.Log("use reverse scroll @ slot {0}", revRetScroll.Slot);
                    usingReverseReturnScroll = true;

                    LoopState = LOOP_AREAS.Trainplace;
                    TrainState = LOOP_TRAINING_STATES.WalkingToTrainplace;

                    return;
                }
                else
                {
                    bot.Log("got no reverse return scroll..");
                }
            }

            startStatistic();

            goToTrainplaceTimer = 1000;
            goToTrainplaceIdx = (ushort)(startIdx >= 0 ? startIdx : 0);
            LoopState = LOOP_AREAS.Trainplace;
            TrainState = LOOP_TRAINING_STATES.WalkingToTrainplace;

            IsStarted = true;
        }

        public int IsOnMyRoute(Point curP)
        {
            var nearestPoint = trainplaceScript.Where(s => s.StartsWith("go;")).Select(s =>
            {
                var splitted = s.Replace("go;", "").Split(',');
                return new Point(Convert.ToInt32(splitted[0]), Convert.ToInt32(splitted[1]));
            }).LastOrDefault(p => Movement.GetDistance(curP.X, p.X, curP.Y, p.Y) < 25);

            if (nearestPoint.X == 0 && nearestPoint.Y == 0) return -1;
            return trainplaceScript.IndexOf(String.Format("go;{0},{1}", nearestPoint.X, nearestPoint.Y));
        }

        private void handleTeleport(string town)
        {
            UInt32 portalId = 0;
            UInt32 teleportDestination = 0;
            
            // sometimes we are not to close to get it as a spawn ..
            switch (curTown)
            {
                case "jangan":
                    
                    break;

                case "downhang":
                    portalId = 42;
                    break;

                case "hotan":
                    portalId = 25;
                    break;
            }

            switch (town)
            {
                case "jangan":
                    teleportDestination = 1;
                    break;

                case "downhang":
                    teleportDestination = 2;
                    break;

                case "hotan":
                    teleportDestination = 5;
                    break;

                case "baghdad":
                    teleportDestination = 250;
                    break;

                case "alex south":
                    teleportDestination = 175;
                    break;

                case "storm and cloud desert":
                    teleportDestination = 180;
                    break;

                case "phantom desert":
                    teleportDestination = 256;
                    break;

                default:
                    bot.Log("dont know this teleport destination: {0}", town);
                    Stop(Statistic.STOP_REASON.INVALID_TELEPORT_DESTINATION);
                    break;
            }

            if (teleportDestination != 0)
            {
                var portal = bot.Spawns.Gates.GetAll().FirstOrDefault(g => g.Links.Contains(teleportDestination));
                if (portal != null || portalId != 0)
                {
                    bot.Log($"teleport to {town}");

                    Actions.Teleport(portal?.IngameId ?? portalId, 2, teleportDestination, bot);
                    bot.IsUsingReturnScroll = true;
                    goToTrainplaceTimer = 60000;
                }
                else
                {
                    bot.Log("found no portal with this destination: {0}", teleportDestination);
                    Stop(Statistic.STOP_REASON.INVALID_TELEPORT_DESTINATION);
                }
            }
        }

        private void castSpeedBuff()
        {
            var speedBuff = bot.GetAvailableSkills().Where(s =>
                    (s.Name.StartsWith("Grass Walk") ||
                    s.Name.StartsWith("Ghost Walk")) &&
                    s.Name.IndexOf("phantom", StringComparison.OrdinalIgnoreCase) < 0 &&
                    s.Name.IndexOf("shadow", StringComparison.OrdinalIgnoreCase) < 0 &&
                    s.Name.IndexOf("immediate", StringComparison.OrdinalIgnoreCase) < 0
                    ).OrderBy(s => s.RequiredMastery1Level).LastOrDefault();

            if (speedBuff != null && !bot.GetActiveBuffs().Any(b => b.Name == speedBuff.Name))
            {
                Actions.CastBuff(speedBuff.Model, bot);
            }
        }

        private void handleGo(string args)
        {
            castSpeedBuff();

            var splittedArgs = args.Split(',');
            var nextPoint = new Point(Convert.ToInt32(splittedArgs[0]), Convert.ToInt32(splittedArgs[1]));

            if (Movement.GetDistance(nextPoint, bot.Char.CurPosition) >= 60)
            {
                bot.Log("trainplacescript: walk to {0} is to far away!! (>= 60) from my position: {1}", nextPoint, bot.Char.CurPosition);

                if (!bot.UseReturnScroll())
                {
                    bot.Log("trainplacescript: could not use return scroll, stop bot!");
                    Stop(Statistic.STOP_REASON.NO_RETURNSCROLL);
                }
                else
                {
                    TrainState = LOOP_TRAINING_STATES.Attacking;
                }

                //nextPoint = new Point((nextPoint.X + bot.Char.CurPosition.X) / 2, (nextPoint.Y + bot.Char.CurPosition.Y) / 2);
                //bot.Log("trainplacescript: go to MIDPOINT: {0}", nextPoint);
                //--goToTrainplaceIdx;

                //if (Movement.GetDistance(nextPoint, bot.Char.CurPosition) >= 60)
                //{
                //    bot.Log("trainplacescript: still to far! stop botting!", nextPoint);
                //    Stop();
                //}
            }
            else
            {
                Movement.WalkTo(bot, nextPoint.X, nextPoint.Y);
                Movement.WalkTo(bot, nextPoint.X, nextPoint.Y);
                Movement.WalkTo(bot, nextPoint.X, nextPoint.Y);

                //bot.Debug("{0} | trainplacescript: walk to {1}", DateTime.Now.ToString("HH:mm:ss.fff"), nextPoint);

                var dist = Movement.GetDistance(nextPoint.X, bot.Char.CurPosition.X, nextPoint.Y, bot.Char.CurPosition.Y);
                var time = Movement.CalculateTime(dist, bot.Char.Speed);

                time = (uint)(time * 0.9);

                goToTrainplaceTimer = time;
            }
        }

        public void CheckGoToTrainplace()
        {
            if (goToTrainplaceTimer > 0)
            {
                goToTrainplaceTimer -= 100;
                if (goToTrainplaceTimer > 0) return;

                if (TrainState != LOOP_TRAINING_STATES.WalkingToTrainplace) return;
                if (goToTrainplaceIdx >= trainplaceScript.Count)
                {
                    if (bot.Config.Training.StopBotOnTrainplace)
                    {
                        bot.Log("STOP ON TRAINPLACE !");
                        Stop(Statistic.STOP_REASON.STOP_ON_TRAINPLACE);
                        return;
                    }

                    TrainState = LOOP_TRAINING_STATES.Attacking;

                    if (bot.Config.GetBuffingSkills().Any())
                    {
                        checkAttackingTimer = 25000; // get some time for buffing ! ;)
                    }

                    bot.Log("trainplacescript: start attacking");
                    return;
                }

                if (!bot.Char.IsAlive)
                {
                    goToTrainplaceTimer = 2000;
                    return;
                }

                var nextStep = trainplaceScript.ElementAt(goToTrainplaceIdx++);

                try
                {
                    var cmdNarg = nextStep.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                    var cmd = cmdNarg[0].ToLower();
                    var args = String.Join(";", cmdNarg.Skip(1)).ToLower();

                    //bot.Debug(nextStep);

                    switch (cmd)
                    {
                        case "go":
                            handleGo(args);
                            break;

                        case "teleport":
                            handleTeleport(args);
                            break;

                        case "setradius":
                            {
                                var radius = Convert.ToUInt16(args);

                                bot.Config.TrainPlace.SetTrainArea(bot.Char.CurPosition, radius);
                                bot.Config.Save();

                                //bot.Debug("{0} | trainplacescript: set radius {1}", DateTime.Now.ToString("HH:mm:ss.fff"), radius);
                            }
                            break;

                        case "setpolygon":
                            {
                                var polyPoints = new List<Point>();
                                try
                                {
                                    var points = cmdNarg.Skip(1).Select(a =>
                                    {
                                        var aSplitted = a.Split(',');
                                        return new Point(Convert.ToInt32(aSplitted[0]), Convert.ToInt32(aSplitted[1]));
                                    });

                                    bot.Config.TrainPlace.SetTrainArea(points);
                                    bot.Config.Save();

                                    //bot.Debug("{0} | trainplacescript: set polygon {1}", DateTime.Now.ToString("HH:mm:ss.fff"), String.Join("; ", points));
                                }
                                catch { }
                            }
                            break;

                        default:
                            bot.Debug("{0} | trainplacescript: unknown cmd: {1}", DateTime.Now.ToString("HH:mm:ss.fff"), cmd);
                            break;
                    }


                }
                catch { goToTrainplaceTimer = 300; }

                if (goToTrainplaceTimer <= 0)
                    goToTrainplaceTimer = 300;

            }
        }
    }
}
