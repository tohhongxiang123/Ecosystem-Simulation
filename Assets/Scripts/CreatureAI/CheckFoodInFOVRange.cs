using System.Collections.Generic;
using UnityEngine;

using BehaviorTree;

public class CheckFoodInFOVRange : Node
{
    private AgentBehavior _agentBehavior;
    public CheckFoodInFOVRange(AgentBehavior _agentBehavior)
    {
        this._agentBehavior = _agentBehavior;
    }

    public override NodeState Evaluate()
    {
        GameObject t = (GameObject)GetData("target");

        if (t == null)
        { // if there is no current target, check if there are valid targets
            List<GameObject> targetFoods = _agentBehavior.GetFoodInFOVRange();
            if (targetFoods.Count == 0)
            {
                state = NodeState.FAILURE;
                return state;
            }
            
            parent.parent.SetData("target", targetFoods[0]);
            state = NodeState.SUCCESS;
            return state;
        }

        state = NodeState.SUCCESS;
        return state;
    }
}
