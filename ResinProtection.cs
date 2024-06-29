using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using UnityEngine;

namespace ResinGuard;

// TODO: Clean up the shit code and make it more expandable after exams are finished.
// TODO: Probably add filtering for what this applies to, for now it's fine.
// TODO: Maybe create an item that applies resin to the object? Instead of using the UseItem method that is already in place.

[HarmonyPatch(typeof(WearNTear), nameof(WearNTear.Awake))]
static class WearNTearAwakePatch
{
    static void Postfix(WearNTear __instance)
    {
        ResinProtection? resinProtection = ResinProtection.GetResinProtection(__instance.gameObject);
        if (resinProtection == null) __instance.gameObject.AddComponent<ResinProtection>();
    }
}

[HarmonyPatch(typeof(WearNTear), nameof(WearNTear.IsWet))]
static class WearNTearIsWetPatch
{
    static void Postfix(WearNTear __instance, ref bool __result)
    {
        ResinProtection? resinProtection = ResinProtection.GetResinProtection(__instance.gameObject);
        if (resinProtection == null) return;
        if (resinProtection.GetTar() > 0.0)
            __result = false;
    }
}

[HarmonyPatch(typeof(WearNTear), nameof(WearNTear.IsUnderWater))]
static class WearNTearIsUnderWatertPatch
{
    static void Postfix(WearNTear __instance, ref bool __result)
    {
        ResinProtection? resinProtection = ResinProtection.GetResinProtection(__instance.gameObject);
        if (resinProtection == null) return;
        if (resinProtection.GetTar() > 0.0)
            __result = false;
    }
}

[HarmonyPatch(typeof(WearNTear), nameof(WearNTear.HaveRoof))]
public static class RemoveWearNTear
{
    private static void Postfix(WearNTear __instance, ref bool __result)
    {
        ResinProtection? resinProtection = ResinProtection.GetResinProtection(__instance.gameObject);
        if (resinProtection == null) return;
        if (resinProtection.GetTar() > 0.0)
            __result = true;
    }
}

[HarmonyPatch(typeof(WearNTear), nameof(WearNTear.ApplyDamage))]
static class WearNTearApplyDamagePatch
{
    static void Prefix(WearNTear __instance, ref float damage, HitData hitData = null)
    {
        ResinProtection? resinProtection = ResinProtection.GetResinProtection(__instance.gameObject);
        if (resinProtection == null) return;
        if (resinProtection.GetResin() > 0.0 && hitData == null)
        {
            float resinRatio = Mathf.Clamp01(resinProtection.GetResin() / resinProtection.m_maxResin);
            float damageBeforeResin = damage;
            damage *= 1.0f - resinRatio;
#if DEBUG
            ResinGuardPlugin.ResinGuardLogger.LogDebug($"Damage before resin: {damageBeforeResin}, Damage after resin: {damage}");
#endif
        }
    }
}

[HarmonyPatch(typeof(WearNTear), nameof(WearNTear.OnDestroy))]
static class WearNTearOnDestroyPatch
{
    static void Postfix(WearNTear __instance)
    {
        if (ResinProtection.CachedComponents.ContainsKey(__instance.gameObject))
        {
            ResinProtection.CachedComponents.Remove(__instance.gameObject);
        }
    }
}

public class ResinProtection : MonoBehaviour, Hoverable, Interactable
{
    public const string ResinTokenName = "$item_resin";
    public const string TarTokenName = "$item_tar";
    public HashSet<string> m_resinItems = [ResinTokenName, TarTokenName];
    public HashSet<ItemDrop> m_resinItemDrops = [];
    public Action m_onProtectionAdded = null!;
    public Piece m_piece = null!;
    public WearNTear m_wearNTear = null!;
    public ZNetView m_nview = null!;
    public static readonly int s_Resin = "resinProtection".GetStableHashCode();
    public static readonly int s_Tar = "tarProtection".GetStableHashCode();
    public static readonly int s_TotalProtection = "totalProtection".GetStableHashCode();
    public int m_maxResin = ResinGuardPlugin.MaxResin.Value;
    public int m_maxTar = 1;
    public readonly StringBuilder HoverTextBuilder = new();
    public readonly Dictionary<Renderer, Color> OriginalColors = new();
    private const float UpdateInterval = 2f;
    internal static readonly Dictionary<GameObject, ResinProtection> CachedComponents = new();


    public void Awake()
    {
        m_nview = GetComponent<ZNetView>();
        if (m_nview == null)
            m_nview = GetComponentInParent<ZNetView>();
        m_wearNTear = GetComponent<WearNTear>();
        m_piece = GetComponent<Piece>();
        if (m_nview.GetZDO() == null || m_piece == null || m_wearNTear == null)
            return;
        m_nview.Register("RPC_AddProtectionItem", new Action<long, string>(RPC_AddProtectionItem));
        m_wearNTear.m_onDestroyed += OnDestroyed;
        m_onProtectionAdded += OnProtectionAdded;

        foreach (string resinItem in m_resinItems)
        {
            ItemDrop? item = ObjectDB.instance.m_items.FirstOrDefault(x => x.GetComponent<ItemDrop>().m_itemData.m_shared.m_name == resinItem)?.GetComponent<ItemDrop>();
            if (item != null)
                m_resinItemDrops.Add(item);
        }

        if (m_wearNTear != null)
        {
            m_wearNTear.m_propertyBlock ??= new MaterialPropertyBlock();
            foreach (Renderer? renderer in m_wearNTear.m_renderers)
            {
                renderer.GetPropertyBlock(m_wearNTear.m_propertyBlock);

                int colorPropertyID = Shader.PropertyToID("_Color");
                Color originalColor = m_wearNTear.m_propertyBlock.GetColor(colorPropertyID);
                OriginalColors.Add(renderer, originalColor);
            }
        }

        InvokeRepeating(nameof(UpdateVisualAppearance), 0.0f, UpdateInterval);
    }

    public void Update()
    {
        if (!(GetResin() > 0)) return;
        float decayAmount = Time.deltaTime / ResinGuardPlugin.DecayTime.Value; // 1 hour default
        SetResin(Mathf.Max(0, GetResin() - decayAmount), true);
    }

    public string GetHoverText()
    {
        if (m_nview.GetZDO() == null || m_piece == null || m_wearNTear == null)
            return string.Empty;
        HoverTextBuilder.Clear();
        HoverTextBuilder.Append(Localization.instance.Localize(m_piece.m_name));

        // Check and display resin status
        if (GetResin() >= m_maxResin)
            HoverTextBuilder.Append($" {Environment.NewLine}{ResinTokenName}: ($msg_itsfull)");
        else
        {
            HoverTextBuilder.Append(GetResin() > 0 ? $" {Environment.NewLine}{ResinTokenName}: ({Mathf.Ceil(GetResin())}/{m_maxResin})" : $" {Environment.NewLine}{ResinTokenName}: (0/{m_maxResin})");
        }

        // Check and display tar status
        HoverTextBuilder.Append(GetTar() > 0 ? $" {Environment.NewLine}$se_tared_name: ($piece_guardstone_active)" : $" {Environment.NewLine}$se_tared_name: ($piece_guardstone_inactive)");
#if DEBUG
        // Add m_health value
        hoverTextBuilder.Append($"\nHealth: {m_wearNTear.m_health}\n ZDOHealth: {m_wearNTear.m_nview.GetZDO().GetFloat(ZDOVars.s_health)}\n Health Percentage: {m_wearNTear.GetHealthPercentage()}");
#endif

        return Localization.instance.Localize(HoverTextBuilder.ToString());
    }

    public string GetHoverName() => string.Empty;

    public bool Interact(Humanoid user, bool hold, bool alt) => false;


    public bool UseItem(Humanoid user, ItemDrop.ItemData item)
    {
        if (item == null) return false;
        if (!m_resinItems.Contains(item.m_shared.m_name))
        {
            if (ResinGuardPlugin.ShowWrongItemMessage.Value == ResinGuardPlugin.Toggle.On)
            {
                if (item.m_shared.m_buildPieces == null || item.m_shared.m_buildPieces.m_pieces.Count == 0) // Eliminate hammers from seeing this message. It's highly annoying when going to repair the piece.
                    user.Message(MessageHud.MessageType.Center, "$msg_wrongitem");
            }

            return false;
        }

        if ((GetResin() > (double)(m_maxResin - 1)) && item.m_shared.m_name == m_resinItems.First())
        {
            user.Message(MessageHud.MessageType.Center, $"{ResinTokenName} $msg_itsfull");
            return false;
        }

        if ((GetTar() > (double)(m_maxTar - 1)) && item.m_shared.m_name == m_resinItems.Last())
        {
            user.Message(MessageHud.MessageType.Center, $"{TarTokenName} $msg_itsfull");
            return false;
        }

        user.Message(MessageHud.MessageType.Center, "$msg_added " + item.m_shared.m_name);
        user.GetInventory().RemoveItem(item.m_shared.m_name, 1);
        m_nview.InvokeRPC(ZNetView.Everybody, "RPC_AddProtectionItem", item.m_shared.m_name);
        return true;
    }

    public void RPC_AddProtectionItem(long sender, string itemName)
    {
        if (!m_nview.IsOwner())
            return;
        switch (itemName)
        {
            case $"{ResinTokenName}":
                SetResin(GetResin() + 1f);
                break;
            case $"{TarTokenName}":
                SetTar(GetTar() + 1f);
                break;
        }
    }

    public void SetResin(float resin, bool isUpdate = false)
    {
        if (!m_nview.IsValid())
            return;
        m_nview.GetZDO().Set(s_Resin, resin);
        m_nview.GetZDO().Set(s_TotalProtection, resin + GetTar());
        if (!isUpdate)
            m_onProtectionAdded?.Invoke();
    }

    public void SetTar(float resin)
    {
        if (!m_nview.IsValid())
            return;
        m_nview.GetZDO().Set(s_Tar, resin);
        m_nview.GetZDO().Set(s_TotalProtection, resin + GetResin());
        m_onProtectionAdded?.Invoke();
    }

    public float GetResin()
    {
        return !m_nview.IsValid() ? 0.0f : m_nview.GetZDO().GetFloat(s_Resin);
    }

    public float GetTar()
    {
        return !m_nview.IsValid() ? 0.0f : m_nview.GetZDO().GetFloat(s_Tar);
    }

    public void OnProtectionAdded()
    {
        if (m_piece == null || m_piece.m_placeEffect == null)
            return;
        m_piece.m_placeEffect?.Create(transform.position, transform.rotation, transform, 1f);
        m_wearNTear.m_hitEffect.Create(transform.position, transform.rotation, transform, 1f);
        if (ResinGuardPlugin.RepairWhenProtectionApplied.Value == ResinGuardPlugin.Toggle.On)
            m_wearNTear.Repair();
        UpdateVisualAppearance();
    }

    public void OnDestroyed()
    {
        if (!m_nview.IsOwner())
            return;
        DropAllItems();
    }

    public void DropAllItems()
    {
        float numResin = m_nview.GetZDO() == null ? 0.0f : m_nview.GetZDO().GetFloat(s_Resin);
        float numTar = m_nview.GetZDO() == null ? 0.0f : m_nview.GetZDO().GetFloat(s_Tar);
        for (int index = 0; index < (int)numResin; ++index)
            ItemDrop.OnCreateNew(Instantiate<GameObject>(m_resinItemDrops.First().gameObject, transform.position + Vector3.up + UnityEngine.Random.insideUnitSphere * 0.3f, Quaternion.Euler(0.0f, UnityEngine.Random.Range(0, 360), 0.0f)));
        for (int index = 0; index < (int)numTar; ++index)
            ItemDrop.OnCreateNew(Instantiate<GameObject>(m_resinItemDrops.Last().gameObject, transform.position + Vector3.up + UnityEngine.Random.insideUnitSphere * 0.3f, Quaternion.Euler(0.0f, UnityEngine.Random.Range(0, 360), 0.0f)));
    }

    public void UpdateVisualAppearance()
    {
        if (ResinGuardPlugin.EnableVisualUpdates.Value == ResinGuardPlugin.Toggle.Off || !m_nview.IsValid()) return;

        // Get the current WearNTear component
        WearNTear? wearNTear = m_wearNTear;
        if (wearNTear == null) return;
        string? pieceName = Utils.GetPrefabName(transform.root.gameObject.name);
        // Ensure the material property block is initialized
        wearNTear.m_propertyBlock ??= new MaterialPropertyBlock();

        foreach (Renderer? renderer in m_wearNTear.m_renderers)
        {
            if (!renderer.material.HasProperty("_Color")) continue;
            renderer.GetPropertyBlock(m_wearNTear.m_propertyBlock);
            float resinRatio = IsPieceExcluded(pieceName, isResin: true) ? 0 : Mathf.Clamp01(GetResin() / m_maxResin);
            float tarRatio = IsPieceExcluded(pieceName, isResin: false) ? 0 : Mathf.Clamp01(GetTar() / m_maxTar);


            Color originalColor = OriginalColors[renderer];
            if (originalColor == Color.clear)
                originalColor = renderer.material.color;
            Color resinColor = Color.Lerp(originalColor, ResinGuardPlugin.ResinColor.Value, resinRatio);
            Color tarColor = Color.Lerp(originalColor, ResinGuardPlugin.TarColor.Value, tarRatio);

            // Blend both colors based on their ratios
            Color finalColor = Color.Lerp(resinColor, tarColor, tarRatio / (resinRatio + tarRatio + 0.01f)); // Avoid division by zero
            finalColor.a = originalColor.a;


            m_wearNTear.m_propertyBlock.SetColor("_Color", finalColor);


            if (renderer.material.HasProperty("_Color"))
                m_wearNTear.m_propertyBlock.SetFloat("_Emission", tarRatio);

            renderer.SetPropertyBlock(m_wearNTear.m_propertyBlock);
        }
    }

    public static void ForceUpdateVisuals()
    {
        foreach (WearNTear wearNTear in WearNTear.s_allInstances)
        {
            wearNTear.gameObject.TryGetComponent(out ResinProtection resinProtection);
            resinProtection?.UpdateVisualAppearance();
        }
    }

    public static void UpdateProtectionValues()
    {
        foreach (WearNTear wearNTear in WearNTear.s_allInstances)
        {
            wearNTear.gameObject.TryGetComponent(out ResinProtection resinProtection);
            if (resinProtection == null)
                continue;
            resinProtection.m_maxResin = ResinGuardPlugin.MaxResin.Value;
        }
    }

    public static ResinProtection? GetResinProtection(GameObject gameObject)
    {
        if (CachedComponents.TryGetValue(gameObject, out ResinProtection? resinProtection)) return resinProtection;
        resinProtection = gameObject.GetComponent<ResinProtection>();
        if (resinProtection != null)
        {
            CachedComponents[gameObject] = resinProtection;
        }

        return resinProtection;
    }

    public static bool IsPieceExcluded(string pieceName, bool isResin)
    {
        return isResin ? ResinGuardPlugin.excludedResinPieces.Contains(pieceName) : ResinGuardPlugin.excludedTarPieces.Contains(pieceName);
    }
}