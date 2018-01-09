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

public class looperDeviceInterface : deviceInterface {

  public omniJack input, output, recordTrigger, playTrigger;
  public sliderNotched durSlider;
  waveTranscribeLooper transcriber;
  public button[] buttons;

  public TextMesh countdownText;

  float[] durations = new float[] { 32, 16, 8, 4, 1 };

  public int curSliderVal = 0;
  public double period = .0625;

  beatTracker _beatManager;
  AudioSource audioSource;

  public override void Awake() {
    base.Awake();
    transcriber = GetComponent<waveTranscribeLooper>();
    audioSource = GetComponent<AudioSource>();
    durSlider = GetComponentInChildren<sliderNotched>();
    _beatManager = ScriptableObject.CreateInstance<beatTracker>();
  }

  void Start() {
    _beatManager.setTrigger(onBeatEvent);
    _beatManager.updateBeatNoTriplets(2);
  }

  void OnDestroy() {
    Destroy(_beatManager);
  }

  public bool playClick = false;
  void Update() {
    if (input.signal != transcriber.incoming) transcriber.incoming = input.signal;

    if (playClick) {
      audioSource.Play();
      playClick = false;
    }
    if (curSliderVal != durSlider.switchVal || period != masterControl.instance.measurePeriod) {

      period = masterControl.instance.measurePeriod;
      curSliderVal = durSlider.switchVal;
      transcriber.updateDuration(durations[durSlider.switchVal], period);
    }

    countdownText.gameObject.SetActive(recordCountdown || playCountdown);
    if (recordCountdown || playCountdown) {
      countdownText.transform.localRotation = Quaternion.Euler(0, 180, countdownText.transform.parent.localRotation.eulerAngles.z);
      if (recordCountdown) countdownText.text = recCountdownRemaining.ToString();
      else countdownText.text = "";
    }
  }

  void RecordCountdown() {
    recordCountdown = true;
    if (!transcriber.playing) {
      if (playCountdown) recCountdownRemaining = playCountdownRemaining;
      else recCountdownRemaining = 4;
    } else {
      transcriber.requestRecord(true);
    }
  }

  public bool recordCountdown = false;

  public int recCountdownRemaining = 0;

  int playCountdownRemaining = 0;
  public bool playCountdown = false;

  void PlayCountdown() {
    if (recordCountdown) playCountdownRemaining = recCountdownRemaining;
    else playCountdownRemaining = 1;
    playCountdown = true;
  }

  void onBeatEvent() {
    if (recordCountdown && !transcriber.playing) {
      transcriber.requestRecord(false);
      recCountdownRemaining--;
      playClick = true;
      if (recCountdownRemaining == 0) {
        recordCountdown = false;
        transcriber.Record();
        buttons[1].phantomHit(true);
      }
    }

    if (playCountdown) {
      playCountdownRemaining--;
      if (playCountdownRemaining == 0) {
        playCountdown = false;
        transcriber.Back();
        transcriber.playing = true;
      }
    }
  }

  void StartRecord(bool on) {
    if (on) buttons[1].keyHit(true);
    transcriber.recording = false;
  }

  public override void hit(bool on, int ID = -1) {
    if (ID == 3 && on) transcriber.Save();
    if (ID == 4 && on) transcriber.Flush();

    if (ID == 0) {
      if (on) {
        RecordCountdown();

      } else {
        transcriber.recording = false;
        recordCountdown = false;
        transcriber.requestRecord(false);
      }

    }
    if (ID == 1) {
      if (on) PlayCountdown();
      else {
        playCountdown = false;
        buttons[0].keyHit(false);
        transcriber.Back();
        transcriber.playing = false;
      }
    }
  }

  public override InstrumentData GetData() {
    LooperData data = new LooperData();
    data.deviceType = menuItem.deviceType.Looper;
    GetTransformData(data);

    data.jackInID = input.transform.GetInstanceID();
    data.jackOutID = output.transform.GetInstanceID();
    data.recordTriggerID = recordTrigger.transform.GetInstanceID();
    data.playTriggerID = playTrigger.transform.GetInstanceID();
    data.dur = durSlider.switchVal;
    return data;
  }

  public override void Load(InstrumentData d) {
    LooperData data = d as LooperData;
    base.Load(data);
    input.ID = data.jackInID;
    output.ID = data.jackOutID;
    recordTrigger.ID = data.recordTriggerID;
    playTrigger.ID = data.playTriggerID;
    durSlider.setVal(data.dur);
  }
}
public class LooperData : InstrumentData {
  public int jackOutID;
  public int jackInID;
  public int recordTriggerID;
  public int playTriggerID;
  public int dur;
}