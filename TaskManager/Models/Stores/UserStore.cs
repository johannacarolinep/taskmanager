using Microsoft.AspNetCore.Identity;
using System.Threading;
using System.Threading.Tasks;
using TaskManager.Models.Services;
using TaskManager.Models;

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


        public async Task<IdentityResult> CreateAsync(UserModel user, CancellationToken cancellationToken) {
            string errorMsg;
            int result = _userMethods.InsertUser(user, out errorMsg);

            if (result > 0) {
                return IdentityResult.Success;
            }

            return IdentityResult.Failed(new IdentityError { Description = errorMsg });
        }


        public async Task<IdentityResult> UpdateAsync(UserModel user, CancellationToken cancellationToken) {
            if (user == null) {
                throw new ArgumentNullException(nameof(user));
            }

            string errorMsg;
            int result = _userMethods.UpdateUser(user, out errorMsg);

            if (result > 0) {
                return IdentityResult.Success;
            }

            return IdentityResult.Failed(new IdentityError { Description = errorMsg });
        }

        public Task<IdentityResult> DeleteAsync(UserModel user, CancellationToken cancellationToken) {

            if (user == null) {
                return Task.FromResult(IdentityResult.Failed(new IdentityError { Description = "User not found." }));
            }

            try {
                // Prepare for soft deletion
                // Generate encrypted versions of the username and email
                string encryptEmail = _encryptionHelper.Encrypt(user.Email.ToLower());
                string encryptUsername = _encryptionHelper.Encrypt(user.UserName.ToLower());

                // Create a DeletedUser instance
                var deletedUser = new DeletedUserModel{
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


        public async Task<UserModel?> FindByIdAsync(string userId, CancellationToken cancellationToken) {
            return await _userMethods.GetUserByIdAsync(userId);
        }

        public Task<UserModel?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
        {
            var user = _userMethods.GetUserByUserName(normalizedUserName);
            return Task.FromResult<UserModel?>(user);
        }

        //  placeholder
        public void Dispose() {
            // Not yet implemented
        }

        public Task<string> GetUserIdAsync(UserModel user, CancellationToken cancellationToken)
        {
            // Return the user's ID
            return Task.FromResult(user.Id.ToString());
        }

        public Task<string?> GetUserNameAsync(UserModel user, CancellationToken cancellationToken)
        {
            // Return the username
            return Task.FromResult(user.UserName);
        }

        // Placeholder, not fully implemented
        public Task SetUserNameAsync(UserModel user, string? userName, CancellationToken cancellationToken)
        {
            user.UserName = userName;
            return Task.CompletedTask;
        }

        public Task<string?> GetNormalizedUserNameAsync(UserModel user, CancellationToken cancellationToken)
        {
            // Return normalized username
            return Task.FromResult(user.UserName?.ToUpper());
        }

        // Placeholder
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
            return Task.FromResult(user.Email);
        }

        public Task SetEmailAsync(UserModel user, string? email, CancellationToken cancellationToken)
        {
            user.Email = email;
            return Task.CompletedTask;
        }

        public Task<bool> GetEmailConfirmedAsync(UserModel user, CancellationToken cancellationToken)
        {
            return Task.FromResult(true); // Return true if email confirmation is not implemented
        }

        public Task SetEmailConfirmedAsync(UserModel user, bool confirmed, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task<UserModel?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
        {
            return _userMethods.FindByEmailAsync(normalizedEmail, cancellationToken);
        }


        public Task<string?> GetNormalizedEmailAsync(UserModel user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.Email?.ToLower());
        }

        public Task SetNormalizedEmailAsync(UserModel user, string? normalizedEmail, CancellationToken cancellationToken)
        {
            user.Email = normalizedEmail?.ToLower();
            return Task.CompletedTask;
        }
    }
}