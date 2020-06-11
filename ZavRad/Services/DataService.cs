using System.Collections.Generic;
using System.IO;
using System.Linq;
using ZavRad.Models;

namespace ZavRad.Services
{
    public class DataService
    {
        public Dictionary<string, List<int>> _histograms;
        public Dictionary<string, List<SamFile>> _readings;
        public List<ReferenceModel> _references;
        public string _referencePath;

        public IEnumerable<ReferenceModel> Init(string alignmentFile, string referenceFile)
        {
            _referencePath = referenceFile;
            _histograms = new Dictionary<string, List<int>>();
            var files = new Dictionary<string, StreamWriter>();
            _references = new List<ReferenceModel>();

            using (var reader = new StreamReader(alignmentFile))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    var data = line.Split('\t');
                    if (line.StartsWith('@'))
                    {
                        if (!line.StartsWith("@SQ"))
                        {
                            continue;
                        }
                        var name = data[1].Substring(3);
                        _references.Add(new ReferenceModel
                        {
                            Length = int.Parse(data[2].Substring(3)),
                            Name = name
                        });

                        files.Add(name, new StreamWriter($"{_references.Count - 1}.tmp", false));
                    }
                    else if (data[1] != "4")
                    {
                        var referenceName = data[2];
                        var writer = files[referenceName];
                        var draw = GetDrawExpressions(data[5]);
                        var realStartingX = ((int.Parse(data[3]) - 1) - (draw.First().Type == DrawType.Line ? draw.First().Length : 0));
                        writer.WriteLine($"{data[0]}\t{data[2]}\t{realStartingX}\t{data[5]}\t{data[1] != "16"}");
                    }
                }
            }

            foreach (var file in files.Values)
            {
                file.Flush();
                file.Close();
            }

            return _references;
        }

        public void SortFiles()
        {
            for (int i = 0; i < _references.Count; i++)
            {
                var lines = File.ReadAllLines($"{i}.tmp");
                File.WriteAllLines($"{i}.tmp", lines.OrderBy(line => int.Parse(line.Split('\t')[2])));
            }
        }

        public void CalculateHist(string referenceName)
        {
            var reference = _references.First(r => r.Name == referenceName);

            var list = new List<int> { 0 };
            _histograms[reference.Name] = Enumerable.Repeat(0, reference.Length).ToList();
            using var reader = new StreamReader($"{_references.IndexOf(reference)}.tmp");
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                var splitLine = line.Split('\t');

                var read = new SamFile
                {
                    QueryName = splitLine[0],
                    ReferenceName = splitLine[1],
                    StartingPos = int.Parse(splitLine[2]),
                    DrawExpressions = GetDrawExpressions(splitLine[3]),
                    Reverse = bool.Parse(splitLine[4])
                };
                var realStartingX = read.StartingPos;
                var index = list.FindIndex(i => realStartingX > i + 1); 
                var lineLength = read.DrawExpressions.Sum(de => de.Length);

                if (index == -1)
                {
                    list.Add(realStartingX + lineLength);
                }
                else
                {
                    list[index] = realStartingX + lineLength;
                }

                var currentX = realStartingX;
                foreach (var draw in read.DrawExpressions)
                {
                    if (draw.Type == DrawType.Rectangle)
                    {
                        for (int i = currentX; i < currentX + draw.Length; i++)
                        {
                            _histograms[reference.Name][i]++;
                        }
                    }
                    currentX += draw.Length;
                }
            }
        }

        public static IEnumerable<DrawExpression> GetDrawExpressions(string cigar)
        {
            var num = 0;
            var list = new List<DrawExpression>();
            for (int i = 0; i < cigar.Length; i++)
            {
                var c = cigar[i];
                if (char.IsDigit(c))
                {
                    num = 10 * num + (c - '0');
                    continue;
                }

                switch (c)
                {
                    case 'M':
                    case '=':
                        if (list.Count > 0 && list.Last().Type == DrawType.Rectangle)
                            list.Last().Length += num;
                        else
                        {
                            list.Add(new DrawExpression
                            {
                                Length = num,
                                Type = DrawType.Rectangle
                            });
                        }
                        break;

                    case 'S':
                    case 'X':
                    case 'N':
                    case 'D':
                        if (list.Count > 0 && list.Last().Type == DrawType.Line)
                            list.Last().Length += num;
                        else
                        {
                            list.Add(new DrawExpression
                            {
                                Length = num,
                                Type = DrawType.Line
                            });
                        }
                        break;

                    default:
                        break;
                }

                num = 0;
            }
            return list;
        }
    }
}