// using UnityEngine;
// using System.IO;
// using System.Collections;
// using AudioSynthesis;
// using AudioSynthesis.Bank;
// using AudioSynthesis.Synthesis;
// using AudioSynthesis.Sequencer;
// using AudioSynthesis.Midi;
//
// namespace ManagedDoom.Unity
// {
//     public class UnityMidiPlayer
//     {
//
//
//         [RequireComponent(typeof(AudioSource))]
//         public class MidiPlayer 
//         {
//             //[SerializeField] StreamingAssetResouce bankSource;
//             //[SerializeField] public StreamingAssetResouce midiSource;
//             [SerializeField] bool loadOnAwake = false;
//             [SerializeField] bool playOnAwake = false;
//             [SerializeField] int channel = 2;
//             [SerializeField] int sampleRate = 44100;
//             [SerializeField] int bufferSize = 1024;
//             PatchBank bank;
//             MidiFile midi;
//             Synthesizer synthesizer;
//             AudioSource audioSource;
//             MidiFileSequencer sequencer;
//             int bufferHead;
//             float[] currentBuffer;
//
//             public AudioSource AudioSource
//             {
//                 get { return audioSource; }
//             }
//
//             public MidiFileSequencer Sequencer
//             {
//                 get { return sequencer; }
//             }
//
//             public PatchBank Bank
//             {
//                 get { return bank; }
//             }
//
//             public MidiFile MidiFile
//             {
//                 get { return midi; }
//             }
//
//             public void Awake()
//             {
//                 synthesizer = new Synthesizer(sampleRate, channel, bufferSize, 1);
//                 sequencer = new MidiFileSequencer(synthesizer);
//                 audioSource = GetComponent<AudioSource>();
//                 
//                 if (loadOnAwake)
//                 {
//                     //LoadBank(new PatchBank(bankSource));
//                     //LoadMidi(new MidiFile(midiSource));
//                 }
//
//                 if (playOnAwake)
//                 {
//                     Play();
//                 }
//             }
//
//             public void LoadBank(PatchBank bank)
//             {
//                 this.bank = bank;
//                 synthesizer.UnloadBank();
//                 synthesizer.LoadBank(bank);
//             }
//
//             public void LoadMidi(MidiFile midi)
//             {
//                 this.midi = midi;
//                 sequencer.Stop();
//                 sequencer.UnloadMidi();
//                 sequencer.LoadMidi(midi);
//             }
//
//             public void Play()
//             {
//                 sequencer.Play();
//                 audioSource.Play();
//             }
//
//             public void Stop()
//             {
//                 synthesizer.NoteOffAll(true);
//                 audioSource.Stop();
//                 sequencer.Stop();
//             }
//
//             void OnAudioFilterRead(float[] data, int channel)
//             {
//                 Debug.Assert(this.channel == channel);
//                 int count = 0;
//                 while (count < data.Length)
//                 {
//                     if (currentBuffer == null || bufferHead >= currentBuffer.Length)
//                     {
//                         sequencer.FillMidiEventQueue();
//                         synthesizer.GetNext();
//                         currentBuffer = synthesizer.WorkingBuffer;
//                         bufferHead = 0;
//                     }
//
//                     var length = Mathf.Min(currentBuffer.Length - bufferHead, data.Length - count);
//                     System.Array.Copy(currentBuffer, bufferHead, data, count, length);
//                     bufferHead += length;
//                     count += length;
//                 }
//             }
//         }
//     }
// }