using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._Starlight.Structures.TableFlip;

public sealed partial class SharedTableFlipSystem : EntitySystem
{
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    private static readonly SoundSpecifier _flipSound =
        new SoundCollectionSpecifier("MetalThud") { Params = AudioParams.Default.WithVariation(0.125f) };

    [SubscribeLocalEvent]
    private void OnGetFlipVerbs(Entity<TableFlippableComponent> table, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !args.CanComplexInteract)
            return;

        if (HasComp<FlippedTableComponent>(table))
            return;

        var user = args.User;
        args.Verbs.Add(new AlternativeVerb
        {
            Act = () => TryStartFlip(table.Owner, user, table.Comp.Delay),
            Text = Loc.GetString("table-flip-verb-flip"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/rotate_cw.svg.192dpi.png")),
            DoContactInteraction = true,
        });
    }

    [SubscribeLocalEvent]
    private void OnGetUnflipVerbs(Entity<FlippedTableComponent> table, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !args.CanComplexInteract)
            return;

        var user = args.User;
        args.Verbs.Add(new AlternativeVerb
        {
            Act = () => TryStartFlip(table.Owner, user, table.Comp.Delay),
            Text = Loc.GetString("table-flip-verb-unflip"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/rotate_ccw.svg.192dpi.png")),
            DoContactInteraction = true,
        });
    }

    private void TryStartFlip(EntityUid table, EntityUid user, TimeSpan delay)
        => _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, user, delay, new TableFlipDoAfterEvent(), table, target: table)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = true,
        });

    [SubscribeLocalEvent]
    private void OnFlipDoAfter(Entity<TableFlippableComponent> table, ref TableFlipDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        if (HasComp<FlippedTableComponent>(table))
            return;

        args.Handled = true;
        Swap(table.Owner, table.Comp.FlippedPrototype, args.User, faceUser: true);
    }

    [SubscribeLocalEvent]
    private void OnUnflipDoAfter(Entity<FlippedTableComponent> table, ref TableFlipDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;
        Swap(table.Owner, table.Comp.UprightPrototype, args.User, faceUser: false);
    }

    private void Swap(EntityUid table, EntProtoId prototype, EntityUid user, bool faceUser)
    {
        var xform = Transform(table);
        var coordinates = xform.Coordinates;
        var wasAnchored = xform.Anchored;

        DamageSpecifier? damage = null;
        if (TryComp<DamageableComponent>(table, out var oldDamage))
            damage = new DamageSpecifier(oldDamage.Damage);

        var rotation = faceUser
            ? GetRotationTowards(table, user, xform)
            : Angle.Zero;

        _audio.PlayPredicted(_flipSound, table, user);

        PredictedDel(table);

        var replacement = PredictedSpawnAtPosition(prototype, coordinates);
        _transform.SetLocalRotation(replacement, rotation);

        var transform = Transform(replacement);
        if (!transform.Anchored && wasAnchored)
            _transform.AnchorEntity(replacement, transform);
        else if (transform.Anchored && !wasAnchored)
            _transform.Unanchor(replacement);

        if (damage != null && HasComp<DamageableComponent>(replacement))
            _damageable.SetDamage(replacement, damage);
    }

    private Angle GetRotationTowards(EntityUid table, EntityUid user, TransformComponent xform)
    {
        var tablePos = _transform.GetMapCoordinates(table);
        var userPos = _transform.GetMapCoordinates(user);

        if (tablePos.MapId != userPos.MapId)
            return Angle.Zero;

        var offset = userPos.Position - tablePos.Position;

        if (offset.LengthSquared() < 0.01f)
            return Angle.Zero;

        var worldAngle = offset.ToWorldAngle();
        var parentRotation = _transform.GetWorldRotation(xform) - xform.LocalRotation;

        return (worldAngle - parentRotation).GetCardinalDir().ToAngle();
    }
}

[Serializable, NetSerializable]
public sealed partial class TableFlipDoAfterEvent : SimpleDoAfterEvent;
