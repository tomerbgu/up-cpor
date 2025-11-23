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
    public static async Task RunTest(string sName, bool bOnline, string sTestPath, bool op=false)
    {
        
        string sPath = sTestPath + sName;
        string sDomainFile = Path.Combine(sPath, "d.pddl");
        string sProblemFile = Path.Combine(sPath, "p.pddl");
        string sOutputFile = Path.Combine(sPath, "out.txt");

        Run.DomainInfo(sDomainFile, sProblemFile);
        //return;
        Console.WriteLine("Starting " + sName);

        List<Tuple<Options.InaccuracyHandlingStrategies, bool, int, bool, int>> settings = new List<Tuple<Options.InaccuracyHandlingStrategies, bool, int, bool, int>>();
        SetSettings(sName, settings);
        

        //memory of seeds that don't lead to deadends in each configuration
        HashSet<int> NoDeadends = new HashSet<int>();

        RandomGenerator.Init();
        List<int> seeds = new List<int>();
        for (int i = 0; i < Options.Iterations; i++)
        {
            seeds.Add(RandomGenerator.Next(1000));
        }
        HashSet<int> seedSet = seeds.ToHashSet();
        List<Tuple<List<ExecutionData>, TimeSpan, InaccuracyHandlingStrategies, string>> ExecutionData = new List<Tuple<List<ExecutionData>, TimeSpan, InaccuracyHandlingStrategies, string>>();
        string folderPath = ExcelHelper.CreateOutputFolder(Path.GetDirectoryName(sDomainFile));
        string genPath = ExcelHelper.CreateOutputFolder(Path.GetDirectoryName(sTestPath));
        for (int j = 0; j < settings.Count; j++)
        {
            Tuple<Options.InaccuracyHandlingStrategies, bool, int, bool, int> setting = settings[j];

            Options.InaccuracyHandlingStrategy = setting.Item1;
            Options.UseCosts = setting.Item2;
            Options.PrecondCost = setting.Item3;
            Options.UseFakePreds = setting.Item4;
            Options.MakeCost = setting.Item5;

            if (j % 4 == 0)
                NoDeadends = new HashSet<int>();

            Console.WriteLine($"Settings: {setting}");
            List<ExecutionData> ED = new List<ExecutionData>();
            TimeSpan totalTime = TimeSpan.Zero;
            int numIterations = op ? Options.MaxIterations : Options.Iterations;
            for (int i = 0; i < numIterations && ED.Count<Options.Iterations; i++)
            {
                //needs reset bc of lazyoptimistic
                Options.InaccuracyHandlingStrategy = setting.Item1;

                SetRandomSeed(i, seeds, NoDeadends);
                if (canOverride)
                {
                    Console.WriteLine("===================Overwriting random seed!!===================");
                    //RandomGenerator.Init(59);
                    //RandomGenerator.Init(442);
                    Console.WriteLine();
                }

                // Create a CancellationTokenSource to manage cancellation
                var cts = new CancellationTokenSource();
                var cancellationToken = cts.Token;

                var result = StartPlanner(sDomainFile, sProblemFile, sOutputFile, bOnline, false, cancellationToken);
                async Task MonitorWorker()
                {
                    await result; // Ensures proper shutdown handling
                    Console.WriteLine("Worker task completed");
                }
                int timing = 0;
                while (timing++ < Options.MaxTime * Options.Factor && !result.IsCompleted)
                    Thread.Sleep(1000 / Options.Factor);

                if (!result.IsCompleted)
                {
                    Console.WriteLine("Main thread requests stop...");
                    cts.Cancel();
                    _ = MonitorWorker(); // Fire and forget

                    //save lists of random seeds that were successful
                    seedSet.Remove(seeds.ElementAt(i));
                    continue;
                }
                
                var res = await result;
                if (res is null)
                {
                    seedSet.Remove(seeds.ElementAt(i));
                    continue;
                }
                res.Seed = seeds.ElementAt(i);

                if (op)
                {
                    if (res.ReplanningCount > 0 || InaccuracyHandlingStrategy==InaccuracyHandlingStrategies.BLOptimistic || InaccuracyHandlingStrategy == InaccuracyHandlingStrategies.BLPessimistic)
                    {
                        ED.Add(res);
                        Console.WriteLine($"Success #{ED.Count}/{Options.Iterations}, {i + 1} trials");
                    }
                    else
                    {
                        Console.WriteLine("No deadends, not counting instance");
                        NoDeadends.Add(i);
                    }
                }
                else
                {
                    ED.Add(res);
                    Console.WriteLine($"Success #{ED.Count}/{Options.Iterations}, {i + 1} trials");
                }
            }

            foreach (var obj in ED)
            {
                totalTime += obj.Time;
            }

            ExecutionData.Add(Tuple.Create(ED, TimeSpan.FromTicks(totalTime.Ticks / Math.Max(1, ED.Count)), InaccuracyHandlingStrategy, setting.ToString()));
            
            if (ED.Count > 0)
            {

                ExcelHelper.writeSummary(folderPath, ED, TimeSpan.FromTicks(totalTime.Ticks / ED.Count), setting.ToString());
            }
        }
        ExcelHelper.WriteToExcel(folderPath, genPath, ExecutionData, seedSet, settings, op);
    }

    

    private static void SetSettings(string sName, List<Tuple<Options.InaccuracyHandlingStrategies, bool, int, bool, int>> settings)
    {
        if (sName.StartsWith("wumpus")) // && InaccuracyHandlingStrategy != InaccuracyHandlingStrategies.MakeTrue
            Options.SDR_OBS = true;

        if (!Options.OverspecifiedPreconditions)
        {
            if (Options.PredicateInaccuracy != PredicateInaccuracies.Both)
            {
                
                if (Options.PredicateInaccuracy == PredicateInaccuracies.FalseNegative)
                {
                    //settings.Add(Tuple.Create(InaccuracyHandlingStrategies.LazyOptimistic, true, 1, false, 0));

                    settings.Add(Tuple.Create(InaccuracyHandlingStrategies.LazyPessimistic, true, 1, false, 0));
                    settings.Add(Tuple.Create(InaccuracyHandlingStrategies.MakeTrue, true, 0, false, 1));
                    settings.Add(Tuple.Create(InaccuracyHandlingStrategies.MakeTrue, true, 0, false, 5));
                    settings.Add(Tuple.Create(InaccuracyHandlingStrategies.MakeTrue, true, 0, false, 20));

                }
                else
                    settings.Add(Tuple.Create(InaccuracyHandlingStrategies.Lazy, true, 1, false, 0));

                //these are after so that in the FN case we only consider relevant examples
                settings.Add(Tuple.Create(InaccuracyHandlingStrategies.BLOptimistic, true, 1, false, 0));
                settings.Add(Tuple.Create(InaccuracyHandlingStrategies.BLPessimistic, true, 1, false, 0));

            }
            else
            {
                settings.Add(Tuple.Create(InaccuracyHandlingStrategies.BLOptimistic, true, 1, false, 0));
                settings.Add(Tuple.Create(InaccuracyHandlingStrategies.LazyPessimistic, true, 1, true, 0));
                settings.Add(Tuple.Create(InaccuracyHandlingStrategies.MakeTrue, true, 1, false, 1));
                settings.Add(Tuple.Create(InaccuracyHandlingStrategies.MakeTrue, true, 1, false, 20));

            }
        }
        else if (Options.PredicateInaccuracy == PredicateInaccuracies.Neither)
        {
            settings.Add(Tuple.Create(InaccuracyHandlingStrategies.MakeTrue, false, 0, false, 0));
            settings.Add(Tuple.Create(InaccuracyHandlingStrategies.MakeTrue, true, 5, false, 0));
        }
        else
        {
            Options.SolveBothSequentially = false;
            //settings.Add(Tuple.Create(InaccuracyHandlingStrategies.MakeTrue, false, 0, false, 0.0));

            //settings.Add(Tuple.Create(InaccuracyHandlingStrategies.MakeTrue, true, 1, false, 1));
            //settings.Add(Tuple.Create(InaccuracyHandlingStrategies.MakeTrue, true, 20, false, 20));
            //settings.Add(Tuple.Create(InaccuracyHandlingStrategies.MakeTrue, true, 1, false, 20));
            //settings.Add(Tuple.Create(InaccuracyHandlingStrategies.MakeTrue, true, 20, false, 1));

            Options.SolveBothSequentially = true;
            settings.Add(Tuple.Create(InaccuracyHandlingStrategies.LazyOptimistic, false, 0, false, 0));
            //settings.Add(Tuple.Create(InaccuracyHandlingStrategies.LazyOptimistic, true, 1, false, 0));
            settings.Add(Tuple.Create(InaccuracyHandlingStrategies.LazyPessimistic, false, 0, false, 0));
            settings.Add(Tuple.Create(InaccuracyHandlingStrategies.LazyPessimistic, true, 1, false, 0));
            //settings.Add(Tuple.Create(InaccuracyHandlingStrategies.BLOptimistic, true, 5, false, 0.0));
            //settings.Add(Tuple.Create(InaccuracyHandlingStrategies.BLOptimistic, true, 20, false, 0.0));

        }
    }

    static async Task<ExecutionData> StartPlanner(string sDomainFile, string sProblemFile, string sOutputFile, bool bOnline, bool bValidate, CancellationToken cancellationToken)
    {
#pragma warning disable CS8603 // Possible null reference return.
        return await Task.Run(() =>
        {
            try
            {
                return Run.RunPlanner(sDomainFile, sProblemFile, sOutputFile, bOnline, false, cancellationToken);
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


        for (int i = curr_i; i < Options.MaxIterations; i++)
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
        FFUtilities.Verbose = false;
        Options.Verbose = true;
        //Options.PredicateInaccuracy = Options.PredicateInaccuracies.Both;
        //Options.OverspecifiedPreconditions = true;
        //Options.Iterations = 2;
        //Options.MaxIterations = 5;
        //for FP/FN usecases

        //Options.threshold = 0;

        gcmd_line.display_info = 0;
        gcmd_line.debug = 0;
        string sPath = @"C:\Users\travkaie\OneDrive - Intel Corporation\Documents\School\up-cpor\Tests\";
        //Options.SDR_OBS = true;
        //RunTest("localize", bOnline, sPath);
        //RunTest("colorballs4-4", bOnline, sPath);
        //RunTest("colorballs5-1", bOnline, sPath);
        //RunTest("colorballs8-4", bOnline, sPath);
        

        RunTest("unix1", bOnline, sPath);
        //RunTest("unix3", bOnline, sPath);
        //RunTest("doors5", bOnline, sPath, true);
        //RunTest("blocks7", bOnline, sPath);
        //RunTest("cloghuge", bOnline, sPath);
        RunTest("wumpus10", bOnline, sPath);
        //RunTest("wumpus15", bOnline, sPath);
        RunTest("doors11", bOnline, sPath);
        //RunTest("doors13", bOnline, sPath);
        //RunTest("doors15", bOnline, sPath);
        //RunTest("colorballs2-2", bOnline, sPath);
        
        //RunTest("colorballs9-3", bOnline, sPath);
        //RunTest("colorballs11-1", bOnline, sPath);
        //RunTest("colorballs11-2", bOnline, sPath);
        //RunTest("blocks7", bOnline, sPath);





        //RunTest("localize5", bOnline);
        //RunTest("localize5knoisy", bOnline);
        //RunTest("medpks010", bOnline);

    }

    public static void Main(string[] args)
    {
        //TestAll(true);
        //return;

        FFUtilities.Verbose = false;

        Iterations = 50;
        canOverride = false;
        Options.OverspecifiedPreconditions = false;
        Options.PredicateInaccuracy = Options.PredicateInaccuracies.Neither;
        if (args.Length < 1)
        {
            Console.WriteLine("Usage: RunPlanner domain_file max_time fp/fn");
        }
        else
        {

            if (args.Length > 1)
            {
                string literalInaccuacies = args[1];
                if (literalInaccuacies == "fp")
                    Options.PredicateInaccuracy = Options.PredicateInaccuracies.FalsePositive;
                else if (literalInaccuacies == "fn")
                    Options.PredicateInaccuracy = Options.PredicateInaccuracies.FalseNegative;
                else if (literalInaccuacies == "fb")
                    Options.PredicateInaccuracy = Options.PredicateInaccuracies.Both;
                else if (literalInaccuacies == "op")
                {
                    Options.PredicateInaccuracy = Options.PredicateInaccuracies.Neither;
                    Options.OverspecifiedPreconditions = true;
                }
                else if (literalInaccuacies == "all")
                {
                    Options.PredicateInaccuracy = Options.PredicateInaccuracies.Both;
                    Options.OverspecifiedPreconditions = true;
                }
                else
                    Console.WriteLine($"Usage: got weird inaccuracy: {literalInaccuacies}");
            }
            if (args.Length > 2)
            {
                 int.TryParse(args[2], out Options.MaxTime);
            }
            if (args.Length > 3)
            {
                double.TryParse(args[3], out Options.threshold);
            }
            bool op = Options.OverspecifiedPreconditions && Options.PredicateInaccuracy == PredicateInaccuracies.Neither;
            _ = RunTest(args[0], true, @"Tests/", op);
            if (Options.PredicateInaccuracy == Options.PredicateInaccuracies.FalseNegative)
                _ = RunTest(args[0], true, @"Tests/", true);
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