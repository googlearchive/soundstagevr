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

public class NoiseDeviceInterface : deviceInterface {

  int ID = 0;
  public dial speedDial;
  omniJack output;

  NoiseSignalGenerator gen;

  int texSize = 16;
  Texture2D tex;
  public Renderer texrend;
  Color32[] texpixels;

  float blackFrequency = .85f;

  public override void Awake() {
    base.Awake();
    gen = GetComponent<NoiseSignalGenerator>();
    output = GetComponentInChildren<omniJack>();
  }

  void Start() {
    tex = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);
    texpixels = new Color32[texSize * texSize];

    GenerateRandomTex();
    tex.SetPixels32(texpixels);
    tex.Apply(false);

    texrend.material.mainTexture = tex;
    texrend.material.SetTexture(Shader.PropertyToID("_Illum"), tex);
    texrend.material.SetColor("_TintColor", Color.white);
    texrend.material.SetFloat("_EmissionGain", .4f);
  }

  void GenerateRandomTex() {
    for (int i = 0; i < texSize; i++) {
      for (int i2 = 0; i2 < texSize; i2++) {
        byte s = 255;
        if (Random.value < blackFrequency) s = 0;
        texpixels[i2 * texSize + i] = new Color32(s, s, s, 255);
      }
    }
  }

  void Update() {
    if (gen.updated || output.near == null) {
      gen.updated = false;
      GenerateRandomTex();
      tex.SetPixels32(texpixels);
      tex.Apply(false);
    }

    gen.updatePercent(speedDial.percent);
  }

  public override InstrumentData GetData() {
    NoiseData data = new NoiseData();
    data.deviceType = menuItem.deviceType.Noise;

    GetTransformData(data);

    data.dialState = speedDial.percent;
    data.jackOutID = output.transform.GetInstanceID();

    return data;
  }

  public override void Load(InstrumentData d) {
    NoiseData data = d as NoiseData;
    base.Load(data);
    ID = data.ID;
    output.ID = data.jackOutID;
    speedDial.setPercent(data.dialState);
  }
}

public class NoiseData : InstrumentData {
  public float dialState;
  public int jackOutID;
}