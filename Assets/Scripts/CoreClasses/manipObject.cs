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

public class manipObject : MonoBehaviour {
  public manipState curState = manipState.none;

  public Transform manipulatorObj, selectObj;
  public manipulator manipulatorObjScript;
  public bool stickyGrip = false;
  public bool canBeDeleted = false;

  bool selected = false;
  bool grabbed = false;

  public virtual void Awake() {
    gameObject.layer = 9;
  }

  public enum manipState {
    none,
    selected,
    grabbed
  };

  public virtual void copy(manipulator m) {
  }

  public virtual void selfdelete() {
  }

  public virtual void grabUpdate(Transform t) {
  }

  public virtual void selectUpdate(Transform t) {
  }

  public virtual void setState(manipState state) {
    curState = state;
  }

  public virtual void onTouch(bool on, manipulator m) {
  }

  public virtual void setTouch(bool on) { }
  public virtual void setPress(bool on) { }
  public virtual void updateTouchPos(Vector2 p) { }

  void OnDisable() {
    if (curState == manipState.grabbed) if (manipulatorObjScript != null) manipulatorObjScript.ForceRelease();
    StopAllCoroutines();
  }


  public void setSelect(bool on, Transform t) {
    selected = on;
    if (selected) {
      selectObj = t;
      setState(manipState.selected);
    } else {
      selectObj = null;
      setState(manipState.none);
    }
  }

  public void setGrab(bool on, Transform t) {
    grabbed = on;
    if (on) {

      manipulatorObj = t;
      manipulatorObjScript = t.GetComponent<manipulator>();
      setState(manipState.grabbed);
    } else {
      manipulatorObj = null;
      manipulatorObjScript = null;
      if (selected) setState(manipState.selected);
      else setState(manipState.none);
    }
  }
}
