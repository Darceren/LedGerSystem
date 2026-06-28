using System.Security.Cryptography;

var hash = "ADNVEC8RasQPv9dH66JReh1R62pgB121GZjPZKktW0c=";
var salt = "iK1DeUS+vrMn1Cii86U7Ww==";
var candidates = new[]
{
    "ChangeMe123!",
    "a123456a123456!",
    "Aa123456Aa123456",
    "shamim",
    "123456",
    "Admin123!"
};

var saltBytes = Convert.FromBase64String(salt);
var expected = Convert.FromBase64String(hash);

foreach (var pwd in candidates)
{
    using var derive = new Rfc2898DeriveBytes(pwd, saltBytes, 100_000, HashAlgorithmName.SHA256);
    var actual = derive.GetBytes(32);
    if (CryptographicOperations.FixedTimeEquals(expected, actual))
    {
        Console.WriteLine($"MATCH: {pwd}");
    }
}

Console.WriteLine("Done.");
