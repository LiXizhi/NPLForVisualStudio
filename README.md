# NPLForVisualStudio
Author: LiXizhi 
Date: 2023.5.1

The new NPL language service extension for Visual Studio 2022 or above.

## Features
1. right click on a line to set NPL breakpoint there. 
2. code completion support as you type. 
3. right click to NPL goto definition.
4. mouse over a function to show quick info
5. custom Documentation/*.xml support in the project folder.

#### XML documetation
 IntelliSense and code completion using XML files under ${SolutionDir|ProjectDir}/Documentation.  Users can add new XML files for their own applications. 
 See ${install path}/Documentation for [examples](https://github.com/LiXizhi/NPLForVisualStudio/blob/master/Documentation/NplDocumentation.xml). Filepath can be found in NPL output panel.

## References
- https://github.com/microsoft/vs-editor-api/issues/9
- for visual studio 2017 or below,  use https://github.com/LiXizhi/NPL