namespace TeamYellow.ViewModels
{
    public class ManagerDashboardPageVM
    {
        public DashboardStatsVM Stats { get; set; } = new();

        public List<ManagerDashboardVM> Counsellors { get; set; } = new();
    }
}
