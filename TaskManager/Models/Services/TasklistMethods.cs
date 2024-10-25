using System;
using System.Data;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;

namespace TaskManager.Models.Services;

public class TasklistMethods {

    private readonly string _connectionString;

    public TasklistMethods(IConfiguration configuration) {

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


    public List<TasklistModel> GetTasklistsForUser(int userId) {
        List<TasklistModel> tasklists = new List<TasklistModel>();

        using (SqlConnection dbConnection = GetOpenConnection()) {
            string sql = @"
                SELECT 
                    TL.Id, 
                    TL.Title, 
                    TL.Description, 
                    TL.CreatedAt, 
                    TL.IsActive, 
                    LU.Role,
                    U.UserName,
                    U.Image
                FROM 
                    Tbl_Tasklist TL
                INNER JOIN 
                    Tbl_ListUser LU ON TL.Id = LU.ListId
                LEFT JOIN 
                    Tbl_User U ON LU.UserId = U.Id
                WHERE 
                    LU.UserId = @UserId
                ORDER BY TL.Id;";

            using (SqlCommand dbCommand = new SqlCommand(sql, dbConnection)) {
                dbCommand.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;

                using (SqlDataReader reader = dbCommand.ExecuteReader()) {
                    int lastTasklistId = -1;
                    TasklistModel currentTasklist = null;

                    while (reader.Read()) {
                        int tasklistId = reader.GetInt32(0);

                        if (tasklistId != lastTasklistId) {
                            currentTasklist = new TasklistModel {
                                Id = tasklistId,
                                Title = reader.GetString(1),
                                Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                                CreatedAt = reader.GetDateTime(3),
                                IsActive = reader.GetBoolean(4),
                                UserRole = reader.GetString(5),
                                Contributors = new List<ContributorModel>()
                            };

                            tasklists.Add(currentTasklist);
                            lastTasklistId = tasklistId;
                        }

                        if (currentTasklist != null && !reader.IsDBNull(6)) {
                            var contributor = new ContributorModel {
                                UserName = reader.GetString(6),
                                ProfileImage = reader.GetString(7)
                            };
                            currentTasklist.Contributors.Add(contributor);
                        }
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