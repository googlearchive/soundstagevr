// Copyright 2017 Google LLC
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;

public class reverbSignalGenerator : signalGenerator {
  public signalGenerator incoming;

  [DllImport("SoundStageNative")] public static extern void SetArrayToSingleValue(float[] a, int length, float val);
  [DllImport("SoundStageNative")] public static extern void DuplicateArrayAndReset(float[] from, float[] to, int length, float val);
  [DllImport("SoundStageNative")] public static extern void lowpassSignal(float[] buffer, int length, ref float lowpassL, ref float lowpassR);
  [DllImport("SoundStageNative")] public static extern void combineArrays(float[] buffer, float[] bufferB, int length, float levelA, float levelB);

  public float decayTime = 1.0f;

  public float sendLevel = 0.1f;

  float prevDecayTime;

  int[] delays = {

          2465, 2755, 3211, 3531, 3871, 4131, //comb filter
        597, 195, 63, 105, 91, 75 //all pass filter      
    };

  CombFilter[] cf;

  float[] bufferCopy;

  public override void Awake() {
    base.Awake();
    prevDecayTime = decayTime;

    cf = new CombFilter[11];
    for (int i = 0; i < 6; i++) cf[i] = new CombFilter(delays[i], Mathf.Pow(10f, -3.0f / (decayTime * 44100) * delays[i]));
    for (int i = 6; i < 11; i++) cf[i] = new CombFilter(delays[i], .7f);

    bufferCopy = new float[MAX_BUFFER_LENGTH];
  }

  void Update() {
    if (decayTime != prevDecayTime) {
      for (int i = 0; i < 6; i++) cf[i].updateGain(Mathf.Pow(10f, -3.0f / (decayTime * 44100) * delays[i]));
      prevDecayTime = decayTime;
    }
  }

  float lowpassL = 0;
  float lowpassR = 0;
  public override void processBuffer(float[] buffer, double dspTime, int channels) {
    if (!incoming) {
      SetArrayToSingleValue(buffer, buffer.Length, 0);
      return;
    }

    incoming.processBuffer(buffer, dspTime, channels);

    if (bufferCopy.Length != buffer.Length)
      System.Array.Resize(ref bufferCopy, buffer.Length);

    DuplicateArrayAndReset(buffer, bufferCopy, buffer.Length, .4f);


    for (int i = 0; i < 6; i++) cf[i].addSignal(bufferCopy, buffer, buffer.Length);
    for (int i = 6; i < 9; i++) cf[i].processSignal(buffer, buffer.Length);

    lowpassSignal(buffer, buffer.Length, ref lowpassL, ref lowpassR);

    for (int i = 9; i < 11; i++) cf[i].processSignal(buffer, buffer.Length);

    combineArrays(buffer, bufferCopy, buffer.Length, sendLevel, 1);

  }
}

public class CombFilter {
  float[] delayBufferL;
  float[] delayBufferR;

  float gain = 0;
  int inPoint;
  int outPoint;

  [DllImport("SoundStageNative")]
  public static extern void addCombFilterSignal(float[] inputbuffer, float[] addbuffer, int length, float[] delayBufferL, float[] delayBufferR, int delaylength, float gain, ref int inPoint, ref int outPoint);
  [DllImport("SoundStageNative")]
  public static extern void processCombFilterSignal(float[] buffer, int length, float[] delayBufferL, float[] delayBufferR, int delaylength, float gain, ref int inPoint, ref int outPoint);

  public void updateGain(float g) {
    gain = g;
  }

  public CombFilter(int d, float g) {
    delayBufferL = new float[d + 1];
    delayBufferR = new float[d + 1];
    gain = g;

    outPoint = inPoint - d;
    if (outPoint < 0) outPoint += delayBufferL.Length;
  }

  public void addSignal(float[] inputbuffer, float[] addbuffer, int length) {
    addCombFilterSignal(inputbuffer, addbuffer, length, delayBufferL, delayBufferR, delayBufferR.Length, gain, ref inPoint, ref outPoint);
  }

  public void processSignal(float[] buffer, int length) {
    processCombFilterSignal(buffer, length, delayBufferL, delayBufferR, delayBufferR.Length, gain, ref inPoint, ref outPoint);
  }
}
