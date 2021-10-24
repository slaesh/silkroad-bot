using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sroBot.SROBot.ItemStats
{
    public class WhiteStats
    {
        /*
         * Created by Daxter (2012) 
         * Credits to Stratti for his awesome code example
         */

        private ulong _value;
        public ulong Value
        {
            get
            {
                return _value;
            }
            set
            {
                _value = value;
                UpdateStats();
            }
        }

        private Types _type;
        public Types Type
        {
            get { return _type; }
            //set { _type = value; }
        }

        #region WhiteStats Fields

        //Weapon: Slot 0
        //Armor: Slot 0
        //Shield: Slot 0
        private byte _Durability;
        public byte Durability
        {
            get { return _Durability; }
            set
            {
                if (value <= 31) //less or equal 31
                {
                    _Durability = value;
                    UpdateValue();
                }
                else
                {
                    throw new System.ArgumentOutOfRangeException("value", "value must be less or equal to 31");
                }
            }
        }
        public byte DurabilityPercentage
        {
            get
            {
                return (byte)(_Durability * 100 / 31);
            }
            set
            {
                if (value <= 100)
                {
                    _Durability = (byte)(31f / 100f * value);
                    UpdateValue();
                }
                else
                {
                    throw new System.ArgumentOutOfRangeException("value", "value must be less or equal to 100");
                }
            }
        }

        //Weapon: Slot 3
        private byte _HitRatio;
        public byte HitRatio
        {
            get { return _HitRatio; }
            set
            {
                if (value <= 31) //less or equal 31
                {
                    _HitRatio = value;
                    UpdateValue();
                }
                else
                {
                    throw new System.ArgumentOutOfRangeException("value", "value must be less or equal to 31");
                }
            }
        }
        public byte HitRatioPercentage
        {
            get
            {
                return (byte)(_HitRatio * 100 / 31);
            }
            set
            {
                if (value <= 100)
                {
                    _HitRatio = (byte)(31f / 100f * value);
                    UpdateValue();
                }
                else
                {
                    throw new System.ArgumentOutOfRangeException("value", "value must be less or equal to 100");
                }
            }
        }

        //Armor: Slot 5
        private byte _ParryRatio;
        public byte ParryRatio
        {
            get { return _ParryRatio; }
            set
            {
                if (value <= 31) //less or equal 31
                {
                    _ParryRatio = value;
                    UpdateValue();
                }
                else
                {
                    throw new System.ArgumentOutOfRangeException("value", "value must be less or equal to 31");
                }
            }
        }
        public byte ParryRatioPercentage
        {
            get
            {
                return (byte)(_ParryRatio * 100 / 31);
            }
            set
            {
                if (value <= 100)
                {
                    _ParryRatio = (byte)(31f / 100f * value);
                    UpdateValue();
                }
                else
                {
                    throw new System.ArgumentOutOfRangeException("value", "value must be less or equal to 100");
                }
            }
        }

        //Shield: Slot 3
        private byte _BlockRatio;
        public byte BlockRatio
        {
            get { return _BlockRatio; }
            set
            {
                if (value <= 31) //less or equal 31
                {
                    _BlockRatio = value;
                    UpdateValue();
                }
                else
                {
                    throw new System.ArgumentOutOfRangeException("value", "value must be less or equal to 31");
                }
            }
        }
        public byte BlockRatioPercentage
        {
            get
            {
                return (byte)(_BlockRatio * 100 / 31);
            }
            set
            {
                if (value <= 100)
                {
                    _BlockRatio = (byte)(31f / 100f * value);
                    UpdateValue();
                }
                else
                {
                    throw new System.ArgumentOutOfRangeException("value", "value must be less or equal to 100");
                }
            }
        }

        //Weapon: Slot 6
        private byte _CriticalRatio;
        public byte CriticalRatio
        {
            get { return _CriticalRatio; }
            set
            {
                if (value <= 31) //less or equal 31
                {
                    _CriticalRatio = value;
                    UpdateValue();
                }
                else
                {
                    throw new System.ArgumentOutOfRangeException("value", "value must be less or equal to 31");
                }
            }
        }
        public byte CriticalRatioPercentage
        {
            get
            {
                return (byte)(_CriticalRatio * 100 / 31);
            }
            set
            {
                if (value <= 100)
                {
                    _CriticalRatio = (byte)(31f / 100f * value);
                    UpdateValue();
                }
                else
                {
                    throw new System.ArgumentOutOfRangeException("value", "value must be less or equal to 100");
                }
            }
        }

        //Weapon: Slot 1
        //Armor: Slot 1
        //Shield: Slot 1
        private byte _PhyReinforce;
        public byte PhyReinforce
        {
            get { return _PhyReinforce; }
            set
            {
                if (value <= 31) //less or equal 31
                {
                    _PhyReinforce = value;
                    UpdateValue();
                }
                else
                {
                    throw new System.ArgumentOutOfRangeException("value", "value must be less or equal to 31");
                }
            }
        }
        public byte PhyReinforcePercentage
        {
            get
            {
                return (byte)(_PhyReinforce * 100 / 31);
            }
            set
            {
                if (value <= 100)
                {
                    _PhyReinforce = (byte)(31f / 100f * value);
                    UpdateValue();
                }
                else
                {
                    throw new System.ArgumentOutOfRangeException("value", "value must be less or equal to 100");
                }
            }
        }

        //Weapon: Slot 2
        //Armor: Slot 2
        //Shield: Slot 2
        private byte _MagReinforce;
        public byte MagReinforce
        {
            get { return _MagReinforce; }
            set
            {
                if (value <= 31) //less or equal 31
                {
                    _MagReinforce = value;
                    UpdateValue();
                }
                else
                {
                    throw new System.ArgumentOutOfRangeException("value", "value must be less or equal to 31");
                }
            }
        }
        public byte MagReinforcePercentage
        {
            get
            {
                return (byte)(_MagReinforce * 100 / 31);
            }
            set
            {
                if (value <= 100)
                {
                    _MagReinforce = (byte)(31f / 100f * value);
                    UpdateValue();
                }
                else
                {
                    throw new System.ArgumentOutOfRangeException("value", "value must be less or equal to 100");
                }
            }
        }

        //Weapon: Slot 4
        private byte _PhyAttack;
        public byte PhyAttack
        {
            get { return _PhyAttack; }
            set
            {
                if (value <= 31) //less or equal 31
                {
                    _PhyAttack = value;
                    UpdateValue();
                }
                else
                {
                    throw new System.ArgumentOutOfRangeException("value", "value must be less or equal to 31");
                }
            }
        }
        public byte PhyAttackPercentage
        {
            get
            {
                return (byte)(_PhyAttack * 100 / 31);
            }
            set
            {
                if (value <= 100)
                {
                    _PhyAttack = (byte)(31f / 100f * value);
                    UpdateValue();
                }
                else
                {
                    throw new System.ArgumentOutOfRangeException("value", "value must be less or equal to 100");
                }
            }
        }


        //Weapon: Slot 5
        private byte _MagAttack;
        public byte MagAttack
        {
            get { return _MagAttack; }
            set
            {
                if (value <= 31) //less or equal 31
                {
                    _MagAttack = value;
                    UpdateValue();
                }
                else
                {
                    throw new System.ArgumentOutOfRangeException("value", "value must be less or equal to 31");
                }
            }
        }
        public byte MagAttackPercentage
        {
            get
            {
                return (byte)(_MagAttack * 100 / 31);
            }
            set
            {
                if (value <= 100)
                {
                    _MagAttack = (byte)(31f / 100f * value);
                    UpdateValue();
                }
                else
                {
                    throw new System.ArgumentOutOfRangeException("value", "value must be less or equal to 100");
                }
            }
        }

        //Armor: Slot 3
        //Shield: Slot 4
        private byte _PhyDefense;
        public byte PhyDefense
        {
            get { return _PhyDefense; }
            set
            {
                if (value <= 31) //less or equal 31
                {
                    _PhyDefense = value;
                    UpdateValue();
                }
                else
                {
                    throw new System.ArgumentOutOfRangeException("value", "value must be less or equal to 31");
                }
            }
        }
        public byte PhyDefensePercentage
        {
            get
            {
                return (byte)(_PhyDefense * 100 / 31);
            }
            set
            {
                if (value <= 100)
                {
                    _PhyDefense = (byte)(31f / 100f * value);
                    UpdateValue();
                }
                else
                {
                    throw new System.ArgumentOutOfRangeException("value", "value must be less or equal to 100");
                }
            }
        }

        //Armor: Slot 4
        //Shield: Slot 5
        private byte _MagDefense;
        public byte MagDefense
        {
            get { return _MagDefense; }
            set
            {
                if (value <= 31) //less or equal 31
                {
                    _MagDefense = value;
                    UpdateValue();
                }
                else
                {
                    throw new System.ArgumentOutOfRangeException("value", "value must be less or equal to 31");
                }
            }
        }
        public byte MagDefensePercentage
        {
            get
            {
                return (byte)(_MagDefense * 100 / 31);
            }
            set
            {
                if (value <= 100)
                {
                    _MagDefense = (byte)(31f / 100f * value);
                    UpdateValue();
                }
                else
                {
                    throw new System.ArgumentOutOfRangeException("value", "value must be less or equal to 100");
                }
            }
        }

        //Accessory: Slot 0
        private byte _PhyAbsorb;
        public byte PhyAbsorb
        {
            get { return _PhyAbsorb; }
            set
            {
                if (value <= 31) //less or equal 31
                {
                    _PhyAbsorb = value;
                    UpdateValue();
                }
                else
                {
                    throw new System.ArgumentOutOfRangeException("value", "value must be less or equal to 31");
                }
            }
        }
        public byte PhyAbsorbPercentage
        {
            get
            {
                return (byte)(_PhyAbsorb * 100 / 31);
            }
            set
            {
                if (value <= 100)
                {
                    _PhyAbsorb = (byte)(31f / 100f * value);
                    UpdateValue();
                }
                else
                {
                    throw new System.ArgumentOutOfRangeException("value", "value must be less or equal to 100");
                }
            }
        }

        //Accessory: Slot 1
        private byte _MagAbsorb;
        public byte MagAbsorb
        {
            get { return _MagAbsorb; }
            set
            {
                if (value <= 31) //less or equal 31
                {
                    _MagAbsorb = value;
                    UpdateValue();
                }
                else
                {
                    throw new System.ArgumentOutOfRangeException("value", "value must be less or equal to 31");
                }
            }
        }
        public byte MagAbsorbPercentage
        {
            get
            {
                return (byte)(_MagAbsorb * 100 / 31);
            }
            set
            {
                if (value <= 100)
                {
                    _MagAbsorb = (byte)(31f / 100f * value);
                    UpdateValue();
                }
                else
                {
                    throw new System.ArgumentOutOfRangeException("value", "value must be less or equal to 100");
                }
            }
        }

        #endregion

        public WhiteStats(Types type)
        {
            this._type = type;
            this._value = 0;
            UpdateStats();
        }

        public WhiteStats(Types type, ulong input)
        {
            this._type = type;
            this._value = input;
            UpdateStats();
        }

        public WhiteStats(Types type, params byte[] stats)
        {
            this._type = type;
            switch (_type)
            {
                case Types.Weapon:
                    _Durability = stats[0];
                    _PhyReinforce = stats[1];
                    _MagReinforce = stats[2];
                    _HitRatio = stats[3];
                    _PhyAttack = stats[4];
                    _MagAttack = stats[5];
                    _CriticalRatio = stats[6];
                    break;
                case Types.Equipment:
                    _Durability = stats[0];
                    _PhyReinforce = stats[1];
                    _MagReinforce = stats[2];
                    _PhyDefense = stats[3];
                    _MagDefense = stats[4];
                    _ParryRatio = stats[5];
                    break;
                case Types.Shield:
                    _Durability = stats[0];
                    _PhyReinforce = stats[1];
                    _MagReinforce = stats[2];
                    _BlockRatio = stats[3];
                    _PhyDefense = stats[4];
                    _MagDefense = stats[5];
                    break;
                case Types.Accessory:
                    _PhyAbsorb = stats[0];
                    _MagAbsorb = stats[1];
                    break;
            }
            UpdateValue();
        }

        private void UpdateStats()
        {
            ulong variance = this._value;
            byte counter = 0;
            switch (_type)
            {
                case Types.Weapon: //7 Slots           
                    #region Weapon
                    while (variance > 0)
                    {
                        byte stat = (byte)(variance & 0x1F);
                        switch (counter)
                        {
                            case 0: //Durability
                                _Durability = stat;
                                break;
                            case 1: //Physical Reinforce
                                _PhyReinforce = stat;
                                break;
                            case 2: //Magical Reinforce
                                _MagReinforce = stat;
                                break;
                            case 3: //Hit Ratio (Attack Ratio)
                                _HitRatio = stat;
                                break;
                            case 4: //Physical Attack
                                _PhyAttack = stat;
                                break;
                            case 5: //Magical Attack
                                _MagAttack = stat;
                                break;
                            case 6: //Critical Ratio
                                _CriticalRatio = stat;
                                break;
                        }

                        //left shit by 5
                        variance >>= 5;
                        counter++;
                    }
                    #endregion
                    break;
                case Types.Equipment: //6 Slots
                    #region Eqipment
                    while (variance > 0)
                    {
                        byte stat = (byte)(variance & 0x1F);
                        switch (counter)
                        {
                            case 0: //Durability
                                _Durability = stat;
                                break;
                            case 1: //Physical Reinforce
                                _PhyReinforce = stat;
                                break;
                            case 2: //Magical Reinforce
                                _MagReinforce = stat;
                                break;
                            case 3: //Physical Defense
                                _PhyDefense = stat;
                                break;
                            case 4: //Magical Defense
                                _MagDefense = stat;
                                break;
                            case 5: //Evasion Rate(Parry Rate)
                                _ParryRatio = stat;
                                break;
                        }

                        //left shit by 5
                        variance >>= 5;
                        counter++;
                    }
                    #endregion
                    break;
                case Types.Shield: //6 Slots
                    #region Shield
                    while (variance > 0)
                    {
                        byte stat = (byte)(variance & 0x1F);
                        switch (counter)
                        {
                            case 0: //Durability
                                _Durability = stat;
                                break;
                            case 1: //Physical Reinforce
                                _PhyReinforce = stat;
                                break;
                            case 2: //Magical Reinforce
                                _MagReinforce = stat;
                                break;
                            case 3: //Block Ratio
                                _BlockRatio = stat;
                                break;
                            case 4: //Physical Defense
                                _PhyDefense = stat;
                                break;
                            case 5://Magical Defense
                                _MagDefense = stat;
                                break;
                        }

                        //left shit by 5
                        variance >>= 5;
                        counter++;
                    }
                    #endregion
                    break;
                case Types.Accessory: //2 Slots
                    #region Shield
                    while (variance > 0)
                    {
                        byte stat = (byte)(variance & 0x1F);
                        switch (counter)
                        {
                            case 0: //Durability
                                _PhyAbsorb = stat;
                                break;
                            case 1: //Physical Reinforce
                                _MagAbsorb = stat;
                                break;
                        }

                        //left shit by 5
                        variance >>= 5;
                        counter++;
                    }
                    #endregion
                    break;
            }
        }

        private void UpdateValue()
        {
            ulong variance = 0;
            switch (_type)
            {
                case Types.Weapon:
                    variance |= _Durability;
                    variance <<= 5;
                    variance |= _PhyReinforce;
                    variance <<= 5;
                    variance |= _MagReinforce;
                    variance <<= 5;
                    variance |= _HitRatio;
                    variance <<= 5;
                    variance |= _PhyAttack;
                    variance <<= 5;
                    variance |= _MagAttack;
                    variance <<= 5;
                    variance |= _CriticalRatio;
                    break;
                case Types.Equipment:
                    variance |= _Durability;
                    variance <<= 5;
                    variance |= _PhyReinforce;
                    variance <<= 5;
                    variance |= _MagReinforce;
                    variance <<= 5;
                    variance |= _PhyDefense;
                    variance <<= 5;
                    variance |= _MagDefense;
                    variance <<= 5;
                    variance |= _ParryRatio;
                    break;
                case Types.Shield:
                    variance |= _Durability;
                    variance <<= 5;
                    variance |= _PhyReinforce;
                    variance <<= 5;
                    variance |= _MagReinforce;
                    variance <<= 5;
                    variance |= _BlockRatio;
                    variance <<= 5;
                    variance |= _PhyDefense;
                    variance <<= 5;
                    variance |= _MagDefense;
                    break;
                case Types.Accessory:
                    variance |= _PhyAbsorb;
                    variance <<= 5;
                    variance |= _MagAbsorb;
                    break;
            }
            _value = variance;
        }

        public override string ToString()
        {
            switch (_type)
            {
                case Types.Weapon:
                    return string.Format("Dura:{0}%, PhySpec:{1}%, MagSpec:{2}%, Hit:{3}%, PhyAtk:{4}%, MagAtk:{5}%, Crit:{6}%",
                        DurabilityPercentage, PhyReinforcePercentage, MagReinforcePercentage, HitRatioPercentage,
                        PhyAttackPercentage, MagAttackPercentage, CriticalRatioPercentage);
                case Types.Shield:
                    return string.Format("Dura:{0}%, PhySpec:{1}%, MagSpec:{2}%, Block:{3}%, PhyDef:{4}%, MagDef:{5}%",
                        DurabilityPercentage, PhyReinforcePercentage, MagReinforcePercentage, BlockRatioPercentage,
                        PhyDefensePercentage, MagDefensePercentage);
                case Types.Equipment:
                    return string.Format("Dura:{0}%, PhySpec:{1}%, MagSpec:{2}%, PhyDef:{3}%, MagDef:{4}%, ParryRate:{5}%",
                        DurabilityPercentage, PhyReinforcePercentage, MagReinforcePercentage, PhyDefensePercentage,
                        MagDefensePercentage, ParryRatioPercentage);
                case Types.Accessory:
                    return string.Format("PhyAbsorb:{0}%, MagAbsorb:{1}%", PhyAbsorbPercentage, MagAbsorbPercentage);
                default:
                    return string.Empty;
            }

        }

        //Replace this with your item class type
        public enum Types
        {
            Weapon,
            Equipment,
            Shield,
            Accessory
        }
    }
}
