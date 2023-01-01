using UnityEngine;

namespace ManagedDoom.Unity
{
    public static class UnityConfigUtilities
    {
        public static Config GetConfig()
        {
            var config = new Config(ConfigUtilities.GetConfigPath());

            if (!config.IsRestoredFromFile)
            {
                var vm = GetDefaultVideoMode();
                config.video_screenwidth = vm.x;
                config.video_screenheight = vm.y;
            }

            return config;
        }

        public static Vector2Int GetDefaultVideoMode()
        {
            // var monitor = Monitor.GetMainMonitor(null);

            var baseWidth = 320;
            var baseHeight = 200;

            return new Vector2Int(baseWidth, baseHeight);

            // var currentWidth = baseWidth;
            // var currentHeight = baseHeight;
            //
            // while (true)
            // {
            //     var nextWidth = currentWidth + baseWidth;
            //     var nextHeight = currentHeight + baseHeight;
            //
            //     if (nextWidth >= 0.9 * monitor.VideoMode.Resolution.Value.X ||
            //         nextHeight >= 0.9 * monitor.VideoMode.Resolution.Value.Y)
            //     {
            //         break;
            //     }
            //
            //     currentWidth = nextWidth;
            //     currentHeight = nextHeight;
            // }

            // return new VideoMode(new Vector2D<int>(currentWidth, currentHeight));
        }

        // public static AudioSource GetMusicInstance(Config config, GameContent content, AudioSource device)
        // {
        //     var sfPath = Path.Combine(ConfigUtilities.GetExeDirectory(), config.audio_soundfont);
        //     if (File.Exists(sfPath))
        //     {
        //         return new SilkMusic(config, content, device, sfPath);
        //     }
        //     else
        //     {
        //         Console.WriteLine("SoundFont '" + config.audio_soundfont + "' was not found!");
        //         return null;
        //     }
        // }
        public static UnityMusic GetMusicInstance(Config config, GameContent content)
        {
            // return null;
            return new UnityMusic(config, content);
        }
    }
}