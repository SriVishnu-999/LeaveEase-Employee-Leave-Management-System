namespace LeaveEase.Api.Services;

public interface ILeavePolicyService
{
    decimal CalculateWorkingDays(DateOnly startDate, DateOnly endDate);
}

public class LeavePolicyService : ILeavePolicyService
{
    public decimal CalculateWorkingDays(DateOnly startDate, DateOnly endDate)
    {
        if (endDate < startDate)
            return 0;

        decimal days = 0;
        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            if (date.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday))
                days++;
        }

        return days;
    }
}
