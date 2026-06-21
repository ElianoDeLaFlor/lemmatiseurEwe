# Lemmatiseur Ewe — UI

Interface web (Blazor WebAssembly) pour le nettoyage et l'analyse linguistique de corpus en langue **éwé** (Afrique de l'Ouest).

## Fonctionnalités

- Nettoyage de corpus texte (saisie directe ou fichier CSV)
- Analyse statistique :
  - Segmentation en tokens (mots)
  - Distribution de Zipf
  - Extraction de bigrammes (N-grammes)
- Comptage des mots et des mots uniques
- Persistance des analyses via IndexedDB (navigateur)
- Détection automatique de la langue éwé

## Utilisation

1. Saisissez ou collez un texte en éwé dans la zone de texte, **ou** importez un fichier CSV (colonne `Index,Ewe,English`)
2. Cliquez sur **Analyser**
3. Consultez les résultats : nombre de mots, distribution de Zipf, bigrammes

## Technologies

- [Blazor WebAssembly](https://learn.microsoft.com/aspnet/core/blazor/) (.NET 10)
- IndexedDB (stockage navigateur)
- CsvHelper, Dapper, SQLite (via SQLitePCLRaw)

## Licence

Copyright (C) 2026 Eliano

Ce programme est un logiciel libre ; vous pouvez le redistribuer et/ou le modifier selon les termes de la **GNU General Public License** telle que publiée par la Free Software Foundation, soit la version 3 de la Licence, soit (à votre convenance) toute version ultérieure.

Ce programme est distribué dans l'espoir qu'il sera utile, mais **SANS AUCUNE GARANTIE**, sans même la garantie implicite de COMMERCIALISATION ou D'ADAPTATION À UN USAGE PARTICULIER. Voir la GNU General Public License pour plus de détails.

Vous devriez avoir reçu une copie de la GNU General Public License avec ce programme. Sinon, consultez <https://www.gnu.org/licenses/>.