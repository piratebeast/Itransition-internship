using MovieShowcase.Components;
using MovieShowcase.Core;
using MovieShowcase.Generation;
using MovieShowcase.Localization;
using MovieShowcase.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton<LocaleProvider>();
builder.Services.AddSingleton<MovieGenerator>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();


//---------test-----------
//// TEMP: Phase 1 verification — delete after confirming
//{
//    var r = new Pcg32(42);
//    Console.WriteLine($"PCG   {r.NextUInt()} {r.NextUInt()} {r.NextUInt()}");
//    Console.WriteLine($"Core  {SeedDerivation.RecordSeed(58933423, 49, SeedField.Core)}");
//    Console.WriteLine($"Likes {SeedDerivation.RecordSeed(58933423, 49, SeedField.Likes)}");
//    Console.WriteLine($"Page  {SeedDerivation.PageSeed(58933423, 3)}");

//    // uniformity check
//    var u = new Pcg32(7);
//    var buckets = new int[10];
//    for (int k = 0; k < 100_000; k++) buckets[u.NextInt(0, 10)]++;
//    Console.WriteLine("Buckets: " + string.Join(", ", buckets));
//}

//using (var scope = app.Services.CreateScope())
//{
//    var lp = scope.ServiceProvider.GetRequiredService<LocaleProvider>();
//    var d = lp.Get("en-US");
//    Console.WriteLine($"Loaded {d.DisplayName}: {d.Genres.Count} genres, {d.TitlePatterns.Count} patterns");
//}

using (var scope = app.Services.CreateScope())
{
    var gen = scope.ServiceProvider.GetRequiredService<MovieGenerator>();

    var page = gen.GeneratePage(new GenerationParams(58933423, "en-US", 3.0, 2.0, 0, 10));
    foreach (var m in page)
        Console.WriteLine($"{m.Index,3}  {m.Genre,-9} {m.Title,-28} {m.Year}  ♥{m.Likes}");

    // --- verification ---
    var sample = gen.GeneratePage(new GenerationParams(58933423, "en-US", 3.7, 2.0, 0, 1000));
    Console.WriteLine($"avg likes (want 3.700) = {sample.Average(m => m.Likes):F3}");

    var half = gen.GeneratePage(new GenerationParams(58933423, "en-US", 0.5, 2.0, 0, 1000));
    Console.WriteLine($"avg likes (want 0.500) = {half.Average(m => m.Likes):F3}");

    var f = new Bogus.Faker(gen is null ? "en" : "en_US");
    Console.WriteLine($"bogus en_US sample: {f.Name.FullName()}, {f.Name.FullName()}");

    var a = gen.GeneratePage(new GenerationParams(58933423, "en-US", 2.0, 2.0, 0, 5));
    var b = gen.GeneratePage(new GenerationParams(58933423, "en-US", 9.0, 2.0, 0, 5));

    bool titlesStable = a.Zip(b).All(x => x.First.Title == x.Second.Title
                                       && x.First.Director == x.Second.Director);
    Console.WriteLine($"titles unchanged when likes change: {titlesStable}");
    Console.WriteLine($"likes actually differ: {a[0].Likes} vs {b[0].Likes}");
}


//----test------

app.Run();
