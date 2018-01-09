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

public class keyFrequencySignalGenerator : signalGenerator {
  float keyMultConst = Mathf.Pow(2, 1f / 12);

  public int octave = 0;
  int curKey = -1;
  int semitone = 0;

  [DllImport("SoundStageNative")]
  public static extern void KeyFrequencySignalGenerator(float[] buffer, int length, int channels, int semitone, float keyMultConst, ref float filteredVal);

  public void UpdateKey(int k) {
    curKey = k;
    semitone = k - 9 + octave * 12;
  }

  public float getMult(int k) {
    semitone = k - 9 + octave * 12;
    return Mathf.Pow(keyMultConst, semitone);
  }

  public void updateOctave(int n) {
    octave = n;
    semitone = curKey - 9 + octave * 12;
  }
  float filteredVal = 0;

  public override void processBuffer(float[] buffer, double dspTime, int channels) {
    KeyFrequencySignalGenerator(buffer, buffer.Length, channels, semitone, keyMultConst, ref filteredVal);
  }
}