namespace Lemmatiseur_Ewe_UI.Models;

public class ZipfItem
{
    public int Rang { get; set; }
    public string Mot { get; set; }=string.Empty;
    public int Frequence { get; set; }
    public double FrequenceRelative { get; set; }      // fréquence / total tokens
    public double ZipfTheorique { get; set; }          // C / rang
    public double EcartPercent { get; set; } 
}