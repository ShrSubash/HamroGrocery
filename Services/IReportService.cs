using HamroGrocery.Models.ViewModels;
using System;
using System.Threading.Tasks;

namespace HamroGrocery.Services
{
    /// <summary>
    /// Service interface for generating reports and analytics
    /// Follows SOLID principles - Single Responsibility
    /// </summary>
    public interface IReportService
    {
        /// <summary>
        /// Generate End of Day Report for today's sales and operations
        /// </summary>
        /// <param name="reportDate">Date for the report (defaults to today)</param>
        /// <returns>End of Day Report ViewModel</returns>
        Task<EndOfDayReportViewModel> GetEndOfDayReportAsync(DateTime? reportDate = null);

        /// <summary>
        /// Generate Weekly Report for the last 7 days
        /// </summary>
        /// <param name="endDate">End date (defaults to today)</param>
        /// <returns>Weekly Report ViewModel</returns>
        Task<WeeklyReportViewModel> GetWeeklyReportAsync(DateTime? endDate = null);

        /// <summary>
        /// Generate Monthly Report for a specific month
        /// </summary>
        /// <param name="year">Year</param>
        /// <param name="month">Month (1-12)</param>
        /// <returns>Monthly Report ViewModel</returns>
        Task<MonthlyReportViewModel> GetMonthlyReportAsync(int? year = null, int? month = null);

        /// <summary>
        /// Generate Yearly Report for a specific year
        /// </summary>
        /// <param name="year">Year (defaults to current year)</param>
        /// <returns>Yearly Report ViewModel</returns>
        Task<YearlyReportViewModel> GetYearlyReportAsync(int? year = null);

        /// <summary>
        /// Generate Inventory Report showing current stock levels
        /// </summary>
        /// <returns>Inventory Report ViewModel</returns>
        Task<InventoryReportViewModel> GetInventoryReportAsync();

        /// <summary>
        /// Generate Sales Report for custom date range
        /// </summary>
        /// <param name="startDate">Start date</param>
        /// <param name="endDate">End date</param>
        /// <returns>Sales Report ViewModel</returns>
        Task<SalesReportViewModel> GetSalesReportAsync(DateTime startDate, DateTime endDate);

        /// <summary>
        /// Export Sales Report to Excel
        /// </summary>
        /// <param name="startDate">Start date</param>
        /// <param name="endDate">End date</param>
        /// <returns>Byte array of Excel file</returns>
        Task<byte[]> ExportSalesReportToExcelAsync(DateTime startDate, DateTime endDate);

        /// <summary>
        /// Export Inventory Report to Excel
        /// </summary>
        /// <returns>Byte array of Excel file</returns>
        Task<byte[]> ExportInventoryReportToExcelAsync();
    }
}