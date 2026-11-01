using System.Globalization;

namespace ScheduleIStats;

public static class DisplayFormat
{
    // MelonLoader may run with globalization-invariant mode enabled.
    // A custom number provider works without requesting an installed culture.
    private static readonly NumberFormatInfo DollarNumbers = new()
    {
        NumberDecimalSeparator = ".",
        NumberGroupSeparator = ",",
        NumberGroupSizes = new[] { 3 },
        NumberDecimalDigits = 0
    };

    public static string Money(decimal value) => "$" + value.ToString("N0", DollarNumbers);
}


