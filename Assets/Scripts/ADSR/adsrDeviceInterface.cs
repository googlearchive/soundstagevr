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

﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class adsrDeviceInterface : deviceInterface {
  adsrInterface _adsrInterface;
  adsrSignalGenerator adsrSignal;
  public omniJack input, output;
  public GameObject embeddedAudio;

  public override void Awake() {
    base.Awake();
    _adsrInterface = GetComponentInChildren<adsrInterface>();
    adsrSignal = GetComponent<adsrSignalGenerator>();
    adsrSignal._devinterface = this;
    adsrSignal.durations = _adsrInterface.durations;
    adsrSignal.volumes = _adsrInterface.volumes;
  }

  void Update() {
    if (input.signal != adsrSignal.incoming) {
      adsrSignal.incoming = input.signal;
    }

    if (embeddedAudio.activeSelf != (output.near == null)) {
      embeddedAudio.SetActive(output.near == null);
    }
  }

  bool buttonState = false;
  bool signalState = false;
  public void pulseUpdate(bool on) {
    signalState = on;
    adsrSignal.hit(buttonState || signalState);
  }

  public override void hit(bool on, int ID = -1) {
    buttonState = on;
    adsrSignal.hit(buttonState || signalState);
  }

  public override InstrumentData GetData() {
    ADSRData data = new ADSRData();
    data.deviceType = menuItem.deviceType.ADSR;
    GetTransformData(data);

    data.ADSRdata = new Vector2[3];
    for (int i = 0; i < 3; i++) {
      data.ADSRdata[i] = _adsrInterface.xyHandles[i].percent;
    }

    data.jackOutID = output.transform.GetInstanceID();
    data.jackInID = input.transform.GetInstanceID();

    return data;
  }

  public override void Load(InstrumentData d) {
    ADSRData data = d as ADSRData;
    base.Load(data);

    output.ID = data.jackOutID;
    input.ID = data.jackInID;

    for (int i = 0; i < 3; i++) _adsrInterface.xyHandles[i].setPercent(data.ADSRdata[i]);
    _adsrInterface.setDefaults = false;

  }

}

public class ADSRData : InstrumentData {
  public Vector2[] ADSRdata;
  public int jackInID;
  public int jackOutID;
}
