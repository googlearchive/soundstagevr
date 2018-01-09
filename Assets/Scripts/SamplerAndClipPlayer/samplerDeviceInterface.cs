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

public class samplerDeviceInterface : deviceInterface {
  public dial speedDial, volumeDial;
  public omniJack speedInput, volumeInput, controlInput, output;
  public basicSwitch dirSwitch, loopSwitch;
  public button playButton, turntableButton;
  public sliderUneven headSlider, tailSlider;
  public GameObject turntableObject;
  clipPlayerComplex player;
  signalGenerator seq;

  bool turntableOn = false;
  public override void Awake() {
    base.Awake();
    player = GetComponent<clipPlayerComplex>();
  }

  public override void hit(bool on, int ID = -1) {
    if (ID == 0) player.togglePause(on);
    if (ID == 1 && on) player.Back();
    if (ID == 2) {
      turntableObject.SetActive(on);
      turntableOn = on;
    }
  }

  public void playEvent(bool on) {
    playButton.phantomHit(on);
  }

  int[] lastSignal = new int[] { 0, 0 };
  void Update() {
    float mod = dirSwitch.switchVal ? 1 : -1;
    if (dirSwitch.switchVal != player.playdirection) player.playdirection = dirSwitch.switchVal;

    player.playbackSpeed = Mathf.Pow(speedDial.percent, 2) * 4 * mod;
    player.amplitude = volumeDial.percent * 2;

    if (loopSwitch.switchVal != player.looping) player.looping = loopSwitch.switchVal;

    if (player.speedGen != speedInput.signal) player.speedGen = speedInput.signal;
    if (player.ampGen != volumeInput.signal) player.ampGen = volumeInput.signal;
    if (player.seqGen != controlInput.signal) player.seqGen = controlInput.signal;

    if (seq != controlInput.signal) seq = controlInput.signal;

    if (tailSlider.percent != player.trackBounds.y) {
      player.trackBounds.y = tailSlider.percent;
      player.updateTrackBounds();
    }
    if (headSlider.percent != player.trackBounds.x) {
      player.trackBounds.x = headSlider.percent;
      player.updateTrackBounds();
    }

    tailSlider.bounds.y = headSlider.transform.localPosition.x;
    headSlider.bounds.x = tailSlider.transform.localPosition.x;
  }

  public override InstrumentData GetData() {
    SamplerData data = new SamplerData();
    data.deviceType = menuItem.deviceType.Sampler;
    GetTransformData(data);
    data.speedDial = speedDial.percent;
    data.ampDial = volumeDial.percent;

    data.file = GetComponent<samplerLoad>().CurFile;
    data.label = GetComponent<samplerLoad>().CurTapeLabel;

    data.jackInAmpID = volumeInput.transform.GetInstanceID();
    data.jackInSpeedID = speedInput.transform.GetInstanceID();
    data.jackInSeqID = controlInput.transform.GetInstanceID();
    data.jackOutID = output.transform.GetInstanceID();
    data.dirSwitch = dirSwitch.switchVal;
    data.loopSwitch = loopSwitch.switchVal;
    data.headPos = headSlider.percent;
    data.tailPos = tailSlider.percent;

    data.playToggle = playButton.isHit;

    data.turntable = turntableOn;
    data.turntablePos = turntableObject.transform.localPosition;
    data.turntableRot = turntableObject.transform.localRotation;

    return data;
  }

  public override void Load(InstrumentData d) {
    SamplerData data = d as SamplerData;
    base.Load(data);
    speedDial.setPercent(data.speedDial);
    volumeDial.setPercent(data.ampDial);
    GetComponent<samplerLoad>().SetSample(data.label, data.file);

    volumeInput.ID = data.jackInAmpID;
    speedInput.ID = data.jackInSpeedID;
    controlInput.ID = data.jackInSeqID;
    output.ID = data.jackOutID;

    playButton.startToggled = data.playToggle;
    dirSwitch.setSwitch(data.dirSwitch);
    loopSwitch.setSwitch(data.loopSwitch);
    headSlider.setPercent(data.headPos);
    tailSlider.setPercent(data.tailPos);

    turntableButton.startToggled = data.turntable;
    if (data.turntablePos != Vector3.zero) turntableObject.transform.localPosition = data.turntablePos;
    if (data.turntableRot != Quaternion.identity) turntableObject.transform.localRotation = data.turntableRot;
  }
}

public class SamplerData : InstrumentData {
  public string label;
  public string file;
  public float ampDial;
  public float speedDial;
  public int jackInAmpID;
  public int jackInSpeedID;
  public int jackInSeqID;
  public int jackOutID;
  public bool dirSwitch;

  public bool playToggle;

  public bool loopSwitch;
  public float headPos = 0;
  public float tailPos = 1;

  public bool turntable;
  public Vector3 turntablePos;
  public Quaternion turntableRot;
}
