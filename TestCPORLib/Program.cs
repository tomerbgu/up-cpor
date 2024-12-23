using CPORLib;
using CPORLib.FFCS;
using CPORLib.Tools;
using System;
using System.IO;

public class Program
{
    public static void RunTest(string sName, bool bOnline)
    {
        string sPath = @"C:\Users\travkaie\OneDrive - Intel Corporation\Documents\School\up-cpor\Tests\" + sName;
        //string sPath = @"Tests/" + sName;
        //string sPath = @"C:\Users\Guy\OneDrive - Ben Gurion University of the Negev\Research\projects\AIPlan4EU\up-cpor\Tests\" + sName;
        string sDomainFile = Path.Combine(sPath, "d.pddl");
        string sProblemFile = Path.Combine(sPath, "p.pddl");
        string sNegateFile = Path.Combine(sPath, "n.pddl");
        string sOutputFile = Path.Combine(sPath, "out.txt");
        Run.DomainInfo(sDomainFile, sProblemFile);
        Run.RunPlanner(sDomainFile
            , sProblemFile,
            sNegateFile,
            sOutputFile,
            bOnline, false);
    }
    public static void TestAll(bool bOnline)
    {
        //Options.Iterations = 1;
        Options.threshold = 0;
        FFUtilities.Verbose = false;
        Options.Verbose = true;
        Options.FalsePositive = false;
        gcmd_line.display_info = 0;
        gcmd_line.debug = 0;

        //RunTest("unix1", bOnline);
        //RunTest("unix2", bOnline);
        //RunTest("unix3", bOnline);

        RunTest("doors5", bOnline);
        //RunTest("doors7", bOnline);
        //RunTest("doors9", bOnline);
        //RunTest("doors15", bOnline);
        //RunTest("doors13", bOnline);
        //RunTest("wumpus05", bOnline);
        //RunTest("colorballs2-2", bOnline);
        //RunTest("blocks3", bOnline);
        //RunTest("clog5", bOnline);

        //RunTest("doors15FP", bOnline); //.3 might not be enough
        //RunTest("wumpus10_test", bOnline); //nice
        //RunTest("unix3", bOnline);
        //RunTest("unix4", bOnline);
        //RunTest("clog5", bOnline);//weirdDeadEnd
        //RunTest("wumpus15clean", bOnline);
        //RunTest("wumpus15FP", bOnline);//returns null
        //RunTest("colorballs11-2", bOnline);
        //RunTest("colorballs2-2", bOnline);


        //RunTest("unix1", bOnline);
        //RunTest("wumpus10", bOnline);
        //RunTest("doors5", bOnline);
        //RunTest("doors11", bOnline);
        //RunTest("doors13", bOnline);
        //RunTest("doors15", bOnline);

        //RunTest("blocks3", bOnline);
        //RunTest("unix3", bOnline);


        //RunTest("wumpus5", bOnline);


        //RunTest("localize5", bOnline);
        //RunTest("localize5knoisy", bOnline);
        //RunTest("medpks010", bOnline);

    }

    public static void Main(string[] args)
    {
        FFUtilities.Verbose = false;
        TestAll(true);
        return;

        if (args.Length < 1)
        {
            Console.WriteLine("Usage: RunPlanner domain_file problem_file [false_pos] [new] (verbose - optional)");
        }
        else
        {
            Console.WriteLine(args[0]);
            RunTest(args[0], true);
            return;
            string sDomainFile = args[0];
            string sProblemFile = args[1];
            string sNegateFile = args[2];
            if (args.Length > 3)
            {
                
                try
                {
                    Options.Iterations = int.Parse(args[3]);
                }
                catch { }
                try
                {
                    Options.threshold = double.Parse(args[4]);
                }
                catch { }
                try
                {
                    Options.FalsePositive = args[5] == "true";
                }
                catch { }
            }
            Console.WriteLine("Iterations: " + Options.Iterations);
            Console.WriteLine("Threshold: " + Options.threshold);
            //Console.WriteLine("False Positives: " + Options.FalsePositive);
            //string sNegateFile = null;
            //Console.WriteLine("Output Path: " + sOutputFile);
            bool bOnline = true;
            //if (args.Length > 3)
            //{
            //    if (args[2] == "false_pos")
            //        Options.FalsePositive = true;
            //    if (args[3] == "new")
            //    {
            //        if (Options.FalsePositive)
            //            Options.InaccuracyHandlingStrategy = Options.InaccuracyHandlingStrategies.MakeTrue;
            //        else
            //            Options.InaccuracyHandlingStrategy = Options.InaccuracyHandlingStrategies.FailHandler;
            //    }

            //}
            Run.RunPlanner(sDomainFile
                , sProblemFile,
                sNegateFile,
                "",
                bOnline);
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