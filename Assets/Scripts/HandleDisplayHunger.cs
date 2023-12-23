using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HandleDisplayHunger : MonoBehaviour
{
    public Slider slider;
    private AgentBehavior agentBehavior;
    // Start is called before the first frame update
    void Start()
    {
        agentBehavior = GetComponent<AgentBehavior>();
        slider.maxValue = agentBehavior.stats.maxHunger;
    }

    // Update is called once per frame
    void Update()
    {
        slider.value = agentBehavior.GetHunger();
    }
}