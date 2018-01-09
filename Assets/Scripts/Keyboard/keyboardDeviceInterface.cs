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
using System.Collections.Generic;

public class keyboardDeviceInterface : deviceInterface {
  public timelineComponentInterface _timeline;
  public midiComponentInterface _midiIn, _midiOut;
  public GameObject whiteKeyPrefab, blackKeyPrefab;
  public omniJack output, freqoutput, ampoutput;
  public button muteButton, midiInButton, midiOutButton, sequenceButton;

  public midiOutOfRange midiLow, midiHigh;

  public speaker _speaker;
  int keyCount = 25;
  key[] keys;

  adsrInterface _adsrInterface;
  basicSwitch octaveToggle;

  keyFrequencySignalGenerator freqSignal;
  adsrSignalGenerator adsrSignal;
  int curKey;

  keyState[] keyStates = new keyState[25];

  public override void Awake() {
    base.Awake();

    curKey = -1;

    _adsrInterface = GetComponentInChildren<adsrInterface>();
    octaveToggle = GetComponentInChildren<basicSwitch>();

    freqSignal = GetComponent<keyFrequencySignalGenerator>();
    adsrSignal = GetComponent<adsrSignalGenerator>();

    freqoutput.homesignal = freqSignal;
    ampoutput.homesignal = adsrSignal;

    keys = new key[keyCount];
    adsrSignal.durations = _adsrInterface.durations;
    adsrSignal.volumes = _adsrInterface.volumes;
    SpawnKeys();

    for (int i = 0; i < 25; i++) keyStates[i] = new keyState(false);
  }

  void SpawnKeys() {
    float separation = .05f;
    int whiteCount = 0;
    for (int i = 0; i < keyCount; i++) {
      GameObject g;
      if (i % 12 == 1 || i % 12 == 3 || i % 12 == 6 || i % 12 == 8 || i % 12 == 10) {
        g = Instantiate(blackKeyPrefab, transform, false) as GameObject;
        g.transform.localPosition = new Vector3(-separation * whiteCount + separation / 2 + .15f, .03f, -.025f);
      } else {
        g = Instantiate(whiteKeyPrefab, transform, false) as GameObject;
        g.transform.localPosition = new Vector3(-separation * whiteCount + .15f, -.007f, .005f);
        whiteCount++;
      }
      keys[i] = g.GetComponent<key>();
      keys[i].keyValue = i;
      keys[i].isKeyboard = true;
      keys[i].sticky = false;
    }
  }

  bool muted = false;
  public void toggleMute(bool on) {
    muted = on;
    _speaker.volume = muted ? 0 : 1;
  }

  public override void onTimelineEvent(int track, bool on) {
    asynchKeyHit(on, track, keyInput.seq);
  }

  bool midiLowDesired = false;
  bool midiHighDesired = false;

  public override void OnMidiNote(int channel, bool on, int pitch) {
    int ID = pitch - 48;
    if (ID < 0) {
      if (on) midiLowDesired = true;
    } else if (ID > 24) {
      if (on) midiHighDesired = true;
    } else {
      asynchKeyHit(on, ID, keyInput.midi);
    }
  }

  public void asynchKeyHit(bool on, int ID, keyInput k) {
    if (k == keyInput.midi) keyStates[ID].midiState = on;
    else if (k == keyInput.seq) keyStates[ID].seqState = on;
    else if (k == keyInput.touch) keyStates[ID].touchState = on;

    if (keyStates[ID].nonSeqStateChange()) {
      keyStates[ID].currentNonSeqState = keyStates[ID].getNonSeqState();
      _timeline.onTimelineEvent(ID, keyStates[ID].currentNonSeqState);
    }

    if (keyStates[ID].stateChange()) {
      on = keyStates[ID].currentState = keyStates[ID].getState();
      keys[ID].phantomHit(on);
      keyHitEvent(on, ID);
    }
  }

  void keyHitEvent(bool on, int ID) {
    if (on) {
      if (curKey != ID) {
        int prev = curKey;
        curKey = ID;

        if (prev != -1) {
          adsrSignal.hit(false);
          if (_midiOut != null) _midiOut.OutputNote(false, prev);
        }

        if (_midiOut != null) _midiOut.OutputNote(on, ID);
        freqSignal.UpdateKey(curKey);
        adsrSignal.hit(true);
      }
    } else {
      if (curKey == ID) {
        _midiOut.OutputNote(false, ID);
        adsrSignal.hit(false);
        curKey = -1;
      }
    }
  }

  void toggleMIDIin(bool on) {
    _midiIn.gameObject.SetActive(on);
  }

  void toggleMIDIout(bool on) {
    _midiOut.gameObject.SetActive(on);
  }

  void toggleSequencer(bool on) {
    _timeline.gameObject.SetActive(on);
  }

  public override void hit(bool on, int ID = -1) {
    if (ID == -1) {
      toggleMute(on);
    } else if (ID == -2) {
      toggleMIDIin(on);
    } else if (ID == -3) {
      toggleMIDIout(on);
    } else if (ID == -4) {
      toggleSequencer(on);
    } else {
      asynchKeyHit(on, ID, keyInput.touch);
    }
  }

  void Update() {
    if (octaveToggle.switchVal) freqSignal.octave = 1;
    else freqSignal.octave = 0;

    if (midiLowDesired) {
      midiLowDesired = false;
      midiLow.gameObject.SetActive(true);
      midiLow.Activate();
    }

    if (midiHighDesired) {
      midiHighDesired = false;
      midiHigh.gameObject.SetActive(true);
      midiHigh.Activate();
    }
  }

  public override InstrumentData GetData() {
    KeyboardData data = new KeyboardData();
    data.deviceType = menuItem.deviceType.Keyboard;
    GetTransformData(data);
    data.muted = muted;
    data.octaveSwitch = octaveToggle.switchVal;
    data.ADSRdata = new Vector2[3];
    for (int i = 0; i < 3; i++) {
      data.ADSRdata[i] = _adsrInterface.xyHandles[i].percent;
    }

    data.jackOutID = output.transform.GetInstanceID();
    data.freqOutID = freqoutput.transform.GetInstanceID();
    data.ampOutID = ampoutput.transform.GetInstanceID();

    data.midiInConnection = _midiIn.connectedDevice;
    data.midiOutConnection = _midiOut.connectedDevice;
    data.sequencerEnabled = _timeline.gameObject.activeSelf;

    data.timelinePresent = true;
    data.timelineData = _timeline.GetTimelineData();
    data.timelineHeight = _timeline.heightHandle.transform.localPosition.y;
    List<timelineEvent.eventData> tempevents = new List<timelineEvent.eventData>();
    for (int i = 0; i < _timeline._tlEvents.Count; i++) {
      if (_timeline._tlEvents[i] != null) tempevents.Add(_timeline._tlEvents[i].getEventInfo());
    }
    data.timelineEvents = tempevents.ToArray();

    return data;
  }

  public override void Load(InstrumentData d) {
    KeyboardData data = d as KeyboardData;
    base.Load(data);

    output.ID = data.jackOutID;
    freqoutput.ID = data.freqOutID;
    ampoutput.ID = data.ampOutID;

    for (int i = 0; i < 3; i++) _adsrInterface.xyHandles[i].setPercent(data.ADSRdata[i]);
    _adsrInterface.setDefaults = false;

    muteButton.startToggled = data.muted;
    octaveToggle.setSwitch(data.octaveSwitch);

    if (data.midiInConnection != null & data.midiInConnection != "") {
      midiInButton.startToggled = true;
      _midiIn.ConnectByName(data.midiInConnection);
    }
    if (data.midiOutConnection != null & data.midiOutConnection != "") {
      midiOutButton.startToggled = true;
      _midiOut.ConnectByName(data.midiOutConnection);
    }

    sequenceButton.startToggled = data.sequencerEnabled;

    if (data.timelinePresent) {
      _timeline.SetTimelineData(data.timelineData);

      Vector3 pos = _timeline.heightHandle.transform.localPosition;
      pos.y = data.timelineHeight;
      _timeline.heightHandle.transform.localPosition = pos;
      _timeline.setStartHeight(data.timelineHeight);

      for (int i = 0; i < data.timelineEvents.Length; i++) {
        _timeline.SpawnTimelineEvent(data.timelineEvents[i].track, data.timelineEvents[i].in_out);
      }
    }
  }

  public enum keyInput {
    seq,
    midi,
    touch
  }

  struct keyState {
    public bool seqState;
    public bool midiState;
    public bool touchState;

    public bool currentState;
    public bool currentNonSeqState;

    public keyState(bool on) {
      currentNonSeqState = currentState = seqState = midiState = touchState = on;
    }

    public bool getState() {
      return seqState || midiState || touchState;
    }

    public bool getNonSeqState() {
      return midiState || touchState;
    }

    public bool stateChange() {
      return getState() != currentState;
    }

    public bool nonSeqStateChange() {
      return getNonSeqState() != currentNonSeqState;
    }
  };
}


public class KeyboardData : InstrumentData {
  public Vector2[] ADSRdata;
  public bool muted;
  public bool octaveSwitch;
  public bool sequencerEnabled;
  public int jackOutID;
  public int freqOutID;
  public int ampOutID;
  public string midiInConnection;
  public string midiOutConnection;

  public bool timelinePresent;
  public TimelineComponentData timelineData;
  public timelineEvent.eventData[] timelineEvents;
  public float timelineHeight;
}