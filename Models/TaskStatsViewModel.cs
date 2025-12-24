
namespace TaskManagerSystem.Models
{
    public class TaskStatsViewModel
    {
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int PendingTasks { get; set; }

        public int OverdueTasks { get; set; }

        public string[] Categories { get; set; }
        public int[] CategoryCounts { get; set; }
    }
}