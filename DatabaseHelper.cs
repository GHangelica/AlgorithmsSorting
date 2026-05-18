using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using Microsoft.Data.SqlClient;
using SortVisualizer.Models;


namespace SortVisualizer.Data
{
    public static class DatabaseHelper
    {
        private static string connectionString = @"Server=.\SQLEXPRESS;Database=SortVisualizerDB;Trusted_Connection=True;Encrypt=False;TrustServerCertificate=True;";

        public static bool TestConnection()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка подключения: {ex.Message}");
                return false;
            }
        }

        public static List<DataSet> GetAllDataSets()
        {
            var dataSets = new List<DataSet>();

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT Id, Name, CreatedAt FROM DataSets ORDER BY CreatedAt DESC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            dataSets.Add(new DataSet
                            {
                                Id = (int)reader["Id"],
                                Name = reader["Name"].ToString(),
                                CreatedAt = (DateTime)reader["CreatedAt"]
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка загрузки наборов данных: {ex.Message}");
                MessageBox.Show($"Ошибка загрузки: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return dataSets;
        }

        public static List<SortValue> GetSortValues(int dataSetId)
        {
            var values = new List<SortValue>();

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT Id, DataSetId, Value, Position FROM SortValues WHERE DataSetId = @DataSetId ORDER BY Position";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@DataSetId", dataSetId);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                values.Add(new SortValue
                                {
                                    Id = (int)reader["Id"],
                                    DataSetId = (int)reader["DataSetId"],
                                    Value = (int)reader["Value"],
                                    Position = (int)reader["Position"]
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка загрузки значений: {ex.Message}");
            }

            return values;
        }

        public static void SaveSortResult(SortResult result)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"
                        INSERT INTO SortResults (DataSetId, SortType, Comparisons, Swaps, DurationMs, ExecutedAt)
                        VALUES (@DataSetId, @SortType, @Comparisons, @Swaps, @DurationMs, @ExecutedAt)";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@DataSetId", result.DataSetId);
                        cmd.Parameters.AddWithValue("@SortType", result.SortType);
                        cmd.Parameters.AddWithValue("@Comparisons", result.Comparisons);
                        cmd.Parameters.AddWithValue("@Swaps", result.Swaps);
                        cmd.Parameters.AddWithValue("@DurationMs", result.DurationMs);
                        cmd.Parameters.AddWithValue("@ExecutedAt", result.ExecutedAt);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка сохранения: {ex.Message}");
            }
        }

        public static List<SortResult> GetSortResults(int dataSetId)
        {
            var results = new List<SortResult>();

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT * FROM SortResults WHERE DataSetId = @DataSetId ORDER BY ExecutedAt DESC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@DataSetId", dataSetId);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                results.Add(new SortResult
                                {
                                    Id = (int)reader["Id"],
                                    DataSetId = (int)reader["DataSetId"],
                                    SortType = reader["SortType"].ToString(),
                                    Comparisons = (int)reader["Comparisons"],
                                    Swaps = (int)reader["Swaps"],
                                    DurationMs = (int)reader["DurationMs"],
                                    ExecutedAt = (DateTime)reader["ExecutedAt"]
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка загрузки результатов: {ex.Message}");
            }

            return results;
        }

        public static void GenerateTestData(string dataSetName, int count)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    using (SqlTransaction transaction = conn.BeginTransaction())
                    {

                        string insertDataSet = "INSERT INTO DataSets (Name, CreatedAt) VALUES (@Name, @CreatedAt); SELECT SCOPE_IDENTITY();";
                        int dataSetId;

                        using (SqlCommand cmd = new SqlCommand(insertDataSet, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@Name", dataSetName);
                            cmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
                            dataSetId = Convert.ToInt32(cmd.ExecuteScalar());
                        }

                        Random rnd = new Random();
                        for (int i = 0; i < count; i++)
                        {
                            string insertValue = "INSERT INTO SortValues (DataSetId, Value, Position) VALUES (@DataSetId, @Value, @Position)";
                            using (SqlCommand cmd = new SqlCommand(insertValue, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@DataSetId", dataSetId);
                                cmd.Parameters.AddWithValue("@Value", rnd.Next(10, 100));
                                cmd.Parameters.AddWithValue("@Position", i);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        transaction.Commit();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка генерации: {ex.Message}");
                throw;
            }
        }

        public static bool CheckTablesExist()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'DataSets'";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        int count = (int)cmd.ExecuteScalar();
                        return count > 0;
                    }
                }
            }
            catch
            {
                return false;
            }
        }
        public static void CreateTablesIfNotExist()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    string createDataSets = @"
                        IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'DataSets')
                        CREATE TABLE DataSets (
                            Id INT IDENTITY(1,1) PRIMARY KEY,
                            Name NVARCHAR(100) NOT NULL,
                            CreatedAt DATETIME DEFAULT GETDATE()
                        )";

                    using (SqlCommand cmd = new SqlCommand(createDataSets, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    string createSortValues = @"
                        IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'SortValues')
                        CREATE TABLE SortValues (
                            Id INT IDENTITY(1,1) PRIMARY KEY,
                            DataSetId INT NOT NULL,
                            Value INT NOT NULL,
                            Position INT NOT NULL,
                            FOREIGN KEY (DataSetId) REFERENCES DataSets(Id) ON DELETE CASCADE
                        )";

                    using (SqlCommand cmd = new SqlCommand(createSortValues, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    string createSortResults = @"
                        IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'SortResults')
                        CREATE TABLE SortResults (
                            Id INT IDENTITY(1,1) PRIMARY KEY,
                            DataSetId INT NOT NULL,
                            SortType NVARCHAR(50) NOT NULL,
                            Comparisons INT DEFAULT 0,
                            Swaps INT DEFAULT 0,
                            DurationMs INT DEFAULT 0,
                            ExecutedAt DATETIME DEFAULT GETDATE(),
                            FOREIGN KEY (DataSetId) REFERENCES DataSets(Id) ON DELETE CASCADE
                        )";

                    using (SqlCommand cmd = new SqlCommand(createSortResults, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка создания таблиц: {ex.Message}");
                throw;
            }
        }

        public static void DeleteDataSet(int dataSetId)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    using (SqlTransaction transaction = conn.BeginTransaction())
                    {

                        string deleteResults = "DELETE FROM SortResults WHERE DataSetId = @DataSetId";
                        using (SqlCommand cmd = new SqlCommand(deleteResults, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@DataSetId", dataSetId);
                            cmd.ExecuteNonQuery();
                        }

                        string deleteValues = "DELETE FROM SortValues WHERE DataSetId = @DataSetId";
                        using (SqlCommand cmd = new SqlCommand(deleteValues, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@DataSetId", dataSetId);
                            cmd.ExecuteNonQuery();
                        }


                        string deleteDataSet = "DELETE FROM DataSets WHERE Id = @DataSetId";
                        using (SqlCommand cmd = new SqlCommand(deleteDataSet, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@DataSetId", dataSetId);
                            cmd.ExecuteNonQuery();
                        }

                        transaction.Commit();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка удаления набора данных: {ex.Message}");
                throw;
            }
        }

        public static bool CanDeleteDataSet(int dataSetId)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();


                    string query = @"
                SELECT COUNT(*) FROM SortResults 
                WHERE DataSetId = @DataSetId 
                AND ExecutedAt > DATEADD(hour, -1, GETDATE())";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@DataSetId", dataSetId);
                        int count = (int)cmd.ExecuteScalar();
                        return count == 0; 
                    }
                }
            }
            catch
            {
                return true;
            }
        }

        public static int CreateDataSet(string name, List<int> values)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    using (SqlTransaction transaction = conn.BeginTransaction())
                    {

                        string insertDataSet = "INSERT INTO DataSets (Name, CreatedAt) VALUES (@Name, @CreatedAt); SELECT SCOPE_IDENTITY();";
                        int dataSetId;

                        using (SqlCommand cmd = new SqlCommand(insertDataSet, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@Name", name);
                            cmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
                            dataSetId = Convert.ToInt32(cmd.ExecuteScalar());
                        }


                        for (int i = 0; i < values.Count; i++)
                        {
                            string insertValue = "INSERT INTO SortValues (DataSetId, Value, Position) VALUES (@DataSetId, @Value, @Position)";
                            using (SqlCommand cmd = new SqlCommand(insertValue, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@DataSetId", dataSetId);
                                cmd.Parameters.AddWithValue("@Value", values[i]);
                                cmd.Parameters.AddWithValue("@Position", i);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        transaction.Commit();
                        return dataSetId;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка создания набора данных: {ex.Message}");
                throw;
            }
        }

        public static void SaveSortReport(SortReport report)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    string createTableIfNotExists = @"
                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'SortReports')
                BEGIN
                    CREATE TABLE SortReports (
                        Id INT IDENTITY(1,1) PRIMARY KEY,
                        SortType NVARCHAR(50) NOT NULL,
                        ArraySize INT NOT NULL,
                        Comparisons INT NOT NULL,
                        Swaps INT NOT NULL,
                        DurationMs INT NOT NULL,
                        StartTime DATETIME NOT NULL,
                        EndTime DATETIME NOT NULL,
                        Status NVARCHAR(20) NOT NULL,
                        DataSetName NVARCHAR(100) NULL
                    )
                END";

                    using (SqlCommand createCmd = new SqlCommand(createTableIfNotExists, conn))
                    {
                        createCmd.ExecuteNonQuery();
                    }

                    string query = @"
                INSERT INTO SortReports (SortType, ArraySize, Comparisons, Swaps, DurationMs, StartTime, EndTime, Status, DataSetName)
                VALUES (@SortType, @ArraySize, @Comparisons, @Swaps, @DurationMs, @StartTime, @EndTime, @Status, @DataSetName)";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@SortType", report.SortType ?? "Неизвестно");
                        cmd.Parameters.AddWithValue("@ArraySize", report.ArraySize);
                        cmd.Parameters.AddWithValue("@Comparisons", report.Comparisons);
                        cmd.Parameters.AddWithValue("@Swaps", report.Swaps);
                        cmd.Parameters.AddWithValue("@DurationMs", report.DurationMs);
                        cmd.Parameters.AddWithValue("@StartTime", report.StartTime);
                        cmd.Parameters.AddWithValue("@EndTime", report.EndTime);
                        cmd.Parameters.AddWithValue("@Status", report.Status ?? "Завершена");
                        cmd.Parameters.AddWithValue("@DataSetName", report.DataSetName ?? "Неизвестно");

                        int rowsAffected = cmd.ExecuteNonQuery();
                        Debug.WriteLine($"Сохранено отчетов: {rowsAffected}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка сохранения отчета: {ex.Message}");
            }
        }

        public static List<SortReport> GetAllSortReports()
        {
            var reports = new List<SortReport>();

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT * FROM SortReports ORDER BY StartTime DESC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            reports.Add(new SortReport
                            {
                                Id = (int)reader["Id"],
                                SortType = reader["SortType"].ToString(),
                                ArraySize = (int)reader["ArraySize"],
                                Comparisons = (int)reader["Comparisons"],
                                Swaps = (int)reader["Swaps"],
                                DurationMs = (int)reader["DurationMs"],
                                StartTime = (DateTime)reader["StartTime"],
                                EndTime = (DateTime)reader["EndTime"],
                                Status = reader["Status"].ToString(),
                                DataSetName = reader["DataSetName"].ToString()
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка загрузки отчетов: {ex.Message}");
            }

            return reports;
        }

        public static void ClearSortReports()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "DELETE FROM SortReports";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка очистки отчетов: {ex.Message}");
            }
        }

        public static void CreateSortReportsTableIfNotExists()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"
                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'SortReports')
                BEGIN
                    CREATE TABLE SortReports (
                        Id INT IDENTITY(1,1) PRIMARY KEY,
                        SortType NVARCHAR(50) NOT NULL,
                        ArraySize INT NOT NULL,
                        Comparisons INT NOT NULL,
                        Swaps INT NOT NULL,
                        DurationMs INT NOT NULL,
                        StartTime DATETIME NOT NULL,
                        EndTime DATETIME NOT NULL,
                        Status NVARCHAR(20) NOT NULL,
                        DataSetName NVARCHAR(100) NULL
                    )
                END";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка создания таблицы SortReports: {ex.Message}");
            }
        }
    }
}