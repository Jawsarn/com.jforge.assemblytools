---
name: regenerate-inherited-assemblies
description: Regenerate a JForge Assembly Tools InheritedAssemblyGenerator-derived .asmdef after editing a base .asmdef or an InheritedAssemblyGenerator asset directly by file (not through the Unity Editor Inspector), since Unity's OnValidate does not fire for external file edits and the derived assembly would otherwise silently go stale.
---

# Regenerate Inherited Assemblies

`InheritedAssemblyGenerator` assets derive a `.asmdef` from a base assembly definition, normally regenerating on Inspector edits via `OnValidate`. **Direct file edits don't trigger `OnValidate`**, so they're silently ignored until this is run manually.

## When to use this

After directly editing, in this repo:
- A `.asmdef` used as an `InheritedAssemblyGenerator`'s `assemblyDefinitionBase`, or
- An `InheritedAssemblyGenerator` `.asset` itself (`assemblyName`, `additionalReferences`, `rootNamespace`, ...).

Use `RegenerateTarget` with the specific file you changed, not `RegenerateAll` — regenerating the whole project for a single-file edit is unnecessary work.

## Before editing: check for unsaved changes

If you have a live-Editor bridge, check first:
```csharp
EditorUtility.IsDirty(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("<path>"))
```
If dirty, **don't edit the file** — the asset already has unrelated unsaved changes, and Unity may resolve the conflict by discarding your file edit when it next reconciles (silently, no error). Tell the user to save/discard first, then retry.

No bridge / can't check: you can't detect this in advance. Always verify your specific change landed after regenerating (below) rather than trusting the log alone — a discarded edit and "nothing needed to change" both report `changes made: False`.

## How to run it

**Prefer Path A** — runs in the live Editor, works whether or not Unity is open, no process management.

### Path A: live-Editor C#/MCP bridge available

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var changed = JForge.AssemblyTools.Inheritance.InheritedAssemblyGeneratorUtility.RegenerateTarget("<path to the file you changed>");
        result.Log("Regenerate changed anything: {0}", changed);
    }
}
```
(Adjust to your bridge's actual convention — the above is the common `IRunCommand`/`CommandScript` pattern.)

Menu-only bridge (no arbitrary C#): execute `JForge/AssemblyTools/Regenerate All Inherited Assemblies` — coarser (whole project), but still avoids Path B.

### Path B: no bridge (headless CI)

1. If a Unity Editor is already open on this project and you have no bridge, you can't automate this — a second process can't acquire the lock. Ask the user to run it in their open Editor (`JForge > AssemblyTools > Regenerate Inherited Assembly` on the selected file), or close the Editor first. This is the only case needing a manual step.
2. Otherwise: read `ProjectSettings/ProjectVersion.txt`'s `m_EditorVersion`, then find that Editor at the Hub default:
   - Windows: `%ProgramFiles%\Unity\Hub\Editor\<version>\Editor\Unity.exe`
   - macOS: `/Applications/Unity/Hub/Editor/<version>/Unity.app/Contents/MacOS/Unity`
   - Linux: `~/Unity/Hub/Editor/<version>/Editor/Unity`

   Not there? Ask the user for the path rather than guessing.
3. Run once per changed file:
   ```
   "<Unity executable>" -batchmode -nographics -projectPath . -executeMethod JForge.AssemblyTools.Inheritance.InheritedAssemblyGeneratorUtility.RegenerateTargetFromCommandLine -jforgeTarget "<path to the file you changed>" -quit -logFile -
   ```

## Checking the result

Check the summary line (`Regenerated N target(s), changes made: True/False`) and any error/warning lines. Then diff the changed `.asmdef`/`.asset` files and confirm your intended change is actually present — not just that the log claimed success (see the unsaved-changes warning above).

## Notes

- Multiple unrelated changed files: call once per file, not combined.
- Unsure what changed, or want the whole project verified: use `RegenerateAll` (same invocation, no target argument) — more expensive, but finds and converges every generator, including inheritance chains.
- Both methods are no-ops if nothing needs regenerating.
- Neither of these runs `AssemblyPackageGenerator`'s "Generate Package" — see the separate `generate-package` skill for that.
