namespace SimplexMethodApp.Models.Simplex
{
    /// <summary>
    /// Regular + M * MCoefficient
    /// </summary>
    public readonly struct MValue
    {
        public double Regular { get; init; }
        public double MCoefficient { get; init; }

        public static readonly double Epsilon = 1e-9;

        public static readonly MValue Zero = new(0, 0);

        public MValue(double regular, double mCoefficient)
        {
            Regular = regular;
            MCoefficient = mCoefficient;
        }

        public static MValue operator +(MValue a, MValue b)
            => new(a.Regular + b.Regular, a.MCoefficient + b.MCoefficient);

        public static MValue operator -(MValue a, MValue b)
            => new(a.Regular - b.Regular, a.MCoefficient - b.MCoefficient);

        public static MValue operator *(MValue a, double scalar)
            => new(a.Regular * scalar, a.MCoefficient * scalar);

        public static MValue operator *(double scalar, MValue a)
            => a * scalar;

        public static MValue operator /(MValue a, double scalar)
            => new(a.Regular / scalar, a.MCoefficient / scalar);

        public static MValue operator -(MValue a)
            => new(-a.Regular, -a.MCoefficient);

        public static bool operator <(MValue a, MValue b)
        {
            double diffM = a.MCoefficient - b.MCoefficient;
            if (Math.Abs(diffM) > Epsilon) return diffM < 0;
            return a.Regular < b.Regular - Epsilon;
        }

        public static bool operator >(MValue a, MValue b)
            => b < a;

        public static implicit operator MValue(double value)
            => new(value, 0);

        public bool IsNegative 
            => this < Zero;
        public bool IsZero 
            => Math.Abs(Regular) < Epsilon && Math.Abs(MCoefficient) < Epsilon;

        public override string ToString() => ToDisplayString();

        public string ToDisplayString()
        {
            if (Math.Abs(Regular) < Epsilon)
            {
                if (Math.Abs(MCoefficient) < Epsilon) return "0";
                if (Math.Abs(MCoefficient - 1) < Epsilon) return "M";
                if (Math.Abs(MCoefficient + 1) < Epsilon) return "-M";
                return $"{FormatNumber(MCoefficient)}M";
            }

            if (Math.Abs(MCoefficient) < Epsilon)
                return FormatNumber(Regular);

            string mPart = MCoefficient > 0
                ? $"+{(Math.Abs(MCoefficient - 1) < Epsilon ? "" : FormatNumber(MCoefficient))}M"
                : $"-{(Math.Abs(MCoefficient + 1) < Epsilon ? "" : FormatNumber(-MCoefficient))}M";

            return $"{FormatNumber(Regular)}{mPart}";
        }

        private static string FormatNumber(double v)
            => v == Math.Floor(v) ? ((int)v).ToString() : $"{v:G6}";
    }
}
