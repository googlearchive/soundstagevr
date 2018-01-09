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

﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public class adsrSignalGenerator : signalGenerator {
  public float[] durations = new float[] { 1f, 1.4f, 1.2f };
  public float[] volumes = new float[] { 1, 0.8f };
  public bool active = false;

  int[] frames = new int[] { 1, 1, 1, 1 };

  float[] lastDur = new float[] { 0, 0, 0 };
  float[] lastVol = new float[] { 0, 0 };
  float ADSRvolume = -1;

  double lastIncomingDspTime;
  public bool sustaining = false;
  float sustainTime = 0;
  double _lastPhase = 0;

  float[] pulseBuffer;

  public signalGenerator incoming;
  public adsrDeviceInterface _devinterface;

  [DllImport("SoundStageNative", EntryPoint = "ADSRSignalGenerator")]
  public static extern void ADSRSignalGenerator(float[] buffer, int length, int channels, int[] frames, ref int frameCount, bool active, ref float ADSRvolume,
  float[] volumes, float startVal, ref int curFrame, bool sustaining);

  [DllImport("SoundStageNative")]
  public static extern bool GetBinaryState(float[] buffer, int length, int channels, ref float lastBuf);

  public override void Awake() {
    base.Awake();
    pulseBuffer = new float[MAX_BUFFER_LENGTH];
  }

  public void hit(bool on) {
    if (on == sustaining) return;

    if (on) {
      active = true;
      _phase = 0;
      sustainTime = 0;

      startVal = 0;

      adsrValUpdate();

      curFrame = 0;
      frameCount = 0;
    } else if (curFrame == 0 && frameCount == 0) active = false;


    sustaining = on;
  }

  float startVal = 0;

  void adsrValUpdate() {
    bool unchanged = true;
    for (int i = 0; i < 3; i++) {
      if (durations[i] != lastDur[i]) {
        unchanged = false;
        lastDur[i] = durations[i];
      }
    }

    for (int i = 0; i < 2; i++) {
      if (volumes[i] != lastVol[i]) {
        unchanged = false;
        lastVol[i] = volumes[i];
      }
    }

    if (!unchanged) {
      frames[0] = Mathf.RoundToInt(durations[0] * (float)_sampleRate);
      frames[1] = Mathf.RoundToInt(durations[1] * (float)_sampleRate);
      frames[3] = Mathf.RoundToInt(durations[2] * (float)_sampleRate);

      if (frames[0] == 0) frames[0] = 1;
      if (frames[1] == 0) frames[1] = 1;
      if (frames[3] == 0) frames[3] = 1;
    }
  }

  float getADSR() {
    switch (curFrame) {
      case 0:
        return startVal + (volumes[0] - startVal) * frameCount / frames[0];

      case 1:
        return volumes[0] + (volumes[1] - volumes[0]) * (float)frameCount / (float)frames[1];

      case 2:
        return volumes[1];

      case 3:
        return volumes[1] * (1f - (float)frameCount / (float)frames[3]);
      case 4:
        return 0;
      default:
        break;
    }
    return 0;
  }

  float lastBuffer = -1;
  int curFrame = 0;
  int frameCount = 0;
  public override void processBuffer(float[] buffer, double dspTime, int channels) {
    ADSRSignalGenerator(buffer, buffer.Length, channels, frames, ref frameCount, active, ref ADSRvolume, volumes, startVal, ref curFrame, sustaining);

    if (incoming != null && _devinterface != null) {
      if (pulseBuffer.Length != buffer.Length)
        System.Array.Resize(ref pulseBuffer, buffer.Length);

      incoming.processBuffer(pulseBuffer, dspTime, channels);
      bool on = GetBinaryState(pulseBuffer, pulseBuffer.Length, channels, ref lastBuffer);
      _devinterface.pulseUpdate(on);
    }
  }


}
