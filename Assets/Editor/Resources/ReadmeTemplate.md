# Unity Editor Icon Browser
<p align="center"><img  src="Documentation~/ToolImage.png" width="50%"></p>

## Instalation
Supported Unity 2019.4+
1. Open context menu: __Window > Package Manager__
2. Click the __➕▾__ in the top left and choose __Add Package from git URL__
3. Enter `https://github.com/ErnSur/unity-editor-icons.git` and press __Add__

## Icon Usage
### Change Script Icon
You can change script icon using icons file id
1. Open `*.cs.meta` in Text Editor
2. Modify line `icon: {instanceID: 0}` to `icon: {fileID: <FILE ID>, guid: 0000000000000000d000000000000000, type: 0}`
3. Save and focus Unity Editor
### Use in IMGUI
You can load Icons below by using `EditorGUIUtility.IconContent` with the icon name.
e.g., `var content = EditorGUIUtility.IconContent("console.warnicon");`

## Icon Table
Click on an Icon to get its details.

Icons from Unity <UnityVersion>
