using System.Security.Cryptography;

public static class PasswordHelper
{
    // Salt boyutu (rastgele eklenen değer)
    private const int SaltSize = 16;
    // Hash boyutu
    private const int HashSize = 32;
    // Hashleme için iterasyon sayısı
    private const int Iterations = 100000;

    // Şifreyi hashler ve salt ile birlikte saklanacak string formatında döner
    public static string HashPassword(string password)
    {
        if (password == null) throw new ArgumentNullException(nameof(password));

        // Rastgele salt oluştur
        byte[] salt = new byte[SaltSize];
        RandomNumberGenerator.Fill(salt);

        byte[] hash;
        // PBKDF2 algoritması ile şifreyi hashle
        using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations))
        {
            hash = pbkdf2.GetBytes(HashSize);
        }

        // Format: iterasyon.salt.hash
        string result = $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
        return result;
    }

    // Girilen şifreyi saklanan hash ile doğrular
    public static bool VerifyPassword(string password, string storedHash)
    {
        if (password == null) throw new ArgumentNullException(nameof(password));
        if (string.IsNullOrWhiteSpace(storedHash)) return false;

        // Hash stringini parçala
        var parts = storedHash.Split('.');
        if (parts.Length != 3) return false;

        int iterations = int.Parse(parts[0]);
        byte[] salt = Convert.FromBase64String(parts[1]);
        byte[] storedHashBytes = Convert.FromBase64String(parts[2]);

        byte[] computedHash;
        // Aynı salt ve iterasyon ile hash oluştur
        using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations))
        {
            computedHash = pbkdf2.GetBytes(storedHashBytes.Length);
        }

        // Byte dizilerini karşılaştır
        return AreByteArraysEqual(storedHashBytes, computedHash);
    }

    // İki byte dizisinin eşit olup olmadığını sabit zamanlı olarak kontrol eder
    private static bool AreByteArraysEqual(byte[] a, byte[] b)
    {
        if (a == null || b == null) return false;
        if (a.Length != b.Length) return false;

        int diff = 0;
        for (int i = 0; i < a.Length; i++)
        {
            diff |= a[i] ^ b[i]; // XOR ile farkları tespit et
        }
        return diff == 0;
    }
}
