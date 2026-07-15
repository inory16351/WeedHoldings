using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Xml.Linq;
using UnityEngine;

namespace WeedHoldings
{
    public static class ExcelParser
    {
        public static List<string[]> ParseSheet(string xlsxPath, int sheetIndex = 0)
        {
            if (!File.Exists(xlsxPath))
            {
                Debug.LogError($"Excel file not found: {xlsxPath}");
                return null;
            }

            try
            {
                using (var archive = ZipFile.OpenRead(xlsxPath))
                {
                    var sharedStrings = ReadSharedStrings(archive);
                    return ReadSheet(archive, sheetIndex, sharedStrings);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to parse Excel: {e.Message}");
                return null;
            }
        }

        public static List<string[]> ParseSheetByName(string xlsxPath, string sheetName)
        {
            if (!File.Exists(xlsxPath))
            {
                Debug.LogError($"Excel file not found: {xlsxPath}");
                return null;
            }

            try
            {
                using (var archive = ZipFile.OpenRead(xlsxPath))
                {
                    var sharedStrings = ReadSharedStrings(archive);
                    var sheetRelationId = GetSheetRelationId(archive, sheetName);
                    if (sheetRelationId == null) return null;
                    return ReadSheetByRId(archive, sheetRelationId, sharedStrings);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to parse Excel sheet '{sheetName}': {e.Message}");
                return null;
            }
        }

        static List<string> ReadSharedStrings(ZipArchive archive)
        {
            var result = new List<string>();
            var entry = archive.GetEntry("xl/sharedStrings.xml");
            if (entry == null) return result;

            var doc = XDocument.Load(entry.Open());
            var ns = doc.Root.Name.Namespace;

            foreach (var si in doc.Root.Elements(ns + "si"))
            {
                var textElem = si.Element(ns + "t");
                if (textElem != null)
                {
                    result.Add(textElem.Value);
                }
                else
                {
                    var richText = string.Concat(si.Elements(ns + "r").Elements(ns + "t"));
                    result.Add(richText);
                }
            }
            return result;
        }

        static string GetSheetRelationId(ZipArchive archive, string sheetName)
        {
            var entry = archive.GetEntry("xl/workbook.xml");
            if (entry == null) return null;

            var doc = XDocument.Load(entry.Open());
            var ns = doc.Root.Name.Namespace;

            foreach (var sheet in doc.Root.Descendants(ns + "sheet"))
            {
                var nameAttr = sheet.Attribute("name");
                if (nameAttr != null && nameAttr.Value == sheetName)
                {
                    return sheet.Attribute(XName.Get("id", "http://schemas.openxmlformats.org/officeDocument/2006/relationships"))?.Value;
                }
            }
            return null;
        }

        static List<string[]> ReadSheet(ZipArchive archive, int sheetIndex, List<string> sharedStrings)
        {
            var sheetPath = $"xl/worksheets/sheet{sheetIndex + 1}.xml";
            var entry = archive.GetEntry(sheetPath);
            if (entry == null) return null;

            return ParseSheetXml(entry, sharedStrings);
        }

        static List<string[]> ReadSheetByRId(ZipArchive archive, string relationId, List<string> sharedStrings)
        {
            var relEntry = archive.GetEntry("xl/_rels/workbook.xml.rels");
            if (relEntry == null) return null;

            var relDoc = XDocument.Load(relEntry.Open());
            var relNs = XNamespace.Get("http://schemas.openxmlformats.org/package/2006/relationships");

            string target = null;
            foreach (var rel in relDoc.Root.Elements(relNs + "Relationship"))
            {
                var idAttr = rel.Attribute("Id");
                if (idAttr != null && idAttr.Value == relationId)
                {
                    target = rel.Attribute("Target")?.Value;
                    break;
                }
            }

            if (target == null) return null;

            // Target은 보통 "worksheets/sheet3.xml"처럼 xl/ 기준 상대경로이지만, openpyxl로 재저장된
            // 파일은 "/xl/worksheets/sheet4.xml"처럼 선행 슬래시가 붙은 절대경로 형태로 바뀐다.
            // 무조건 "xl/"를 붙이면 후자의 경우 "xl//xl/worksheets/..."가 되어 엔트리를 못 찾으므로 정규화한다.
            string normalizedTarget = target.TrimStart('/');
            if (!normalizedTarget.StartsWith("xl/", StringComparison.OrdinalIgnoreCase))
                normalizedTarget = "xl/" + normalizedTarget;

            var sheetEntry = archive.GetEntry(normalizedTarget);
            if (sheetEntry == null) return null;

            return ParseSheetXml(sheetEntry, sharedStrings);
        }

        static List<string[]> ParseSheetXml(ZipArchiveEntry entry, List<string> sharedStrings)
        {
            var result = new List<string[]>();
            var doc = XDocument.Load(entry.Open());
            var ns = doc.Root.Name.Namespace;

            foreach (var row in doc.Root.Descendants(ns + "row"))
            {
                // 완전히 빈 셀은 XML에 <c> 자체가 아예 없는 경우가 많다. 그냥 순서대로 이어붙이면
                // 중간에 빈 셀이 있는 행에서 그 뒤 컬럼이 전부 한 칸씩 밀려버리므로,
                // 반드시 r="I4" 같은 셀 참조에서 실제 열 위치를 읽어 그 자리에 값을 넣어야 한다.
                var cellValues = new Dictionary<int, string>();
                int maxCol = -1;
                int fallbackIndex = 0;

                foreach (var cell in row.Elements(ns + "c"))
                {
                    var refAttr = cell.Attribute("r");
                    int colIndex = refAttr != null ? ColumnLetterToIndex(refAttr.Value) : fallbackIndex;
                    fallbackIndex = colIndex + 1;

                    var valueElem = cell.Element(ns + "v");
                    var typeAttr = cell.Attribute("t");
                    string value = null;
                    if (typeAttr != null && typeAttr.Value == "inlineStr")
                    {
                        // 공유 문자열 테이블 없이 <c t="inlineStr"><is><t>...</t></is></c> 형태로 직접 담긴 문자열.
                        // (openpyxl로 재저장한 파일은 sharedStrings.xml 없이 이 형태를 쓴다)
                        var isElem = cell.Element(ns + "is");
                        value = isElem?.Element(ns + "t")?.Value ?? "";
                    }
                    else if (valueElem != null)
                    {
                        if (typeAttr != null && typeAttr.Value == "s")
                        {
                            int si = int.Parse(valueElem.Value);
                            value = si >= 0 && si < sharedStrings.Count ? sharedStrings[si] : "";
                        }
                        else
                        {
                            value = valueElem.Value;
                        }
                    }

                    cellValues[colIndex] = value;
                    if (colIndex > maxCol) maxCol = colIndex;
                }

                var cells = new string[maxCol + 1];
                foreach (var kvp in cellValues)
                    cells[kvp.Key] = kvp.Value;

                result.Add(cells);
            }
            return result;
        }

        /// <summary>셀 참조("I4", "AA10" 등)의 알파벳 부분을 0-based 열 인덱스로 변환한다.</summary>
        static int ColumnLetterToIndex(string cellRef)
        {
            int col = 0;
            foreach (char c in cellRef)
            {
                if (!char.IsLetter(c)) break;
                col = col * 26 + (char.ToUpperInvariant(c) - 'A' + 1);
            }
            return col - 1;
        }
    }
}
