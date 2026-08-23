# Changelog

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