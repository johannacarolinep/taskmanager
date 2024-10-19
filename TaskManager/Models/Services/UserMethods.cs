using System;
using System.Data;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;

namespace TaskManager.Models.Services
{
    public class UserMethods
    {
        private readonly string _connectionString;

        public UserMethods()
        {
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

        /// <summary>
        /// Inserts a new user into the database.
        /// </summary>
        /// <param name="user">The user model containing the user's details.</param>
        /// <param name="errorMsg">An output parameter containing an error message if the insertion fails.</param>
        /// <returns>The number of rows affected by the insert operation. 1 if successful, 0 if not.</returns>
        public int InsertUser(UserModel user, out string errorMsg)
        {
            errorMsg = "";
            try
            {
                using (SqlConnection dbConnection = GetOpenConnection())
                {
                    string sql = @"
                        INSERT INTO Tbl_user (UserName, Email, PasswordHash, CreatedAt, Image, IsActive, LastLogin) 
                        VALUES(@UserName, @Email, @PasswordHash, @CreatedAt, @Image, @IsActive, @LastLogin)";

                    using (SqlCommand dbCommand = new SqlCommand(sql, dbConnection))
                    {
                        dbCommand.Parameters.Add("@UserName", SqlDbType.VarChar, 30).Value = user.UserName;
                        dbCommand.Parameters.Add("@Email", SqlDbType.VarChar, 100).Value = user.Email;
                        dbCommand.Parameters.Add("@PasswordHash", SqlDbType.VarChar, 255).Value = user.PasswordHash;
                        dbCommand.Parameters.Add("@CreatedAt", SqlDbType.DateTime).Value = user.CreatedAt;
                        dbCommand.Parameters.Add("@Image", SqlDbType.VarChar, 255).Value = user.Image ?? (object)DBNull.Value;
                        dbCommand.Parameters.Add("@IsActive", SqlDbType.Bit).Value = user.IsActive;
                        dbCommand.Parameters.Add("@LastLogin", SqlDbType.DateTime).Value = user.LastLogin.HasValue ? (object)user.LastLogin.Value : DBNull.Value;

                        int result = dbCommand.ExecuteNonQuery();
                        if (result == 1)
                        {
                            return result; // Successfully inserted
                        }
                        else
                        {
                            errorMsg = "User could not be saved in the database.";
                            return 0;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                errorMsg = $"Error occurred while inserting user: {ex.Message}";
                return 0;
            }
        }

        /// <summary>
        /// Retrieves a user from the database by their ID.
        /// </summary>
        /// <param name="userId">The ID of the user to retrieve.</param>
        /// <returns>A UserModel object if found; otherwise, null.</returns>
        public UserModel GetUserById(string userId)
        {
            try
            {
                using (SqlConnection dbConnection = GetOpenConnection())
                {
                    string sql = "SELECT * FROM Tbl_user WHERE Id = @Id";
                    using (SqlCommand dbCommand = new SqlCommand(sql, dbConnection))
                    {
                        dbCommand.Parameters.Add("@Id", SqlDbType.NVarChar, 450).Value = userId;

                        using (SqlDataReader reader = dbCommand.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return MapReaderToUser(reader);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception or handle it as needed
                Console.WriteLine($"Error in GetUserById: {ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// Retrieves a user from the database by their normalized username.
        /// </summary>
        /// <param name="userName">The normalized username of the user to retrieve.</param>
        /// <returns>A UserModel object if found; otherwise, null.</returns>
        public UserModel? GetUserByUserName(string userName)
        {
            try
            {
                using (SqlConnection dbConnection = GetOpenConnection())
                {
                    string sql = "SELECT * FROM Tbl_user WHERE LOWER(UserName) = LOWER(@UserName)";
                    using (SqlCommand dbCommand = new SqlCommand(sql, dbConnection))
                    {
                        dbCommand.Parameters.Add("@UserName", SqlDbType.VarChar, 30).Value = userName;

                        using (SqlDataReader reader = dbCommand.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return MapReaderToUser(reader);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetUserByUserName: {ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// Maps a data reader to a UserModel object.
        /// </summary>
        /// <param name="reader">The SqlDataReader containing user data.</param>
        /// <returns>A UserModel object populated with data from the reader.</returns>
        private UserModel MapReaderToUser(SqlDataReader reader)
        {
            var user = new UserModel
            {
                Id = Convert.ToInt32(reader["Id"]),
                UserName = reader["UserName"].ToString(),
                Email = reader["Email"].ToString(),
                PasswordHash = reader["PasswordHash"].ToString(),
                CreatedAt = Convert.ToDateTime(reader["CreatedAt"]),
                Image = reader["Image"] != DBNull.Value ? reader["Image"].ToString() : null,
                IsActive = Convert.ToBoolean(reader["IsActive"]),
                LastLogin = reader["LastLogin"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["LastLogin"]) : null
                // Map other properties as needed
            };
            return user;
        }

        /// <summary>
        /// Updates an existing user in the database.
        /// </summary>
        /// <param name="user">The user model containing updated details.</param>
        /// <param name="errorMsg">An output parameter containing an error message if the update fails.</param>
        /// <returns>True if the update was successful; otherwise, false.</returns>
        public bool UpdateUser(UserModel user, out string errorMsg)
        {
            errorMsg = "";
            try
            {
                using (SqlConnection dbConnection = GetOpenConnection())
                {
                    string sql = @"
                        UPDATE Tbl_user SET 
                            UserName = @UserName,
                            NormalizedUserName = @NormalizedUserName,
                            Email = @Email,
                            PasswordHash = @PasswordHash,
                            Image = @Image,
                            IsActive = @IsActive,
                            LastLogin = @LastLogin
                        WHERE Id = @Id";

                    using (SqlCommand dbCommand = new SqlCommand(sql, dbConnection))
                    {
                        dbCommand.Parameters.Add("@Id", SqlDbType.NVarChar, 450).Value = user.Id;
                        dbCommand.Parameters.Add("@UserName", SqlDbType.VarChar, 30).Value = user.UserName;
                        dbCommand.Parameters.Add("@NormalizedUserName", SqlDbType.VarChar, 30).Value = user.NormalizedUserName;
                        dbCommand.Parameters.Add("@Email", SqlDbType.VarChar, 100).Value = user.Email;
                        dbCommand.Parameters.Add("@PasswordHash", SqlDbType.VarChar, 255).Value = user.PasswordHash;
                        dbCommand.Parameters.Add("@Image", SqlDbType.VarChar, 255).Value = user.Image ?? (object)DBNull.Value;
                        dbCommand.Parameters.Add("@IsActive", SqlDbType.Bit).Value = user.IsActive;
                        dbCommand.Parameters.Add("@LastLogin", SqlDbType.DateTime).Value = user.LastLogin.HasValue ? (object)user.LastLogin.Value : DBNull.Value;

                        int rowsAffected = dbCommand.ExecuteNonQuery();
                        return rowsAffected == 1;
                    }
                }
            }
            catch (Exception ex)
            {
                errorMsg = $"Error occurred while updating user: {ex.Message}";
                return false;
            }
        }

        /// <summary>
        /// Deletes a user from the database by their ID.
        /// </summary>
        /// <param name="userId">The ID of the user to delete.</param>
        /// <param name="errorMsg">An output parameter containing an error message if the deletion fails.</param>
        /// <returns>True if the deletion was successful; otherwise, false.</returns>
        public bool DeleteUser(string userId, out string errorMsg)
        {
            errorMsg = "";
            try
            {
                using (SqlConnection dbConnection = GetOpenConnection())
                {
                    string sql = "DELETE FROM Tbl_user WHERE Id = @Id";
                    using (SqlCommand dbCommand = new SqlCommand(sql, dbConnection))
                    {
                        dbCommand.Parameters.Add("@Id", SqlDbType.NVarChar, 450).Value = userId;

                        int rowsAffected = dbCommand.ExecuteNonQuery();
                        return rowsAffected == 1;
                    }
                }
            }
            catch (Exception ex)
            {
                errorMsg = $"Error occurred while deleting user: {ex.Message}";
                return false;
            }
        }
    }
}
