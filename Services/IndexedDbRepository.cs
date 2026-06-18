using System.Reflection;
using System.Text.Json;
using Microsoft.JSInterop;
using Lemmatiseur_Ewe_UI.Interfaces;

namespace Lemmatiseur_Ewe_UI.Services;

public class IndexedDbRepository<T> : IGenericDataRepository<T> where T : class
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IJSRuntime _js;
    private readonly string _storeName;

    public IndexedDbRepository(IJSRuntime js)
    {
        _js = js;
        _storeName = typeof(T).Name;
    }

    public async Task<T?> GetByIdAsync(int id)
    {
        var json = await _js.InvokeAsync<string?>("IndexedDbInterop.getById", _storeName, id);
        if (json is null) return null;
        return JsonSerializer.Deserialize<T>(json, JsonOptions);
    }

    public async Task<IEnumerable<T>> GetAllAsync()
    {
        var results = await _js.InvokeAsync<string[]>("IndexedDbInterop.getAll", _storeName);
        return results.Select(r => JsonSerializer.Deserialize<T>(r, JsonOptions)!)
                      .Where(x => x is not null)
                      .ToList();
    }

    public async Task<bool> InsertAsync(T item)
    {
        return await InsertAsync([item]);
    }

    public async Task<bool> InsertAsync(IEnumerable<T> items)
    {
        // Sérialise les objets directement (propriétés = colonnes du store)
        var json = JsonSerializer.Serialize(items, JsonOptions);
        return await _js.InvokeAsync<bool>("IndexedDbInterop.add", _storeName, json);
    }

    public async Task<bool> UpdateAsync(T item)
    {
        var json = JsonSerializer.Serialize(item, JsonOptions);
        return await _js.InvokeAsync<bool>("IndexedDbInterop.update", _storeName, json);
    }

    public async Task<bool> UpdateAsync(IEnumerable<T> items)
    {
        await ClearAsync();
        return await InsertAsync(items);
    }

    public async Task<bool> DeleteAsync(T item)
    {
        // Cherche la propriété Id pour supprimer par clé IndexedDB
        var idProp = typeof(T).GetProperty("Id", BindingFlags.Public | BindingFlags.Instance);
        if (idProp?.GetValue(item) is int id && id > 0)
        {
            return await _js.InvokeAsync<bool>("IndexedDbInterop.delete", _storeName, id);
        }
        // Sans Id, on efface tout le store
        await ClearAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(IEnumerable<T> items)
    {
        var idProp = typeof(T).GetProperty("Id", BindingFlags.Public | BindingFlags.Instance);
        var hasId = idProp?.PropertyType == typeof(int);

        if (!hasId)
        {
            await ClearAsync();
            return true;
        }

        var deleted = false;
        foreach (var item in items)
        {
            if (idProp!.GetValue(item) is int id && id > 0)
            {
                deleted |= await _js.InvokeAsync<bool>("IndexedDbInterop.delete", _storeName, id);
            }
        }
        return deleted;
    }

    public async Task ClearAsync()
    {
        await _js.InvokeAsync<bool>("IndexedDbInterop.clear", _storeName);
    }
}