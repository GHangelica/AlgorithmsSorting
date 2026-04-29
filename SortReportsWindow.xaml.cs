using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using SortVisualizer.Data;
using SortVisualizer.Models;

namespace SortVisualizer.Views
{
    public partial class SortReportsWindow : Window
    {
        private List<SortReport> allReports;

        public SortReportsWindow()
        {
            InitializeComponent();
            LoadReports();
        }

        private async void LoadReports()
        {
            try
            {
                var reports = await System.Threading.Tasks.Task.Run(() => DatabaseHelper.GetAllSortReports());
                allReports = reports;
                FilterReports();
                UpdateStatistics();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки отчетов: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FilterReports()
        {
            if (allReports == null) return;

            string filter = (FilterComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
            var filtered = allReports;

            if (filter != "Все сортировки")
            {
                filtered = allReports.Where(r => r.SortType == filter).ToList();
            }

            ReportsGrid.ItemsSource = filtered.OrderByDescending(r => r.StartTime);
        }

        private void FilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FilterReports();
        }

        private void UpdateStatistics()
        {
            TotalCountText.Text = allReports.Count.ToString();

            if (allReports.Count > 0)
            {
                var avgTime = allReports.Average(r => r.DurationMs);
                var bestTime = allReports.Min(r => r.DurationMs);
                var worstTime = allReports.Max(r => r.DurationMs);

                AvgTimeText.Text = $"{avgTime:F0} мс";
                BestTimeText.Text = $"{bestTime} мс";
                WorstTimeText.Text = $"{worstTime} мс";
            }
        }

        private async void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new Microsoft.Win32.SaveFileDialog();
                dialog.Filter = "CSV файл (*.csv)|*.csv";
                dialog.DefaultExt = "csv";
                dialog.FileName = $"SortReports_{DateTime.Now:yyyyMMdd_HHmmss}";

                if (dialog.ShowDialog() == true)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("Время начала,Тип сортировки,Размер массива,Сравнений,Обменов,Время(мс),Набор данных,Статус");

                    foreach (var report in allReports)
                    {
                        sb.AppendLine($"{report.StartTime:HH:mm:ss},{report.SortType},{report.ArraySize},{report.Comparisons},{report.Swaps},{report.DurationMs},{report.DataSetName},{report.Status}");
                    }

                    System.IO.File.WriteAllText(dialog.FileName, sb.ToString(), Encoding.UTF8);
                    MessageBox.Show($"Отчеты экспортированы!\n\n{dialog.FileName}", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Удалить все отчеты о сортировках?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                await System.Threading.Tasks.Task.Run(() => DatabaseHelper.ClearSortReports());
                LoadReports();
                MessageBox.Show("История очищена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}