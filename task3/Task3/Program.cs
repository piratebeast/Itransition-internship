using System.Numerics;
using System.Text.RegularExpressions;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls($"http://+:{Environment.GetEnvironmentVariable("PORT") ?? "8080"}");

var app = builder.Build();

app.MapGet("/shishir_mosharrof_gmail_com",
    (string? x, string? y) => Results.Text(Lcm.Compute(x, y), "text/plain"));


/* Uncomment the following lines to test the Lcm.Compute method in a console application
Console.WriteLine(Lcm.Compute("4", "6"));      // expect 12
Console.WriteLine(Lcm.Compute("3.5", "2"));    // expect NaN
Console.WriteLine(Lcm.Compute("-4", "6"));     // expect NaN
Console.WriteLine(Lcm.Compute("abc", "6"));    // expect NaN
Console.WriteLine(Lcm.Compute(null, "6"));     // expect NaN    
Console.WriteLine(Lcm.Compute("", "6"));       // expect NaN
Console.WriteLine(Lcm.Compute("0", "5"));      // your call — but don't crash
Console.WriteLine(Lcm.Compute("2000000000", "1999999999")); // must not overflow
*/

app.Run();


static class Lcm
{
    public static string Compute(string? x, string? y)
    {
        try
        {
            if ((x is not null && x.Length > 1000) || (y is not null && y.Length > 1000))
            {
                return "NaN";
            }

            //Guard against null, empty strings, and non-digit characters (negatives, floats, letters)
            if (x is null || y is null || !Regex.IsMatch(x, @"^[0-9]+$") || !Regex.IsMatch(y, @"^[0-9]+$"))
            {
                return "NaN";
            }

            BigInteger a = BigInteger.Parse(x);
            BigInteger b = BigInteger.Parse(y);

            if (a == 0 || b == 0)
            {
                return "0";
            }

            BigInteger gcd = BigInteger.GreatestCommonDivisor(a, b);
            BigInteger lcm = (a / gcd) * b; // Use this formula to avoid overflow

            return lcm.ToString();
        }
        catch (Exception)
        {
            return "NaN";
        }

        
    }
}