using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using ManagedDoom;
using ManagedDoom.Unity;
using Xabblll.DoomInInspector;
using UnityEngine;
using UnityEngine.UI;
using Sprite = UnityEngine.Sprite;

public class RuntimeDoom : MonoBehaviour
{
    [SerializeField]
    private int fps = 30;

    [SerializeField]
    private Image _image;

    [SerializeField]
    private DoomedComponent doomedComponent;

    private readonly List<KeyCode> keysPressed = new(8);
    private UnityDoom doom;
    private Texture2D screen;

    private KeyCode[] _allKeycodes;
    private Sprite _sprite;

    private void Start()
    {
        _allKeycodes = Enum.GetValues(typeof(KeyCode)).Cast<KeyCode>().ToArray();

        CreateDoom();

        _sprite = Sprite.Create(screen, new Rect(0, 0, screen.width, screen.height), Vector2.zero);
        _image.sprite = _sprite;
        _image.SetNativeSize();

        StartCoroutine(UpdateFrame());
    }

    private void OnDestroy()
    {
        doom.OnClose();
        doom.Dispose();

        _sprite = null;
        screen = null;
        keysPressed.Clear();
    }

    private void Update()
    {
        FetchInput();
    }

    private IEnumerator UpdateFrame()
    {
        var wait = new WaitForSeconds(1f / fps);

        while(true)
        {
            yield return wait;

            FrameTick();
        }
    }

    private void CreateDoom()
    {
        //todo - set correct paths for runtime build
        var wadPath = doomedComponent.WadPath;
        var sfPath = doomedComponent.SfPath;

#if UNITY_EDITOR
        if (doomedComponent.Wad != null)
        {
            wadPath = UnityEditor.AssetDatabase.GetAssetPath(doomedComponent.Wad);
        }

        if (doomedComponent.SoundFont != null)
        {
            sfPath = UnityEditor.AssetDatabase.GetAssetPath(doomedComponent.SoundFont);
        }
#endif
        var args = new CommandLineArgs(new string[]
        {
            "iwad", wadPath
        });
        doom = new UnityDoom(args, sfPath);
        doom.OnLoad();
        screen = doom.GetVideoTexture();
    }

    private void FrameTick()
    {
        doom.UpdateKeys(keysPressed);
        doom.OnUpdate();
        doom.OnRender();
    }

    private void FetchInput()
    {
        for (int i = 0; i < _allKeycodes.Length; i++)
        {
            var keyCode = _allKeycodes[i];

            if (Input.GetKeyDown(keyCode))
            {
                if (!keysPressed.Contains(keyCode))
                {
                    keysPressed.Add(keyCode);
                }

                doom.KeyDown(keyCode);
            }

            if (Input.GetKeyUp(keyCode))
            {
                if (keysPressed.Contains(keyCode))
                {
                    keysPressed.Remove(keyCode);
                }

                doom.KeyUp(keyCode);
            }
        }
    }
}
