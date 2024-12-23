using CPORLib.Algorithms;
using CPORLib.FFCS;
using CPORLib.LogicalUtilities;
using CPORLib.Parsing;
using CPORLib.PlanningModel;
using CPORLib.Tools;
using OfficeOpenXml;
using Python.Runtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading;
using System.Timers;
using static CPORLib.Tools.Options;

namespace CPORLib
{
    public class Run
    {
        public static void Main(string[] args)
        {
            //TestAll(true);
            //return;


            if (args.Length < 3)
            {
                Console.WriteLine("Usage: RunPlanner domain_file problem_file output_file [online/offline]");
            }
            else
            {
                string sDomainFile = args[0];
                string sProblemFile = args[1];
                string sOutputFile = args[2];
                string sNegateFile = args[3];
                bool bOnline = false;
                if (args.Length > 4)
                    bOnline = args[3] == "online";
                RunPlanner(sDomainFile
                    , sProblemFile,
                    sNegateFile,
                    sOutputFile,
                    bOnline);
            }
        }


        public static void TestHAdd(Domain d, Problem p)
        {
            int cExecutions = 1000;
            HAddHeuristic h = new HAddHeuristic(d, p);
            BeliefState bs = p.GetInitialBelief();
            Console.WriteLine("Testing " + p.Name);

            Console.WriteLine("Choosing states");

            List<State> states = new List<State>();
            for (int i = 0; i < cExecutions; i++)
            {
                State s = bs.ChooseState(true);
                states.Add(s);
                if (i % 100 == 0)
                    Console.Write("\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b" + i + "/" + cExecutions);

            }

            DateTime dtStart = DateTime.Now;
            Console.WriteLine("\n Computing hadd");

            double dSum = 0.0;

            for(int i = 0; i < cExecutions; i++)
            {
                State s = states[i];
                double cost = h.ComputeHAdd(s);
                dSum += cost;
                //if (i % 100 == 0)
                  //  Console.Write("\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b" + i + "/" + cExecutions);
            }

            DateTime dtEnd = DateTime.Now;
            Console.WriteLine();
            Console.WriteLine("Run " + cExecutions + " in " + (dtEnd - dtStart).TotalMilliseconds + ", avg = " + dSum / cExecutions);

        }

        private static void saveNegations(string sDomainFile, SDRPlanner sdr, int iter)
        {
            string folderPath = Path.GetDirectoryName(sDomainFile);
            string filePath = folderPath +"\\n"+ iter + ".pddl";
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                // Write content to the file
                
                writer.WriteLine(";Run info");
                writer.WriteLine("(and");
                foreach (Predicate p in sdr.PredicatesToNegate)
                    writer.WriteLine(p.ToString());
                writer.WriteLine(")");

                // You can write more content if needed
            }
        }

        private static Dictionary<string, object> saveToDict(List<ExecutionData> ED, TimeSpan time)
        {
            Dictionary<string, object> myDictionary = new Dictionary<string, object>();
            myDictionary["Domain/Problem"] = $"{ED[0].Domain.Name}/{ED[0].Problem.Name}";
            myDictionary["False Positives/Negatives"] = $"{Options.FalsePositive}";
            myDictionary["Mode"] = $"{Options.InaccuracyHandlingStrategy}";
            myDictionary["Total Time"] = $"{time.TotalMinutes:00}:{time.Seconds:00}.{time.Milliseconds:000}";
            myDictionary["Total Actions"] = $"{ED.Sum(obj => obj.Actions)}";
            List<Dictionary<string, string>> list = new List<Dictionary<string, string>>();
            for (int i = 0; i < ED.Count; i++)
            {
                Dictionary<string, string> dict = new Dictionary<string, string>();
                ExecutionData ed = ED[i];
                dict["Time"] = $"{ed.Time.TotalMinutes:00}:{ed.Time.Seconds:00}.{ed.Time.Milliseconds:000}";
                dict["Number of Negations"] = $"{ed.NumberOfNegations}";
                dict["Number of actions"] = $"{ed.Actions}";
                dict["Replanning"] = $"{ed.ReplanningCount}";
                dict["Replanning w/ Mod"] = $"{ed.ReplanningIncludeModCount}";
                dict["FailCount"] = $"{ed.FailCount}";
                list.Add(dict);
            }
            myDictionary["List"] = list;
            return myDictionary;
        }
        private static void printData(List<Tuple<List<ExecutionData>, TimeSpan, InaccuracyHandlingStrategies, string>> EDList)
        {
            //Console.WriteLine($"Domain/Problem: {ED[0].Domain.Name}/{ED[0].Problem.Name}\n");
            for (int j = 0; j < EDList.Count; j++)
            {
                Tuple<List<ExecutionData>, TimeSpan, InaccuracyHandlingStrategies, string> info = EDList[j];
                List<ExecutionData> ED = info.Item1;
                TimeSpan time = info.Item2;
                string setting = info.Item4;
                double average = ED.Average(obj => obj.Actions);
                double sumOfSquaredDifferences = ED.Sum(x => Math.Pow(x.Actions - average, 2));
                double variance = sumOfSquaredDifferences / ED.Count;
                Console.WriteLine($"Strategy: {info.Item3}\n");
                Console.WriteLine($"Number of Successful iterations: {ED.Count}");
                Console.WriteLine($"False Positives/Negatives: {Options.FalsePositive}");
                //Console.WriteLine($"Mode: {Options.InaccuracyHandlingStrategy}");
                Console.WriteLine($"Average Time: {time.TotalMinutes:00}:{time.Seconds:00}.{time.Milliseconds:000}");
                Console.WriteLine($"Average Actions: {ED.Average(obj => obj.Actions)}");
                Console.WriteLine($"\t std Actions: {Math.Sqrt(variance)}");
                Console.WriteLine($"\t Sensing Actions: {ED.Average(obj => obj.SensingActions)}");
                Console.WriteLine($"\t Movement Actions: {ED.Average(obj => obj.EffectActions)}");
                Console.WriteLine($"Average Planning count: {ED.Average(obj => obj.stepsToReplan.Count)}");
                Console.WriteLine($"Average Number of Negations: {ED.Average(obj => obj.NumberOfNegations)}");
                Console.WriteLine($"Average Number of Replannings: {ED.Average(obj => obj.ReplanningCount)}");
                Console.WriteLine($"Average FailCount: {ED.Average(obj => obj.FailCount)}");
                Console.WriteLine("\n\n");
                
            }
        }
        private static void writeSummary(string folderPath, List<ExecutionData> ED, TimeSpan time, string setting)
        {
            string filePath = folderPath + $"/output_summary_{DateTime.Now.ToString("MM_dd_HHmmss")}.txt";
            Console.WriteLine($"Outputs saved to {filePath} - settings: {setting}");
            double average = ED.Average(obj => obj.Actions);
            double sumOfSquaredDifferences = ED.Sum(x => Math.Pow(x.Actions - average, 2));
            double variance = sumOfSquaredDifferences / ED.Count;
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                writer.WriteLine($"Domain/Problem: {ED[0].Domain.Name}/{ED[0].Problem.Name}\n");
                writer.WriteLine($"Settings: {setting}\n");
                writer.WriteLine($"Number of Successful iterations: {ED.Count}");
                writer.WriteLine($"False Positives/Negatives: {Options.FalsePositive}");
                writer.WriteLine($"Mode: {Options.InaccuracyHandlingStrategy}");
                writer.WriteLine($"Average Actions: {ED.Average(obj => obj.Actions)}");
                //writer.WriteLine($"\t std Actions: {Math.Sqrt(variance)}");
                //writer.WriteLine($"\t Sensing Actions: {ED.Average(obj => obj.SensingActions)}");
                //writer.WriteLine($"\t Movement Actions: {ED.Average(obj => obj.EffectActions)}");
                //writer.WriteLine($"\t Average MakeActions: {ED.Average(obj => obj.MakeActions.Sum())}");
                //writer.WriteLine($"\t Count MakeActions: {ED.Average(obj => obj.MakeActions.Count(x=>x>0))}");
                writer.WriteLine($"Average Planning count: {ED.Average(obj => obj.stepsToReplan.Count)}");
                //writer.WriteLine($"Average Number of Negations: {ED.Average(obj => obj.NumberOfNegations)}");
                writer.WriteLine($"Average Number of Replannings: {ED.Average(obj => obj.ReplanningCount)}");
                writer.WriteLine($"Average FailCount: {ED.Average(obj => obj.FailCount)}");
                writer.WriteLine($"Average Time: {time.TotalMinutes:00}:{time.Seconds:00}.{time.Milliseconds:000}");

                writer.WriteLine("\n\n");
                for (int i = 0;i < ED.Count;i++)
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
        }

        static void ApplyBoldOutline(ExcelWorksheet worksheet, int startRow, int startCol, int endRow, int endCol)
        {
            // Apply thick border only to the outer edges of the range
            worksheet.Cells[startRow, startCol, endRow, startCol].Style.Border.Left.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thick;  // Left border
            worksheet.Cells[startRow, endCol, endRow, endCol].Style.Border.Right.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thick; // Right border
            worksheet.Cells[startRow, startCol, startRow, endCol].Style.Border.Top.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thick;   // Top border
            worksheet.Cells[endRow, startCol, endRow, endCol].Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thick;  // Bottom border
        }

        private static void formatTable(ExcelWorksheet worksheet, string problemName, int count, int top)
        {
            worksheet.Cells[top+1, 1, top + 7, 1].Merge = true;
            worksheet.Cells[top + 1, 1, top + 7, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
            worksheet.Cells[top + 1, 1, top + 7, 1].Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
            ApplyBoldOutline(worksheet, top + 1, 1, top + 7, 1);
            worksheet.Cells[top + 1, 1].Value = problemName;

            worksheet.Cells[top + 1, 2, top+2, 2].Merge = true;
            worksheet.Cells[top + 1, 2, top+2, 2].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
            worksheet.Cells[top + 1, 2, top + 2, 2].Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
            ApplyBoldOutline(worksheet, top + 1, 2, top + 2, 2);
            worksheet.Cells[top + 1, 2].Value = $"{count} Iter";

            worksheet.Cells[top+1, 3, top + 1, 6].Merge = true;
            worksheet.Cells[top+1, 3, top + 1, 6].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
            worksheet.Cells[top + 1, 3, top + 1, 6].Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
            ApplyBoldOutline(worksheet, top+1, 3, top + 2, 6);
            ApplyBoldOutline(worksheet, top + 1, 3, top + 7, 6);
            worksheet.Cells[top + 1, 3].Value = "No Fake";

            worksheet.Cells[top + 1, 7, top + 1, 10].Merge = true;
            worksheet.Cells[top + 1, 7, top + 1, 10].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
            worksheet.Cells[top + 1, 7, top + 1, 101].Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
            ApplyBoldOutline(worksheet, top + 1, 7, top + 2, 10);
            ApplyBoldOutline(worksheet, top + 1, 7, top + 7, 10);
            worksheet.Cells[top + 1, 7].Value = "Fake Threshold = 0";

            worksheet.Cells[top+1, 11, top + 1, 14].Merge = true;
            worksheet.Cells[top+1, 11, top + 1, 14].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
            worksheet.Cells[top + 1, 11, top + 1, 14].Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
            ApplyBoldOutline(worksheet, top + 1, 11, top + 2, 14);
            ApplyBoldOutline(worksheet, top + 1, 11, top + 7, 14);
            worksheet.Cells[top + 1, 11].Value = "Fake Threshold = 0.2";

            worksheet.Cells[top + 1, 15, top + 1, 18].Merge = true;
            worksheet.Cells[top + 1, 15, top + 1, 18].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
            worksheet.Cells[top + 1, 15, top + 1, 18].Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
            ApplyBoldOutline(worksheet, top + 1, 15, top + 2, 18);
            ApplyBoldOutline(worksheet, top + 1, 15, top + 7, 18);
            worksheet.Cells[top + 1, 15].Value = "Fake Threshold = 0.5";

            worksheet.Cells[top+1, 19, top + 1, 22].Merge = true;
            worksheet.Cells[top+1, 19, top + 1, 22].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
            worksheet.Cells[top + 1, 19, top + 1, 22].Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
            ApplyBoldOutline(worksheet, top + 1, 19, top + 2, 22);
            ApplyBoldOutline(worksheet, top + 1, 19, top + 7, 22);
            worksheet.Cells[top + 1, 19].Value = "Fake Threshold = 0.8";

            Dictionary<int, string> costs = new Dictionary<int, string> { { 0, "No Costs" }, { 1, "Cost=1" }, { 2, "Cost=5" }, { 3, "Cost=20" } };
            for (int i = 0; i < 20; i++)
            {
                worksheet.Cells[top + 2, i + 3].Value = costs[i % 4];
            }

            worksheet.Cells[top + 3, 2].Value = "Actions";
            worksheet.Cells[top + 4, 2].Value = "Deadends";
            worksheet.Cells[top + 5, 2].Value = "Replanning";
            worksheet.Cells[top + 6, 2].Value = "Failures";
            worksheet.Cells[top + 7, 2].Value = "Time";
            ApplyBoldOutline(worksheet, top + 3, 2, top + 7, 2);
            ApplyBoldOutline(worksheet, top + 1, 1, top + 7, 22);
        }

        private static void WriteToExcel(string folderPath, List<Tuple<List<ExecutionData>, TimeSpan, InaccuracyHandlingStrategies, string>> EDList)
        {
            string filePath = folderPath + $"/output_summary_{DateTime.Now.ToString("MM_dd_HHmmss")}.xlsx";
            string problemName = EDList[0].Item1[0].Problem.Name;
            int count = EDList[0].Item1.Count;
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add(EDList[0].Item1[0].Domain.Name);

                // Merging cells for header
                int top = 2;
                formatTable(worksheet, problemName, count, top);

                int col = 3;

                foreach (var entry in EDList)
                {
                    int row = 3;

                    List<ExecutionData> ED = entry.Item1;
                    TimeSpan time = entry.Item2;
                    worksheet.Cells[top + row++, col].Value = ED.Average(obj => obj.Actions);
                    worksheet.Cells[top + row++, col].Value = ED.Average(obj => obj.stepsToReplan.Count);
                    worksheet.Cells[top + row++, col].Value = ED.Average(obj => obj.ReplanningCount);
                    worksheet.Cells[top + row++, col].Value = ED.Average(obj => obj.FailCount);
                    worksheet.Cells[top + row++, col].Value = $"{ time.TotalMinutes:00}:{ time.Seconds:00}.{ time.Milliseconds:000}";
                    col++;
                }

                // Save the file
                FileInfo fileInfo = new FileInfo(filePath);
                package.SaveAs(fileInfo);
            }
            //}
        }

        public static void DomainInfo(string sDomainFile, string sProblemFile)
        {
            Parser parser = new Parser();

            Domain domain = parser.ParseDomain(sDomainFile);
            Problem problem = parser.ParseProblem(sProblemFile, domain);

            Console.WriteLine($"Init Known - True:\t{problem.Known.Where(p=>!p.Negation && domain.Uncertainties.Select(u=>u.Name).Contains(p.Name)).Count()}");
            Console.WriteLine($"Init Known - False:\t{problem.Known.Where(p => p.Negation && domain.Uncertainties.Select(u => u.Name).Contains(p.Name)).Count()}");
        }

        static void ThrowError(object state)
        {
            // Throw a runtime exception after 5 minutes
            throw new TimeoutException("Operation timed out after 5 minutes.");
        }


        public static void RunPlanner(string sDomainFile, string sProblemFile, string sNegateFile, string sOutputFile, bool bOnline, bool bValidate = false)
        {

            Debug.WriteLine("Reading domain and problem");
            Parser parser = new Parser();

            int cIterations = Options.Iterations;
            RandomGenerator.Init();
            List<int> seeds = new List<int>();
            for (int i = 0; i < cIterations; i++)
            {
                seeds.Add(RandomGenerator.Next(1000));
            }
            List<Tuple<Options.InaccuracyHandlingStrategies, bool, int, bool, double>> settings = new List<Tuple<Options.InaccuracyHandlingStrategies, bool, int, bool, double>>();
            if (Options.FalsePositive)
            {
                //settings.Add(Tuple.Create(InaccuracyHandlingStrategies.BL0, true));
                //settings.Add(Tuple.Create(InaccuracyHandlingStrategies.BLOptimistic, true));
                settings.Add(Tuple.Create(InaccuracyHandlingStrategies.FailHandler, true, 0, false, 0.0));
            }
            else
            {
                //settings.Add(Tuple.Create(InaccuracyHandlingStrategies.BL0, false));
                //settings.Add(Tuple.Create(InaccuracyHandlingStrategies.BLOptimistic, false));
                //settings.Add(Tuple.Create(InaccuracyHandlingStrategies.MakeTrue, false));
                //settings.Add(Tuple.Create(InaccuracyHandlingStrategies.Baseline, false));
                settings.Add(Tuple.Create(InaccuracyHandlingStrategies.OverspecifiedPrecondition, false, 0, false, 0.0));
                settings.Add(Tuple.Create(InaccuracyHandlingStrategies.OverspecifiedPrecondition, true, 1, false, 0.0));
                settings.Add(Tuple.Create(InaccuracyHandlingStrategies.OverspecifiedPrecondition, true, 5, false, 0.0));
                settings.Add(Tuple.Create(InaccuracyHandlingStrategies.OverspecifiedPrecondition, true, 20, false, 0.0));

                settings.Add(Tuple.Create(InaccuracyHandlingStrategies.OverspecifiedPrecondition, false, 0, true, 0.0));
                settings.Add(Tuple.Create(InaccuracyHandlingStrategies.OverspecifiedPrecondition, true, 1, true, 0.0));
                settings.Add(Tuple.Create(InaccuracyHandlingStrategies.OverspecifiedPrecondition, true, 5, true, 0.0));
                settings.Add(Tuple.Create(InaccuracyHandlingStrategies.OverspecifiedPrecondition, true, 20, true, 0.0));

                settings.Add(Tuple.Create(InaccuracyHandlingStrategies.OverspecifiedPrecondition, false, 0, true, 0.2));
                settings.Add(Tuple.Create(InaccuracyHandlingStrategies.OverspecifiedPrecondition, true, 1, true, 0.2));
                settings.Add(Tuple.Create(InaccuracyHandlingStrategies.OverspecifiedPrecondition, true, 5, true, 0.2));
                settings.Add(Tuple.Create(InaccuracyHandlingStrategies.OverspecifiedPrecondition, true, 20, true, 0.2));

                settings.Add(Tuple.Create(InaccuracyHandlingStrategies.OverspecifiedPrecondition, false, 0, true, 0.5));
                settings.Add(Tuple.Create(InaccuracyHandlingStrategies.OverspecifiedPrecondition, true, 1, true, 0.5));
                settings.Add(Tuple.Create(InaccuracyHandlingStrategies.OverspecifiedPrecondition, true, 5, true, 0.5));
                settings.Add(Tuple.Create(InaccuracyHandlingStrategies.OverspecifiedPrecondition, true, 20, true, 0.5));

                settings.Add(Tuple.Create(InaccuracyHandlingStrategies.OverspecifiedPrecondition, false, 0, true, 0.8));
                settings.Add(Tuple.Create(InaccuracyHandlingStrategies.OverspecifiedPrecondition, true, 1, true, 0.8));
                settings.Add(Tuple.Create(InaccuracyHandlingStrategies.OverspecifiedPrecondition, true, 5, true, 0.8));
                settings.Add(Tuple.Create(InaccuracyHandlingStrategies.OverspecifiedPrecondition, true, 20, true, 0.8));

            }
            Domain oDomain = parser.ParseDomain(sDomainFile);

            

            List<Predicate> toNegate = new List<Predicate>();
            List<Tuple<List<ExecutionData>, TimeSpan, InaccuracyHandlingStrategies, string>> ExecutionData = new List<Tuple<List<ExecutionData>, TimeSpan, InaccuracyHandlingStrategies, string>>();
            Debug.WriteLine("Done reading domain and problem");
            Problem problem = parser.ParseProblem(sProblemFile, oDomain);

            if (bOnline)
            {
                for (int j= 0; j < settings.Count; j++)
                {
                    Tuple<Options.InaccuracyHandlingStrategies, bool, int, bool, double> setting = settings[j];

                    Options.InaccuracyHandlingStrategy = setting.Item1;
                    //Options.FalsePositive = setting.Item2;
                    Options.UseCosts = setting.Item2;
                    Options.ActionCost = setting.Item3;
                    Options.UseFakePreds = setting.Item4;
                    Options.fakePredicateThreshold = setting.Item5;
                    int cSuccess = 0;

                    Console.WriteLine("Starting " + oDomain.Name);
                    Console.WriteLine($"Settings: {setting}");

                    List<ExecutionData> ED = new List<ExecutionData>();
                    Queue myQueue = new Queue();
                    for (int i = 0; ED.Count < cIterations && i<300; i++) // (int i = 0; i < cIterations; i++) // 
                    {

                        if (i < cIterations)
                        {
                            RandomGenerator.Init(seeds.ElementAt(i));
                            Console.WriteLine($"Random Seed {seeds.ElementAt(i)}");
                            if (seeds.ElementAt(i)<0)
                            {
                                continue;
                            }
                        }
                        else
                        {
                            if (i >= seeds.Count)
                                seeds.Add(RandomGenerator.Next(1000));
                            RandomGenerator.Init(seeds.ElementAt(i)); 
                            Console.WriteLine($"Random Seed {seeds.ElementAt(i)}");
                        }


                        Domain domain = new Domain(oDomain);
                        if (Options.UseFakePreds)
                        {
                            domain.AddFakePredicates();
                        }
                        problem = parser.ParseProblem(sProblemFile, domain);

                        SDRPlanner sdr = new SDRPlanner(domain, problem, toNegate);
                        Simulator sim = new Simulator(domain, problem);
                        DateTime dtStart = DateTime.Now;

                        //Timer timer = new Timer(ThrowError, null, TimeSpan.FromMinutes(7), Timeout.InfiniteTimeSpan);
                        int idx = 0;
                        //try
                        //{
                        while (!(sim.GoalReached && sdr.GoalReached))
                        {
                            string sAction = sdr.GetAction();
                            if (sAction == null) //we are already at goalstate
                            {
                                Console.Write("*");
                                continue;
                            }
                            string sObservation = sim.Apply(sAction);
                            if (sObservation == "Fail")
                                sdr.ExecutionData.FailCount++;
                            bool bResult = sdr.SetObservation(sObservation);
                            if (Options.Verbose)
                                Console.WriteLine(idx + ") Executed " + sAction + ", received " + sObservation);
                            idx++;

                        }
                        cSuccess++;
                        Console.WriteLine("=========================================================================");

                        Console.WriteLine($"{problem.Name}: Success # {cSuccess}/{i + 1}\t\t{idx} Actions");
                        DateTime dtEnd = DateTime.Now;
                        sdr.ExecutionData.Time = dtEnd - dtStart;
                        sdr.ExecutionData.Actions = idx;
                            
                        if (sdr.ExecutionData.FailCount > 0)
                        {
                            //Console.WriteLine($"Succeeded with {sdr.ExecutionData.FailCount} Fails");
                        }
                        if (sdr.ExecutionData.ReplanningCount > 0)
                        {
                            //Console.WriteLine($"Succeeded with {sdr.ExecutionData.ReplanningCount} deadends");
                            ED.Add(sdr.ExecutionData); //this is here bc it makes more sense for replanning
                        }
                        else
                        {
                            seeds[i]=-1;
                        }
                        if (Options.OverSpecifyThreshold == 0) 
                            ED.Add(sdr.ExecutionData);
                        //}
                        //catch (Exception e)
                        //{
                        //    Console.WriteLine(e.Message);
                        //    //if (!domain.Name.Equals("wumpus"))
                        //    //    throw e;
                        //}
                    }
                    TimeSpan totalTime = TimeSpan.Zero;
                    foreach (var obj in ED)
                    {
                        totalTime += obj.Time;
                    }

                    //Console.WriteLine("\n\n");
                    //saveToDict(ED, totalTime);
                    if (ED.Count > 0)
                    {
                        writeSummary(Path.GetDirectoryName(sDomainFile), ED, TimeSpan.FromTicks(totalTime.Ticks / ED.Count), setting.ToString());
                    }
                    ExecutionData.Add(Tuple.Create(ED, TimeSpan.FromTicks(totalTime.Ticks / Math.Max(1, ED.Count)), InaccuracyHandlingStrategy, setting.ToString()));
                }
                WriteToExcel(Path.GetDirectoryName(sDomainFile), ExecutionData);

                //printData(ExecutionData);
            }
            else
            {
                //Options.SDR_OBS = true;
                CPORPlanner cpor = new CPORPlanner(oDomain, problem);
                cpor.InfoLevel = 1;
                ConditionalPlanTreeNode n = cpor.OfflinePlanning();
                cpor.WritePlan(sOutputFile, n);

                if (bValidate)
                    if (!cpor.ValidatePlanGraph(n))
                        Console.WriteLine("Invalid plan");
            }
            //StreamWriter sw = new StreamWriter(sOutputFile);
            //sw.WriteLine("Success!");
            //sw.Close();
        }


    }
}
