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
    public partial class TournamentControl : UserControl
    {
        private List<int> originalData;
        private List<TournamentAlgorithm> algorithms = new List<TournamentAlgorithm>();
        private List<TournamentAlgorithm> activeAlgorithms = new List<TournamentAlgorithm>();
        private DateTime tournamentStartTime;
        private bool isRunning = false;
        private bool isPaused = false;
        private bool stopRequested = false;
        private CancellationTokenSource cts;

        public TournamentControl(List<int> data)
        {
            InitializeComponent();
            originalData = new List<int>(data);
            InitializeAlgorithms();
            CreateAlgorithmPanels();
            UpdateActiveAlgorithms();

            if (DelaySlider != null)
            {
                DelaySlider.ValueChanged += (s, e) =>
                    DelayValueText.Text = DelaySlider.Value.ToString("F0");
            }
        }

        public void UpdateData(List<int> newData)
        {
            originalData = new List<int>(newData);

            foreach (var alg in algorithms)
            {
                alg.Reset();
                DrawBars(alg.Canvas, originalData);
            }

            isRunning = false;
            isPaused = false;
            stopRequested = false;

            WinnerText.Text = "—";
            TotalTimeText.Text = "0 мс";

            StartTournamentButton.IsEnabled = true;
            PauseButton.IsEnabled = false;
            ResumeButton.IsEnabled = false;
            StopButton.IsEnabled = false;
        }

        private void InitializeAlgorithms()
        {
            algorithms.Clear();

            algorithms.Add(new TournamentAlgorithm
            {
                Name = "Bubble Sort",
                Color = Brushes.Gold,
                SortFunction = RunBubbleSort
            });
            algorithms.Add(new TournamentAlgorithm
            {
                Name = "Quick Sort",
                Color = Brushes.DodgerBlue,
                SortFunction = RunQuickSort
            });
            algorithms.Add(new TournamentAlgorithm
            {
                Name = "Insertion Sort",
                Color = Brushes.LimeGreen,
                SortFunction = RunInsertionSort
            });
            algorithms.Add(new TournamentAlgorithm
            {
                Name = "Selection Sort",
                Color = Brushes.MediumPurple,
                SortFunction = RunSelectionSort
            });
            algorithms.Add(new TournamentAlgorithm
            {
                Name = "Merge Sort",
                Color = Brushes.IndianRed,
                SortFunction = RunMergeSort
            });
        }

        private void CreateAlgorithmPanels()
        {
            TournamentGrid.Children.Clear();

            int index = 0;
            int maxCols = 3;

            foreach (var alg in algorithms)
            {
                int row = index / maxCols;
                int col = index % maxCols;

                var border = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(37, 37, 37)),
                    CornerRadius = new CornerRadius(8),
                    Margin = new Thickness(5),
                    Padding = new Thickness(5),
                    Tag = alg
                };

                var grid = new Grid();
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                var titleText = new TextBlock
                {
                    Text = alg.Name,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    FontWeight = FontWeights.Bold,
                    Foreground = alg.Color,
                    Margin = new Thickness(5)
                };
                Grid.SetRow(titleText, 0);
                grid.Children.Add(titleText);

                var progressBar = new ProgressBar
                {
                    Height = 3,
                    Minimum = 0,
                    Maximum = 100,
                    Value = 0,
                    Margin = new Thickness(10, 20, 10, 3)
                };
                Grid.SetRow(progressBar, 1);
                grid.Children.Add(progressBar);

                var canvas = new Canvas
                {
                    Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                    ClipToBounds = true
                };
                Grid.SetRow(canvas, 1);
                grid.Children.Add(canvas);

                var statsPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(5)
                };
                statsPanel.Children.Add(new TextBlock { Text = "Время:", Foreground = new SolidColorBrush(Color.FromRgb(160, 160, 160)), FontSize = 10 });
                var timeText = new TextBlock { Text = "0 мс", Foreground = alg.Color, FontSize = 10, Margin = new Thickness(5, 0, 0, 0) };
                statsPanel.Children.Add(timeText);
                statsPanel.Children.Add(new TextBlock { Text = "| Сравнений:", Foreground = new SolidColorBrush(Color.FromRgb(160, 160, 160)), FontSize = 10, Margin = new Thickness(5, 0, 0, 0) });
                var comparisonsText = new TextBlock { Text = "0", Foreground = alg.Color, FontSize = 10, Margin = new Thickness(5, 0, 0, 0) };
                statsPanel.Children.Add(comparisonsText);
                Grid.SetRow(statsPanel, 2);
                grid.Children.Add(statsPanel);

                border.Child = grid;

                alg.Border = border;
                alg.Canvas = canvas;
                alg.ProgressBar = progressBar;
                alg.TimeText = timeText;
                alg.ComparisonsText = comparisonsText;

                Grid.SetRow(border, row);
                Grid.SetColumn(border, col);
                TournamentGrid.Children.Add(border);

                index++;
            }
        }

        private void UpdateActiveAlgorithms()
        {
            activeAlgorithms.Clear();

            if (ChkBubble.IsChecked == true)
                activeAlgorithms.Add(algorithms[0]);
            if (ChkQuick.IsChecked == true)
                activeAlgorithms.Add(algorithms[1]);
            if (ChkInsertion.IsChecked == true)
                activeAlgorithms.Add(algorithms[2]);
            if (ChkSelection.IsChecked == true)
                activeAlgorithms.Add(algorithms[3]);
            if (ChkMerge.IsChecked == true)
                activeAlgorithms.Add(algorithms[4]);

            foreach (var alg in algorithms)
            {
                alg.Border.Visibility = activeAlgorithms.Contains(alg) ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateActiveAlgorithms();

            if (activeAlgorithms.Count < 2)
            {
                MessageBox.Show("Выберите минимум 2 алгоритма!", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void StartTournamentButton_Click(object sender, RoutedEventArgs e)
        {
            if (isRunning) return;

            UpdateActiveAlgorithms();

            if (activeAlgorithms.Count < 2)
            {
                MessageBox.Show("Выберите минимум 2 алгоритма для турнира!",
                    "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            isRunning = true;
            isPaused = false;
            stopRequested = false;

            StartTournamentButton.IsEnabled = false;
            PauseButton.IsEnabled = true;
            ResumeButton.IsEnabled = false;
            StopButton.IsEnabled = true;

            tournamentStartTime = DateTime.Now;

            foreach (var alg in activeAlgorithms)
            {
                alg.Reset();
                DrawBars(alg.Canvas, originalData);
            }

            cts = new CancellationTokenSource();

            var tasks = new List<Task>();
            foreach (var alg in activeAlgorithms)
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
                    var winner = activeAlgorithms.OrderBy(a => a.TimeMs).First();
                    WinnerText.Text = $"{winner.Name} ({winner.TimeMs} мс)";
                    WinnerText.Foreground = winner.Color;
                    TotalTimeText.Text = $"{(DateTime.Now - tournamentStartTime).TotalMilliseconds:F0} мс";
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
                PauseButton.IsEnabled = false;
                ResumeButton.IsEnabled = false;
                StopButton.IsEnabled = false;
            }
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (isRunning && !isPaused)
            {
                isPaused = true;
                PauseButton.IsEnabled = false;
                ResumeButton.IsEnabled = true;
            }
        }

        private void ResumeButton_Click(object sender, RoutedEventArgs e)
        {
            if (isRunning && isPaused)
            {
                isPaused = false;
                PauseButton.IsEnabled = true;
                ResumeButton.IsEnabled = false;
            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            if (isRunning)
            {
                stopRequested = true;
                isPaused = false;

                if (cts != null)
                {
                    cts.Cancel();
                }

                PauseButton.IsEnabled = false;
                ResumeButton.IsEnabled = false;
                StopButton.IsEnabled = false;
            }
        }

        private async Task RunAlgorithmWithControl(TournamentAlgorithm alg, List<int> data, CancellationToken token)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var task = alg.SortFunction(data, alg, token);

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

        #region Алгоритмы сортировки

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
                        await Task.Delay((int)DelaySlider.Value);
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
                    await Task.Delay((int)DelaySlider.Value);
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
                    await Task.Delay((int)DelaySlider.Value);
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
                    await Task.Delay((int)DelaySlider.Value);
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
                await Task.Delay((int)DelaySlider.Value);
            }
        }

        #endregion
    }

    public class TournamentAlgorithm
    {
        public string Name { get; set; }
        public Border Border { get; set; }
        public Canvas Canvas { get; set; }
        public ProgressBar ProgressBar { get; set; }
        public TextBlock TimeText { get; set; }
        public TextBlock ComparisonsText { get; set; }
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