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

public class xylorollDeviceInterface : deviceInterface {
  public GameObject keyPrefab;
  public Material blackmat;

  public midiOutOfRange midiLow, midiHigh;
  public midiComponentInterface _midiIn, _midiOut;

  public GameObject oscBank, sampleBank, arpBank;
  public sliderNotched arpSpeedSlider, arpPatternSlider, octaveSlider;
  public Transform adsrTransform;

  public button arpEnableButton, seqEnableButton, midiInButton, midiOutButton;
  public omniJack output;

  basicSwitch inputSwitch;
  xylorollSignalGenerator signal;
  adsrInterface _adsrInterface;

  public timelineComponentInterface _timeline;

  public int keyCount = 24;
  int voiceCount = 5;

  int arpPattern = 1;
  int arpSpeed = 5;
  int octaveValue = 2;

  List<int> voiceIndex;
  drumstick[] sticks;
  public key[] keys;

  List<int> selectedKeys = new List<int>();

  bool inputVal = true;

  beatTracker _beatManager;

  keyState[] keyStates = new keyState[24];

  public override void Awake() {
    base.Awake();
    _beatManager = ScriptableObject.CreateInstance<beatTracker>();
    sticks = GetComponentsInChildren<drumstick>();
    signal = GetComponent<xylorollSignalGenerator>();
    _adsrInterface = GetComponentInChildren<adsrInterface>();
    inputSwitch = GetComponentInChildren<basicSwitch>();
    signal.spawnVoices(voiceCount, _adsrInterface.volumes, _adsrInterface.durations);

    SpawnKeys();
    oscBank.SetActive(inputVal);
    sampleBank.SetActive(!inputVal);

    _timeline.setStartTracks(24);

    for (int i = 0; i < 24; i++) keyStates[i] = new keyState(false);
  }

  void Start() {
    _beatManager.setTriggers(arpStep, resetArp);
    _beatManager.updateBeat(arpSpeed + 2);
  }

  void SpawnKeys() {
    keys = new key[keyCount];
    float separation = .042f;
    int whiteCount = 0;
    for (int i = 0; i < keyCount; i++) {
      GameObject g = Instantiate(keyPrefab, transform, false) as GameObject;
      float s = Mathf.Lerp(1, .7f, (float)i / keyCount);

      keys[i] = g.GetComponent<key>();

      if (i % 12 == 1 || i % 12 == 3 || i % 12 == 6 || i % 12 == 8 || i % 12 == 10) {
        keys[i].setOffMat(blackmat);
        keys[i].keyValue = i;
        g.transform.localPosition = Vector3.right * (-separation * whiteCount + separation / 2) + Vector3.up * (.03f) + Vector3.forward * (-.09f);
      } else {
        g.transform.localPosition = Vector3.right * (-separation * whiteCount);
        keys[i].keyValue = i;
        whiteCount++;
      }

      g.transform.localScale = new Vector3(1.1f, 1, s);
    }
  }

  void Update() {
    if (inputVal != inputSwitch.switchVal) {
      inputVal = inputSwitch.switchVal;
      signal.oscInput = inputVal;
      oscBank.SetActive(inputVal);
      sampleBank.SetActive(!inputVal);
    }

    if (arpPatternSlider.switchVal != arpPattern) updateArpPattern(arpPatternSlider.switchVal);
    if (arpSpeedSlider.switchVal != arpSpeed) updateArpSpeed(arpSpeedSlider.switchVal);
    if (octaveSlider.switchVal != octaveValue) {
      octaveValue = octaveSlider.switchVal;
      signal.updateOctave(octaveValue - 2);
    }

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

  bool midiLowDesired = false;
  bool midiHighDesired = false;
  public override void OnMidiNote(int channel, bool on, int pitch) {
    int ID = pitch - 48;
    if (ID < 0) {
      if (on) midiLowDesired = true;

    } else if (ID > 23) {
      if (on) midiHighDesired = true;
    } else {
      asynchKeyHit(on, ID, keyInput.midi);
    }
  }

  public override void onTimelineEvent(int track, bool on) {
    asynchKeyHit(on, track, keyInput.seq);
  }


  public void asynchKeyHit(bool on, int ID, keyInput k) {
    if (k == keyInput.midi) keyStates[ID].midiState = on;
    else if (k == keyInput.seq) keyStates[ID].seqState = on;
    coreSignalHit(on, ID);
  }

  public bool isSelectedKey(int ID) {
    return selectedKeys.Contains(ID);
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

  public enum keyInput {
    seq,
    midi,
    touch
  }

  void coreSignalHit(bool on, int ID) {
    if (keyStates[ID].nonSeqStateChange()) {
      keyStates[ID].currentNonSeqState = keyStates[ID].getNonSeqState();
      _timeline.onTimelineEvent(ID, keyStates[ID].currentNonSeqState);
    }

    if (keyStates[ID].stateChange()) {
      on = keyStates[ID].currentState = keyStates[ID].getState();
      keys[ID].phantomHit(on);
    } else return;

    if (on) {
      if (selectedKeys.Contains(ID)) selectedKeys.Remove(ID);
      selectedKeys.Insert(0, ID);
    } else selectedKeys.Remove(ID);

    if (!on) {
      signal.updateVoices(ID, on);
      _midiOut.OutputNote(on, ID);
      if (arpPattern != 0) {
        if (selectedKeys.Count == 1) setKeyActive(selectedKeys[0], true);
      }

    } else if (arpPattern == 0 || selectedKeys.Count < 2) setKeyActive(ID, on);
  }

  void toggleMIDIin(bool on) {
    _midiIn.gameObject.SetActive(on);
  }

  void toggleMIDIout(bool on) {
    _midiOut.gameObject.SetActive(on);
  }

  public override void hit(bool on, int ID = -1) {
    if (ID >= 0) {
      keyStates[ID].touchState = on;
      coreSignalHit(on, ID);
    } else {
      if (ID == -1) enableSequencer(on);
      else if (ID == -2) enableArp(on);
      else if (ID == -3) toggleMIDIin(on);
      else if (ID == -4) toggleMIDIout(on);
    }
  }

  Vector3[] adsrPos = new Vector3[]
  {
        new Vector3(.213f,.211f,-.1f),
        new Vector3(-.155f,.159f,-.172f)
  };

  Quaternion[] adsrRot = new Quaternion[]
  {
       Quaternion.Euler(0,148,0),
        Quaternion.Euler(0,180,0)
  };

  void enableSequencer(bool on) {
    _timeline.gameObject.SetActive(on);
  }

  void enableArp(bool on) {
    arpBank.SetActive(on);
  }

  public void updateArpSpeed(int n) {
    if (arpSpeed == n) return;
    arpSpeed = n;
    _beatManager.updateBeat(arpSpeed + 2);
  }

  public void updateArpPattern(int n) {
    if (arpPattern == n) return;
    arpPattern = n;

    if (arpPattern == 0) {
      for (int i = 0; i < selectedKeys.Count; i++) setKeyActive(selectedKeys[i], true);
    }
  }

  int arpMod = 1;
  int updateArpStep(int cur) {
    switch (arpPattern) {
      case 1:
        cur = (cur + 1) % selectedKeys.Count;
        break;
      case 2:
        if (cur <= 0) {
          arpMod = 1;
          cur = 1;
        } else if (cur >= selectedKeys.Count - 1) {
          arpMod = -1;
          cur = Mathf.Clamp(selectedKeys.Count - 2, 0, selectedKeys.Count);
        } else cur += arpMod;
        break;
      case 3:
      case 4:
        cur--;
        if (cur < 0) cur = selectedKeys.Count - 1;
        else if (cur > selectedKeys.Count - 1) cur = Mathf.Clamp(selectedKeys.Count - 2, 0, selectedKeys.Count);
        break;
      default:
        break;
    }
    return cur;
  }

  void setKeyActive(int ID, bool on) {
    signal.updateVoices(ID, on);
    _midiOut.OutputNote(on, ID);
    keys[ID].setSelectAsynch(on);
  }

  public void resetArp() {
    arpCur = 0;
    arpPlayingKey = -1;
  }

  int arpCur = 0;
  int arpPlayingKey = -1;
  public void arpStep() {
    int[] sortedKeys = selectedKeys.ToArray();
    if (arpPattern != 4) System.Array.Sort(sortedKeys);

    if (arpPattern != 0 && selectedKeys.Count > 1) {
      arpCur = updateArpStep(arpCur);
      if (sortedKeys[arpCur] == arpPlayingKey) arpCur = updateArpStep(arpCur);

      arpPlayingKey = sortedKeys[arpCur];
      for (int i = 0; i < sortedKeys.Length; i++) {
        if (isSelectedKey(sortedKeys[i])) setKeyActive(sortedKeys[i], i == arpCur);
      }
    }
  }

  void OnDestroy() {
    Destroy(_beatManager);
    for (int i = 0; i < sticks.Length; i++) Destroy(sticks[i].gameObject);
    StopAllCoroutines();
  }

  public override InstrumentData GetData() {
    XyloRollData data = new XyloRollData();
    data.deviceType = menuItem.deviceType.XyloRoll;
    GetTransformData(data);

    data.ADSRdata = new Vector2[3];
    for (int i = 0; i < 3; i++) {
      data.ADSRdata[i] = _adsrInterface.xyHandles[i].percent;
    }
    data.octaveSetting = octaveSlider.switchVal;

    data.seqon = seqEnableButton.isHit;
    data.arpon = arpEnableButton.isHit;

    data.arpSetting = arpPattern;
    data.arpSpeed = arpSpeed;
    data.inputSetting = inputVal ? 1 : 0;

    data.inputSample = new string[2];

    samplerLoad tempSampLoad = sampleBank.GetComponent<samplerLoad>();
    if (tempSampLoad.queuedSample[0] != "") {
      data.inputSample[0] = tempSampLoad.queuedSample[0];
      data.inputSample[1] = tempSampLoad.queuedSample[1];
    } else tempSampLoad.getTapeInfo(out data.inputSample[0], out data.inputSample[1]);

    oscillatorBankComponentInterface _oscInterface = oscBank.GetComponent<oscillatorBankComponentInterface>();
    data.oscAamp = _oscInterface.ampPercent[0];
    data.oscAfreq = _oscInterface.freqPercent[0];
    data.oscAwave = _oscInterface.wavePercent[0];

    data.oscBamp = _oscInterface.ampPercent[1];
    data.oscBfreq = _oscInterface.freqPercent[1];
    data.oscBwave = _oscInterface.wavePercent[1];

    data.jackOutID = output.transform.GetInstanceID();

    data.midiInConnection = _midiIn.connectedDevice;
    data.midiOutConnection = _midiOut.connectedDevice;

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
    XyloRollData data = d as XyloRollData;
    base.Load(data);

    for (int i = 0; i < 3; i++) _adsrInterface.xyHandles[i].setPercent(data.ADSRdata[i]);
    _adsrInterface.setDefaults = false;

    octaveSlider.setVal(data.octaveSetting);

    seqEnableButton.startToggled = data.seqon;
    arpEnableButton.startToggled = data.arpon;

    arpPatternSlider.switchVal = data.arpSetting;
    arpSpeedSlider.switchVal = data.arpSpeed;

    inputSwitch.setSwitch(data.inputSetting == 1);

    sampleBank.GetComponent<samplerLoad>().QueueSample(data.inputSample[0], data.inputSample[1]);

    oscBank.GetComponent<oscillatorBankComponentInterface>().setValues(data.oscAamp, data.oscAfreq, data.oscAwave, data.oscBamp, data.oscBfreq, data.oscBwave);

    if (data.midiInConnection != null && data.midiInConnection != "") {
      midiInButton.startToggled = true;
      _midiIn.ConnectByName(data.midiInConnection);
    }
    if (data.midiOutConnection != null && data.midiOutConnection != "") {
      midiOutButton.startToggled = true;
      _midiOut.ConnectByName(data.midiOutConnection);
    }

    output.ID = data.jackOutID;

    if (data.timelinePresent) {
      _timeline.SetTimelineData(data.timelineData);

      Vector3 pos = _timeline.heightHandle.transform.localPosition;
      pos.y = data.timelineHeight;
      _timeline.heightHandle.transform.localPosition = pos;
      _timeline.setStartHeight(data.timelineHeight);

      for (int i = 0; i < data.timelineEvents.Length; i++) {
        _timeline.SpawnTimelineEvent(data.timelineEvents[i].track, data.timelineEvents[i].in_out);
      }
    } else {
      if (data.seqInID != 0) _timeline.playInput.ID = data.seqInID;
    }

  }

}

public class XyloRollData : InstrumentData {
  public Vector2[] ADSRdata;
  public int octaveSetting;

  public int seqInID;

  public bool seqon; // seq showing?
  public bool arpon; // arp showing?

  // arp
  public int arpSetting;
  public int arpSpeed;

  // sampler
  public int inputSetting;
  public string[] inputSample = new string[] { "", "" };

  // osc
  public float oscAfreq, oscAamp, oscAwave;
  public float oscBfreq, oscBamp, oscBwave;

  //jack out
  public int jackOutID;

  public string midiInConnection;
  public string midiOutConnection;

  public bool timelinePresent;
  public TimelineComponentData timelineData;
  public timelineEvent.eventData[] timelineEvents;
  public float timelineHeight;
}