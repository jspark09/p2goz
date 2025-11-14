using UnityEngine;

public class SettingsMenuController : MonoBehaviour
{
    public GameObject settingsPanel;  
    public Camera mainCamera;         
    public float distanceFromCamera = 2f;  

    private bool isOpen = false;

    void Start()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);  
    }

    void Update()
    {
        // Press X to open/close settings
        if (Input.GetKeyDown(KeyCode.X))
        {
            isOpen = !isOpen;

            if (settingsPanel != null)
            {
                settingsPanel.SetActive(isOpen);
            }
        }
    }
}

