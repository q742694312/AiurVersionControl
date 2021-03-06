﻿using System;

namespace SnakeGame.Models
{
    public class Position : ICloneable
    {
        public int X { get; set; }
        public int Y { get; set; }

        public override bool Equals(object obj)
        {
            if (obj is Position p)
            {
                return this.X == p.X && this.Y == p.Y;
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return this.X.GetHashCode() + this.Y.GetHashCode();
        }
        
        public override string ToString()
        {
            return X + "," + Y;
        }

        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }
}
