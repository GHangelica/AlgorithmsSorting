using SortVisualizer.Data;
using SortVisualizer.Models;
using SortVisualizer.Views;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace SortVisualizer
{
    public partial class MainWindow : Window
    {
        private List<int> currentData;
        private List<Rectangle> bars = new List<Rectangle>();
        private bool isSorting = false;
        private bool isPaused = false;
        private bool stopRequested = false;
        private int comparisons = 0;
        private int swaps = 0;
        private int currentDataSetId = 0;

        public MainWindow()
        {
            InitializeComponent();
            DatabaseHelper.CreateSortReportsTableIfNotExists();

            DelaySlider.ValueChanged += (s, e) =>
                DelayValueText.Text = DelaySlider.Value.ToString("F0");

            SortTypeComboBox.SelectionChanged += SortTypeComboBox_SelectionChanged;

            _ = LoadDataSets();

            UpdateAlgorithmInfo();

            this.SizeChanged += (s, e) => DrawBars();

            SortTypeComboBox.Foreground = Brushes.White;
            DataSetComboBox.Foreground = Brushes.White;
        }

        #region Загрузка данных

        private async Task LoadDataSets()
        {
            try
            {
                var dataSets = await Task.Run(() => DatabaseHelper.GetAllDataSets());
                DataSetComboBox.Items.Clear();

                foreach (var ds in dataSets)
                {
                    DataSetComboBox.Items.Add(new ComboBoxItem
                    {
                        Content = $"{ds.Name} ({ds.CreatedAt:dd.MM.yyyy HH:mm})",
                        Tag = ds.Id
                    });
                }

                if (DataSetComboBox.Items.Count > 0)
                {
                    DataSetComboBox.SelectedIndex = 0;
                    await LoadDataFromDatabase(((ComboBoxItem)DataSetComboBox.SelectedItem).Tag);
                }
                else
                {
                    InfoText.Text = "Нет сохраненных наборов данных. Используйте ручной ввод или создайте набор.";
                    StatusText.Text = "Нет данных";
                    currentData = null;
                    DrawBars();
                    AddLog("Нет наборов данных в базе. Используйте кнопку 'Ввести массив вручную'.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка загрузки наборов данных: {ex.Message}");
                InfoText.Text = "Ошибка подключения к БД. Используйте ручной ввод.";
                currentData = null;
                DrawBars();
            }
        }

        private void GenerateLocalTestData(int count)
        {
            Random rnd = new Random();
            currentData = Enumerable.Range(0, count).Select(x => rnd.Next(10, 100)).ToList();
            ArraySizeText.Text = currentData.Count.ToString();
            DrawBars();
            AddLog($"Сгенерирован тестовый массив из {count} элементов (не сохранен в БД)");
            InfoText.Text = $"Тестовые данные: {count} элементов (не сохранены)";
        }

        private async Task LoadDataFromDatabase(object dataSetIdObj)
        {
            try
            {
                currentDataSetId = (int)dataSetIdObj;
                StatusText.Text = "Загрузка...";

                var values = await Task.Run(() => DatabaseHelper.GetSortValues(currentDataSetId));

                if (values.Count == 0)
                {
                    GenerateLocalTestData(50);
                    return;
                }

                currentData = values.OrderBy(v => v.Position).Select(v => v.Value).ToList();
                ArraySizeText.Text = currentData.Count.ToString();
                DrawBars();

                StatusText.Text = "Готов";
                AddLog($"Загружено {currentData.Count} элементов из БД");
                InfoText.Text = $"Загружено {currentData.Count} элементов";
            }
            catch
            {
                GenerateLocalTestData(50);
            }
        }

        private async void LoadDataButton_Click(object sender, RoutedEventArgs e)
        {
            if (isSorting)
            {
                MessageBox.Show("Сначала остановите сортировку!", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (DataSetComboBox.SelectedItem == null)
            {
                MessageBox.Show("Нет доступных наборов данных!\n\nСначала создайте набор данных через 'Ввести массив вручную' и сохраните его.",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            await LoadDataFromDatabase(((ComboBoxItem)DataSetComboBox.SelectedItem).Tag);

            if (TournamentModeRadio.IsChecked == true && currentTournamentControl != null)
            {
                currentTournamentControl.UpdateData(currentData);
            }
        }

        private void ManualInputButton_Click(object sender, RoutedEventArgs e)
        {
            if (isSorting)
            {
                MessageBox.Show("Сначала остановите сортировку!", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialog = new ArrayInputDialog();
            if (dialog.ShowDialog() == true && dialog.InputArray != null && dialog.InputArray.Length > 0)
            {
                currentData = new List<int>(dialog.InputArray);
                ArraySizeText.Text = currentData.Count.ToString();
                DrawBars();

                if (dialog.ShouldSaveToDatabase)
                {
                    AddLog($"Создан и сохранен набор данных: \"{dialog.DataSetName}\" с {dialog.InputArray.Length} элементами");
                    InfoText.Text = $"Создан набор \"{dialog.DataSetName}\" с {dialog.InputArray.Length} элементами";
                    _ = LoadDataSets();
                }
                else
                {
                    AddLog($"Введен массив вручную: [{string.Join(", ", currentData)}]");
                    InfoText.Text = $"Введено {currentData.Count} элементов вручную (не сохранено)";
                }

                StatusText.Text = "Готов";

                if (TournamentModeRadio.IsChecked == true && currentTournamentControl != null)
                {
                    currentTournamentControl.UpdateData(currentData);
                }
            }
        }

        #endregion

        #region Визуализация

        private void DrawBars()
        {
            Dispatcher.Invoke(() =>
            {
                VisualizationCanvas.Children.Clear();
                bars.Clear();

                if (currentData == null || currentData.Count == 0) return;

                double width = VisualizationCanvas.ActualWidth - 10;
                double height = VisualizationCanvas.ActualHeight - 30;
                if (width <= 0) width = 800;
                if (height <= 0) height = 400;

                double barWidth = width / currentData.Count;
                double maxValue = currentData.Max();

                for (int i = 0; i < currentData.Count; i++)
                {
                    var rect = new Rectangle();
                    rect.Width = Math.Max(barWidth - 1, 2);
                    rect.Height = (currentData[i] / maxValue) * height;
                    rect.Fill = GetBarColor(currentData[i], maxValue);
                    rect.Stroke = Brushes.White;
                    rect.StrokeThickness = 0.5;
                    rect.RadiusX = 2;
                    rect.RadiusY = 2;

                    Canvas.SetLeft(rect, i * barWidth + 5);
                    Canvas.SetBottom(rect, 5);

                    VisualizationCanvas.Children.Add(rect);
                    bars.Add(rect);
                }
            });
        }

        private Brush GetBarColor(int value, double maxValue)
        {
            double percent = value / maxValue;
            if (percent < 0.33) return new SolidColorBrush(Color.FromRgb(46, 204, 113));
            if (percent < 0.66) return new SolidColorBrush(Color.FromRgb(241, 196, 15));
            return new SolidColorBrush(Color.FromRgb(231, 76, 60));
        }

        private void HighlightBar(int index, Brush color)
        {
            Dispatcher.Invoke(() =>
            {
                if (index >= 0 && index < bars.Count)
                    bars[index].Fill = color;
            });
        }

        private void ResetBarColors()
        {
            if (currentData == null || currentData.Count == 0) return;
            double maxValue = currentData.Max();
            for (int i = 0; i < bars.Count; i++)
                bars[i].Fill = GetBarColor(currentData[i], maxValue);
        }

        #endregion

        #region Сортировка

        private async void StartSortButton_Click(object sender, RoutedEventArgs e)
        {
            if (isSorting)
            {
                MessageBox.Show("Сортировка уже выполняется! Дождитесь завершения или остановите.",
                    "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (currentData == null || currentData.Count == 0)
            {
                MessageBox.Show("Нет данных для сортировки!\n\nЗагрузите данные из БД или введите массив вручную.",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (SortTypeComboBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите тип сортировки!",
                    "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            isSorting = true;
            isPaused = false;
            stopRequested = false;
            comparisons = 0;
            swaps = 0;

            UpdateButtonsState(true);

            string sortType = (SortTypeComboBox.SelectedItem as ComboBoxItem).Content.ToString();
            AddLog($"Начало сортировки: {sortType}");

            DateTime startTime = DateTime.Now;

            try
            {
                switch (SortTypeComboBox.SelectedIndex)
                {
                    case 0: await BubbleSort(); break;
                    case 1: await QuickSort(0, currentData.Count - 1); break;
                    case 2: await InsertionSort(); break;
                    case 3: await SelectionSort(); break;
                    case 4: await MergeSort(0, currentData.Count - 1); break;
                    default:
                        AddLog("Ошибка: неизвестный тип сортировки");
                        return;
                }

                if (!stopRequested && !isPaused)
                {
                    int durationMs = (int)(DateTime.Now - startTime).TotalMilliseconds;

                    string dataSetName = "Ручной ввод";
                    if (DataSetComboBox.SelectedItem != null)
                    {
                        dataSetName = (DataSetComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Неизвестно";
                    }

                    var report = new SortReport
                    {
                        SortType = sortType,
                        ArraySize = currentData.Count,
                        Comparisons = comparisons,
                        Swaps = swaps,
                        DurationMs = durationMs,
                        StartTime = startTime,
                        EndTime = DateTime.Now,
                        Status = "Завершена",
                        DataSetName = dataSetName
                    };

                    AddLog($"Сохранение отчета: {sortType}, {currentData.Count} элементов, {durationMs} мс");

                    try
                    {
                        await Task.Run(() => DatabaseHelper.SaveSortReport(report));
                        AddLog($"Отчет успешно сохранен в базу данных");
                    }
                    catch (Exception ex)
                    {
                        AddLog($"Ошибка сохранения отчета: {ex.Message}");
                    }

                    AddLog($"Сортировка завершена! Сравнений: {comparisons}, Обменов: {swaps}, Время: {durationMs} мс");
                    StatusText.Text = "Готово";
                    InfoText.Text = $"Сортировка завершена за {durationMs} мс";
                }
            }
            catch (Exception ex)
            {
                AddLog($"Ошибка: {ex.Message}");
                StatusText.Text = "Ошибка";
            }
            finally
            {
                isSorting = false;
                UpdateButtonsState(false);
                ResetBarColors();
            }
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (isSorting && !isPaused)
            {
                isPaused = true;
                AddLog("Пауза");
                StatusText.Text = "На паузе";
                UpdateButtonsState(true);
            }
        }

        private void ResumeButton_Click(object sender, RoutedEventArgs e)
        {
            if (isSorting && isPaused)
            {
                isPaused = false;
                AddLog("Продолжение");
                StatusText.Text = "Сортировка...";
                UpdateButtonsState(true);
            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            if (isSorting)
            {
                stopRequested = true;
                isPaused = false;
                AddLog("Остановка...");
                StatusText.Text = "Остановлена";
            }
        }

        private void UpdateButtonsState(bool sorting)
        {
            Dispatcher.Invoke(() =>
            {
                StartSortButton.IsEnabled = !sorting;
                PauseButton.IsEnabled = sorting && !isPaused;
                ResumeButton.IsEnabled = sorting && isPaused;
                StopButton.IsEnabled = sorting;
                LoadDataButton.IsEnabled = !sorting;
                ManualInputButton.IsEnabled = !sorting;
                DataSetComboBox.IsEnabled = !sorting;
                SortTypeComboBox.IsEnabled = !sorting;
            });
        }

        private async Task BubbleSort()
        {
            for (int i = 0; i < currentData.Count - 1 && !stopRequested; i++)
            {
                for (int j = 0; j < currentData.Count - i - 1 && !stopRequested; j++)
                {
                    while (isPaused && !stopRequested) await Task.Delay(100);
                    if (stopRequested) return;

                    comparisons++;
                    Dispatcher.Invoke(() => ComparisonsText.Text = comparisons.ToString());

                    HighlightBar(j, Brushes.Blue);
                    HighlightBar(j + 1, Brushes.Blue);
                    await Task.Delay((int)DelaySlider.Value);

                    if (currentData[j] > currentData[j + 1])
                    {
                        swaps++;
                        Dispatcher.Invoke(() => SwapsText.Text = swaps.ToString());

                        (currentData[j], currentData[j + 1]) = (currentData[j + 1], currentData[j]);
                        DrawBars();
                        AddLog($"Меняем местами элементы {j} и {j + 1}");
                        await Task.Delay((int)DelaySlider.Value);
                    }

                    HighlightBar(j, GetBarColor(currentData[j], currentData.Max()));
                    HighlightBar(j + 1, GetBarColor(currentData[j + 1], currentData.Max()));
                }
            }
        }

        private async Task QuickSort(int left, int right)
        {
            if (left < right && !stopRequested)
            {
                while (isPaused && !stopRequested) await Task.Delay(100);
                if (stopRequested) return;

                int pivotIndex = await Partition(left, right);
                await QuickSort(left, pivotIndex - 1);
                await QuickSort(pivotIndex + 1, right);
            }
        }

        private async Task<int> Partition(int left, int right)
        {
            int pivot = currentData[right];
            int i = left - 1;

            for (int j = left; j < right && !stopRequested; j++)
            {
                while (isPaused && !stopRequested) await Task.Delay(100);
                if (stopRequested) return i;

                comparisons++;
                Dispatcher.Invoke(() => ComparisonsText.Text = comparisons.ToString());

                HighlightBar(j, Brushes.Blue);
                HighlightBar(right, Brushes.Red);
                await Task.Delay((int)DelaySlider.Value);

                if (currentData[j] <= pivot)
                {
                    i++;
                    swaps++;
                    Dispatcher.Invoke(() => SwapsText.Text = swaps.ToString());

                    (currentData[i], currentData[j]) = (currentData[j], currentData[i]);
                    DrawBars();
                    AddLog($"Быстрая сортировка: меняем местами {i} и {j}");
                    await Task.Delay((int)DelaySlider.Value);
                }

                HighlightBar(j, GetBarColor(currentData[j], currentData.Max()));
            }

            (currentData[i + 1], currentData[right]) = (currentData[right], currentData[i + 1]);
            DrawBars();
            AddLog($"Устанавливаем опорный элемент на позицию {i + 1}");

            return i + 1;
        }

        private async Task InsertionSort()
        {
            for (int i = 1; i < currentData.Count && !stopRequested; i++)
            {
                int key = currentData[i];
                int j = i - 1;

                while (j >= 0 && currentData[j] > key && !stopRequested)
                {
                    while (isPaused && !stopRequested) await Task.Delay(100);
                    if (stopRequested) return;

                    comparisons++;
                    Dispatcher.Invoke(() => ComparisonsText.Text = comparisons.ToString());

                    HighlightBar(j, Brushes.Blue);
                    HighlightBar(j + 1, Brushes.Blue);
                    await Task.Delay((int)DelaySlider.Value);

                    swaps++;
                    currentData[j + 1] = currentData[j];
                    DrawBars();

                    j--;
                }
                currentData[j + 1] = key;
                DrawBars();
            }
        }

        private async Task SelectionSort()
        {
            for (int i = 0; i < currentData.Count - 1 && !stopRequested; i++)
            {
                int minIdx = i;

                for (int j = i + 1; j < currentData.Count && !stopRequested; j++)
                {
                    while (isPaused && !stopRequested) await Task.Delay(100);
                    if (stopRequested) return;

                    comparisons++;
                    Dispatcher.Invoke(() => ComparisonsText.Text = comparisons.ToString());

                    HighlightBar(j, Brushes.Blue);
                    HighlightBar(minIdx, Brushes.Red);
                    await Task.Delay((int)DelaySlider.Value);

                    if (currentData[j] < currentData[minIdx])
                        minIdx = j;

                    HighlightBar(j, GetBarColor(currentData[j], currentData.Max()));
                }

                if (minIdx != i)
                {
                    swaps++;
                    Dispatcher.Invoke(() => SwapsText.Text = swaps.ToString());

                    (currentData[i], currentData[minIdx]) = (currentData[minIdx], currentData[i]);
                    DrawBars();
                    AddLog($"Сортировка выбором: меняем местами {i} и {minIdx}");
                    await Task.Delay((int)DelaySlider.Value);
                }
            }
        }

        private async Task MergeSort(int left, int right)
        {
            if (left < right && !stopRequested)
            {
                int mid = (left + right) / 2;
                await MergeSort(left, mid);
                await MergeSort(mid + 1, right);
                await Merge(left, mid, right);
            }
        }

        private async Task Merge(int left, int mid, int right)
        {
            int[] temp = new int[right - left + 1];
            int i = left, j = mid + 1, k = 0;

            while (i <= mid && j <= right && !stopRequested)
            {
                while (isPaused && !stopRequested) await Task.Delay(100);
                if (stopRequested) return;

                comparisons++;
                Dispatcher.Invoke(() => ComparisonsText.Text = comparisons.ToString());

                if (currentData[i] <= currentData[j])
                    temp[k++] = currentData[i++];
                else
                    temp[k++] = currentData[j++];

                await Task.Delay((int)DelaySlider.Value);
            }

            while (i <= mid && !stopRequested) temp[k++] = currentData[i++];
            while (j <= right && !stopRequested) temp[k++] = currentData[j++];

            for (int idx = 0; idx < temp.Length && !stopRequested; idx++)
            {
                currentData[left + idx] = temp[idx];
                DrawBars();
                await Task.Delay((int)DelaySlider.Value);
            }
        }

        #endregion

        #region Логирование

        private void AddLog(string message)
        {
            Dispatcher.Invoke(() =>
            {
                LogListBox.Items.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {message}");
                if (LogListBox.Items.Count > 100)
                    LogListBox.Items.RemoveAt(LogListBox.Items.Count - 1);
                LogScrollViewer.ScrollToBottom();
            });
        }

        private void ClearLogButton_Click(object sender, RoutedEventArgs e)
        {
            LogListBox.Items.Clear();
            AddLog("История очищена");
        }

        private void DataSetComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataSetComboBox.SelectedItem != null)
            {
                var item = (ComboBoxItem)DataSetComboBox.SelectedItem;
                InfoText.Text = $"Выбран: {item.Content}";
                DeleteDataSetButton.ToolTip = $"Удалить набор \"{item.Content}\"";
            }
        }

        #endregion

        #region Удаление и отчеты

        private async void DeleteDataSetButton_Click(object sender, RoutedEventArgs e)
        {
            if (isSorting)
            {
                MessageBox.Show("Сначала остановите сортировку!", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (DataSetComboBox.SelectedItem == null)
            {
                MessageBox.Show("Нет наборов данных для удаления!", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedItem = DataSetComboBox.SelectedItem as ComboBoxItem;
            string dataSetName = selectedItem.Content.ToString();
            int dataSetId = (int)selectedItem.Tag;

            var result = MessageBox.Show(
                $"Вы уверены, что хотите удалить набор данных:\n\n\"{dataSetName}\"?\n\nЭто действие необратимо!",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    StatusText.Text = "Удаление...";
                    InfoText.Text = $"Удаление набора данных...";

                    await Task.Run(() => DatabaseHelper.DeleteDataSet(dataSetId));

                    AddLog($"Удален набор данных: {dataSetName}");
                    await LoadDataSets();

                    StatusText.Text = "Готов";
                    InfoText.Text = $"Набор данных \"{dataSetName}\" удален";

                    if (DataSetComboBox.Items.Count == 0)
                    {
                        InfoText.Text = "Нет наборов данных. Используйте ручной ввод.";
                        currentData = null;
                        DrawBars();
                    }

                    MessageBox.Show($"Набор данных \"{dataSetName}\" успешно удален!",
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    StatusText.Text = "Ошибка";
                    AddLog($"Ошибка удаления: {ex.Message}");
                    MessageBox.Show($"Ошибка при удалении: {ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ReportsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var reportsWindow = new Views.SortReportsWindow();
                reportsWindow.Owner = this;
                reportsWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка открытия отчетов: {ex.Message}\n\nВозможно, таблица SortReports не создана в базе данных.",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SortTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateAlgorithmInfo();
        }

        private void UpdateAlgorithmInfo()
        {
            string info = "";
            string sortType = (SortTypeComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "";

            if (sortType.Contains("Bubble"))
            {
                info = "Bubble Sort: Соседние элементы сравниваются и меняются местами. " +
                       "Наибольший элемент 'всплывает' в конец массива. Сложность: O(n²)";
            }
            else if (sortType.Contains("Quick"))
            {
                info = "Quick Sort: Выбирается опорный элемент (pivot). Массив разделяется на " +
                       "элементы меньше и больше опорного. Рекурсивная сортировка. Сложность: O(n log n)";
            }
            else if (sortType.Contains("Insertion"))
            {
                info = "Insertion Sort: Массив постепенно строится из отсортированных элементов. " +
                       "Каждый новый элемент вставляется в правильную позицию. Сложность: O(n²)";
            }
            else if (sortType.Contains("Selection"))
            {
                info = "Selection Sort: На каждом шаге находится минимальный элемент и " +
                       "меняется местами с текущим. Сложность: O(n²)";
            }
            else if (sortType.Contains("Merge"))
            {
                info = "Merge Sort: Массив рекурсивно делится на две части, сортируется, " +
                       "затем сливается обратно. Сложность: O(n log n)";
            }
            else if (sortType.Contains("Heap"))
            {
                info = "Heap Sort: Строится двоичная куча, затем элементы извлекаются. " +
                       "Сложность: O(n log n)";
            }
            else
            {
                info = "Выберите алгоритм сортировки для получения информации о нем";
            }

            AlgorithmInfo.Text = info;
        }

        #endregion

        /*private async Task AnimateBarAppearance(Rectangle bar, double targetHeight)
        {
            bar.Height = 0;
            int steps = 20;
            double stepHeight = targetHeight / steps;

            for (int i = 0; i < steps; i++)
            {
                bar.Height += stepHeight;
                await Task.Delay(2);
            }
            bar.Height = targetHeight;
        }*/

        private void ModeRadio_Checked(object sender, RoutedEventArgs e)
        {
            if (SingleModeRadio == null || TournamentModeRadio == null) return;

            if (TournamentModeRadio.IsChecked == true)
            {
                SingleModePanel.Visibility = Visibility.Collapsed;
                VisualizationCanvas.Visibility = Visibility.Collapsed;
                TournamentContainer.Visibility = Visibility.Visible;

                AlgorithmInfo.Visibility = Visibility.Collapsed;

                if (currentData != null && currentData.Count > 0)
                {
                    currentTournamentControl = new Views.TournamentControl(currentData);
                    TournamentContainer.Content = currentTournamentControl;
                }
                else
                {
                    MessageBox.Show("Сначала загрузите данные!", "Предупреждение",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    SingleModeRadio.IsChecked = true;
                }
            }
            else
            {
                SingleModePanel.Visibility = Visibility.Visible;
                VisualizationCanvas.Visibility = Visibility.Visible;
                TournamentContainer.Visibility = Visibility.Collapsed;
                TournamentContainer.Content = null;
                currentTournamentControl = null;

                AlgorithmInfo.Visibility = Visibility.Visible;
            }
        }
        private TournamentControl currentTournamentControl;

    } 


    }