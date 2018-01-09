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
using System.Runtime.InteropServices;

public class airhornDeviceInterface : deviceInterface {

  airhornSignalGenerator signal;
  airhornUI _airhornUI;

  public clipPlayerSimple[] samplers;
  public AudioSource defaultAudioSource;
  public AudioClip offClip;
  public omniJack jackOut;

  public override void Awake() {
    base.Awake();
    signal = GetComponent<airhornSignalGenerator>();
    _airhornUI = GetComponentInChildren<airhornUI>();
  }

  void Start() {
    if (alreadyLoaded) return;
    for (int i = 0; i < 4; i++) {
      samplers[i].GetComponent<samplerLoad>().SetSample("Airhorn", "APP" + System.IO.Path.DirectorySeparatorChar + "SFX" + System.IO.Path.DirectorySeparatorChar + "Airhorn.wav");
    }

  }

  public void PlaySample(bool on, int id) {
    signal.curPlayer = samplers[id];
    for (int i = 0; i < 4; i++) {
      if (on && id == i) {
        if (samplers[i].loaded) samplers[i].Play();
        else defaultAudioSource.PlayOneShot(offClip, .4f);
      } else samplers[i].Stop();
    }
  }

  void OnDestroy() {
    if (_airhornUI.transform.parent != transform) Destroy(_airhornUI.gameObject);
  }

  public override InstrumentData GetData() {
    AirhornData data = new AirhornData();
    data.deviceType = menuItem.deviceType.Airhorn;
    GetTransformData(data);
    data.jackOutID = jackOut.transform.GetInstanceID();

    data.samples = new string[4][];
    for (int i = 0; i < 4; i++) {
      data.samples[i] = new string[] { "", "" };
      samplers[i].GetComponent<samplerLoad>().getTapeInfo(out data.samples[i][0], out data.samples[i][1]);
    }
    return data;
  }

  bool alreadyLoaded = false;
  public override void Load(InstrumentData d) {
    AirhornData data = d as AirhornData;
    base.Load(data);
    for (int i = 0; i < 4; i++) {
      if (data.samples[i][0] != "") samplers[i].GetComponent<samplerLoad>().SetSample(data.samples[i][0], data.samples[i][1]);
      else samplers[i].GetComponent<samplerLoad>().ForceEject();
    }

    alreadyLoaded = true;
    jackOut.ID = data.jackOutID;
  }
}

public class AirhornData : InstrumentData {
  public int jackOutID;
  public string[][] samples;
}
