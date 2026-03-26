using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ProjectSun.Policy;
using ProjectSun.Policy.Testing;

/// <summary>
/// 정책 시스템 테스트 씬 + SO 에셋 자동 생성.
/// </summary>
public static class PolicyTestSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/PolicyTest.unity";
    private const string DataPath = "Assets/Data/Policy";

    [MenuItem("ProjectSun/Create Policy Test Scene")]
    public static void CreateTestScene()
    {
        EnsureDirectories();

        // SO 에셋 생성
        var tree = CreatePolicyTreeAsset();

        // 씬 생성
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // PolicyManager
        var managerGO = new GameObject("PolicyManager");
        var policyManager = managerGO.AddComponent<PolicyManager>();
        SerializedObject soManager = new(policyManager);
        soManager.FindProperty("policyTree").objectReferenceValue = tree;
        soManager.ApplyModifiedPropertiesWithoutUndo();

        // PolicyEffectResolver
        var resolverGO = new GameObject("PolicyEffectResolver");
        var effectResolver = resolverGO.AddComponent<PolicyEffectResolver>();
        SerializedObject soResolver = new(effectResolver);
        soResolver.FindProperty("policyManager").objectReferenceValue = policyManager;
        soResolver.ApplyModifiedPropertiesWithoutUndo();

        // PolicyUI
        var uiGO = new GameObject("PolicyUI");
        var policyUI = uiGO.AddComponent<PolicyUI>();
        SerializedObject soUI = new(policyUI);
        soUI.FindProperty("policyManager").objectReferenceValue = policyManager;
        soUI.FindProperty("effectResolver").objectReferenceValue = effectResolver;
        soUI.ApplyModifiedPropertiesWithoutUndo();

        // PolicyTestController
        var controllerGO = new GameObject("PolicyTestController");
        var testController = controllerGO.AddComponent<PolicyTestController>();
        SerializedObject soController = new(testController);
        soController.FindProperty("policyManager").objectReferenceValue = policyManager;
        soController.FindProperty("effectResolver").objectReferenceValue = effectResolver;
        soController.FindProperty("policyUI").objectReferenceValue = policyUI;
        soController.ApplyModifiedPropertiesWithoutUndo();

        // 초기 턴 1 해금 트리거
        policyManager.OnNewTurn(1);

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.Refresh();

        Debug.Log($"Policy Test Scene created: {ScenePath}");
    }

    private static PolicyTreeSO CreatePolicyTreeAsset()
    {
        // ── 내정 카테고리 노드 ──
        var domestic1 = CreateNode("dom_rationing", "균등 배급", "기지 내 자원을 균등하게 배분합니다.",
            PolicyCategory.Domestic, 1, null, false, null,
            new PolicyEffect { effectType = PolicyEffectType.BasicProductionMod, value = 0.15f, isPercentage = true },
            new PolicyEffect { effectType = PolicyEffectType.HopeInstant, value = 3f, isPercentage = false });

        var domestic2A = CreateNode("dom_standard_work", "표준 근무", "적절한 노동 시간으로 효율을 높입니다.",
            PolicyCategory.Domestic, 3, domestic1, true, null,
            new PolicyEffect { effectType = PolicyEffectType.WorkerEfficiencyMod, value = 0.20f, isPercentage = true },
            new PolicyEffect { effectType = PolicyEffectType.DiscontentPerTurn, value = -2f, isPercentage = false });

        var domestic2B = CreateNode("dom_overtime", "연장 근무", "더 오래 일하여 생산량을 극대화합니다.",
            PolicyCategory.Domestic, 3, domestic1, true, null,
            new PolicyEffect { effectType = PolicyEffectType.BasicProductionMod, value = 0.30f, isPercentage = true },
            new PolicyEffect { effectType = PolicyEffectType.DiscontentPerTurn, value = 3f, isPercentage = false });

        // 분기 쌍 설정
        SetBranchPair(domestic2A, domestic2B);

        var domestic3 = CreateNode("dom_self_sufficient", "자급자족", "내부 자원 순환 체계를 구축합니다.",
            PolicyCategory.Domestic, 5, null, false, null,
            new PolicyEffect { effectType = PolicyEffectType.AdvancedProductionMod, value = 0.10f, isPercentage = true });
        // domestic3의 선행 노드는 분기 노드 중 하나 — 어느 쪽이든 제정하면 해금
        // 분기 A or B 중 제정된 쪽이 선행이 됨. 구현 단순화를 위해 domestic1을 선행으로 설정
        SetPrerequisite(domestic3, domestic1);

        // ── 탐사 카테고리 노드 ──
        var explore1 = CreateNode("exp_basic_recon", "기본 정찰", "기본 정찰 체계를 수립합니다.",
            PolicyCategory.Exploration, 1, null, false, null,
            new PolicyEffect { effectType = PolicyEffectType.EncounterChanceMod, value = -0.10f, isPercentage = true },
            new PolicyEffect { effectType = PolicyEffectType.ExplorationRewardMod, value = 0.15f, isPercentage = true });

        var explore2A = CreateNode("exp_safety_first", "안전 우선", "원정대의 안전을 최우선으로 합니다.",
            PolicyCategory.Exploration, 3, explore1, true, null,
            new PolicyEffect { effectType = PolicyEffectType.ExplorationDamageMod, value = -0.30f, isPercentage = true },
            new PolicyEffect { effectType = PolicyEffectType.ExplorationSpeedMod, value = -0.20f, isPercentage = true });

        var explore2B = CreateNode("exp_speed_first", "속도 우선", "빠른 탐사를 위해 위험을 감수합니다.",
            PolicyCategory.Exploration, 3, explore1, true, null,
            new PolicyEffect { effectType = PolicyEffectType.ExplorationSpeedMod, value = 0.30f, isPercentage = true },
            new PolicyEffect { effectType = PolicyEffectType.ExplorationDamageMod, value = 0.20f, isPercentage = true });

        SetBranchPair(explore2A, explore2B);

        var explore3 = CreateNode("exp_tactical_share", "전술 공유", "탐사 정보를 방어에 활용합니다.",
            PolicyCategory.Exploration, 5, explore1, false, null,
            new PolicyEffect { effectType = PolicyEffectType.TowerDamageMod, value = 0.10f, isPercentage = true });

        // ── 방어 카테고리 노드 ──
        var defense1 = CreateNode("def_heightened_alert", "경계 강화", "전 기지에 경계 태세를 강화합니다.",
            PolicyCategory.Defense, 1, null, false, null,
            new PolicyEffect { effectType = PolicyEffectType.TowerRangeMod, value = 0.15f, isPercentage = true },
            new PolicyEffect { effectType = PolicyEffectType.DefenseResourceCostMod, value = -0.10f, isPercentage = true });

        var defense2A = CreateNode("def_resource_militia", "자원 민병", "자원을 투입하여 민병대를 운영합니다.",
            PolicyCategory.Defense, 3, defense1, true, null,
            new PolicyEffect { effectType = PolicyEffectType.DefenseResourceCostMod, value = -0.25f, isPercentage = true },
            new PolicyEffect { effectType = PolicyEffectType.WorkerEfficiencyMod, value = -0.10f, isPercentage = true });

        var defense2B = CreateNode("def_conscript_militia", "징집 민병", "주민을 징집하여 전투력을 확보합니다.",
            PolicyCategory.Defense, 3, defense1, true, null,
            new PolicyEffect { effectType = PolicyEffectType.WorkerEfficiencyMod, value = 0.15f, isPercentage = true },
            new PolicyEffect { effectType = PolicyEffectType.DiscontentPerTurn, value = 2f, isPercentage = false },
            new PolicyEffect { effectType = PolicyEffectType.TowerDamageMod, value = 0.20f, isPercentage = true });

        SetBranchPair(defense2A, defense2B);

        var defense3 = CreateNode("def_fortify", "방벽 보강", "방벽을 강화하여 방어력을 높입니다.",
            PolicyCategory.Defense, 5, defense1, false, null,
            new PolicyEffect { effectType = PolicyEffectType.WallHPMod, value = 0.25f, isPercentage = true },
            new PolicyEffect { effectType = PolicyEffectType.WallRepairCostMod, value = -0.15f, isPercentage = true });

        // ── PolicyTreeSO 생성 ──
        var tree = ScriptableObject.CreateInstance<PolicyTreeSO>();
        SerializedObject soTree = new(tree);
        soTree.FindProperty("treeName").stringValue = "PoC 정책 트리";
        soTree.FindProperty("treeDescription").stringValue = "프로토타입 검증용 정책 트리 (3카테고리 x 3~4노드)";

        SetNodeList(soTree, "domesticNodes", new[] { domestic1, domestic2A, domestic2B, domestic3 });
        SetNodeList(soTree, "explorationNodes", new[] { explore1, explore2A, explore2B, explore3 });
        SetNodeList(soTree, "defenseNodes", new[] { defense1, defense2A, defense2B, defense3 });

        soTree.ApplyModifiedPropertiesWithoutUndo();

        AssetDatabase.CreateAsset(tree, $"{DataPath}/PoCPolicyTree.asset");
        return tree;
    }

    private static PolicyNodeSO CreateNode(string id, string name, string desc,
        PolicyCategory category, int unlockTurn,
        PolicyNodeSO prerequisite, bool isBranch, PolicyNodeSO branchPair,
        params PolicyEffect[] effects)
    {
        var node = ScriptableObject.CreateInstance<PolicyNodeSO>();
        SerializedObject so = new(node);
        so.FindProperty("nodeId").stringValue = id;
        so.FindProperty("nodeName").stringValue = name;
        so.FindProperty("description").stringValue = desc;
        so.FindProperty("category").enumValueIndex = (int)category;
        so.FindProperty("unlockTurn").intValue = unlockTurn;
        so.FindProperty("prerequisiteNode").objectReferenceValue = prerequisite;
        so.FindProperty("isBranchNode").boolValue = isBranch;
        so.FindProperty("branchPairNode").objectReferenceValue = branchPair;

        var effectsProp = so.FindProperty("effects");
        effectsProp.ClearArray();
        for (int i = 0; i < effects.Length; i++)
        {
            effectsProp.InsertArrayElementAtIndex(i);
            var elem = effectsProp.GetArrayElementAtIndex(i);
            elem.FindPropertyRelative("effectType").enumValueIndex = (int)effects[i].effectType;
            elem.FindPropertyRelative("value").floatValue = effects[i].value;
            elem.FindPropertyRelative("isPercentage").boolValue = effects[i].isPercentage;
            elem.FindPropertyRelative("target").stringValue = effects[i].target ?? "";
        }

        so.ApplyModifiedPropertiesWithoutUndo();

        string safeName = id.Replace("_", "-");
        AssetDatabase.CreateAsset(node, $"{DataPath}/Node_{safeName}.asset");
        return node;
    }

    private static void SetBranchPair(PolicyNodeSO a, PolicyNodeSO b)
    {
        SerializedObject soA = new(a);
        soA.FindProperty("branchPairNode").objectReferenceValue = b;
        soA.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject soB = new(b);
        soB.FindProperty("branchPairNode").objectReferenceValue = a;
        soB.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetPrerequisite(PolicyNodeSO node, PolicyNodeSO prerequisite)
    {
        SerializedObject so = new(node);
        so.FindProperty("prerequisiteNode").objectReferenceValue = prerequisite;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetNodeList(SerializedObject treeObj, string propName, PolicyNodeSO[] nodes)
    {
        var prop = treeObj.FindProperty(propName);
        prop.ClearArray();
        for (int i = 0; i < nodes.Length; i++)
        {
            prop.InsertArrayElementAtIndex(i);
            prop.GetArrayElementAtIndex(i).objectReferenceValue = nodes[i];
        }
    }

    private static void EnsureDirectories()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Data"))
            AssetDatabase.CreateFolder("Assets", "Data");
        if (!AssetDatabase.IsValidFolder("Assets/Data/Policy"))
            AssetDatabase.CreateFolder("Assets/Data", "Policy");
        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            AssetDatabase.CreateFolder("Assets", "Scenes");
    }
}
