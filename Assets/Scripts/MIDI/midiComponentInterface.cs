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

public class midiComponentInterface : componentInterface {
  deviceInterface _deviceInterface;

  public bool input = true;
  public string connectedDevice = "";
  int channel = 0;

  public GameObject midiPanelPrefab, statusLight;
  public TextMesh statusText;
  public sliderNotched channelSlider;

  midiPanel mainMidiPanel;
  List<midiPanel> midiPanelList = new List<midiPanel>();

  Material statusLightMat;
  Color connectedColor = new Color32(0x32, 0xA3, 0x23, 0xFF); //A32323FF
  Color disconnectedColor = new Color32(0xA3, 0x23, 0x23, 0xFF); //A32323FF
  Color connectingColor = new Color32(0x9A, 0xA3, 0x23, 0xFF); //9AA323FF

  MIDIdevice curMIDIdevice;

  void Awake() {
    _deviceInterface = GetComponentInParent<deviceInterface>();

    statusLightMat = statusLight.GetComponent<Renderer>().material;
    statusLightMat.SetFloat("_EmissionGain", .3f);
    statusLightMat.SetColor("_TintColor", connectingColor);

    createMainMidiPanel();
  }

  void createMainMidiPanel() {
    mainMidiPanel = (Instantiate(midiPanelPrefab, transform, false) as GameObject).GetComponent<midiPanel>();
    mainMidiPanel.transform.localPosition = new Vector3(0, 0, .015f);
    mainMidiPanel.transform.localRotation = Quaternion.identity;
    mainMidiPanel.transform.localScale = new Vector3(0.15f, .02f, 1.25f);
    mainMidiPanel.label.text = input ? "[SELECT MIDI INPUT DEVICE]" : "[SELECT MIDI OUTPUT DEVICE]";
    mainMidiPanel.buttonID = -1;
  }

  public void ConnectByName(string s) {
    MIDImaster.instance.RefreshDevices(input);
    int ID = MIDImaster.instance.GetIDbyName(input, s);
    if (ID == -1) {
      Debug.Log("FAILED ID BY NAME");
    } else {
      mainMidiPanel.label.text = input ? MIDImaster.instance.inputDevices[ID].name : MIDImaster.instance.outputDevices[ID].name;
      listopen = false;
      TryConnect(ID, s);
      CloseList();
    }
  }

  void OnDestroy() {
    if (curMIDIdevice != null) MIDImaster.instance.Disconnect(curMIDIdevice, input, this);
  }

  Coroutine _textKillRoutine;
  IEnumerator TextKillRoutine() {
    yield return new WaitForSeconds(3);
    statusText.text = "";
    statusText.gameObject.SetActive(false);
  }

  void TryConnect(int index, string name) {

    string s = "Connecting";
    MIDIdevice lastMidiDevice = curMIDIdevice;
    curMIDIdevice = MIDImaster.instance.Connect(this, index, input);
    statusText.gameObject.SetActive(true);
    if (lastMidiDevice != curMIDIdevice && lastMidiDevice != null) {
      MIDImaster.instance.Disconnect(lastMidiDevice, input, this);
    }

    if (curMIDIdevice != null) {
      connectedDevice = name;
      s = "CONNECTED!";
      statusLightMat.SetColor("_TintColor", connectedColor);
      statusText.text = s;

    } else {
      connectedDevice = "";
      s = "Connection failed\nDevice may be connected to other software.";
      statusLightMat.SetColor("_TintColor", disconnectedColor);
      statusText.text = s;
    }

    if (gameObject.activeSelf) {
      if (_textKillRoutine != null) StopCoroutine(_textKillRoutine);
      _textKillRoutine = StartCoroutine(TextKillRoutine());
    }
  }

  void OnDisable() {
    StopAllCoroutines();
    statusText.gameObject.SetActive(false);
  }

  void toggleList() {
    listopen = !listopen;
    if (listopen) OpenList();
    else CloseList();
  }

  void Update() {
    if (channelSlider != null) channel = channelSlider.switchVal;
  }

  bool listopen = false;
  public override void hit(bool on, int ID = -1) {
    if (!on) return;
    if (ID == -1) toggleList();

    else {
      mainMidiPanel.label.text = input ? MIDImaster.instance.inputDevices[ID].name : MIDImaster.instance.outputDevices[ID].name;

      listopen = false;
      TryConnect(ID, mainMidiPanel.label.text);
      CloseList();
    }
  }

  void CloseList() {
    for (int i = 0; i < midiPanelList.Count; i++) {
      Destroy(midiPanelList[i].gameObject);
    }

    midiPanelList.Clear();
  }

  void OpenList() {
    MIDImaster.instance.RefreshDevices(input);

    int count = input ? MIDImaster.instance.inputDevices.Count : MIDImaster.instance.outputDevices.Count;

    for (int i = 0; i < count; i++) {
      midiPanel m = (Instantiate(midiPanelPrefab, transform, false) as GameObject).GetComponent<midiPanel>();
      m.transform.localPosition = new Vector3(0, i * .03f + .045f, .015f);
      m.transform.localRotation = Quaternion.identity;
      m.transform.localScale = new Vector3(0.15f, .02f, 1.25f);
      m.label.text = input ? MIDImaster.instance.inputDevices[i].name : MIDImaster.instance.outputDevices[i].name;

      m.buttonID = i;
      midiPanelList.Add(m);
    }

    if (count == 0) {
      statusText.gameObject.SetActive(true);
      statusText.text = "NO MIDI DEVICES FOUND";
      if (gameObject.activeSelf) {
        if (_textKillRoutine != null) StopCoroutine(_textKillRoutine);
        _textKillRoutine = StartCoroutine(TextKillRoutine());
      }
      listopen = false;
      CloseList();
    }
  }

  public void InputNoteOn(Midi.NoteOnMessage msg) {
    if (channel == 0 || channel == (int)msg.Channel + 1) {
      _deviceInterface.OnMidiNote((int)msg.Channel + 1, msg.Velocity != 0, (int)msg.Pitch);
    }
  }

  public void InputNoteOff(Midi.NoteOffMessage msg) {
    if (channel == 0 || channel == (int)msg.Channel + 1) {
      _deviceInterface.OnMidiNote((int)msg.Channel + 1, false, (int)msg.Pitch);
    }

  }

  public void InputControlChange(Midi.ControlChangeMessage msg) {
    if (channel == 0 || channel == (int)msg.Channel + 1) {
      _deviceInterface.OnMidiCC((int)msg.Channel + 1, (int)msg.Control, msg.Value);
    }
  }

  public void OutputNote(bool on, int ID, bool add48 = true) {
    if (input || curMIDIdevice == null) return;

    if (add48) ID += 48;
    MIDImaster.instance.outputNote(curMIDIdevice, on, ID, channel);
  }

  public void OutputCC(int val, int ID) {
    if (input || curMIDIdevice == null) return;
    MIDImaster.instance.outputCC(curMIDIdevice, val, ID, channel);
  }
}


