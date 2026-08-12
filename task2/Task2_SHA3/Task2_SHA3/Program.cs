using System.Numerics;
using System.Security.Cryptography;
using System.Text;

//if (SHA3_256.IsSupported) {Console.WriteLine("true");}

var path = "D:\\Itransition-internship\\task2\\task2";

String[] files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);

List<string> hashes = new List<string>();

foreach (var file in files) 
{

    Byte[] input = File.ReadAllBytes(file);
    Byte[] hash = SHA3_256.HashData(input);
    var hex = Convert.ToHexString(hash).ToLower();
    hashes.Add(hex);

}

Console.WriteLine(hashes.Count);
Console.WriteLine(hashes[0]);

BigInteger CalculateKey(string hash)
{
    BigInteger accumulator = BigInteger.One;

    foreach (char c in hash)
    {
        int digitValue = Convert.ToInt32(c.ToString(), 16);
        accumulator *= (digitValue + 1);
    }

    return accumulator;
}


var sortedHashes = hashes.OrderBy(CalculateKey).ToList();
string joinedHashes = string.Concat(sortedHashes);

Console.WriteLine($"Length before email: {joinedHashes.Length}"); 

string email = "shishir.mosharrof@gmail.com".ToLower();
string finalPayload = joinedHashes + email;

Console.WriteLine($"Length after email:  {finalPayload.Length}"); 
Console.WriteLine($"Difference:          {finalPayload.Length - joinedHashes.Length} (Email length: {email.Length})");


byte[] finalBytes = Encoding.UTF8.GetBytes(finalPayload);
byte[] finalHashBytes = SHA3_256.HashData(finalBytes);
string finalHash = Convert.ToHexString(finalHashBytes).ToLower();

Console.WriteLine($"Final SHA3-256 Hash: {finalHash}");
Console.WriteLine($"Final Hash Length:   {finalHash.Length}");