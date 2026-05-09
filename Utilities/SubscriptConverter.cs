namespace SimplexMethodApp.Utilities
{
    public class SubscriptConverter
    {
        public static string ToSubscript(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            return string.Create(input.Length, input, (span, state) =>
            {
                for (int i = 0; i < span.Length; i++)
                {
                    char originalChar = state[i];
                    char lowerC = char.ToLowerInvariant(originalChar);

                    span[i] = lowerC switch
                    {
                        '0' => '₀', '1' => '₁', '2' => '₂', '3' => '₃',
                        '4' => '₄', '5' => '₅', '6' => '₆', '7' => '₇',
                        '8' => '₈', '9' => '₉', 'a' => 'ₐ', 'e' => 'ₑ',
                        'h' => 'ₕ', 'i' => 'ᵢ', 'j' => 'ⱼ', 'k' => 'ₖ',
                        'l' => 'ₗ', 'm' => 'ₘ', 'n' => 'ₙ', 'o' => 'ₒ',
                        'p' => 'ₚ', 'r' => 'ᵣ', 's' => 'ₛ', 't' => 'ₜ',
                        'u' => 'ᵤ', 'v' => 'ᵥ', 'x' => 'ₓ',
                        _ => originalChar
                    };
                }
            });
        }

        public static string ToSubscript(int number)
        {
            return ToSubscript(number.ToString());
        }
    }
}
