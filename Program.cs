using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Lemmatiseur_Ewe_UI;
using Lemmatiseur_Ewe_UI.Interfaces;
using Lemmatiseur_Ewe_UI.Models;
using Lemmatiseur_Ewe_UI.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// HttpClient pour les fichiers locaux (wwwroot)
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Repository SQLite (persistance dans le navigateur — NON persistante)
// builder.Services.AddSingleton<DbConnectionFactory>(_ =>
//     new DbConnectionFactory("Data Source=lemmatiseur.db"));
// builder.Services.AddSingleton<GenericDataRepository<CorpusData>>();
// builder.Services.AddSingleton<GenericDataRepository<ZipfItem>>();
// builder.Services.AddSingleton<GenericDataRepository<NGramModel>>();

// Repository IndexedDB (persistance dans le navigateur — persistante)
builder.Services.AddScoped<IGenericDataRepository<CorpusData>, IndexedDbRepository<CorpusData>>();
builder.Services.AddScoped<IGenericDataRepository<ZipfItem>, IndexedDbRepository<ZipfItem>>();
builder.Services.AddScoped<IGenericDataRepository<NGramModel>, IndexedDbRepository<NGramModel>>();

builder.Services.AddScoped<ICorpusCleaner, CorpusCleaner>();

await builder.Build().RunAsync();