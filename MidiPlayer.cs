using System;
using System.IO;
using AudioSynthesis.Bank;
using AudioSynthesis.Synthesis;
using AudioSynthesis.Sequencer;
using AudioSynthesis.Midi;
using UnityEngine;

namespace UnityMidi
{
    [RequireComponent(typeof(AudioSource))]
    public class MidiPlayer : MonoBehaviour
    {
        //TEST
        public bool isPlayTestMidiAndBank;
        PatchBank bank;
        MidiFile midi;
        
        private Synthesizer synthesizer;
        private AudioSource audioSource;
        private MidiFileSequencer sequencer;
        private int bufferHead;
        private float[] currentBuffer;

        public string midiFilePath;
        public string bankPath;

        public AudioSource AudioSource { get { return audioSource; } }
        public MidiFileSequencer Sequencer { get { return sequencer; } }
        public PatchBank Bank { get { return bank; } }
        public MidiFile MidiFile { get { return midi; } }

        private bool IsPlaying => sequencer.IsPlaying;

        private void Start()
        {
            Init();
        }

        public void Init()
        {
            var sampleRate = AudioSettings.outputSampleRate;
            synthesizer = new Synthesizer(sampleRate, 2, 8, 8, 64);
            sequencer = new MidiFileSequencer(synthesizer);
            audioSource = GetComponent<AudioSource>();

            if (isPlayTestMidiAndBank)
            {
                var bankData = File.ReadAllBytes(bankPath);
                LoadBank(new PatchBank(new MemoryStream(bankData), Path.GetExtension(bankPath).ToLower().Remove(0, 1)));
                var midiFileData = File.ReadAllBytes(midiFilePath);
                LoadMidi(new MidiFile(midiFileData));
                SetVolume(1f);
                Play();
            }
        }

        public void SetLoopMode(bool value)
        {
            if(sequencer == null)
                return;
            sequencer.IsLoop = value;
        }

        public void SetVolume(float volume)
        {
            synthesizer.MixGain = volume * 2.5f;
        }

        public void LoadBank(PatchBank bank)
        {
            this.bank = bank;
            synthesizer.UnloadBank();
            synthesizer.LoadBank(bank);
        }

        public void LoadMidi(MidiFile midi)
        {
            Stop();
            this.midi = midi;
            sequencer.UnloadMidi();
            sequencer.LoadMidi(midi);
        }

        public void Play()
        {
            gameObject.SetActive(true);
            // sequencer.Seek(sequencer.EndTime * synthesizer.SampleRate); 
            audioSource.volume = 1f;
            sequencer.Play();
            audioSource.Play();
            // isPlaying = true;
        }

        public void Stop()
        {
            // isPlaying = false;
            gameObject.SetActive(false);
            audioSource.volume = 0f;
            synthesizer.NoteOffAll(true);
            audioSource.Stop();
            sequencer.Stop();
        }

        void OnAudioFilterRead(float[] data, int channel)
        {
            // if (!IsPlaying)
            // {
            //     Array.Clear(data, 0, data.Length);
            //     currentBuffer = null;
            //     bufferHead = 0;
            //     return;
            // }

            int count = 0;
            while (count < data.Length)
            {
                if (currentBuffer == null || bufferHead >= currentBuffer.Length)
                {
                    sequencer.FillMidiEventQueue();
                    synthesizer.GetNext();
                    currentBuffer = synthesizer.WorkingBuffer;
                    bufferHead = 0;
                }
                var length = Mathf.Min(currentBuffer.Length - bufferHead, data.Length - count);
                Array.Copy(currentBuffer, bufferHead, data, count, length);
                bufferHead += length;
                count += length;
   
            }
        }
    }
}
