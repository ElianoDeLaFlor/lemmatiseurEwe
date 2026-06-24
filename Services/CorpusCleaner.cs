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

using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using CsvHelper;
using CsvHelper.Configuration;
using Lemmatiseur_Ewe_UI.Interfaces;
using Lemmatiseur_Ewe_UI.Models;

namespace Lemmatiseur_Ewe_UI.Services;

public class CorpusCleaner : ICorpusCleaner
{
    private static readonly Regex WhitespaceRegex = new Regex(@"\s+", RegexOptions.Compiled);
    private readonly HttpClient _http;
    private readonly IGenericDataRepository<CorpusData> _corpusDateRepository;
    private readonly IGenericDataRepository<ZipfItem> _ZipfItemDateRepository;
    private readonly IGenericDataRepository<NGramModel> _NgramRepository;
    
    // Expression régulière optimisée pour l'Ewe (inclut l'alphabet étendu et les élisions potentielles)
    private static readonly Regex EweTokenRegex = new Regex(
        @"[a-zA-ZɖɛƒɣɔʋŋŊÁáÉéÍíÓóÚúÀàÈèÌìÒòÙùäëïöüÄËÏÖÜṽ]+(?:['’ˈ][a-zA-ZɖɛƒɣɔʋŋŊ]+)?", 
        RegexOptions.Compiled
    );

    public CorpusCleaner(HttpClient http,IGenericDataRepository<CorpusData> corpusDateRepository,IGenericDataRepository<ZipfItem> zipfItemDateRepository,IGenericDataRepository<NGramModel> NgramRepository)
    {
        _http = http;
        _corpusDateRepository = corpusDateRepository;
        _ZipfItemDateRepository = zipfItemDateRepository;
        _NgramRepository = NgramRepository;
    }

    public async Task<AnalyseResultat?> CleanCorpusFromStringAsync(string textData, string outputFileName)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.WriteLine("=== Début du nettoyage du corpus Ewe (via HTTP) ===");
        AnalyseResultat analyseResult = new AnalyseResultat();

        try
        {
            // var response = await _http.GetAsync(fileUrl);
            // response.EnsureSuccessStatusCode();
            //
            // using var stream = await response.Content.ReadAsStreamAsync();
            var result= await NettoyerEtSauvegarderAsync(textData, outputFileName);
            if (!string.IsNullOrWhiteSpace(result.TexteNettoye))
            {
                var resultat= await AnalyseCorpus(result.TexteNettoye);
                resultat.CorpusData = result.CorpusData;
                resultat.TexteNettoye = result.TexteNettoye;


                return resultat;
            }
            else
            {
                return null;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Erreur] {ex}");
            return null;
        }
    }

    public async Task<AnalyseResultat?> CleanCorpusFromStreamAsync(Stream fileStream, string outputFileName)
    {
        Debug.WriteLine("=== Début du nettoyage du corpus Ewe (via upload) ===");

        try
        {
            // BrowserFileStream ne supporte pas les lectures synchrones (StreamReader)
            using var memoryStream = new MemoryStream();
            await fileStream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;
            var result= await NettoyerEtSauvegarderAsync(memoryStream, outputFileName);
            
            if (!string.IsNullOrWhiteSpace(result.TexteNettoye))
            {
               var resultat= await AnalyseCorpus(result.TexteNettoye);
               resultat.CorpusData = result.CorpusData;
               resultat.TexteNettoye=result.TexteNettoye;
               resultat.DateAnalyse=DateTime.Now;

               return resultat;
            }
            else
            {
                return null;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Erreur] {ex}");
            return null;
        }
    }

    private async Task<AnalyseResultat> NettoyerEtSauvegarderAsync(string rawText, string outputFileName)
    {
        // 1. Nettoyage du texte brut ligne par ligne
        var cleanCorpusBuilder = new StringBuilder();
        var corpusDataList = new List<CorpusData>();
        int index = 0;

        using (var reader = new StringReader(rawText))
        {
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                string trimmedText = line.Trim().Trim('"');
                string linearText = WhitespaceRegex.Replace(trimmedText, " ");
                string normalizedText = linearText.Normalize(NormalizationForm.FormC);

                if (!string.IsNullOrEmpty(normalizedText))
                {
                    cleanCorpusBuilder.AppendLine(normalizedText);
                    corpusDataList.Add(new CorpusData
                    {
                        Index = index++,
                        Ewe = normalizedText,
                        English = string.Empty
                    });
                }
            }
        }

        string texteNettoye = cleanCorpusBuilder.ToString();

        // 2. Sauvegarde dans le fichier
        Debug.WriteLine("\nSauvegarde dans {0}", outputFileName);
        await File.WriteAllTextAsync(outputFileName, texteNettoye, new UTF8Encoding(false));
        Debug.WriteLine("[Succès] Écriture dans le fichier '{0}'", outputFileName);
        
        var analyseResult = new AnalyseResultat
        {
            CorpusData = corpusDataList,
            TexteNettoye = texteNettoye
        };

        // 4. Statistiques
        var mots = texteNettoye.Split([' ', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries);
        int nombreMots = mots.Length;
        int nombreMotsUniques = mots.Distinct(StringComparer.OrdinalIgnoreCase).Count();
        Debug.WriteLine("[Stats] {0} mots, {1} mots uniques", nombreMots, nombreMotsUniques);

        return analyseResult;
    }

    private async Task<AnalyseResultat> NettoyerEtSauvegarderAsync(Stream stream, string outputFileName)
    {
        // 1. Lecture et nettoyage
        var cleanCorpusBuilder = new StringBuilder();
        AnalyseResultat analyseResult = new AnalyseResultat();

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = ",",
            Encoding = Encoding.UTF8,
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.InsideQuotes
        };

        IEnumerable<CorpusData> data;
        using (var reader = new StreamReader(stream))
        using (var csv = new CsvReader(reader, config))
        {
            data = csv.GetRecords<CorpusData>().ToList();
        }

        int dataCount = data.Count();
        int count = 0;

        foreach (var item in data)
        {
            string trimmedText = item.Ewe.Trim().Trim('"');
            string linearText = WhitespaceRegex.Replace(trimmedText, " ");
            string normalizedText = linearText.Normalize(NormalizationForm.FormC);

            if (!string.IsNullOrEmpty(normalizedText))
            {
                cleanCorpusBuilder.AppendLine(normalizedText);
            }
            count++;
            Debug.WriteLine("\n {0} fichier(s) nettoyé(s) sur {1}", count, dataCount);
        }

        string texteNettoye = cleanCorpusBuilder.ToString();

        // 2. Sauvegarde dans le fichier
        Debug.WriteLine("\n {0} fichier(s) écrits dans {1}", dataCount, outputFileName);
        await File.WriteAllTextAsync(outputFileName, texteNettoye, new UTF8Encoding(false));
        Debug.WriteLine("\n[Succès] Écriture dans le fichier '{0}'", outputFileName);
        Debug.WriteLine("Sauvegarde du cleaned corpus dans la base de donne");
        analyseResult.CorpusData = data;
        analyseResult.TexteNettoye = texteNettoye;
        var saved = await _corpusDateRepository.InsertAsync(data);
        
        Debug.WriteLine("Sauvegarde du cleaned corpus dans la base de donne terminee");

        // 3. Statistiques
        var mots = texteNettoye.Split([' ', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries);
        int nombreMots = mots.Length;
        int nombreMotsUniques = mots.Distinct(StringComparer.OrdinalIgnoreCase).Count();

        // 4. Sauvegarde dans la base SQLite
        

        //var saved = await _analyseRepo.InsertAsync(resultat);
        //Debug.WriteLine("[Base de données] Analyse sauvegardée avec l'Id {0}", saved);

        // 5. Aperçu
        Debug.WriteLine("\nAperçu du premier enregistrement nettoyé :");
        string preview = texteNettoye.Split(Environment.NewLine)[0];
        Debug.WriteLine("[Aperçu] {0}", preview);
        Debug.WriteLine("[Stats] {0} mots, {1} mots uniques", nombreMots, nombreMotsUniques);

        return analyseResult;
    }
    


    private async Task<AnalyseResultat> AnalyseCorpus(string cleanedText)
    {
        Debug.WriteLine("=== Phase 2 : Début de l'analyse statistique ===");
        AnalyseResultat analyseResult = new AnalyseResultat();
        // 1. Segmentation Algorithmique (Tokenization)
        List<string> tokens = TokenizeEwe(cleanedText);
        int totalTokens = tokens.Count;
        analyseResult.TotalTokens = totalTokens;
        Debug.WriteLine($"\n[1/4] Segmentation terminée : {totalTokens} tokens (mots) identifiés.");
        // 3. Analyse Statistique : Vérification de la Loi de Zipf
        var zipfDistribution = CalculateZipfDistribution(tokens);
        int totalTypes = zipfDistribution.Count;
        analyseResult.ZipfData = zipfDistribution;
        var saved = await _ZipfItemDateRepository.InsertAsync(zipfDistribution);
        Debug.WriteLine("\nTop 5 des mots-outils (Zipf Validation) :");
        Debug.WriteLine($"[2/4] Analyse lexicale : {totalTypes} types (mots uniques) trouvés.");
        Debug.WriteLine($"      Ratio Type/Token (TTR) : {((double)totalTypes / totalTokens):P2}");
        // Affichage des premiers rangs à la console (Validation)
        Debug.WriteLine("\nTop 5 des mots-outils (Zipf Validation) :");
        foreach (var item in zipfDistribution.Take(5))
        {
            Debug.WriteLine($"   Rang {item.Rang} : '{item.Mot}' -> Fréquence : {item.Frequence} (r*f = {item.Rang * item.Frequence})");
        }
        // 4. Extraction des N-grammes (Bigrammes de mots)
        var bigrams = ExtractWordBigrams(tokens);
        analyseResult.NGramModels = bigrams;
        var savedbigram = await _NgramRepository.InsertAsync(bigrams);
        Debug.WriteLine($"\n[3/4] Extraction des N-grammes : {bigrams.Count} bigrammes uniques générés.");
        
        Debug.WriteLine("=== Phase 2 : Fin de l'analyse statistique ===");
        return analyseResult;
    }
    
    /// <summary>
    /// Segmente le texte brut en tokens en préservant l'alphabet d'Afrique de l'Ouest.
    /// </summary>
    private static List<string> TokenizeEwe(string text)
    {
        var matches = EweTokenRegex.Matches(text);
        return matches.Cast<Match>()
            .Select(m => m.Value.ToLowerInvariant())
            .ToList();
    }
    
    /// <summary>
    /// Calcule le rang et la fréquence de chaque mot selon les postulats de Zipf-Mandelbrot.
    /// </summary>
    public static List<ZipfItem> CalculateZipfDistribution(List<string> tokens)
    {
        int totalTokens = tokens.Count;
        var plusFrequent = tokens.GroupBy(t => t)
            .Select(g => new { Mot = g.Key, Frequence = g.Count() })
            .OrderByDescending(x => x.Frequence)
            .ToList();
    
        double zipfConstante = plusFrequent.First().Frequence; // constante de Zipf : fréquence du rang 1
    
        return plusFrequent.Select((x, index) => 
        {
            int rang = index + 1;
            double frequenceReelle = x.Frequence;
            double zipfTheorique = zipfConstante / rang; // f ∝ 1/rang
        
            return new ZipfItem
            {
                Rang = rang,
                Mot = x.Mot,
                Frequence = (int)frequenceReelle,
                FrequenceRelative = frequenceReelle / totalTokens,
                ZipfTheorique = zipfTheorique,
                EcartPercent = (frequenceReelle - zipfTheorique) / frequenceReelle * 100
            };
        }).ToList();
    }
    
    /// <summary>
    /// Extrait les bigrammes de mots consécutifs.
    /// </summary>
    private static List<NGramModel> ExtractWordBigrams(List<string> tokens)
    {
        var bigrams = new Dictionary<string, int>();
    
        for (int i = 0; i < tokens.Count - 1; i++)
        {
            string bigram = $"{tokens[i]} {tokens[i + 1]}";
            if (bigrams.ContainsKey(bigram))
                bigrams[bigram]++;
            else
                bigrams[bigram] = 1;
        }

        return bigrams
            .OrderByDescending(x => x.Value)
            .Select(x => new NGramModel
            {
                Id = 0, // Laissez la base de données générer l'ID
                Key = x.Key,
                Value = x.Value.ToString()
            })
            .ToList();
    }
}