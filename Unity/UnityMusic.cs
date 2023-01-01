using System;
using System.IO;
using AudioSynthesis.Bank;
using AudioSynthesis.Midi;
// using CSharpSynth.Midi;
// using CSharpSynth.Sequencer;
// using CSharpSynth.Synthesis;
using ManagedDoom.Audio;
using UnityEngine;
using UnityMidi;
using WadTools;

namespace ManagedDoom.Unity
{
    public class UnityMusic : IMusic, IDisposable
    {
        private Config config;
        private Wad wad;
        
        private Bgm current;
        
        private AudioSource audioSource;
        private MidiPlayer midiPlayer;


        public UnityMusic(Config config, GameContent content)
        {
            try
            {
                this.config = config;
                wad = content.Wad;
                
               
                var sfPath = Path.Combine(config.audio_soundfont);
                if (!File.Exists(sfPath))
                {
                    return;
                }
                var sfData = File.ReadAllBytes(sfPath);
                var sfType = Path.GetExtension(sfPath).ToLower().Remove(0, 1);
                var patchBank = new PatchBank(new MemoryStream(sfData), sfType);
                
                var midiPlayerGo = new GameObject("Doom_MusicPlayer");
                midiPlayerGo.hideFlags = HideFlags.HideAndDontSave;
                midiPlayer = midiPlayerGo.AddComponent<MidiPlayer>();
                audioSource = midiPlayerGo.GetComponent<AudioSource>();
                midiPlayer.Init();
                midiPlayer.SetVolume(DoomVolumeNormalized(Volume));
                
                midiPlayer.LoadBank(patchBank);

                // midiPlayer.LoadBank(new PatchBank(File.OpenRead(sfPath), "bank"));

                current = Bgm.NONE;
            }
            catch (Exception e)
            {
                Dispose();
                throw (e);
            }
        }

        public void Dispose()
        {
            if (midiPlayer != null)
            {
                 UnityEngine.Object.DestroyImmediate(midiPlayer.gameObject, true);
            }
        }

        public void StartMusic(Bgm bgm, bool loop)
        {
            // return;
            
            if (bgm == current)
            {
                return;
            }
            //
            var lump = "D_" + DoomInfo.BgmNames[(int)bgm].ToString().ToUpper();
            var musicData = wad.ReadLump(lump);
            
            var musicType = GetMusicType(musicData);
            
            MidiFile midi = null;
            if (musicType == MusicType.MUS) 
            {
                midi = new MidiFile(new MusToMidiConverter(musicData).MidiData());
                // midi = new MidiFile(MusToMidi.Convert(musicData));
            } 
            else if (musicType == MusicType.MIDI) 
            {
                midi = new MidiFile(musicData);
            }
            
            if (midi != null && midiPlayer != null) {
                midiPlayer.LoadMidi(midi);
                midiPlayer.Play();
                midiPlayer.SetLoopMode(loop);
            }
            
            current = bgm;
        }

        public void StopMusic()
        {
            midiPlayer.Stop();
        }
      
        
        public int MaxVolume => 15;

        public float DoomVolumeNormalized(int doomVolume)
        {
            return (float)doomVolume / MaxVolume;
        }

        public int Volume
        {
            get { return config.audio_musicvolume; }

            set
            {
                if (midiPlayer != null)
                {
                    midiPlayer.SetVolume(DoomVolumeNormalized(value));
                }
                config.audio_musicvolume = value;
            }
        }

        public enum MusicType
        {
            Unknown,
            MIDI,
            MUS
        }


        public MusicType GetMusicType(byte[] data)
        {
            if (data[0] == Convert.ToByte('M') &&
                data[1] == Convert.ToByte('T') &&
                data[2] == Convert.ToByte('h') &&
                data[3] == Convert.ToByte('d'))
            {
                return MusicType.MIDI;
            }

            if (data[0] == Convert.ToByte('M') &&
                data[1] == Convert.ToByte('U') &&
                data[2] == Convert.ToByte('S'))
            {
                return MusicType.MUS;
            }

            return MusicType.Unknown;
        }
    }
}
