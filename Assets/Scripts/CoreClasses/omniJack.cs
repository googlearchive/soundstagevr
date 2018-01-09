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
using System.Linq;

public class omniJack : manipObject {
  public bool outgoing = true;
  public bool plugged = false;

  public GameObject plugPrefab;
  public GameObject plugRep;
  public int ID = -1;

  public signalGenerator signal, homesignal;
  Material mat;
  Renderer jackRepRend;

  public omniPlug far, near;

  public soundUtils.Hue jackHue = soundUtils.Hue.Red;

  Color jackColor = Color.white;
  float jackTargetHue = 0.5f;

  public override void Awake() {
    base.Awake();
    gameObject.layer = 12; //jacks
    mat = GetComponent<Renderer>().material;
    jackRepRend = plugRep.GetComponent<Renderer>();
    jackTargetHue = findHue();
    jackColor = Color.HSVToRGB(jackTargetHue, 0.8f, 0.5f);
    if (homesignal == null) homesignal = transform.parent.GetComponent<signalGenerator>();
    mat.SetColor("_EmissionColor", jackColor);

    if (masterControl.instance != null) {
      if (!masterControl.instance.jacksEnabled) GetComponent<Collider>().enabled = false;
    }
  }

  public void setColor(Color c) {
    mat.SetColor("_EmissionColor", c);
  }

  public override void setState(manipState state) {
    if (curState == state) return;
    curState = state;

    if (curState == manipState.grabbed) {
      if (near != null) {
        manipulatorObj.GetComponent<manipulator>().ForceGrab(near);
      } else {

        if (manipulatorObjScript != null) manipulatorObjScript.hapticPulse(750);

        GameObject j = Instantiate(plugPrefab, manipulatorObj.position, manipulatorObj.rotation) as GameObject;
        near = j.GetComponent<omniPlug>();
        near.transform.localScale = transform.localScale;
        near.transform.parent = transform;
        near.transform.localPosition = new Vector3(0, -.0175f, 0);
        near.transform.localRotation = Quaternion.Euler(-90, 0, 0);
        near.connected = this;
        near.signal = homesignal;

        j = Instantiate(plugPrefab, manipulatorObj.position, manipulatorObj.rotation) as GameObject;
        far = j.GetComponent<omniPlug>();
        far.Setup(jackTargetHue, outgoing, near);
        near.Setup(jackTargetHue, !outgoing, far);
        manipulatorObj.GetComponent<manipulator>().ForceGrab(far);

        plugRep.SetActive(false);
      }
    }

    if (curState == manipState.none) {
      if (near == null) dimCoroutine = StartCoroutine(dimRoutine());
      else near.mouseoverEvent(false);
    } else if (curState == manipState.selected) {
      if (dimCoroutine != null) StopCoroutine(dimCoroutine);
      jackColor = Color.HSVToRGB(findHue(), 0.8f, 0.2f);
      jackRepRend.material.SetFloat("_EmissionGain", .3f);
      jackRepRend.material.SetColor("_TintColor", jackColor);

      if (near == null) plugRep.SetActive(true);
      else near.mouseoverEvent(true);
    }
  }

  void Update() {
    if (outgoing) return;
    if (near == null) {
      signal = null;
      return;
    }

    if (near.otherPlug.connected == null) signal = null;
    else if (signal != near.otherPlug.signal) signal = near.otherPlug.signal;
  }

  public void endConnection() {
    near = null;
    far = null;
  }


  public void beginConnection(omniPlug plug) {
    near = plug;
    far = plug.otherPlug;

    if (!outgoing && near.otherPlug.signal != null) {
      signal = near.otherPlug.signal;
    }
  }

  float findHue() {
    if (jackHue == soundUtils.Hue.Red) return Random.value * .14f;
    else if (jackHue == soundUtils.Hue.Green) return Random.Range(.15f, .44f);
    else return Random.Range(.45f, .7f);
  }

  Coroutine dimCoroutine;
  IEnumerator dimRoutine() {
    float t = 0;
    while (t < 1) {
      t = Mathf.Clamp01(t + Time.deltaTime * 2);
      jackRepRend.material.SetFloat("_EmissionGain", Mathf.Lerp(.3f, 0, t));
      jackRepRend.material.SetColor("_TintColor", Color.Lerp(jackColor, Color.black, t));
      yield return null;
    }
    plugRep.SetActive(false);
  }


  Coroutine flashCoroutine;
  public void flash(Color c) {
    if (flashCoroutine != null)
      StopCoroutine(flashCoroutine);
    mat.SetColor("_EmissionColor", jackColor);
    if (c != Color.black) {
      targColor = c;
      flashCoroutine = StartCoroutine(flashRoutine());
    }
  }

  Color targColor = new Color(.5f, .5f, 1f);
  IEnumerator flashRoutine() {
    float t = 0;
    while (true) {
      t += Time.deltaTime * 6;
      mat.SetColor("_EmissionColor", Color.Lerp(Color.black, targColor, Mathf.Abs(Mathf.Sin(t))));
      yield return null;
    }
  }
}
