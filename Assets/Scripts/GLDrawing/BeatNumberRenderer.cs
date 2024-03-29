﻿using NoteEditor.Model;
using NoteEditor.Utility;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace NoteEditor.GLDrawing
{
    public class BeatNumberRenderer : SingletonMonoBehaviour<BeatNumberRenderer>
    {
        [SerializeField]
        GameObject beatNumberPrefab = default;

        List<RectTransform> rectTransformPool = new List<RectTransform>();
        List<Text> textPool = new List<Text>();

        static int size;
        static int countPrevActive = 0;
        static int countCurrentActive = 0;

        static public void Render(Vector3 pos, float number)
        {
            if (countCurrentActive < size)
            {
                if (countCurrentActive >= countPrevActive)
                {
                    Instance.textPool[countCurrentActive].gameObject.SetActive(true);
                }

                Instance.rectTransformPool[countCurrentActive].position = pos;
                float secNum = (number / EditData.LPB.Value /EditData.BPM.Value * 60);
                string sec = string.Format("{0:0.00}", secNum);
                Instance.textPool[countCurrentActive].text = sec;
            }
            else
            {
                var obj = Instantiate(Instance.beatNumberPrefab, pos, Quaternion.identity) as GameObject;
                obj.transform.SetParent(Instance.transform);
                obj.transform.localScale = Vector3.one;
                Instance.rectTransformPool.Add(obj.GetComponent<RectTransform>());
                Instance.textPool.Add(obj.GetComponent<Text>());
                size++;
            }

            countCurrentActive++;
        }

        static public void Begin()
        {
            countPrevActive = countCurrentActive;
            countCurrentActive = 0;
        }

        static public void End()
        {
            if (countCurrentActive < countPrevActive)
            {
                for (int i = countCurrentActive; i < countPrevActive; i++)
                {
                    Instance.textPool[i].gameObject.SetActive(false);
                }
            }

            if (countCurrentActive * 2 < size)
            {
                foreach (var text in Instance.textPool.Skip(countCurrentActive + 1))
                {
                    Destroy(text.gameObject);
                }

                Instance.rectTransformPool.RemoveRange(countCurrentActive, size - countCurrentActive);
                Instance.textPool.RemoveRange(countCurrentActive, size - countCurrentActive);
                size = countCurrentActive;
            }
        }
    }
}
