using System;
using ManagedDoom.Audio;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ManagedDoom.Unity
{
    public class UnitySound : ISound, IDisposable
    {
        private static readonly int channelCount = 8;

        private static readonly float fastDecay = (float)Math.Pow(0.5, 1.0 / (35 / 5));
        private static readonly float slowDecay = (float)Math.Pow(0.5, 1.0 / 35);

        private static readonly float clipDist = 1200;
        private static readonly float closeDist = 160;
        private static readonly float attenuator = clipDist - closeDist;

        private Config config;

        private AudioClip[] buffers;
        private float[] amplitudes;

        private DoomRandom random;
        
        private AudioSource[] channels;
        private ChannelInfo[] infos;

        private AudioSource uiChannel;
        private Sfx uiReserved;

        private Mobj listener;

        private float masterVolumeDecay;

        private DateTime lastUpdate;
    
        public UnitySound(Config config, GameContent content)
        {
            try
            {
                this.config = config;

                config.audio_soundvolume = Math.Clamp(config.audio_soundvolume, 0, MaxVolume);

                buffers = new AudioClip[DoomInfo.SfxNames.Length];
                amplitudes = new float[DoomInfo.SfxNames.Length];

                if (config.audio_randompitch)
                {
                    random = new DoomRandom();
                }

                for (var i = 0; i < DoomInfo.SfxNames.Length; i++)
                {
                    var name = "DS" + DoomInfo.SfxNames[i].ToString().ToUpper();
                    var lump = content.Wad.GetLumpNumber(name);
                    if (lump == -1)
                    {
                        continue;
                    }

                    int sampleRate;
                    int sampleCount;
                    var samples = GetSamples(content.Wad, name, out sampleRate, out sampleCount);
                    if (samples.Length != 0)
                    {
                        var clip = AudioClip.Create(name, samples.Length, 1, sampleRate, false);
                        clip.SetData(samples, 0);
                        buffers[i] = clip;
                        amplitudes[i] = GetAmplitude(samples, sampleRate, samples.Length);
                    }
                }

                channels = CreateChannels(channelCount);
                infos = new ChannelInfo[channelCount];
                for (var i = 0; i < channelCount; i++)
                {
                    infos[i] = new ChannelInfo();
                }

                uiChannel = CreateChannel("DoomAudio_UIChannel");
                uiReserved = Sfx.NONE;

                masterVolumeDecay = (float)config.audio_soundvolume / MaxVolume;

                lastUpdate = DateTime.MinValue;
            }
            catch (Exception e)
            {
                Dispose();
                throw e;
            }
        }

        private AudioSource[] CreateChannels(int channelCount)
        {
            var channels =new AudioSource[channelCount];
            for (int i = 0; i < channelCount; i++)
            {
                channels[i] = CreateChannel($"DoomAudio_Channel{i}");
            }

            return channels;
        }

        private static AudioSource CreateChannel(string name)
        {
            var channelGO = new GameObject(name);
            channelGO.hideFlags = HideFlags.HideAndDontSave;
            var channel = channelGO.AddComponent<AudioSource>();
            channel.spatialBlend = 0f;
            channel.playOnAwake = false;
            return channel;
        }


        private static float[] GetSamples(Wad wad, string name, out int sampleRate, out int sampleCount)
        {
            var data = wad.ReadLump(name);

            if (data.Length < 8)
            {
                sampleRate = -1;
                sampleCount = -1;
                return null;
            }

            sampleRate = BitConverter.ToUInt16(data, 2);
            sampleCount = BitConverter.ToInt32(data, 4);

            var offset = 8;
            if (ContainsDmxPadding(data))
            {
                offset += 16;
                sampleCount -= 32;
            }

            if (sampleCount > 0)
            {
                var floatArray = new float[sampleCount];
                for (int i = offset; i < sampleCount; i++)
                {
                    floatArray[i] = (float)data[i] / 127 - 1f;
                }
                return floatArray;
            }
            else
            {
                return Array.Empty<float>();
            }
        }

        // Check if the data contains pad bytes.
        // If the first and last 16 samples are the same,
        // the data should contain pad bytes.
        // https://doomwiki.org/wiki/Sound
        private static bool ContainsDmxPadding(byte[] data)
        {
            var sampleCount = BitConverter.ToInt32(data, 4);
            if (sampleCount < 32)
            {
                return false;
            }
            else
            {
                var first = data[8];
                for (var i = 1; i < 16; i++)
                {
                    if (data[8 + i] != first)
                    {
                        return false;
                    }
                }

                var last = data[8 + sampleCount - 1];
                for (var i = 1; i < 16; i++)
                {
                    if (data[8 + sampleCount - i - 1] != last)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static float GetAmplitude(float[] samples, int sampleRate, int sampleCount)
        {
            var max = 0f;
            if (sampleCount > 0)
            {
                var count = Math.Min(sampleRate / 5, sampleCount);
                for (var t = 0; t < count; t++)
                {
                    var a = samples[t] - 0.5f;
                    if (a < 0f)
                    {
                        a = -a;
                    }

                    if (a > max)
                    {
                        max = a;
                    }
                }
            }

            return max;
        }

        public void SetListener(Mobj listener)
        {
            this.listener = listener;
        }

        public void Update()
        {
            var now = DateTime.Now;
            if ((now - lastUpdate).TotalSeconds < 0.01)
            {
                // Don't update so frequently (for timedemo).
                return;
            }

            for (var i = 0; i < infos.Length; i++)
            {
                var info = infos[i];
                var channel = channels[i];

                if (info.Playing != Sfx.NONE)
                {
                    if (info.Type == SfxType.Diffuse)
                    {
                        info.Priority *= slowDecay;
                    }
                    else
                    {
                        info.Priority *= fastDecay;
                    }

                    SetParam(channel, info);
                }

                if (info.Reserved != Sfx.NONE)
                {
                    if (info.Playing != Sfx.NONE)
                    {
                        channel.Stop();
                    }

                    channel.clip = buffers[(int)info.Reserved];
                    SetParam(channel, info);
                    channel.pitch = GetPitch(info.Type, info.Reserved);
                    channel.PlayOneShot(channel.clip);
                    // channel.PlayOneShot(channel.clip);

                    info.Playing = info.Reserved;
                    info.Reserved = Sfx.NONE;
                }
            }

            if (uiReserved != Sfx.NONE)
            {
                if (uiChannel.isPlaying)
                {
                    uiChannel.Stop();
                }

                uiChannel.volume = masterVolumeDecay;
                uiChannel.clip = buffers[(int)uiReserved];
                uiChannel.Play();
                uiReserved = Sfx.NONE;
            }

            lastUpdate = now;
        }

        public void StartSound(Sfx sfx)
        {
            if (buffers[(int)sfx] == null)
            {
                return;
            }

            uiReserved = sfx;
        }

        public void StartSound(Mobj mobj, Sfx sfx, SfxType type)
        {
            StartSound(mobj, sfx, type, 100);
        }

        public void StartSound(Mobj mobj, Sfx sfx, SfxType type, int volume)
        {
            if (buffers[(int)sfx] == null)
            {
                return;
            }

            var x = (mobj.X - listener.X).ToFloat();
            var y = (mobj.Y - listener.Y).ToFloat();
            var dist = MathF.Sqrt(x * x + y * y);

            float priority;
            if (type == SfxType.Diffuse)
            {
                priority = volume;
            }
            else
            {
                priority = amplitudes[(int)sfx] * GetDistanceDecay(dist) * volume;
            }

            for (var i = 0; i < infos.Length; i++)
            {
                var info = infos[i];
                if (info.Source == mobj && info.Type == type)
                {
                    info.Reserved = sfx;
                    info.Priority = priority;
                    info.Volume = volume;
                    return;
                }
            }

            for (var i = 0; i < infos.Length; i++)
            {
                var info = infos[i];
                if (info.Reserved == Sfx.NONE && info.Playing == Sfx.NONE)
                {
                    info.Reserved = sfx;
                    info.Priority = priority;
                    info.Source = mobj;
                    info.Type = type;
                    info.Volume = volume;
                    return;
                }
            }

            var minPriority = float.MaxValue;
            var minChannel = -1;
            for (var i = 0; i < infos.Length; i++)
            {
                var info = infos[i];
                if (info.Priority < minPriority)
                {
                    minPriority = info.Priority;
                    minChannel = i;
                }
            }

            // if (minChannel >= 0 && priority >= minPriority)
            if (priority >= minPriority)
            {
                var info = infos[minChannel];
                info.Reserved = sfx;
                info.Priority = priority;
                info.Source = mobj;
                info.Type = type;
                info.Volume = volume;
            }
        }

        public void StopSound(Mobj mobj)
        {
            for (var i = 0; i < infos.Length; i++)
            {
                var info = infos[i];
                if (info.Source == mobj)
                {
                    info.LastX = info.Source.X;
                    info.LastY = info.Source.Y;
                    info.Source = null;
                    info.Volume /= 5;
                }
            }
        }

        public void Reset()
        {
            if (random != null)
            {
                random.Clear();
            }

            for (var i = 0; i < infos.Length; i++)
            {
                channels[i].Stop();
                infos[i].Clear();
            }

            listener = null;
        }

        public void Pause()
        {
            // for (var i = 0; i < infos.Length; i++)
            // {
            //     var channel = channels[i];
            //
            //     if (channel.isPlaying)
            //     {
            //         channels[i].Pause();
            //         // channels[i].paus
            //     }
            // }
        }

        public void Resume()
        {
            // for (var i = 0; i < infos.Length; i++)
            // {
            //     var channel = channels[i];
            //
            //     //TODO: Looking sus
            //     // if (!channel.isPlaying)
            //     {
            //         channels[i].UnPause();
            //     }
            // }
        }

        private void SetParam(AudioSource sound, ChannelInfo info)
        {
            if (info.Type == SfxType.Diffuse)
            {
                sound.panStereo = 0f;
                sound.volume = 0.01F * masterVolumeDecay * info.Volume;
            }
            else
            {
                Fixed sourceX;
                Fixed sourceY;
                if (info.Source == null)
                {
                    sourceX = info.LastX;
                    sourceY = info.LastY;
                }
                else
                {
                    sourceX = info.Source.X;
                    sourceY = info.Source.Y;
                }

                var x = (sourceX - listener.X).ToFloat();
                var y = (sourceY - listener.Y).ToFloat();

                if (Math.Abs(x) < 16 && Math.Abs(y) < 16)
                {
                    sound.panStereo = 0f;
                    sound.volume = 0.01F * masterVolumeDecay * info.Volume;
                }
                else
                {
                    var dist = MathF.Sqrt(x * x + y * y);
                    var angle = MathF.Atan2(y, x) - (float)listener.Angle.ToRadian();
                    angle = (angle * Mathf.Rad2Deg) / 180f;
                    angle = Mathf.Clamp(angle,-0.2f, 0.2f);
                    sound.panStereo = angle;
                    // sound.panStereo = new Vector3(MathF.Cos(angle), 0, MathF.Sin(angle));
                    sound.volume = 0.01F * masterVolumeDecay * GetDistanceDecay(dist) * info.Volume;
                }
            }
        }

        private float GetDistanceDecay(float dist)
        {
            if (dist < closeDist)
            {
                return 1F;
            }
            else
            {
                return Math.Max((clipDist - dist) / attenuator, 0F);
            }
        }

        private float GetPitch(SfxType type, Sfx sfx)
        {
            if (random != null)
            {
                if (sfx == Sfx.ITEMUP || sfx == Sfx.TINK || sfx == Sfx.RADIO)
                {
                    return 1.0F;
                }
                else if (type == SfxType.Voice)
                {
                    return 1.0F + 0.075F * (random.Next() - 128) / 128;
                }
                else
                {
                    return 1.0F + 0.025F * (random.Next() - 128) / 128;
                }
            }
            else
            {
                return 1.0F;
            }
        }

        public void Dispose()
        {
            if (channels != null)
            {
                for (var i = 0; i < channels.Length; i++)
                {
                    if (channels[i] != null)
                    {
                        channels[i].Stop();
                        GameObject.DestroyImmediate(channels[i].gameObject, true);
                        channels[i] = null;
                    }
                }

                channels = null;
            }

            if (buffers != null)
            {
                for (var i = 0; i < buffers.Length; i++)
                {
                    if (buffers[i] != null)
                    {
                        GameObject.DestroyImmediate(buffers[i], true);
                        buffers[i] = null;
                    }
                }

                buffers = null;
            }

            if (uiChannel != null)
            {
                GameObject.DestroyImmediate(uiChannel.gameObject, true);
                uiChannel = null;
            }
        }

        public int MaxVolume
        {
            get { return 15; }
        }

        public int Volume
        {
            get { return config.audio_soundvolume; }

            set
            {
                config.audio_soundvolume = value;
                masterVolumeDecay = (float)config.audio_soundvolume / MaxVolume;
            }
        }



        private class ChannelInfo
        {
            public Sfx Reserved;
            public Sfx Playing;
            public float Priority;

            public Mobj Source;
            public SfxType Type;
            public int Volume;
            public Fixed LastX;
            public Fixed LastY;

            public void Clear()
            {
                Reserved = Sfx.NONE;
                Playing = Sfx.NONE;
                Priority = 0;
                Source = null;
                Type = 0;
                Volume = 0;
                LastX = Fixed.Zero;
                LastY = Fixed.Zero;
            }
        }
    }
}