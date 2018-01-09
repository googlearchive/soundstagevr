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

public class ControlCubeDeviceInterface : deviceInterface {

  public Vector3 percent;
  public ControlCubeSingleSignalGenerator[] signals;
  public omniJack[] outputs;
  public oscillatorSignalGenerator Osc1, Osc2;
  public cubeZone cubeManip;
  public dial volumeDial;
  public button muteButton;

  public override void Awake() {
    base.Awake();
    Setup(percent);
  }

  void Setup(Vector3 p) {
    percent = p;
    cubeManip.updateLines(percent);
    updatePercent(percent);
  }

  public override void hit(bool on, int ID = -1) {
    toggleMute(on);
  }

  void Update() {
    if (!muted) Osc1.amplitude = volumeDial.percent;
  }

  bool muted = false;
  public void toggleMute(bool on) {
    muted = on;
    Osc1.amplitude = muted ? 0 : volumeDial.percent;
  }

  public void updatePercent(Vector3 p) {
    percent = p;

    signals[0].value = (percent.x - .5f) * 2;
    signals[1].value = (percent.y - .5f) * 2;
    signals[2].value = (percent.z - .5f) * 2;

    Osc1.analogWave = percent.x;
    Osc1.frequency = 40 + 5000 * Mathf.Pow(percent.y, 2);
    Osc2.frequency = percent.z * 8;
    Osc2.amplitude = percent.z;
  }

  public override InstrumentData GetData() {
    ControlCubeData data = new ControlCubeData();

    data.deviceType = menuItem.deviceType.ControlCube;
    GetTransformData(data);

    data.jackOutID = new int[4];
    for (int i = 0; i < 4; i++) data.jackOutID[i] = outputs[i].transform.GetInstanceID();

    data.dimensionValues = new float[3];
    for (int i = 0; i < 3; i++) data.dimensionValues[i] = percent[i];

    data.muted = muted;

    data.volume = volumeDial.percent;

    return data;
  }

  public override void Load(InstrumentData d) {
    ControlCubeData data = d as ControlCubeData;

    base.Load(data);

    for (int i = 0; i < 4; i++) outputs[i].ID = data.jackOutID[i];
    for (int i = 0; i < 3; i++) percent[i] = data.dimensionValues[i];

    volumeDial.setPercent(data.volume);

    muteButton.startToggled = data.muted;

    Setup(percent);
  }
}

public class ControlCubeData : InstrumentData {
  public int[] jackOutID;
  public float[] dimensionValues;
  public bool muted;
  public float volume;
}