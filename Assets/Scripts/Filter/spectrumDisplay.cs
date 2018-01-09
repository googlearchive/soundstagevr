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

public class spectrumDisplay : MonoBehaviour {
  public AudioSource source;
  int texW = 256;
  int texH = 32;
  Texture2D tex;
  public Renderer texrend;
  Color32[] texpixels;

  bool active = false;

  float[] spectrum;

  void Start() {
    spectrum = new float[texW];

    tex = new Texture2D(texW, texH, TextureFormat.RGBA32, false);
    texpixels = new Color32[texW * texH];

    for (int i = 0; i < texpixels.Length; i++) texpixels[i] = new Color32(0, 0, 0, 255);
    tex.SetPixels32(texpixels);
    tex.Apply(false);

    texrend.material.mainTexture = tex;
    texrend.material.SetTexture(Shader.PropertyToID("_Illum"), tex);
    texrend.material.SetColor("_EmissionColor", Color.HSVToRGB(10 / 400f, 98 / 255f, 1f));
    texrend.material.SetFloat("_EmissionGain", .4f);
  }

  const float spectrumMult = 5;
  void GenerateTex() {
    for (int i = 0; i < texW; i++) {
      for (int i2 = 0; i2 < texH; i2++) {
        byte s = 0;
        if (spectrum[i] * spectrumMult * texH >= i2) s = 255;
        texpixels[i2 * texW + i] = new Color32(s, s, s, 255);
      }
    }
  }

  public void toggleActive(bool on) {
    active = on;
    if (!active) {
      for (int i = 0; i < texpixels.Length; i++) texpixels[i] = new Color32(0, 0, 0, 255);
      tex.SetPixels32(texpixels);
      tex.Apply(false);
    }
  }

  void Update() {
    if (!active) return;

    source.GetSpectrumData(spectrum, 0, FFTWindow.Rectangular);
    GenerateTex();
    tex.SetPixels32(texpixels);
    tex.Apply(false);
  }
}
