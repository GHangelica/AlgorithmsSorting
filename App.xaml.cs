using System.Windows;
using SortVisualizer.Data;

namespace SortVisualizer
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            if (!DatabaseHelper.TestConnection())
            {
                MessageBox.Show(
                    "Не удалось подключиться к базе данных SQL Server.\n\n" +
                    "Приложение будет работать в режиме офлайн с тестовыми данными.\n\n" +
                    "Для подключения к БД проверьте:\n" +
                    "1. Запущен ли SQL Server (Services.msc)\n" +
                    "2. Существует ли база данных SortVisualizerDB\n" +
                    "3. Правильная ли строка подключения в DatabaseHelper.cs",
                    "Предупреждение",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }
    }
}