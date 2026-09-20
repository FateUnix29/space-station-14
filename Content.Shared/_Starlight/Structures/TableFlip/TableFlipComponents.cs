using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Starlight.Structures.TableFlip;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TableFlippableComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public EntProtoId FlippedPrototype;

    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(1.5);
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class FlippedTableComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public EntProtoId UprightPrototype;

    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(3);
}
