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

﻿using UnityEngine;
using System.Collections;

public class pauseMenuItem : manipObject {
  public TextMesh label;
  public Texture2D tex;
  Material mat;
  Material textMat;
  pauseMenu mainmenu;
  public pauseMenu.itemType itemType;
  public int ID = -1;

  Color normalColor;
  public override void Awake() {
    base.Awake();
    mat = transform.GetChild(0).GetComponent<Renderer>().material;
    mat.SetTexture("_MainTex", tex);

    normalColor = Color.HSVToRGB(.6f, .7f, .9f);
    if (itemType == pauseMenu.itemType.confirmItem) normalColor = Color.HSVToRGB(.4f, .7f, .9f);
    else if (itemType == pauseMenu.itemType.cancelItem) normalColor = Color.HSVToRGB(0, .7f, .9f);

    if (label != null) {
      textMat = label.GetComponent<Renderer>().material;
      textMat.SetColor("_TintColor", normalColor);
    }
    if (itemType == pauseMenu.itemType.tooltipItem) {
      if (label != null) {
        label.text = "TOOLTIP\nON";
      }
    }
    if (itemType == pauseMenu.itemType.exampleItem) {
      if (label != null) {
        label.text = "HIDE\nEXAMPLES";
      }
    }
    if (itemType == pauseMenu.itemType.confirmItem || itemType == pauseMenu.itemType.cancelItem) {
      if (label != null) {
        textMat.SetColor("_TintColor", normalColor);
      }
    }

    mat.SetColor("_TintColor", normalColor);
    mat.SetFloat("_EmissionGain", .3f);
  }

  void Start() {
    mainmenu = transform.parent.parent.GetComponent<pauseMenu>();
    if (mainmenu == null) mainmenu = transform.parent.parent.parent.GetComponent<pauseMenu>();

  }

  void Update() {
    if (itemType == pauseMenu.itemType.tooltipItem && label != null) {
      if (masterControl.instance.tooltipsOn) label.text = "TOOLTIPS\nON";
      else label.text = "TOOLTIPS\nOFF";
    }

    if (itemType == pauseMenu.itemType.exampleItem && label != null) {
      if (masterControl.instance.examplesOn) label.text = "HIDE\nEXAMPLES";
      else label.text = "SHOW\nEXAMPLES";
    }
  }

  public override void setState(manipState state) {
    curState = state;
    if (curState == manipState.none) {
      mat.SetColor("_TintColor", normalColor);
      mat.SetFloat("_EmissionGain", .3f);


      if (textMat != null) {
        textMat.SetColor("_TintColor", normalColor);
        textMat.SetFloat("_EmissionGain", .3f);
      }
    } else if (curState == manipState.selected) {
      mat.SetColor("_TintColor", normalColor);
      mat.SetFloat("_EmissionGain", .6f);

      if (textMat != null) {
        textMat.SetColor("_TintColor", normalColor);
        textMat.SetFloat("_EmissionGain", .3f);
      }
    } else if (curState == manipState.grabbed) {
      mat.SetColor("_TintColor", Color.white);

      if (textMat != null) {
        textMat.SetColor("_TintColor", normalColor);
        textMat.SetFloat("_EmissionGain", .6f);
      }
      mainmenu.itemSelect(itemType, ID);
    }
  }

  IEnumerator appearRoutine(bool on) {
    float timer = 0;
    if (on) {
      Vector3[] sizes = new Vector3[]
      {
                new Vector3(.05f,.05f,.05f),
                new Vector3(1f,.1f,1f),
                new Vector3(1,1,1)
      };

      mat.SetFloat("_EmissionGain", .9f);

      transform.localScale = sizes[0];
      while (timer < 1) {
        timer = Mathf.Clamp01(timer + Time.deltaTime * 12);
        transform.localScale = Vector3.Lerp(sizes[0], sizes[1], timer);
        yield return null;
      }

      timer = 0;
      while (timer < 1) {
        timer = Mathf.Clamp01(timer + Time.deltaTime * 6);
        mat.SetFloat("_EmissionGain", Mathf.Lerp(.9f, .7f, timer));
        transform.localScale = Vector3.Lerp(sizes[1], sizes[2], timer);
        yield return null;
      }

      timer = 0;
      while (timer < 1) {
        timer = Mathf.Clamp01(timer + Time.deltaTime * 10);
        mat.SetFloat("_EmissionGain", Mathf.Lerp(.7f, .3f, timer));
        yield return null;
      }
    } else {
      while (timer < 1) {
        timer = Mathf.Clamp01(timer + Time.deltaTime * 6);
        transform.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, timer);
        yield return null;
      }
    }
  }

  void OnDisable() {
    StopAllCoroutines();
  }

  Coroutine appearCoroutine;
  public void Appear(bool on) {
    if (!gameObject.activeInHierarchy) return;
    if (appearCoroutine != null) StopCoroutine(appearCoroutine);
    appearCoroutine = StartCoroutine(appearRoutine(on));
  }
}