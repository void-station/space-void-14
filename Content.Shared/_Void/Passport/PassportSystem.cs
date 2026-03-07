using Content.Shared.Examine;
using Content.Shared.UserInterface;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using static Content.Shared._Void.Passport.PassportComponent;

namespace Content.Shared._Void.Passport;

public sealed class PassportSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PassportComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<PassportComponent, ActivatableUIOpenAttemptEvent>(OnUIOpenAttempt);
        SubscribeLocalEvent<PassportComponent, BeforeActivatableUIOpenEvent>(BeforeUIOpen);
        SubscribeLocalEvent<PassportComponent, BoundUIClosedEvent>(OnUIClosed);
    }

    private void OnUIOpenAttempt(Entity<PassportComponent> ent, ref ActivatableUIOpenAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        // Opening the passport and showing the UI in one action.
        SetOpen(ent, true);
    }

    private void BeforeUIOpen(Entity<PassportComponent> ent, ref BeforeActivatableUIOpenEvent args)
    {
        UpdateUserInterface(ent);
    }

    private void OnUIClosed(Entity<PassportComponent> ent, ref BoundUIClosedEvent args)
    {
        SetOpen(ent, false);
    }

    private void SetOpen(Entity<PassportComponent> ent, bool open)
    {
        if (ent.Comp.IsOpen == open)
            return;

        ent.Comp.IsOpen = open;
        Dirty(ent);

        if (TryComp<AppearanceComponent>(ent, out var appearance))
            _appearance.SetData(ent, PassportVisuals.Open, ent.Comp.IsOpen, appearance);
    }

    private void OnExamined(Entity<PassportComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        using (args.PushGroup(nameof(PassportComponent)))
        {
            if (!ent.Comp.IsOpen)
            {
                var citizenshipName = GetCitizenshipName(ent.Comp.Citizenship);
                args.PushMarkup(Loc.GetString("passport-examine-closed", ("citizenship", citizenshipName)));
                return;
            }

            var name = ent.Comp.OwnerName ?? Loc.GetString("passport-unknown");
            var surname = ent.Comp.OwnerSurname ?? Loc.GetString("passport-unknown");
            var age = PassportComponent.CurrentYear - ent.Comp.BirthYear;
            var citizenship = GetCitizenshipName(ent.Comp.Citizenship);
            var birthPlace = ent.Comp.BirthPlace ?? Loc.GetString("passport-unknown");
            var height = ent.Comp.Height;
            var weight = ent.Comp.Weight;

            args.PushMarkup(Loc.GetString("passport-examine-open-name", ("name", name), ("surname", surname)));
            args.PushMarkup(Loc.GetString("passport-examine-open-age", ("age", age)));
            args.PushMarkup(Loc.GetString("passport-examine-open-height", ("height", $"{height:F1}")));
            args.PushMarkup(Loc.GetString("passport-examine-open-weight", ("weight", $"{weight:F1}")));
            args.PushMarkup(Loc.GetString("passport-examine-open-citizenship", ("citizenship", citizenship)));
            args.PushMarkup(Loc.GetString("passport-examine-open-birthplace", ("birthplace", birthPlace)));
        }
    }

    private string GetCitizenshipName(ProtoId<CitizenshipPrototype>? citizenshipId)
    {
        if (citizenshipId == null)
            return Loc.GetString("passport-unknown");

        if (_proto.TryIndex(citizenshipId.Value, out var proto))
            return proto.Name;

        return citizenshipId.Value;
    }

    public void UpdateUserInterface(Entity<PassportComponent> ent)
    {
        var state = new PassportBoundUserInterfaceState(
            ent.Comp.OwnerName,
            ent.Comp.OwnerSurname,
            ent.Comp.Weight,
            ent.Comp.Height,
            ent.Comp.BirthYear,
            GetCitizenshipName(ent.Comp.Citizenship),
            ent.Comp.BirthPlace,
            ent.Comp.PhotoEntity);

        _uiSystem.SetUiState(ent.Owner, PassportUiKey.Key, state);
    }

    /// <summary>
    /// Sets passport data from a character profile.
    /// </summary>
    public void SetPassportData(
        Entity<PassportComponent> ent,
        EntityUid owner,
        EntityUid photoEntity,
        string? firstName,
        string? surname,
        float weight,
        float height,
        int age,
        ProtoId<CitizenshipPrototype>? citizenship,
        string? birthPlace)
    {
        ent.Comp.OwnerEntity = GetNetEntity(owner);
        ent.Comp.PhotoEntity = GetNetEntity(photoEntity);
        ent.Comp.OwnerName = firstName;
        ent.Comp.OwnerSurname = surname;
        ent.Comp.Weight = weight;
        ent.Comp.Height = height;
        ent.Comp.BirthYear = PassportComponent.CurrentYear - age;
        ent.Comp.Citizenship = citizenship;
        ent.Comp.BirthPlace = birthPlace;

        // Store the photo dummy inside the passport so it gets networked to clients via PVS.
        var container = _container.EnsureContainer<ContainerSlot>(ent, PassportComponent.PhotoContainerId);
        _container.Insert(photoEntity, container);

        Dirty(ent);
    }
}
