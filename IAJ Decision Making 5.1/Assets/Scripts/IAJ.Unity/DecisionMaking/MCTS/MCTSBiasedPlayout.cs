using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Scripts.IAJ.Unity.DecisionMaking;
using Assets.Scripts.GameManager;
using Assets.Scripts.IAJ.Unity.DecisionMaking.GOB;
using Assets.Scripts.DecisionMakingActions;
using UnityEngine;

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
			Reward reward = new Reward();
			WorldModel current = initialPlayoutState;

            double random;
            float h = 0;
            double accumulate = 0;
            float euclidean = 0;
            List<double> interval = new List<double>();
            WalkToTargetAndExecuteAction wa;

			actions = current.GetExecutableActions();
			if (actions.Length == 0)
			{
				reward.PlayerID = current.GetNextPlayer();
				reward.Value = 0;
			}

            while (!current.IsTerminal())
			{
				accumulate = 0;
				interval.Clear();
				//if (actions.Length == 0)
				//    break;

				foreach (var a in actions)
				{
                    h = 0;
					var gameMan = this.CurrentStateWorldModel.GetGameManager();
					var character = gameMan.characterData;
					wa = a as WalkToTargetAndExecuteAction;
					if (wa != null)
					{
						euclidean = (wa.Target.transform.position - wa.Character.transform.position).magnitude;
					}

					if (a.Name.Contains("GetH"))
					{
						h = (character.MaxHP - character.HP) * 2.5f;                            //0-25
					}
					else if (a.Name.Contains("Pi"))                                             //5-25
					{
						h = (character.Money + 5) * 2.5f;
					}
					else if (a.Name.Contains("FireballS") || a.Name.Contains("FireballO"))      //0-25
					{
						h = character.Mana;
					}
					else if (a.Name.Contains("SwordAttackS"))
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


                    h = h * 1000 / euclidean;
                        
					accumulate += h;
					interval.Add(Math.Pow(Math.E, h/accumulate));
				}
		
				random = RandomGenerator.NextDouble() * Math.Pow(Math.E, h / accumulate);
				for (int j = 0; j < interval.Count; j++)
				{
					if (random <= interval[j])
					{
						action = actions[j];
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
