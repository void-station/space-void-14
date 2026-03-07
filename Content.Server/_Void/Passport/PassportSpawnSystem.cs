using Content.Server.Humanoid.Components;
using Content.Server.Humanoid.Systems;
using Content.Shared.Body;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Inventory;
using Content.Shared.NPC.Systems;
using Content.Shared._Void.Passport;
using Content.Shared.Roles;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Void.Passport;

/// <summary>
/// Spawns a passport on players and NPC humanoids, filling in their profile data.
/// </summary>
public sealed class PassportSpawnSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedStorageSystem _storage = default!;
    [Dependency] private readonly PassportSystem _passport = default!;
    [Dependency] private readonly NpcFactionSystem _faction = default!;
    [Dependency] private readonly SharedVisualBodySystem _visualBody = default!;

    private const string FallbackPassportProto = "Passport";
    private const string SyndicateFaction = "Syndicate";

    // English set (rare)
    private static readonly string[] EnglishOutfitOuter = { "ClothingOuterCoatEnglishRed" };
    private static readonly string[] EnglishOutfitUniform = { "ClothingUniformJumpsuitEnglishRed" };
    private static readonly string[] EnglishOutfitNeck = { "ClothingNeckCloakEnglishRed" };
    private static readonly string[] EnglishOutfitHead = { "ClothingHeadEnglishHatRed" };

    // Casual jumpsuits (male)
    private static readonly string[] CasualJumpsuitsMale =
    {
        "ClothingUniformJumpsuitCasualBlue",
        "ClothingUniformJumpsuitCasualPurple",
        "ClothingUniformJumpsuitCasualRed",
        "ClothingUniformJumpsuitCasualGreen",
    };

    // Casual jumpskirts (female)
    private static readonly string[] CasualJumpsuitsFemale =
    {
        "ClothingUniformJumpskirtCasualBlue",
        "ClothingUniformJumpskirtCasualPurple",
        "ClothingUniformJumpskirtCasualRed",
        "ClothingUniformJumpskirtCasualGreen",
    };

    // Shoes
    private static readonly string[] Shoes =
    {
        "ClothingShoesColorBlack",
        "ClothingShoesColorWhite",
        "ClothingShoesColorBlue",
        "ClothingShoesColorBrown",
        "ClothingShoesColorGreen",
        "ClothingShoesColorRed",
        "ClothingShoesColorPurple",
    };

    // Scarfs (non-syndie)
    private static readonly string[] Scarfs =
    {
        "ClothingNeckScarfStripedRed",
        "ClothingNeckScarfStripedBlue",
        "ClothingNeckScarfStripedGreen",
        "ClothingNeckScarfStripedBlack",
        "ClothingNeckScarfStripedBrown",
        "ClothingNeckScarfStripedLightBlue",
        "ClothingNeckScarfStripedOrange",
        "ClothingNeckScarfStripedPurple",
    };

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HumanoidProfileComponent, StartingGearEquippedEvent>(OnPlayerGearEquipped);
        SubscribeLocalEvent<HumanoidProfileComponent, MapInitEvent>(OnNpcMapInit,
            after: new[] { typeof(RandomHumanoidAppearanceSystem) });
    }

    private void OnPlayerGearEquipped(Entity<HumanoidProfileComponent> ent, ref StartingGearEquippedEvent args)
    {
        SpawnPassportOnEntity(ent);
    }

    private void OnNpcMapInit(Entity<HumanoidProfileComponent> ent, ref MapInitEvent args)
    {
        // Only spawn passports on NPC humanoids (those with RandomHumanoidAppearanceComponent).
        // Players get theirs via StartingGearEquippedEvent.
        if (!HasComp<RandomHumanoidAppearanceComponent>(ent))
            return;

        SpawnPassportOnEntity(ent);
    }

    private void SpawnPassportOnEntity(EntityUid entity)
    {
        // Prevent duplicate passports
        if (EntityAlreadyHasPassport(entity))
            return;

        var data = GatherPassportData(entity);

        // Pick the passport entity prototype from the citizenship
        var passportProto = FallbackPassportProto;
        if (data.Citizenship != null && _proto.TryIndex(data.Citizenship.Value, out var citizenshipProto))
            passportProto = citizenshipProto.PassportEntity;

        var passportEntity = Spawn(passportProto, Transform(entity).Coordinates);

        if (!TryComp<PassportComponent>(passportEntity, out var passport))
            return;

        // Create a photo dummy with casual clothes
        var photoEntity = CreatePhotoDummy(entity);

        _passport.SetPassportData(
            (passportEntity, passport),
            entity,
            photoEntity,
            data.FirstName, data.Surname,
            data.Weight, data.Height, data.Age,
            data.Citizenship, data.BirthPlace);

        // Try to put it in a pocket first, then backpack
        if (!TryInsertIntoInventoryStorage(entity, passportEntity, "pocket1") &&
            !TryInsertIntoInventoryStorage(entity, passportEntity, "pocket2") &&
            !TryInsertIntoEquipmentStorage(entity, passportEntity, "back") &&
            !TryInsertIntoEquipmentStorage(entity, passportEntity, "bag"))
        {
            _inventory.TryEquip(entity, passportEntity, "pocket1", silent: true, force: true);
        }
    }

    /// <summary>
    /// Creates a dummy entity that looks like the owner in randomized casual clothing.
    /// Used as the passport "photo".
    /// </summary>
    private EntityUid CreatePhotoDummy(EntityUid owner)
    {
        if (!TryComp<HumanoidProfileComponent>(owner, out var profileComp) ||
            !_proto.TryIndex<SpeciesPrototype>(profileComp.Species, out var species))
        {
            // Fallback: just reference the owner directly
            return owner;
        }

        var dummy = Spawn(species.DollPrototype, MapCoordinates.Nullspace);

        // Copy the body appearance (hair, skin color, markings, etc.)
        _visualBody.CopyAppearanceFrom(owner, dummy);

        // Dress in random casual clothes
        DressPhotoDummy(dummy, profileComp.Sex);

        return dummy;
    }

    private void DressPhotoDummy(EntityUid dummy, Sex sex)
    {
        // 5% chance for the rare English set
        if (_random.Prob(0.05f))
        {
            EquipOnDummy(dummy, _random.Pick(EnglishOutfitUniform), "jumpsuit");
            EquipOnDummy(dummy, _random.Pick(EnglishOutfitOuter), "outerClothing");
            EquipOnDummy(dummy, _random.Pick(EnglishOutfitNeck), "neck");
            EquipOnDummy(dummy, _random.Pick(EnglishOutfitHead), "head");
            EquipOnDummy(dummy, _random.Pick(Shoes), "shoes");
            return;
        }

        // Normal casual outfit
        var jumpsuit = sex == Sex.Female
            ? _random.Pick(CasualJumpsuitsFemale)
            : _random.Pick(CasualJumpsuitsMale);

        EquipOnDummy(dummy, jumpsuit, "jumpsuit");
        EquipOnDummy(dummy, _random.Pick(Shoes), "shoes");

        // 30% chance for a scarf
        if (_random.Prob(0.30f))
            EquipOnDummy(dummy, _random.Pick(Scarfs), "neck");
    }

    private void EquipOnDummy(EntityUid dummy, string protoId, string slot)
    {
        var item = Spawn(protoId, MapCoordinates.Nullspace);
        if (!_inventory.TryEquip(dummy, item, slot, silent: true, force: true))
            QueueDel(item);
    }

    private bool EntityAlreadyHasPassport(EntityUid entity)
    {
        if (!_inventory.TryGetSlots(entity, out var slots))
            return false;

        foreach (var slot in slots)
        {
            if (!_inventory.TryGetSlotEntity(entity, slot.Name, out var slotEntity))
                continue;

            // Check if the slot item itself is a passport
            if (HasComp<PassportComponent>(slotEntity))
                return true;

            // Check inside storage containers (bags, pockets with items)
            if (!TryComp<StorageComponent>(slotEntity, out var storage))
                continue;

            foreach (var stored in storage.Container.ContainedEntities)
            {
                if (HasComp<PassportComponent>(stored))
                    return true;
            }
        }

        return false;
    }

    private (string FirstName, string Surname, int Age, float Height, float Weight,
        ProtoId<CitizenshipPrototype>? Citizenship, string? BirthPlace) GatherPassportData(EntityUid entity)
    {
        var name = MetaData(entity).EntityName;
        var parts = name.Split(' ', 2);
        var firstName = parts[0];
        var surname = parts.Length > 1 ? parts[1] : "";

        var age = 25;
        var height = 176f;
        var weight = 70f;
        ProtoId<CitizenshipPrototype>? citizenship = null;
        string? birthPlace = null;

        if (TryComp<HumanoidProfileComponent>(entity, out var profileComp))
        {
            age = profileComp.Age;

            var profileHeight = profileComp.Height > 0 ? profileComp.Height : 1f;
            var profileWidth = profileComp.Width > 0 ? profileComp.Width : 1f;

            if (_proto.TryIndex<SpeciesPrototype>(profileComp.Species, out var speciesProto))
            {
                height = speciesProto.AverageHeight * profileHeight;

                // Calculate weight from physics fixtures, same as character editor
                if (_proto.TryIndex(speciesProto.Prototype, out var entProto) &&
                    entProto.TryGetComponent<FixturesComponent>(out var fixture, EntityManager.ComponentFactory) &&
                    fixture.Fixtures.TryGetValue("fix1", out var fix1))
                {
                    var avg = (profileWidth + profileHeight) / 2;
                    weight = FixtureSystem.GetMassData(fix1.Shape, fix1.Density).Mass * avg;
                }
                else
                {
                    weight = 71f;
                }
            }

            // Use player's chosen citizenship if set
            if (!string.IsNullOrEmpty(profileComp.Citizenship))
            {
                citizenship = profileComp.Citizenship;
                birthPlace = !string.IsNullOrEmpty(profileComp.BirthPlace)
                    ? profileComp.BirthPlace
                    : null;
            }

            // Syndicate faction members get Syndicate citizenship
            if (citizenship == null &&
                _faction.IsMember(entity, SyndicateFaction))
            {
                if (_proto.TryIndex<CitizenshipPrototype>("Syndicate", out var syndicateProto))
                {
                    citizenship = syndicateProto.ID;
                    if (syndicateProto.BirthPlaces.Count > 0)
                        birthPlace = _random.Pick(syndicateProto.BirthPlaces);
                }
            }

            // If no citizenship set, pick a random valid one for the species (only player-selectable ones)
            if (citizenship == null)
            {
                foreach (var proto in _proto.EnumeratePrototypes<CitizenshipPrototype>())
                {
                    if (proto.PlayerSelectable && proto.AllowedSpecies.Contains(profileComp.Species))
                    {
                        citizenship = proto.ID;
                        if (proto.BirthPlaces.Count > 0)
                            birthPlace = _random.Pick(proto.BirthPlaces);
                        break;
                    }
                }
            }
        }

        return (firstName, surname, age, height, weight, citizenship, birthPlace);
    }

    /// <summary>
    /// Try to put item directly into an inventory slot (for pocket slots that accept items directly).
    /// </summary>
    private bool TryInsertIntoInventoryStorage(EntityUid entity, EntityUid item, string slotName)
    {
        if (!_inventory.TryGetSlotEntity(entity, slotName, out var slotEntity))
        {
            // Slot is empty, try to equip directly
            return _inventory.TryEquip(entity, item, slotName, silent: true, force: true);
        }

        // Slot has something, try its storage
        if (TryComp<StorageComponent>(slotEntity, out var storage))
            return _storage.Insert(slotEntity.Value, item, out _, storageComp: storage, playSound: false);

        return false;
    }

    /// <summary>
    /// Try to put item into the storage of an equipped item (e.g., backpack).
    /// </summary>
    private bool TryInsertIntoEquipmentStorage(EntityUid entity, EntityUid item, string slotName)
    {
        if (!_inventory.TryGetSlotEntity(entity, slotName, out var slotEntity))
            return false;

        if (!TryComp<StorageComponent>(slotEntity, out var storage))
            return false;

        return _storage.Insert(slotEntity.Value, item, out _, storageComp: storage, playSound: false);
    }
}
