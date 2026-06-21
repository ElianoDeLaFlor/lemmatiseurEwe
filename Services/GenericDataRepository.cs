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

using Microsoft.Data.Sqlite;
using System.Reflection;
using Dapper;
using Lemmatiseur_Ewe_UI.Interfaces;

namespace Lemmatiseur_Ewe_UI.Services;

public class GenericDataRepository<T> : IGenericDataRepository<T> where T : class
{
    private readonly DbConnectionFactory _dbFactory;
    private readonly object _initLock = new();
    private bool _initialized;

    public GenericDataRepository(DbConnectionFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    private string TableName => typeof(T).Name;

    private string PrimaryKeyName
    {
        get
        {
            var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            return props.FirstOrDefault(p =>
                       string.Equals(p.Name, "Id", StringComparison.OrdinalIgnoreCase))?.Name
                   ?? props.FirstOrDefault(p =>
                       string.Equals(p.Name, $"{TableName}Id", StringComparison.OrdinalIgnoreCase))?.Name
                   ?? props[0].Name;
        }
    }

    private PropertyInfo[] DataProperties =>
        typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite)
            .ToArray();

    private string ColumnList =>
        string.Join(", ", DataProperties.Select(p => $"[{p.Name}]"));

    private string ParameterList =>
        string.Join(", ", DataProperties.Select(p => $"@{p.Name}"));

    private string UpdateSetClause =>
        string.Join(", ", DataProperties
            .Where(p => !string.Equals(p.Name, PrimaryKeyName, StringComparison.OrdinalIgnoreCase))
            .Select(p => $"[{p.Name}] = @{p.Name}"));

    private string CreateTableSql =>
        $"CREATE TABLE IF NOT EXISTS [{TableName}] (" +
        string.Join(", ", DataProperties.Select(p =>
        {
            var colType = p.PropertyType switch
            {
                Type t when t == typeof(int) || t == typeof(long) => "INTEGER",
                Type t when t == typeof(double) || t == typeof(float) || t == typeof(decimal) => "REAL",
                _ => "TEXT"
            };
            var isPk = string.Equals(p.Name, PrimaryKeyName, StringComparison.OrdinalIgnoreCase);
            return $"[{p.Name}] {colType}{(isPk ? " PRIMARY KEY" : "")}";
        })) + ")";

    protected SqliteConnection CreateConnection()
    {
        var conn = new SqliteConnection(_dbFactory.ConnectionString);
        conn.Open();
        EnsureCreated(conn);
        return conn;
    }

    private void EnsureCreated(SqliteConnection conn)
    {
        if (_initialized) return;
        lock (_initLock)
        {
            if (_initialized) return;
            conn.Execute(CreateTableSql);
            _initialized = true;
        }
    }

    public async Task<T?> GetByIdAsync(int id)
    {
        using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<T>(
            $"SELECT * FROM [{TableName}] WHERE [{PrimaryKeyName}] = @id",
            new { id });
    }

    public async Task<IEnumerable<T>> GetAllAsync()
    {
        using var conn = CreateConnection();
        return await conn.QueryAsync<T>($"SELECT * FROM [{TableName}]");
    }

    public async Task<bool> InsertAsync(T item)
    {
        using var conn = CreateConnection();
        var rows = await conn.ExecuteAsync(
            $"INSERT INTO [{TableName}] ({ColumnList}) VALUES ({ParameterList})", item);
        return rows > 0;
    }

    public async Task<bool> InsertAsync(IEnumerable<T> items)
    {
        using var conn = CreateConnection();
        using var tx = conn.BeginTransaction();

        try
        {
            var sql = $"INSERT INTO [{TableName}] ({ColumnList}) VALUES ({ParameterList})";
            await conn.ExecuteAsync(sql, items, transaction: tx);
            tx.Commit();
            return true;
        }
        catch
        {
            tx.Rollback();
            return false;
        }
    }

    public async Task<bool> UpdateAsync(T item)
    {
        using var conn = CreateConnection();
        var rows = await conn.ExecuteAsync(
            $"UPDATE [{TableName}] SET {UpdateSetClause} WHERE [{PrimaryKeyName}] = @{PrimaryKeyName}", item);
        return rows > 0;
    }

    public async Task<bool> UpdateAsync(IEnumerable<T> items)
    {
        using var conn = CreateConnection();
        using var tx = conn.BeginTransaction();

        try
        {
            var sql = $"UPDATE [{TableName}] SET {UpdateSetClause} WHERE [{PrimaryKeyName}] = @{PrimaryKeyName}";
            await conn.ExecuteAsync(sql, items, transaction: tx);
            tx.Commit();
            return true;
        }
        catch
        {
            tx.Rollback();
            return false;
        }
    }

    public async Task<bool> DeleteAsync(T item)
    {
        using var conn = CreateConnection();
        var pkProp = typeof(T).GetProperty(PrimaryKeyName, BindingFlags.Public | BindingFlags.Instance)!;
        var pkValue = pkProp.GetValue(item);

        var rows = await conn.ExecuteAsync(
            $"DELETE FROM [{TableName}] WHERE [{PrimaryKeyName}] = @id",
            new { id = pkValue });
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(IEnumerable<T> items)
    {
        using var conn = CreateConnection();
        using var tx = conn.BeginTransaction();

        try
        {
            var pkProp = typeof(T).GetProperty(PrimaryKeyName, BindingFlags.Public | BindingFlags.Instance)!;
            var ids = items.Select(i => new { id = pkProp.GetValue(i) });

            await conn.ExecuteAsync(
                $"DELETE FROM [{TableName}] WHERE [{PrimaryKeyName}] = @id",
                ids, transaction: tx);
            tx.Commit();
            return true;
        }
        catch
        {
            tx.Rollback();
            return false;
        }
    }
}