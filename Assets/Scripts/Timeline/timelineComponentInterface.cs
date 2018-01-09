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
using System.Linq;
using UnityEngine;
using System.Runtime.InteropServices;

public class timelineComponentInterface : componentInterface {

  [DllImport("SoundStageNative")]
  public static extern int CountPulses(float[] buffer, int length, int channels, float[] lastSig);

  public deviceInterface _targetDeviceInterface;
  public GameObject tlEventPrefab;
  public Transform gridPlane;
  public xHandle gridHandle, tailMarkerHandle, headMarkerHandle;
  public yHandle heightHandle;
  public Transform tailMarkerLine, headMarkerLine;

  timelineDeviceInterface _timelineDeviceInterface;

  timelineGridRender _timelineGridRender;
  public timelinePlayer _timelinePlayer;
  timelineGridUI _timelineGridUI;
  timelineScrollInterface _scrollInterface;
  public sliderNotched unitSlider;
  public basicSwitch overdubSwitch, snapSwitch, loopSwitch, notelockSwitch;
  float offsetMult = 2;
  float gridHeight = .3f;

  public List<timelineEvent> _tlEvents = new List<timelineEvent>();
  public gridParams _gridParams;

  public button playButton, recButton;

  public omniJack playInput, recInput;
  signalGenerator playSignal, recSignal;
  float[] lastPlaySig, lastRecSig;

  public Transform[] markers;

  public bool playing = false;
  public bool recording = false;
  public bool overdub = false;
  public bool snapping = false;
  public bool notelock = false;

  int startTracks = 25;
  int startunitres = 3;
  float startwidth = .4f;
  float startheight = .3f;
  Vector2 startrange = new Vector2(0, 8);
  Vector2 startio = new Vector2(0, 8);

  public void killMultiselect() {
    _timelineGridUI.killMultiselect();
  }

  public void Awake() {

    _timelineDeviceInterface = GetComponent<timelineDeviceInterface>();
    Init();
  }

  public void setStartHeight(float h) {
    startheight = h;
  }

  public void setStartTracks(int n) {
    startTracks = n;
  }

  public void SetStartVariables(int _tracks, int _unitResolution, float _width, float _height, Vector2 _range, Vector2 _io) {
    startTracks = _tracks;
    startunitres = _unitResolution;
    startwidth = _width;
    startheight = _height;
    startrange = _range;
    startio = _io;
  }

  void Start() {
    snapSwitch.setSwitch(snapping);
    notelockSwitch.setSwitch(notelock);
    Setup(startTracks, startunitres, startwidth, startheight, startrange, startio);
  }

  public void Init() {
    _timelineGridRender = GetComponentInChildren<timelineGridRender>();
    _timelinePlayer = GetComponent<timelinePlayer>();
    _timelineGridUI = GetComponentInChildren<timelineGridUI>();
    _scrollInterface = GetComponentInChildren<timelineScrollInterface>();

    _timelineGridRender.Init();

    lastPlaySig = new float[] { 0, 0 };
    lastRecSig = new float[] { 0, 0 };
  }

  public void Setup(int _tracks, int _unitResolution, float _width, float _height, Vector2 _range, Vector2 _io) {
    _gridParams = new gridParams(_tracks, _unitResolution, _width, _height, _range, _io);
    setGridHandles();
    updateHeight();
    updateGrid(_range, _width);
    _scrollInterface.Activate();
  }

  void updateHeight() {
    gridHandle.transform.localScale = new Vector3(
       .005f,
        _gridParams.trackHeight * _gridParams.tracks + .01f,
        .02f);

    Vector3 pos;

    pos = gridHandle.transform.localPosition;
    gridHandle.transform.localPosition = new Vector3(
        pos.x,
        _gridParams.trackHeight / 2f * _gridParams.tracks,
        0);


    pos = _timelinePlayer.playheadHandle.transform.localPosition;
    pos.y = _gridParams.trackHeight * _gridParams.tracks + .015f;
    _timelinePlayer.playheadHandle.transform.localPosition = pos;

    Transform playheadLine = _timelinePlayer.playheadHandle.transform.GetChild(0);
    playheadLine.parent = transform;
    playheadLine.localScale = new Vector3(
       .001f,
        _gridParams.trackHeight * (_gridParams.tracks),
        .001f);
    pos.y = _gridParams.trackHeight / 2f * (_gridParams.tracks);
    playheadLine.localPosition = pos;
    playheadLine.parent = _timelinePlayer.playheadHandle.transform;

    pos = headMarkerHandle.transform.localPosition;
    pos.y = _gridParams.trackHeight * (_gridParams.tracks) + .015f;
    headMarkerHandle.transform.localPosition = pos;

    markers[0].position = headMarkerHandle.transform.position;
    markers[0].transform.parent = null;

    markers[1].localScale = new Vector3(
       .002f,
        _gridParams.trackHeight * (_gridParams.tracks) + .01f,
        .002f);
    pos.y = _gridParams.trackHeight / 2f * (_gridParams.tracks) + .005f;
    markers[1].localPosition = pos;

    markers[0].transform.parent = markers[1];

    pos = tailMarkerHandle.transform.localPosition;
    pos.y = _gridParams.trackHeight * (_gridParams.tracks) + .015f;
    tailMarkerHandle.transform.localPosition = pos;

    markers[2].position = tailMarkerHandle.transform.position;
    markers[2].transform.parent = null;


    markers[3].localScale = markers[1].localScale;
    pos.y = _gridParams.trackHeight / 2f * _gridParams.tracks + .005f;
    markers[3].localPosition = pos;

    markers[2].transform.parent = markers[3];

  }

  void setGridHandles() {
    Vector3 pos;

    if (heightHandle != null) {
      pos = heightHandle.transform.localPosition;
      pos.y = _gridParams.getGridHeight();
      heightHandle.transform.localPosition = pos;
    }

    pos = gridHandle.transform.localPosition;
    gridHandle.transform.localPosition = new Vector3(
        -_gridParams.width,
        _gridParams.trackHeight / 2f * _gridParams.tracks,
        0);
  }

  bool isOverlap(timelineEvent tlA, timelineEvent tlB) {
    if (tlA.track != tlB.track) return false;

    Vector2 a = tlA.in_out;
    Vector2 b = tlB.in_out;
    bool c1 = a.x <= b.x && a.y >= b.x;
    bool c2 = b.x <= a.x && b.y >= a.x;
    return c1 || c2;
  }

  bool isInside(Vector2 a, Vector2 b) {
    return a.x >= b.x && a.y <= b.y;
  }

  public void overlapCheck(timelineEvent e) {
    for (int i = _tlEvents.Count - 1; i >= 0; i--) {
      if (e != _tlEvents[i]) {
        if (isOverlap(_tlEvents[i], e)) {

          if (isInside(_tlEvents[i].in_out, e.in_out)) {
            if (_tlEvents[i].playing) tlEventTrigger(i, false);
            _tlEvents[i].pleaseDestroy = true;
            _tlEvents.RemoveAt(i);
          } else if (isInside(e.in_out, _tlEvents[i].in_out)) {
            Vector2 a = new Vector2(e.in_out.y + .01f, _tlEvents[i].in_out.y);
            _tlEvents[i].setOut(e.in_out.x - .01f);
            lock (_spawnLock) {
              toSpawn[_tlEvents[i].track] = a;
            }
          } else if (e.in_out.x < _tlEvents[i].in_out.x) {
            _tlEvents[i].setIn(e.in_out.y + .01f);
          } else {
            _tlEvents[i].setOut(e.in_out.x - .01f);
          }
        }
      }
    }
  }


  private void OnAudioFilterRead(float[] buffer, int channels) {
    if (playSignal == null && recSignal == null) return;
    double dspTime = AudioSettings.dspTime;

    if (playSignal != null) {
      playSignal.processBuffer(buffer, dspTime, channels);
      int hits = CountPulses(buffer, buffer.Length, channels, lastPlaySig);
      if (hits % 2 == 1) playFromStart();
    }

    if (recSignal != null) {
      recSignal.processBuffer(buffer, dspTime, channels);
      int hits = CountPulses(buffer, buffer.Length, channels, lastRecSig);
      if (hits % 2 == 1) toggleRec();
    }

  }

  void playFromStart() {
    _timelinePlayer.Back();
    if (!playing) setPlay(true, true);
  }

  void toggleRec() {
    setRecord(!recording, true);
  }

  void setRecord(bool on, bool immediate = false) {

    if (on & !playing) {
      playButton.phantomHit(true);
      setPlay(true, immediate);
    }
    recording = on;

    _timelinePlayer.setRecord(on);
    recButton.phantomHit(on);
  }

  public void setPlay(bool on, bool immediate = false) {
    playing = on;
    _timelinePlayer.setPlay(on, immediate);

    if (!on) {
      if (recording) {
        recButton.phantomHit(false);
        setRecord(false);
      }
      for (int i = 0; i < _tlEvents.Count; i++) {
        if (_tlEvents[i].playing) tlEventTrigger(i, false);
      }
    }
    playButton.phantomHit(on);
  }

  void clearTimeline() {
    for (int i = 0; i < _tlEvents.Count; i++) {
      if (_tlEvents[i].playing) tlEventTrigger(i, false);
      Destroy(_tlEvents[i].gameObject);
    }
    _tlEvents.Clear();
    _timelinePlayer.clearActiveEvents();
  }

  public override void hit(bool on, int ID = -1) {
    if (ID == 0) setRecord(on);
    if (ID == 1) setPlay(on);
    if (ID == 2 && on) _timelinePlayer.Back();
    if (ID == 3 && on) clearTimeline();
  }

  void updateHeadTail() {
    if (headMarkerHandle.curState != manipObject.manipState.grabbed) {
      Vector3 p1 = headMarkerLine.localPosition;
      p1.x = _gridParams.UnittoX(_gridParams.head_tail.x);
      headMarkerLine.localPosition = p1;

      p1 = headMarkerHandle.transform.localPosition;
      p1.x = headMarkerLine.localPosition.x;
      headMarkerHandle.transform.localPosition = p1;
    }

    if (tailMarkerHandle.curState != manipObject.manipState.grabbed) {
      Vector3 p1 = tailMarkerLine.localPosition;
      p1.x = _gridParams.UnittoX(_gridParams.head_tail.y);
      tailMarkerLine.localPosition = p1;

      p1 = tailMarkerHandle.transform.localPosition;
      p1.x = tailMarkerLine.localPosition.x;
      tailMarkerHandle.transform.localPosition = p1;
    }

    if (_gridParams.range.y < _gridParams.head_tail.y && tailMarkerHandle.curState != manipObject.manipState.grabbed) {
      tailMarkerLine.gameObject.SetActive(false);
      tailMarkerHandle.gameObject.SetActive(false);
    } else {
      tailMarkerLine.gameObject.SetActive(true);
      tailMarkerHandle.gameObject.SetActive(true);
    }

    if (_gridParams.range.x > _gridParams.head_tail.x && headMarkerHandle.curState != manipObject.manipState.grabbed) {
      headMarkerLine.gameObject.SetActive(false);
      headMarkerHandle.gameObject.SetActive(false);
    } else {
      headMarkerLine.gameObject.SetActive(true);
      headMarkerHandle.gameObject.SetActive(true);
    }
  }

  public void updateGrid(Vector2 range, float length) {
    _gridParams.updateDimensions(range, length);
    refreshGrid();
  }

  public void updateTrackCount(int _tracks) {
    _gridParams.updateTrackCount(_tracks);

    _gridParams.setTrackHeight(_tracks * _gridParams.trackHeight);
    updateHeight();
    refreshGrid();
  }

  public void refreshGrid() {
    _timelineGridRender.updateGrid(_gridParams);
    updateHeadTail();
    for (int i = 0; i < _tlEvents.Count; i++) {
      _tlEvents[i].gridUpdate();
    }

    _timelineGridUI.updateResolution();
  }

  Dictionary<int, Vector2> toSpawn = new Dictionary<int, Vector2>();
  private object _spawnLock = new object();
  void Update() {
    bool updated = false;

    lock (_spawnLock) {
      if (toSpawn.Keys.Count > 0) {
        foreach (KeyValuePair<int, Vector2> entry in toSpawn) SpawnTimelineEvent(entry.Key, entry.Value);
        toSpawn.Clear();
      }
    }

    if (heightHandle != null) {
      if (heightHandle.transform.localPosition.y != _gridParams.getGridHeight()) {
        _gridParams.setTrackHeight(heightHandle.transform.localPosition.y);
        updateHeight();
        refreshGrid();
      }
    }

    if (unitSlider.switchVal != _gridParams.snapResolution) {
      _gridParams.updateUnitResolution(unitSlider.switchVal);
      _timelineGridRender.updateGrid(_gridParams);
    }

    overdub = overdubSwitch.switchVal;
    notelock = notelockSwitch.switchVal;

    if (snapSwitch.switchVal != snapping) {
      snapping = snapSwitch.switchVal;
      for (int i = 0; i < _tlEvents.Count; i++) _tlEvents[i].updateSnap(snapping);
    }

    _timelinePlayer.looping = loopSwitch.switchVal;

    if (playInput.signal != playSignal) playSignal = playInput.signal;
    if (recInput.signal != recSignal) recSignal = recInput.signal;

    if (tailMarkerHandle.curState == manipObject.manipState.grabbed) {
      if (tailMarkerHandle.transform.localPosition.x < gridHandle.transform.localPosition.x) {
        Vector3 pos = gridHandle.transform.localPosition;
        pos.x = tailMarkerHandle.transform.localPosition.x;
        gridHandle.transform.localPosition = pos;

        if (gridHandle.curState == manipObject.manipState.grabbed) gridHandle.recalcOffset();
        updated = true;
      }

      _gridParams.head_tail.y = Mathf.RoundToInt(_gridParams.XtoUnit(tailMarkerHandle.transform.localPosition.x));
      Vector3 p1 = tailMarkerLine.localPosition;
      p1.x = _gridParams.UnittoX(_gridParams.head_tail.y);
      tailMarkerLine.localPosition = p1;

      headMarkerHandle.xBounds.x = p1.x + _gridParams.unitSize;
    } else if (tailMarkerHandle.transform.localPosition.x != tailMarkerLine.localPosition.x) {
      Vector3 p1 = tailMarkerHandle.transform.localPosition;
      p1.x = tailMarkerLine.localPosition.x;
      tailMarkerHandle.transform.localPosition = p1;
    }

    if (headMarkerHandle.curState == manipObject.manipState.grabbed) {
      _gridParams.head_tail.x = Mathf.RoundToInt(_gridParams.XtoUnit(headMarkerHandle.transform.localPosition.x));

      Vector3 p1 = headMarkerLine.localPosition;
      p1.x = _gridParams.UnittoX(_gridParams.head_tail.x);
      headMarkerLine.localPosition = p1;
      tailMarkerHandle.xBounds.y = p1.x - _gridParams.unitSize;
    } else if (headMarkerHandle.transform.localPosition.x != headMarkerLine.localPosition.x) {
      Vector3 p1 = headMarkerHandle.transform.localPosition;
      p1.x = headMarkerLine.localPosition.x;
      headMarkerHandle.transform.localPosition = p1;
    }

    if (gridHandle.curState == manipObject.manipState.grabbed) updated = true;

    if (updated) _scrollInterface.handleUpdate(gridHandle.transform.localPosition.x);

  }

  //helper function
  public Vector2 worldPosToGridPos(Vector3 pos, bool offset = false, bool yCap = true) {
    pos = transform.InverseTransformPoint(pos);
    pos.z = 0;
    pos.y = _gridParams.snapToTrack(pos.y, yCap);

    if (snapping) {
      pos.x = _gridParams.XtoSnap(pos.x, offset);
    }

    return pos;
  }

  //timeline functions
  public override void onTimelineEvent(int track, bool on) {
    if (recording) _timelinePlayer.RecordEvent(track, on);
  }

  public void tlEventTrigger(int index, bool play) {
    _tlEvents[index].playing = play;
    if (_targetDeviceInterface != null) _targetDeviceInterface.onTimelineEvent(_tlEvents[index].track, play);
  }

  public timelineEvent SpawnTimelineEvent(int t, Vector2 io) {
    _tlEvents.Add((Instantiate(tlEventPrefab, transform, false) as GameObject).GetComponent<timelineEvent>());
    _tlEvents.Last().init(t, io, this);
    return _tlEvents.Last();
  }

  public void clearEvents() {
    for (int i = 0; i < _tlEvents.Count; i++) {
      Destroy(_tlEvents[i].gameObject);
    }

    _tlEvents.Clear();
  }

  public TimelineComponentData GetTimelineData() {
    TimelineComponentData data = new TimelineComponentData();

    data.playing = playing;
    data.recording = recording;
    data.playTrigID = playInput.transform.GetInstanceID();
    data.recTrigID = recInput.transform.GetInstanceID();

    data.loop = loopSwitch.switchVal;
    data.snap = snapping;
    data.overdub = overdub;
    data.notelock = notelock;

    data.unitResolution = _gridParams.snapResolution;

    data.gridWidth = _gridParams.width;
    data.head_tail = _gridParams.head_tail;
    data.gridRange = _gridParams.range;

    return data;
  }

  public void SetTimelineData(TimelineComponentData data) {
    playButton.startToggled = data.playing;
    recButton.startToggled = data.recording;
    playInput.ID = data.playTrigID;
    recInput.ID = data.recTrigID;

    overdub = data.overdub;
    snapping = data.snap;
    notelock = data.notelock;
    _timelinePlayer.looping = data.loop;

    loopSwitch.setSwitch(data.loop);
    snapSwitch.setSwitch(data.snap);
    overdubSwitch.setSwitch(data.overdub);
    notelockSwitch.setSwitch(data.notelock);

    unitSlider.setVal(data.unitResolution);

    startunitres = data.unitResolution;
    startwidth = data.gridWidth;
    startrange = data.gridRange;
    startio = data.head_tail;
  }
}

public struct TimelineComponentData {
  public bool playing;
  public bool recording;
  public int playTrigID;
  public int recTrigID;

  public bool loop;
  public bool snap;
  public bool overdub;
  public bool notelock;

  public int unitResolution;

  public float gridWidth;
  public Vector2 head_tail;
  public Vector2 gridRange;
};


//track heights are hardcoded
public struct gridParams {
  public int snapResolution;
  public float snapFraction;

  public float unitDuration;
  public float unitSize;

  public Vector2 head_tail;

  public Vector2 range;

  public float tracks;
  public float trackHeight;
  public float width;

  public float getGridWidth() {
    return width;
  }

  public void setTrackHeight(float _height) {
    trackHeight = _height / (float)tracks;
  }

  public float getGridHeight() {
    return trackHeight * tracks;
  }

  public void updateTrackCount(int _tracks) {
    tracks = _tracks;
  }

  public void updateDimensions(Vector2 _range, float _width) {
    range = _range;
    width = _width;
    unitSize = width / (range.y - range.x);
  }

  public bool isEventVisible(int track, Vector2 eventRange) {
    bool a = track < tracks && track >= 0;
    bool b = eventRange.x < range.y && eventRange.y > range.x;
    return a && b;
  }

  float[] snapResolutions;
  public void updateUnitResolution(int n) {
    snapResolution = n;
    snapFraction = snapResolutions[n];
  }

  public gridParams(int _tracks, int _unitResolution, float _width, float _height, Vector2 _range, Vector2 _headtail) {
    tracks = _tracks;
    trackHeight = _height / (float)_tracks;

    snapResolution = _unitResolution;
    unitDuration = 1 / 8f;
    snapFraction = 1 / 8f;
    width = _width;

    range = _range;
    head_tail = _headtail;

    unitSize = width / (range.y - range.x);
    snapResolutions = new float[] { 1, 2, 3, 4, 6, 8, 16 };
    updateUnitResolution(_unitResolution);
  }

  public float YtoUnit(float y) {
    return y / trackHeight;
  }

  public float XtoUnit(float x) {
    return Mathf.Lerp(range.x, range.y, -x / getGridWidth());
  }

  public float UnittoX(float x) {
    return Mathf.InverseLerp(range.x, range.y, x) * -getGridWidth();
  }

  public float UnittoY(float y) {
    return Mathf.InverseLerp(0, tracks, y) * getGridHeight();
  }

  public bool isOnGrid(float x) {
    return x >= range.x && x <= range.y;
  }

  public float UnittoSnap(float x, bool floor = true) {
    if (floor) x = (Mathf.Round(x * snapFraction)) / snapFraction;
    else x = (Mathf.Ceil(x * snapFraction)) / snapFraction;
    return x;
  }

  public float XtoSnap(float x, bool offset) {
    if (offset) x = (Mathf.Floor(XtoUnit(x) * snapFraction) + .5f) / snapFraction;
    else x = (Mathf.Round(XtoUnit(x) * snapFraction)) / snapFraction;
    return UnittoX(x);
  }

  public float snapToTrack(float y, bool capped = true) {
    y = Mathf.FloorToInt(YtoUnit(y));
    if (capped) {
      if (y < 0) y = 0;
      if (y > tracks - 1) y = tracks - 1;
    }

    return (y + .5f) * trackHeight;
  }
};

