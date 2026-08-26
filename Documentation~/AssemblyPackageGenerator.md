# Assembly Package Generator
## Summary
The Assembly Package Generator is a tool to generate feature packages from a template folder structure with assemblies and references.
## Using
* Set up a template folder structure with assemblies and references.
  * The template folder structure can use regular Assemblies, [Inherited Assembly Generator](Documentation~/InheritedAssemblyGenerator.md)s and any other project files.
  * Name files and folders with a replacement string e.g. "#" to later be replaced with a feature name.

![AssemblyPackageGeneratorFolder](Images/AssemblyPackageGeneratorFolder.png)

* Create a **DirectoryReferencePackageTemplate** scriptable object in the project folder from **Create->JForge->DirectoryReferencePackageTemplate**
  * Click the "Select folder" button to set the correct template folder.
  * Set the replacement string

![AssemblyPackageGeneratorSO](Images/AssemblyPackageGeneratorSO.png)

* Create a AssemblyPackageGenerator **Create->JForge->AssemblyPackageGenerator** in the parent folder of where you want to create the package
  * Set the feature name and target template
* Click the "Generate" button to create the package (or right-click the asset in the Project window → **JForge > AssemblyTools > Generate Package**, which does the same thing).

![AssemblyPackageGeneratorSO2](Images/AssemblyPackageGeneratorSO2.png)


## Advanced
* If you want to create new types of custom templates akin to **DirectoryReferencePackageTemplate** you can inherit the **PackageTemplate** scriptable object.
* If you want to support custom assets being copied that requires post processing you can implement the **IAssetPackageGeneratorAssetPostProcessor** which will be picked up by reflection.

## Re-running generation
This action is not safe to rerun blindly on an already-generated feature. Unlike regenerating an inherited assembly, it deletes and replaces any existing file at a path the template produces, with no diff or confirmation, so rerunning it against a hand-edited feature destroys those edits. This is true regardless of whether it's re-triggered from the Inspector button, the context menu, or programmatically.

## For AI coding agents and CI
Programmatic invocation - direct C# calls and `-executeMethod` command lines - along with the overwrite-risk warning above, is documented in the AI Assistant skill at `AIAssistantSkills/generate-package/SKILL.md`. As with the [regenerate-inherited-assemblies](InheritedAssemblyGenerator.md#for-ai-coding-agents-and-ci) skill, this is discovered automatically by Unity's own AI Assistant with no install step; other agent tools may need pointing at the file directly.
