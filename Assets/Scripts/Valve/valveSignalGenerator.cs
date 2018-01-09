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

public class valveSignalGenerator : signalGenerator {

  public signalGenerator incoming, controlSig;
  public bool active = true;
  public float amp = 1f;

  [DllImport("SoundStageNative")] public static extern void SetArrayToSingleValue(float[] a, int length, float val);
  [DllImport("SoundStageNative")] public static extern void GateProcessBuffer(float[] buffer, int length, int channels, bool incoming, float[] controlBuffer, bool bControlSig, float amp);

  public override void processBuffer(float[] buffer, double dspTime, int channels) {
    if (!active) {
      SetArrayToSingleValue(buffer, buffer.Length, -1f);
      return;
    }

    float[] controlBuffer = new float[buffer.Length];
    if (controlSig != null) controlSig.processBuffer(controlBuffer, dspTime, channels);
    if (incoming != null) incoming.processBuffer(buffer, dspTime, channels);

    GateProcessBuffer(buffer, buffer.Length, channels, (incoming != null), controlBuffer, (controlSig != null), amp);
  }
}
