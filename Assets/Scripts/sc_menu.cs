using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class sc_menu : MonoBehaviour
{
    [Header("In Game")]
    [SerializeField] private string level_unlock = "";
    [SerializeField] private o_bar_destruction bar;
    [SerializeField] private sc_ai_behavior ai;

    [Header("Menu")]
    [SerializeField] private RectTransform screen;
    [SerializeField] private List<Animation> a_canvas = new List<Animation>();

    private int currentCanvas = 0;

    void Start()
    {
        if (screen != null)
        {
            float width = screen.rect.width;// Screen.width;

            AnimationClip clipLeave = new AnimationClip();
            AnimationClip clipEnter = new AnimationClip();

            // define animation curve
            AnimationCurve leaveTranslate = AnimationCurve.Linear(0, 0, 1.5f, width);
            AnimationCurve enterTranslate = AnimationCurve.Linear(0, -width, 1.5f, 0);

            // set animation clip to be legacy
            clipLeave.legacy = true;
            clipEnter.legacy = true;

            clipLeave.SetCurve("", typeof(Transform), "localPosition.x", leaveTranslate);
            clipEnter.SetCurve("", typeof(Transform), "localPosition.x", enterTranslate);

            foreach (Animation anim in a_canvas)
            {
                //TODO
                // find a way to apply transparency during animation
                //float transparency = anim.GetComponent<Image>().color.a;
                //AnimationCurve leavecolor = AnimationCurve.Linear(0, transparency, 1.5f, 0);
                //AnimationCurve entercolor = AnimationCurve.Linear(0, 0, 1.5f, transparency);

                //clipLeave.SetCurve("", typeof(Image), "color.a", leavecolor);
                //clipEnter.SetCurve("", typeof(Image), "color.a", entercolor);

                anim.AddClip(clipLeave, "leave");
                anim.AddClip(clipEnter, "enter");
            }
        }
    }

    private void Update()
    {
        if (bar != null)
        {
            if (bar.hasWin)
            {
                PlayerPrefs.SetInt(level_unlock, 1); // 1 = true, 0 = false

                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;

                ChangeScene("sn_menu");
            }
            else if (ai.isEnd)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;

                ChangeScene("sn_menu");
            }
        }
    }

    public void ChangeCanvas(int index)
    {
        int length = a_canvas.Count;

        if (length == 0)
        {
            return;
        }

        if (index >= length) { index = length - 1; }
        if (index < 0) { index = 0; }

        a_canvas[currentCanvas].Play("leave");
        a_canvas[index].Play("enter");

        currentCanvas = index;
    }

    public void ChangeScene(string scene_name)
    {
        //SceneManager.UnloadSceneAsync(SceneManager.GetActiveScene());
        SceneManager.LoadScene(scene_name);
    }

    public void ResetGame()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.SetInt("sn_lvl_0", 1);
        ChangeScene("sn_menu");
    }

    public void Quit()
    {
        Application.Quit();
        Debug.Log("[MenuManager]\n Game quit");
    }
}
