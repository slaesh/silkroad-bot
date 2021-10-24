using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace sroBot.SROBot
{
    public class Character : MVVM.ViewModelBase
    {
        public uint CharId = 0;
        public uint AccountId = 0;
        public String Name = "";
        public bool IsAlive = true;
        public byte Zerk = 0;
        public bool ZerkInUse = false;
        public float Speed = 0;
        public Point CurPosition = new Point();
        public Stack<Point> LastPositions = new Stack<Point>(20);
        public uint MaxHP
        {
            get { return GetValue(() => MaxHP); }
            set { SetValue(() => MaxHP, value); }
        }
        public uint MaxMP
        {
            get { return GetValue(() => MaxMP); }
            set { SetValue(() => MaxMP, value); }
        }
        public uint CurHP
        {
            get { return GetValue(() => CurHP); }
            set
            {
                SetValue(() => CurHP, value);
                if (MaxHP != 0)
                {
                    CurHpPercentage = CurHP * 100 / MaxHP;
                }
            }
        }
        public uint CurMP
        {
            get { return GetValue(() => CurMP); }
            set
            {
                SetValue(() => CurMP, value);
                if (MaxMP != 0)
                {
                    CurMpPercentage = CurMP * 100 / MaxMP;
                }
            }
        }
        public uint CurHpPercentage
        {
            get { return GetValue(() => CurHpPercentage); }
            set { SetValue(() => CurHpPercentage, value); }
        }
        public uint CurMpPercentage
        {
            get { return GetValue(() => CurMpPercentage); }
            set { SetValue(() => CurMpPercentage, value); }
        }
        public bool BadStatus = false;
        public byte Level
        {
            get { return GetValue(() => Level); }
            set { SetValue(() => Level, value); }
        }
        public byte MaxLevel = 0;
        public ushort RemainStatPoints = 0;
        public ushort STR = 0;
        public ushort INT = 0;
        public Mastery Masteries = new Mastery();
        public ulong Gold
        {
            get { return GetValue(() => Gold); }
            set { SetValue(() => Gold, value); }
        }
        public uint Model
        {
            get { return GetValue(() => Model); }
            set { SetValue(() => Model, value); }
        }
        public ulong EXP
        {
            get { return GetValue(() => EXP); }
            set { SetValue(() => EXP, value); EXPPercentage = (EXP * 100d) / SROData.ExpPoints.AtLevel[Level]; }
        }
        public double EXPPercentage
        {
            get { return GetValue(() => EXPPercentage); }
            set { SetValue(() => EXPPercentage, value); }
        }
        public ulong SP
        {
            get { return GetValue(() => SP); }
            set { SetValue(() => SP, value); }
        }
        public ulong Silk
        {
            get { return GetValue(() => Silk); }
            set { SetValue(() => Silk, value); }
        }
        public Pet Ridepet = null;
        public Pet Pickpet = null;
        public Pet Attackpet = null;

        // statistic stuff ..
        public UInt32 HighestDmg
        {
            get { return GetValue(() => HighestDmg); }
            set { SetValue(() => HighestDmg, value); }
        }

        public bool IsParsed = false;

        public Character()
        {
        }
        
    }
}
