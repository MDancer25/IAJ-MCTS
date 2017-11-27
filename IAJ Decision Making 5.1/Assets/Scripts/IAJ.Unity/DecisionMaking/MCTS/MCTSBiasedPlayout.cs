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
            double softmax = 0;
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
                        if (euclidean <= 0)
                            euclidean = 1;
					}

                    if (a.Name.Contains("LevelUp"))                                                      //1000
                    {
                        h = 1000;                                                                        
                    }
                    if (a.Name.Contains("GetHealthPotion"))                                              //0-25
                    {
						h = (character.MaxHP - character.HP) * 1.5f;                                    
					}
					else if (a.Name.Contains("PickUpChest"))                                             //5-25
                    {
                        h = (character.Money + 5) * 3.5f;
					}
					else if (a.Name.Contains("FireballSkeleton") || a.Name.Contains("FireballOrc"))      //0-25
					{
						h = character.Mana*30;
					}
					else if (a.Name.Contains("SwordAttackSkeleton"))
					{
						h = (character.HP - 5)*2;
					}
					else if (a.Name.Contains("SwordAttackOrc"))
					{
						h = (character.HP - 10)*2;
					}
					else if (a.Name.Contains("SwordAttackDragon"))
					{
						h = character.HP - 20;
					}

                    if (h < 0)
                        h = 0;

                    h = h * 1000 / euclidean;
                        
					accumulate += h;
                    if (h > 0)
                    {
                        softmax += Math.Pow(Math.E, -h / accumulate);
                        interval.Add(softmax);
                        Debug.Log(softmax);
                    }
                    else
                    {
                        interval.Add(0);
                    }
				}

                random = RandomGenerator.NextDouble() * softmax;
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
