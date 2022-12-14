using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class LevelManager : MonoBehaviour
{
    public GameObject[] trees;
    public Material[] terrainsMaterial;
    public Material[] skyboxMaterial;
    public Color[] treeColor;
    public Transform treeParent;
    public Terrain terrain;
    public GameObject snowParticle;
    public Tree activeTree;
    public Material leavesBlue;
    public Material leavesOrange;
    public ParticleSystem moneyPrt;
    public int numberParticles;
    public float endPosZ;
    public float shadowTest;
    [SerializeField]private Level[] _levels;
    [SerializeField]private TreeTypesController _treeTypeController;

    public AnimationCurve curveAnimation;
    public System.Numerics.BigInteger changeTreePrice;



 private void Update()
    {
        if (Input.GetKeyDown(KeyCode.K))
        {
            StartCoroutine(SellTreeAnimation());
        }
        if (Input.GetKeyDown(KeyCode.M))
        {
            UIManager.instance.normalCurrencyCounter.ChangeCurrency(new System.Numerics.BigInteger(1000000), 0);
        }
    }

    public void InstantiateTree(int _id)
    {
        GameObject go = Instantiate(trees[_id], Vector3.zero, Quaternion.identity, treeParent);
        activeTree = go.GetComponent<Tree>();
        changeTreePrice = (System.Numerics.BigInteger)(100000*Mathf.Pow(10,SaveManager.LoadCurrentLevel())/2.0f);
        UIManager.instance.gameplayWindow.txtNextLevelPrice.text = "$" + GameManager.instance.levelManager.changeTreePrice.ToCompactString();
        UIManager.instance.gameplayWindow.SetChristmasButtons(activeTree.isChristmasPine);
        ChangeScenario();
    }

    public void SellTree()
    {
        if(SaveManager.LoadOnlyTutorial() == 2)  //  Vendes tu primer arbol
        {
            SaveManager.ChangeOnlyTutorial(3);
            UIManager.instance.CloseThirdTutorial();
        }
        SaveManager.SaveSoldTrees(SaveManager.LoadSoldTrees() + 1);
        StartCoroutine(SellTreeAnimation());
    }

    public void ChangeScenario()
    {
        Shader.SetGlobalFloat("ShadowIntensity", 0.8f);
        int level = SaveManager.LoadCurrentLevel();
        Shader.SetGlobalColor("shadowColor", level != 4 ? Color.black : new Color(.4f, 0, 1));
        RenderSettings.skybox = skyboxMaterial[level];
        terrain.materialTemplate = terrainsMaterial[level];
        leavesBlue.color = treeColor[level * 2];
        leavesOrange.color = treeColor[(level * 2) + 1];
        if (level == 1)
        {
            snowParticle.SetActive(true);
        }
        else
        {
            snowParticle.SetActive(false);
        }
        if (_treeTypeController != null) { _treeTypeController.ActivateTreeType(_levels[level].TreeType); }
    }

    IEnumerator SellTreeAnimation()
    {
        UIManager.instance.fadeWindow.Show();
        yield return new WaitUntil(() => UIManager.instance.fadeWindow.anmtrWindow.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1);
        UIManager.instance.normalCurrencyCounter.ChangeCurrency(activeTree.GetSellPrice());
        int growSpeed = activeTree.upgradesManager.GetUpgradeType(UpgradeType.IncreaseSpeed).CurrentLevel;
        Destroy(activeTree.gameObject);
        activeTree = null;
        InstantiateTree((int)Mathf.Repeat(SaveManager.LoadCurrentLevel(),trees.Length));
        activeTree.upgradesManager.GetUpgradeType(UpgradeType.IncreaseSpeed).CurrentLevel = growSpeed;
        moneyPrt.Emit(numberParticles);
        yield return new WaitForSeconds(.2f);
        UIManager.instance.fadeWindow.Hide();
        StartCoroutine(MoveParticles());
    }

    IEnumerator MoveParticles()
    {
        yield return new WaitForSeconds(.5f);
        ParticleSystem.Particle[] particles = new ParticleSystem.Particle[moneyPrt.particleCount];
        moneyPrt.GetParticles(particles);
        Vector3 endPos = moneyPrt.transform.InverseTransformVector( UIManager.instance.normalCurrencyCounter.transform.position);
        endPos.z = endPosZ;
        endPos.y *= 0.2f;
        float timeForParticleToArrive = .5f;
        float eTime = 0;
        List<Vector3> startPositions = new();
        for (int i = 0; i < particles.Length; i++)
        {
            startPositions.Add(particles[i].position);
            particles[i].velocity = new Vector3();
        }
        while (eTime < timeForParticleToArrive)
        {
            eTime += Time.deltaTime;
            for (int i = 0; i < particles.Length; i++)
            {
                particles[i].position = Vector3.Lerp(startPositions[i], endPos, eTime / timeForParticleToArrive);
                particles[i].startColor = Color.Lerp(Color.white, new Color(1, 1, 1, 0), eTime / timeForParticleToArrive);
                moneyPrt.SetParticles(particles);
                if (eTime >= timeForParticleToArrive)
                {
                    particles[i].startLifetime = 0;
                    particles[i].remainingLifetime = 0;
                }
            }
            yield return new WaitForEndOfFrame();
        }
        SaveManager.ResetInvestments();
        //activeTree.upgradesManager.UpgdradeLevelReset();
        //ResetUpgrades();
    }

    public Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Vector3 angles)
    {
        return Quaternion.Euler(angles) * (point - pivot) + pivot;
    }

    public void GoToNextLevel()
    {
        if(SaveManager.LoadCurrency(Currency.Regular)>= changeTreePrice)
        {
            UIManager.instance.fadeWindow.midAction = () => ChangeLevel();
            UIManager.instance.fadeWindow.Show();
        }
    }

    void ChangeLevel()
    {
        UIManager.instance.normalCurrencyCounter.ChangeCurrency(-changeTreePrice);
        SaveManager.SaveSoldTrees(0);
        ResetUpgrades();
        SaveManager.SaveCurrentLevel(SaveManager.LoadCurrentLevel() + 1);
        SellTree();
        SaveManager.SaveSoldTrees(0);

    }

    public void ResetUpgrades()
    {
        for(int i=0; i<activeTree.upgradesManager.upgrades.Count; i++)
        {
            SaveManager.SaveTotalUpgradeLevel(activeTree.upgradesManager.upgrades[i].upgradeType, 0);
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        Shader.SetGlobalFloat("ShadowIntensity", shadowTest);

    }
#endif
}

[System.Serializable]
public class Level
{
    [field:SerializeField]public TreeType TreeType { get; private set; }
}

public enum TreeType { Normal, Japanese, Sakura, Length }