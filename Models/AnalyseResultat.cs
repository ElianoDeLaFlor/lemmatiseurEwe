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