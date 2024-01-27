using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using CPORLib.FFCS;
using CPORLib.LogicalUtilities;
using CPORLib.Tools;

namespace CPORLib.PlanningModel
{
    public class Rule
    {
        private static int IDs = 0;

        public int ID { get; private set; }

        public string Name { get; set; }
        public Formula Constraints { get; set; } //maybe use diff constraints class

        public Rule(string sName)
        {
            Name = sName;
            //m_mMapConditionsChoices = new Dictionary<int, List<int>>();
            ID = IDs++;
            //NonDeterministicEffects = new HashSet<Predicate>();
        }

        public override string ToString()
        {
            string s = "(:rule " + Name + "\n";
            if (Constraints != null)
                s += " :constraints " + Constraints + "\n";
            s += ")";
            return s;
        }

        public virtual Rule Clone()
        {
            //PlanningAction aNew = new PlanningAction(Name);
            //if (Preconditions != null)
            //    aNew.Preconditions = Preconditions.Clone();
            //if (Effects != null)
            //    aNew.Effects = Effects.Clone();
            //if (Observe != null)
            //    aNew.Observe = Observe.Clone();
            //aNew.HasConditionalEffects = HasConditionalEffects;
            //aNew.ContainsNonDeterministicEffect = ContainsNonDeterministicEffect;
            //aNew.NonDeterministicEffects = new HashSet<Predicate>(NonDeterministicEffects);
            //aNew.Original = Original;
            //return aNew;
            Rule rNew = new Rule(Name);
            return rNew;
        }

        public void SetConstraints(Formula f)
        {
            CompoundFormula fRemovePFalse = new CompoundFormula("and");
            if (f is CompoundFormula cf)
            {
                if (cf.Operator == "and")
                {
                    foreach (Formula fSub in cf.Operands)
                    {
                        if (fSub is PredicateFormula pf)
                        {
                            Predicate p = pf.Predicate;
                            if (p != Utilities.FALSE_PREDICATE)
                            {
                                fRemovePFalse.AddOperand(p);
                            }
                        }
                        else
                        {
                            fRemovePFalse.AddOperand(fSub);
                        }
                    }
                }
                else
                    fRemovePFalse = cf; // this is in case of when, or - if there is a P_FALSE in the effect, we will still have a problem, but this does not occur in current benchmarks
            }
            else
            {
                Predicate p = ((PredicateFormula)f).Predicate;
                if (p != Utilities.FALSE_PREDICATE)
                {
                    fRemovePFalse.AddOperand(p);
                }
            }
            Constraints = fRemovePFalse;
        }

        public bool RuleHolds(Predicate toRemove, Predicate toAdd, BeliefState bs)
        {
            //need to figure out if the predicate is in the constraint. then decide what it effects
            //maybe better way is only to negate things that don't cause chain reaction bc if we start changing what it effects it can ruin the world-building
            return true;
        }
    }
}