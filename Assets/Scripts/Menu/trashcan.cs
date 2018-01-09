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

public class trashcan : MonoBehaviour {
  public bool ready = false;
  Material mat;
  menuManager manager;

  public AudioClip trashAct, trashOn, trashOff;

  Color offColor = new Color(.5f, .5f, .1f);
  Color onColor = new Color(1, 1, 1);

  void Awake() {
    mat = transform.GetChild(0).GetComponent<Renderer>().material;
    manager = transform.parent.parent.GetComponent<menuManager>();
    offColor = Color.HSVToRGB(.6f, .7f, .9f);
    mat.SetColor("_TintColor", offColor);
    mat.SetFloat("_EmissionGain", .2f);
  }

  public void trashEvent() {
    manager.GetComponent<AudioSource>().PlayOneShot(trashAct, .5f);
    StartCoroutine(flash());
  }

  IEnumerator flash() {
    float t = 0;
    mat.SetFloat("_EmissionGain", .6f);
    while (t < 1) {
      t = Mathf.Clamp01(t + Time.deltaTime * 2);
      mat.SetFloat("_EmissionGain", Mathf.Lerp(.6f, .2f, t));
      mat.SetColor("_TintColor", Color.Lerp(onColor, offColor, t));
      yield return null;
    }
  }

  public void setReady(bool on) {
    if (on) {
      mat.SetColor("_TintColor", onColor);
      manager.GetComponent<AudioSource>().PlayOneShot(trashOn, .15f);
    } else {
      manager.GetComponent<AudioSource>().PlayOneShot(trashOff, .15f);
      mat.SetColor("_TintColor", offColor);
    }
  }
}
