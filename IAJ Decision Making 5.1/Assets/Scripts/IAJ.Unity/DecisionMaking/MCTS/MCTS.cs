﻿using Assets.Scripts.DecisionMakingActions;
using Assets.Scripts.GameManager;
using Assets.Scripts.IAJ.Unity.DecisionMaking.GOB;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.IAJ.Unity.DecisionMaking.MCTS
{
    public class MCTS
    {
        public const float C = 1.4f;
        public bool InProgress { get; private set; }
        public int MaxIterations { get; set; }
        public int MaxIterationsProcessedPerFrame { get; set; }
        public int MaxPlayoutDepthReached { get; private set; }
        public int MaxSelectionDepthReached { get; private set; }
        public float TotalProcessingTime { get; private set; }
        public MCTSNode BestFirstChild { get; set; }
        public List<GOB.Action> BestActionSequence { get; private set; }


        private int CurrentIterations { get; set; }
        private int CurrentIterationsInFrame { get; set; }
        private int CurrentDepth { get; set; }

        public CurrentStateWorldModel CurrentStateWorldModel { get; set; }
        private MCTSNode InitialNode { get; set; }
        protected virtual System.Random RandomGenerator { get; set; }

        public MCTS(CurrentStateWorldModel currentStateWorldModel)
        {
            this.InProgress = false;
            this.CurrentStateWorldModel = currentStateWorldModel;
            this.MaxIterations = 15000;
            this.MaxIterationsProcessedPerFrame = 10;
            this.RandomGenerator = new System.Random();
        }

        public void InitializeMCTSearch()
        {
            this.MaxPlayoutDepthReached = 0;
            this.MaxSelectionDepthReached = 0;
            this.CurrentIterations = 0;
            this.CurrentIterationsInFrame = 0;
            this.TotalProcessingTime = 0.0f;
            this.CurrentStateWorldModel.Initialize();
            this.InitialNode = new MCTSNode(this.CurrentStateWorldModel)
            {
                Action = null,
                Parent = null,
                PlayerID = 0
            };
            this.InProgress = true;
            this.BestFirstChild = null;
            this.BestActionSequence = new List<GOB.Action>();
        }

        public GOB.Action Run()
        {
            MCTSNode selectedNode;
            Reward reward;

            var startTime = Time.realtimeSinceStartup;
            this.CurrentIterationsInFrame = 0;

            while (this.CurrentIterationsInFrame < this.MaxIterations)
            {
                this.CurrentDepth = 0;
                selectedNode = Selection(this.InitialNode);
                reward = Playout(selectedNode.State);
                Backpropagate(selectedNode, reward);
                this.CurrentIterationsInFrame++;
                //this.CurrentIterations++;
            }
            
            this.InProgress = false;
            this.BestFirstChild = BestChild(InitialNode);

            if (this.BestFirstChild == null || this.BestFirstChild.Q == 0)
            {
                return null;
            }
            
            this.TotalProcessingTime += Time.realtimeSinceStartup - startTime;
            var bestchild = this.BestFirstChild;
            while (bestchild != null)
            {
                this.BestActionSequence.Add(bestchild.Action);
                bestchild = BestChild(bestchild);
            }
            return this.BestFirstChild.Action;
        }

        private MCTSNode Selection(MCTSNode initialNode)
        {
            GOB.Action nextAction;
            MCTSNode currentNode = initialNode;
            MCTSNode bestChild;

            while (!currentNode.State.IsTerminal())
            {
                nextAction = currentNode.State.GetNextAction();
                if (nextAction != null)
                {
                    return Expand(currentNode, nextAction);
                }
                else
                {
                    bestChild = currentNode;
                    currentNode = BestUCTChild(currentNode);
                    if (currentNode == null)
                    {
                        return bestChild;
                    }
                    this.CurrentDepth++;
                }
            }
            return currentNode;
        }

        protected virtual Reward Playout(WorldModel initialPlayoutState)
        {
            GOB.Action action;
            GOB.Action[] actions;
            Reward reward = new Reward();
            WorldModel current = initialPlayoutState;
            int random;
            actions = current.GetExecutableActions();
            if (actions.Length == 0)
            {
                reward.PlayerID = current.GetNextPlayer();
                reward.Value = 0;
            }

            while (!current.IsTerminal())
            {
                current = current.GenerateChildWorldModel();
                random = RandomGenerator.Next(0, actions.Length);
                action = actions[random];
                action.ApplyActionEffects(current);
                current.CalculateNextPlayer();
            }

            reward.PlayerID = current.GetNextPlayer();
            reward.Value = current.GetScore();
            return reward;
        }

        private void Backpropagate(MCTSNode node, Reward reward)
        {
            int i = 1;
            while (node != null)
            {
                i--;
                node.N++;

                if (node.Parent == null || node.Parent.PlayerID == reward.PlayerID)
                {
                    node.Q += reward.Value;
                }
                else
                {
                    node.Q -= reward.Value;
                }
                node = node.Parent;
                //if(node != null)
                //{
                //    Debug.Log("-------------acção escolhida------------");
                //    Debug.Log(node.Q);
                //    Debug.Log("----------------------------------------");
                //}
            }
        }


        private MCTSNode Expand(MCTSNode parent, GOB.Action action)
        {
            WorldModel state = parent.State.GenerateChildWorldModel();
            MCTSNode child = new MCTSNode(state);

            child.Parent = parent;

            action.ApplyActionEffects(state);
            state.CalculateNextPlayer();

            child.Action = action;
            parent.ChildNodes.Add(child);

            return child;
        }

        //gets the best child of a node, using the UCT formula
        private MCTSNode BestUCTChild(MCTSNode node)
        {
            List<MCTSNode> children = node.ChildNodes;
            MCTSNode best, currentChild;
            float ui;
            double uct;
            double BestUCT = -1;
            best = null;
            for (int count = 0; count < children.Count; count++)
            {
                currentChild = children[count];
                ui = currentChild.Q / currentChild.N;
                uct = ui + (C * Math.Sqrt(Math.Log(currentChild.Parent.N) / currentChild.N));
                if (uct > BestUCT)
                {
                    BestUCT = uct;
                    best = currentChild;
                }
            }
            return best;
        }

        //this method is very similar to the bestUCTChild, but it is used to return the final action of the MCTS search, and so we do not care about
        //the exploration factor
        public MCTSNode BestChild(MCTSNode node)
        {
            List<MCTSNode> children = node.ChildNodes;
            MCTSNode best, currentChild;
            float ui;
            double uct;
            double BestUCT = -1;
            best = null;
            for (int count = 0; count < children.Count; count++)
            {
                currentChild = children[count];
                ui = currentChild.Q / (currentChild.N);
                uct = ui * 20/ currentChild.N;
                /*Debug.Log("-----------Q---------");
                Debug.Log(currentChild.Q);
                Debug.Log("-------------------------");
                Debug.Log("-----------UCT---------");
                Debug.Log(uct);
                Debug.Log("-------------------------");
                Debug.Log("---------UI-------");
                Debug.Log(ui);
                Debug.Log("-------------------------");
                Debug.Log("---------SQUIRT--------");
                Debug.Log(20 / Math.Sqrt(currentChild.N));
                Debug.Log("-------------------------");*/
                if (uct > BestUCT)
                {
                    BestUCT = uct;
                    best = currentChild;
                }
                else if (uct == BestUCT)
                {
                    var random = RandomGenerator.Next(0, 2);
                    if (random == 1)
                    {
                        BestUCT = uct;
                        best = currentChild;
                    }
                }
            }
            return best;
        }
    }
}
