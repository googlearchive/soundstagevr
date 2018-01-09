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

public class UIpanel : manipObject {

  public TextMesh label;
  public GameObject outline;
  public Material textMat;
  public Color onColor, offColor;

  public Renderer outlineRender;

  public componentInterface _componentInterface;
  public int buttonID;

  public override void Awake() {
    base.Awake();
    if (transform.parent) _componentInterface = transform.parent.GetComponent<componentInterface>();

    onColor = Color.HSVToRGB(182f / 359, 1f, 118f / 255);
    offColor = Color.HSVToRGB(182f / 359, 0f, 118f / 255);
    textMat = label.GetComponent<Renderer>().material;
    setTextState(false);

    outlineRender = outline.GetComponent<Renderer>();
    outlineRender.material.SetColor("_TintColor", onColor);
    outlineRender.material.SetFloat("_EmissionGain", .428f);
    outlineRender.material.SetFloat("_InvFade", 1f);

    outline.SetActive(false);

    AwakeB();
  }

  public virtual void AwakeB() {

  }

  public void setText(string s) {
    label.text = s;
  }

  public virtual void setTextState(bool on) {
    textMat.SetColor("_TintColor", on ? onColor : offColor);
  }

  public override void setState(manipState state) {
    if (curState == manipState.selected && curState != state) selectEvent(false);
    else if (curState == manipState.grabbed && curState != state) grabEvent(false);


    curState = state;
    if (curState == manipState.none) {
      if (!toggled) setTextState(false);
    } else if (curState == manipState.selected) {
      selectEvent(true);
      setTextState(true);
    } else if (curState == manipState.grabbed) {
      setTextState(true);
      keyHit(true);
      grabEvent(true);
    }
  }

  public virtual void grabEvent(bool on) {

  }

  public virtual void selectEvent(bool on) {

  }

  public bool isHit = false;
  public bool toggled = false;
  public void keyHit(bool on) {
    isHit = on;
    toggled = on;
    if (on) {
      if (_componentInterface != null) _componentInterface.hit(on, buttonID);
      setToggleAppearance(true);
    } else {
      if (_componentInterface != null) _componentInterface.hit(on, buttonID);
      setToggleAppearance(false);
    }
  }

  public void setToggleAppearance(bool on) {
    outline.SetActive(on);
    setTextState(on);
  }

  public override void onTouch(bool on, manipulator m) {
    if (m != null) {
      if (m.emptyGrab) {
        if (on) {
          keyHit(true);
        }
      }
    }
  }
}
