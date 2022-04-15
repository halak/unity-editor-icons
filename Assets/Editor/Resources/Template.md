# Unity Editor Built-in Icons
![Window/Icon Browser](/Documentation~/ToolImage.png)
Unity version: <UnityVersion>

Icons that you can load using `EditorGUIUtility.IconContent`

## File ID

You can change script icon by file id
1. Open `*.cs.meta` in Text Editor
2. Modify line `icon: {instanceID: 0}` to `icon: {fileID: <FILE ID>, guid: 0000000000000000d000000000000000, type: 0}`
3. Save and focus Unity Editor
