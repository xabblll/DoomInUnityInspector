using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using UnityEngine;

namespace ManagedDoom.Unity
{
    public class UnityDoom
    {
        private CommandLineArgs args;

        private Config config;
        private GameContent content;


        private UnityVideo video;

        private UnitySound sound;
        private UnityMusic music;

        private UnityUserInput userInput;

        private Doom doom;

        public UnityDoom(CommandLineArgs args, string sfPath)
        {
            try
            {
                this.args = args;

                config = UnityConfigUtilities.GetConfig();
                content = new GameContent(args);

                config.video_screenwidth = Math.Clamp(config.video_screenwidth, 320, 3200);
                config.video_screenheight = Math.Clamp(config.video_screenheight, 200, 2000);

                config.audio_soundfont = sfPath;
            }
            catch (Exception e)
            {
                Dispose();
                ExceptionDispatchInfo.Throw(e);
            }
        }

        public void OnLoad()
        {
            video = new UnityVideo(config, content);

            if (!args.nosound.Present && !(args.nosfx.Present && args.nomusic.Present))
            {
                if (!args.nosfx.Present)
                {
                    sound = new UnitySound(config, content);
                }
            
                if (!args.nomusic.Present)
                {
                    music = UnityConfigUtilities.GetMusicInstance(config, content);
                }
            }

            userInput = new UnityUserInput(config, this, !args.nomouse.Present);

            doom = new Doom(args, config, content, video, sound, music, userInput);
        }

        public Texture2D GetVideoTexture()
        {
            return video.Texture;
        }

        public void UpdateKeys(List<KeyCode> keys)
        {
            userInput.KeysPressed = keys;
        }

        public UpdateResult OnUpdate()
        {
            return doom.Update();
        }

        public void OnRender()
        {
            video.Render(doom);
        }

        // private void OnResize(Vector2D<int> obj)
        // {
        //     video.Resize(obj.X, obj.Y);
        // }

        public void OnClose()
        {
            if (userInput != null)
            {
                userInput.Dispose();
                userInput = null;
            }

            if (music != null)
            {
                music.Dispose();
                music = null;
            }

            if (sound != null)
            {
                sound.Dispose();
                sound = null;
            }

            if (video != null)
            {
                video.Dispose();
                video = null;
            }

            config.Save(ConfigUtilities.GetConfigPath());
        }

        public void KeyDown(KeyCode key)
        {
            doom.PostEvent(new DoomEvent(EventType.KeyDown, UnityUserInput.UnityToDoomKey(key)));
        }
        
        public void KeyUp(KeyCode key)
        {
            doom.PostEvent(new DoomEvent(EventType.KeyUp, UnityUserInput.UnityToDoomKey(key)));
        }

        public void Dispose()
        {
            OnClose();
        }

        public string QuitMessage => doom.QuitMessage;
    }
}