using Lemmatiseur_Ewe_UI.Models;

namespace Lemmatiseur_Ewe_UI.Interfaces;

public interface IGenericDataRepository<T> where T: class
{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<bool> InsertAsync(T item);
    Task<bool> InsertAsync(IEnumerable<T> item);
    Task<bool> UpdateAsync(T item);
    Task<bool> UpdateAsync(IEnumerable<T> item);
    Task<bool> DeleteAsync(T item);
    Task<bool> DeleteAsync(IEnumerable<T> item);
}
