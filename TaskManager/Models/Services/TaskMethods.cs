using System;
using System.Data;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;

namespace TaskManager.Models.Services;

public class TaskMethods {

    private readonly string _connectionString;

    public TaskMethods(IConfiguration configuration) {

            _connectionString = configuration.GetConnectionString("DefaultConnection");
    }

    /// <summary>
    /// Creates and opens a SQL connection using the configuration connection string.
    /// </summary>
    /// <returns>An open SqlConnection object.</returns>
    private SqlConnection GetOpenConnection()
    {
        var dbConnection = new SqlConnection(_connectionString);
        dbConnection.Open();
        return dbConnection;
    }


    public List<TaskModel> GetActiveTasksByListId(int listId) {
        var tasks = new List<TaskModel>();

        using (SqlConnection dbConnection = GetOpenConnection()) {
            string sql = @"
                SELECT 
                    Id, 
                    ListId, 
                    Description, 
                    Priority, 
                    Status, 
                    Deadline, 
                    CreatedAt, 
                    IsActive
                FROM 
                    Tbl_Task
                WHERE 
                    ListId = @ListId AND IsActive = 1
                ORDER BY 
                    Priority ASC, Deadline DESC;";

            using (SqlCommand dbCommand = new SqlCommand(sql, dbConnection)) {
                dbCommand.Parameters.Add("@ListId", SqlDbType.Int).Value = listId;

                using (SqlDataReader reader = dbCommand.ExecuteReader()) {
                    while (reader.Read()) {
                        var task = new TaskModel {
                            Id = reader.GetInt32(reader.GetOrdinal("Id")),
                            ListId = reader.GetInt32(reader.GetOrdinal("ListId")),
                            Description = reader.GetString(reader.GetOrdinal("Description")),
                            Priority = reader.GetByte(reader.GetOrdinal("Priority")),
                            Status = (TaskStatus)Enum.Parse(typeof(TaskStatus), reader.GetString(reader.GetOrdinal("Status"))),
                            Deadline = reader.IsDBNull(reader.GetOrdinal("Deadline")) ? null : reader.GetDateTime(reader.GetOrdinal("Deadline")),
                            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                            IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))
                        };
                        tasks.Add(task);
                    }
                }
            }
        }

        return tasks;
    }


    public void AddTask(TaskModel task, out string errorMsg) {
        
        errorMsg = "";

        try {    
            using (SqlConnection dbConnection = GetOpenConnection()) {
                string sql = @"
                    INSERT INTO Tbl_Task (ListId, Description, Priority, Status, Deadline, CreatedAt, IsActive)
                    VALUES (@ListId, @Description, @Priority, @Status, @Deadline, @CreatedAt, @IsActive)";

                using (SqlCommand dbCommand = new SqlCommand(sql, dbConnection)) {
                    // Set parameter values
                    dbCommand.Parameters.Add("@ListId", SqlDbType.Int).Value = task.ListId;
                    dbCommand.Parameters.Add("@Description", SqlDbType.NVarChar, 100).Value = task.Description;
                    dbCommand.Parameters.Add("@Priority", SqlDbType.TinyInt).Value = task.Priority;
                    dbCommand.Parameters.Add("@Status", SqlDbType.NVarChar, 20).Value = task.Status.ToString();
                    dbCommand.Parameters.Add("@Deadline", SqlDbType.DateTime).Value = task.Deadline;
                    dbCommand.Parameters.Add("@CreatedAt", SqlDbType.DateTime).Value = task.CreatedAt;
                    dbCommand.Parameters.Add("@IsActive", SqlDbType.Bit).Value = task.IsActive;

                    // Execute the insert command
                    dbCommand.ExecuteNonQuery();
                }
            }
        } catch (Exception) {
            errorMsg = "An error occurred while adding the task. Please try again.";
        }
    }


    public TaskModel? GetActiveTaskById(int taskId) {
        using (SqlConnection dbConnection = GetOpenConnection()) {
            string sql = @"
                SELECT Id, ListId, Description, Priority, Status, Deadline, CreatedAt, IsActive
                FROM Tbl_Task
                WHERE Id = @Id AND IsActive = 1";

            using (SqlCommand dbCommand = new SqlCommand(sql, dbConnection)) {
                dbCommand.Parameters.Add("@Id", SqlDbType.Int).Value = taskId;

                using (SqlDataReader reader = dbCommand.ExecuteReader()) {
                    if (reader.Read()) {
                        // Map the result to TaskModel
                        var task = new TaskModel {
                            Id = reader.GetInt32(reader.GetOrdinal("Id")),
                            ListId = reader.GetInt32(reader.GetOrdinal("ListId")),
                            Description = reader.GetString(reader.GetOrdinal("Description")),
                            Priority = reader.GetByte(reader.GetOrdinal("Priority")),
                            Status = Enum.Parse<TaskStatus>(reader.GetString(reader.GetOrdinal("Status"))),
                            Deadline = reader.GetDateTime(reader.GetOrdinal("Deadline")),
                            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                            IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))
                        };
                        return task;
                    }
                }
            }
        }
        return null;
    }


    public bool UpdateTask(TaskModel task, out string errorMsg) {
        errorMsg = "";
        try {
            using (SqlConnection dbConnection = GetOpenConnection()) {
                string sql = @"
                    UPDATE Tbl_Task
                    SET Description = @Description,
                        Priority = @Priority,
                        Status = @Status,
                        Deadline = @Deadline,
                        IsActive = @IsActive
                    WHERE Id = @Id AND IsActive = 1";

                using (SqlCommand dbCommand = new SqlCommand(sql, dbConnection)) {
                    // Set parameter values
                    dbCommand.Parameters.Add("@Id", SqlDbType.Int).Value = task.Id;
                    dbCommand.Parameters.Add("@Description", SqlDbType.NVarChar, 100).Value = task.Description;
                    dbCommand.Parameters.Add("@Priority", SqlDbType.TinyInt).Value = task.Priority;
                    dbCommand.Parameters.Add("@Status", SqlDbType.NVarChar, 20).Value = task.Status.ToString();
                    dbCommand.Parameters.Add("@Deadline", SqlDbType.DateTime).Value = task.Deadline;
                    dbCommand.Parameters.Add("@IsActive", SqlDbType.Bit).Value = task.IsActive;

                    // Execute the update command
                    int rowsAffected = dbCommand.ExecuteNonQuery();
                    return rowsAffected > 0;
                }
            }
        } catch (Exception) {
            errorMsg = "An error occurred while updating the task. Please try again.";
            return false;
        }
    }


    public bool SoftDeleteTask(int taskId, out string errorMsg) {
        errorMsg = "";
        try {
            using (SqlConnection dbConnection = GetOpenConnection()) {
                string sql = @"
                    UPDATE Tbl_Task
                    SET IsActive = 0
                    WHERE Id = @Id";

                using (SqlCommand dbCommand = new SqlCommand(sql, dbConnection)) {
                    dbCommand.Parameters.Add("@Id", SqlDbType.Int).Value = taskId;

                    int rowsAffected = dbCommand.ExecuteNonQuery();
                    return rowsAffected > 0;
                }
            }
        } catch (Exception) {
            errorMsg = "An error occurred while deleting the task. Please try again.";
            return false;
        }
    }

}