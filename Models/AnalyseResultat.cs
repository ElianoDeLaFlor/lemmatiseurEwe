namespace Lemmatiseur_Ewe_UI.Models;

public class AnalyseResultat
{
    public int Id { get; set; }
    public IEnumerable<CorpusData> CorpusData { get; set; } = [];
    public IEnumerable<ZipfItem> ZipfData { get; set; } = [];
    public IEnumerable<NGramModel> NGramModels { get; set; } = [];
    public string TexteNettoye { get; set; } = string.Empty;
    public DateTime DateAnalyse { get; set; }
    public int TotalTokens { get; set; }
}