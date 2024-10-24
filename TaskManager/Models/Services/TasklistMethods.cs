using System;
using System.Data;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;

namespace TaskManager.Models.Services;

public class TasklistMethods {

    private readonly string _connectionString;

    public TasklistMethods() {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appSettings.json")
                .Build();

            _connectionString = builder.GetConnectionString("DefaultConnection");
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


    public List<TasklistModel> GetTasklistsForUser(int userId) {
        List<TasklistModel> tasklists = new List<TasklistModel>();

        using (SqlConnection dbConnection = GetOpenConnection()) {
            // SQL query to get task lists and the user's role in each list
            string sql = @"
                SELECT 
                    TL.Id, 
                    TL.Title, 
                    TL.Description, 
                    TL.CreatedAt, 
                    TL.IsActive, 
                    LU.Role,
                    (
                        SELECT STRING_AGG(U.UserName, ', ') 
                        FROM Tbl_ListUser LU2
                        INNER JOIN Tbl_User U ON LU2.UserId = U.Id
                        WHERE LU2.ListId = TL.Id
                    ) AS Contributors
                FROM 
                    Tbl_Tasklist TL
                INNER JOIN 
                    Tbl_ListUser LU ON TL.Id = LU.ListId
                WHERE 
                    LU.UserId = @UserId;";

            using (SqlCommand dbCommand = new SqlCommand(sql, dbConnection)) {
                dbCommand.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;

                using (SqlDataReader reader = dbCommand.ExecuteReader()) {
                    while (reader.Read()) {
                        TasklistModel tasklist = new TasklistModel {
                            Id = reader.GetInt32(0),
                            Title = reader.GetString(1),
                            Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                            CreatedAt = reader.GetDateTime(3),
                            IsActive = reader.GetBoolean(4),
                            // Custom property to store the user's role in this task list
                            UserRole = reader.GetString(5), // owner, admin, or contributor
                            Contributors = reader.IsDBNull(6) ? "" : reader.GetString(6) // List of usernames
                        };
                        tasklists.Add(tasklist);
                    }
                }
            }
        }

        return tasklists;
    }


    public bool CreateTasklist(int userId, TasklistModel tasklist, out string errorMsg) {

        errorMsg = "";

        using (SqlConnection dbConnection = GetOpenConnection()) {
            using (SqlTransaction transaction = dbConnection.BeginTransaction()) {
                try {
                    // Insert the tasklist
                    string tasklistSql = @"
                        INSERT INTO Tbl_Tasklist (Title, Description, CreatedAt, IsActive, CreatedBy) 
                        OUTPUT INSERTED.Id
                        VALUES (@Title, @Description, @CreatedAt, @IsActive, @CreatedBy)";

                    int tasklistId;
                    using (SqlCommand tasklistCommand = new SqlCommand(tasklistSql, dbConnection, transaction)) {
                        tasklistCommand.Parameters.AddWithValue("@Title", tasklist.Title);
                        tasklistCommand.Parameters.AddWithValue("@Description", tasklist.Description ?? (object)DBNull.Value);
                        tasklistCommand.Parameters.AddWithValue("@CreatedAt", tasklist.CreatedAt);
                        tasklistCommand.Parameters.AddWithValue("@IsActive", tasklist.IsActive);
                        tasklistCommand.Parameters.AddWithValue("@CreatedBy", userId);

                        // Execute and get the newly inserted Tasklist ID
                        tasklistId = (int)tasklistCommand.ExecuteScalar();
                    }

                    // Insert the ListUser (owner of the newly created tasklist)
                    string listUserSql = @"
                        INSERT INTO Tbl_ListUser (UserId, ListId, Role, InvitationStatus, IsActive) 
                        VALUES (@UserId, @ListId, @Role, @InvitationStatus, @IsActive)";

                    using (SqlCommand listUserCommand = new SqlCommand(listUserSql, dbConnection, transaction)) {
                        listUserCommand.Parameters.AddWithValue("@UserId", userId);
                        listUserCommand.Parameters.AddWithValue("@ListId", tasklistId);
                        listUserCommand.Parameters.AddWithValue("@Role", UserListRole.Owner.ToString());
                        listUserCommand.Parameters.AddWithValue("@InvitationStatus", InvitationStatus.Accepted.ToString()); // Automatically accepted
                        listUserCommand.Parameters.AddWithValue("@IsActive", true);

                        listUserCommand.ExecuteNonQuery();
                    }

                    // Commit transaction
                    transaction.Commit();
                    return true;
                } catch (Exception) {
                    // Rollback the transaction if an error occurs
                    transaction.Rollback();
                    errorMsg = "List could not be created";
                    return false;
                }
            }
        }
    }

}