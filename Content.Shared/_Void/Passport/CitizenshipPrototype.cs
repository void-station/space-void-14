using Robust.Shared.Prototypes;

namespace Content.Shared._Void.Passport;

[Prototype]
public sealed partial class CitizenshipPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public string Name { get; private set; } = default!;

    /// <summary>
    /// Species allowed to hold this citizenship.
    /// </summary>
    [DataField(required: true)]
    public List<string> AllowedSpecies { get; private set; } = new();

    /// <summary>
    /// Possible birthplaces for this citizenship.
    /// </summary>
    [DataField(required: true)]
    public List<string> BirthPlaces { get; private set; } = new();

    /// <summary>
    /// The entity prototype to spawn as the ID document for this citizenship.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId PassportEntity { get; private set; } = "Passport";

    /// <summary>
    /// Whether this citizenship can be selected in the character editor.
    /// False for special citizenships like Syndicate that are only assigned by the system.
    /// </summary>
    [DataField]
    public bool PlayerSelectable { get; private set; } = true;
}
