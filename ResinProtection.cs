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
        __instance.gameObject.TryGetComponent(out ResinProtection resinProtection);
        if (resinProtection == null) __instance.gameObject.AddComponent<ResinProtection>();

        // Update health based on current resin level
        UpdateHealth(__instance, resinProtection);
    }

    private static void UpdateHealth(WearNTear instance, ResinProtection? resinProtection)
    {
        if (resinProtection != null && resinProtection.GetResin() > 0.0)
        {
            // Calculate the new health as a linear interpolation between the original health and double the health
            instance.m_health = resinProtection.originalHealth * (1.0f + resinProtection.GetResin() / resinProtection.m_maxResin);
        }
    }
}

[HarmonyPatch(typeof(WearNTear), nameof(WearNTear.IsWet))]
static class WearNTearIsWetPatch
{
    static void Postfix(WearNTear __instance, ref bool __result)
    {
        __instance.gameObject.TryGetComponent(out ResinProtection resinProtection);
        if (resinProtection == null)
            return;
        if (resinProtection.GetTar() > 0.0)
            __result = false;
    }
}

[HarmonyPatch(typeof(WearNTear), nameof(WearNTear.IsUnderWater))]
static class WearNTearIsUnderWatertPatch
{
    static void Postfix(WearNTear __instance, ref bool __result)
    {
        __instance.gameObject.TryGetComponent(out ResinProtection resinProtection);
        if (resinProtection == null)
            return;
        if (resinProtection.GetTar() > 0.0)
            __result = false;
    }
}

[HarmonyPatch(typeof(WearNTear), nameof(WearNTear.HaveRoof))]
public static class RemoveWearNTear
{
    private static void Postfix(WearNTear __instance, ref bool __result)
    {
        __instance.gameObject.TryGetComponent(out ResinProtection resinProtection);
        if (resinProtection == null)
            return;
        if (resinProtection.GetTar() > 0.0)
            __result = true;
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
    public int m_maxResin = 10;
    public int m_maxTar = 1;
    public float originalHealth;
    public StringBuilder hoverTextBuilder = new();

    public void Awake()
    {
        m_nview = GetComponent<ZNetView>();
        if (m_nview == null)
            m_nview = GetComponentInParent<ZNetView>();
        m_wearNTear = GetComponent<WearNTear>();
        originalHealth = m_wearNTear.m_health;
        m_piece = GetComponent<Piece>();
        if (m_nview.GetZDO() == null)
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
    }

    public void Update()
    {
        if (!(GetResin() > 0)) return;
        float decayAmount = Time.deltaTime / ResinGuardPlugin.DecayTime.Value; // 1 hour default
        SetResin(Mathf.Max(0, GetResin() - decayAmount), true);
    }

    public string GetHoverText()
    {
        hoverTextBuilder.Clear();
        hoverTextBuilder.Append(Localization.instance.Localize(m_piece.m_name));

        // Check and display resin status
        if (GetResin() >= m_maxResin)
            hoverTextBuilder.Append($" {Environment.NewLine}{ResinTokenName}: ($msg_itsfull)");
        else
        {
            hoverTextBuilder.Append(GetResin() > 0 ? $" {Environment.NewLine}{ResinTokenName}: ({Mathf.Ceil(GetResin())}/{m_maxResin})" : $" {Environment.NewLine}{ResinTokenName}: (0/{m_maxResin})");
        }

        // Check and display tar status
        hoverTextBuilder.Append(GetTar() > 0 ? $" {Environment.NewLine}$se_tared_name: ($piece_guardstone_active)" : $" {Environment.NewLine}$se_tared_name: ($piece_guardstone_inactive)");
#if DEBUG
        // Add m_health value
        hoverTextBuilder.Append($"\nHealth: {m_wearNTear.m_health}\n ZDOHealth: {m_wearNTear.m_nview.GetZDO().GetFloat(ZDOVars.s_health)}\n Health Percentage: {m_wearNTear.GetHealthPercentage()}");
#endif

        return Localization.instance.Localize(hoverTextBuilder.ToString());
    }

    public string GetHoverName() => string.Empty;

    public bool Interact(Humanoid user, bool hold, bool alt) => false;


    public bool UseItem(Humanoid user, ItemDrop.ItemData item)
    {
        if (item == null) return false;
        if (!m_resinItems.Contains(item.m_shared.m_name) && item.m_shared.m_name != "$item_hammer")
        {
            user.Message(MessageHud.MessageType.Center, "$msg_wrongitem");
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
        m_nview.InvokeRPC("RPC_AddProtectionItem", item.m_shared.m_name);
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
                m_wearNTear.m_health = originalHealth * (1.0f + GetResin() / m_maxResin);
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
        else
        {
            m_wearNTear.m_health = originalHealth * (1.0f + GetResin() / m_maxResin);
        }
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
        m_wearNTear.Repair();
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
}