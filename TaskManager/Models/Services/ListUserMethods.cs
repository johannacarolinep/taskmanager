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


    public UserListRole? GetUserRoleInList(int listId, int userId) {
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
}
