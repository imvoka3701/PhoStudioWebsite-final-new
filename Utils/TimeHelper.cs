namespace PhoStudioMVC.Utils;

public static class TimeHelper
{
    public static DateTime VnNow =>
        TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.UtcNow,
            TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
}
