using System;

namespace WadTools {
	public class MusToMidiConverter {

		private enum MusEvent : byte {
			MusReleaseKey = 0x00,
			MusPressKey = 0x10,
			MusPitchWheel = 0x20,
			MusSystemEvent = 0x30,
			MusChangeController = 0x40,
			MusScoreEnd = 0x60
		}

		private enum MidiEvent : byte {
			MidiReleaseKey = 0x80,
			MidiPressKey = 0x90,
			MidiAfterTouchKey = 0xA0,
			MidiChangeController = 0xB0,
			MidiChangePatch = 0xC0,
			MidiAfterTouchChannel = 0xD0,
			MidiPitchWheel = 0xE0,
			MidiAllNoteOff = 0x7B,
			MidiAllSoundsOff = 0x78,
			MidiResetControllers = 0x79,
		}

		private class MusHeader {
			public string ID;
			public int ScoreLength;
			public int ScoreStart;
			public int PrimaryChannels;
			public int SecondaryChannels;
			public int InstrumentCount;
		}

		private MusHeader musHeader;

		private readonly byte[] midiHeader = {
			0x4d, 0x54, 0x68, 0x64,	// Main header
			0x00, 0x00, 0x00, 0x06, // Header size
			0x00, 0x00,				// MIDI type (0)
			0x00, 0x01,				// Number of tracks
			0x00, 0x46,				// Resolution
			0x4d, 0x54, 0x72, 0x6b,	// Start of track
			0x00, 0x00, 0x00, 0x00  // Placeholder for track length
		};

		// Constants
		private const int NUMBER_OF_CHANNELS = 16;
		private const int MUS_PERCUSSION_CHANNEL = 15;
		private const int MIDI_PERCUSSION_CHANNEL = 9;

		// Cached velocities
		private readonly byte[] channelVelocities = {
			127, 127, 127, 127, 127, 127, 127, 127, 
			127, 127, 127, 127, 127, 127, 127, 127
		};

		// Timestamps between sequences of MUS events
		private int queuedTime;

		// Counter for the length of the track
		private int trackSize;

		private readonly byte[] controllerMap = 
		{
		    0x00, //Unused
		    0x02, //Bank select coarse 0x00 (or BankSelectFine at 0x02??)
		    0x01, //Modulation wheel coarse
		    0x07, //Channel volume coarse
		    0x0A, //Pan coarse
		    0x0B, //Expression coarse
		    0x5B, //EffectsLevel
		    0x5D, //ChorusLevel
		    0x40, //HoldPedal
		    0x43, //SoftPedal
		    0x78, //AllSoundOff
		    0x7B, //AllNotesOff
		    0x7E, //MonoMode
		    0x7F, //PolyMode
		    0x79 //Reset controllers
		};

		private readonly int[] channelMap = new int[NUMBER_OF_CHANNELS];
		private int pos;
		private readonly byte[] inputData;
		byte[] outputData = Array.Empty<byte>();
		private void WriteData(byte[] data) {
			byte[] newData = new byte[outputData.Length + data.Length];
			Buffer.BlockCopy(outputData, 0, newData, 0, outputData.Length);
			Buffer.BlockCopy(data, 0, newData, outputData.Length, data.Length);
			outputData = newData;
		}

		private void WriteByte(byte data) {
			WriteData(new[] {data});
		}

		private void WriteEventByte(MidiEvent midiEvent, int channel) {
			WriteByte((byte) ((byte)midiEvent | (byte)channel));
		}

		private void WriteDataByte(byte eventData, int mod = 0x7F) {
			WriteByte((byte)(eventData & mod));
		}

		private void WriteTime(int time)
		{
		    int buffer = time & 0x7F;
		    byte writeValue;

		    while ((time >>= 7) != 0)
		    {
		        buffer <<= 8;
		        buffer |= ((time & 0x7F) | 0x80);
		    }

		    for (;;)
		    {
		        writeValue = (byte)(buffer & 0xFF);

		        WriteByte(writeValue);

		        ++trackSize;

		        if ((buffer & 0x80) != 0)
		            buffer >>= 8;
		        else
		        {
		            queuedTime = 0;
		            return;
		        }
		    }
		}

		private void WriteOfEndTrack()
		{
		    var endOfTrack = new byte[] {0xFF, 0x2F, 0x00};
		    WriteTime(queuedTime);
		    WriteData(endOfTrack);
		    trackSize += 3;
		}

		private void WritePressKey(int channel, byte note, byte velocity)
		{
			WriteTime(queuedTime);
			
			WriteEventByte(MidiEvent.MidiPressKey, channel);
			WriteDataByte(note);
			WriteDataByte(velocity);

			trackSize += 3;
		}

		private void WriteReleaseKey(int channel, byte key)
		{
			WriteTime(queuedTime);
			
			WriteEventByte(MidiEvent.MidiReleaseKey, channel);
			WriteDataByte(key);
			WriteByte(0);

			trackSize += 3;
		}

		private void WritePitchWheel(int channel, byte wheel)
		{
			WriteTime(queuedTime);

			WriteEventByte(MidiEvent.MidiPitchWheel, channel);
			
			var pw2 = (wheel << 7) / 2;
			var pw1 = pw2 & 127;
			pw2 >>= 7;
			
			WriteDataByte((byte)pw1);
			WriteDataByte((byte)pw2);

			trackSize += 3;
		}

		private void WriteSystemEvent(int channel, byte systemEvent)
		{
			switch (systemEvent)
			{
				case 11:
					WriteTime(queuedTime);
					WriteEventByte(MidiEvent.MidiAllNoteOff, channel); // Write Note Off (not immediately)
					trackSize++;
					break;
				case 14:
					WriteTime(queuedTime);
					WriteEventByte(MidiEvent.MidiResetControllers, channel); // Reset controllers
					trackSize++;
					break;
			}
		}

		private void WriteChangePatch(int channel, byte patch)
		{
			WriteTime(queuedTime);

			WriteEventByte(MidiEvent.MidiChangePatch, channel);
			WriteDataByte(patch);

			trackSize += 2;
		}

		private void WriteChangeController(int channel, byte controllerNumber, byte controllerValue)
		{
			if (controllerNumber == 0)
			{
				WriteChangePatch(channel, controllerValue);
				return;
			}
			
			WriteChangeControllerData(channel, controllerMap[controllerNumber], controllerValue);
		}

		private void WriteChangeControllerData(int channel, byte control, byte val)
		{
			WriteTime(queuedTime);

			WriteEventByte(MidiEvent.MidiChangeController, channel);
			WriteDataByte(control);
			// WriteByte((byte) ((val & 0x80)!=0 ? 0x7F : val));
			WriteByte(val);

			trackSize += 3;
		}
		
		private int GetMidiChannel(int musChannel)
		{
			if (musChannel == MUS_PERCUSSION_CHANNEL)
			{
				 return MIDI_PERCUSSION_CHANNEL;
			}
			if (musChannel >= MUS_PERCUSSION_CHANNEL)
			{
				return musChannel + 1;
			}

			return musChannel;
		}

		private void ReadMusHeader() {
			musHeader = new MusHeader() {
				// ID = new string(Encoding.ASCII.GetChars(data, 0, 4)),
				// ScoreLength = BitConverter.ToUInt16(data, 4),
				ScoreStart = BitConverter.ToUInt16(inputData, 6),
				// PrimaryChannels = BitConverter.ToUInt16(data, 8),
				// SecondaryChannels = BitConverter.ToUInt16(data, 10),
				// InstrumentCount = BitConverter.ToUInt16(data, 12)
			};
		}

		byte GetByte() {
			var outputByte = inputData[pos];
			pos++;
			return outputByte;
		}

		public MusToMidiConverter(byte[] data)
		{
			inputData = data;

			ReadMusHeader();

			byte eventDescriptor;
			int channel;
			int musEvent;

			byte key;
			byte controllerNumber;
			byte controllerValue;

			pos = 0;
			int hitScoreEnd = 0;

			byte working;
			int timeDelay;

			for (channel = 0; channel < NUMBER_OF_CHANNELS; channel++)
			{
				channelMap[channel] = -1;
			}

			pos = musHeader.ScoreStart;

			WriteData(midiHeader);
			trackSize = 0;
			

			while (hitScoreEnd == 0)
			{
				while (hitScoreEnd == 0)
				{
					eventDescriptor = GetByte();
					channel = GetMidiChannel(eventDescriptor & 0x0F);
					musEvent = eventDescriptor & 0x70;
					
					switch (musEvent)
					{
						case (int)MusEvent.MusReleaseKey: // 0
							key = GetByte();
							WriteReleaseKey(channel, key);
							break;
						case (int)MusEvent.MusPressKey: // 1
							key = GetByte();
							var noteNumber = key & 127;
							var noteVolume = (key & 128) != 0 ? GetByte() : -1;
							if (noteVolume == -1)
								noteVolume = channelVelocities[channel];
							else
								channelVelocities[channel] = (byte)noteVolume;

							WritePressKey(channel, (byte)noteNumber, (byte)noteVolume);
							break;
						case (int)MusEvent.MusPitchWheel: // 2
							key = GetByte();
							WritePitchWheel(channel, key);
							break;
						case (int)MusEvent.MusSystemEvent: // 3
							key = GetByte();
							WriteSystemEvent(channel, key);
							break;
						case (int)MusEvent.MusChangeController: // 4
							controllerNumber = GetByte();
							controllerValue = GetByte();
							WriteChangeController(channel, controllerNumber, controllerValue);
							break;
						case (int)MusEvent.MusScoreEnd: // 6
							hitScoreEnd = 1;
							break;
					}

					if (eventDescriptor >> 7 != 0)
					{
						break;
					}
				}

				if (hitScoreEnd == 0)
				{
					timeDelay = 0;
					for (;;)
					{
						working = GetByte();
						timeDelay = timeDelay * 128 + (working & 0x7F);
						if ((working & 0x80) == 0) break;
					}

					queuedTime += timeDelay;
				}

				WriteOfEndTrack();

				outputData[18 + 0] = (byte)((trackSize >> 24) & 0xff);
				outputData[18 + 1] = (byte)((trackSize >> 16) & 0xff);
				outputData[18 + 2] = (byte)((trackSize >> 8) & 0xff);
				outputData[18 + 3] = (byte)(trackSize & 0xff);
			}
		}

		public byte[] MidiData() {
			return outputData;
		}
	}
}