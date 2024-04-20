using CPORLib.Algorithms;
using CPORLib.FFCS;
using CPORLib.LogicalUtilities;
using CPORLib.Parsing;
using CPORLib.PlanningModel;
using CPORLib.Tools;
using Python.Runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
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

        private static void writeSummary(string folderPath, List<ExecutionData> ED, TimeSpan time)
        {
            string filePath = folderPath + $"/output_summary_{DateTime.Now.ToString("MM_dd_HHmmss")}.txt";
            Console.WriteLine($"Outputs saved to {filePath}");
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                writer.WriteLine($"Domain/Problem: {ED[0].Domain.Name}/{ED[0].Problem.Name}\n");
                writer.WriteLine($"Number of Successful iterations: {ED.Count}");
                writer.WriteLine($"False Positives/Negatives: {Options.FalsePositive}");
                writer.WriteLine($"Mode: {Options.InaccuracyHandlingStrategy}");
                writer.WriteLine($"Average Time: {time.TotalMinutes:00}:{time.Seconds:00}.{time.Milliseconds:000}");
                writer.WriteLine($"Average Actions: {ED.Average(obj => obj.Actions)}");
                writer.WriteLine($"Average Number of Negations: {ED.Average(obj => obj.NumberOfNegations)}");
                writer.WriteLine($"Average Number of Replannings: {ED.Average(obj => obj.ReplanningCount)}");
                writer.WriteLine("\n\n");
                for (int i = 0;i < ED.Count;i++)
                {
                    ExecutionData ed = ED[i];
                    writer.WriteLine($"{i}:");
                    writer.WriteLine($"Time: {ed.Time.TotalMinutes:00}:{ed.Time.Seconds:00}.{ed.Time.Milliseconds:000}");
                    writer.WriteLine($"Number of Negations: {ed.NumberOfNegations}");
                    writer.WriteLine($"Number of actions: {ed.Actions}");
                    writer.WriteLine($"Replanning: {ed.ReplanningCount}");
                    writer.WriteLine($"Replanning w/ Mod: {ed.ReplanningIncludeModCount}");
                    writer.WriteLine($"FailCount: {ed.FailCount}");
                    writer.WriteLine("\n\n");
                }
            }
        }

        public static void RunPlanner(string sDomainFile, string sProblemFile, string sNegateFile, string sOutputFile, bool bOnline, bool bValidate = false)
        {

            Debug.WriteLine("Reading domain and problem");
            Parser parser = new Parser();

            Domain domain = parser.ParseDomain(sDomainFile);
            int cIterations = Options.Iterations;
            RandomGenerator.Init();
            int seed = RandomGenerator.Next(100);
            Problem[] problems1 = new Problem[cIterations];
            Problem[] problems2 = new Problem[cIterations];
            List<Tuple<Options.InaccuracyHandlingStrategies, bool, Problem[]>> settings = new List<Tuple<Options.InaccuracyHandlingStrategies, bool, Problem[]>>
                {
                    //Tuple.Create(InaccuracyHandlingStrategies.Baseline, true),
                    Tuple.Create(InaccuracyHandlingStrategies.FailHandler, true, problems1),
                    //Tuple.Create(InaccuracyHandlingStrategies.MakeTrue, false, problems2),
                    //Tuple.Create(InaccuracyHandlingStrategies.Baseline, false, problems1)
                    
                };
            //settings.Add(Tuple.Create(InaccuracyHandlingStrategy, Options.FalsePositive, problems1));
            RandomGenerator.Init(seed);
            for (int i = 0; i < cIterations; i++)
            {
                problems1[i] = parser.ParseProblem(sProblemFile, domain);
            }
            RandomGenerator.Init(seed);
            for (int i = 0; i < cIterations; i++)
            {
                problems2[i] = parser.ParseProblem(sProblemFile, domain);
            }
            List<Predicate> toNegate = parser.ParseNegations(domain, sNegateFile);
            ExecutionData ExecutionData = new ExecutionData("", "", domain, problems1[0], Options.DeadendStrategy);

            Debug.WriteLine("Done reading domain and problem");


            //TestHAdd(domain, problem);
            //return;

            Options.TagsCount = 2;
            //Options.SDR_OBS = true;


            if (bOnline)
            {
                double thresh = Options.threshold;

                for (int j= 0; j < settings.Count; j++)
                {
                    Options.threshold = thresh;
                    Tuple<Options.InaccuracyHandlingStrategies, bool, Problem[]> setting = settings[j];

                    RandomGenerator.Init(seed);//we want same seed accross trials
                    Options.InaccuracyHandlingStrategy = setting.Item1;
                    Options.FalsePositive = setting.Item2;
                    int cSuccess = 0;

                    Console.WriteLine("Starting " + domain.Name);
                    List<ExecutionData> ED = new List<ExecutionData>();
                    DateTime dtStartAll = DateTime.Now;
                    for (int i = 0; i < cIterations; i++)
                    {
                        Problem problem = setting.Item3[i];
                        ExecutionData = new ExecutionData("", "", domain, problem, Options.DeadendStrategy);
                        Debug.WriteLine("Done reading domain and problem");
                        SDRPlanner sdr = new SDRPlanner(domain, problem, toNegate);
                        Simulator sim = new Simulator(domain, problem);
                        DateTime dtStart = DateTime.Now;
                        int idx = 0;
                        try
                        {
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
                                Console.WriteLine(idx + ") Executed " + sAction + ", received " + sObservation);
                                idx++;

                            }
                            cSuccess++;
                            Console.WriteLine($"{problem.Name}: Success # {cSuccess}/{i + 1}");
                            DateTime dtEnd = DateTime.Now;
                            sdr.ExecutionData.Time = dtEnd - dtStart;
                            sdr.ExecutionData.Actions = idx;
                            //ExecutionData.Planning = cPlanning;
                            if (sdr.ExecutionData.ReplanningCount > 0)
                            {
                                Console.WriteLine($"Succeeded with {sdr.ExecutionData.ReplanningCount} deadends");
                                //saveNegations(sDomainFile, sdr, i);
                            }
                            if (sdr.ExecutionData.FailCount > 0)
                            {
                                Console.WriteLine($"Succeeded with {sdr.ExecutionData.FailCount} Fails");
                                //saveNegations(sDomainFile, sdr, i);
                            }
                            ED.Add(sdr.ExecutionData);
                        }
                        catch (Exception e)
                        {
                            if (domain.Name != "wumpus")
                                throw e;
                        }

                    }
                    DateTime dtEndAll = DateTime.Now;
                    TimeSpan totalTime = TimeSpan.Zero;
                    foreach (var obj in ED)
                    {
                        totalTime += obj.Time;
                    }
                    //saveToDict(ED, totalTime);
                    //if (ED.Count>0)
                    //    writeSummary(Path.GetDirectoryName(sDomainFile), ED, TimeSpan.FromTicks(totalTime.Ticks / ED.Count));
                }
            }
            else
            {
                //Options.SDR_OBS = true;
                CPORPlanner cpor = new CPORPlanner(domain, problems1[0]);
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
