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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

public class midiNoteOut : signalGenerator {
  public int ID;

  midiOutDeviceInterface _deviceinterface;

  signalGenerator incoming;
  public omniJack input;

  public GameObject statusObject;

  public Renderer[] glowRends;
  public TextMesh label;

  public bool noteOn = false;

  [DllImport("SoundStageNative")]
  public static extern bool GetBinaryState(float[] buffer, int length, int channels, ref float lastBuf);

  public override void Awake() {
    base.Awake();
    input = GetComponentInChildren<omniJack>();
    _deviceinterface = GetComponentInParent<midiOutDeviceInterface>();
  }

  void Start() {
    glowRends[1].material.SetFloat("_EmissionGain", .3f);
    statusObject.SetActive(noteOn);
  }

  public void SetAppearance(string s, float h) {
    label.text = s;

    for (int i = 0; i < glowRends.Length; i++) glowRends[i].material.SetColor("_TintColor", Color.HSVToRGB(h, .5f, .7f));
  }

  void Update() {
    if (input.signal != incoming) incoming = input.signal;
    statusObject.SetActive(noteOn);
  }

  float lastBuffer = -1;
  public override void processBuffer(float[] buffer, double dspTime, int channels) {
    if (incoming == null) return;
    incoming.processBuffer(buffer, dspTime, channels);

    bool on = GetBinaryState(buffer, buffer.Length, channels, ref lastBuffer);

    if (on != noteOn) {
      noteOn = on;
      _deviceinterface.receiveMidiNote(ID, noteOn);
    }
  }
}
