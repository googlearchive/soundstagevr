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

public class NoiseSignalGenerator : signalGenerator {

  float speedPercent = 1;
  int speedFrames = 1;

  int maxLength = 11025; //  44100 / 2 - 1/4 second with sample rate of 44.1k
  int counter = 0;

  float curSample = -1.0f;

  [DllImport("SoundStageNative")]
  public static extern int NoiseProcessBuffer(float[] buffer, ref float sample, int length, int channels, float frequency, int counter, int speedFrames, ref bool updated);

  public bool updated = false;

  public void updatePercent(float per) {
    if (speedPercent == per) return;
    speedPercent = per;
    speedFrames = Mathf.RoundToInt(maxLength * (1 - per));
  }

  public override void processBuffer(float[] buffer, double dspTime, int channels) {
    counter = NoiseProcessBuffer(buffer, ref curSample, buffer.Length, channels, speedPercent, counter, speedFrames, ref updated);
  }
}
