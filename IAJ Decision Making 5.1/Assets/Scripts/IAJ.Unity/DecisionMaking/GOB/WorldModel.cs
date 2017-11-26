using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.IAJ.Unity.DecisionMaking.GOB
{
    public class WorldModel
    {
        private object[] Properties { get; set; }
        //private Dictionary<string, object> Properties { get; set; }
        private List<Action> Actions { get; set; }
        protected IEnumerator<Action> ActionEnumerator { get; set; }

        private float[] GoalValues;
        //private Dictionary<string, float> GoalValues { get; set; } 

        protected WorldModel Parent { get; set; }

        public WorldModel(List<Action> actions)
        {
            
            //this.Properties = new Dictionary<string, object>();
            this.Properties = new object[23];
            //this.GoalValues = new Dictionary<string, float>();
            this.GoalValues = new float[4];
            this.Actions = actions;
            this.ActionEnumerator = actions.GetEnumerator();
        }

        public WorldModel(WorldModel parent)
        {
            //this.Properties = new Dictionary<string, object>();
            this.Properties = new object[23];
            //this.GoalValues = new Dictionary<string, float>();
            this.GoalValues = new float[4];
            this.Actions = parent.Actions;
            this.Parent = parent;
            this.ActionEnumerator = this.Actions.GetEnumerator();
        }

        public int parseProperty(string propertyName)
        {
            switch (propertyName)
            {
                case "Mana":
                    return 0;
                case "HP":
                    return 1;
                case "MAXHP":
                    return 2;
                case "MAXMANA":
                    return 3;
                case "XP":
                    return 4;
                case "Time":
                    return 5;
                case "Money":
                    return 6;
                case "Level":
                    return 7;
                case "Position":
                    return 8;
                case "Chest1":
                    return 9;
                case "Chest2":
                    return 10;
                case "Chest3":
                    return 11;
                case "Chest4":
                    return 12;
                case "Chest5":
                    return 13;
                case "Orc1":
                    return 14;
                case "Orc2":
                    return 15;
                case "Skeleton1":
                    return 16;
                case "Skeleton2":
                    return 17;
                case "Dragon":
                    return 18;
                case "HealthPotion1":
                    return 19;
                case "HealthPotion2":
                    return 20;
                case "ManaPotion1":
                    return 21;
                case "ManaPotion2":
                    return 22;
                default:
                    break;
            }
            return -1;
        }

        public int parseGoalValue(string goalName)
        {
            switch (goalName)
            {
                case "Survive":
                    return 0;
                case "GainXP":
                    return 1;
                case "BeQuick":
                    return 2;
                case "GetRich":
                    return 3;
                default:
                    break;
            }
            return -1;
        }

        public virtual object GetProperty(string propertyName)
        {
            //recursive implementation of WorldModel
            object value = this.Properties[parseProperty(propertyName)];
            if (value != null)
            {
                return value;
            }
            else if (this.Parent != null)
            {
                return this.Parent.GetProperty(propertyName);
            }
            return null;

            //if (this.Properties.ContainsKey(propertyName))
            //{
            //    return this.Properties[propertyName];
            //}
            //else if (this.Parent != null)
            //{
            //    return this.Parent.GetProperty(propertyName);
            //}
            //else
            //{
            //    return null;
            //}
        }

        public virtual void SetProperty(string propertyName, object value)
        {
            this.Properties[parseProperty(propertyName)] = value;
            //this.Properties[propertyName] = value;
        }

        public virtual float GetGoalValue(string goalName)
        {
            //recursive implementation of WorldModel
            float value = this.GoalValues[parseGoalValue(goalName)];
            if (value != 0)
            {
                return value;
            }
            else if (this.Parent != null)
            {
                return this.Parent.GetGoalValue(goalName);
            }
            return 0;
            //if (this.GoalValues.ContainsKey(goalName))
            //{
            //    return this.GoalValues[goalName];
            //}
            //else if (this.Parent != null)
            //{
            //    return this.Parent.GetGoalValue(goalName);
            //}
            //else
            //{
            //    return 0;
            //}
        }

        public virtual void SetGoalValue(string goalName, float value)
        {
            var limitedValue = value;
            if (value > 10.0f)
            {
                limitedValue = 10.0f;
            }

            else if (value < 0.0f)
            {
                limitedValue = 0.0f;
            }

            this.GoalValues[parseGoalValue(goalName)] = limitedValue;
        }

        public virtual WorldModel GenerateChildWorldModel()
        {
            return new WorldModel(this);
        }

        public float CalculateDiscontentment(List<Goal> goals)
        {
            var discontentment = 0.0f;

            foreach (var goal in goals)
            {
                var newValue = this.GetGoalValue(goal.Name);

                discontentment += goal.GetDiscontentment(newValue);
            }

            return discontentment;
        }

        public virtual Action GetNextAction()
        {
            Action action = null;
            //returns the next action that can be executed or null if no more executable actions exist
            if (this.ActionEnumerator.MoveNext())
            {
                action = this.ActionEnumerator.Current;
            }

            while (action != null && !action.CanExecute(this))
            {
                if (this.ActionEnumerator.MoveNext())
                {
                    action = this.ActionEnumerator.Current;    
                }
                else
                {
                    action = null;
                }
            }

            return action;
        }

        public virtual Action[] GetExecutableActions()
        {
            return this.Actions.Where(a => a.CanExecute(this)).ToArray();
        }

        public virtual bool IsTerminal()
        {
            return true;
        }
        

        public virtual float GetScore()
        {
            return 0.0f;
        }

        public virtual int GetNextPlayer()
        {
            return 0;
        }

        public virtual void CalculateNextPlayer()
        {
        }
    }
}
