namespace SimplexMethodApp.Models.Simplex
{
    /// <summary>
    /// Regular + M * MCoefficient
    /// </summary>
    public readonly struct MValue
    {
        public Fraction Regular { get; init; }
        public Fraction MCoefficient { get; init; }

        public static readonly double Epsilon = 1e-9;

        public static readonly MValue Zero = new(Fraction.Zero, Fraction.Zero);

        public MValue(Fraction regular, Fraction mCoefficient)
        {
            Regular = regular;
            MCoefficient = mCoefficient;
        }

        public MValue(Fraction regular)
        {
            Regular = regular;
            MCoefficient = Fraction.Zero;
        }

        public static MValue operator +(MValue a, MValue b)
            => new(a.Regular + b.Regular, a.MCoefficient + b.MCoefficient);

        public static MValue operator -(MValue a, MValue b)
            => new(a.Regular - b.Regular, a.MCoefficient - b.MCoefficient);

        public static MValue operator *(MValue a, Fraction scalar)
            => new(a.Regular * scalar, a.MCoefficient * scalar);

        public static MValue operator *(Fraction scalar, MValue a)
            => a * scalar;

        public static MValue operator /(MValue a, Fraction scalar)
            => new(a.Regular / scalar, a.MCoefficient / scalar);

        public static MValue operator -(MValue a)
            => new(-a.Regular, -a.MCoefficient);

        public static bool operator <(MValue a, MValue b)
        {
            Fraction diffM = a.MCoefficient - b.MCoefficient;
            if (!diffM.IsZero) return diffM.IsNegative;
            return (a.Regular - b.Regular).IsNegative;
        }

        public static bool operator >(MValue a, MValue b)
            => b < a;

        //public static implicit operator MValue(double value)
        //    => new(Fraction.FromDouble(value));

        public static implicit operator MValue(Fraction f) => new(f);
        public static implicit operator MValue(int v) => new(new Fraction(v));

        public bool IsZero => Regular.IsZero && MCoefficient.IsZero;
        public bool IsNegative => this < Zero;

        public override string ToString()
        {
            if (MCoefficient.IsZero) return Regular.ToString();
            if (Regular.IsZero)
            {
                if (MCoefficient == Fraction.One) return "M";
                if (MCoefficient == -Fraction.One) return "-M";
                return $"{MCoefficient}M";
            }

            string mPart = MCoefficient.IsNegative
                ? $" - {(-MCoefficient)}M"
                : $" + {MCoefficient}M";

            return $"{Regular}{mPart}";
        }

        private static string FormatNumber(double v)
            => v == Math.Floor(v) ? ((int)v).ToString() : $"{v:G6}";
    }
}
