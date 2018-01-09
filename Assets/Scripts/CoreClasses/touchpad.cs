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

public class touchpad : MonoBehaviour {
    public GameObject[] halfOutlines;
    public Transform padTouchFeedback;
    public Renderer[] halfSprites;
    public GameObject[] buttonContainers;

    public manipulator manip;

    public Color onColor = Color.HSVToRGB(208 / 359f, 234 / 255f, 93 / 255f);

    public Color offColor = Color.HSVToRGB(0,0,40 / 255f);

    bool[] halfSelected = new bool[] { false, false };

    bool copyOn = false;
    bool deleteOn = false;
    bool multiselectOn = false;
    void Awake () {
        padTouchFeedback.gameObject.SetActive(false);
        Material temp = padTouchFeedback.GetComponent<Renderer>().material;
        padTouchFeedback.GetComponent<Renderer>().material.SetColor("_TintColor", onColor);
        padTouchFeedback.GetComponent<Renderer>().material.SetFloat("_EmissionGain", .6f);

        for (int i = 0; i < halfOutlines.Length; i++)
        {

            halfOutlines[i].GetComponent<Renderer>().material.SetColor("_TintColor", onColor);
            halfOutlines[i].GetComponent<Renderer>().material.SetFloat("_EmissionGain", .55f);
            halfOutlines[i].SetActive(false);
        }

        halfSprites[0].material.SetColor("_TintColor", onColor);
        halfSprites[0].material.SetFloat("_EmissionGain", .5f);

        for (int i = 1; i < halfSprites.Length; i++)
        {

            halfSprites[i].material.SetColor("_TintColor", offColor);
            halfSprites[i].material.SetFloat("_EmissionGain", 0);
            halfSprites[i].gameObject.SetActive(false);
        }

        buttonContainers[1].SetActive(false);
        if(masterControl.instance != null) buttonContainers[0].SetActive(masterControl.instance.tooltipsOn);
    }
   
    void onSelect(int n, bool on) 
    {
        halfSelected[n] = on;
        halfOutlines[n].SetActive(on);
    }
    
    public void toggleCopy(bool on)
    {
        if (copyOn == on) return;
        copyOn = on;
        buttonContainers[1].SetActive(on);
        halfSprites[1].gameObject.SetActive(on);
        onSelect(1, false);
        manip.SetCopy(false);
        halfSprites[1].material.SetColor("_TintColor", offColor);
        halfSprites[1].material.SetFloat("_EmissionGain", 0);
    }

    public void toggleDelete(bool on)
    {
        if (on && multiselectOn)
        {
            toggleMultiselect(false);
        }

        if (deleteOn == on) return;
        deleteOn = on;
        buttonContainers[1].SetActive(on);
        halfSprites[2].gameObject.SetActive(on);
        onSelect(1, false);
        halfSprites[2].material.SetColor("_TintColor", offColor);
        halfSprites[2].material.SetFloat("_EmissionGain", 0);
    }

    public void toggleMultiselect(bool on)
    {
        if (on && deleteOn)
        {
            return;
        }

        if (multiselectOn == on) return;
        multiselectOn = on;
        buttonContainers[1].SetActive(on);
        halfSprites[3].gameObject.SetActive(on);
        onSelect(1, false);
        halfSprites[3].material.SetColor("_TintColor", offColor);
        halfSprites[3].material.SetFloat("_EmissionGain", 0);
    }

    public void updateTouchPos(Vector2 p)
    {
        padTouchFeedback.localPosition = new Vector3(p.x * .004f, .0008f, p.y * .004f);
        if(halfSelected[0] != (p.y < -0.1f))
        {
            onSelect(0, (p.y < -0.1f));
        }
        if(copyOn || deleteOn || multiselectOn)
        {
            if (halfSelected[1] != (p.y > 0.1f))
            {
                onSelect(1, (p.y > 0.1f));
            }
        }
    }

    public  void setTouch(bool on)
    {
        padTouchFeedback.gameObject.SetActive(on);
        if (!on)
        {
            onSelect(0, false);
            onSelect(1, false);
        }
    }

    public void setPress(bool on)
    {
        padTouchFeedback.GetComponent<Renderer>().material.SetFloat("_EmissionGain", on ? .7f : .6f);
        if (halfSelected[0])
        {
            if(!on || (on && masterControl.instance.tooltipsOn))
            manip.toggleTips(on);
        }
        else if (halfSelected[1])
        {
            if (deleteOn)
            {
                manip.DeleteSelection(on);
                halfSprites[2].material.SetColor("_TintColor", on ? onColor : offColor);
                halfSprites[2].material.SetFloat("_EmissionGain", on ? .5f : 0);
            }
            else if (multiselectOn)
            {
                manip.MultiselectSelection(on);
                halfSprites[3].material.SetColor("_TintColor", on ? onColor : offColor);
                halfSprites[3].material.SetFloat("_EmissionGain", on ? .5f : 0);
            }
            else
            {
                manip.SetCopy(on);

                halfSprites[1].material.SetColor("_TintColor", on ? onColor : offColor);
                halfSprites[1].material.SetFloat("_EmissionGain", on ? .5f : 0);
            }
          
        }
    }

    public void setQuestionMark(bool on)
    {
        halfSprites[0].material.SetColor("_TintColor", on ? onColor : offColor);
        halfSprites[0].material.SetFloat("_EmissionGain", on ? .5f : 0);
    }
}
