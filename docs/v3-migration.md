# Migrating to ObjectListView v3

[Documentation index](README.md) | [Implementation plan](v3-plan.md)

**V3 is still a work in progress.** This guide covers implemented API and platform
changes, not a completed release. Recompile consumers and update affected
overrides; v3 is not a drop-in binary replacement.

## Runtime targets, designers, and saved layouts

Runtime targets are **.NET Framework 4.6.2** and **.NET 10 for Windows**.
The .NET 10 runtime does not depend on System.Design or the Designer SDK.

Framework designer support is retained. Modern designer support is a preview
for Visual Studio 2026; install the NuGet package rather than referencing a
standalone DLL. Its Framework 4.7.2 designer client does not raise your
application's runtime requirement.

**Known limitation:** native header-divider dragging is not supported reliably
in the modern designer. Set widths through the Columns editor or Properties.
For ListViewPrinter, reference the matching runtime assembly from its normal
deployment location, not DLLs under the package's `Design\WinForms` directories.

`SaveState()` and `RestoreState(byte[])` keep their signatures. Both runtimes
use the same versioned layout format, but **old BinaryFormatter layouts cannot
be restored**. Discard those bytes and save a replacement.

`RestoreState` returns `false` for unsupported, malformed, or column-count-
mismatched layouts without changing the current layout. Null is an argument
error; errors applying a valid layout propagate.

```csharp
if (!list.RestoreState(savedLayout)) {
    // Notify the user and save a replacement layout when appropriate.
}
```

Layouts match columns by `AllColumns` position and do not store selection or
scroll position.

## DPI-aware hosts and column widths

Your application configures process DPI awareness; ObjectListView does not.
The demos use system awareness on Framework and `PerMonitorV2` on .NET 10.

Initial column widths and minimum/maximum widths are **96-DPI design values**.
ObjectListView scales them after initialization, including hidden and fixed-width
columns. Remove duplicate manual startup scaling. On .NET 10, monitor DPI changes
also rescale current user widths; recreating a handle at the same DPI does not
scale them again.

Widths assigned after initialization and handle creation, and widths in saved
layouts, remain device-pixel values. Saved layouts are not DPI-independent.
Framework system-aware windows may still be bitmap-scaled between monitors.

## Generic control hierarchy

`TypedObjectListView<T>` is now a control with owned `List<T>` storage, not a
wrapper. Both it and `ObjectListView` derive from `ObjectListViewBase`;
`ObjectListView` is the `<object>` specialization.

| Previous usage | Migration |
| --- | --- |
| `new TypedObjectListView<Person>(existingControl)` | Create `new TypedObjectListView<Person>()`, configure columns, and add it to the form |
| Access through `.ListView` on the old wrapper | Access the typed control's properties directly |
| Keep an existing control and add typed access | Use `new TypedObjectListViewAdapter<Person>(existingControl)` |
| Integrations accepting `ObjectListView` | Accept `ObjectListViewBase` to support typed controls |

The adapter does not create or dispose the adapted control. Its `ListView` is
read-only; create another adapter to adapt a different control.

```csharp
var people = new TypedObjectListViewAdapter<Person>(existingControl);
people.BooleanCheckStateGetter = person => person.IsActive;
people.CellToolTipGetter = (column, person) => person.Name;
people.GetColumn(0).AspectGetter = person => person.Name;
```

On a typed control, use `GetTypedColumn(...)` for typed column delegates and
`control.Typed` for typed callback configuration. `GetColumn(...)` and the
control's check-state/tooltip callbacks retain their shared object-based contracts.

For designer/toolbox use, declare a concrete, non-generic subclass in a named
namespace:

```csharp
public class PersonListView : TypedObjectListView<Person> { }
```

Fast, virtual, tree, and data-bound controls retain their specialized sources;
they are not converted into generic backing lists.

### Subclass extension points

- Update renderer, overlay, and drag/drop signatures to `ObjectListViewBase`.
  Shared untyped access includes `ModelObjects`, `GetModelAt`, and the
  `SelectedModelObject(s)` / `CheckedModelObject(s)` bridges.
- Generic `GetModelAt` and selection/check bridges are sealed. Existing
  `ObjectListView` subclasses should keep overriding `GetModelObject`,
  `SelectedObject(s)`, and `CheckedObject(s)` for legacy customization.
- Move source logic from public `Objects` overrides into `GetModelObjectsCore`
  and `SetModelObjectsCore`. **The UI no longer reads its source through the
  public `Objects` snapshot getter.**
- Typed enumerable commands are non-virtual entry points that dispatch through
  the existing untyped virtual pipeline. Update those overrides to `IEnumerable`.
- Custom storage must consistently implement its read and mutation hooks:
  `MaterializeModelObjects`, `SetStoredModelObjects`, `InsertStoredModelObjects`,
  `RemoveStoredModelObject`, and `ReplaceStoredModelObject`. Transferred batches
  are private snapshots; do not retain or mutate them outside storage.

Model lookup still uses `object.Equals`. Implementing only `IEquatable<T>` does
not change identity semantics.

## Changed contracts

Arrays and lists remain valid inputs. LINQ queries and iterators no longer need
caller-side `ToList()` or `ToArray()`.

| API | V3 contract |
| --- | --- |
| `AddObjects`, `InsertObjects`, `RemoveObjects` | `IEnumerable` instead of `ICollection` |
| `RefreshObjects`, `SelectObjects`, `CopyObjectsToClipboard` | `IEnumerable` instead of `IList` |
| `SetSelectedObjects`, `SetCheckedObjects` | New enumerable methods that replace the current set |
| `SelectedObjects`, `CheckedObjects` | Still `IList` properties, now returning independent snapshots |
| Typed `Objects` | `IEnumerable<T>` with a snapshot getter, instead of `IList<T>` |
| Typed selection/check properties and commands | `IList<T>` snapshots and corresponding `IEnumerable<T>` methods |
| `IVirtualListDataSource` add/insert/remove methods | `IEnumerable` inputs |
| `OLVDataObject` constructor | `IEnumerable` input, copied rather than retained |

Regular controls copy assigned sequences. Later source-collection changes do not
update the control; use mutation methods or assign the sequence again. Untyped
mutations on typed controls reject incompatible model types before changing rows.

### Drawing-resource lifetime

`TextAdornment` (including text overlays/decorations), `BorderDecoration`,
`SimpleDropSink`, `TextSprite`, `Animation`, and `AnimationAdapter` implement
`IDisposable`. Dispose caller-created instances when no longer needed.
They release their generated drawing resources or timers, not caller-assigned
fonts, formats, pens, brushes, or animation components. Gradient borders use
a temporary brush without replacing or disposing `FillBrush`.

ObjectListView disposes its generated standard text overlays and drop sinks
created by `IsSimpleDropSink`. Explicitly assigned overlays and drop sinks remain
caller-owned; detached drop sinks stop scrolling and can be reused.

## Queries and snapshot getters

```csharp
var view = new TypedObjectListView<Person>();
view.Columns.Add(new OLVColumn("Name", "Name"));
view.Objects = people.Where(person => person.IsActive);
view.SetSelectedObjects(view.Objects.Where(person => person.Name.StartsWith("A")));
view.RemoveObjects(view.Objects.Where(person => person.CulinaryRating < 10));
```

Import `System.Linq` and `BrightIdeasSoftware` for this example. On non-generic
controls, use `Cast<Person>()` to query `IList` snapshots.

`Objects`, `SelectedObjects`, and `CheckedObjects` getters return independent
collections, not live views. Mutating them does not update the control, though
their elements still refer to the original model objects. Tree `Objects`
snapshots contain roots, not flattened rows. An unset/null source returns null;
an explicitly empty source returns an empty snapshot.

Use `GetObjectsSnapshot()` for an explicit copy or `Rows` for read-only
display-order access without a full snapshot. `Rows` follows sorting, filtering,
and tree expansion; changes invalidate active row enumerators. `ModelObjects`
is shared source access, not a snapshot or display-order guarantee.

Selection/check property assignments still work. Prefer `SetSelectedObjects`
and `SetCheckedObjects` for sequence inputs; `CheckedObjectsEnumerable` remains
a compatibility alias.

## Enumeration, events, and threading

Changed mutation APIs consume input once, before modifying models or clearing
selection/check state. Queries over the control's own snapshots are safe inputs.
Enumeration failures propagate without partially applying the enumerated batch.
This is **not a general transaction guarantee** for event handlers, callbacks,
renderers, or custom data sources.

Adding/removing event payloads remain `ICollection`. Handlers can modify, replace,
or cancel them; accepted payloads are copied before mutation. Notification-method
overrides should call their base implementation.

Add, insert, remove, and refresh marshal to an existing handle's thread before
enumeration. Other selection/check and data-transfer operations require the UI
thread; clipboard operations require STA. Sources themselves are not made
thread-safe, and direct virtual-data-source calls do not marshal.

## Nulls and control-specific behavior

| Input or control | Behavior |
| --- | --- |
| Null/empty selection or checked set | Clears the set |
| Null add/remove/insert input | No additions or removals |
| Null refresh or clipboard input | Requires non-null input; existing no-source/empty-tree early exits remain |
| Empty clipboard input | Leaves the clipboard unchanged |
| `OLVDataObject` | Requires a non-null control and sequence |
| Checkboxes disabled | Setting checks is a no-op |
| Data-bound add/insert/remove | No-op; modify the bound source instead |
| Tree add/remove | Changes roots |
| Tree insertion | Still throws `NotImplementedException` |

Virtual check-state optimizations and hierarchical tree checks are retained.
Duplicate/null model handling, ordering, sorting, and filtering are otherwise
unchanged; enumerable inputs do not enable unsupported operations.

## Overrides and virtual data sources

Update override and interface parameter types together:

```csharp
public override void AddObjects(System.Collections.IEnumerable models) {
    base.AddObjects(models);
}
```

`CheckedObjects` overrides remain `IList`. To customize sequence-based checking,
override `SetCheckedObjects(IEnumerable)` and route the property setter through
it. The new method does not automatically invoke an existing property override;
typed property assignments and the compatibility alias retain that behavior.

For custom `IVirtualListDataSource` implementations, materialize inputs before
mutation if they refer to the source or require multiple passes:

```csharp
var snapshot = ObjectListView.EnumerableToArray(models, alwaysCreate: true);
foreach (object model in snapshot)
    storedModels.Remove(model);
```

`alwaysCreate: true` guarantees an owned copy rather than borrowing a caller's list.
