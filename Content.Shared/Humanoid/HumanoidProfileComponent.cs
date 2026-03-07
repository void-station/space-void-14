using Content.Shared._Erida.TTS;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences;
using Robust.Shared.Enums;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Humanoid;

/// <summary>
/// Dictates what species and age this character "looks like"
/// </summary>
[NetworkedComponent, RegisterComponent, AutoGenerateComponentState(true)]
[Access(typeof(HumanoidProfileSystem))]
public sealed partial class HumanoidProfileComponent : Component
{
    [DataField, AutoNetworkedField]
    public Gender Gender;

    [DataField, AutoNetworkedField]
    public Sex Sex;

    [DataField, AutoNetworkedField]
    public int Age = 18;

    [DataField, AutoNetworkedField]
    public ProtoId<SpeciesPrototype> Species = HumanoidCharacterProfile.DefaultSpecies;

    [DataField, AutoNetworkedField]
    public string CustomSpecies = string.Empty;

    // Corvax-TTS-Start
    /// <summary>
    ///     Current voice. Used for correct cloning.
    /// </summary>
    [DataField("voice")]
    public ProtoId<TTSVoicePrototype> Voice { get; set; } = HumanoidProfileSystem.DefaultVoice;
    // Corvax-TTS-End

    // begin Goobstation: port EE height/width sliders
    [DataField, AutoNetworkedField]
    public float Height { get; set; }

    [DataField, AutoNetworkedField]
    public float Width { get; set; }
    // end Goobstation: port EE height/width sliders

    [DataField, AutoNetworkedField]
    public string Citizenship { get; set; } = string.Empty;

    [DataField, AutoNetworkedField]
    public string BirthPlace { get; set; } = string.Empty;
}
