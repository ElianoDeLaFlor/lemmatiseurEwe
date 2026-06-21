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
