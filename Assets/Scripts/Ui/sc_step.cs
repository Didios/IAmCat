using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class sc_step : MonoBehaviour
{
    [SerializeField] private Animator ar_diorama;
    [SerializeField] private List<sc_step> a_step;
    [SerializeField] private string level;

    //[SerializeField] private 
    [SerializeField] private ODestructive ob_glass;
    [SerializeField] private bool available = false;

    private bool isTouch = false;
    public bool activate = true;

    private int UILayer;

    private void Start()
    {
        UILayer = LayerMask.NameToLayer("UI");

        if (!PlayerPrefs.HasKey(level))
        {
            PlayerPrefs.SetInt(level, (available) ? 1 : 0);
        }

        available = (PlayerPrefs.GetInt(level) == 1);
    }

    private void Update()
    {
        if (activate && isTouch)
        {
            if (ar_diorama.GetCurrentAnimatorStateInfo(1).IsName("an_diorama_end"))
            {
                isTouch = false;

                if (available)
                {
                    SceneManager.LoadScene(level);
                    //SceneManager.UnloadSceneAsync(SceneManager.GetActiveScene().buildIndex);
                }

                foreach (var step in a_step)
                {
                    step.activate = true;
                }
            }
        }
    }

    private void OnMouseDown()
    {
        if (activate && !IsPointerOverUIElement() && !isTouch)
        {
            if (available)
            {
                ob_glass.Break();
            }

            ar_diorama.SetTrigger("touch");

            isTouch = true;
            foreach (var step in a_step)
            {
                step.activate = false;
            }
        }
    }

    //Returns 'true' if we touched or hovering on Unity UI element.
    public bool IsPointerOverUIElement()
    {
        return IsPointerOverUIElement(GetEventSystemRaycastResults());
    }

    //Returns 'true' if we touched or hovering on Unity UI element.
    private bool IsPointerOverUIElement(List<RaycastResult> eventSystemRaysastResults)
    {
        for (int index = 0; index < eventSystemRaysastResults.Count; index++)
        {
            RaycastResult curRaysastResult = eventSystemRaysastResults[index];
            if (curRaysastResult.gameObject.layer == UILayer)
                return true;
        }
        return false;
    }

    //Gets all event system raycast results of current mouse or touch position.
    static List<RaycastResult> GetEventSystemRaycastResults()
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = Input.mousePosition;
        List<RaycastResult> raysastResults = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, raysastResults);
        return raysastResults;
    }
}
