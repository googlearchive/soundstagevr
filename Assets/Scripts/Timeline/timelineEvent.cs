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

public class timelineEvent : MonoBehaviour {

  public int track;
  public Vector2 in_out;
  public timelineHandle body;
  public Transform edgeIn, edgeOut;
  public xHandle edgeInHandle, edgeOutHandle;
  public timelineComponentInterface _componentInterface;

  public bool recording = false;
  public bool snapping = false;
  public bool playing = false;
  public bool grabbed = false;

  public bool multiselected = false;

  public struct eventData {
    public int track;
    public Vector2 in_out;

    public eventData(int t, Vector2 io) {
      track = t;
      in_out = io;
    }
  };

  public eventData getEventInfo() {
    return new eventData(track, in_out);
  }

  public bool inRange(float x) {
    return x > in_out.x && x < in_out.y && !recording && body.curState != manipObject.manipState.grabbed;
  }

  public bool inMultiSelectRange(Vector2 msX, Vector2 msY) {
    if ((track + 1) < msY.x || track > msY.y) return false;
    bool c1 = in_out.x <= msX.x && in_out.y >= msX.x;
    bool c2 = msX.x <= in_out.x && msX.y >= in_out.x;
    return c1 || c2;
  }

  public Vector2 multiselect_io;
  public int multiselect_track;
  public void OnMultiselect(bool on) {
    multiselected = on;
    body.OnMultiselect(on);
    multiselect_io = in_out;
    multiselect_track = track;
  }

  public void removeSelf() {
    if (playing) _componentInterface._targetDeviceInterface.onTimelineEvent(track, false);
    _componentInterface._tlEvents.Remove(this);

  }

  public void overlapCheck() {
    _componentInterface.overlapCheck(this);
  }

  bool recordingUpdate = false;
  public void setRecord(bool on) {
    recordingUpdate = true;
    if (recording) playing = true;
    recording = on;
    body.setRecord(on);
  }

  public void toggleEdges(bool on) {
    edgeInHandle.gameObject.SetActive(on);
    edgeOutHandle.gameObject.SetActive(on);
  }

  public void init(int t, Vector2 io, timelineComponentInterface _dev) {
    snapping = _dev.snapping;
    track = t;
    in_out = io;
    _componentInterface = _dev;
    body.setHue(t);
    gridUpdate();
  }

  Vector2 preSnapIO = Vector2.zero;
  Vector2 postSnapIO = Vector2.zero;
  int preSnapTrack = -1;
  public void updateSnap(bool s) {
    snapping = s;

    if (snapping) {
      preSnapTrack = track;
      preSnapIO = in_out;
      in_out.x = _componentInterface._gridParams.UnittoSnap(in_out.x, true);
      in_out.y = in_out.x + (preSnapIO.y - preSnapIO.x);
      postSnapIO = in_out;
    } else {
      if (postSnapIO == in_out && preSnapTrack == track) {
        in_out = preSnapIO;
      }
    }

    overlapCheck();
    gridUpdate();
  }

  void Update() {
    if (pleaseDestroy) Destroy(gameObject);

    if (recordingUpdate) {
      recordingUpdate = false;
      toggleEdges(!recording);
    }

    if (edgeInHandle.curState == manipObject.manipState.grabbed || edgeOutHandle.curState == manipObject.manipState.grabbed) {
      edgeIn.localPosition = edgeInHandle.transform.localPosition;
      edgeOut.localPosition = edgeOutHandle.transform.localPosition;

      edgeIn.position = _componentInterface.transform.TransformPoint(_componentInterface.worldPosToGridPos(edgeIn.position));
      edgeOut.position = _componentInterface.transform.TransformPoint(_componentInterface.worldPosToGridPos(edgeOut.position));

      gridUpdateDesired = true;
      recalcTrackPosition();
      overlapCheck();
    } else if (gridUpdateDesired) {
      gridUpdateDesired = false;
      gridUpdate();
      rangeCheck();
    }

    bool edgeCheck = in_out.y - in_out.x >= .2f || edgeInHandle.curState == manipObject.manipState.grabbed || edgeOutHandle.curState == manipObject.manipState.grabbed;
    toggleEdges(edgeCheck);

  }

  public bool pleaseDestroy = false;
  void rangeCheck() {
    bool a = in_out.y <= in_out.x || in_out.x >= in_out.y;
    bool b = Mathf.Abs(in_out.x - in_out.y) < .002f;
    bool c = (a || b) && !recording;
    if (c) {
      _componentInterface._tlEvents.Remove(this);
      pleaseDestroy = true;
    }
  }

  bool gridUpdateDesired = false;
  public void setOut(float o) {
    if (snapping) {
      in_out.y = _componentInterface._gridParams.UnittoSnap(o, false);
      if (in_out.y == in_out.x) in_out.y += 1f / _componentInterface._gridParams.snapFraction;
    } else in_out.y = o;
    rangeCheck();
    gridUpdateDesired = true;
  }

  public void setIn(float o, bool toFloor = true) {
    if (snapping) in_out.x = _componentInterface._gridParams.UnittoSnap(o, toFloor);
    else in_out.x = o;
    rangeCheck();
    gridUpdateDesired = true;
  }

  public void recalcTrackPosition() {
    int prevTrack = track;

    edgeInHandle.xBounds.x = edgeOut.transform.localPosition.x + .01f;
    edgeInHandle.xBounds.y = -transform.localPosition.x;

    edgeOutHandle.xBounds.x = -_componentInterface._gridParams.getGridWidth() - transform.localPosition.x;
    edgeOutHandle.xBounds.y = edgeIn.transform.localPosition.x - .01f;

    in_out.x = _componentInterface._gridParams.XtoUnit(_componentInterface.transform.InverseTransformPoint(edgeIn.transform.position).x);
    in_out.y = _componentInterface._gridParams.XtoUnit(_componentInterface.transform.InverseTransformPoint(edgeOut.transform.position).x);
    track = (int)_componentInterface._gridParams.YtoUnit(_componentInterface.transform.InverseTransformPoint(transform.position).y);

    if (prevTrack != track) body.setHue(track);

    gridUpdate();
  }

  public float clampX(float x) {
    if ((x + body.transform.localScale.x / 2f) > 0) x = -body.transform.localScale.x / 2f;
    if ((x - body.transform.localScale.x / 2f) < -_componentInterface._gridParams.getGridWidth()) x = -_componentInterface._gridParams.getGridWidth() + body.transform.localScale.x / 2f;
    return x;
  }

  public void gridUpdate() {

    if (!_componentInterface._gridParams.isEventVisible(track, in_out)) {
      body.gameObject.SetActive(false);
      return;
    }

    body.gameObject.SetActive(true);

    Vector2 temprange = in_out;
    if (temprange.x < _componentInterface._gridParams.range.x) {
      temprange.x = _componentInterface._gridParams.range.x;
    }

    if (temprange.y > _componentInterface._gridParams.range.y) {
      temprange.y = _componentInterface._gridParams.range.y;
    }

    float tempX = (temprange.y - temprange.x) * _componentInterface._gridParams.unitSize;
    body.transform.localScale = new Vector3(tempX, _componentInterface._gridParams.trackHeight, .025f);
    edgeOutHandle.transform.localScale = edgeInHandle.transform.localScale = new Vector3(.002f, _componentInterface._gridParams.trackHeight, .028f);

    Vector2 pos = Vector2.zero;
    pos.y = (track + .5f) * _componentInterface._gridParams.trackHeight;
    pos.x = ((temprange.y - temprange.x) / 2f + (temprange.x - _componentInterface._gridParams.range.x)) * -_componentInterface._gridParams.unitSize;
    transform.localPosition = pos;

    if (edgeInHandle.curState != manipObject.manipState.grabbed) {
      edgeInHandle.transform.localPosition = edgeIn.localPosition = new Vector2(tempX / 2f, 0);
    }

    if (edgeOutHandle.curState != manipObject.manipState.grabbed) {
      edgeOutHandle.transform.localPosition = edgeOut.localPosition = new Vector2(tempX / -2f, 0);
    }
  }
}
