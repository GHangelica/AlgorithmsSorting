using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using SortVisualizer.Data;

namespace SortVisualizer
{
    public partial class ArrayInputDialog : Window
    {
        public int[] InputArray { get; private set; }
        public bool ShouldSaveToDatabase { get; private set; }
        public string DataSetName { get; private set; }

        public ArrayInputDialog()
        {
            InitializeComponent();
            InputTextBox.TextChanged += (s, e) => UpdatePreview();

            InputTextBox.KeyDown += (s, e) =>
            {
                if (e.Key == System.Windows.Input.Key.Enter)
                {
                    ApplyButton_Click(s, e);
                }
            };
        }

        private void UpdatePreview()
        {
            var result = ParseInput(InputTextBox.Text);
            if (result != null && result.Length > 0)
            {
                if (result.Length <= 30)
                {
                    PreviewText.Text = $"[{string.Join(", ", result)}]\n\n{result.Length} элементов";
                }
                else
                {
                    var first10 = result.Take(10);
                    var last10 = result.Skip(result.Length - 10);
                    PreviewText.Text = $"[{string.Join(", ", first10)} ... {string.Join(", ", last10)}]\n\n{result.Length} элементов";
                }
                PreviewText.Foreground = new SolidColorBrush(Color.FromRgb(46, 204, 113));
            }
            else
            {
                PreviewText.Text = "Некорректный ввод\n\nПодсказка: 1..20, random(50), или 64, 34, 25";
                PreviewText.Foreground = new SolidColorBrush(Color.FromRgb(231, 76, 60));
            }
        }

        private int[] ParseInput(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;

            input = input.Trim();

            if (input.Contains(".."))
            {
                var parts = input.Split(new[] { ".." }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 2 && int.TryParse(parts[0], out int start) && int.TryParse(parts[1], out int end))
                {
                    if (start <= end && start > 0 && end <= 1000)
                        return Enumerable.Range(start, end - start + 1).ToArray();
                }
            }

            if (input.StartsWith("random(", StringComparison.OrdinalIgnoreCase) && input.EndsWith(")"))
            {
                var inner = input.Substring(7, input.Length - 8);
                if (int.TryParse(inner, out int count) && count > 0 && count <= 200)
                {
                    Random rnd = new Random();
                    return Enumerable.Range(0, count).Select(x => rnd.Next(10, 100)).ToArray();
                }
            }

            var separators = new[] { ',', ' ', ';', '\t', '\n', '\r' };
            var numbers = input.Split(separators, StringSplitOptions.RemoveEmptyEntries);

            var result = new List<int>();
            foreach (var num in numbers)
            {
                if (int.TryParse(num, out int value) && value > 0 && value <= 200)
                {
                    result.Add(value);
                }
                else
                {
                    return null;
                }
            }

            if (result.Count > 200)
            {
                MessageBox.Show("Максимальное количество элементов - 200!", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }

            return result.Count > 0 ? result.ToArray() : null;
        }

        private void TestButton_Click(object sender, RoutedEventArgs e)
        {
            var result = ParseInput(InputTextBox.Text);
            if (result != null && result.Length > 0)
            {
                if (result.Length <= 30)
                {
                    MessageBox.Show($"Корректный ввод!\n\nМассив: [{string.Join(", ", result)}]\nКоличество: {result.Length}",
                        "Проверка", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    var first10 = result.Take(10);
                    var last10 = result.Skip(result.Length - 10);
                    MessageBox.Show($"Корректный ввод!\n\nМассив: [{string.Join(", ", first10)} ... {string.Join(", ", last10)}]\nКоличество: {result.Length}",
                        "Проверка", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                MessageBox.Show("Некорректный ввод!\n\nПримеры правильного ввода:\n• 64, 34, 25, 12\n• 1..20\n• random(50)",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            InputArray = ParseInput(InputTextBox.Text);
            if (InputArray != null && InputArray.Length > 0)
            {
                ShouldSaveToDatabase = false;
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Введите корректные данные!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void SaveToDBButton_Click(object sender, RoutedEventArgs e)
        {
            InputArray = ParseInput(InputTextBox.Text);
            if (InputArray == null || InputArray.Length == 0)
            {
                MessageBox.Show("Введите корректные данные!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DataSetName = DataSetNameBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(DataSetName))
            {
                MessageBox.Show("Введите название набора данных!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var existingDataSets = await Task.Run(() => DatabaseHelper.GetAllDataSets());
                if (existingDataSets.Any(ds => ds.Name.Equals(DataSetName, StringComparison.OrdinalIgnoreCase)))
                {
                    var result = MessageBox.Show(
                        $"Набор данных с именем \"{DataSetName}\" уже существует.\n\nЗаменить его новыми данными?",
                        "Подтверждение",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result != MessageBoxResult.Yes)
                        return;

                    var oldDataSet = existingDataSets.First(ds => ds.Name.Equals(DataSetName, StringComparison.OrdinalIgnoreCase));
                    await Task.Run(() => DatabaseHelper.DeleteDataSet(oldDataSet.Id));
                }

                await Task.Run(() => DatabaseHelper.CreateDataSet(DataSetName, InputArray.ToList()));

                ShouldSaveToDatabase = true;
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void QuickSorted_Click(object sender, RoutedEventArgs e)
        {
            InputTextBox.Text = "1..20";
            DataSetNameBox.Text = "Отсортированный 1..20";
            UpdatePreview();
        }

        private void QuickReversed_Click(object sender, RoutedEventArgs e)
        {
            InputTextBox.Text = "20, 19, 18, 17, 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1";
            DataSetNameBox.Text = "Обратный 20..1";
            UpdatePreview();
        }

        private void QuickRandom_Click(object sender, RoutedEventArgs e)
        {
            InputTextBox.Text = "random(20)";
            DataSetNameBox.Text = "Случайный 20 элементов";
            UpdatePreview();
        }

        private void QuickSame_Click(object sender, RoutedEventArgs e)
        {
            InputTextBox.Text = "42, 42, 42, 42, 42, 42, 42, 42, 42, 42";
            DataSetNameBox.Text = "Одинаковые значения";
            UpdatePreview();
        }

        private void QuickFew_Click(object sender, RoutedEventArgs e)
        {
            InputTextBox.Text = "15, 8, 23, 42, 4";
            DataSetNameBox.Text = "Несколько элементов";
            UpdatePreview();
        }

        private void QuickMany_Click(object sender, RoutedEventArgs e)
        {
            InputTextBox.Text = "random(100)";
            DataSetNameBox.Text = "Большой массив 100 элементов";
            UpdatePreview();
        }
    }
}