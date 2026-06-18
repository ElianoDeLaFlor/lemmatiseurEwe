using Lemmatiseur_Ewe_UI.Models;

namespace Lemmatiseur_Ewe_UI.Interfaces;

public interface ICorpusCleaner
{
    Task<AnalyseResultat?> CleanCorpusFromStringAsync(string textData, string outputFileName);
    Task<AnalyseResultat?> CleanCorpusFromStreamAsync(Stream fileStream, string outputFileName);
}