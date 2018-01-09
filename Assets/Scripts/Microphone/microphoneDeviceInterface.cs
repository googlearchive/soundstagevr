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

public class microphoneDeviceInterface : deviceInterface {

  MicrophoneSignalGenerator signal;
  omniJack output;
  omniPlug outputplug;
  dial ampDial;

  float amp = .25f;
  public override void Awake() {
    base.Awake();
    signal = GetComponent<MicrophoneSignalGenerator>();
    output = GetComponentInChildren<omniJack>();
    ampDial = GetComponentInChildren<dial>();
    microphoneDeviceInterface[] otherMics = FindObjectsOfType<microphoneDeviceInterface>();
    for (int i = 0; i < otherMics.Length; i++) {
      if (otherMics[i] != this) Destroy(otherMics[i].gameObject);
    }
  }

  void Update() {
    if (output.near != outputplug) {
      outputplug = output.near;
      if (outputplug == null) {
        signal.freqBuffers.Clear();
        System.GC.Collect();
      }
    }

    if (amp != ampDial.percent) {
      amp = ampDial.percent;
      signal.amp = amp * 4;
    }
  }

  public override void hit(bool on, int ID = -1) {
    signal.active = on;
  }

  public override InstrumentData GetData() {
    MicrophoneData data = new MicrophoneData();
    data.deviceType = menuItem.deviceType.Microphone;
    GetTransformData(data);
    data.jackOutID = output.transform.GetInstanceID();
    data.amp = amp;
    return data;
  }

  public override void Load(InstrumentData d) {
    MicrophoneData data = d as MicrophoneData;
    base.Load(data);
    output.ID = data.jackOutID;
    ampDial.setPercent(data.amp);
  }
}

public class MicrophoneData : InstrumentData {
  public int jackOutID;
  public float amp;
}