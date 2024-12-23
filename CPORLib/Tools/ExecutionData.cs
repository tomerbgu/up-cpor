
using CPORLib.PlanningModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace CPORLib.Tools
{
    public class ExecutionData
    {
        public bool FinalStateDeadend { get; set; }
        public bool InitialStateDeadend { get; set; }

        public int SensingActions { get; set; }
        public int EffectActions { get; set; }
        public int Actions { get; set; }
        public List<int> MakeActions { get; set; }
        public List<int> planLength { get; set; }
        public List<int> lFilteredActions { get; set; }
        public List<int> stepsToReplan { get; set; }
        public int steps;
        public int Planning { get; set; }
        public int DeadendCount { get; set; }
        public int ReplanningCount { get; set; }
        public int ReplanningIncludeModCount { get; set; }
        public int FailCount { get; set; }
        public int NumberOfNegations { get; set; }
        public TimeSpan Time { get; set; }
        public Domain Domain { get; set; }
        public Problem Problem { get; set; }

        public bool SampleDeadendState { get; set; }
        public string Path { get; set; }
        public string DeadEndFile { get; set; }
        public string Exception { get; set; }
        public bool Failure { get { return Exception != ""; } }



        public Options.DeadendStrategies DeadendStrategy { get; set; }

        public ExecutionData(string sPath, string sDeadEndFile, Domain d, Problem p, Options.DeadendStrategies ds)
        {
            Domain = d;
            Problem = p;
            Path = sPath;
            DeadEndFile = sDeadEndFile;
            Exception = "";
            DeadendStrategy = ds;
            SampleDeadendState = false;
            ReplanningIncludeModCount = 0;
            ReplanningCount = 0;
            DeadendCount = 0;
            FailCount = 0;
            MakeActions = new List<int>();
            planLength = new List<int>();
            lFilteredActions = new List<int>();
            stepsToReplan = new List<int>();
        }

        public ExecutionData(Domain d, Problem p)
        {
            Domain = d;
            Problem = p;
            Path = "";
            DeadEndFile = "";
            Exception = "";
            SampleDeadendState = false;
            ReplanningIncludeModCount = 0;
            ReplanningCount = 0;
            DeadendCount = 0;
            FailCount = 0;
            MakeActions = new List<int>();
            planLength = new List<int>();
            lFilteredActions = new List<int>();
            stepsToReplan = new List<int>();
        }
    }
}
