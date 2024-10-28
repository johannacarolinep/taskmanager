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


    // get lists incl disabledd lists
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
                    -- Retrieve the current user's role specifically
                    (SELECT Role FROM Tbl_ListUser WHERE ListId = TL.Id AND UserId = @UserId) AS UserRole,
                    U.UserName,
                    U.Image
                FROM 
                    Tbl_Tasklist TL
                INNER JOIN 
                    Tbl_ListUser LU ON TL.Id = LU.ListId
                LEFT JOIN 
                    Tbl_User U ON LU.UserId = U.Id
                WHERE 
                    LU.IsActive = 1   -- Only include active contributors
                    AND LU.ListId IN (  -- Ensure this user is a member of the tasklist
                        SELECT ListId 
                        FROM Tbl_ListUser 
                        WHERE UserId = @UserId 
                        AND IsActive = 1  -- Only consider active membership
                    )
                ORDER BY TL.Id;";

            using (SqlCommand dbCommand = new SqlCommand(sql, dbConnection)) {
                dbCommand.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;

                using (SqlDataReader reader = dbCommand.ExecuteReader()) {
                    int lastTasklistId = -1;
                    TasklistModel currentTasklist = null;

                    while (reader.Read()) {
                        int tasklistId = reader.GetInt32(0);

                        // When tasklist ID changes, create a new TasklistModel
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

                        // Add contributor data to the current tasklist
                        if (currentTasklist != null) {
                            var contributor = new ContributorModel {
                                UserName = reader.IsDBNull(6) ? null : reader.GetString(6),
                                ProfileImage = reader.IsDBNull(7) ? null : reader.GetString(7)
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


    public TasklistModel GetTasklistById(int listId, int userId) {
        TasklistModel tasklist = null;

        using (SqlConnection dbConnection = GetOpenConnection()) {
            string sql = @"
                SELECT 
                    TL.Id, 
                    TL.Title, 
                    TL.Description, 
                    TL.CreatedAt, 
                    TL.IsActive, 
                    U.UserName AS CreatedByUserName,
                    LU.Role AS UserRole -- Fetch the role of the logged-in user for this task list
                FROM 
                    Tbl_Tasklist TL
                LEFT JOIN 
                    Tbl_User U ON TL.CreatedBy = U.Id
                LEFT JOIN 
                    Tbl_ListUser LU ON TL.Id = LU.ListId AND LU.UserId = @UserId -- Role of the current user
                WHERE 
                    TL.Id = @ListId;";

            using (SqlCommand dbCommand = new SqlCommand(sql, dbConnection)) {
                dbCommand.Parameters.Add("@ListId", SqlDbType.Int).Value = listId;
                dbCommand.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;

                using (SqlDataReader reader = dbCommand.ExecuteReader()) {
                    if (reader.Read()) {
                        tasklist = new TasklistModel {
                            Id = reader.GetInt32(reader.GetOrdinal("Id")),
                            Title = reader.GetString(reader.GetOrdinal("Title")),
                            Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
                            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                            IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                            CreatedByUserName = reader.IsDBNull(reader.GetOrdinal("CreatedByUserName")) ? null : reader.GetString(reader.GetOrdinal("CreatedByUserName")),
                            UserRole = reader.IsDBNull(reader.GetOrdinal("UserRole")) ? null : reader.GetString(reader.GetOrdinal("UserRole")) // Set UserRole
                        };
                    }
                }
            }
        }

        return tasklist;
    }


    public void SoftDeleteTasklist(int listId, int userId, out string errorMsg) {
        errorMsg = "";

        try {
            using (SqlConnection dbConnection = GetOpenConnection()) {
                string sql = @"
                    BEGIN TRY
                        BEGIN TRANSACTION;

                        -- Soft delete the tasklist
                        UPDATE Tbl_Tasklist
                        SET IsActive = 0
                        WHERE Id = @ListId;

                        -- Soft delete all associated tasks
                        UPDATE Tbl_Task
                        SET IsActive = 0
                        WHERE ListId = @ListId;

                        -- Delete the ListUser entry for the owner
                        DELETE FROM Tbl_ListUser
                        WHERE ListId = @ListId AND UserId = @UserId;

                        COMMIT TRANSACTION;
                    END TRY
                    BEGIN CATCH
                        -- Rollback the transaction on error
                        ROLLBACK TRANSACTION;
                    END CATCH;";

                using (SqlCommand cmd = new SqlCommand(sql, dbConnection)) {
                    cmd.Parameters.Add("@ListId", SqlDbType.Int).Value = listId;
                    cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;

                    cmd.ExecuteNonQuery(); // Execute the command
                }
            }
        } catch (Exception) {
            errorMsg = "Something went wrong when deleting the tasklist and its tasks. Please try again.";
        }
    }


    public bool UpdateTasklist(TasklistModel model, out string errorMsg) {
        errorMsg = "";
        try {
            using (SqlConnection dbConnection = GetOpenConnection()) {
                string sql = @"
                    UPDATE Tbl_Tasklist
                    SET Title = @Title,
                        Description = @Description
                    WHERE Id = @ListId";

                using (SqlCommand cmd = new SqlCommand(sql, dbConnection)) {
                    cmd.Parameters.Add("@Title", SqlDbType.NVarChar, 100).Value = model.Title;
                    cmd.Parameters.Add("@Description", SqlDbType.NVarChar, 255).Value = (object)model.Description ?? DBNull.Value;
                    cmd.Parameters.Add("@ListId", SqlDbType.Int).Value = model.Id;

                    int rowsAffected = cmd.ExecuteNonQuery();
                    return rowsAffected > 0;
                }
            }
        } catch (Exception ex) {
            errorMsg = $"Failed to update tasklist. Please try again. {ex}";
            return false;
        }
    }

}