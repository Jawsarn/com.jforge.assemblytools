# Changelog

## [1.1.2] - 2026-08-25
### Added
* `InheritedAssemblyGenerator.NeedsRegeneration()` - reports whether `TryGenerate` would actually write anything right now, without writing anything itself (no file I/O, no reimport). Extracted from `TryGenerate` via a shared `TryComputeContent` helper, so the two stay in sync by construction rather than by convention.
* `InheritedAssemblyGeneratorUtility.AnyRootAssemblyChanged()` - a cheap, read-only check for whether any "root" generator (one whose `assemblyDefinitionBase` is a hand-authored assembly, not itself another generator's `generatedDefinition`) has drifted from what's currently generated. Meant to gate `RegenerateAll`, which is materially more expensive and shouldn't be called reflexively - if this returns `false`, there's nothing for a full sweep to do. Does not catch a mid-chain generator's own fields being edited directly outside `RegenerateTarget`, but that case is already handled cheaply by `RegenerateTarget` itself.
* `InheritedAssemblyGeneratorUtility.CheckRootAssembliesFromCommandLine()` - `AnyRootAssemblyChanged` as a CI staleness gate via `-executeMethod`: exits the Editor process with code 1 if a root assembly changed (regeneration needed but not run/committed), 0 otherwise. Calls `EditorApplication.Exit`, so it's only safe to invoke from a dedicated batch-mode process, never a live-Editor bridge.

### Changed
* Both AI Assistant skills (`AIAssistantSkills/regenerate-inherited-assemblies`, `AIAssistantSkills/generate-package`) now target the `unity` CLI (`com.unity.pipeline`'s `unity command eval`/`console`) as the primary way to drive a live Editor instance, replacing the earlier `unity-mcp`-specific `RunCommand`/`ManageMenuItem`/`Selection` workaround. `unity command eval` calls straight into the package's own assembly with no compile-context limitations, unlike `RunCommand`'s isolated dynamic assembly (which couldn't resolve `JForge.AssemblyTools` at all). The headless `-executeMethod` fallback for when no live instance is running is unchanged. Both skills now also call out passing `--project-path`/`--instance` explicitly, since more than one Unity project can have a live instance running at once.

## [1.1.1] - 2026-08-25
### Added
* A project setting, **Edit > Project Settings > JForge Assembly Tools > Default To GUID References** (on by default), controlling whether generated assemblies default to GUID- or name-based references when their style can't be inferred from an existing reference. Previously this default (`AssemblySerializer.TryDeserialize`'s `defaultUseGUID` parameter, added in `[1.0.2]`) was hardcoded to `true` with no way to change it. Backed by `AssemblyToolsSettings.DefaultUseGuidReferences`, a `ScriptableSingleton<T>` persisted at `ProjectSettings/JForgeAssemblyToolsSettings.asset` (shared via source control, not per-user `EditorPrefs`) - not a regular `AssetDatabase`-tracked asset, since `ProjectSettings/` isn't part of the AssetDatabase. The Project Settings page is a plain `SettingsProvider` with a `Toggle` bound to the setting's own get/set property (`AssemblyToolsSettingsProvider`); also settable directly from code (`AssemblyToolsSettings.DefaultUseGuidReferences = false;`) or by hand-editing the file.

### Changed
* `AssemblySerializer.TryDeserialize` no longer takes a `defaultUseGUID` parameter - it now reads `AssemblyToolsSettings.DefaultUseGuidReferences` directly, so both callers (`InheritedAssemblyGenerator.TryGenerate`, `AssemblyDefinitionProcessor.Process`) no longer need to pass it through. Any external code calling `TryDeserialize` with the second argument will need updating to drop it.

## [1.1.0] - 2026-08-24
### Added
* `InheritedAssemblyGeneratorUtility.RegenerateTarget(string)` - regenerates only the `InheritedAssemblyGenerator`(s) affected by a single changed file (itself, or a `.asmdef` used as some generator's base), without touching any other generator in the project. Callable directly (e.g. from a live-Editor MCP/tool bridge), from a Project-window context menu item on a selected generator or base assembly (`JForge > AssemblyTools > Regenerate Inherited Assembly`), or headless via `RegenerateTargetFromCommandLine` (reads a `-jforgeTarget <path>` argument) through Unity's `-executeMethod` batch-mode flag. This is the primary fix for regeneration not being triggered by direct file edits (e.g. from an AI coding agent) rather than Inspector edits, since it's scoped and fast enough to run after every such edit.
* `InheritedAssemblyGeneratorUtility.RegenerateAll()` - the whole-project fallback: finds and regenerates every `InheritedAssemblyGenerator`, looping passes so a change propagates through an inheritance chain. Available as an Editor menu item (`JForge > AssemblyTools > Regenerate All Inherited Assemblies`) and via `-executeMethod`. Materially more expensive than `RegenerateTarget` since every generator in the project runs regardless of relevance - prefer `RegenerateTarget` when the changed file is known.
* An AI Assistant skill (`AIAssistantSkills/regenerate-inherited-assemblies/SKILL.md`), following Unity's documented package-skill convention (`Packages/<package-name>/AIAssistantSkills/<skill-folder>/SKILL.md`) so it's discovered automatically with no install step. Documents preferring a live-Editor C#/MCP bridge over spawning a headless Unity process when one is available, with the command-line form as fallback, and a manual step only as the genuine last resort (no bridge available *and* the Editor is already open, so batch mode can't acquire the project lock). Currently covers discovery by Unity's own AI Assistant; other agent tools may need pointing at the file directly - see `Documentation~/InheritedAssemblyGenerator.md`.
* `AssemblyPackageGeneratorUtility.GeneratePackage(string)` / `GeneratePackage(AssemblyPackageGenerator)` - a programmatic entry point for "Generate Package", previously only triggerable via the Inspector button. Available the same way as the above: directly, via a Project-window context menu item (`JForge > AssemblyTools > Generate Package`), or headless via `GeneratePackageFromCommandLine` through `-executeMethod`. Also now calls `AssetDatabase.SaveAssets()` after running, so dirty state from the generation (and any post-processed `InheritedAssemblyGenerator` copies) reliably persists in batch mode.
* A matching AI Assistant skill (`AIAssistantSkills/generate-package/SKILL.md`) for the above, which explicitly warns that - unlike regenerating an inherited assembly - rerunning package generation against an already-generated, hand-edited feature silently overwrites those edits, and instructs checking for existing destination files before running rather than assuming a rerun is safe.

### Changed
* `InheritedAssemblyGenerator.Generate()` renamed to `TryGenerate()` and now returns `bool` (whether it actually wrote a new/changed assembly definition) instead of `void`, matching the `Try*` convention already used elsewhere in the package (e.g. `AssemblySerializer.TryDeserialize`). Existing callers that ignore the return value are unaffected by the return-type change, but any external code calling `Generate()` directly will need updating to `TryGenerate()`.
* `AssemblyPackageGeneratorEditor`'s "Generate Package" button now delegates to `AssemblyPackageGeneratorUtility.GeneratePackage` instead of duplicating the logic inline. Behavior is unchanged except that a missing `packageTemplate` is now reported as a `Debug.LogError` instead of the button silently doing nothing.
* The command-line-argument-reading helper used by `-executeMethod` entry points is now shared (`PackageUtilities.GetCommandLineArgValue`) instead of duplicated per utility class.

## [1.0.2] - 2026-08-23
### Changed
* Assemblies with no existing references now default to GUID-based references (instead of name-based) when references are added, matching Unity's own asmdef inspector default. Assemblies that already have at least one reference are unaffected - they keep following whichever style they already use.
* Resolving name-based references no longer does a project-wide asset search per reference on every generate; it now uses Unity's own assembly-name index directly, which is both faster and immune to the search matching the wrong asset when names overlap.

### Fixed
* Custom `IAssetPackageGeneratorAssetPostProcessor` implementations defined outside the package are now discovered.
* Renaming or moving a generated assembly definition no longer leaves an orphaned `.meta` file behind.
* `InheritedAssemblyGenerator` no longer regenerates its assembly definition on every keystroke while editing the name/root namespace fields; it now waits until the field is submitted or loses focus.
* Adding an entry to `additionalReferences` via the inspector's "+" button (which duplicates the previous entry by default) no longer produces a duplicate-reference error on regenerate; duplicate references are now skipped when generating, and are called out with a console warning and an inspector warning box so they don't go unnoticed.
* `InheritedAssemblyGenerator` now marks itself dirty after generating, so `generatedDefinition` and its cached reference lists reliably survive a domain reload instead of silently reverting when nothing else happened to save the asset (most notably when generated via package generation, with no Inspector edit involved).

## [1.0.1] - 2026-03-07
### Fixed
* Fixed incorrect reference copying when multiple assemblies shared part of a name; inherited assemblies now reference the correct base assembly.

## [1.0.0] - 2024-06-02

### Added
* **InheritedAssemblyGenerator** - A tool to generate inherited assemblies from other assembly definitions.
* **AssemblyPackageGenerator** - A tool to generate feature packages from a template folder structure with assemblies and references.
* Simple samples to showcase the tools. 