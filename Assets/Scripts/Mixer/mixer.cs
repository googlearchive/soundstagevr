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
using System.Collections.Generic;
using System.Runtime.InteropServices;

public class mixer : signalGenerator {
  public List<signalGenerator> incoming = new List<signalGenerator>();

  [DllImport("SoundStageNative")] public static extern void SetArrayToSingleValue(float[] a, int length, float val);
  [DllImport("SoundStageNative")] public static extern void AddArrays(float[] a, float[] b, int length);
  const int MAX_COUNT = 32; // It's very important to enforce this gracefully. Feel free to change the number, but must be enforced in game.
  float[][] b;

  public override void Awake() {
    base.Awake();
    b = new float[MAX_COUNT][];
    for (int i = 0; i < MAX_COUNT; ++i) {
      b[i] = new float[MAX_BUFFER_LENGTH];
    }
  }

  public override void processBuffer(float[] buffer, double dspTime, int channels) {
    int count = incoming.Count;

    for (int i = 0; i < count; i++) {
      if (buffer.Length != b[i].Length)
        System.Array.Resize(ref b[i], buffer.Length);

      SetArrayToSingleValue(b[i], buffer.Length, 0.0f);

      if (i < incoming.Count) {
        if (incoming[i] != null) incoming[i].processBuffer(b[i], dspTime, channels);
      }
    }

    for (int i = 0; i < count; i++) AddArrays(buffer, b[i], buffer.Length);
  }
}
