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

public class touchpadDeviceInterface : deviceInterface {

  touchpadSignalGenerator pad;
  omniJack output;

  public override void Awake() {
    base.Awake();
    pad = GetComponent<touchpadSignalGenerator>();
    output = GetComponentInChildren<omniJack>();
  }

  public override void hit(bool on, int ID = -1) {
    pad.signalOn = on;
  }

  public override InstrumentData GetData() {
    TouchPadData data = new TouchPadData();
    data.deviceType = menuItem.deviceType.TouchPad;
    GetTransformData(data);

    data.jackOutID = output.transform.GetInstanceID();
    return data;
  }

  public override void Load(InstrumentData d) {
    TouchPadData data = d as TouchPadData;
    base.Load(data);
    output.ID = data.jackOutID;
  }
}

public class TouchPadData : InstrumentData {
  public int jackOutID;
}
