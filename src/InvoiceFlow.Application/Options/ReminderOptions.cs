namespace InvoiceFlow.Application.Options;

public class ReminderOptions
{
    public const string SectionName = "Reminders";

    public int CheckIntervalHours { get; set; } = 1;
    public int OverdueThresholdDays { get; set; } = 1;
    public int CooldownDays { get; set; } = 7;
    public int MaxAutoReminders { get; set; } = 2;
}
