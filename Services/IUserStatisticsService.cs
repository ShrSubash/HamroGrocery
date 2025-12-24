namespace HamroGrocery.Services
{
    public interface IUserStatisticsService
    {
        Task UpdateUserStatisticsAsync(int userId);
        Task RecalculateAllUserStatisticsAsync();
    }
}