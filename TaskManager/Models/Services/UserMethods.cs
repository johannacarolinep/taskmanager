using System;
using System.Data;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using TaskManager.Models;

namespace TaskManager.Models.Services
{
    public class UserMethods {
        private readonly string _connectionString;

        public UserMethods(IConfiguration configuration) {
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
        public async Task<UserModel?> GetUserByIdAsync(string userId)
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
                // Console.WriteLine($"Error in GetUserById: {ex.Message}");
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


        public async Task<UserModel> FindByEmailAsync(string email, CancellationToken cancellationToken) {
            
            try {
                using (SqlConnection dbConnection = GetOpenConnection()) {
                    string sql = "SELECT * FROM Tbl_user WHERE Email = @Email";
                    using (SqlCommand dbCommand = new SqlCommand(sql, dbConnection)) {
                        dbCommand.Parameters.AddWithValue("@Email", email);

                        using (SqlDataReader reader = await dbCommand.ExecuteReaderAsync(cancellationToken)) {
                            if (reader.Read()) {
                                return MapReaderToUser(reader);
                            }
                        }
                    }
                }
            }
            catch (Exception ex) {
                Console.WriteLine($"Error occurred while retrieving user by email: {ex.Message}");
                return null;
            }

            return null; // Return null if no user is found with the provided email
        }


        public async Task<DeletedUserModel?> FindDeletedByEmailAsync(string encryptedEmail) {
            try {
                using (SqlConnection dbConnection = GetOpenConnection()) {
                    string sql = "SELECT * FROM Tbl_DeletedUser WHERE EmailEncrypted = @EmailEncrypted";
                    using (SqlCommand dbCommand = new SqlCommand(sql, dbConnection)) {
                        dbCommand.Parameters.AddWithValue("@EmailEncrypted", encryptedEmail);

                        using (SqlDataReader reader = await dbCommand.ExecuteReaderAsync()) {
                            if (await reader.ReadAsync()) {
                                // Map the data to a DeletedUserModel
                                var deletedUser = new DeletedUserModel {
                                    Id = Convert.ToInt32(reader["Id"]),
                                    UserId = Convert.ToInt32(reader["UserId"]),
                                    EmailEncrypted = reader["EmailEncrypted"].ToString(),
                                    UserNameEncrypted = reader["UserNameEncrypted"].ToString(),
                                    DeletionDate = Convert.ToDateTime(reader["DeletionDate"])
                                };

                                return deletedUser;
                            }
                        }
                    }
                }
            } catch (Exception) {
                return null;
            }

            return null;
        }


        public async Task<DeletedUserModel?> FindDeletedByEmailOrUserNameAsync(string encryptedString) {
            try {
                using (SqlConnection dbConnection = GetOpenConnection()) {
                    string sql = "SELECT * FROM Tbl_DeletedUser WHERE EmailEncrypted = @EncryptedString OR UserNameEncrypted = @EncryptedString";
                    using (SqlCommand dbCommand = new SqlCommand(sql, dbConnection)) {
                        dbCommand.Parameters.AddWithValue("@EncryptedString", encryptedString);

                        using (SqlDataReader reader = await dbCommand.ExecuteReaderAsync()) {
                            if (await reader.ReadAsync()) {
                                // Map the data to a DeletedUserModel
                                var deletedUser = new DeletedUserModel {
                                    Id = Convert.ToInt32(reader["Id"]),
                                    UserId = Convert.ToInt32(reader["UserId"]),
                                    EmailEncrypted = reader["EmailEncrypted"].ToString(),
                                    UserNameEncrypted = reader["UserNameEncrypted"].ToString(),
                                    DeletionDate = Convert.ToDateTime(reader["DeletionDate"])
                                };

                                return deletedUser;
                            }
                        }
                    }
                }
            } catch (Exception) {
                return null;
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
        public int UpdateUser(UserModel user, out string errorMsg)
        {
            errorMsg = "";
            try
            {
                using (SqlConnection dbConnection = GetOpenConnection())
                {
                    string sql = @"
                        UPDATE Tbl_user SET 
                            UserName = @UserName,
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
                        dbCommand.Parameters.Add("@Email", SqlDbType.VarChar, 100).Value = user.Email;
                        dbCommand.Parameters.Add("@PasswordHash", SqlDbType.VarChar, 255).Value = user.PasswordHash;
                        dbCommand.Parameters.Add("@Image", SqlDbType.VarChar, 255).Value = user.Image ?? (object)DBNull.Value;
                        dbCommand.Parameters.Add("@IsActive", SqlDbType.Bit).Value = user.IsActive;
                        dbCommand.Parameters.Add("@LastLogin", SqlDbType.DateTime).Value = user.LastLogin.HasValue ? (object)user.LastLogin.Value : DBNull.Value;

                        int rowsAffected = dbCommand.ExecuteNonQuery();
                        
                        if (rowsAffected == 1) {
                            return rowsAffected;
                        } else {
                            errorMsg = "User could not be updated in the database.";
                            return 0;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                errorMsg = $"Error occurred while updating user: {ex.Message}";
                return 0;
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


        public bool SoftDeleteUser(UserModel user, DeletedUserModel deletedUser, out string errorMsg) {
            errorMsg = "";

            try {
                using (SqlConnection dbConnection = GetOpenConnection()) {
                    using (SqlTransaction transaction = dbConnection.BeginTransaction()) {
                        try {
                            // Step 1: Update the Tbl_User to set IsActive to 0 and anonymize email and username
                            string updateUserSql = @"
                                UPDATE Tbl_User
                                SET IsActive = 0, Email = @AnonymousEmail, UserName = @AnonymousUserName
                                WHERE Id = @UserId;";

                            using (SqlCommand updateUserCmd = new SqlCommand(updateUserSql, dbConnection, transaction)) {
                                updateUserCmd.Parameters.Add("@AnonymousEmail", SqlDbType.NVarChar).Value = $"anonymoususer{user.Id}@email.com";
                                updateUserCmd.Parameters.Add("@AnonymousUserName", SqlDbType.NVarChar).Value = $"anonymoususer{user.Id}";
                                updateUserCmd.Parameters.Add("@UserId", SqlDbType.Int).Value = user.Id;

                                updateUserCmd.ExecuteNonQuery(); // No need to check rows affected
                            }

                            // Step 2: Insert the deleted user into Tbl_DeletedUser
                            string insertDeletedUserSql = @"
                                INSERT INTO Tbl_DeletedUser (UserId, EmailEncrypted, UserNameEncrypted, DeletionDate)
                                VALUES (@UserId, @EmailEncrypted, @UserNameEncrypted, GETDATE());";

                            using (SqlCommand insertDeletedUserCmd = new SqlCommand(insertDeletedUserSql, dbConnection, transaction))
                            {
                                insertDeletedUserCmd.Parameters.Add("@UserId", SqlDbType.Int).Value = deletedUser.UserId;
                                insertDeletedUserCmd.Parameters.Add("@EmailEncrypted", SqlDbType.NVarChar).Value = deletedUser.EmailEncrypted;
                                insertDeletedUserCmd.Parameters.Add("@UserNameEncrypted", SqlDbType.NVarChar).Value = deletedUser.UserNameEncrypted;

                                insertDeletedUserCmd.ExecuteNonQuery(); // No need to check rows affected
                            }

                            // Commit the transaction if both operations succeed
                            transaction.Commit();
                            return true;
                        }
                        catch (Exception ex)
                        {
                            // Rollback if an error occurs
                            transaction.Rollback();
                            errorMsg = $"Error during soft delete: {ex.Message}";
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                errorMsg = $"Database connection error: {ex.Message}";
                return false;
            }
        }


        public bool CheckIfUsernameExists(string username, string encryptedUsername)
        {
            // Check if username exists in active users
            bool existsInActiveUsers = CheckUsernameInActiveUsers(username);
            
            // Check if encrypted username exists in deleted users
            bool existsInDeletedUsers = CheckEncryptedUsernameInDeletedUsers(encryptedUsername);

            // Return true if the username exists in either table
            return existsInActiveUsers || existsInDeletedUsers;
        }

        private bool CheckUsernameInActiveUsers(string username)
        {
            try
            {
                using (SqlConnection dbConnection = GetOpenConnection())
                {
                    string sql = "SELECT COUNT(*) FROM Tbl_User WHERE UserName COLLATE SQL_Latin1_General_CP1_CI_AS = @UserName";
                    using (SqlCommand dbCommand = new SqlCommand(sql, dbConnection))
                    {
                        dbCommand.Parameters.AddWithValue("@UserName", username);

                        int count = (int)dbCommand.ExecuteScalar();
                        return count > 0; // Return true if there is at least one match
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking active username: {ex.Message}");
                return false; // In case of an error, assume username doesn't exist
            }
        }

        private bool CheckEncryptedUsernameInDeletedUsers(string encryptedUsername)
        {
            try
            {
                using (SqlConnection dbConnection = GetOpenConnection())
                {
                    string sql = "SELECT COUNT(*) FROM Tbl_DeletedUser WHERE UserNameEncrypted = @UserNameEncrypted";
                    using (SqlCommand dbCommand = new SqlCommand(sql, dbConnection))
                    {
                        dbCommand.Parameters.AddWithValue("@UserNameEncrypted", encryptedUsername);

                        int count = (int)dbCommand.ExecuteScalar();
                        return count > 0; // Return true if there is at least one match
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking deleted username: {ex.Message}");
                return false; // In case of an error, assume username doesn't exist
            }
        }


        public bool DeleteDeletedUser(int deletedUserId) {
            try {
                using (SqlConnection dbConnection = GetOpenConnection()) {
                    string sql = "DELETE FROM Tbl_DeletedUser WHERE Id = @DeletedUserId";
                    using (SqlCommand dbCommand = new SqlCommand(sql, dbConnection)) {
                        dbCommand.Parameters.AddWithValue("@DeletedUserId", deletedUserId);

                        // Execute the command and return the number of affected rows
                        int affectedRows = dbCommand.ExecuteNonQuery();
                        return affectedRows > 0; // Return true if a row was deleted, otherwise false
                    }
                }
            } catch (Exception) {
                // Log the exception
                // Console.WriteLine($"Error occurred while deleting deleted user: {ex.Message}");
                return false; // Return false in case of error
            }
        }

    }
}
