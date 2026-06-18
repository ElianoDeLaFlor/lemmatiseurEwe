namespace Lemmatiseur_Ewe_UI.Services;

public class DbConnectionFactory(string connectionString)
{
    public string ConnectionString { get; } = connectionString;
}