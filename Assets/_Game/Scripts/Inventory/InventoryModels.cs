using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public enum ItemId
{
    None = 0,
    HealthPotion = 1,
    WadoIchimonji = 2,
    PirateBandana = 3,
    MarineCoat = 4,
    SwordsmanPants = 5,
    SwordsmanBoots = 6,
    KizaruLightSword = 7,
    LightRing = 8
}

public enum ItemKind
{
    Consumable,
    Equipment
}

public enum ItemRarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary
}

public enum EquipmentSlot
{
    Weapon = 0,
    Head = 1,
    Body = 2,
    Legs = 3,
    Feet = 4,
    Accessory = 5
}

public sealed class ItemDefinition
{
    public ItemId Id { get; }
    public string DisplayName { get; }
    public string Description { get; }
    public ItemKind Kind { get; }
    public int Level { get; }
    public ItemRarity Rarity { get; }
    public EquipmentSlot EquipmentSlot { get; }
    public int MaxStack { get; }
    public int Strength { get; }
    public int Vitality { get; }
    public int Agility { get; }
    public int Energy { get; }
    public int HealAmount { get; }

    public ItemDefinition(
        ItemId id,
        string displayName,
        string description,
        ItemKind kind,
        int level = 1,
        ItemRarity rarity = ItemRarity.Common,
        EquipmentSlot equipmentSlot = EquipmentSlot.Weapon,
        int maxStack = 1,
        int strength = 0,
        int vitality = 0,
        int agility = 0,
        int energy = 0,
        int healAmount = 0)
    {
        Id = id;
        DisplayName = displayName;
        Description = description;
        Kind = kind;
        Level = Mathf.Max(1, level);
        Rarity = rarity;
        EquipmentSlot = equipmentSlot;
        MaxStack = Mathf.Max(1, maxStack);
        Strength = strength;
        Vitality = vitality;
        Agility = agility;
        Energy = energy;
        HealAmount = healAmount;
    }
}

public static class ItemCatalog
{
    private static readonly Dictionary<ItemId, ItemDefinition> Items = new()
    {
        [ItemId.HealthPotion] = new ItemDefinition(
            ItemId.HealthPotion, "Bình Máu", "Hồi 50 HP.", ItemKind.Consumable,
            level: 1, rarity: ItemRarity.Common, maxStack: 20, healAmount: 50),
        [ItemId.WadoIchimonji] = new ItemDefinition(
            ItemId.WadoIchimonji, "Wado Ichimonji", "+8 Strength.", ItemKind.Equipment,
            level: 5, rarity: ItemRarity.Rare, equipmentSlot: EquipmentSlot.Weapon, strength: 8),
        [ItemId.PirateBandana] = new ItemDefinition(
            ItemId.PirateBandana, "Khăn Hải Tặc", "+3 Strength, +2 Agility.", ItemKind.Equipment,
            level: 2, rarity: ItemRarity.Uncommon, equipmentSlot: EquipmentSlot.Head, strength: 3, agility: 2),
        [ItemId.MarineCoat] = new ItemDefinition(
            ItemId.MarineCoat, "Áo Hải Quân", "+5 Vitality.", ItemKind.Equipment,
            level: 4, rarity: ItemRarity.Rare, equipmentSlot: EquipmentSlot.Body, vitality: 5),
        [ItemId.SwordsmanPants] = new ItemDefinition(
            ItemId.SwordsmanPants, "Quần Kiếm Sĩ", "+3 Vitality, +2 Energy.", ItemKind.Equipment,
            level: 3, rarity: ItemRarity.Uncommon, equipmentSlot: EquipmentSlot.Legs, vitality: 3, energy: 2),
        [ItemId.SwordsmanBoots] = new ItemDefinition(
            ItemId.SwordsmanBoots, "Giày Kiếm Sĩ", "+4 Agility.", ItemKind.Equipment,
            level: 3, rarity: ItemRarity.Uncommon, equipmentSlot: EquipmentSlot.Feet, agility: 4),
        [ItemId.KizaruLightSword] = new ItemDefinition(
            ItemId.KizaruLightSword, "Kiếm Ánh Sáng", "+15 Strength, +5 Agility.", ItemKind.Equipment,
            level: 12, rarity: ItemRarity.Legendary, equipmentSlot: EquipmentSlot.Weapon, strength: 15, agility: 5),
        [ItemId.LightRing] = new ItemDefinition(
            ItemId.LightRing, "Nhẫn Ánh Sáng", "+5 Energy, +3 Agility.", ItemKind.Equipment,
            level: 10, rarity: ItemRarity.Epic, equipmentSlot: EquipmentSlot.Accessory, agility: 3, energy: 5)
    };

    public static ItemDefinition Get(ItemId id)
    {
        Items.TryGetValue(id, out ItemDefinition definition);
        return definition;
    }
}

public struct InventorySlotState : INetworkSerializable, IEquatable<InventorySlotState>
{
    public int ItemIdValue;
    public int Quantity;
    public ItemId ItemId => (ItemId)ItemIdValue;
    public bool IsEmpty => ItemId == ItemId.None || Quantity <= 0;
    public static InventorySlotState Empty => new InventorySlotState(ItemId.None, 0);

    public InventorySlotState(ItemId itemId, int quantity)
    {
        ItemIdValue = (int)itemId;
        Quantity = Mathf.Max(0, quantity);
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ItemIdValue);
        serializer.SerializeValue(ref Quantity);
    }

    public bool Equals(InventorySlotState other)
    {
        return ItemIdValue == other.ItemIdValue && Quantity == other.Quantity;
    }
}

[Serializable]
public struct LootDrop
{
    public ItemId itemId;
    [Min(1)] public int minQuantity;
    [Min(1)] public int maxQuantity;
    [Range(0f, 1f)] public float dropChance;
}
