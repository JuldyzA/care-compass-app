namespace TeamYellow.ViewModels
{
    /// <summary>
    /// View model representing the manager dashboard page, including summary statistics
    /// and counsellor transaction data.
    /// </summary>
    public class ManagerDashboardPageVM
    {
        public DashboardStatsVM Stats { get; set; } = new();

        public List<ManagerDashboardVM> Counsellors { get; set; } = new();
    }
}
