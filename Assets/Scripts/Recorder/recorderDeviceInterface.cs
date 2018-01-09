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

public class recorderDeviceInterface : deviceInterface {
  public omniJack input, output, recordTrigger, playTrigger, backTrigger;
  public sliderNotched durSlider;
  waveTranscribeRecorder transcriber;
  public button[] buttons;

  int[] durations = new int[] { 300, 150, 60, 30, 10 };//{ 10,30,60,150,300 };

  public override void Awake() {
    base.Awake();
    transcriber = GetComponent<waveTranscribeRecorder>();
    durSlider = GetComponentInChildren<sliderNotched>();
  }

  void Update() {
    if (input.signal != transcriber.incoming) transcriber.incoming = input.signal;
    if (durations[durSlider.switchVal] != transcriber.duration) {
      transcriber.updateDuration(durations[durSlider.switchVal]);
    }
  }

  public override void hit(bool on, int ID = -1) {
    if (ID == 2 && on) transcriber.Back();
    if (ID == 3 && on) transcriber.Save();
    if (ID == 4 && on) transcriber.Flush();

    if (ID == 0) {
      if (on) buttons[1].keyHit(true);
      transcriber.recording = on;
    }
    if (ID == 1) {
      if (!on) buttons[0].keyHit(false);
      transcriber.playing = on;
    }
  }

  public override InstrumentData GetData() {
    RecorderData data = new RecorderData();
    data.deviceType = menuItem.deviceType.Recorder;
    GetTransformData(data);

    data.jackInID = input.transform.GetInstanceID();
    data.jackOutID = output.transform.GetInstanceID();
    data.recordTriggerID = recordTrigger.transform.GetInstanceID();
    data.playTriggerID = playTrigger.transform.GetInstanceID();
    data.backTriggerID = backTrigger.transform.GetInstanceID();
    data.dur = durSlider.switchVal;
    return data;
  }

  public override void Load(InstrumentData d) {
    RecorderData data = d as RecorderData;
    base.Load(data);
    input.ID = data.jackInID;
    output.ID = data.jackOutID;
    recordTrigger.ID = data.recordTriggerID;
    playTrigger.ID = data.playTriggerID;
    backTrigger.ID = data.backTriggerID;
    durSlider.setVal(data.dur);
  }
}

public class RecorderData : InstrumentData {
  public int jackOutID;
  public int jackInID;
  public int recordTriggerID;
  public int playTriggerID;
  public int backTriggerID;
  public int dur;
  public string audioFilename;
}