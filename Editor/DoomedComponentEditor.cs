using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using ManagedDoom;
using ManagedDoom.Unity;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using Xabblll.DoomInInspector;
using EventType = UnityEngine.EventType;


namespace Editor
{
    [CustomEditor(typeof(DoomedComponent))]
    public class DoomedComponentEditor : UnityEditor.Editor
    {
        private Texture2D screen;

        // CPU Ticks
        private double timeLastFrame;
        private double timeCurrentTick;
        private double tickDelta;
        private double currentFrameDelta;
        private double lastDeltaTime;
        private double timeLastTick;

        private bool isFocused = false;
        private EditorCoroutine cpuCoroutine;
        private string cpuLog = "CPU Log:";

        // Inputs
        private readonly List<KeyCode> keysPressed = new(8);

        private DoomedComponent doomedComponent;
        private UnityDoom doom;
        private bool isLoaded;


        private void OnEnable()
        {
            doomedComponent = (DoomedComponent)target;
        }

        private void StartDoom()
        {
            if(isLoaded) return;
            
            //Get Paths
            var wadPath = doomedComponent.WadPath;
            var sfPath = doomedComponent.SfPath;
            if (doomedComponent.Wad != null)
            {
                wadPath = AssetDatabase.GetAssetPath(doomedComponent.Wad);
            }

            if (doomedComponent.SoundFont != null)
            {
                sfPath = AssetDatabase.GetAssetPath(doomedComponent.SoundFont);
            }

            try
            {
                var args = new CommandLineArgs(new string[]
                {
                    "iwad", wadPath
                });
                doom = new UnityDoom(args, sfPath);
                doom.OnLoad();
                screen = doom.GetVideoTexture();

                isLoaded = true;
            }
            catch (System.Exception e)
            {
                throw e;
            }
            cpuCoroutine = EditorCoroutineUtility.StartCoroutine(DoomCPU(), this);
            doomedComponent.LockKeyboard = true;
        }

        public override bool RequiresConstantRepaint()
        {
            return false;
        }

        private IEnumerator DoomCPU()
        {
            long frequency = Stopwatch.Frequency;
            var sw = Stopwatch.StartNew();
            long frameTime;

            while (true)
            {
                if (isLoaded)
                {
                    doom.UpdateKeys(new List<KeyCode>(keysPressed));
                    if (doom.OnUpdate() == UpdateResult.Completed)
                    {
                        StopDoom();
                        yield break;
                    }

                    doom.OnRender();
                    Repaint();

                    var desiredTickTime = (long)Math.Floor(frequency * doomedComponent.TickTime);
                    frameTime = sw.ElapsedTicks;
                    var gamePerformance = frameTime;
                    yield return null;

                    // More or less reliable way to wait exact time
                    frameTime = sw.ElapsedTicks;
                    long threadSleepTime = 0;
                    var residentSleeper = new TimeSpan(ticks:1000);
                    while (frameTime + 1000 < desiredTickTime)
                    {
                        Thread.Sleep(residentSleeper);
                        frameTime = sw.ElapsedTicks;
                        threadSleepTime += 1000;
                    }
                    frameTime = sw.ElapsedTicks;
                    int waiter = 0;
                    while (frameTime < desiredTickTime)
                    {
                        waiter++;
                        frameTime = sw.ElapsedTicks;
                    }

                    cpuLog =
                        $"CPU:{(float)sw.ElapsedTicks / frequency * 1000:00.00000} ms |" +
                        $"ThreadSleep:{threadSleepTime:00000000000} ticks |" +
                        $"FineWait:{waiter:000000}" +
                        $"\nDoom Performance:{(float)gamePerformance / frequency * 1000:00.000ms}";
                    sw.Restart();
                }
            }
        }

        private void StopDoom()
        {
            isLoaded = false;
            doomedComponent.LockKeyboard = false;
            
            if (cpuCoroutine != null)
                EditorCoroutineUtility.StopCoroutine(cpuCoroutine);
            
            if (doom != null)
            {
                doom.OnClose();
                doom.Dispose();
            }

            screen = null;
            keysPressed.Clear();
            Repaint();

            GC.Collect();
            EditorUtility.UnloadUnusedAssetsImmediate(true);
        }

        private void OnDisable()
        {
            StopDoom();
        }
        
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            isFocused = doomedComponent.LockKeyboard;
            if (isFocused && isLoaded)
            {
                var keyboardID = GUIUtility.GetControlID(FocusType.Keyboard);
                GUIUtility.keyboardControl = keyboardID;

                var evt = Event.current;
                if (evt.isKey && evt.keyCode != KeyCode.None)
                {
                    if (evt.rawType == EventType.KeyDown)
                    {
                        if (!keysPressed.Contains(evt.keyCode))
                            keysPressed.Add(evt.keyCode);

                        doom.KeyDown(evt.keyCode);
                    }

                    if (evt.rawType == EventType.KeyUp)
                    {
                        if (keysPressed.Contains(evt.keyCode))
                            keysPressed.Remove(evt.keyCode);

                        doom.KeyUp(evt.keyCode);
                    }

                    if (evt.keyCode == KeyCode.X)
                    {
                        isFocused = false;
                    }

                    evt.Use();
                }
            }

            if(isLoaded)
                DrawScreen();
            else
            {
                GUILayout.Space(20f);
                if (GUILayout.Button("DOOM", GUILayout.Height(50f)))
                {
                    StartDoom();
                }
            }
        }

        private void DrawScreen()
        {
            if (screen == null) return;

            var screenRatio = (float)screen.height / screen.width;
            // var width = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.ExpandWidth(true), GUILayout.Height(1)).width;
            var rect = GUILayoutUtility.GetAspectRect(screenRatio, GUILayout.ExpandWidth(true));
            var bgRect = rect;
            var padding = 6.66f;
            rect.xMin += padding;
            rect.xMax -= padding;
            rect.yMin += padding;
            rect.yMax -= padding;
            GUI.Label(bgRect, GUIContent.none, EditorStyles.objectFieldThumb);
            // EditorGUI.DrawRect(bgRect, new Color(1,1,1,1f));
            GUIUtility.RotateAroundPivot(90, rect.min);
            var screenRect = new Rect(rect.x, rect.y, rect.height, rect.width);
            screenRect.y -= screenRect.height;
            GUI.DrawTexture(screenRect, screen);
            GUIUtility.RotateAroundPivot(-90, rect.min);

            var currentEvent = Event.current;
            if (currentEvent.type == EventType.MouseDown)
            {
                doomedComponent.LockKeyboard = rect.Contains(currentEvent.mousePosition);
            }

            GUILayout.Label(cpuLog, EditorStyles.centeredGreyMiniLabel);
        }
    }
}