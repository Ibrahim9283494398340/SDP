using System.Security.Cryptography;

namespace OxuMen.Security;

// PBKDF2-SHA256 password hashes, stored as
// "pbkdf2-sha256.{iterations}.{salt}.{hash}" (salt and hash in Base64).
public static class PasswordHashing
{
    private const string Algorithm = "pbkdf2-sha256";
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 210_000;

    // Used when the user name does not exist, so a failed login takes the
    // same time whether or not the account exists.
    private static readonly string DummyHash = Hash(Guid.NewGuid().ToString());

    public static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);

        var key = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            KeySize);

        return string.Join(
            '.',
            Algorithm,
            Iterations,
            Convert.ToBase64String(salt),
            Convert.ToBase64String(key));
    }

    public static bool Verify(string password, string storedHash)
    {
        var parts = storedHash.Split('.');

        if (parts.Length != 4 ||
            parts[0] != Algorithm ||
            !int.TryParse(parts[1], out var iterations) ||
            iterations < 1)
        {
            return false;
        }

        byte[] salt;
        byte[] expected;

        try
        {
            salt = Convert.FromBase64String(parts[2]);
            expected = Convert.FromBase64String(parts[3]);
        }
        catch (FormatException)
        {
            return false;
        }

        var actual = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            expected.Length);

        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    public static void VerifyAgainstDummy(string password)
    {
        Verify(password, DummyHash);
    }

    public static string NewSecurityStamp()
    {
        return Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
    }

    public static string GeneratePassword(int length = 14)
    {
        // No look-alike characters (0/O, 1/l/I).
        const string choices =
            "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789";

        return RandomNumberGenerator.GetString(choices, length);
    }
}
