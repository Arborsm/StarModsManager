﻿namespace StarModsManager.Api;

public static class SdvDialogueFixer
{
    private const string SeparatorToken = "#";
    private const string GenderSwitchToken = "^";
    private const string GenericTextBoxToken = "%";
    private const string QuickResponseDelineatorToken = "*";
    private const string OtherToken = "$";
    private const string BracketTokenStart = "{{";
    private const string BracketTokenEnd = "}}";

    public static bool? IsMismatchedTokens(this Dictionary<string, string> defaultLang,
        Dictionary<string, string> targetLang)
    {
        var hasNullResult = false;

        foreach (var key in defaultLang.Keys)
        {
            targetLang.TryGetValue(key, out var value);
            var result = defaultLang[key].IsMismatchedTokens(value);

            if (result == null)
            {
                hasNullResult = true;
                continue;
            }

            if (result == true)
                return true;
        }

        return hasNullResult ? null : false;
    }

    public static bool? IsMismatchedTokens(this string? originalText, string? translatedText,
        bool isCheckDefaultLang = false)
    {
        if (string.IsNullOrEmpty(translatedText) || string.IsNullOrEmpty(originalText)) return null;
        var originalTokens = originalText.Split(SeparatorToken);
        var translatedTokens = translatedText.Split(SeparatorToken);

        if (isCheckDefaultLang && originalText.Contains("##")) return true;

        if (originalTokens.Length != translatedTokens.Length) return true;

        for (var i = 0; i < originalTokens.Length; i++)
        {
            var originalToken = originalTokens[i];
            var translatedToken = translatedTokens[i];

            var originalSymbolOrder = CacheSymbolOrder(originalToken);
            var translatedSymbolOrder = CacheSymbolOrder(translatedToken);

            if (AreOrdersEqual(originalSymbolOrder, translatedSymbolOrder)) continue;
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