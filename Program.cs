// Copyright (C) 2026 Eliano
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

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