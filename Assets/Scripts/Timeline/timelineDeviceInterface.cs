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

public class timelineDeviceInterface : deviceInterface {
  public GameObject trackPrefab;

  [DllImport("SoundStageNative")]
  public static extern void SetArrayToSingleValue(float[] a, int length, float val);

  timelineComponentInterface _timeline;
  public Transform trackHandle;

  List<timelineTrackComponentInterface> _trackInterfaces = new List<timelineTrackComponentInterface>();

  int startTracks = 4;
  int startunitres = 3;
  float startwidth = .4f;
  float startheight = .2f;
  Vector2 startrange = new Vector2(0, 8);
  Vector2 startio = new Vector2(0, 8);



  public override void Awake() {
    base.Awake();
    _timeline = GetComponent<timelineComponentInterface>();
    _timeline.SetStartVariables(4, 3, .4f, .2f, new Vector2(0, 8), new Vector2(0, 8));
  }

  void spawnTrack() {
    int n = _trackInterfaces.Count;
    timelineTrackComponentInterface _t = (Instantiate(trackPrefab, transform, false) as GameObject).GetComponentInChildren<timelineTrackComponentInterface>();
    _t.transform.localPosition = new Vector3(.075f, .024f + .05f * n, -.04f);
    _t.transform.localRotation = Quaternion.identity;
    _t.ID = n;
    _trackInterfaces.Add(_t);
  }

  void deleteTrack() {
    timelineTrackComponentInterface _t = _trackInterfaces[_trackInterfaces.Count - 1];
    _trackInterfaces.RemoveAt(_trackInterfaces.Count - 1);
    Destroy(_t.gameObject);
  }

  void Update() {
    int dif = Mathf.FloorToInt((trackHandle.localPosition.y - .025f) / .05f) - _trackInterfaces.Count;

    if (dif == 0) return;

    if (dif > 0) {
      for (int i = 0; i < dif; i++) spawnTrack();
    } else {
      for (int i = 0; i < -dif; i++) deleteTrack();
    }

    // update number of tracks on grid
    _timeline.updateTrackCount(_trackInterfaces.Count);
  }

  public override void onTimelineEvent(int track, bool on) {
    if (track < _trackInterfaces.Count) _trackInterfaces[track].onTimelineEvent(on);
  }

  public void trackUpdate(int trackID, bool on) {
    _timeline.onTimelineEvent(trackID, on);
  }

  private void OnAudioFilterRead(float[] buffer, int channels) {
    double dspTime = AudioSettings.dspTime;
    for (int i = 0; i < _trackInterfaces.Count; i++) {
      if (!_trackInterfaces[i].isOutgoing()) _trackInterfaces[i].signal.processBuffer(buffer, dspTime, channels);
    }
    SetArrayToSingleValue(buffer, buffer.Length, 0);
  }

  public override InstrumentData GetData() {
    TimelineData data = new TimelineData();
    data.deviceType = menuItem.deviceType.Timeline;
    GetTransformData(data);

    data.timelineData = _timeline.GetTimelineData();

    data.handleHeight = trackHandle.localPosition.y;

    List<timelineEvent.eventData> tempevents = new List<timelineEvent.eventData>();
    for (int i = 0; i < _timeline._tlEvents.Count; i++) {
      if (_timeline._tlEvents[i] != null) tempevents.Add(_timeline._tlEvents[i].getEventInfo());
    }

    data.timelineEvents = tempevents.ToArray();

    List<timelineTrackComponentInterface.trackComponent> _tracks = new List<timelineTrackComponentInterface.trackComponent>();
    for (int i = 0; i < _trackInterfaces.Count; i++) {
      if (_trackInterfaces[i] != null) _tracks.Add(_trackInterfaces[i].getTrackInfo());
    }

    data.tracks = _tracks.ToArray();

    return data;
  }

  public override void Load(InstrumentData d) {
    TimelineData data = d as TimelineData;
    base.Load(data);

    _timeline.SetTimelineData(data.timelineData);

    Vector3 pos = trackHandle.localPosition;
    pos.y = data.handleHeight;
    trackHandle.localPosition = pos;


    for (int i = 0; i < data.timelineEvents.Length; i++) {
      _timeline.SpawnTimelineEvent(data.timelineEvents[i].track, data.timelineEvents[i].in_out);
    }

    Update();
    for (int i = 0; i < data.tracks.Length; i++) {
      if (i < _trackInterfaces.Count) _trackInterfaces[i].Load(data.tracks[i]);
      else Debug.Log("ERROR");
    }
  }

}

public class TimelineData : InstrumentData {
  public TimelineComponentData timelineData;
  public float handleHeight;
  public timelineTrackComponentInterface.trackComponent[] tracks;
  public timelineEvent.eventData[] timelineEvents;
}