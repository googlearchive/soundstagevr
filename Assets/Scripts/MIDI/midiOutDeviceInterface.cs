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
public class midiOutDeviceInterface : deviceInterface {

  public GameObject midiCCOutprefab;
  public GameObject midiNoteOutprefab;

  public yHandle CChandle, notehandle;

  List<midiCCout> CClist = new List<midiCCout>();
  List<midiNoteOut> Notelist = new List<midiNoteOut>();

  midiComponentInterface _midiComponentInterface;

  [DllImport("SoundStageNative")]
  public static extern void SetArrayToSingleValue(float[] a, int length, float val);

  public override void Awake() {
    base.Awake();
    _midiComponentInterface = GetComponentInChildren<midiComponentInterface>();
  }

  private void OnAudioFilterRead(float[] buffer, int channels) {
    double dspTime = AudioSettings.dspTime;

    for (int i = 0; i < CClist.Count; i++) {
      CClist[i].processBuffer(buffer, dspTime, channels);
    }

    for (int i = 0; i < Notelist.Count; i++) {
      Notelist[i].processBuffer(buffer, dspTime, channels);
    }
  }

  void spawnCC() {
    midiCCout m = (Instantiate(midiCCOutprefab, transform, false) as GameObject).GetComponent<midiCCout>();
    m.transform.localPosition = new Vector3(-0.09f, -.06f + CClist.Count * -.04f, 0);
    m.transform.localRotation = Quaternion.identity;
    string s = "CC" + (CClist.Count + 1);
    m.SetAppearance(s, .25f);
    CClist.Add(m);
  }

  void spawnNote() {
    midiNoteOut m = (Instantiate(midiNoteOutprefab, transform, false) as GameObject).GetComponent<midiNoteOut>();
    m.transform.localPosition = new Vector3(0.09f, -.06f + Notelist.Count * -.04f, 0);
    m.transform.localRotation = Quaternion.identity;
    m.ID = Notelist.Count + 48;
    string s = m.ID + " " + ((Midi.Pitch)m.ID).ToString().Replace("Sharp", "#");
    m.SetAppearance(s, .25f);
    Notelist.Add(m);
  }

  public void receiveMidiNote(int ID, bool on) {
    _midiComponentInterface.OutputNote(on, ID, false);
  }

  void CCupdate() {
    int y = Mathf.FloorToInt((CChandle.transform.localPosition.y + .045f) / -.04f);

    if (y > CClist.Count) {
      int dif = y - CClist.Count;
      for (int i = 0; i < dif; i++) {
        spawnCC();
      }
    } else if (y < CClist.Count) {
      int dif = CClist.Count - y;
      for (int i = 0; i < dif; i++) {
        int index = CClist.Count - 1 - i;
        Destroy(CClist[index].gameObject);
        CClist.RemoveAt(index);
      }
    }
  }

  void NoteUpdate() {
    int y = Mathf.FloorToInt((notehandle.transform.localPosition.y + .045f) / -.04f);
    if (y > Notelist.Count) {
      int dif = y - Notelist.Count;
      for (int i = 0; i < dif; i++) {
        spawnNote();
      }
    } else if (y < Notelist.Count) {
      int dif = Notelist.Count - y;
      for (int i = 0; i < dif; i++) {
        int index = Notelist.Count - 1 - i;
        Destroy(Notelist[index].gameObject);
        Notelist.RemoveAt(index);
      }
    }
  }

  void Update() {
    CCupdate();
    NoteUpdate();

    for (int i = 0; i < CClist.Count; i++) {
      if (CClist[i].ccMessageDesired) {
        int val = Mathf.RoundToInt(Mathf.Lerp(0, 127, (CClist[i].curValue + 1) / 2f));
        _midiComponentInterface.OutputCC(val, i);
        CClist[i].ccMessageDesired = false;
      }
    }
  }

  public override InstrumentData GetData() {
    MIDIoutData data = new MIDIoutData();
    data.deviceType = menuItem.deviceType.MIDIOUT;
    GetTransformData(data);

    data.connection = _midiComponentInterface.connectedDevice;
    data.CChandle = CChandle.transform.localPosition.y;
    data.notehandle = notehandle.transform.localPosition.y;

    List<int> jacks = new List<int>();
    for (int i = 0; i < CClist.Count; i++) {
      jacks.Add(CClist[i].input.transform.GetInstanceID());
    }
    data.CCjacks = jacks.ToArray();

    jacks.Clear();
    for (int i = 0; i < Notelist.Count; i++) {
      jacks.Add(Notelist[i].input.transform.GetInstanceID());
    }
    data.NOTEjacks = jacks.ToArray();

    return data;
  }

  public override void Load(InstrumentData d) {
    MIDIoutData data = d as MIDIoutData;
    base.Load(data);

    if (data.connection != "") _midiComponentInterface.ConnectByName(data.connection);

    Vector3 pos;
    pos = CChandle.transform.localPosition;
    pos.y = data.CChandle;
    CChandle.transform.localPosition = pos;

    pos = notehandle.transform.localPosition;
    pos.y = data.notehandle;
    notehandle.transform.localPosition = pos;

    CCupdate();
    NoteUpdate();

    for (int i = 0; i < CClist.Count; i++) CClist[i].input.ID = data.CCjacks[i];
    for (int i = 0; i < Notelist.Count; i++) Notelist[i].input.ID = data.NOTEjacks[i];
  }
}

public class MIDIoutData : InstrumentData {
  public string connection;
  public float CChandle;
  public float notehandle;
  public int[] CCjacks;
  public int[] NOTEjacks;
}