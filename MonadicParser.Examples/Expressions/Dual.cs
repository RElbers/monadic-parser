﻿using System;

 namespace MonadicParser.Examples.Expressions
{
    public readonly struct Dual : IEquatable<Dual>
    {
        public double Value { get; }
        public double Deriv { get; }

        public Dual(double value, double deriv)
        {
            Value = value;
            Deriv = deriv;
        }

        #region Operators

        public static Dual operator +(Dual a)
            => a;

        public static Dual operator -(Dual a)
            => new Dual(-a.Value, a.Deriv);

        public static Dual operator +(Dual a, Dual b)
            => new Dual(a.Value + b.Value, a.Deriv + b.Deriv);

        public static Dual operator -(Dual a, Dual b)
            => a + -b;

        public static Dual operator *(Dual a, Dual b)
            => new Dual(a.Value * b.Value,
                a.Value * b.Deriv + b.Value * a.Deriv);

        public static Dual operator /(Dual lhs, Dual rhs)
            => new Dual(lhs.Value / rhs.Value,
                (lhs.Value / rhs.Value) + (lhs.Deriv * rhs.Value - lhs.Value * rhs.Deriv) / (rhs.Value * rhs.Value));


        public static bool operator ==(Dual left, Dual right) => Equals(left, right);

        public static bool operator !=(Dual left, Dual right) => !Equals(left, right);

        #endregion Operators

        public override string ToString() => $"({Value}, {Deriv})";

        public bool Equals(Dual other) => Value.Equals(other.Value) && Deriv.Equals(other.Deriv);

        public override bool Equals(object? obj) => obj != null && Equals((Dual) obj);

        public override int GetHashCode() => HashCode.Combine(Value, Deriv);
    }
}