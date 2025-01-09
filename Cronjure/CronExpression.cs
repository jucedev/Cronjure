namespace Cronjure;

public class CronExpression
{
    private readonly HashSet<int> _minutes;
    private readonly HashSet<int> _hours;
    private readonly HashSet<int> _daysOfMonth;
    private readonly HashSet<int> _months;
    private readonly HashSet<int> _daysOfWeek;

    private CronExpression(
        HashSet<int> minutes, 
        HashSet<int> hours, 
        HashSet<int> daysOfMonth, 
        HashSet<int> months, 
        HashSet<int> daysOfWeek)
    {
        _minutes = minutes;
        _hours = hours;
        _daysOfMonth = daysOfMonth;
        _months = months;
        _daysOfWeek = daysOfWeek;
    }

    private bool IsMatch(DateTime date)
    {
        return _minutes.Contains(date.Minute) &&
               _hours.Contains(date.Hour) &&
               _months.Contains(date.Month) &&
               _daysOfMonth.Contains(date.Day) &&
               _daysOfWeek.Contains((int)date.DayOfWeek);
    }

    public DateTime GetNextOccurrence(DateTime date)
    {
        // Start looking from the next minute.
        var next = date.AddMinutes(1);
        
        next = new DateTime(next.Year, next.Month, next.Day, next.Hour, next.Minute, 0);

        while (!IsMatch(next))
        {
            // Try the next minute.
            next = next.AddMinutes(1);
            
            // Skip ahead when possible.
            if (!_hours.Contains(next.Hour))
            {
                next = next.AddHours(1).AddMinutes(-next.Minute);
                continue;
            }
            
            if (!_months.Contains(next.Month))
            {
                next = next.AddMonths(1)
                    .AddDays(-next.Day + 1)
                    .AddHours(-next.Hour)
                    .AddMinutes(-next.Minute);
            }
        }

        return next;
    }

    public static CronExpression Parse(string expression)
    {
        var parts = expression.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length != 5)
        {
            throw new FormatException("Cron expression must contain 5 arguments: minute, hour, day of month, month, day of week");
        }

        return new CronExpression(
            ParseField(parts[0], 0, 59), // minutes
            ParseField(parts[1], 0, 23), // hours
            ParseField(parts[2], 0, 59), // days of month
            ParseField(parts[3], 0, 59), // months
            ParseField(parts[4], 0, 59) // days of week (0 = sunday)
        );
    }

    private static HashSet<int> ParseField(string field, int min, int max)
    {
        var result = new HashSet<int>();

        // Handle asterisk first
        if (field == "*")
        {
            for (var i = min; i <= max; i++)
            {
                result.Add(i);
            }
            
            return result;
        }

        // Split the field into comma separated parts
        foreach (var part in field.Split(','))
        {
            if (part.Contains('/'))
            {
                // Step values - e.g. */5, 0-30/5
                ParseStepValue(part, min, max, result);
            }
            else if (part.Contains('-'))
            {
                // Ranges - e.g. 1-5
                ParseRange(part, min, max, result);
            }
            else
            {
                // Single Value
                if (int.TryParse(part, out var value))
                {
                    if (value < min || value > max)
                    {
                        throw new FormatException($"Value '{value}' is outside the valid range. Must be between {min} - {max}.");
                    }

                    result.Add(value);
                }
                else
                {
                    throw new FormatException($"Invalid value in Cron expression: {part}");
                }
            }
        }
        
        return result;
    }

    private static void ParseStepValue(string part, int min, int max, HashSet<int> result)
    {
        var stepParts = part.Split('/');

        if (stepParts.Length != 2)
        {
            throw new FormatException($"Invalid step value in Cron expression: {part}");
        }
        
        var range = stepParts[0] == "*" 
            ? (min, max) 
            : ParseRangeValues(stepParts[0], min, max);

        if (!int.TryParse(stepParts[1], out _))
        {
            throw new FormatException($"Invalid step value in Cron expression: {stepParts[1]}");
        }

        for (var i = range.Item1; i <= range.Item2; i++)
        {
            result.Add(i);
        }
    }

    private static void ParseRange(string part, int min, int max, HashSet<int> result)
    {
        var range = ParseRangeValues(part, min, max);
        
        for (var i = range.Item1; i <= range.Item2; i++)
        {
            result.Add(i);
        }
    }
    
    private static (int, int) ParseRangeValues(string part, int min, int max)
    {
        var rangeParts = part.Split('-');

        if (rangeParts.Length != 2)
        {
            throw new FormatException($"Invalid range value in Cron expression: {part}");
        }

        if (!int.TryParse(rangeParts[0], out var start) || !int.TryParse(rangeParts[1], out var end))
        {
            throw new FormatException($"Invalid range values in Cron expression: {part}");
        }

        if (start < min || end > max || start > end)
        {
            throw new FormatException($"Invalid range {start} - {end} in Cron expression for values between {min} - {max}.");
        }

        return (start, end);
    }
}