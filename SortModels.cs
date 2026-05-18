using System;
using System.Collections.Generic;

namespace SortVisualizer.Models
{
    public class DataSet
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<SortValue> Values { get; set; } = new List<SortValue>();
    }

    public class SortValue
    {
        public int Id { get; set; }
        public int DataSetId { get; set; }
        public int Value { get; set; }
        public int Position { get; set; }
    }

    public class SortResult
    {
        public int Id { get; set; }
        public int DataSetId { get; set; }
        public string SortType { get; set; }
        public int Comparisons { get; set; }
        public int Swaps { get; set; }
        public int DurationMs { get; set; }
        public DateTime ExecutedAt { get; set; }
    }
}