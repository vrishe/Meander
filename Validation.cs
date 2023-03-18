namespace Meander;

internal static class Validation
{
    public static bool CheckIsPositiveInteger(string text)
    {
        if (string.IsNullOrEmpty(text)) return false;

        var allZeroes = true;
        foreach (var c in text)
        {
            if (!char.IsDigit(c)) return false;
            allZeroes &= c == '0';
        }

        return !allZeroes;
    }
}
