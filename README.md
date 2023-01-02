# Doom in Unity Inspector

![Screenshot](https://github.com/xabblll/DoomInUnityInspector/blob/master/DoomInInspector_Screenshot.png)

**About**

Doom port inside Unity Editor component inspector, made for fun in 4 evenings.
- Core engine mostly based on [sinshu's Managed Doom](https://github.com/sinshu/managed-doom)
- CPU ticks simulated with EditorCorutines and Thread.Sleep(), along with cycle of easy instructions for stable 35 FPS
- Screen Render: Texture2D in InspectorGUI, updates in DoomEngine via Texture.SetPixelData(bytes)
- Sounds: Hidden AudioSources - 1 AudioSource per SFX
- Music (hopefully works): Rewritten CSharpSynth library, that can load real SF2 banks and Midi files. Doom stores music in own "MUS" format, so it converted Midi on loading

Package contains original DOOM1.WAD Shareware version and Roland SC-55 SF2 bank, but you can use your own files. (Doom2 works fine too for example) 


**Installation**

1. Copy https link from github
2. Open Unity > Package manager
3. Press "+" button then "Add package from git URL"
4. Paste copied link and apply
