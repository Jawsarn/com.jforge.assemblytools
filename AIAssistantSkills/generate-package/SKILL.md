---
name: generate-package
description: Run a JForge Assembly Tools AssemblyPackageGenerator's "Generate Package" action (scaffolding a feature's folder structure from a template) after creating or configuring the generator asset directly by file, since this action has no Inspector-independent trigger of its own and otherwise requires manually clicking a button in the Unity Editor. Drives a live Unity Editor instance directly via the `unity` CLI (unity-pipeline).
---

# Generate Package

`AssemblyPackageGenerator` assets copy a template folder structure into a new feature folder, given `packageTemplate` + `generatedPackageName`. This was always an explicit, one-shot action (never auto-triggered by `OnValidate`) — the gap this fills is just giving it a programmatic entry point.

This assumes the `unity` CLI is set up (`unity-pipeline` package installed, `unity` on PATH) - see the `unity-pipeline` skill if not.

## When to use this

After configuring an `AssemblyPackageGenerator` asset directly by file (`generatedPackageName`, `packageTemplate`), to actually run the generation it now describes.

**Always pass `--project-path` (or `--instance host:port`) explicitly** — more than one Unity project can have a live instance running at once (`unity pipeline list` shows every reachable one).

## Before running: this can silently overwrite existing files

**Not idempotent.** For every file the template produces, if something already exists at the destination path, it's deleted and replaced — no diff, no confirmation, only an info log. Re-running against an already-generated, hand-edited feature **destroys those edits**.

- New feature, destination folder doesn't exist / is empty: safe to proceed.
- Possibly re-running against an existing feature: check first (`git status`, list the folder). If files exist there, get the user's explicit confirmation before proceeding — tell them what would be overwritten.

## How to run it

### A live instance is running for this project (preferred)

```bash
unity command eval "return JForge.AssemblyTools.PackageGenerator.AssemblyPackageGeneratorUtility.GeneratePackage(\"<path to the AssemblyPackageGenerator asset>\");" --project-path "<project path>"
```

The JSON response's `result` field is `GeneratePackage`'s return value. This copies/creates a batch of new files, which can trigger a recompile - poll before reading logs if the result was `true`:

```bash
unity command recompile_status --project-path "<project path>"   # repeat until "completed" or "up_to_date"
```

### No live instance (headless / CI)

1. Locate the Unity executable the same way as the `regenerate-inherited-assemblies` skill (via `ProjectSettings/ProjectVersion.txt` + Hub default paths).
2. Run:
   ```bash
   "<Unity executable>" -batchmode -nographics -projectPath . -executeMethod JForge.AssemblyTools.PackageGenerator.AssemblyPackageGeneratorUtility.GeneratePackageFromCommandLine -jforgeTarget "<path to the AssemblyPackageGenerator asset>" -quit -logFile -
   ```

## Checking the result

```bash
unity command console --tail 10 --project-path "<project path>"
```

Check for error lines (missing `packageTemplate`, invalid folders), then check what changed under the destination folder (`git status`) — specifically confirm nothing pre-existing was silently overwritten, per the warning above.

## Notes

- Package generation's post-processors call `InheritedAssemblyGenerator.TryGenerate` on any copied generators - failures/warnings from that show up in the same console output.
- Missing `packageTemplate` is reported as an error, not silently skipped.
