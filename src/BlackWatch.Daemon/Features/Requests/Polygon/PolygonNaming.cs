using System;
using System.Text.RegularExpressions;

namespace BlackWatch.Daemon.Features.Requests.Polygon;

public static class PolygonNaming
{
    private static readonly Regex SymbolRegex = new(@"^\w+?\:(\w+)\w{3}$", RegexOptions.Compiled);

    public static string AdjustSymbol(string polygonSymbol)
    {
        var match = SymbolRegex.Match(polygonSymbol);
        if (match.Success == false)
        {
            throw new ArgumentException("value does not match required format", nameof(polygonSymbol));
        }

        return match.Groups[1].Value;
    }

    public static string ExtractCurrency(string polygonSymbol)
    {
        return polygonSymbol.Length > 3 ? polygonSymbol[^3..] : string.Empty;
    }
}
