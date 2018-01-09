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

public class handle : manipObject {

  public Transform masterObj;
  Transform masterObjParent;
  public handle otherHandle;
  Material mat;
  public GameObject matTarg;
  public int ID = 0;

  GameObject highlight;
  Material highlightMat;

  public override void Awake() {
    base.Awake();
    if (masterObj == null) masterObj = transform.parent;
    masterObjParent = masterObj.parent;
    if (ID == 0) {
      if (GetComponent<Renderer>() == null) mat = matTarg.GetComponent<Renderer>().material;
      else mat = GetComponent<Renderer>().material;
    }
    handle[] temp = transform.parent.GetComponentsInChildren<handle>();
    for (int i = 0; i < temp.Length; i++) {
      if (temp[i] != this) otherHandle = temp[i];
    }
    if (ID == 0) createHandleFeedback();
  }

  public void setObjectParent(Transform t) {
    masterObjParent = t;
    masterObj.parent = t;
  }

  void createHandleFeedback() {

    highlight = new GameObject("highlight");

    MeshFilter m = highlight.AddComponent<MeshFilter>();

    if (GetComponent<Renderer>() == null) m.mesh = matTarg.GetComponent<MeshFilter>().mesh;
    else m.mesh = GetComponent<MeshFilter>().mesh;


    MeshRenderer r = highlight.AddComponent<MeshRenderer>();
    r.material = Resources.Load("Materials/Highlight") as Material;
    highlightMat = r.material;

    highlight.transform.SetParent(transform, false);

    if (transform.name == "keyboardSeating")
      highlight.transform.localScale = new Vector3(1.03f, 1.1f, 1.1f);
    else if (transform.name == "adsrFrame")
      highlight.transform.localScale = new Vector3(1.03f, 1.05f, 1.1f);
    else if (transform.name == "tapeDeck")
      highlight.transform.localScale = new Vector3(1.03f, 1.1f, 1.1f);
    else if (transform.name == "drumpadHandle")
      highlight.transform.localScale = new Vector3(1.1f, 1.3f, 1.1f);
    else if (transform.name == "xyloSide") {
      highlight.transform.localScale = new Vector3(1.01f, 1.1f, 1.1f);
      highlight.transform.localPosition = new Vector3(.01f, 0, 0);
    } else if (transform.name == "title") highlight.transform.localScale = Vector3.one * 1.02f;
    else highlight.transform.localScale = Vector3.one * 1.1f;

    Color c = Color.HSVToRGB(.65f, .7f, .5f);
    highlightMat.SetColor("_TintColor", c);
    highlightMat.SetFloat("_EmissionGain", .2f);

    highlight.SetActive(false);
  }

  bool scaling = false;
  public override void grabUpdate(Transform t) {
    if (otherHandle.curState == manipState.grabbed) {
      if (ID == 1) {
        scaleUpdate();
      }

      if (manipulatorObj.parent == masterObj.parent) posRotUpdate();
    } else {
      masterObj.parent = manipulatorObj.parent;
      scaling = false;
      doublePosRot = false;
    }
  }

  bool doublePosRot = false;
  Vector3 initOtherManipPos = Vector3.zero;
  Quaternion initRot = Quaternion.identity;
  Vector3 doubleManipOffset = Vector3.zero;
  void posRotUpdate() {
    if (!doublePosRot) {
      initOtherManipPos = manipulatorObj.InverseTransformPoint(otherHandle.manipulatorObj.position);
      initRot = masterObj.localRotation;
      doubleManipOffset = masterObj.position - Vector3.Lerp(manipulatorObj.position, otherHandle.manipulatorObj.position, .5f);
    }
    doublePosRot = true;

    Vector3 otherManipPos = manipulatorObj.InverseTransformPoint(otherHandle.manipulatorObj.position);
    Quaternion q = Quaternion.FromToRotation(initOtherManipPos, otherManipPos);
    masterObj.localRotation = q * initRot;
    masterObj.position = Vector3.Lerp(manipulatorObj.position, otherHandle.manipulatorObj.position, .5f) + doubleManipOffset;
  }

  float initDistance = 0;
  Vector3 initScale = Vector3.one;
  void scaleUpdate() {
    if (!scaling) {
      initDistance = Vector3.Distance(otherHandle.manipulatorObj.position, manipulatorObj.position);
      initScale = masterObj.localScale;
    }
    scaling = true;
    float dist = Vector3.Distance(otherHandle.manipulatorObj.position, manipulatorObj.position);
    masterObj.localScale = initScale * (dist / initDistance);
  }

  public bool trashReady = false;
  public trashcan curTrash;
  void OnCollisionEnter(Collision coll) {
    if (coll.transform.name == "trashMenu" && (curState == manipState.grabbed || otherHandle.curState == manipState.grabbed) && ID == 0) {
      curTrash = coll.transform.GetComponent<trashcan>();
      curTrash.setReady(true);
      if (manipulatorObjScript != null) manipulatorObjScript.hapticPulse(1000);
      else if (otherHandle.manipulatorObjScript != null) otherHandle.manipulatorObjScript.hapticPulse(1000);
      trashReady = true;
      soundUtils.SetupMaterialWithBlendMode(mat, soundUtils.BlendMode.Fade);
    }
  }

  void OnCollisionExit(Collision coll) {
    if (coll.transform.name == "trashMenu" && ID == 0) {
      coll.transform.GetComponent<trashcan>().setReady(false);
      if (manipulatorObjScript != null) manipulatorObjScript.hapticPulse(1000);
      else if (otherHandle.manipulatorObjScript != null) otherHandle.manipulatorObjScript.hapticPulse(1000);
      curTrash = null;
      trashReady = false;
      soundUtils.SetupMaterialWithBlendMode(mat, soundUtils.BlendMode.Opaque);
    }
  }

  public void toggleHandles(bool on) {
    Collider[] colls = GetComponents<Collider>();
    for (int i = 0; i < colls.Length; i++) colls[i].enabled = on;
  }

  public override void setState(manipState state) {
    if (curState == state) return;

    if (curState == manipState.grabbed && state != manipState.grabbed) {
      if (curTrash != null) {
        if (curTrash.gameObject.activeSelf) curTrash.trashEvent();
        else {
          curTrash.setReady(false);
          curTrash = null;
          trashReady = false;
          soundUtils.SetupMaterialWithBlendMode(mat, soundUtils.BlendMode.Opaque);
        }
      } else if (otherHandle.curTrash != null) {
        if (otherHandle.curTrash.gameObject.activeSelf) otherHandle.curTrash.trashEvent();
        else {
          otherHandle.curTrash.setReady(false);
          otherHandle.curTrash = null;
          otherHandle.trashReady = false;
          soundUtils.SetupMaterialWithBlendMode(mat, soundUtils.BlendMode.Opaque);
        }
      } else {
        otherHandle.trashReady = false;
        trashReady = false;
        soundUtils.SetupMaterialWithBlendMode(mat, soundUtils.BlendMode.Opaque);
      }

      if (trashReady || otherHandle.trashReady) {
        if (masterObj.gameObject.GetComponentInChildren<tape>())
          masterObj.gameObject.GetComponentInChildren<tape>().Eject();
        Destroy(masterObj.gameObject);
      } else masterObj.parent = masterObjParent;

      if (!masterControl.instance.handlesEnabled) {
        toggleHandles(false);
        otherHandle.toggleHandles(false);
      }
    }

    curState = state;

    if (ID == 0) {
      if (curState == manipState.none) {
        if (otherHandle.curState == manipState.none) highlight.SetActive(false);
      }
      if (curState == manipState.selected) {
        highlight.SetActive(true);
        highlightMat.SetFloat("_EmissionGain", .35f);
      }
      if (curState == manipState.grabbed) {
        highlight.SetActive(true);
        highlightMat.SetFloat("_EmissionGain", .45f);
      }
    } else {
      if (curState == manipState.none) {
        if (otherHandle.curState == manipState.none) otherHandle.highlight.SetActive(false);
      }
      if (curState == manipState.selected) {
        otherHandle.highlight.SetActive(true);
        otherHandle.highlightMat.SetFloat("_EmissionGain", .35f);
      }
      if (curState == manipState.grabbed) {
        otherHandle.highlight.SetActive(true);
        otherHandle.highlightMat.SetFloat("_EmissionGain", .45f);
      }
    }
    if (curState == manipState.grabbed) {
      masterObj.parent = manipulatorObj.parent;
      doublePosRot = false;
    }
  }
}
