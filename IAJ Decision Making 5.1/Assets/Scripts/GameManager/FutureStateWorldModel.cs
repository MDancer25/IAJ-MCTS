using Assets.Scripts.DecisionMakingActions;
using Assets.Scripts.IAJ.Unity.DecisionMaking.GOB;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.GameManager
{
    public class FutureStateWorldModel : WorldModel
    {
        protected GameManager GameManager { get; set; }
        protected int NextPlayer { get; set; }
        protected Action NextEnemyAction { get; set; }
        protected Action[] NextEnemyActions { get; set; }

        public FutureStateWorldModel(GameManager gameManager, List<Action> actions) : base(actions)
        {
            this.GameManager = gameManager;
            this.NextPlayer = 0;
        }

        public FutureStateWorldModel(FutureStateWorldModel parent) : base(parent)
        {
            this.GameManager = parent.GameManager;
        }

        public override WorldModel GenerateChildWorldModel()
        {
            return new FutureStateWorldModel(this);
        }

        public override bool IsTerminal()
        {
            int HP = (int)this.GetProperty(Properties.HP);
            float time = (float)this.GetProperty(Properties.TIME);
            int money = (int)this.GetProperty(Properties.MONEY);

            return HP <= 0 ||  time >= 200 || money == 25;
        }

        public override float GetScore()
        {
            float score = 0.0f;
            int money = (int)this.GetProperty(Properties.MONEY);
            int maxhp = (int)this.GetProperty(Properties.MAXHP);
            int hp = (int)this.GetProperty(Properties.HP);
            int mana = (int)this.GetProperty(Properties.MANA);
            int maxmana = (int)this.GetProperty(Properties.MAXMANA);
            int xp = (int)this.GetProperty(Properties.XP);
            int level = (int)this.GetProperty(Properties.LEVEL);
            float time = (float)this.GetProperty(Properties.TIME);

            if (money == 25)
            {
                score += 100.0f;                                        //dinheiro maximo -> 100 pts
            }

            else
            {
                score += 3 * money;                                     //cc. -> 0 a 60 pts (3x dinheiro actual)
            }

            if (time < 60.0f)    
            {
                score += 50.0f;                                         //menos de 1min -> 50 pts
            }

            else
            {
                score += (200 - time) / 5;                             //cc. -> 0 a 28 pts (0.1 por cada segundo a menos de 2min)
            }

            if (hp == maxhp)
            {
                score += 50;                                            //hp maximo -> 50 pts
            }
            
            else
            {
                score += ((maxhp + hp) / (maxhp - hp) - 1) * level;     //cc. -> 0 a 30 pts (quanto menor a diferença entre maxhp e hp mais pontos se ganha)
            }

            score += xp;                                                //0 a 30

            score += 10 * level - 10;                                   //0 a 20
            if (hp == 0) score = 0;
            return score/275;
        }

        public override int GetNextPlayer()
        {
            return this.NextPlayer;
        }

        public override void CalculateNextPlayer()
        {
            Vector3 position = (Vector3)this.GetProperty(Properties.POSITION);
            bool enemyEnabled;

            //basically if the character is close enough to an enemy, the next player will be the enemy.
            foreach (var enemy in this.GameManager.enemies)
            {
                enemyEnabled = (bool) this.GetProperty(enemy.name);
                if (enemyEnabled && (enemy.transform.position - position).sqrMagnitude <= 400)
                {
                    this.NextPlayer = 1;
                    this.NextEnemyAction = new SwordAttack(this.GameManager.autonomousCharacter, enemy);
                    this.NextEnemyActions = new Action[] { this.NextEnemyAction };
                    return; 
                }
            }
            this.NextPlayer = 0;
            //if not, then the next player will be player 0
        }

        public override Action GetNextAction()
        {
            Action action;
            if (this.NextPlayer == 1)
            {
                action = this.NextEnemyAction;
                this.NextEnemyAction = null;
                return action;
            }
            else return base.GetNextAction();
        }

        public override Action[] GetExecutableActions()
        {
            if (this.NextPlayer == 1)
            {
                return this.NextEnemyActions;
            }
            else return base.GetExecutableActions();
        }

    }
}
