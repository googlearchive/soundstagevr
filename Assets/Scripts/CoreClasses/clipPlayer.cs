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
public class clipPlayer : signalGenerator {

  public bool loaded = false;
  public float[] clipSamples;
  public int clipChannels = 2;
  public int[] sampleBounds = new int[] { 0, 0 };
  public float floatingBufferCount = 0;
  public int bufferCount = 0;
  public Vector2 trackBounds = new Vector2(0, 1);

  public void UnloadClip() {
    loaded = false;
    toggleWaveDisplay(false);
  }

  public GCHandle m_ClipHandle;

  public void LoadSamples(float[] samples, GCHandle _cliphandle, int channels) {

    m_ClipHandle = _cliphandle;
    clipChannels = channels;
    clipSamples = samples;
    sampleBounds[0] = (int)((clipSamples.Length / clipChannels - 1) * (trackBounds.x));
    sampleBounds[1] = (int)((clipSamples.Length / clipChannels - 1) * (trackBounds.y));
    floatingBufferCount = bufferCount = sampleBounds[0];

    toggleWaveDisplay(true);
    DrawClipTex();
    loaded = true;
  }

  public virtual void toggleWaveDisplay(bool on) {
  }

  public virtual void DrawClipTex() {
  }
}
