/*
 * Copyright 2017 Google LLC
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

#define SOUNDSTAGE_API __declspec(dllexport) 

extern "C" {
	SOUNDSTAGE_API void SetArrayToFixedValue(float buf[], int length, float value);
	SOUNDSTAGE_API int DrumSignalGenerator(float buffer[], int length, int channels, bool signalOn, int counter);
	SOUNDSTAGE_API void SetArrayToSingleValue(float a[], int length, float val);
	SOUNDSTAGE_API void MultiplyArrayBySingleValue(float buffer[], int length, float val);
	SOUNDSTAGE_API void AddArrays(float a[], float b[], int length);
	SOUNDSTAGE_API void CopyArray(float from[], float to[], int length);
	SOUNDSTAGE_API void DuplicateArrayAndReset(float from[], float to[], int length, float val);
	SOUNDSTAGE_API int CountPulses(float buffer[], int length, int channels, float lastSig[]);
	SOUNDSTAGE_API void MaracaProcessBuffer(float buffer[], int length, int channels, float amp, double& _phase, double _sampleDuration);
	SOUNDSTAGE_API void MaracaProcessAudioBuffer(float buffer[], float controlBuffer[], int length, int channels, double& _phase, double _sampleDuration);
	SOUNDSTAGE_API void processFader(float buffer[], int length, int channels, float bufferB[], int lengthB, bool aSig, bool bSig, bool samePercent, float lastpercent, float sliderPercent);
	SOUNDSTAGE_API float LoganTest();
	SOUNDSTAGE_API bool GetBinaryState(float buffer[], int length, int channels, float &lastBuf);
	SOUNDSTAGE_API bool IsPulse(float buffer[], int length);
	SOUNDSTAGE_API void CompressClip(float buffer[], int length);
	SOUNDSTAGE_API void MicFunction(float a[], float b[], int length, float val);
	SOUNDSTAGE_API void ColorTest(char a[]);
	SOUNDSTAGE_API int NoiseProcessBuffer(float buffer[], float& sample, int length, int channels, float frequency, int counter, int speedFrames, bool& updated);
	SOUNDSTAGE_API void GateProcessBuffer(float buffer[], int length, int channels, bool incoming, float controlBuffer[], bool bControlSig, float amp);
	SOUNDSTAGE_API float ClipSignalGenerator(float buffer[], float speedBuffer[], float ampBuffer[], float seqBuffer[], int length, float lastSeqGen[2], int channels, bool speedGen, bool ampGen, bool seqGen, float floatingBufferCount
		, int sampleBounds[2], float playbackSpeed, void* clip, int clipChannels, float amplitude, bool playdirection, bool looping, double _sampleDuration, int bufferCount, bool& active);
	SOUNDSTAGE_API void ADSRSignalGenerator(float buffer[], int length, int channels, int frames[], int& frameCount, bool active, float &ADSRvolume,
		float volumes[], float startVal, int& curFrame, bool sustaining);
	SOUNDSTAGE_API void KeyFrequencySignalGenerator(float buffer[], int length, int channels, int semitone, float keyMultConst, float& filteredVal );
	SOUNDSTAGE_API void XylorollMergeSignalsWithOsc(float buf[], int length, float buf1[], float buf2[]);
	SOUNDSTAGE_API void XylorollMergeSignalsWithoutOsc(float buf[], int length, float buf1[], float buf2[]);
	SOUNDSTAGE_API void OscillatorSignalGenerator(float buffer[], int length, int channels, double &_phase, float analogWave, float frequency, float amplitude, float prevAmplitude
		, float frequencyBuffer[], float amplitudeBuffer[], bool bFreqGen, bool bAmpGen, double _sampleDuration, double &dspTime);
	SOUNDSTAGE_API void addCombFilterSignal(float inputbuffer[], float addbuffer[], int length, float delayBufferL[], float delayBufferR[], int delaylength, float gain, int& inPoint, int& outPoint);
	SOUNDSTAGE_API void processCombFilterSignal(float buffer[], int length, float delayBufferL[], float delayBufferR[], int delaylength, float gain, int& inPoint, int& outPoint);
	SOUNDSTAGE_API void lowpassSignal(float buffer[], int length, float& lowpassL, float& lowpassR);
	SOUNDSTAGE_API void combineArrays(float buffer[], float bufferB[], int length, float levelA, float levelB);
	SOUNDSTAGE_API void ProcessWaveTexture(float buffer[], int length, void* pixels, unsigned char Ra, unsigned char Ga, unsigned char Ba, unsigned char Rb, unsigned char Gb, unsigned char Bb,
		int period, int waveheight, int wavewidth, int& lastWaveH, int& curWaveW);
}