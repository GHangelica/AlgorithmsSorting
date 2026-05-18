using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace SortVisualizer.Views
{
    public partial class TournamentWindow : Window
    {
        private List<int> originalData;
        private List<TournamentAlgorithm> algorithms = new List<TournamentAlgorithm>();
        private DateTime tournamentStartTime;
        private bool isRunning = false;
        private bool isPaused = false;
        private bool stopRequested = false;
        private CancellationTokenSource cts;

        public TournamentWindow(List<int> data)
        {
            InitializeComponent();
            originalData = new List<int>(data);
            ArrayInfo.Text = $"{data.Count} элементов";

            InitializeAlgorithms();
            DrawInitialBars();

            this.SizeChanged += (s, e) => DrawInitialBars();
        }

        private void InitializeAlgorithms()
        {
            algorithms.Add(new TournamentAlgorithm
            {
                Name = "Bubble Sort",
                Canvas = BubbleCanvas,
                TimeText = BubbleTime,
                ComparisonsText = BubbleComparisons,
                ProgressBar = BubbleProgress,
                Border = BubbleBorder,
                Color = Brushes.Gold,
                SortFunction = RunBubbleSort
            });
            algorithms.Add(new TournamentAlgorithm
            {
                Name = "Quick Sort",
                Canvas = QuickCanvas,
                TimeText = QuickTime,
                ComparisonsText = QuickComparisons,
                ProgressBar = QuickProgress,
                Border = QuickBorder,
                Color = Brushes.DodgerBlue,
                SortFunction = RunQuickSort
            });
            algorithms.Add(new TournamentAlgorithm
            {
                Name = "Insertion Sort",
                Canvas = InsertionCanvas,
                TimeText = InsertionTime,
                ComparisonsText = InsertionComparisons,
                ProgressBar = InsertionProgress,
                Border = InsertionBorder,
                Color = Brushes.LimeGreen,
                SortFunction = RunInsertionSort
            });
            algorithms.Add(new TournamentAlgorithm
            {
                Name = "Selection Sort",
                Canvas = SelectionCanvas,
                TimeText = SelectionTime,
                ComparisonsText = SelectionComparisons,
                ProgressBar = SelectionProgress,
                Border = SelectionBorder,
                Color = Brushes.MediumPurple,
                SortFunction = RunSelectionSort
            });
            algorithms.Add(new TournamentAlgorithm
            {
                Name = "Merge Sort",
                Canvas = MergeCanvas,
                TimeText = MergeTime,
                ComparisonsText = MergeComparisons,
                ProgressBar = MergeProgress,
                Border = MergeBorder,
                Color = Brushes.IndianRed,
                SortFunction = RunMergeSort
            });
        }

        private void DrawInitialBars()
        {
            foreach (var alg in algorithms)
            {
                DrawBars(alg.Canvas, originalData);
            }
        }

        private void DrawBars(Canvas canvas, List<int> data)
        {
            canvas.Dispatcher.Invoke(() =>
            {
                canvas.Children.Clear();
                if (data == null || data.Count == 0) return;

                double width = canvas.ActualWidth - 20;
                double height = canvas.ActualHeight - 30;
                if (width <= 0) width = 200;
                if (height <= 0) height = 100;

                double barWidth = width / data.Count;
                double maxValue = data.Max();

                for (int i = 0; i < data.Count; i++)
                {
                    var rect = new Rectangle();
                    rect.Width = Math.Max(barWidth - 1, 2);
                    rect.Height = Math.Max((data[i] / maxValue) * height, 3);
                    rect.Fill = GetBarColor(data[i], maxValue);
                    rect.Stroke = Brushes.White;
                    rect.StrokeThickness = 0.5;

                    Canvas.SetLeft(rect, i * barWidth + 10);
                    Canvas.SetBottom(rect, 10);

                    canvas.Children.Add(rect);
                }
            });
        }

        private void UpdateBar(Canvas canvas, List<int> data)
        {
            canvas.Dispatcher.Invoke(() =>
            {
                if (canvas.Children.Count != data.Count)
                {
                    DrawBars(canvas, data);
                    return;
                }

                double width = canvas.ActualWidth - 20;
                double height = canvas.ActualHeight - 30;
                if (width <= 0) width = 200;
                if (height <= 0) height = 100;

                double barWidth = width / data.Count;
                double maxValue = data.Max();

                for (int i = 0; i < data.Count; i++)
                {
                    var rect = canvas.Children[i] as Rectangle;
                    if (rect != null)
                    {
                        rect.Height = Math.Max((data[i] / maxValue) * height, 3);
                        rect.Fill = GetBarColor(data[i], maxValue);
                        Canvas.SetLeft(rect, i * barWidth + 10);
                    }
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

        #region Управление турниром

        private async void StartTournamentButton_Click(object sender, RoutedEventArgs e)
        {
            if (isRunning) return;

            isRunning = true;
            isPaused = false;
            stopRequested = false;

            StartTournamentButton.IsEnabled = false;
            PauseTournamentButton.IsEnabled = true;
            ResumeTournamentButton.IsEnabled = false;
            StopTournamentButton.IsEnabled = true;

            tournamentStartTime = DateTime.Now;

            foreach (var alg in algorithms)
            {
                alg.Reset();
                alg.Canvas.Dispatcher.Invoke(() =>
                {
                    DrawBars(alg.Canvas, originalData);
                });
            }

            cts = new CancellationTokenSource();

            var tasks = new List<Task>();
            foreach (var alg in algorithms)
            {
                var data = new List<int>(originalData);
                var task = RunAlgorithmWithControl(alg, data, cts.Token);
                tasks.Add(task);
            }

            try
            {
                await Task.WhenAll(tasks);

                if (!stopRequested)
                {
                    var winner = algorithms.OrderBy(a => a.TimeMs).First();
                    WinnerText.Text = $"{winner.Name} ({winner.TimeMs} мс)";
                    WinnerText.Foreground = winner.Color;
                    TotalTimeText.Text = $"{(DateTime.Now - tournamentStartTime).TotalMilliseconds:F0} мс";

                    MessageBox.Show($"🏆 ПОБЕДИТЕЛЬ: {winner.Name}\n⏱ Время: {winner.TimeMs} мс\n📊 Сравнений: {winner.Comparisons}\n🔄 Обменов: {winner.Swaps}",
                        "Турнир завершен", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    WinnerText.Text = "Остановлен";
                    TotalTimeText.Text = "—";
                }
            }
            catch (OperationCanceledException)
            {
                WinnerText.Text = "Прерван";
                TotalTimeText.Text = "—";
            }
            finally
            {
                isRunning = false;
                StartTournamentButton.IsEnabled = true;
                PauseTournamentButton.IsEnabled = false;
                ResumeTournamentButton.IsEnabled = false;
                StopTournamentButton.IsEnabled = false;
            }
        }

        private void PauseTournamentButton_Click(object sender, RoutedEventArgs e)
        {
            if (isRunning && !isPaused)
            {
                isPaused = true;
                PauseTournamentButton.IsEnabled = false;
                ResumeTournamentButton.IsEnabled = true;
            }
        }

        private void ResumeTournamentButton_Click(object sender, RoutedEventArgs e)
        {
            if (isRunning && isPaused)
            {
                isPaused = false;
                PauseTournamentButton.IsEnabled = true;
                ResumeTournamentButton.IsEnabled = false;
            }
        }

        private void StopTournamentButton_Click(object sender, RoutedEventArgs e)
        {
            if (isRunning)
            {
                stopRequested = true;
                isPaused = false;

                if (cts != null)
                {
                    cts.Cancel();
                }

                PauseTournamentButton.IsEnabled = false;
                ResumeTournamentButton.IsEnabled = false;
                StopTournamentButton.IsEnabled = false;
            }
        }

        private async Task RunAlgorithmWithControl(TournamentAlgorithm alg, List<int> data, CancellationToken token)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Запускаем алгоритм с поддержкой отмены
                var task = alg.SortFunction(data, alg, token);

                // Ждем с возможностью паузы
                while (!task.IsCompleted && !token.IsCancellationRequested)
                {
                    if (isPaused)
                    {
                        await Task.Delay(100);
                        continue;
                    }
                    await Task.Delay(10);
                }

                if (token.IsCancellationRequested || stopRequested)
                {
                    alg.TimeMs = -1;
                    alg.TimeText.Text = "Остановлен";
                    return;
                }

                await task;
                stopwatch.Stop();

                if (!stopRequested)
                {
                    alg.TimeMs = (int)stopwatch.ElapsedMilliseconds;
                    alg.TimeText.Text = $"{alg.TimeMs} мс";
                }
            }
            catch (OperationCanceledException)
            {
                alg.TimeMs = -1;
                alg.TimeText.Text = "Остановлен";
            }
            finally
            {
                UpdateResultsGrid();
            }
        }

        private void UpdateResultsGrid()
        {
            Dispatcher.Invoke(() =>
            {
                var results = algorithms.Select(a => new
                {
                    a.Name,
                    Time = a.TimeMs < 0 ? "Остановлен" : $"{a.TimeMs} мс",
                    a.Comparisons,
                    a.Swaps,
                    Status = a.IsCompleted ? "✅ Завершен" : (a.TimeMs < 0 ? "⏹ Остановлен" : "⏳ В процессе")
                }).OrderBy(r => r.Time != "Остановлен" ? int.Parse(r.Time.Replace(" мс", "")) : int.MaxValue);

                ResultsGrid.ItemsSource = results.ToList();
            });
        }

        #endregion

        #region Алгоритмы с поддержкой паузы и отмены

        private async Task RunBubbleSort(List<int> data, TournamentAlgorithm alg, CancellationToken token)
        {
            for (int i = 0; i < data.Count - 1 && !stopRequested; i++)
            {
                token.ThrowIfCancellationRequested();

                for (int j = 0; j < data.Count - i - 1 && !stopRequested; j++)
                {
                    while (isPaused && !stopRequested)
                    {
                        await Task.Delay(50);
                        token.ThrowIfCancellationRequested();
                    }
                    token.ThrowIfCancellationRequested();

                    alg.Comparisons++;
                    alg.ComparisonsText.Text = alg.Comparisons.ToString();

                    if (data[j] > data[j + 1])
                    {
                        alg.Swaps++;
                        (data[j], data[j + 1]) = (data[j + 1], data[j]);
                        UpdateBar(alg.Canvas, data);
                        await Task.Delay(3);
                    }
                }
                alg.ProgressBar.Value = (double)(i + 1) / data.Count * 100;
            }
            alg.IsCompleted = true;
            alg.Border.BorderBrush = alg.Color;
            alg.Border.BorderThickness = new Thickness(3);
        }

        private async Task RunQuickSort(List<int> data, TournamentAlgorithm alg, CancellationToken token)
        {
            await QuickSort(data, 0, data.Count - 1, alg, token);
            alg.IsCompleted = true;
            alg.Border.BorderBrush = alg.Color;
            alg.Border.BorderThickness = new Thickness(3);
        }

        private async Task QuickSort(List<int> data, int left, int right, TournamentAlgorithm alg, CancellationToken token)
        {
            if (left < right && !stopRequested)
            {
                token.ThrowIfCancellationRequested();

                int pivotIndex = await Partition(data, left, right, alg, token);
                await QuickSort(data, left, pivotIndex - 1, alg, token);
                await QuickSort(data, pivotIndex + 1, right, alg, token);
            }
        }

        private async Task<int> Partition(List<int> data, int left, int right, TournamentAlgorithm alg, CancellationToken token)
        {
            int pivot = data[right];
            int i = left - 1;

            for (int j = left; j < right && !stopRequested; j++)
            {
                while (isPaused && !stopRequested)
                {
                    await Task.Delay(50);
                    token.ThrowIfCancellationRequested();
                }
                token.ThrowIfCancellationRequested();

                alg.Comparisons++;
                alg.ComparisonsText.Text = alg.Comparisons.ToString();

                if (data[j] <= pivot)
                {
                    i++;
                    alg.Swaps++;
                    (data[i], data[j]) = (data[j], data[i]);
                    UpdateBar(alg.Canvas, data);
                    await Task.Delay(2);
                }
            }

            alg.Swaps++;
            (data[i + 1], data[right]) = (data[right], data[i + 1]);
            UpdateBar(alg.Canvas, data);
            return i + 1;
        }

        private async Task RunInsertionSort(List<int> data, TournamentAlgorithm alg, CancellationToken token)
        {
            for (int i = 1; i < data.Count && !stopRequested; i++)
            {
                token.ThrowIfCancellationRequested();

                int key = data[i];
                int j = i - 1;

                while (j >= 0 && data[j] > key && !stopRequested)
                {
                    while (isPaused && !stopRequested)
                    {
                        await Task.Delay(50);
                        token.ThrowIfCancellationRequested();
                    }
                    token.ThrowIfCancellationRequested();

                    alg.Comparisons++;
                    alg.Swaps++;
                    data[j + 1] = data[j];
                    j--;
                    UpdateBar(alg.Canvas, data);
                    await Task.Delay(2);
                }
                data[j + 1] = key;
                alg.ProgressBar.Value = (double)i / data.Count * 100;
            }
            alg.IsCompleted = true;
            alg.Border.BorderBrush = alg.Color;
            alg.Border.BorderThickness = new Thickness(3);
        }

        private async Task RunSelectionSort(List<int> data, TournamentAlgorithm alg, CancellationToken token)
        {
            for (int i = 0; i < data.Count - 1 && !stopRequested; i++)
            {
                token.ThrowIfCancellationRequested();

                int minIdx = i;
                for (int j = i + 1; j < data.Count && !stopRequested; j++)
                {
                    while (isPaused && !stopRequested)
                    {
                        await Task.Delay(50);
                        token.ThrowIfCancellationRequested();
                    }
                    token.ThrowIfCancellationRequested();

                    alg.Comparisons++;
                    if (data[j] < data[minIdx])
                        minIdx = j;
                }

                if (minIdx != i)
                {
                    alg.Swaps++;
                    (data[i], data[minIdx]) = (data[minIdx], data[i]);
                    UpdateBar(alg.Canvas, data);
                    await Task.Delay(3);
                }
                alg.ProgressBar.Value = (double)(i + 1) / data.Count * 100;
            }
            alg.IsCompleted = true;
            alg.Border.BorderBrush = alg.Color;
            alg.Border.BorderThickness = new Thickness(3);
        }

        private async Task RunMergeSort(List<int> data, TournamentAlgorithm alg, CancellationToken token)
        {
            await MergeSort(data, 0, data.Count - 1, alg, token);
            alg.IsCompleted = true;
            alg.Border.BorderBrush = alg.Color;
            alg.Border.BorderThickness = new Thickness(3);
        }

        private async Task MergeSort(List<int> data, int left, int right, TournamentAlgorithm alg, CancellationToken token)
        {
            if (left < right && !stopRequested)
            {
                token.ThrowIfCancellationRequested();

                int mid = (left + right) / 2;
                await MergeSort(data, left, mid, alg, token);
                await MergeSort(data, mid + 1, right, alg, token);
                await Merge(data, left, mid, right, alg, token);
            }
        }

        private async Task Merge(List<int> data, int left, int mid, int right, TournamentAlgorithm alg, CancellationToken token)
        {
            int[] temp = new int[right - left + 1];
            int i = left, j = mid + 1, k = 0;

            while (i <= mid && j <= right && !stopRequested)
            {
                while (isPaused && !stopRequested)
                {
                    await Task.Delay(50);
                    token.ThrowIfCancellationRequested();
                }
                token.ThrowIfCancellationRequested();

                alg.Comparisons++;
                if (data[i] <= data[j])
                    temp[k++] = data[i++];
                else
                    temp[k++] = data[j++];
            }

            while (i <= mid && !stopRequested) temp[k++] = data[i++];
            while (j <= right && !stopRequested) temp[k++] = data[j++];

            for (int idx = 0; idx < temp.Length && !stopRequested; idx++)
            {
                data[left + idx] = temp[idx];
                alg.Swaps++;
                UpdateBar(alg.Canvas, data);
                await Task.Delay(1);
            }
        }

        #endregion

        private void SelectDataButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ArrayInputDialog();
            if (dialog.ShowDialog() == true && dialog.InputArray != null)
            {
                originalData = new List<int>(dialog.InputArray);
                ArrayInfo.Text = $"{originalData.Count} элементов";
                DrawInitialBars();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    public class TournamentAlgorithm
    {
        public string Name { get; set; }
        public Canvas Canvas { get; set; }
        public TextBlock TimeText { get; set; }
        public TextBlock ComparisonsText { get; set; }
        public ProgressBar ProgressBar { get; set; }
        public Border Border { get; set; }
        public Brush Color { get; set; }
        public Func<List<int>, TournamentAlgorithm, CancellationToken, Task> SortFunction { get; set; }

        public int Comparisons { get; set; } = 0;
        public int Swaps { get; set; } = 0;
        public int TimeMs { get; set; } = 0;
        public bool IsCompleted { get; set; } = false;

        public void Reset()
        {
            Comparisons = 0;
            Swaps = 0;
            TimeMs = 0;
            IsCompleted = false;
            TimeText.Text = "0 мс";
            ComparisonsText.Text = "0";
            ProgressBar.Value = 0;
            Border.BorderBrush = Brushes.Transparent;
            Border.BorderThickness = new Thickness(0);
        }

    }

}