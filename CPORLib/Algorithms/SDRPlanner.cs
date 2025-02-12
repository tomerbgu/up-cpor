using CPORLib.FFCS;
using CPORLib.LogicalUtilities;
using CPORLib.Parsing;
using CPORLib.PlanningModel;
using CPORLib.Tools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using static CPORLib.Tools.Options;
using static CPORLib.Algorithms.PlannerBase;
using Action = CPORLib.PlanningModel.PlanningAction;
using static System.Collections.Specialized.BitVector32;

namespace CPORLib.Algorithms
{

    public class SDRPlanner : PlannerBase
    {
        private PartiallySpecifiedState CurrentState;
        private List<string> FutureActions;
        private int NextActionIndex;
        private bool ExpectingObservation;
        public string Error { get; private set; }
        public bool GoalReached { get { return CurrentState.IsGoalState(); } }
        private bool CheckGoalReached = true;
        public HashSet<Predicate> PredicatesToNegate;
        public SDRPlanner(Domain domain, Problem problem) : base(domain, problem)    
        {
            
            Options.ComputeCompletePlanTree = false;
            //Options.AddAllKnownToGiven = true; //this is needed in, e.g., medpks010

            Problem = new Problem(problem); //modify this back to Problem fakeProb
            Domain = Problem.Domain;


            //this is part of experiment setup - overspecify preconditions in Planner
            if (Options.OverspecifiedPreconditions)
            {
                OverSpecifyPreconds();
            }
            else
            {
                Domain.resetPreconditions();
            }
            //end part

            //this is part of our experiment setup for negating initial literals
            PredicatesToNegate = new HashSet<Predicate>();
            if (PredicatesToNegate.Count == 0)
            {
                foreach (Predicate p in problem.Known)
                {
                    if (p.Negation == Options.FalsePositive && domain.Uncertainties.Select(obj => obj.Name).Contains(p.Name))
                    {
                        if (RandomGenerator.NextDouble() < Options.threshold)
                        {
                            PredicatesToNegate.Add(p);
                        }
                    }
                }
            }
            
            ExecutionData.NumberOfNegations = PredicatesToNegate.Count;
            Console.WriteLine($"Num of negations: {ExecutionData.NumberOfNegations}");
            foreach (Predicate p in PredicatesToNegate)
            {
                //Console.WriteLine("negated this: " + p.ToString());
                Problem.Known.Remove(p);
                Problem.AddKnown(p.Negate());
            }
            //end part


            BeliefState bsInitial = Problem.GetInitialBelief();
            bool removeInaccuracies = (InaccuracyHandlingStrategy == InaccuracyHandlingStrategies.BL0 || InaccuracyHandlingStrategy == InaccuracyHandlingStrategies.BLOptimistic);
            if (removeInaccuracies)
                bsInitial.ModifyProblemBeforeStateSelection(CurrentState);
            CurrentState = bsInitial.GetPartiallySpecifiedState();
            FutureActions = null;
            NextActionIndex= 0;
            ExpectingObservation = false;

            //this is  to make sure that if part of initial literals are incorrect, we still make sure we are in goal state to succeed. Not experimental
            
            //if (Problem.Goal is CompoundFormula cff) //only relevant for compound formulas?
            //{
            ISet<Predicate> GoalPredicates = Problem.Goal.GetAllPredicates();
            CompoundFormula cf;

            foreach (Predicate curr in GoalPredicates.Where(x => ((GroundedPredicate)x).Constants.Count()>0))
            {
                if (CurrentState.Observed.Contains(curr))
                {
                    CurrentState.m_bsInitialBelief.Observed.Remove(curr);
                    CurrentState.Observed.Remove(curr);
                    CurrentState.Hidden.Add(curr);

                    //break; //dont need to remove all to make it think something is wrong - unless goal is an or-formula

                    //todo should this be outside the if
                    cf = new CompoundFormula("or");
                    cf.SimpleAddOperand(curr);
                    cf.SimpleAddOperand(curr.Negate());
                    Problem.AddHidden(cf);
                    CurrentState.m_bsInitialBelief.Hidden.Add(cf);
                }
                
            }
            //}

        }

        private void OverSpecifyPreconds()
        {
            List<Predicate> fakePredicates = Domain.OverspecifyPreconditions();
            HashSet<Predicate> lGrounded = new HashSet<Predicate>();
            foreach (ParametrizedPredicate p in fakePredicates)
            {
                Domain.GroundPredicate(p, new Dictionary<Parameter, Constant>(),
                    new List<Argument>(p.Parameters), lGrounded);
            }
            foreach (Predicate p in lGrounded)
            {
                if (RandomGenerator.NextDouble() < Options.fakePredicateThreshold)
                    Problem.AddKnown(p); //adding that sometimes the predicate is true
            }
        }

        public string GetAction()
        {
            Error = "";
            if (ExpectingObservation)
            {
                Error = "Expecting the observation received from the last action before computing a new action.";
                return null;
            }
            if (GoalReached)
            {
                //if (CheckGoalReached)
                //{
                //    CheckGoalReached = false;
                //    ISet<Predicate> GoalPredicates = Problem.Goal.GetAllPredicates();
                //    CompoundFormula cf;

                //    foreach (Predicate curr in GoalPredicates)
                //    {
                //        if (CurrentState.Observed.Contains(curr))
                //        {
                //            CurrentState.m_bsInitialBelief.Observed.Remove(curr);
                //            CurrentState.Observed.Remove(curr);
                //            CurrentState.Hidden.Add(curr);

                //            //break; //dont need to remove all to make it think something is wrong - unless goal is an or-formula
                //        }
                //        cf = new CompoundFormula("or");
                //        cf.SimpleAddOperand(curr);
                //        cf.SimpleAddOperand(curr.Negate());
                //        CurrentState.m_bsInitialBelief.AddHidden(cf);

                //        //HashSet<int> hsModified = CurrentState.m_bsInitialBelief.ReviseInitialBelief(cf, CurrentState, true);
                //        //if (hsModified.Count > 0)
                //        //{
                //        //    if (!Options.OptimizeMemoryConsumption)
                //        //        CurrentState.PropogateObservedPredicates();
                //        //}
                //    }
                    
                    



                //    //ISet<Predicate> GoalPredicates = Problem.Goal.GetAllPredicates();
                //    //foreach (Predicate curr in GoalPredicates)
                //    //{
                //    //    if (CurrentState.Observed.Contains(curr))
                //    //    {
                //    //        CurrentState.m_bsInitialBelief.Observed.Remove(curr);
                //    //        CurrentState.Observed.Remove(curr);
                //    //        CurrentState.Hidden.Add(curr);

                //    //        //break; //dont need to remove all to make it think something is wrong - unless goal is an or-formula
                //    //    }
                //    //}

                //    //CompoundFormula cf = new CompoundFormula("or");
                //    //cf.SimpleAddOperand(Problem.Goal);
                //    //cf.SimpleAddOperand(Problem.Goal.Negate());
                //    //CurrentState.m_bsInitialBelief.Hidden.Add(cf);

                //    ////CurrentState.m_bsInitialBelief.Unknown.Add(Problem.pGoal.Canonical());
                //    //if (Problem.pGoal != null)
                //    //    CurrentState.m_bsInitialBelief.Unknown.Add(Problem.pGoal.Canonical());
                //    //else
                //    //{
                //    //    //CompoundFormula cfg = ((CompoundFormula)(Problem.Goal)).RemoveNestedConjunction(out bool changed);
                //    //    foreach (Predicate p in GoalPredicates)
                //    //        CurrentState.m_bsInitialBelief.Unknown.Add(p.Canonical());
                //    //}
                //}
                //else
                //{
                    Error = "Goal already reached, no additional actions should be executed.";
                    return null;
                //}

            }
            bool bPreconditionFailure = false;
            
            if (FutureActions != null && NextActionIndex < FutureActions.Count)
            {
                if (SDR_OBS)
                {
                    List<Action> lObservationActions = Domain.GroundAllObservationActions(CurrentState.Observed, true);
                    int counter = 0;
                    foreach (Action a in lObservationActions)
                    {
                        Predicate pObserve = ((PredicateFormula)a.Observe).Predicate;
                        if (!CurrentState.Observed.Contains(pObserve) && !CurrentState.Observed.Contains(pObserve.Negate()) && !FutureActions.Contains(a.Name))
                        {
                            FutureActions.Insert(NextActionIndex, a.Name);
                            if (Options.Verbose)
                                Console.WriteLine("Inserted " + a.Name + " to plan");
                            counter++;
                        }
                    }
                    //clear the rest of the future actions to force replan after observations
                    while (NextActionIndex + counter < FutureActions.Count)
                    {
                        string s = FutureActions[NextActionIndex + counter];
                        if (Domain.GetActionByName(s).Observe == null)
                            break;
                        counter++;
                    }
                    if (counter > 0)
                    {
                        int i = NextActionIndex + counter;
                        FutureActions.RemoveRange(i, FutureActions.Count - i);
                    }
                }
                string sAction = FutureActions[NextActionIndex];
                bool bPreconditionsHold = CurrentState.IsApplicable(sAction);
                //Console.WriteLine("SDR: Checking applicability of action " + sAction +" : " + bPreconditionsHold);
                if (bPreconditionsHold)
                    return sAction;
                else
                    bPreconditionFailure = true;
            }

            List<string> lPlan = Plan(CurrentState, bPreconditionFailure, out bool bDeadEndReached, out State sChosen);
            
            if (lPlan == null || lPlan.Count ==0)
            {
                Error = "Could not plan for the current state";
                return GetAction();
                //return null;
            }
            else
                CheckGoalReached = true; //TODO maybe irrelevant
            FutureActions = lPlan;
            NextActionIndex = 0;
            return GetAction();
        }

        public bool SetObservation(string sObservation)
        {
            Error = "";
            if (GoalReached)
            {
                Error = "Goal already reached, no additional actions should be executed.";
                return false;
            }
            string sAction = FutureActions[NextActionIndex];
            string sRevisedActionName = sAction.Replace(Utilities.DELIMITER_CHAR, " ");
            string[] aName = Utilities.SplitString(sRevisedActionName, ' ');
            Action a = Domain.GroundActionByName(aName);
            if (a.Observe!=null)
            {
                ExecutionData.SensingActions++;
            }
            else if (a.Effects != null)
            {
                ExecutionData.EffectActions++;
            }
            ExecutionData.steps++;
            if (a.Observe == null && sObservation != null)
            {
                Error = "Action was not a sensing action, null observation expected.";
            }
            if (a.Observe != null && sObservation == null)
            {
                Error = "Sensing action executed, expecting an observation.";
                return false;
            }
            PartiallySpecifiedState psNext = CurrentState.Apply(a, sObservation);
            if (psNext == null)
            {
                Error = "Failed to apply the action at the current state.";
                return false;
            }
            CurrentState = psNext;
            NextActionIndex++;
            if (NextActionIndex == FutureActions.Count || sObservation != null)//(sObservation != null && sObservation != "Fail")) TODO
            {
                FutureActions = null;
                NextActionIndex = -1;
            }
            ExpectingObservation = false;
            return true;
        }

        public bool OnlineReplanning()
        {
            Console.WriteLine("Started online replanning for " + Domain.Name + ", " + DateTime.Now);


            bool bSampleDeadendState = Options.SampleDeadendState;

            BeliefState bsInitial = Problem.GetInitialBelief();
            bsInitial.UnderlyingEnvironmentState = null;
            State s = null;

            if (Problem.DeadEndList.Count == 0)
                bSampleDeadendState = false;

            s = bsInitial.ChooseState(true, bSampleDeadendState);

            ExecutionData.FinalStateDeadend = false;
            ExecutionData.InitialStateDeadend = false;

            foreach (Formula f in Problem.DeadEndList)
            {
                if (f.IsTrue(s.Predicates, false))
                    ExecutionData.InitialStateDeadend = true;
            }



            bool bPreconditionFailure = false;

            PartiallySpecifiedState pssCurrent = bsInitial.GetPartiallySpecifiedState(), pssNext = null;
            List<State> lTrueStates = new List<State>();
            lTrueStates.Add(pssCurrent.UnderlyingEnvironmentState);
            List<string> lActions = new List<string>();

            List<List<string>> lExecutedPlans = new List<List<string>>();
            List<State> lChosen = new List<State>();

            TimeSpan tsTime;
            Formula fObserved = null;
            int cActions = 0;
            int cPlanning = 0;
            int cObservations = 0;

            bool bPlanEndedSuccessfully = false, bGoalReached = false, bDeadEndReached = false;
            DateTime dtStart = DateTime.Now;
            while (!bGoalReached && !bDeadEndReached)
            {
                State sChosen = null;

                if (StuckInLoopPlanBased(cActions, pssCurrent, lExecutedPlans))
                    throw new Exception("SDR is stuck in a loop");
                


                List<string> lPlan = Plan(pssCurrent, bPreconditionFailure, out bDeadEndReached, out sChosen);
                if (lPlan == null)
                    throw new Exception("Could not plan for the current state");

                
                cPlanning++;
                lChosen.Add(sChosen);
                bPlanEndedSuccessfully = true;

                lExecutedPlans.Add(new List<string>());

                if (lPlan != null)
                {
                    foreach (string sAction in lPlan)
                    {

                        if (!IsReasoningAction(sAction.ToLower()))
                        {
                            Debug.Write("\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b" +
                                Domain.Name + ": " + cActions + "/" + lPlan.Count + ", memory:" + Process.GetCurrentProcess().PrivateMemorySize64 / 1000000 + "MB        ");
                            Debug.WriteLine("");
                            TimeSpan ts = DateTime.Now - dtStart;
                            if (ts.TotalMinutes > 60)
                                throw new Exception("Execution taking too long");
                            Debug.WriteLine((int)(ts.TotalMinutes) + "," + cActions + ") " + Domain.Name + ", executing action " + sAction);
                            lExecutedPlans.Last().Add(sAction);
                            DateTime dtBefore = DateTime.Now;

                            pssNext = pssCurrent.Apply(sAction, out fObserved);

                            List<Formula> lDeadends = null;
                            if (pssNext != null)
                            {
                                Console.WriteLine("Executed " + sAction/* + ", received " + sObservation*/);
                                bPreconditionFailure = false;
                                DeadEndExistence isDeadEnd = pssNext.IsDeadEndExistenceAll(out lDeadends);
                                if (isDeadEnd == DeadEndExistence.DeadEndTrue)
                                {
                                    pssCurrent = pssNext;
                                    bDeadEndReached = true;
                                    bPlanEndedSuccessfully = true;
                                    break;

                                }
                            }
                            if (pssNext == null )
                            {
                                bPlanEndedSuccessfully = false;
                                Console.WriteLine(Domain.Name + ", cannot execute " + sAction + " FAIL");

                                
                                bPreconditionFailure = true;
                                break;
                            }
                            else
                            {
                                lTrueStates.Add(pssNext.UnderlyingEnvironmentState);

                                if (pssNext != null)
                                {
                                    lActions.Add(sAction);

                                    if (SDR_OBS)
                                    {
                                        List<Action> lObservationActions = bsInitial.Problem.Domain.GroundAllObservationActions(pssNext.Observed, true);
                                        foreach (Action a in lObservationActions)
                                        {
                                            Predicate pObserve = ((PredicateFormula)a.Observe).Predicate;
                                            if (!pssNext.Observed.Contains(pObserve) && !pssNext.Observed.Contains(pObserve.Negate()))
                                            {
                                                pssNext = pssNext.Apply(a, out fObserved);
                                                lActions.Add(a.Name);
                                                cObservations++;
                                                Console.WriteLine("Executed " + sAction +  ", observed " + fObserved);
                                                cActions++;
                                            }
                                        }
                                    }
                                    cActions++;
                                    pssCurrent = pssNext;
                                    if (fObserved != null)
                                    {
                                        cObservations++;
                                        Console.WriteLine("Executed " + sAction + ", observed " + fObserved);

                                    }
                                }
                                else
                                {
                                    //Debug.WriteLine("Skipping inapplicable KW action...");
                                }
                            }
                        }
                    }
                }

                if (bDeadEndReached)
                {
                    DateTime dtBefore = DateTime.Now;
                    Debug.WriteLine("Dead End time: " + (DateTime.Now - dtBefore).TotalSeconds);
                    ExecutionData.FinalStateDeadend = true;
                }
                if (bPlanEndedSuccessfully)
                {
                    DateTime dtBefore = DateTime.Now;
                    bGoalReached = pssCurrent.IsGoalState();
                    Debug.WriteLine("Goal time: " + (DateTime.Now - dtBefore).TotalSeconds);
                }
                else
                {
                    GroundedPredicate gpDead = new GroundedPredicate("dead");
                    if (pssCurrent.Observed.Contains(gpDead))
                        bGoalReached = true;

                }
            }
            bool bValid = pssCurrent.IsGoalState();
            if (!bValid)
                bValid = pssCurrent.IsDeadEndState() == Options.DeadEndExistence.DeadEndTrue;


            tsTime = DateTime.Now - dtStart;

            int cHistory = 0;
            PartiallySpecifiedState pssIt = pssCurrent;
            while (pssIt != null)
            {
                pssIt = pssIt.Predecessor;
                cHistory++;
            }
            cActions = cHistory;

            ExecutionData.Actions = cActions;
            ExecutionData.Planning = cPlanning;
            ExecutionData.SensingActions = cObservations;
            ExecutionData.Time = DateTime.Now - dtStart;

            Console.WriteLine("Actions: " + cHistory);
            Console.WriteLine("Average time - " + tsTime.TotalSeconds * 1.0 / cActions);
            Console.WriteLine("Total time - " + tsTime.TotalSeconds);
            Console.WriteLine("*******************************************************************************");

            return bValid;
        }


        

    }
}
