using CPORLib.Tools;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CPORLib.Tools.Options;

namespace RunCPOR
{
    public class ExcelHelper
    {

        private static void ApplyBoldOutline(ExcelWorksheet worksheet, int startRow, int startCol, int endRow, int endCol)
        {
            // Apply thick border only to the outer edges of the range
            worksheet.Cells[startRow, startCol, endRow, startCol].Style.Border.Left.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thick;  // Left border
            worksheet.Cells[startRow, endCol, endRow, endCol].Style.Border.Right.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thick; // Right border
            worksheet.Cells[startRow, startCol, startRow, endCol].Style.Border.Top.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thick;   // Top border
            worksheet.Cells[endRow, startCol, endRow, endCol].Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thick;  // Bottom border
        }

        private static void FormatTable(ExcelWorksheet worksheet, string problemName, int count, int top, List<Tuple<List<ExecutionData>, TimeSpan, InaccuracyHandlingStrategies, string>> EDList, int overlap)
        {
            int numOfStats = 9;
            worksheet.Cells[top + 1, 1, top + numOfStats, 1].Merge = true;
            worksheet.Cells[top + 1, 1, top + numOfStats, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
            worksheet.Cells[top + 1, 1, top + numOfStats, 1].Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
            ApplyBoldOutline(worksheet, top + 1, 1, top + numOfStats, 1);
            worksheet.Cells[top + 1, 1].Value = problemName;

            worksheet.Cells[top + 1, 2, top + 2, 2].Merge = true;
            worksheet.Cells[top + 1, 2, top + 2, 2].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
            worksheet.Cells[top + 1, 2, top + 2, 2].Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
            ApplyBoldOutline(worksheet, top + 1, 2, top + 2, 2);
            worksheet.Cells[top + 1, 2].Value = $"{count} Iter";

            worksheet.Cells[top + 1, 3, top + 1, 6].Merge = true;
            worksheet.Cells[top + 1, 3, top + 1, 6].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
            worksheet.Cells[top + 1, 3, top + 1, 6].Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
            ApplyBoldOutline(worksheet, top + 1, 3, top + 2, 6);
            ApplyBoldOutline(worksheet, top + 1, 3, top + numOfStats, 6);
            worksheet.Cells[top + 1, 3].Value = "No Fake";

            worksheet.Cells[top + 1, 7, top + 1, 10].Merge = true;
            worksheet.Cells[top + 1, 7, top + 1, 10].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
            worksheet.Cells[top + 1, 7, top + 1, 101].Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
            ApplyBoldOutline(worksheet, top + 1, 7, top + 2, 10);
            ApplyBoldOutline(worksheet, top + 1, 7, top + numOfStats, 10);
            worksheet.Cells[top + 1, 7].Value = "Fake Threshold = 0";

            worksheet.Cells[top + 1, 11, top + 1, 14].Merge = true;
            worksheet.Cells[top + 1, 11, top + 1, 14].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
            worksheet.Cells[top + 1, 11, top + 1, 14].Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
            ApplyBoldOutline(worksheet, top + 1, 11, top + 2, 14);
            ApplyBoldOutline(worksheet, top + 1, 11, top + numOfStats, 14);
            worksheet.Cells[top + 1, 11].Value = "Fake Threshold = 0.2";

            worksheet.Cells[top + 1, 15, top + 1, 18].Merge = true;
            worksheet.Cells[top + 1, 15, top + 1, 18].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
            worksheet.Cells[top + 1, 15, top + 1, 18].Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
            ApplyBoldOutline(worksheet, top + 1, 15, top + 2, 18);
            ApplyBoldOutline(worksheet, top + 1, 15, top + numOfStats, 18);
            worksheet.Cells[top + 1, 15].Value = "Fake Threshold = 0.5";

            worksheet.Cells[top + 1, 19, top + 1, 22].Merge = true;
            worksheet.Cells[top + 1, 19, top + 1, 22].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
            worksheet.Cells[top + 1, 19, top + 1, 22].Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
            ApplyBoldOutline(worksheet, top + 1, 19, top + 2, 22);
            ApplyBoldOutline(worksheet, top + 1, 19, top + numOfStats, 22);
            worksheet.Cells[top + 1, 19].Value = "Fake Threshold = 0.8";

            Dictionary<int, string> costs = new Dictionary<int, string> { { 0, EDList[0].Item3.ToString() }, { 1, EDList[1].Item3.ToString() },
                                                                          { 2, EDList.Count>2 ? EDList[2].Item3.ToString() : "NA"}, { 3, EDList.Count>3 ? EDList[3].Item3.ToString() : "NA"} };
            if (Options.OverspecifiedPreconditions)
                costs = new Dictionary<int, string> { { 0, "No Costs" }, { 1, "Cost=1" }, { 2, "Cost=5" }, { 3, "Cost=20"} };

            for (int i = 0; i < 20; i++)
            {
                worksheet.Cells[top + 2, i + 3].Value = costs[i % 4];
            }

            int firstStat = 3;
            worksheet.Cells[top + firstStat++, 2].Value = "Actions";
            worksheet.Cells[top + firstStat++, 2].Value = $"ActionsOverlap={overlap}";
            worksheet.Cells[top + firstStat++, 2].Value = $"Success/{Options.Iterations}";
            worksheet.Cells[top + firstStat++, 2].Value = "Deadends";
            worksheet.Cells[top + firstStat++, 2].Value = "Replanning";
            worksheet.Cells[top + firstStat++, 2].Value = "Failures";
            worksheet.Cells[top + firstStat++, 2].Value = "Time";
            ApplyBoldOutline(worksheet, top + 3, 2, top + numOfStats, 2);
            ApplyBoldOutline(worksheet, top + 1, 1, top + numOfStats, 22);
        }

        static string plusMinus = "±";
        public static void WriteToExcel(string folderPath, string sTestPath, List<Tuple<List<ExecutionData>, TimeSpan, InaccuracyHandlingStrategies, string>> EDList, HashSet<int> seedSet, List<string> settings)
        {
            string[] filePaths = {folderPath + $"/output_summary_{DateTime.Now.ToString("MM_dd_HHmmss")}_{Options.PredicateInaccuracy}_{Options.OverspecifiedPreconditions}.xlsx",
                sTestPath + $"/output_summary_{DateTime.Now.ToString("MM_dd_HHmmss")}_{Options.PredicateInaccuracy}_{Options.OverspecifiedPreconditions}.xlsx" };
            string problemName = EDList[0].Item1[0].Problem.Name;
            int count = Options.Iterations;
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            foreach (string filePath in filePaths)
            {
                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add(EDList[0].Item1[0].Domain.Name);

                    // Merging cells for header
                    int top = 0;
                    FormatTable(worksheet, problemName, count, top, EDList, seedSet.Count);

                    int col = 3;
                    int settingsCounter = 0;
                    for (int i = 0; i < EDList.Count; i++)
                    {
                        int row = 3;
                        Tuple<List<ExecutionData>, TimeSpan, InaccuracyHandlingStrategies, string> entry = EDList[i];
                        while (entry.Item4 != settings[i + settingsCounter])
                        {
                            settingsCounter++;
                            col++;
                        }
                        List<ExecutionData> ED = entry.Item1;
                        TimeSpan time = entry.Item2;

                        // Actions stats
                        var actionsList = ED.Select(e => e.Actions).ToList();
                        double avgActions = actionsList.Average();
                        double stdDevActions = Math.Sqrt(actionsList.Average(t => Math.Pow(t - avgActions, 2)));
                        worksheet.Cells[top + row++, col].Value = $"{avgActions.ToString("F2")} {plusMinus} {stdDevActions.ToString("F2")}";

                        var filteredED = ED.Where(obj => seedSet.Contains(obj.Seed)).ToList();
                        var filteredActionsList = filteredED.Select(e => e.Actions).ToList();
                        double filteredAvgActions = filteredActionsList.Average();
                        double filteredStdDevActions = Math.Sqrt(filteredActionsList.Average(t => Math.Pow(t - filteredAvgActions, 2)));

                        worksheet.Cells[top + row++, col].Value = $"{filteredAvgActions.ToString("F2")} {plusMinus} {filteredStdDevActions.ToString("F2")}";
                        worksheet.Cells[top + row++, col].Value = ((double) ED.Count/count).ToString("F2");
                        worksheet.Cells[top + row++, col].Value = ED.Average(obj => obj.stepsToReplan.Count);
                        worksheet.Cells[top + row++, col].Value = ED.Average(obj => obj.ReplanningCount);

                        // Failures
                        var failuresList = ED.Select(e => e.FailCount).ToList();
                        double avgFail = failuresList.Average();
                        double stdDevFail = Math.Sqrt(failuresList.Average(t => Math.Pow(t - avgFail, 2)));
                        worksheet.Cells[top + row++, col].Value = $"{avgFail.ToString("F2")} {plusMinus} {stdDevFail.ToString("F2")}";

                        // Time stats (convert to total seconds)
                        var timeList = ED.Select(e => e.Time.TotalSeconds).ToList();
                        double avgTime = timeList.Average();
                        double stdDevTime = Math.Sqrt(timeList.Average(t => Math.Pow(t - avgTime, 2)));
                        worksheet.Cells[top + row++, col].Value = $"{avgTime.ToString("F2")} {plusMinus} {stdDevTime.ToString("F2")}";
                        col++;
                    }

                    // Save the file
                    FileInfo fileInfo = new FileInfo(filePath);
                    package.SaveAs(fileInfo);
                }
                Console.WriteLine($"Outputs saved to {filePath}");
            }
        }


        public static void writeSummary(string folderPath, List<ExecutionData> ED, TimeSpan time, string setting)
        {
            string filePath = folderPath + $"/output_summary_{DateTime.Now.ToString("MM_dd_HHmmss")}.txt";
            
            double average = ED.Average(obj => obj.Actions);
            double sumOfSquaredDifferences = ED.Sum(x => Math.Pow(x.Actions - average, 2));
            double variance = sumOfSquaredDifferences / ED.Count;
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                writer.WriteLine($"Domain/Problem: {ED[0].Domain.Name}/{ED[0].Problem.Name}\n");
                writer.WriteLine($"Settings: {setting}\n");
                writer.WriteLine($"Number of Successful iterations: {ED.Count}");
                writer.WriteLine($"False Positives/Negatives: {Options.PredicateInaccuracy}");
                writer.WriteLine($"Mode: {Options.InaccuracyHandlingStrategy}");
                writer.WriteLine($"Average Actions: {ED.Average(obj => obj.Actions)}");
                writer.WriteLine($"\t std Actions: {Math.Sqrt(variance)}");
                writer.WriteLine($"\t Sensing Actions: {ED.Average(obj => obj.SensingActions)}");
                writer.WriteLine($"\t Movement Actions: {ED.Average(obj => obj.EffectActions)}");
                //writer.WriteLine($"\t Average MakeActions: {ED.Average(obj => obj.MakeActions.Sum())}");
                //writer.WriteLine($"\t Count MakeActions: {ED.Average(obj => obj.MakeActions.Count(x=>x>0))}");
                writer.WriteLine($"Average Planning count: {ED.Average(obj => obj.stepsToReplan.Count)}");
                //writer.WriteLine($"Average Number of Negations: {ED.Average(obj => obj.NumberOfNegations)}");
                writer.WriteLine($"Average Number of Replannings: {ED.Average(obj => obj.ReplanningCount)}");
                writer.WriteLine($"Average FailCount: {ED.Average(obj => obj.FailCount)}");
                writer.WriteLine($"Average Time: {time.TotalMinutes:00}:{time.Seconds:00}.{time.Milliseconds:000}");

                writer.WriteLine("\n\n");
                for (int i = 0; i < ED.Count; i++)
                {
                    ExecutionData ed = ED[i];
                    writer.WriteLine($"{i}:");
                    writer.WriteLine($"Time: {ed.Time.TotalMinutes:00}:{ed.Time.Seconds:00}.{ed.Time.Milliseconds:000}");
                    writer.WriteLine($"Number of Negations: {ed.NumberOfNegations}");
                    writer.WriteLine($"Number of actions: {ed.Actions}");
                    writer.WriteLine($"\t Sensing Actions: {ed.SensingActions}");
                    writer.WriteLine($"\t Movement Actions: {ed.EffectActions}");
                    writer.WriteLine($"\t Make Actions: {string.Join(", ", ed.MakeActions)}");
                    writer.WriteLine($"Planning count: {ed.stepsToReplan.Count}");
                    writer.WriteLine($"Replanning: {ed.ReplanningCount}");
                    writer.WriteLine($"Replanning w/ Mod: {ed.ReplanningIncludeModCount}");
                    writer.WriteLine($"FailCount: {ed.FailCount}");
                    writer.WriteLine("\n\n");
                }
            }
            Console.WriteLine($"Outputs saved to {filePath} - settings: {setting}");
            
        }


        public static string CreateOutputFolder(string path)
        {
            string folderPath = path + $"/outputs_{DateTime.Now.ToString("MM_dd")}";
            if (!Directory.Exists(folderPath))
            {
                // Create the directory if it doesn't exist
                Directory.CreateDirectory(folderPath);
                Console.WriteLine("Folder created.");
            }
            return folderPath;
        }
    }
}
