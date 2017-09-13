using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class UIListViewItemSort : MonoBehaviour 
{
    float centerPos;
    float offsetPos;

    List<Transform> children;

    public void Init(float centerPos, float offsetPos)
    {
        this.centerPos = centerPos;

        this.offsetPos = offsetPos;

        children = new List<Transform>();

        for (int i = 0; i < transform.childCount; i++)
        {
            children.Add(transform.GetChild(i));
        }

        isInit = true;
    }


    bool isInit = false;
    float referPos;
    bool isChanging = false;
    float aPos;
    float bPos;
    void Update()
    {
        if (isInit)
        {
            float referPos = -transform.localPosition.x - centerPos + offsetPos;

            if (children.Count == 0)
            {
                for (int i = 0; i < transform.childCount; i++)
                {
                    children.Add(transform.GetChild(i));
                }
            }

            children.Sort(
                (a, b) =>
                {
                    aPos = Mathf.Abs(a.localPosition.x - referPos);
                    bPos = Mathf.Abs(b.localPosition.x - referPos);

                    if (aPos != bPos)
                    {
                        if (aPos < bPos)
                        {
                            return 1;
                        }
                        isChanging = true;
                        return -1;
                    }
                    else
                    {
                        return 0;
                    }
                }
                );

            if (isChanging)
            {
                for (int i = 0; i < children.Count; i++)
                {
                    children[i].SetSiblingIndex(i);
                }
            }

        }
    }



}
