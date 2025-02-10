using CPORLib.Algorithms;
using CPORLib.FFCS;
using CPORLib.LogicalUtilities;
using CPORLib.Parsing;
using CPORLib.PlanningModel;
using CPORLib.Tools;
using Microsoft.SolverFoundation.Services;
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
using Domain = CPORLib.PlanningModel.Domain;

namespace CPORLib
{
    public class Run
    {
        public static CancellationToken cancellationFlag { get; private set; }
        public static void Main(string[] args)
        {
            //TestAll(true);
            //return;


            //if (args.Length < 3)
            //{
            //    Console.WriteLine("Usage: RunPlanner domain_file problem_file output_file [online/offline]");
            //}
            //else
            //{
            //    string sDomainFile = args[0];
            //    string sProblemFile = args[1];
            //    string sOutputFile = args[2];
            //    string sNegateFile = args[3];
            //    bool bOnline = false;
            //    if (args.Length > 4)
            //        bOnline = args[3] == "online";
            //    RunPlanner(sDomainFile
            //        , sProblemFile,
            //        sOutputFile,
            //        bOnline);
            //}
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



        public static ExecutionData RunPlanner(string sDomainFile, string sProblemFile, string sOutputFile, bool bOnline, HashSet<int> NoDeadends, bool bValidate, CancellationToken cancellationFlagInput)
        {
            cancellationFlag = cancellationFlagInput;
            Debug.WriteLine("Reading domain and problem");
            Parser parser = new Parser();
            Domain domain = parser.ParseDomain(sDomainFile);

            Debug.WriteLine("Done reading domain and problem");
            Problem problem = parser.ParseProblem(sProblemFile, domain);

            if (NoDeadends == null)
                NoDeadends = new HashSet<int>();

            if (bOnline)
            {    
                
                if (Options.UseFakePreds)
                {
                    domain.AddFakePredicates();
                }
                SDRPlanner sdr = new SDRPlanner(domain, problem);
                Simulator sim = new Simulator(domain, problem);
                DateTime dtStart = DateTime.Now;

                int idx = 0;

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
                Console.WriteLine("=========================================================================");
                DateTime dtEnd = DateTime.Now;
                sdr.ExecutionData.Time = dtEnd - dtStart;
                sdr.ExecutionData.Actions = idx;

                return sdr.ExecutionData;
            }
            else
            {
                //Options.SDR_OBS = true;
                CPORPlanner cpor = new CPORPlanner(domain, problem);
                cpor.InfoLevel = 1;
                ConditionalPlanTreeNode n = cpor.OfflinePlanning();
                cpor.WritePlan(sOutputFile, n);

                if (bValidate)
                    if (!cpor.ValidatePlanGraph(n))
                        Console.WriteLine("Invalid plan");
            }
            
            return null;
        }


    }
}
