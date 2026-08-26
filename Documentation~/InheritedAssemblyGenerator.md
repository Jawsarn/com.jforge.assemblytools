# Inherited Assembly Generator
## Summary
The Inherited Assembly Generator is a tool to generate inherited assemblies from assembly definition. This can be useful when you have a lot of assemblies to create or manage, where a project is undergoing constant changes that result in package dependencies changes over time. As unity does not support any inspector or other tooling for assemblies, these generators are a way to allow better reusability and maintainability of assemblies.
## Using
* Create a normal assembly definition which will be used as the base assembly.
    * Modify the assembly with dependencies or target platforms as needed.
* Create a InheritedAssemblyGenerator **Create->JForge->InheritedAssemblyGenerator** in the folder of where you want your new assembly.
  * Set the base assembly definition.
  * Add any additional dependencies & adjust names if you do not want to keep the same naming as the created **InheritedAssemblyGenerator**.
* An assembly will be generated when any changes are made to the **InheritedAssemblyGenerator**.
* (optional) Create additional **InheritedAssemblyGenerator**s to target the generated assembly to create inherited hierarchies.

![InheritedAssemblyGeneratorSO](Images/InheritedAssemblyGeneratorSO.png)

## Settings
**Edit > Project Settings > JForge Assembly Tools** has a **Default To GUID References** toggle (on by default). It controls how references get written when a generated assembly's reference style can't be inferred from an existing reference - e.g. the base assembly has none yet. GUID references survive the referenced assembly being renamed; name-based references are more readable in the generated `.asmdef` but break if the referenced assembly is renamed. Once an assembly has at least one reference, its existing style is always followed regardless of this setting.

This is `AssemblyToolsSettings.DefaultUseGuidReferences` - also settable from code (`AssemblyToolsSettings.DefaultUseGuidReferences = false;`, persists immediately) or by hand-editing `ProjectSettings/JForgeAssemblyToolsSettings.asset` directly. Check that file into source control so the whole team shares the same default.

## Regenerating after a direct file edit
Editing a base assembly or an **InheritedAssemblyGenerator** asset directly - e.g. with a text editor or another tool, rather than the Unity Inspector - does not trigger regeneration automatically, since it's driven by Unity's `OnValidate`, which only fires for Inspector-driven changes. Trigger it manually instead:
* Just the generator(s) affected by a specific file you changed (fast - does not touch unrelated generators): select the changed file (either the `InheritedAssemblyGenerator` or the base `.asmdef`) in the Project window, right-click → **JForge > AssemblyTools > Regenerate Inherited Assembly**.
* Everything in the project (e.g. if you're not sure what changed, or want the whole project verified) - noticeably more expensive, since every generator in the project runs regardless of relevance: **JForge > AssemblyTools > Regenerate All Inherited Assemblies**.

Both repeat passes as needed so a change propagates through an inheritance chain (e.g. Gen1 → Gen2 → Gen3).

## For AI coding agents and CI
Programmatic invocation - direct C# calls, `-executeMethod` command lines, and cheap staleness checks to avoid running a full regenerate unnecessarily - is documented in the AI Assistant skill at `AIAssistantSkills/regenerate-inherited-assemblies/SKILL.md`. This is Unity's documented convention for package-provided skills, so it's discovered automatically with no install step. It currently covers discovery by Unity's own AI Assistant; other agent tools (e.g. Claude Code) may need pointing at the file directly.
