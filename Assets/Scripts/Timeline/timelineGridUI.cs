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

public class timelineGridUI : MonoBehaviour {
  public GameObject eventPreviewPrefab, multiselectPrefab;
  public timelineComponentInterface _interface;
  Dictionary<manipulator, GameObject> activePreviews = new Dictionary<manipulator, GameObject>();

  void Awake() {
    gameObject.layer = 9;
    _interface = GetComponentInParent<timelineComponentInterface>();
  }

  public void updateResolution() {
    foreach (KeyValuePair<manipulator, GameObject> entry in activePreviews) {
      Vector3 s = entry.Value.transform.localScale;
      s.y = _interface._gridParams.trackHeight;
      s.x = _interface._gridParams.unitSize / _interface._gridParams.snapFraction;
      entry.Value.transform.localScale = s;
    }

    if (multiselectWindow != null) {
      multiselectWindow.Close();
    }
  }

  void OnCollisionEnter(Collision coll) {
    manipulator o = coll.transform.GetComponent<manipulator>();
    if (o != null) {
      o.toggleMultiselect(true, this);

      if (activePreviews.ContainsKey(o)) Destroy(activePreviews[o]);
      activePreviews[o] = Instantiate(eventPreviewPrefab, transform.parent, false);
      Vector3 s = activePreviews[o].transform.localScale;
      s.y = _interface._gridParams.trackHeight;
      s.x = _interface._gridParams.unitSize / _interface._gridParams.snapFraction;
      activePreviews[o].transform.localScale = s;
    }
  }

  void OnCollisionExit(Collision coll) {
    manipulator o = coll.transform.GetComponent<manipulator>();
    if (o != null) {
      o.toggleMultiselect(false, this);
      if (activePreviews.ContainsKey(o)) {
        Destroy(activePreviews[o]);
        activePreviews.Remove(o);
      }
    }
  }

  public void spawnEvent(manipulator m, Vector2 gridpos) {
    int track = (int)_interface._gridParams.YtoUnit(gridpos.y);
    Vector2 io = Vector2.zero;
    io.x = _interface._gridParams.XtoUnit(gridpos.x + _interface._gridParams.unitSize / (2f * _interface._gridParams.snapFraction));
    io.y = _interface._gridParams.XtoUnit(gridpos.x - _interface._gridParams.unitSize / (2f * _interface._gridParams.snapFraction));
    timelineHandle tl = _interface.SpawnTimelineEvent(track, io).GetComponentInChildren<timelineHandle>();
    tl.stretchMode = true;
    m.ForceGrab(tl);
  }

  void Update() {
    foreach (KeyValuePair<manipulator, GameObject> entry in activePreviews) {
      if (entry.Key.isGrabbing() || multiselectTransform != null) {
        entry.Value.SetActive(false);
        entry.Key.toggleMultiselect(false, this);
      } else if (entry.Key.emptyGrab) {
        spawnEvent(entry.Key, _interface.worldPosToGridPos(entry.Key.transform.position));
        killMultiselect();
      } else {
        entry.Value.SetActive(true);
        entry.Key.toggleMultiselect(true, this);
        entry.Value.transform.position = _interface.transform.TransformPoint(_interface.worldPosToGridPos(entry.Key.transform.position, true));
      }
    }

    if (multiselectTransform != null) //multiselecting
    {
      updateMultiselect();
    }
  }

  public void killMultiselect() {
    if (multiselectWindow != null) {
      multiselectWindow.Close();

    }
  }

  void updateMultiselect() {
    Vector2 b = transform.parent.InverseTransformPoint(multiselectTransform.position);
    multiselectWindow.transform.localPosition = Vector2.Lerp(startMultiselect, b, .5f);
    multiselectWindow.transform.localScale = new Vector3(b.x - startMultiselect.x, b.y - startMultiselect.y, 1);
    multiselectWindow.SelectCheck();
  }

  timelineMultiSelect multiselectWindow; // make a manip object that can be moved like a timeline but effects all the events below relative 
  Transform multiselectTransform;
  Vector2 startMultiselect;
  public void onMultiselect(bool on, Transform m) {

    if (on) {
      multiselectTransform = m;
      if (multiselectWindow != null) killMultiselect();

      startMultiselect = transform.parent.InverseTransformPoint(m.position);
      multiselectWindow = (Instantiate(multiselectPrefab, transform.parent, false) as GameObject).GetComponent<timelineMultiSelect>();
      multiselectWindow._interface = _interface;
      updateMultiselect();
    } else {
      if (multiselectWindow != null) {
        multiselectWindow.Activate();
        multiselectTransform = null;
      }
    }
  }
}
