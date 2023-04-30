using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

public class o_bar_destruction : MonoBehaviour
{
    private float scaleBase;

    public int percentToFinish = 100;
    private int scoreTotal = 0;
    private int scoreToFinish = 1;
    private int score = 0;

    public GameObject parent;

    public TextMeshProUGUI text;
    public RectTransform bar;
    public Animation barAnim;
    private AnimationClip barClip;

    public Interval time;
    private float timerAnim = -999;
    private int scoreToSet = 0;

    private List<ODestructive> a_check = new List<ODestructive>();

    public bool hasWin
    {
        get { return score >= scoreToFinish; }
        private set { }
    }

    private void Start()
    {
        ODestructive[] a_toDestruct = parent.GetComponentsInChildren<ODestructive>();

        scoreTotal = 0;
        foreach(ODestructive od in a_toDestruct)
        {
            if (od.breakEvent == null)
            {
                a_check.Add(od);
            }
            else
            {
                scoreTotal += od.weight;
                od.breakEvent.AddListener(Test);
            }
        }

        scoreToFinish = Mathf.Max(1, scoreTotal * percentToFinish/100);

        scaleBase = bar.localScale.y;
        bar.localScale = new Vector3(bar.localScale.x, 0, bar.localScale.z);

        barClip = new AnimationClip();
        barClip.legacy = true;

        UpdateVisual();
    }

    private void Update()
    {
        int length = a_check.Count;
        if (length > 0)
        {
            ODestructive od;

            length--;
            while (length > -1)
            {
                od = a_check[length];
                
                if (od.breakEvent != null)
                {
                    od.breakEvent.AddListener(Test);
                    a_check.RemoveAt(length);

                    scoreTotal += od.weight;
                    scoreToFinish = Mathf.Max(1, scoreTotal * percentToFinish / 100);
                }

                length--;
            }
        }

        if (timerAnim > 0)
        {
            timerAnim -= Time.deltaTime;
        }
        else if (timerAnim != -999)
        {
            score = scoreToSet;
            timerAnim = -999;
        }
    }

    private void Test(int weight)
    {
        scoreToSet += weight;

        UpdateVisual();

        //score = scoreToSet;
    }

    private void UpdateVisual()
    {
        float percent = scoreToSet / (Mathf.Max(1, scoreToFinish) * 1f);

        float size = scoreToSet - score;
        timerAnim = time.Lerp(size / Mathf.Max(1f, scoreToFinish));

        AnimationCurve scale_y = AnimationCurve.Linear(0f, bar.localScale.y, timerAnim, scaleBase * percent);
        barClip.SetCurve("", typeof(RectTransform), "localScale.y", scale_y);
        barAnim.AddClip(barClip, "rescale");
        barAnim.Play("rescale");
        //bar.localScale = new Vector3(bar.localScale.x, scaleBase * percent, bar.localScale.z);
        
        text.text = $"{Mathf.FloorToInt(100 * percent)} %";
    }
}
