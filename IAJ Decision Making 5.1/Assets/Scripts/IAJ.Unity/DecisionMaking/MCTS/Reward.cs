using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.Scripts.IAJ.Unity.DecisionMaking.MCTS
{
    public class Reward
    {
        public float Value { get; set; }
        public int PlayerID { get; set; }
        public float getRewardForNode(MCTSNode node)
        {
            if (node.Parent == null)
                return this.Value;

            if(node.Parent.PlayerID == this.PlayerID)
            {
                return this.Value;
            }
            return -this.Value;
        }
    }
}
