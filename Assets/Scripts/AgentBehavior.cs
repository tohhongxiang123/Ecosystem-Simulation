using UnityEngine.AI;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.ComponentModel;

[RequireComponent(typeof(NavMeshAgent))]
public class AgentBehavior : MonoBehaviour
{
    [SerializeField] public string foodTag = "";
    [SerializeField] private float fovRange = 10f;
    private float interactRadius;
    private float reproduceRadius;

    [Header("Wander Parameters")]
    [SerializeField] private float wanderRadius = 10;
    [SerializeField] private float wanderTimer = 5.0f;
    private float wanderCycleTimer;

    [Header("Stats")]
    [SerializeField] private float maxHealth = 100;
    [SerializeField] private int healthDecayRate = 1;
    private float health;
    float foodHealthReplenish = 20; // TODO: Move this to each individual food
    [SerializeField] private float matingCooldownSeconds = 30;
    public enum AgentGender { MALE, FEMALE };
    [SerializeField] private AgentGender agentGender;
    public AgentGender getAgentGender()
    {
        return agentGender;
    }

    private NavMeshAgent agent;
    private Animator animator;

    public enum AgentState { EATING, DONE_EATING, MATING, DONE_MATING, WANDERING, DEAD };
    private AgentState agentState = AgentState.WANDERING;

    public AgentState getAgentState()
    {
        return agentState;
    }

    [SerializeField] private bool isChild = false;
    private float childCounter = 0;
    private float growIntoAdultDurationSeconds = 30;
    private const float childScale = 0.5f;

    // Start is called before the first frame update
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        wanderCycleTimer = wanderTimer;
        interactRadius = agent.stoppingDistance + 0.1f;
        reproduceRadius = agent.stoppingDistance + 2 * agent.radius;
        health = maxHealth;
    }

    void Update()
    {
        HandleHealthUpdate();
        HandleGrowIntoAdultUpdate();
    }

    private void HandleHealthUpdate()
    {
        health -= Time.deltaTime * healthDecayRate;
        if (health <= 0)
        {
            Die();
        }
    }

    private void HandleGrowIntoAdultUpdate() {
        if (isChild) {
            childCounter += Time.deltaTime;
            float progressToAdult = childCounter / growIntoAdultDurationSeconds;

            float size = Mathf.Lerp(childScale, 1, progressToAdult);
            transform.localScale = new Vector3(size, size, size);
        }

        if (childCounter >= growIntoAdultDurationSeconds) {
            isChild = false;
            transform.localScale = new Vector3(1, 1, 1);
            childCounter = 0;
        }
    }

    public void Die()
    {
        StartCoroutine(HandleDeath());
    }

    IEnumerator HandleDeath()
    {
        agentState = AgentState.DEAD;
        animator.SetBool("isDead", true);

        yield return new WaitForSeconds(1);

        while (animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1 || animator.IsInTransition(0)) // while animation is not finished
        {
            yield return new WaitForSeconds(0.1f);
        }

        Destroy(gameObject);
    }

    public void Seek(Vector3 location)
    {
        agent.SetDestination(location);
    }

    public void Flee(Vector3 location)
    {
        Vector3 fleeVector = location - transform.position;
        Seek(transform.position - fleeVector);
    }

    public void Pursue(Transform target)
    {
        Vector3 targetDirection = target.transform.position - transform.position;
        float relativeHeading = Vector3.Angle(transform.forward, transform.TransformVector(target.transform.forward));
        float toTarget = Vector3.Angle(transform.forward, transform.TransformVector(targetDirection));

        NavMeshAgent targetAgent = target.GetComponent<NavMeshAgent>();
        float targetSpeed = targetAgent == null ? 0 : targetAgent.speed;

        if ((toTarget > 90 && relativeHeading < 20) || targetSpeed < 0.01f)
        {
            Seek(target.transform.position);
            return;
        }

        float lookAhead = targetDirection.magnitude / (agent.speed + targetSpeed);
        Seek(target.transform.position + target.transform.forward * lookAhead);
    }

    public void Evade(Transform target)
    {
        Vector3 targetDirection = target.transform.position - transform.position;

        NavMeshAgent targetAgent = target.GetComponent<NavMeshAgent>();
        float targetSpeed = targetAgent == null ? 0 : targetAgent.speed;

        float lookAhead = targetDirection.magnitude / (agent.speed + targetSpeed);

        Flee(target.transform.position + target.transform.forward * lookAhead);
    }

    public void Wander()
    {
        agentState = AgentState.WANDERING;
        wanderCycleTimer += Time.deltaTime;

        if (wanderCycleTimer >= wanderTimer || agent.remainingDistance <= agent.stoppingDistance)
        {
            Vector3 newPos = RandomNavSphere(transform.position, wanderRadius, -1);
            agent.SetDestination(newPos);
            wanderCycleTimer = 0;
        }
    }

    public List<GameObject> GetFoodInFOVRange()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, fovRange);
        List<GameObject> foods = new List<GameObject>();

        foreach (Collider collider in colliders)
        {
            if (collider.gameObject.CompareTag(foodTag) && NavMesh.SamplePosition(collider.transform.position, out NavMeshHit hit, agent.stoppingDistance + 0.1f, NavMesh.AllAreas))
            {
                foods.Add(collider.gameObject);
            }
        }

        foods = foods.OrderBy((d) => (d.transform.position - transform.position).sqrMagnitude).ToList();
        return foods;
    }

    public bool IsTargetInteractable(GameObject target)
    {
        return Vector3.Distance(transform.position, target.transform.position) <= interactRadius;
    }

    public bool IsTargetInReproduceRange(GameObject target)
    {
        return Vector3.Distance(transform.position, target.transform.position) <= reproduceRadius;
    }

    public bool IsAtDestination()
    {
        return agent.remainingDistance <= agent.stoppingDistance;
    }

    public void Eat(GameObject target)
    {
        if (target == null || agentState == AgentState.EATING) // make sure that the current creature is not already consuming
        {
            return;
        }

        StartCoroutine(HandleEat(target));
    }

    IEnumerator HandleEat(GameObject target)
    {
        agentState = AgentState.EATING;
        animator.SetBool("isEating", true);
        target.tag = "Untagged";

        yield return new WaitForSeconds(1);

        while (animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1 || animator.IsInTransition(0)) // while animation is not finished
        {
            yield return new WaitForSeconds(0.1f);
        }

        Destroy(target);
        animator.SetBool("isEating", false);
        health = Mathf.Min(health + foodHealthReplenish, maxHealth);
        agentState = AgentState.DONE_EATING;
    }

    private static Vector3 RandomNavSphere(Vector3 origin, float dist, int layerMask)
    {
        Vector3 randDirection = Random.insideUnitSphere * dist;
        randDirection += origin;

        NavMeshHit navHit;
        NavMesh.SamplePosition(randDirection, out navHit, dist, layerMask);
        return navHit.position;
    }

    public List<GameObject> GetMatesInFOVRange()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, fovRange);
        List<GameObject> mates = new List<GameObject>();

        foreach (Collider collider in colliders)
        {
            if (collider.gameObject == gameObject)
            { // dont get itself
                continue;
            }

            if (collider.gameObject.TryGetComponent<AgentBehavior>(out var partnerAgentBehavior) && partnerAgentBehavior.CanMate() && partnerAgentBehavior.getAgentGender() != agentGender)
            {
                mates.Add(collider.gameObject);
            }
        }

        mates = mates.OrderBy((d) => (d.transform.position - transform.position).sqrMagnitude).ToList();
        return mates;
    }

    public void Mate(GameObject mate)
    {
        if (mate == null || agentState == AgentState.MATING) // make sure that the current creature is not already consuming
        {
            return;
        }

        StartCoroutine(HandleMatingCooldown(mate));
        StartCoroutine(HandleMating(mate));
    }

    public bool CanMate()
    {
        return !gameObject.CompareTag("Mated") && !isChild;
    }

    IEnumerator HandleMating(GameObject mate)
    {
        agentState = AgentState.MATING;
        animator.SetBool("isMating", true);

        yield return new WaitForSeconds(1);

        while (animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1 || animator.IsInTransition(0)) // while animation is not finished
        {
            yield return new WaitForSeconds(0.1f);
        }

        animator.SetBool("isMating", false);

        gameObject.tag = "Mated";
        mate.tag = "Mated";
        agentState = AgentState.DONE_MATING;

        if (agentGender == AgentGender.FEMALE)
        {
            GameObject child = Instantiate(gameObject, gameObject.transform.parent);
            AgentBehavior childAgentBehavior = child.GetComponent<AgentBehavior>();
            childAgentBehavior.isChild = true;
            child.tag = "Untagged";
        }
    }

    IEnumerator HandleMatingCooldown(GameObject mate)
    {
        yield return new WaitForSeconds(matingCooldownSeconds);

        mate.tag = "Untagged";
        gameObject.tag = "Untagged";
    }
}