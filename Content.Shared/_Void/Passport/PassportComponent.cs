using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Void.Passport;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class PassportComponent : Component
{
    public const int CurrentYear = 2566;
    public const string PhotoContainerId = "passport-photo";

    [DataField, AutoNetworkedField]
    public string? OwnerName;

    [DataField, AutoNetworkedField]
    public string? OwnerSurname;

    [DataField, AutoNetworkedField]
    public float Weight;

    [DataField, AutoNetworkedField]
    public float Height;

    [DataField, AutoNetworkedField]
    public int BirthYear;

    [DataField, AutoNetworkedField]
    public ProtoId<CitizenshipPrototype>? Citizenship;

    [DataField, AutoNetworkedField]
    public string? BirthPlace;

    /// <summary>
    /// The entity this passport belongs to.
    /// </summary>
    [DataField, AutoNetworkedField]
    public NetEntity OwnerEntity;

    /// <summary>
    /// A dummy entity used as the passport "photo" - shows the owner's appearance in casual clothes.
    /// </summary>
    [DataField, AutoNetworkedField]
    public NetEntity PhotoEntity;

    /// <summary>
    /// Whether this passport is currently open (showing detailed info).
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IsOpen;

    [Serializable, NetSerializable]
    public sealed class PassportBoundUserInterfaceState : BoundUserInterfaceState
    {
        public readonly string? OwnerName;
        public readonly string? OwnerSurname;
        public readonly float Weight;
        public readonly float Height;
        public readonly int BirthYear;
        public readonly string? Citizenship;
        public readonly string? BirthPlace;
        public readonly NetEntity PhotoEntity;

        public PassportBoundUserInterfaceState(
            string? ownerName,
            string? ownerSurname,
            float weight,
            float height,
            int birthYear,
            string? citizenship,
            string? birthPlace,
            NetEntity photoEntity)
        {
            OwnerName = ownerName;
            OwnerSurname = ownerSurname;
            Weight = weight;
            Height = height;
            BirthYear = birthYear;
            Citizenship = citizenship;
            BirthPlace = birthPlace;
            PhotoEntity = photoEntity;
        }
    }

    [Serializable, NetSerializable]
    public enum PassportUiKey : byte
    {
        Key
    }

    [Serializable, NetSerializable]
    public enum PassportVisuals : byte
    {
        Open
    }
}
