# NPLForVisualStudio
Author: LiXizhi 
Date: 2023.5.1

## Installation
Install the new [NPL/Lua language service extension](https://marketplace.visualstudio.com/items?itemName=Xizhi.xizhi2022) for Visual Studio 2022 or above.

- for visual studio 2017 or below,  [click here](https://github.com/LiXizhi/NPL)

## Features
1. right click on a line to set NPL breakpoint there. 
2. code completion support as you type. 
3. right click to NPL goto definition.
4. mouse over a function to show quick info
5. custom Documentation/*.xml support in the project folder.

### XML documetation
 IntelliSense and code completion uses XML files under ${SolutionDir|ProjectDir}/Documentation/ folder.  Users can add new XML files for their own applications. 
 See ${install path}/Documentation for [examples](https://github.com/LiXizhi/NPLForVisualStudio/blob/master/Documentation/NplDocumentation.xml). 
 Solution must be reloaded after adding new XML files.

 To automatically generate XML file from a batch of source code files, please install [paracraft](https://paracraft.cn/)  and run command `/docgen`.

### How to Build
Tested with visual studio 2022. 
1. Use nuget to install dependencies like `Microsoft.VisualStudio.*` and `Microsoft.VSSDK.BuildTools`
2. Build the project
3. Upload to visual studio marketplace

#### Customizations
1. edit extension.vsixmanifest assets to include vspackage (custom command) and mefcomponent (code sense)
2. edit Documentation/*.xml for predefined functions, make sure they are built included in vsix. 
3. NPLDocs.Instance is a single object that contains API for all documentation XML files. 
4. ./CodeSense folder contains platform-independent helper classes for parsing and searching documentations. 
5. Root directory contains async interface for VsPackage like context menu commands and Mef Component like Async QuickInfo and Code completion. 
6. The entry VsPackage defined in `NPLForVisualStudioPackage.cs` is configured to start when visual studio or solution is opened. 

## References
- https://github.com/microsoft/vs-editor-api/issues/9
