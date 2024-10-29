using Microsoft.AspNetCore.Identity;
using System.Threading;
using System.Threading.Tasks;
using TaskManager.Models.Services;

namespace TaskManager.Models.Stores
{
    public class UserStore : IUserStore<UserModel>, IUserPasswordStore<UserModel>, IUserEmailStore<UserModel>
    {
        private readonly UserMethods _userMethods;
        private readonly EncryptionHelper _encryptionHelper;

        public UserStore(UserMethods userMethods, IConfiguration configuration) {
            _userMethods = userMethods ?? throw new ArgumentNullException(nameof(userMethods));
            _encryptionHelper = new EncryptionHelper(configuration);
        }

        // 1. CreateAsync: Already implemented
        public async Task<IdentityResult> CreateAsync(UserModel user, CancellationToken cancellationToken)
        {
            string errorMsg;
            int result = _userMethods.InsertUser(user, out errorMsg);

            if (result > 0)
                return IdentityResult.Success;
            Console.WriteLine($"errormsg fr db: {errorMsg}");

            return IdentityResult.Failed(new IdentityError { Description = errorMsg });
        }

        // 2. UpdateAsync: Not implemented yet, but returning a default IdentityResult
        public async Task<IdentityResult> UpdateAsync(UserModel user, CancellationToken cancellationToken)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            string errorMsg;
            int result = _userMethods.UpdateUser(user, out errorMsg);

            if (result > 0)
            {
                return IdentityResult.Success;
            }

            return IdentityResult.Failed(new IdentityError { Description = errorMsg });
        }

        // 3. DeleteAsync: Not implemented yet, returning a default IdentityResult
        public Task<IdentityResult> DeleteAsync(UserModel user, CancellationToken cancellationToken) {

            // Check if the user is null
            if (user == null) {
                return Task.FromResult(IdentityResult.Failed(new IdentityError { Description = "User not found." }));
            }

            try {
                // Prepare for soft deletion
                // Generate encrypted versions of the username and email
                string encryptEmail = _encryptionHelper.Encrypt(user.Email);
                string encryptUsername = _encryptionHelper.Encrypt(user.UserName);

                // Create a DeletedUser instance
                var deletedUser = new DeletedUser{
                    UserId = user.Id,
                    EmailEncrypted = encryptEmail,
                    UserNameEncrypted = encryptUsername
                };

                user.Email = $"anonymoususer{user.Id}@email.com";
                user.UserName = $"anonymoususer{user.Id}";
                user.IsActive = false;

                // Attempt the deletion
                string errorMsg = "";
                if(!_userMethods.SoftDeleteUser(user, deletedUser, out errorMsg)) {
                    return Task.FromResult(IdentityResult.Failed(new IdentityError { Description = errorMsg }));
                }
                // User soft deleted
                return Task.FromResult(IdentityResult.Success);

            } catch (Exception) {
                return Task.FromResult(IdentityResult.Failed(new IdentityError { Description = "An error occurred during account deactivation. Please try again or contact support." }));
            }
        }


        // 4. FindByIdAsync: Placeholder, returning null
        public async Task<UserModel?> FindByIdAsync(string userId, CancellationToken cancellationToken)
        {
            // Implement user lookup by ID here later
            return await _userMethods.GetUserByIdAsync(userId);
        }

        // 5. FindByNameAsync: Placeholder, returning null
        public Task<UserModel?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
        {
            var user = _userMethods.GetUserByUserName(normalizedUserName);
            return Task.FromResult<UserModel?>(user);
        }

        // 6. Dispose: placeholder
        public void Dispose()
        {
            // No resources to dispose at this stage
        }

        // 7. GetUserIdAsync: Needs to return user ID, placeholder for now
        public Task<string> GetUserIdAsync(UserModel user, CancellationToken cancellationToken)
        {
            // Return the user's ID
            return Task.FromResult(user.Id.ToString());
        }

        // 8. GetUserNameAsync: Returns user's username
        public Task<string?> GetUserNameAsync(UserModel user, CancellationToken cancellationToken)
        {
            // Return the username
            return Task.FromResult(user.UserName);
        }

        // Placeholder, no operation performed
        public Task SetUserNameAsync(UserModel user, string? userName, CancellationToken cancellationToken)
        {
            user.UserName = userName;
            return Task.CompletedTask;
        }

        // 10. GetNormalizedUserNameAsync: Return normalized username
        public Task<string?> GetNormalizedUserNameAsync(UserModel user, CancellationToken cancellationToken)
        {
            // Return normalized username
            return Task.FromResult(user.UserName?.ToUpper());
        }

        // 11. SetNormalizedUserNameAsync: Placeholder for future implementation
        public Task SetNormalizedUserNameAsync(UserModel user, string? normalizedName, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task SetPasswordHashAsync(UserModel user, string? passwordHash, CancellationToken cancellationToken)
        {
            user.PasswordHash = passwordHash;
            return Task.CompletedTask;
        }

        public Task<string?> GetPasswordHashAsync(UserModel user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.PasswordHash);
        }

        public Task<bool> HasPasswordAsync(UserModel user, CancellationToken cancellationToken)
        {
            return Task.FromResult(!string.IsNullOrEmpty(user.PasswordHash));
        }


        // Email store
        public Task<string?> GetEmailAsync(UserModel user, CancellationToken cancellationToken)
        {
            // Assuming user.Email is already part of your UserModel
            return Task.FromResult(user.Email);
        }

        public Task SetEmailAsync(UserModel user, string? email, CancellationToken cancellationToken)
        {
            user.Email = email;
            return Task.CompletedTask;
        }

        public Task<bool> GetEmailConfirmedAsync(UserModel user, CancellationToken cancellationToken)
        {
            // Implement if you track email confirmation
            return Task.FromResult(true); // Return true if email confirmation is not implemented
        }

        public Task SetEmailConfirmedAsync(UserModel user, bool confirmed, CancellationToken cancellationToken)
        {
            // Implement email confirmation logic here
            return Task.CompletedTask;
        }

        public Task<UserModel?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
        {
            // Implement logic to find a user by their email address using _userMethods
            return _userMethods.FindByEmailAsync(normalizedEmail, cancellationToken);
        }

        public Task<string?> GetNormalizedEmailAsync(UserModel user, CancellationToken cancellationToken)
        {
            // Implement if you normalize emails (e.g., convert to lowercase)
            return Task.FromResult(user.Email?.ToLower());
        }

        public Task SetNormalizedEmailAsync(UserModel user, string? normalizedEmail, CancellationToken cancellationToken)
        {
            // Implement logic to store normalized email (e.g., lowercase email)
            user.Email = normalizedEmail?.ToLower();
            return Task.CompletedTask;
        }
    }
}
