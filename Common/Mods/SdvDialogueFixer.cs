namespace StarModsManager.Common.Mods;

internal static class SdvDialogueFixer
{
    private const string SeparatorToken = "#";
    private const string GenderSwitchToken = "^";
    private const string GenericTextBoxToken = "%";
    private const string QuickResponseDelineatorToken = "*";
    private const string OtherToken = "$";
    private const string BracketTokenStart = "{{";
    private const string BracketTokenEnd = "}}";

    public static bool IsMismatchedTokens(this Dictionary<string, string> defaultLang,
        Dictionary<string, string> targetLang)
    {
        return defaultLang.Keys.Any(key =>
        {
            targetLang.TryGetValue(key, out var value);
            return defaultLang[key].IsMismatchedTokens(value);
        });
    }

    public static bool IsMismatchedTokens(this string originalText, string? translatedText,
        bool isCheckDefaultLang = true)
    {
        if (string.IsNullOrEmpty(translatedText)) return false;
        var originalTokens = originalText.Split(SeparatorToken);
        var translatedTokens = translatedText.Split(SeparatorToken);

        if (isCheckDefaultLang && originalText.Contains("##"))
        {
            Console.WriteLine(@"Found ## in default lang file.");
            return true;
        }

        if (originalTokens.Length != translatedTokens.Length)
        {
            Console.WriteLine(@"Mismatched tokens found.");
            return true;
        }

        for (var i = 0; i < originalTokens.Length; i++)
        {
            var originalToken = originalTokens[i];
            var translatedToken = translatedTokens[i];

            var originalSymbolOrder = CacheSymbolOrder(originalToken);
            var translatedSymbolOrder = CacheSymbolOrder(translatedToken);

            if (AreOrdersEqual(originalSymbolOrder, translatedSymbolOrder)) continue;
            Console.WriteLine(
                @$"Token index: {i}, Original order: {string.Join(", ", originalSymbolOrder)}, Translated order: {string.Join(", ", translatedSymbolOrder)}");
            return true;
        }

        return false;
    }

    private static List<string> CacheSymbolOrder(string token)
    {
        var symbolOrder = new List<string>();
        string[] symbols = [GenderSwitchToken, GenericTextBoxToken, QuickResponseDelineatorToken, OtherToken];

        var index = 0;
        while (index < token.Length)
        {
            if (token.IndexOf(BracketTokenStart, index, StringComparison.Ordinal) == index)
            {
                var endIndex = token.IndexOf(BracketTokenEnd, index + BracketTokenStart.Length,
                    StringComparison.Ordinal);
                if (endIndex != -1)
                {
                    var fullToken = token.Substring(index, endIndex - index + BracketTokenEnd.Length);
                    symbolOrder.Add(fullToken);
                    index = endIndex + BracketTokenEnd.Length;
                    continue;
                }
            }

            var foundSymbol = false;
            foreach (var symbol in symbols)
            {
                if (token.IndexOf(symbol, index, StringComparison.Ordinal) != index) continue;
                symbolOrder.Add(symbol);
                index += symbol.Length;
                foundSymbol = true;
                break;
            }

            if (!foundSymbol) index++;
        }

        return symbolOrder;
    }

    private static bool AreOrdersEqual(List<string> originalOrder, List<string> translatedOrder)
    {
        if (originalOrder.Count != translatedOrder.Count)
            return false;

        return !originalOrder.Where((t, i) => t != translatedOrder[i]).Any();
    }
}