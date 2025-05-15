using IMSystem.Server.Core.Interfaces.Services;
using BCryptNet = BCrypt.Net.BCrypt;

namespace IMSystem.Server.Infrastructure.Identity
{
    public class PasswordHasher : IPasswordHasher
    {
        public string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentNullException(nameof(password), "Password cannot be null or empty.");
            }
            return BCryptNet.HashPassword(password);
        }

        public bool VerifyHashedPassword(string hashedPassword, string providedPassword)
        {
            if (string.IsNullOrEmpty(hashedPassword) || string.IsNullOrEmpty(providedPassword))
            {
                return false; // Or throw an exception, depending on desired behavior for empty inputs
            }
            try
            {
                return BCryptNet.Verify(providedPassword, hashedPassword);
            }
            catch (BCrypt.Net.SaltParseException)
            {
                // This can happen if the hashedPassword is not a valid BCrypt hash.
                // Log this event for security monitoring.
                return false;
            }
        }
    }
}