using System.Globalization;

namespace ScheduleIStats;

public static class DisplayFormat
{
    // MelonLoader may run with globalization-invariant mode enabled.
    // A custom number provider works without requesting an installed culture.
    private static readonly NumberFormatInfo SpanishNumbers = new()
    {
        NumberDecimalSeparator = ",",
        NumberGroupSeparator = ".",
        NumberGroupSizes = new[] { 3 },
        NumberDecimalDigits = 2
    };

    public static string Money(decimal value) => "$" + value.ToString("N2", SpanishNumbers);
}
