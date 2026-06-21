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

public interface ICorpusCleaner
{
    Task<AnalyseResultat?> CleanCorpusFromStringAsync(string textData, string outputFileName);
    Task<AnalyseResultat?> CleanCorpusFromStreamAsync(Stream fileStream, string outputFileName);
}