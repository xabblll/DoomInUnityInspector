using System;
using ManagedDoom.Video;
using UnityEngine;
using Object = UnityEngine.Object;
using Renderer = ManagedDoom.Video.Renderer;

namespace ManagedDoom.Unity
{
    public class UnityVideo : IVideo, IDisposable
    {
        private int textureWidth;
        private int textureHeight;

        private byte[] textureData;
        private Texture2D texture;
        public Texture2D Texture { get => texture; }

        private Renderer renderer;

        public UnityVideo(Config config, GameContent content)
        {
            try
            {
                if (config.video_highresolution)
                {
                    textureWidth = 400;
                    textureHeight = 640;
                }
                else
                {
                    textureWidth = 200;
                    textureHeight = 320;
                }

                renderer = new Renderer(config, content);

                
                //TODO: make texture data with 3 bytes per pixel
                textureData = new byte[4 * textureWidth * textureHeight];
                texture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false, false);
                texture.filterMode = FilterMode.Point;
                texture.name = "DoomScreen";
            }
            catch (Exception e)
            {
                Dispose();
                throw(e);
            }
        }

        public void Render(Doom doom)
        {
            renderer.Render(doom, textureData);
            texture.SetPixelData(textureData, 0,0);
            texture.Apply(false, false);
        }

        public void InitializeWipe()
        {
            renderer.InitializeWipe();
        }

        public bool HasFocus()
        {
            return true;
        }

        public void Dispose()
        {
            Console.WriteLine("Shutdown video.");

            if (texture != null)
            {
                Object.DestroyImmediate(texture);
                texture = null;
            }
        }

        public int WipeBandCount => renderer.WipeBandCount;
        public int WipeHeight => renderer.WipeHeight;

        public int MaxWindowSize => renderer.MaxWindowSize;

        public int WindowSize
        {
            get => renderer.WindowSize;
            set => renderer.WindowSize = value;
        }

        public bool DisplayMessage
        {
            get => renderer.DisplayMessage;
            set => renderer.DisplayMessage = value;
        }

        public int MaxGammaCorrectionLevel => renderer.MaxGammaCorrectionLevel;

        public int GammaCorrectionLevel
        {
            get => renderer.GammaCorrectionLevel;
            set => renderer.GammaCorrectionLevel = value;
        }

    }
}