using CPORLib;
using CPORLib.FFCS;
using CPORLib.Tools;
using OfficeOpenXml;
using RunCPOR;
using System;
using System.IO;
using System.Linq.Expressions;
using System.Numerics;
using static CPORLib.Tools.Options;
using static CPORLib.Tools.RandomGenerator;

public class Program
{
    static bool canOverride = true;
    public static async Task RunTest(string sName, bool bOnline, string sPath)
    {
        
        sPath = sPath + sName;
        string sDomainFile = Path.Combine(sPath, "d.pddl");
        string sProblemFile = Path.Combine(sPath, "p.pddl");
        string sOutputFile = Path.Combine(sPath, "out.txt");

        Run.DomainInfo(sDomainFile, sProblemFile);
        Console.WriteLine("Starting " + sName);

        List<Tuple<Options.InaccuracyHandlingStrategies, bool, int, bool, double>> settings = new List<Tuple<Options.InaccuracyHandlingStrategies, bool, int, bool, double>>();
        SetSettings(sName, settings);
        

        //memory of seeds that don't lead to deadends in each configuration
        HashSet<int> NoDeadends = new HashSet<int>();

        RandomGenerator.Init();
        List<int> seeds = new List<int>();
        for (int i = 0; i < Options.Iterations; i++)
        {
            seeds.Add(RandomGenerator.Next(1000));
        }

        List<Tuple<List<ExecutionData>, TimeSpan, InaccuracyHandlingStrategies, string>> ExecutionData = new List<Tuple<List<ExecutionData>, TimeSpan, InaccuracyHandlingStrategies, string>>();
        string folderPath = ExcelHelper.CreateOutputFolder(Path.GetDirectoryName(sDomainFile));

        for (int j = 0; j < settings.Count; j++)
        {
            Tuple<Options.InaccuracyHandlingStrategies, bool, int, bool, double> setting = settings[j];

            Options.InaccuracyHandlingStrategy = setting.Item1;
            Options.UseCosts = setting.Item2;
            Options.ActionCost = setting.Item3;
            Options.UseFakePreds = setting.Item4;
            Options.fakePredicateThreshold = setting.Item5;

            if (j % 4 == 0)
                NoDeadends = new HashSet<int>();

            Console.WriteLine($"Settings: {setting}");
            List<ExecutionData> ED = new List<ExecutionData>();
            TimeSpan totalTime = TimeSpan.Zero; 
            bool TimeOutFlag = false;
            for (int i = 0; ED.Count < Options.Iterations && i < Options.MaxIterations; i++) // (int i = 0; i < cIterations; i++) // 
            {
                SetRandomSeed(i, seeds, NoDeadends);
                if (canOverride)
                {
                    //Console.WriteLine("===================Overwriting random seed!!===================");
                    //RandomGenerator.Init(755); //pessimistic fail only bugs - blocks 961
                    Console.WriteLine();
                }

                // Create a CancellationTokenSource to manage cancellation
                var cts = new CancellationTokenSource();
                var cancellationToken = cts.Token;

                var result = StartPlanner(sDomainFile, sProblemFile, sOutputFile, bOnline, NoDeadends, false, cancellationToken);
                int timing = 0;
                while (timing++ < Options.MaxTime * Options.Factor && !result.IsCompleted)
                    Thread.Sleep(1000 / Options.Factor);

                if (!result.IsCompleted)
                {
                    Console.WriteLine("Main thread requests stop...");
                    cts.Cancel();
                    TimeOutFlag = true;
                    break;
                }

                var res = await result;
                if (res is null)
                {
                    //Console.WriteLine($"Timeout");
                    continue;
                }
                if (res.FailCount > 0)
                {
                    //Console.WriteLine($"Succeeded with {sdr.ExecutionData.FailCount} Fails");
                }
                //if (res.ReplanningCount > 0)
                //{
                //    Console.WriteLine($"Success #{ED.Count}/{Options.Iterations}");
                //    //    ED.Add(res); //this is here bc it makes more sense for replanning
                //    //Console.WriteLine($"Success #{ED.Count}/{Options.Iterations}");
                //}
                //else
                //{
                //    Console.WriteLine($"Overspec did not lead to deadend");
                //}
                //if (Options.OverspecifiedPreconditions)
                //    ED.Add(res);
                ED.Add(res); //this is here bc it makes more sense for replanning
                Console.WriteLine($"Success #{ED.Count}/{Options.Iterations}");
            }

            foreach (var obj in ED)
            {
                totalTime += obj.Time;
            }

            if (!TimeOutFlag)
                ExecutionData.Add(Tuple.Create(ED, TimeSpan.FromTicks(totalTime.Ticks / Math.Max(1, ED.Count)), InaccuracyHandlingStrategy, setting.ToString()));
            
            if (ED.Count > 0)
            {

                ExcelHelper.writeSummary(folderPath, ED, TimeSpan.FromTicks(totalTime.Ticks / ED.Count), setting.ToString());
            }
        }
        ExcelHelper.WriteToExcel(folderPath, ExecutionData, settings.Select(t => t.ToString()).ToList());
    }

    private static void SetSettings(string sName, List<Tuple<Options.InaccuracyHandlingStrategies, bool, int, bool, double>> settings)
    {
        if (sName.StartsWith("wumpus"))
            Options.SDR_OBS = true;

        settings.Add(Tuple.Create(InaccuracyHandlingStrategies.Lazy, true, 1, true, 0.2));
        settings.Add(Tuple.Create(InaccuracyHandlingStrategies.BLOptimistic, true, 1, false, 0.2));
        settings.Add(Tuple.Create(InaccuracyHandlingStrategies.BLPessimistic, true, 1, true, 0.2));
        if (Options.PredicateInaccuracy!=PredicateInaccuracies.FalsePositive)
            settings.Add(Tuple.Create(InaccuracyHandlingStrategies.MakeTrue, true, 1, false, 0.2));

        //mixes
        //settings.Add(Tuple.Create(InaccuracyHandlingStrategies.Baseline, false, 1, true, 0.0));
        //settings.Add(Tuple.Create(InaccuracyHandlingStrategies.Baseline, false, 1, false, 0.0));
        //settings.Add(Tuple.Create(InaccuracyHandlingStrategies.Baseline, true, 1, true, 0.0));
        //settings.Add(Tuple.Create(InaccuracyHandlingStrategies.Baseline, true, 1, false, 0.0));


        //settings.Add(Tuple.Create(InaccuracyHandlingStrategies.BL0, false));
        //settings.Add(Tuple.Create(InaccuracyHandlingStrategies.BLOptimistic, false));
        //settings.Add(Tuple.Create(InaccuracyHandlingStrategies.MakeTrue, false));
        //settings.Add(Tuple.Create(InaccuracyHandlingStrategies.Baseline, false));
        //settings.Add(Tuple.Create(InaccuracyHandlingStrategies.OverspecifiedPrecondition, false, 0, false, 0.0));
        //settings.Add(Tuple.Create(InaccuracyHandlingStrategies.OverspecifiedPrecondition, true, 1, false, 0.0));
        //settings.Add(Tuple.Create(InaccuracyHandlingStrategies.OverspecifiedPrecondition, true, 5, false, 0.0));
        //settings.Add(Tuple.Create(InaccuracyHandlingStrategies.OverspecifiedPrecondition, true, 20, false, 0.0));

        //settings.Add(Tuple.Create(InaccuracyHandlingStrategies.OverspecifiedPrecondition, false, 0, true, 0.0));
        //settings.Add(Tuple.Create(InaccuracyHandlingStrategies.OverspecifiedPrecondition, true, 1, true, 0.0));
        //settings.Add(Tuple.Create(InaccuracyHandlingStrategies.OverspecifiedPrecondition, true, 5, true, 0.0));
        //settings.Add(Tuple.Create(InaccuracyHandlingStrategies.OverspecifiedPrecondition, true, 20, true, 0.0));

        //settings.Add(Tuple.Create(InaccuracyHandlingStrategies.OverspecifiedPrecondition, false, 0, true, 0.2));
        //settings.Add(Tuple.Create(InaccuracyHandlingStrategies.OverspecifiedPrecondition, true, 1, true, 0.2));
        //settings.Add(Tuple.Create(InaccuracyHandlingStrategies.OverspecifiedPrecondition, true, 5, true, 0.2));
        //settings.Add(Tuple.Create(InaccuracyHandlingStrategies.OverspecifiedPrecondition, true, 20, true, 0.2));

        //settings.Add(Tuple.Create(InaccuracyHandlingStrategies.OverspecifiedPrecondition, false, 0, true, 0.5));
        //settings.Add(Tuple.Create(InaccuracyHandlingStrategies.OverspecifiedPrecondition, true, 1, true, 0.5));
        //settings.Add(Tuple.Create(InaccuracyHandlingStrategies.OverspecifiedPrecondition, true, 5, true, 0.5));
        //settings.Add(Tuple.Create(InaccuracyHandlingStrategies.OverspecifiedPrecondition, true, 20, true, 0.5));

        //settings.Add(Tuple.Create(InaccuracyHandlingStrategies.OverspecifiedPrecondition, false, 0, true, 0.8));
        //settings.Add(Tuple.Create(InaccuracyHandlingStrategies.OverspecifiedPrecondition, true, 1, true, 0.8));
        //settings.Add(Tuple.Create(InaccuracyHandlingStrategies.OverspecifiedPrecondition, true, 5, true, 0.8));
        //settings.Add(Tuple.Create(InaccuracyHandlingStrategies.OverspecifiedPrecondition, true, 20, true, 0.8));

    }


    static async Task<ExecutionData> StartPlanner(string sDomainFile, string sProblemFile, string sOutputFile, bool bOnline, HashSet<int> NoDeadends, bool bValidate, CancellationToken cancellationToken)
    {
#pragma warning disable CS8603 // Possible null reference return.
        return await Task.Run(() =>
        {
            try
            {
                return Run.RunPlanner(sDomainFile, sProblemFile, sOutputFile, bOnline, NoDeadends, false, cancellationToken);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return null;
            }

        });
#pragma warning restore CS8603 // Possible null reference return.
    }


    private static void SetRandomSeed(int curr_i, List<int> seeds, HashSet<int> NoDeadEnds)
    {


        for (int i = curr_i; i < Options.MaxIterations; i++) // (int i = 0; i < cIterations; i++) // 
        {
            if (i < Options.Iterations)
            {
                if (NoDeadEnds.Contains(i))
                {
                    continue;
                }
                RandomGenerator.Init(seeds.ElementAt(i));
                Console.WriteLine($"Random Seed {seeds.ElementAt(i)}");
                return;
            }
            else
            {
                if (i >= seeds.Count)
                    seeds.Add(RandomGenerator.Next(1000));
                if (NoDeadEnds.Contains(i))
                {
                    continue;
                }
                RandomGenerator.Init(seeds.ElementAt(i));
                Console.WriteLine($"Random Seed {seeds.ElementAt(i)}");
                return;
            }
        }

    }


    public static void TestAll(bool bOnline)
    {
        //FFUtilities.Verbose = true;
        Options.Verbose = true;

        //for FP/FN usecases

        //Options.threshold = 0;

        gcmd_line.display_info = 0;
        gcmd_line.debug = 0;
        string sPath = @"C:\Users\travkaie\OneDrive - Intel Corporation\Documents\School\up-cpor\Tests\";
        //Options.SDR_OBS = true;
        RunTest("unix1", bOnline, sPath);
        RunTest("doors5", bOnline, sPath);
        RunTest("colorballs2-2", bOnline, sPath);
        RunTest("clog5", bOnline, sPath);
        RunTest("blocks3", bOnline, sPath);
        //RunTest("wumpus05", bOnline, sPath);



        //RunTest("localize5", bOnline);
        //RunTest("localize5knoisy", bOnline);
        //RunTest("medpks010", bOnline);

    }

    public static void Main(string[] args)
    {
        FFUtilities.Verbose = false;
        TestAll(true);
        return;
        canOverride = false;
        if (args.Length < 1)
        {
            Console.WriteLine("Usage: RunPlanner domain_file problem_file [false_pos] [new] (verbose - optional)");
        }
        else
        {
            if (args.Length == 2)
            {
                 int.TryParse(args[1], out Options.MaxTime);
            }
            _ = RunTest(args[0], true, @"Tests/");
            return;
        }
    }

    private static void TestClassicalFFCS()
    {
        string sDomainFile = @"C:\Users\shanigu\Downloads\domain-driver1.pddl";
        string sProblemFile = @"C:\Users\shanigu\Downloads\problem-driver1.pddl";
        MemoryStream ms = new MemoryStream();
        StreamWriter sw = new StreamWriter(ms);
        using (StreamReader sr = new StreamReader(sDomainFile))
        {
            string sDomain = sr.ReadToEnd();
            sw.Write(sDomain);
            sr.Close();
        }
        using (StreamReader sr = new StreamReader(sProblemFile))
        {
            string sProblem = sr.ReadToEnd();
            sw.Write(sProblem);
            sr.Close();
        }
        sw.Flush();
        ms.Position = 0;
        FF ff = new FF(ms);
        List<string> lPlan = ff.Plan();
    }
}