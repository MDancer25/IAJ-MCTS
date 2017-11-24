using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Scripts.IAJ.Unity.DecisionMaking;
using Assets.Scripts.GameManager;
using Assets.Scripts.IAJ.Unity.DecisionMaking.GOB;
using Assets.Scripts.DecisionMakingActions;

namespace Assets.Scripts.IAJ.Unity.DecisionMaking.MCTS
{
    public class MCTSBiasedPlayout : MCTS
    {
        public MCTSBiasedPlayout(CurrentStateWorldModel currentStateWorldModel) : base(currentStateWorldModel)
        {
        }

        protected override Reward Playout(WorldModel initialPlayoutState)
        {
            GOB.Action action;
            GOB.Action[] actions;
            List<double> interval = new List<double>();
            double accumulate = 0;
            WorldModel current = initialPlayoutState;
            Reward reward = new Reward();
            double random;


            current = current.GenerateChildWorldModel();
            actions = current.GetExecutableActions();
            if (actions.Length == 0)
            {
                reward.Value = 0;
                reward.PlayerID = current.GetNextPlayer();
                return reward;
            }
            while (!current.IsTerminal())
            {
                accumulate = 0;
                interval.Clear();
                if (actions.Length == 0)
                    break;

                foreach (var a in actions)
                {
                    float h = 0;
                    //var child = current.GenerateChildWorldModel();
                    var gameMan = this.CurrentStateWorldModel.GetGameManager();
                    var character = gameMan.characterData;
                    WalkToTargetAndExecuteAction wa = a as WalkToTargetAndExecuteAction;
                    if(wa != null)
                    {
                        
                    }
                    if (a.Name.Contains("GetH"))
                    {
                        h = character.MaxHP - character.HP;                         //0-25
                    }
                    else if (a.Name.Contains("Pi") && character.Mana <= 5)           //5-25
                    {
                        h = character.Money + 5;
                    }
                    else if (a.Name.Contains("FireballS") && character.Mana > 0)    //0-25
                    {
                        h = character.HP - 5;
                    }else if (a.Name.Contains("FireballO") && character.Mana > 0)
                    {
                        h = character.HP - 10;
                    }
                    else if(a.Name.Contains("SwordAttackS"))
                    {
                        h = character.HP - 5;
                    }
                    else if (a.Name.Contains("SwordAttackO"))
                    {
                        h = character.HP - 10;
                    }
                    else if (a.Name.Contains("SwordAttackD"))
                    {
                        h = character.HP - 20;
                    }
                    //var h = Math.Pow(Math.E, character.BeQuickGoal.InsistenceValue * 2 + character.SurviveGoal.InsistenceValue * 2 + character.GainXPGoal.InsistenceValue * 1 + character.GetRichGoal.InsistenceValue * 3 + 1 / (float)current.GetProperty(Properties.TIME))
                    //var h = Math.Pow(Math.E, child.CalculateDiscontentment(character.Goals) *(float)current.GetProperty(Properties.TIME));
                    //var h = Math.Pow(Math.E,
                    //    CurrentStateWorldModel.GetGoalValue("BeQuick")
                    //    + CurrentStateWorldModel.GetGoalValue("Survive")
                    //    + CurrentStateWorldModel.GetGoalValue("GainXP")
                    //    + CurrentStateWorldModel.GetGoalValue("GetRich")
                    //    );


                    //var h = a.GetGoalChange(character.SurviveGoal)
                    //            + a.GetGoalChange(character.GainXPGoal)
                    //            + a.GetGoalChange(character.GetRichGoal)
                    //            + a.GetGoalChange(character.BeQuickGoal)
                    //    ;

                    //Debug.Log((float)current.GetProperty(Properties.TIME));
                    if (accumulate<h)
                    {
                        accumulate = h;
                    }
                    interval.Add(h);
                }
                //Debug.Log(accumulate);
                //Debug.Log(RandomGenerator.NextDouble());
                random = RandomGenerator.NextDouble() * accumulate;
                //interval.Sort();
                for (int j = 1; j < interval.Count; j++)
                {
                    //maybe it gets stuck here

                    if (random <= accumulate/2)
                    {
                        action = actions[interval.Count - j];
                        current = current.GenerateChildWorldModel();
                        action.ApplyActionEffects(current);
                        current.CalculateNextPlayer();
                        break;
                    }

                    if (j == interval.Count - 1)
                    {
                        current = current.GenerateChildWorldModel();
                        reward.Value = 0;
                        reward.PlayerID = current.GetNextPlayer();
                        return reward;
                    }
                }
            }

            reward.PlayerID = current.GetNextPlayer();
            reward.Value = current.GetScore();
            return reward;
        }
    }
}
