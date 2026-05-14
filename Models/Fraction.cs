namespace SimplexMethodApp.Models
{
    public readonly struct Fraction : IComparable<Fraction>
    {
        public long Numerator { get; }
        private readonly long _denominator;
        public long Denominator => _denominator == 0 ? 1 : _denominator;
        public bool IsZero => Numerator == 0;
        public bool IsNegative => Numerator < 0;
        public bool IsPositive => Numerator > 0;

        public static readonly Fraction Zero = new(0, 1);
        public static readonly Fraction One = new(1, 1);

        public Fraction(long numerator, long denominator = 1)
        {
            if (denominator == 0)
            {
                if (numerator == 0) { Numerator = 0; _denominator = 1; return; }
                throw new DivideByZeroException("Знаменник не може бути нулем");
            }

            if (denominator < 0) { numerator = -numerator; denominator = -denominator; }

            long gcd = Gcd(Math.Abs(numerator), denominator);
            Numerator = numerator / gcd;
            _denominator = denominator / gcd;
        }

        public static Fraction operator +(Fraction a, Fraction b)
            => new(a.Numerator * b.Denominator + b.Numerator * a.Denominator,
               a.Denominator * b.Denominator);
        public static Fraction operator -(Fraction a, Fraction b)
            => new(a.Numerator * b.Denominator - b.Numerator * a.Denominator,
                   a.Denominator * b.Denominator);
        public static Fraction operator *(Fraction a, Fraction b)
            => new(a.Numerator * b.Numerator, a.Denominator * b.Denominator);
        public static Fraction operator /(Fraction a, Fraction b)
            => new(a.Numerator * b.Denominator, a.Denominator * b.Numerator);
        public static Fraction operator -(Fraction a)
            => new(-a.Numerator, a.Denominator);
        public static bool operator ==(Fraction a, Fraction b)
            => a.Numerator == b.Numerator && a.Denominator == b.Denominator;
        public static bool operator !=(Fraction a, Fraction b) => !(a == b);
        public static bool operator <(Fraction a, Fraction b)
            => a.Numerator * b.Denominator < b.Numerator * a.Denominator;
        public static bool operator >(Fraction a, Fraction b) => b < a;
        public static bool operator <=(Fraction a, Fraction b) => !(a > b);
        public static bool operator >=(Fraction a, Fraction b) => !(a < b);

        public static implicit operator Fraction(int value) => new(value);
        public static implicit operator Fraction(long value) => new(value);

        public double ToDouble() => (double)Numerator / Denominator;

        public static Fraction FromDouble(double value, long maxDenominator = 1_000_000)
        {
            if (value == 0) return Zero;

            bool negative = value < 0;
            value = Math.Abs(value);

            long bestNum = 1, bestDen = 1;
            double bestError = double.MaxValue;

            // Stern–Brocot tree
            long lNum = 0, lDen = 1, rNum = 1, rDen = 0;
            for (int i = 0; i < 1000; i++)
            {
                long mNum = lNum + rNum;
                long mDen = lDen + rDen;
                if (mDen > maxDenominator) break;

                double error = Math.Abs((double)mNum / mDen - value);
                if (error < bestError) { bestError = error; bestNum = mNum; bestDen = mDen; }
                if (error < 1e-10) break;

                if ((double)mNum / mDen < value) { lNum = mNum; lDen = mDen; }
                else { rNum = mNum; rDen = mDen; }
            }

            return new Fraction(negative ? -bestNum : bestNum, bestDen);
        }

        public static bool TryParse(string input, out Fraction result)
        {
            result = Zero;
            if (string.IsNullOrWhiteSpace(input)) return false;

            input = input.Trim().Replace(',', '.');

            int slashIndex = input.IndexOf('/');
            if (slashIndex > 0)
            {
                if (long.TryParse(input[..slashIndex], out long num) &&
                    long.TryParse(input[(slashIndex + 1)..], out long den) &&
                    den != 0)
                {
                    result = new Fraction(num, den);
                    return true;
                }
                return false;
            }

            if (double.TryParse(input,
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out double d))
            {
                result = FromDouble(d);
                return true;
            }

            return false;
        }

        public int CompareTo(Fraction other) => (this < other) ? -1 : (this > other) ? 1 : 0;

        public override bool Equals(object? obj) => obj is Fraction f && this == f;

        public override int GetHashCode() => HashCode.Combine(Numerator, Denominator);

        public override string ToString()
        {
            if (Denominator == 1) return Numerator.ToString();
            return $"{Numerator}/{Denominator}";
        }

        private static long Gcd(long a, long b)
        {
            while (b != 0) { long t = b; b = a % b; a = t; }
            return a == 0 ? 1 : a;
        }
    }
}
