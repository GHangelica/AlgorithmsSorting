using System;

namespace SortVisualizer.Models
{
    public class SortReport
    {
        public int Id { get; set; }
        public string SortType { get; set; }
        public int ArraySize { get; set; }
        public int Comparisons { get; set; }
        public int Swaps { get; set; }
        public int DurationMs { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Status { get; set; }
        public string DataSetName { get; set; }

        public string DurationFormatted => $"{DurationMs} мс";
        public string StartTimeFormatted => StartTime.ToString("HH:mm:ss");
        public string EndTimeFormatted => EndTime.ToString("HH:mm:ss");
        public string Performance => $"{ArraySize} элементов за {DurationMs} мс";
    }
}