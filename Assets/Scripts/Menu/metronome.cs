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

public class metronome : componentInterface {
  public dial bpmDial, volumeDial;

  float bpm = 120f;
  float bpmpercent = .1f;

  float volumepercent = 0;

  public Transform rod;
  public TextMesh txt;

  void Awake() {
    bpmDial = GetComponentInChildren<dial>();
  }

  public void Reset() {
    float targ = 120;
    bpmpercent = (targ - 40) / 160;
    bpmDial.setPercent(bpmpercent);
    UpdateBPM();
  }

  public void SetBPM(float targ) {
    bpmpercent = (targ - 40) / 160;
    bpmDial.setPercent(bpmpercent);
    UpdateBPM();
  }

  public override void hit(bool on, int ID = -1) {
    if (ID == 0) masterControl.instance.toggleBeatUpdate(on);
    if (ID == 1 && on) masterControl.instance.resetClock();
  }

  void OnEnable() {
    bpm = masterControl.instance.bpm;
    bpmpercent = (bpm - 40) / 160;

    bpmDial.setPercent(bpmpercent);
    txt.text = bpm.ToString("N1");
  }

  bool rodDir = false;
  void Update() {
    float cyc = Mathf.Repeat(masterControl.instance.curCycle * 4, 1);

    if (cyc < 0.5f) {
      rod.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(-80, 80, cyc * 2));
      if (!rodDir) {
        rodDir = true;
      }
    } else {
      rod.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(80, -80, (cyc - .5f) * 2));
      if (rodDir) {
        rodDir = false;
      }
    }

    if (volumepercent != volumeDial.percent) {
      volumepercent = volumeDial.percent;
      masterControl.instance.metronomeClick.volume = Mathf.Clamp01(volumepercent - .1f);
    }

    if (bpmpercent != bpmDial.percent) UpdateBPM();
  }

  void UpdateBPM() {
    bpmpercent = bpmDial.percent;
    bpm = bpmpercent * 160 + 40;
    masterControl.instance.setBPM(bpm);
    txt.text = bpm.ToString("N0");
  }
}
