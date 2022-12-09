using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Dreamteck.Splines;
public class Branch : MonoBehaviour
{
    [Header("Properties")]
    public bool isActive;
    public bool isDone;
    public double tipLength;
    public double startLength;
    public Tree tree;
    [Header("Movement Components")]
    public SplineComputer rendererComputer;
    public List<Node> renderingNodes;
    public List<SplineFollower> followingNodes;
    public SplineComputer pathComputer;
    public List<Node> pathNodes;
    [Header("Speed")]
    [SerializeField] float minSpeed;
    [SerializeField] float branchSpeedMultiplier;
    [SerializeField] float currentMovement;
    public float rotationSubBranch = 0;

    public float CurrentMovement
    {
        get 
        { 
            return currentMovement; 
        }
        set 
        { 
            currentMovement = value; 
            for(int i=0; i<followingNodes.Count; i++)
            {
                followingNodes[i].followSpeed = currentMovement;
            }
        }
    }

    [Header("Scaling")]
    public Vector2 scaleSetUpLimits;
    [Header("Sub Components")]
    public Branch parentBranch;
    public List<Branch> subBranches;
    public List<Leaf> leaves;
    public List<Fruit> fruits;
    [Header("Sub Data")]
    public bool isSubBranch;
    public SplineFollower mainBranch;
    public float percentToStart;
    [Header("Idle Profit")]
    public int baseIdleProfit;
    public int leafIdleProfitBonus;
    public float percentToIdle;
    public float timeToIdleReward;
    public float rewardTimer;
    public Vector3 profitMessageOffset;
    [Header("Sell Price")]
    public int baseSellPrice;

    private void OnEnable()
    {
        SetUp();
    }

    private void Update()
    {
        Grow();
        IdleProfit();
    }

    public double GetGrowthPercent()
    {
        if(followingNodes.Count > 0)
        {
            if(!isSubBranch)
            {
                //Debug.Log("Valor: " + followingNodes[0].GetPercent());

                if(SaveManager.LoadOnlyTutorial() == 2)  //  Vendes tu primer arbol
                {
                    if(followingNodes[0].GetPercent() >= 0.15f)
                    {
                        GameManager.instance.appearButtonTutorial = true;
                        UIManager.instance.CallThirdtTutorial();
                    }
                }
            }

            return followingNodes[0].GetPercent();
        }
        else
        {
            return 0;
        }
    }

    void Grow()
    {
        if (isActive)
        {
            if (!isDone)
            {
                for (int i = 0; i < rendererComputer.pointCount; i++)
                {
                    SplinePoint point = rendererComputer.GetPoint(i);
                    point.size = scaleSetUpLimits.x + (scaleSetUpLimits.y - scaleSetUpLimits.x) * (float)GetGrowthPercent() * ((float)i / rendererComputer.pointCount);
                    rendererComputer.SetPoint(i, point);
                    rendererComputer.Rebuild();
                }

                CurrentMovement = Mathf.MoveTowards(currentMovement, Mathf.Max(GameManager.instance.targetMovement, isSubBranch ? 0 : minSpeed) * GameManager.instance.movementSpeedBonus, Time.deltaTime * GameManager.instance.movementSpeedMultiplier) * branchSpeedMultiplier;
                if (GetGrowthPercent() >= 1)
                {
                    //if (isSubBranch)
                    //{
                    //    GetComponent<Animator>().enabled = true;
                    //}
                    isDone = true;
                }
            }
        }
    }

    void SetUp()
    {
        //if (!isSubBranch)
        //{
        //    Shader.SetGlobalFloat("MinScreenPos", Camera.main.WorldToViewportPoint(new Vector3(0, .38f, 0)).y);
        //    //Shader.SetGlobalFloat("MaxScreenPos", Camera.main.WorldToViewportPoint(new Vector3(0, 2.37f, 0)).y);
        //}
        //else
        //{

        //}

        for (int i = 0; i < rendererComputer.pointCount; i++)
        {
            SplinePoint point = rendererComputer.GetPoint(i);
            point.size = scaleSetUpLimits.x;
            rendererComputer.SetPoint(i, point);
            rendererComputer.Rebuild();
        }
        CurrentMovement = 0;
        if (mainBranch != null)
        {
            isSubBranch = true;
            SetActive(false);
        }
        else
        {
            isActive = true;
        }
    }

    public void SetActive(bool _active)
    {
        isActive = _active;
        rendererComputer.gameObject.SetActive(_active);
    }

    
    void IdleProfit()
    {
        if(!isSubBranch)
        {
            if (GetGrowthPercent() >= percentToIdle)
            {
                rewardTimer += Time.deltaTime;
                if (rewardTimer >= timeToIdleReward)
                {
                    rewardTimer -= timeToIdleReward;
                    IdleReward();
                }
            }
        }
    }
    void IdleReward()
    {
        
        UIManager.instance.normalCurrencyCounter.ChangeCurrency((System.Numerics.BigInteger)(baseIdleProfit * GetGrowthPercent()));
        IncomeMessages.AddMessage(transform.position + profitMessageOffset, (System.Numerics.BigInteger)(baseIdleProfit * GetGrowthPercent()));
    }

    public int GetActiveLeaves()
    {
        int temp = 0;
        for(int i=0; i<leaves.Count; i++)
        {
            if(leaves[i].isDone)
            {
                temp++;
            }
        }
        return temp;
    }

}
