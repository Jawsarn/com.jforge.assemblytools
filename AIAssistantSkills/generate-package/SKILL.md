---
name: generate-package
description: Run a JForge Assembly Tools AssemblyPackageGenerator's "Generate Package" action (scaffolding a feature's folder structure from a template) after creating or configuring the generator asset directly by file, since this action has no Inspector-independent trigger of its own and otherwise requires manually clicking a button in the Unity Editor.
---

# Generate Package

`AssemblyPackageGenerator` assets copy a template folder structure into a new feature folder, given `packageTemplate` + `generatedPackageName`. This was always an explicit, one-shot action (never auto-triggered by `OnValidate`) — the gap this fills is just giving it a programmatic entry point.

## When to use this

After configuring an `AssemblyPackageGenerator` asset directly by file (`generatedPackageName`, `packageTemplate`), to actually run the generation it now describes.

## Before running: this can silently overwrite existing files

**Not idempotent.** For every file the template produces, if something already exists at the destination path, it's deleted and replaced — no diff, no confirmation, only an info log. Re-running against an already-generated, hand-edited feature **destroys those edits**.

- New feature, destination folder doesn't exist / is empty: safe to proceed.
- Possibly re-running against an existing feature: check first (`git status`, list the folder). If files exist there, get the user's explicit confirmation before proceeding — tell them what would be overwritten.

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
        var success = JForge.AssemblyTools.PackageGenerator.AssemblyPackageGeneratorUtility.GeneratePackage("<path to the AssemblyPackageGenerator asset>");
        result.Log("GeneratePackage success: {0}", success);
    }
}
```
(Adjust to your bridge's actual convention — the above is the common `IRunCommand`/`CommandScript` pattern.)

Menu-only bridge: select the `AssemblyPackageGenerator` asset (`Selection.activeObject`), then execute `Assets/JForge/AssemblyTools/Generate Package`.

### Path B: no bridge (headless CI)

1. If a Unity Editor is already open on this project and you have no bridge, you can't automate this — a second process can't acquire the lock. Ask the user to run it in their open Editor (select the asset, right-click → `JForge > AssemblyTools > Generate Package`), or close the Editor first.
2. Otherwise: read `ProjectSettings/ProjectVersion.txt`'s `m_EditorVersion`, then find that Editor at the Hub default:
   - Windows: `%ProgramFiles%\Unity\Hub\Editor\<version>\Editor\Unity.exe`
   - macOS: `/Applications/Unity/Hub/Editor/<version>/Unity.app/Contents/MacOS/Unity`
   - Linux: `~/Unity/Hub/Editor/<version>/Editor/Unity`

   Not there? Ask the user for the path rather than guessing.
3. Run:
   ```
   "<Unity executable>" -batchmode -nographics -projectPath . -executeMethod JForge.AssemblyTools.PackageGenerator.AssemblyPackageGeneratorUtility.GeneratePackageFromCommandLine -jforgeTarget "<path to the AssemblyPackageGenerator asset>" -quit -logFile -
   ```

## Checking the result

Check the summary line (`Generated package '<name>' from '<generator>', success: True/False`) and any error lines (missing `packageTemplate`, invalid folders). Then check what changed under the destination folder (`git status`) — specifically confirm nothing pre-existing was silently overwritten, per the warning above.

## Notes

- Package generation's post-processors call `InheritedAssemblyGenerator.TryGenerate` on any copied generators — failures/warnings from that show up in the same console output.
- Missing `packageTemplate` is reported as an error, not silently skipped.
