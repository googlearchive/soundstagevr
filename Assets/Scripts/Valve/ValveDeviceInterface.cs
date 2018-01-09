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

public class ValveDeviceInterface : deviceInterface {
  public omniJack input, output, controlInput;
  dial ampDial;
  basicSwitch activeSwitch;
  valveSignalGenerator signal;

  public override void Awake() {
    base.Awake();
    ampDial = GetComponentInChildren<dial>();
    activeSwitch = GetComponentInChildren<basicSwitch>();
    signal = GetComponent<valveSignalGenerator>();
  }

  void Update() {
    signal.amp = ampDial.percent;
    signal.active = activeSwitch.switchVal;
    if (signal.incoming != input.signal) signal.incoming = input.signal;
    if (signal.controlSig != controlInput.signal) signal.controlSig = controlInput.signal;
  }

  public override InstrumentData GetData() {
    ValveData data = new ValveData();
    data.deviceType = menuItem.deviceType.Valve;
    GetTransformData(data);

    data.dialState = ampDial.percent;
    data.switchState = activeSwitch.switchVal;

    data.jackInID = input.transform.GetInstanceID();
    data.jackOutID = output.transform.GetInstanceID();
    data.jackControlID = controlInput.transform.GetInstanceID();

    return data;
  }

  public override void Load(InstrumentData d) {
    ValveData data = d as ValveData;
    base.Load(data);

    input.ID = data.jackInID;
    output.ID = data.jackOutID;
    controlInput.ID = data.jackControlID;

    ampDial.setPercent(data.dialState);
    activeSwitch.setSwitch(data.switchState);
  }
}

public class ValveData : InstrumentData {
  public float dialState;
  public bool switchState;
  public int jackOutID;
  public int jackInID;
  public int jackControlID;
}
