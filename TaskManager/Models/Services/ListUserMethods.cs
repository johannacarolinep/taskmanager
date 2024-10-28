using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using TaskManager.ViewModels;
using TaskManager.Models;

public class ListUserMethods {
    private readonly string _connectionString;

    public ListUserMethods(IConfiguration configuration) {
        _connectionString = configuration.GetConnectionString("DefaultConnection");
    }

    private SqlConnection GetOpenConnection() {
        var dbConnection = new SqlConnection(_connectionString);
        dbConnection.Open();
        return dbConnection;
    }

    public List<TasklistDetailViewModel.ContributorInfo> GetContributorsByListId(int listId) {
        var contributors = new List<TasklistDetailViewModel.ContributorInfo>();

        using (SqlConnection dbConnection = GetOpenConnection()) {
            string sql = @"
                SELECT LU.Id AS ListUserId, 
                    LU.UserId, 
                    U.UserName, 
                    U.Image, 
                    LU.Role
                FROM Tbl_ListUser LU
                INNER JOIN Tbl_User U ON LU.UserId = U.Id
                WHERE LU.ListId = @ListId AND LU.IsActive = 1;";

            using (SqlCommand dbCommand = new SqlCommand(sql, dbConnection)) {
                dbCommand.Parameters.Add("@ListId", SqlDbType.Int).Value = listId;

                using (SqlDataReader reader = dbCommand.ExecuteReader()) {
                    while (reader.Read()) {
                        var contributor = new TasklistDetailViewModel.ContributorInfo {
                            ListUserId = reader.GetInt32(reader.GetOrdinal("ListUserId")),
                            UserId = reader.IsDBNull(reader.GetOrdinal("UserId")) ? 0 : reader.GetInt32(reader.GetOrdinal("UserId")),
                            Username = reader.IsDBNull(reader.GetOrdinal("UserName")) ? "Unknown" : reader.GetString(reader.GetOrdinal("UserName")),
                            Image = reader.IsDBNull(reader.GetOrdinal("Image")) ? null : reader.GetString(reader.GetOrdinal("Image")),
                            Role = reader.IsDBNull(reader.GetOrdinal("Role")) ? "Contributor" : reader.GetString(reader.GetOrdinal("Role"))
                        };

                        contributors.Add(contributor);
                    }
                }
            }
        }

        return contributors;
    }


    public UserListRole? GetUserRoleInList(int userId, int listId) {
        using (SqlConnection dbConnection = GetOpenConnection()) {
            string sql = @"
                SELECT Role FROM Tbl_ListUser 
                WHERE ListId = @ListId AND UserId = @UserId AND IsActive = 1";

            using (SqlCommand dbCommand = new SqlCommand(sql, dbConnection)) {
                dbCommand.Parameters.Add("@ListId", SqlDbType.Int).Value = listId;
                dbCommand.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;

                var result = dbCommand.ExecuteScalar();
                if (result != null) {
                    return Enum.Parse<UserListRole>(result.ToString());
                }
            }
        }
        return null;
    }


    public void AddListUser(ListUserModel newListUser, out string errorMsg) {
        errorMsg = string.Empty;

        if (newListUser == null) {
            errorMsg = "List user cannot be null.";
            return;
        }

        try {
            using (SqlConnection dbConnection = GetOpenConnection()) {
                string sql = @"
                    INSERT INTO Tbl_listuser (UserId, ListId, Role, InviteEmail, InvitationStatus, InvitationSentAt, IsActive)
                    VALUES (@UserId, @ListId, @Role, @InviteEmail, @InvitationStatus, @InvitationSentAt, @IsActive);";

                using (SqlCommand dbCommand = new SqlCommand(sql, dbConnection)) {
                    dbCommand.Parameters.AddWithValue("@UserId", (object)newListUser.UserId ?? DBNull.Value);
                    dbCommand.Parameters.AddWithValue("@ListId", newListUser.ListId);
                    dbCommand.Parameters.AddWithValue("@Role", newListUser.Role.ToString());
                    dbCommand.Parameters.AddWithValue("@InviteEmail", (object)newListUser.InviteEmail ?? DBNull.Value);
                    dbCommand.Parameters.AddWithValue("@InvitationStatus", newListUser.InvitationStatus.ToString());
                    dbCommand.Parameters.AddWithValue("@InvitationSentAt", (object)newListUser.InvitationSentAt ?? DBNull.Value);
                    dbCommand.Parameters.AddWithValue("@IsActive", newListUser.IsActive);

                    dbCommand.ExecuteNonQuery();
                }
            }
        } catch (SqlException ex) {
            Console.WriteLine($"SQL exception - Number: {ex.Number}");
            if (ex.Number == 2601) {
                errorMsg = "Looks like this user has already been invited to the list.";
            } else {
                errorMsg = $"Error occurred while adding list user.";
            }
        } catch (Exception) {
            errorMsg = "Unforeseen error while creating the invitation. Please try again.";
        }
    }


    public List<TasklistModel> GetTasklistsWithPendingInvitations(int userId) {
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
                    U.UserName AS ContributorName,
                    U.Image AS ContributorImage
                FROM 
                    Tbl_Tasklist TL
                INNER JOIN 
                    Tbl_ListUser LU ON TL.Id = LU.ListId
                LEFT JOIN 
                    Tbl_User U ON LU.UserId = U.Id
                WHERE 
                    TL.Id IN (
                        SELECT ListId 
                        FROM Tbl_ListUser 
                        WHERE UserId = @UserId AND InvitationStatus = 'pending'
                    )
                    AND (LU.UserId IS NULL OR LU.IsActive = 1)  -- Include null for invite emails or active users only
                ORDER BY 
                    TL.Id, U.UserName;";

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
                                UserRole = reader.IsDBNull(5) ? null : reader.GetString(5),
                                Contributors = new List<ContributorModel>()
                            };

                            tasklists.Add(currentTasklist);
                            lastTasklistId = tasklistId;
                        }

                        // Add contributor data to the current tasklist
                        if (currentTasklist != null && !reader.IsDBNull(6)) {
                            var contributor = new ContributorModel {
                                UserName = reader.GetString(6),
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


    public ListUserModel? GetListUserByListAndUser(int listId, int userId) {
        using (SqlConnection dbConnection = GetOpenConnection()) {
            string sql = @"
                SELECT Id, UserId, ListId, Role, InvitationStatus, IsActive 
                FROM Tbl_ListUser
                WHERE ListId = @ListId AND UserId = @UserId";

            using (SqlCommand dbCommand = new SqlCommand(sql, dbConnection)) {
                dbCommand.Parameters.Add("@ListId", SqlDbType.Int).Value = listId;
                dbCommand.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;

                using (SqlDataReader reader = dbCommand.ExecuteReader()) {
                    if (reader.Read()) {
                        return new ListUserModel {
                            Id = reader.GetInt32(0),
                            UserId = reader.GetInt32(1),
                            ListId = reader.GetInt32(2),
                            Role = Enum.TryParse(reader.GetString(3), out UserListRole parsedRole) ? parsedRole : UserListRole.Contributor,
                            InvitationStatus = Enum.TryParse(reader.GetString(4), out InvitationStatus parsedStatus) ? parsedStatus : InvitationStatus.Pending,
                            IsActive = reader.GetBoolean(5)
                        };
                    }
                }
            }
        }

        return null;
    }


    public void UpdateListUser(ListUserModel listUser, out string errorMsg) {

        errorMsg = "";

        try {
            using (SqlConnection dbConnection = GetOpenConnection()) {
            string sql = @"
                UPDATE Tbl_ListUser
                SET 
                    InvitationStatus = @InvitationStatus,
                    IsActive = @IsActive
                WHERE Id = @Id";

                using (SqlCommand dbCommand = new SqlCommand(sql, dbConnection)) {
                    dbCommand.Parameters.Add("@InvitationStatus", SqlDbType.VarChar).Value = listUser.InvitationStatus;
                    dbCommand.Parameters.Add("@IsActive", SqlDbType.Bit).Value = listUser.IsActive;
                    dbCommand.Parameters.Add("@Id", SqlDbType.Int).Value = listUser.Id;

                    dbCommand.ExecuteNonQuery();
                }
            }
        } catch (Exception) {
            errorMsg = "Sorry, something went wrong when accepting the invite.";
        }
        
    }


    public bool DeleteListUserByUserAndList(int userId, int listId, out string errorMsg) {
        errorMsg = "";

        try {
            using (SqlConnection dbConnection = GetOpenConnection()) {
                string sql = @"
                    DELETE FROM Tbl_ListUser
                    WHERE UserId = @UserId AND ListId = @ListId";

                using (SqlCommand dbCommand = new SqlCommand(sql, dbConnection)) {
                    dbCommand.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
                    dbCommand.Parameters.Add("@ListId", SqlDbType.Int).Value = listId;

                    int rowsAffected = dbCommand.ExecuteNonQuery();
                    
                    if (rowsAffected == 0) {
                        errorMsg = "No invitation found to decline.";
                        return false;
                    }
                }
            }
            
            return true;
        } catch (Exception) {
            errorMsg = "An error occurred while declining the invitation.";
            return false;
        }
    }


    public void AssignUserIdToInvitations(int userId, string email, out string errorMsg) {
        errorMsg = "";
        try {
            using (SqlConnection dbConnection = GetOpenConnection()) {
                string sql = @"
                    UPDATE Tbl_ListUser 
                    SET UserId = @UserId, InviteEmail = NULL 
                    WHERE InviteEmail = @Email;";

                using (SqlCommand dbCommand = new SqlCommand(sql, dbConnection)) {
                    dbCommand.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
                    dbCommand.Parameters.Add("@Email", SqlDbType.NVarChar).Value = email;

                    dbCommand.ExecuteNonQuery();
                }
            }
        } catch (Exception) {
            errorMsg = "We failed to connect your new account to existing invitations. Please ask your collaborators to re-invite you.";
        }
    }

}
