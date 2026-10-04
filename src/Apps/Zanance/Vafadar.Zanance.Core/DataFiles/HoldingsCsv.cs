using System.Globalization;
using Vafadar.Zanance.Core.Holdings;
using Vafadar.Zanance.Core.Money;

namespace Vafadar.Zanance.Core.DataFiles;

/// <summary>A line of a holdings file that cannot be read, with the reason (a key such as <c>Date</c> or <c>Quantity</c>).</summary>
public sealed record HoldingsCsvError(int Line, string Error);

/// <summary>What a holdings file contains; ids are kept so that importing twice creates nothing twice.</summary>
public sealed record HoldingsCsvContent(
    IReadOnlyList<AssetType> Types,
    IReadOnlyList<AssetLocation> Locations,
    IReadOnlyList<AssetEvent> Events,
    IReadOnlyList<AssetValuation> Valuations,
    IReadOnlyList<HoldingsCsvError> Errors);

/// <summary>
/// The holdings file (ZEX-S0409): one CSV with a <c>record</c> column for asset types, locations, events and prices.
/// Quantities are written in grams or units with three decimals, amounts as invariant decimals in the type's price
/// currency and prices per gram or unit with three more decimals, so no precision is lost. Every row carries the format
/// version. The money entries of purchases and sales are in the entries file; both files share the group id.
/// </summary>
public static class HoldingsCsv
{
    /// <summary>The format version written in every row of every CSV file of this app version.</summary>
    public const string FormatVersion = "2";

    /// <summary>The columns of the holdings file.</summary>
    public static readonly IReadOnlyList<string> Columns =
    [
        "record", "id", "asset_type", "name", "kind", "dimension", "metal", "purity", "unit_weight_g", "count_unit", "divisible",
        "currency", "location", "to_location", "date", "quantity", "basis", "proceeds", "increase", "group_id", "reason", "note",
        "price_per_unit", "source", "archived", "format_version",
    ];

    /// <summary>Returns whether a header row is a holdings file.</summary>
    public static bool IsHoldingsFormat(IReadOnlyList<string> header)
    {
        ArgumentNullException.ThrowIfNull(header);
        return header.Count >= 3 && Columns.Take(3).Select((c, i) => string.Equals(header[i].Trim(), c, StringComparison.OrdinalIgnoreCase)).All(x => x);
    }

    /// <summary>Returns the CSV text of all holdings.</summary>
    public static string Write(IEnumerable<AssetType> types, IEnumerable<AssetLocation> locations, IEnumerable<AssetEvent> events, IEnumerable<AssetValuation> valuations, bool includeNotes)
    {
        ArgumentNullException.ThrowIfNull(types);
        ArgumentNullException.ThrowIfNull(locations);
        ArgumentNullException.ThrowIfNull(events);
        ArgumentNullException.ThrowIfNull(valuations);
        var typeList = types.ToList();
        var byId = typeList.ToDictionary(t => t.Id);
        var rows = new List<IReadOnlyList<string>> { Columns };
        string[] Row() => [.. Enumerable.Repeat(string.Empty, Columns.Count - 1), FormatVersion];
        string? Note(string? note) => includeNotes ? Csv.Text(note) : string.Empty;

        foreach (var type in typeList.OrderBy(t => t.SortOrder).ThenBy(t => t.Name, StringComparer.Ordinal))
        {
            var row = Row();
            row[0] = "type";
            row[1] = Id(type.Id);
            row[3] = Csv.Text(type.Name);
            row[4] = type.Kind.ToString();
            row[5] = type.Dimension.ToString();
            row[6] = type.Metal.ToString();
            row[7] = type.PurityPer10000?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            row[8] = type.UnitWeightMg is { } weight ? Quantity(weight) : string.Empty;
            row[9] = Csv.Text(type.CountUnitName);
            row[10] = type.Divisible ? "true" : "false";
            row[11] = type.PriceCurrencyCode;
            row[21] = Note(type.Note) ?? string.Empty;
            row[24] = type.IsArchived ? "true" : "false";
            rows.Add(row);
        }

        foreach (var location in locations.OrderBy(l => l.SortOrder).ThenBy(l => l.Name, StringComparer.Ordinal))
        {
            var row = Row();
            row[0] = "location";
            row[1] = Id(location.Id);
            row[3] = Csv.Text(location.Name);
            row[24] = location.IsArchived ? "true" : "false";
            rows.Add(row);
        }

        foreach (var assetEvent in events.OrderBy(e => e.Date).ThenBy(e => e.CreatedAt))
        {
            var currency = byId.TryGetValue(assetEvent.AssetTypeId, out var type) ? type.PriceCurrencyCode : string.Empty;
            var row = Row();
            row[0] = "event";
            row[1] = Id(assetEvent.Id);
            row[2] = Id(assetEvent.AssetTypeId);
            row[4] = assetEvent.Kind.ToString();
            row[11] = currency;
            row[12] = Id(assetEvent.LocationId);
            row[13] = assetEvent.ToLocationId is { } to ? Id(to) : string.Empty;
            row[14] = assetEvent.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            row[15] = Quantity(assetEvent.Quantity);
            row[16] = assetEvent.BasisAmount is { } basis ? Amount(basis, currency) : string.Empty;
            row[17] = assetEvent.ProceedsAmount is { } proceeds ? Amount(proceeds, currency) : string.Empty;
            row[18] = assetEvent.Kind == AssetEventKind.Correction ? (assetEvent.IsIncrease ? "true" : "false") : string.Empty;
            row[19] = assetEvent.GroupId is { } group ? Id(group) : string.Empty;
            row[20] = Csv.Text(assetEvent.Reason);
            row[21] = Note(assetEvent.Note) ?? string.Empty;
            rows.Add(row);
        }

        foreach (var valuation in valuations.OrderBy(v => v.Date))
        {
            var row = Row();
            row[0] = "valuation";
            row[1] = Id(valuation.Id);
            row[2] = Id(valuation.AssetTypeId);
            row[11] = valuation.CurrencyCode;
            row[14] = valuation.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            row[21] = Note(valuation.Note) ?? string.Empty;
            row[22] = PricePerUnit(valuation.PricePerUnitMilli, valuation.CurrencyCode);
            row[23] = valuation.Source.ToString();
            rows.Add(row);
        }

        return Csv.Write(rows);
    }

    /// <summary>
    /// Reads a holdings file. Rows that cannot be read are reported with their line and never guessed; references to
    /// types and locations must exist in the file or in <paramref name="knownTypes"/> and <paramref name="knownLocations"/>.
    /// </summary>
    public static HoldingsCsvContent Read(IReadOnlyList<IReadOnlyList<string>> rows, IReadOnlyCollection<AssetType> knownTypes, IReadOnlyCollection<AssetLocation> knownLocations)
    {
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(knownTypes);
        ArgumentNullException.ThrowIfNull(knownLocations);
        var types = new List<AssetType>();
        var locations = new List<AssetLocation>();
        var events = new List<AssetEvent>();
        var valuations = new List<AssetValuation>();
        var errors = new List<HoldingsCsvError>();
        var header = rows.Count > 0 ? rows[0].Select(h => h.Trim().ToLowerInvariant()).ToList() : [];
        var typeCurrency = knownTypes.ToDictionary(t => t.Id, t => t.PriceCurrencyCode);
        var locationIds = knownLocations.Select(l => l.Id).ToHashSet();

        // Types and locations first, so events may refer to them in any order of the file.
        var pending = new List<(int Line, Func<string, string> Cell)>();
        for (var i = 1; i < rows.Count; i++)
        {
            var cells = rows[i];
            string Cell(string name)
            {
                var index = header.IndexOf(name);
                return index >= 0 && index < cells.Count ? Csv.Unprotect(cells[index].Trim()) : string.Empty;
            }

            var line = i + 1;
            switch (Cell("record").ToLowerInvariant())
            {
                case "type":
                    if (ReadType(Cell) is { } type)
                    {
                        types.Add(type);
                        typeCurrency[type.Id] = type.PriceCurrencyCode;
                    }
                    else
                    {
                        errors.Add(new HoldingsCsvError(line, "Type"));
                    }

                    break;
                case "location":
                    if (Guid.TryParse(Cell("id"), out var locationId) && Empty(Cell("name")) is { } name)
                    {
                        locations.Add(new AssetLocation(locationId) { Name = name, IsArchived = Bool(Cell("archived")) });
                        locationIds.Add(locationId);
                    }
                    else
                    {
                        errors.Add(new HoldingsCsvError(line, "Location"));
                    }

                    break;
                case "event" or "valuation":
                    pending.Add((line, Cell));
                    break;
                default:
                    errors.Add(new HoldingsCsvError(line, "Record"));
                    break;
            }
        }

        foreach (var (line, cell) in pending)
        {
            var isEvent = string.Equals(cell("record"), "event", StringComparison.OrdinalIgnoreCase);
            var (item, error) = isEvent ? ReadEvent(cell, typeCurrency, locationIds) : ReadValuation(cell, typeCurrency);
            switch (item)
            {
                case AssetEvent assetEvent:
                    events.Add(assetEvent);
                    break;
                case AssetValuation valuation:
                    valuations.Add(valuation);
                    break;
                default:
                    errors.Add(new HoldingsCsvError(line, error ?? "Record"));
                    break;
            }
        }

        return new HoldingsCsvContent(types, locations, events, valuations, [.. errors.OrderBy(e => e.Line)]);
    }

    private static AssetType? ReadType(Func<string, string> cell)
    {
        if (!Guid.TryParse(cell("id"), out var id) || Empty(cell("name")) is not { } name || Empty(cell("currency")) is not { } currency
            || !TryEnum<AssetKind>(cell("kind"), out var kind) || !TryEnum<AssetDimension>(cell("dimension"), out var dimension))
        {
            return null;
        }

        var metal = TryEnum<Metal>(cell("metal"), out var parsedMetal) ? parsedMetal : Metal.None;
        int? purity = int.TryParse(cell("purity"), NumberStyles.None, CultureInfo.InvariantCulture, out var p) && p is > 0 and <= 10_000 ? p : null;
        long? weight = TryQuantity(cell("unit_weight_g"), out var mg) ? mg : null;
        return new AssetType(id)
        {
            Name = name,
            Kind = kind,
            Dimension = dimension,
            Metal = metal,
            PurityPer10000 = purity,
            UnitWeightMg = dimension == AssetDimension.Count ? weight : null,
            CountUnitName = Empty(cell("count_unit")),
            Divisible = Bool(cell("divisible")),
            PriceCurrencyCode = currency.ToUpperInvariant(),
            Note = Empty(cell("note")),
            IsArchived = Bool(cell("archived")),
        };
    }

    private static (object? Item, string? Error) ReadEvent(Func<string, string> cell, IReadOnlyDictionary<Guid, string> typeCurrency, IReadOnlySet<Guid> locations)
    {
        if (!Guid.TryParse(cell("id"), out var id))
        {
            return (null, "Id");
        }

        if (!Guid.TryParse(cell("asset_type"), out var typeId) || !typeCurrency.TryGetValue(typeId, out var currency))
        {
            return (null, "Type");
        }

        if (!TryEnum<AssetEventKind>(cell("kind"), out var kind))
        {
            return (null, "Kind");
        }

        if (!Guid.TryParse(cell("location"), out var location) || !locations.Contains(location))
        {
            return (null, "Location");
        }

        Guid? to = null;
        if (kind == AssetEventKind.LocationTransfer)
        {
            if (!Guid.TryParse(cell("to_location"), out var target) || !locations.Contains(target) || target == location)
            {
                return (null, "Location");
            }

            to = target;
        }

        if (!DateOnly.TryParseExact(cell("date"), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return (null, "Date");
        }

        if (!TryQuantity(cell("quantity"), out var quantity))
        {
            return (null, "Quantity");
        }

        long? basis = null, proceeds = null;
        if (Empty(cell("basis")) is { } basisText)
        {
            if (!CsvImport.TryAmount(basisText, currency, '.', out var value) || value < 0)
            {
                return (null, "Amount");
            }

            basis = value;
        }

        if (Empty(cell("proceeds")) is { } proceedsText)
        {
            if (!CsvImport.TryAmount(proceedsText, currency, '.', out var value) || value < 0)
            {
                return (null, "Amount");
            }

            proceeds = value;
        }

        return (new AssetEvent(id)
        {
            AssetTypeId = typeId,
            Kind = kind,
            LocationId = location,
            ToLocationId = to,
            Date = date,
            Quantity = quantity,
            BasisAmount = basis,
            ProceedsAmount = proceeds,
            IsIncrease = Bool(cell("increase")),
            GroupId = Guid.TryParse(cell("group_id"), out var group) ? group : null,
            Reason = Empty(cell("reason")),
            Note = Empty(cell("note")),
        }, null);
    }

    private static (object? Item, string? Error) ReadValuation(Func<string, string> cell, IReadOnlyDictionary<Guid, string> typeCurrency)
    {
        if (!Guid.TryParse(cell("id"), out var id))
        {
            return (null, "Id");
        }

        if (!Guid.TryParse(cell("asset_type"), out var typeId) || !typeCurrency.TryGetValue(typeId, out var currency))
        {
            return (null, "Type");
        }

        // A price is in the type's currency; a file never relabels it (FX-01).
        if (Empty(cell("currency")) is { } fileCurrency && !string.Equals(fileCurrency, currency, StringComparison.OrdinalIgnoreCase))
        {
            return (null, "Currency");
        }

        if (!DateOnly.TryParseExact(cell("date"), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return (null, "Date");
        }

        if (!TryPricePerUnit(cell("price_per_unit"), currency, out var milli))
        {
            return (null, "Amount");
        }

        return (new AssetValuation(id)
        {
            AssetTypeId = typeId,
            CurrencyCode = currency,
            Date = date,
            PricePerUnitMilli = milli,
            Source = TryEnum<ValuationSource>(cell("source"), out var source) ? source : ValuationSource.Manual,
            Note = Empty(cell("note")),
        }, null);
    }

    private static string Id(Guid id) => id.ToString("D", CultureInfo.InvariantCulture);

    // Base units (mg or thousandths) as grams or units with three decimals.
    private static string Quantity(long baseUnits) => (baseUnits / (decimal)Quantities.PerGramOrUnit).ToString("0.000", CultureInfo.InvariantCulture);

    private static bool TryQuantity(string text, out long baseUnits)
    {
        baseUnits = 0;
        if (!decimal.TryParse(text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var value) || value <= 0)
        {
            return false;
        }

        var exact = value * Quantities.PerGramOrUnit;
        if (exact != decimal.Truncate(exact) || exact > long.MaxValue)
        {
            return false;
        }

        baseUnits = (long)exact;
        return true;
    }

    private static string Amount(long minor, string currencyCode)
    {
        var currency = Currencies.TryGet(currencyCode, out var known) ? known : new Currency(currencyCode, 2);
        return MoneyAmount.ToDecimal(minor, currency).ToString("F" + currency.MinorDigits, CultureInfo.InvariantCulture);
    }

    // minor × 1,000 per gram or unit as a decimal amount with three more digits, e.g. 50.00000 EUR.
    private static string PricePerUnit(long milli, string currencyCode)
    {
        var currency = Currencies.TryGet(currencyCode, out var known) ? known : new Currency(currencyCode, 2);
        return (milli / 1_000m / currency.MinorFactor).ToString("F" + (currency.MinorDigits + 3), CultureInfo.InvariantCulture);
    }

    private static bool TryPricePerUnit(string text, string currencyCode, out long milli)
    {
        milli = 0;
        var currency = Currencies.TryGet(currencyCode, out var known) ? known : new Currency(currencyCode, 2);
        if (!decimal.TryParse(text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var value) || value <= 0)
        {
            return false;
        }

        var exact = value * currency.MinorFactor * 1_000m;
        if (exact != decimal.Truncate(exact) || exact > long.MaxValue)
        {
            return false;
        }

        milli = (long)exact;
        return true;
    }

    private static bool TryEnum<T>(string text, out T value)
        where T : struct, Enum =>
        Enum.TryParse(text, ignoreCase: true, out value) && Enum.IsDefined(value) && !int.TryParse(text, out _);

    private static bool Bool(string text) => string.Equals(text, "true", StringComparison.OrdinalIgnoreCase);

    private static string? Empty(string value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
